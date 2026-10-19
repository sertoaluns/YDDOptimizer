// YddResize.Core - MeshSimplifier.cs
// Saf motor: CodeWalker'a bağımlı DEĞİL. Girdi: pozisyon/UV/skin dizileri + üçgen indeksleri.
// Çıktı: yeni indeks listesi + hangi eski vertex'lerin kullanıldığı.
//
// Yöntem: quadric edge collapse. Vertex a, komşusu b'nin üstüne çöker (yeni vertex ÜRETİLMEZ).
// Bu yüzden vertex byte'larına dokunmadan sadece satır kopyalanır, vertex formatı (FVF) bozulmaz.
//
// Kilitli (hiç hareket etmeyen) vertex'ler:
//   - Aynı pozisyonda birden fazla vertex olanlar (UV seam / hard edge ayrımı) -> UV bozulmaz
//   - Açık kenar (mesh sınırı) üzerindeki vertex'ler
// Hedefe ulaşılamazsa (kilitli vertex çok ise) AchievedTriangles hedeften büyük kalır.
//
// Hedef: .NET Framework 4.8 uyumlu (CodeWalker ile aynı), C# 7.3 sözdizimi.

using System;
using System.Collections.Generic;
using System.Numerics;

namespace YddResize.Core
{
    public sealed class SimplifyOptions
    {
        public int TargetTriangles;        // hedef üçgen sayısı
        public float SkinPenalty = 1.0f;   // kemik ağırlığı farkı cezası
        public float JointProtect = 2.0f;  // çok kemikli (eklem) vertex'leri koruma çarpanı
        public float UvPenalty = 0.5f;     // UV mesafesi cezası
        public float MinNormalDot = 0.2f;  // üçgen normal flip toleransı (1 = hiç değişmesin)
    }

    public sealed class SimplifyInput
    {
        public Vector3[] Positions;   // zorunlu
        public Vector2[] Uv;          // opsiyonel (UV flip kontrolü ve ceza için)
        public float[] SkinWeights;   // opsiyonel, 4 * vertexCount (0..1)
        public byte[] SkinIndices;    // opsiyonel, 4 * vertexCount
        public int[] Indices;         // üçgen listesi
    }

    public sealed class SimplifyResult
    {
        public int[] NewIndices;        // yeni vertex numaralarıyla üçgen listesi
        public int[] OldVertexOfNew;    // yeni vertex i -> eski vertex numarası
        public int OriginalTriangles;
        public int AchievedTriangles;
        public int LockedVertices;
    }

    public static class MeshSimplifier
    {
        public static SimplifyResult Run(SimplifyInput input, SimplifyOptions options)
        {
            var w = new Work(input, options);
            return w.Execute();
        }

        private struct Cand
        {
            public double Cost;
            public int A, B;
        }

        private sealed class MinHeap
        {
            private readonly List<Cand> h = new List<Cand>();
            public int Count { get { return h.Count; } }

            public void Push(Cand c)
            {
                h.Add(c);
                int i = h.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (h[p].Cost <= h[i].Cost) break;
                    Cand t = h[p]; h[p] = h[i]; h[i] = t;
                    i = p;
                }
            }

            public Cand Pop()
            {
                Cand top = h[0];
                int last = h.Count - 1;
                h[0] = h[last];
                h.RemoveAt(last);
                int n = h.Count, i = 0;
                while (true)
                {
                    int l = 2 * i + 1, r = l + 1, s = i;
                    if (l < n && h[l].Cost < h[s].Cost) s = l;
                    if (r < n && h[r].Cost < h[s].Cost) s = r;
                    if (s == i) break;
                    Cand t = h[s]; h[s] = h[i]; h[i] = t;
                    i = s;
                }
                return top;
            }
        }

        private sealed class Work
        {
            private readonly Vector3[] P;
            private readonly Vector2[] Uv;
            private readonly float[] W;
            private readonly byte[] SI;
            private readonly int[] F;
            private readonly SimplifyOptions opt;
            private readonly int n;
            private readonly int triCount;
            private readonly bool[] fDead;
            private readonly List<int>[] vf;
            private readonly bool[] vDead;
            private readonly bool[] locked;
            private readonly double[] Q;
            private float L2;
            private int alive;
            private readonly List<int> na = new List<int>();
            private readonly List<int> nb = new List<int>();
            private readonly List<int> tmp = new List<int>();
            private readonly MinHeap heap = new MinHeap();

            public Work(SimplifyInput inp, SimplifyOptions o)
            {
                P = inp.Positions; Uv = inp.Uv; W = inp.SkinWeights; SI = inp.SkinIndices;
                F = (int[])inp.Indices.Clone();
                opt = o;
                n = P.Length;
                triCount = F.Length / 3;
                alive = triCount;
                fDead = new bool[triCount];
                vDead = new bool[n];
                locked = new bool[n];
                Q = new double[n * 10];
                vf = new List<int>[n];
                for (int i = 0; i < n; i++) vf[i] = new List<int>(6);
                for (int t = 0; t < triCount; t++)
                    for (int k = 0; k < 3; k++) vf[F[t * 3 + k]].Add(t);
            }

            public SimplifyResult Execute()
            {
                var res = new SimplifyResult { OriginalTriangles = triCount };
                if (triCount == 0 || opt.TargetTriangles >= triCount)
                {
                    int[] same;
                    res.OldVertexOfNew = IdentityMap((int[])F.Clone(), out same);
                    res.NewIndices = same;
                    res.AchievedTriangles = triCount;
                    return res;
                }

                ComputeLocks();
                BuildQuadrics();

                for (int a = 0; a < n; a++)
                    if (!locked[a] && vf[a].Count > 0) Enqueue(a);

                long guard = 0, guardMax = 60L * n + 1000;
                while (alive > opt.TargetTriangles && heap.Count > 0 && guard++ < guardMax)
                {
                    Cand c = heap.Pop();
                    if (vDead[c.A] || vDead[c.B] || locked[c.A]) continue;
                    double cost = Cost(c.A, c.B);
                    if (double.IsPositiveInfinity(cost)) continue;
                    if (cost > c.Cost * 1.0001 + 1e-12)
                    {
                        heap.Push(new Cand { Cost = cost, A = c.A, B = c.B });
                        continue;
                    }
                    Collapse(c.A, c.B);
                    Neigh(c.B, tmp);
                    var s = new List<int>(tmp);
                    s.Add(c.B);
                    for (int i = 0; i < s.Count; i++)
                        if (!locked[s[i]] && !vDead[s[i]]) Enqueue(s[i]);
                }

                var outIdx = new List<int>(alive * 3);
                for (int t = 0; t < triCount; t++)
                {
                    if (fDead[t]) continue;
                    outIdx.Add(F[t * 3]); outIdx.Add(F[t * 3 + 1]); outIdx.Add(F[t * 3 + 2]);
                }
                int[] compact;
                res.OldVertexOfNew = IdentityMap(outIdx.ToArray(), out compact);
                res.NewIndices = compact;
                res.AchievedTriangles = alive;
                int lc = 0;
                for (int i = 0; i < n; i++) if (locked[i]) lc++;
                res.LockedVertices = lc;
                return res;
            }

            // Kullanılan vertex'leri ilk görünme sırasıyla yeniden numaralar.
            private int[] IdentityMap(int[] idx, out int[] remapped)
            {
                var map = new int[n];
                for (int i = 0; i < n; i++) map[i] = -1;
                var old = new List<int>();
                remapped = new int[idx.Length];
                for (int i = 0; i < idx.Length; i++)
                {
                    int v = idx[i];
                    if (map[v] < 0) { map[v] = old.Count; old.Add(v); }
                    remapped[i] = map[v];
                }
                return old.ToArray();
            }

            private void ComputeLocks()
            {
                // 1) açık kenar / non-manifold kenar
                var edgeCount = new Dictionary<long, int>();
                for (int t = 0; t < triCount; t++)
                    for (int k = 0; k < 3; k++)
                    {
                        long key = EdgeKey(F[t * 3 + k], F[t * 3 + (k + 1) % 3]);
                        int c; edgeCount.TryGetValue(key, out c);
                        edgeCount[key] = c + 1;
                    }
                for (int t = 0; t < triCount; t++)
                    for (int k = 0; k < 3; k++)
                    {
                        int a = F[t * 3 + k], b = F[t * 3 + (k + 1) % 3];
                        if (edgeCount[EdgeKey(a, b)] != 2) { locked[a] = true; locked[b] = true; }
                    }

                // 2) aynı pozisyondaki birden fazla vertex (UV seam / hard edge)
                var groups = new Dictionary<ValueTuple<long, long, long>, int>();
                var gid = new int[n];
                var gcount = new List<int>();
                for (int v = 0; v < n; v++)
                {
                    var key = new ValueTuple<long, long, long>(
                        (long)Math.Round(P[v].X * 100000.0),
                        (long)Math.Round(P[v].Y * 100000.0),
                        (long)Math.Round(P[v].Z * 100000.0));
                    int g;
                    if (!groups.TryGetValue(key, out g)) { g = gcount.Count; groups[key] = g; gcount.Add(0); }
                    gid[v] = g;
                    gcount[g]++;
                }
                for (int v = 0; v < n; v++)
                    if (gcount[gid[v]] > 1) locked[v] = true;
            }

            private long EdgeKey(int a, int b)
            {
                return a < b ? (long)a * n + b : (long)b * n + a;
            }

            private void BuildQuadrics()
            {
                double areaSum = 0; int areaN = 0;
                double edgeSum = 0; int edgeN = 0;
                for (int t = 0; t < triCount; t++)
                {
                    Vector3 p0 = P[F[t * 3]], p1 = P[F[t * 3 + 1]], p2 = P[F[t * 3 + 2]];
                    areaSum += Vector3.Cross(p1 - p0, p2 - p0).Length() * 0.5; areaN++;
                    edgeSum += (p1 - p0).Length() + (p2 - p1).Length() + (p0 - p2).Length(); edgeN += 3;
                }
                double meanArea = areaN > 0 ? areaSum / areaN : 1.0;
                if (meanArea < 1e-12) meanArea = 1e-12;
                double meanEdge = edgeN > 0 ? edgeSum / edgeN : 1.0;
                L2 = (float)(meanEdge * meanEdge);

                for (int t = 0; t < triCount; t++)
                {
                    int i0 = F[t * 3], i1 = F[t * 3 + 1], i2 = F[t * 3 + 2];
                    Vector3 p0 = P[i0], p1 = P[i1], p2 = P[i2];
                    Vector3 nrm = Vector3.Cross(p1 - p0, p2 - p0);
                    double len = nrm.Length();
                    if (len < 1e-12) continue;
                    nrm /= (float)len;
                    double d = -(nrm.X * p0.X + nrm.Y * p0.Y + nrm.Z * p0.Z);
                    double wgt = (len * 0.5) / meanArea;
                    AddPlane(i0, nrm.X, nrm.Y, nrm.Z, d, wgt);
                    AddPlane(i1, nrm.X, nrm.Y, nrm.Z, d, wgt);
                    AddPlane(i2, nrm.X, nrm.Y, nrm.Z, d, wgt);
                }
            }

            private void AddPlane(int v, double a, double b, double c, double d, double w)
            {
                int o = v * 10;
                Q[o + 0] += w * a * a; Q[o + 1] += w * a * b; Q[o + 2] += w * a * c; Q[o + 3] += w * a * d;
                Q[o + 4] += w * b * b; Q[o + 5] += w * b * c; Q[o + 6] += w * b * d;
                Q[o + 7] += w * c * c; Q[o + 8] += w * c * d;
                Q[o + 9] += w * d * d;
            }

            private double Eval(int v, Vector3 p)
            {
                int o = v * 10;
                double x = p.X, y = p.Y, z = p.Z;
                return Q[o + 0] * x * x + 2 * Q[o + 1] * x * y + 2 * Q[o + 2] * x * z + 2 * Q[o + 3] * x
                     + Q[o + 4] * y * y + 2 * Q[o + 5] * y * z + 2 * Q[o + 6] * y
                     + Q[o + 7] * z * z + 2 * Q[o + 8] * z
                     + Q[o + 9];
            }

            private bool Has(int t, int v)
            {
                return F[t * 3] == v || F[t * 3 + 1] == v || F[t * 3 + 2] == v;
            }

            private void Neigh(int v, List<int> outList)
            {
                outList.Clear();
                var fl = vf[v];
                for (int i = 0; i < fl.Count; i++)
                {
                    int t = fl[i];
                    for (int k = 0; k < 3; k++)
                    {
                        int u = F[t * 3 + k];
                        if (u != v && !outList.Contains(u)) outList.Add(u);
                    }
                }
            }

            private void Enqueue(int a)
            {
                Neigh(a, na);
                var copy = new List<int>(na);
                for (int i = 0; i < copy.Count; i++)
                {
                    double c = Cost(a, copy[i]);
                    if (!double.IsPositiveInfinity(c))
                        heap.Push(new Cand { Cost = c, A = a, B = copy[i] });
                }
            }

            private static float Cross2(Vector2 a, Vector2 b) { return a.X * b.Y - a.Y * b.X; }

            private float WeightOf(int v, byte bone)
            {
                float s = 0;
                for (int k = 0; k < 4; k++) if (SI[v * 4 + k] == bone) s += W[v * 4 + k];
                return s;
            }

            private float SkinDist(int a, int b)
            {
                float d = 0;
                for (int k = 0; k < 4; k++)
                {
                    if (W[a * 4 + k] > 0f) { byte bone = SI[a * 4 + k]; d += Math.Abs(WeightOf(a, bone) - WeightOf(b, bone)) * 0.25f; }
                    if (W[b * 4 + k] > 0f) { byte bone = SI[b * 4 + k]; d += Math.Abs(WeightOf(a, bone) - WeightOf(b, bone)) * 0.25f; }
                }
                return d;
            }

            private int Influences(int v)
            {
                int c = 0;
                for (int k = 0; k < 4; k++) if (W[v * 4 + k] > 0.05f) c++;
                return Math.Min(3, Math.Max(0, c - 1));
            }

            // a vertex'i b'nin üstüne çökerse maliyet. Geçersizse +sonsuz.
            private double Cost(int a, int b)
            {
                if (vDead[a] || vDead[b]) return double.PositiveInfinity;
                var fa = vf[a];
                int shared = 0;
                for (int i = 0; i < fa.Count; i++) if (Has(fa[i], b)) shared++;
                if (shared != 2) return double.PositiveInfinity;

                // link condition: ortak komşu sayısı tam 2 olmalı
                Neigh(a, na); Neigh(b, nb);
                int common = 0;
                for (int i = 0; i < na.Count; i++) if (na[i] != b && nb.Contains(na[i])) common++;
                if (common != 2) return double.PositiveInfinity;

                Vector3 pb = P[b];
                for (int i = 0; i < fa.Count; i++)
                {
                    int t = fa[i];
                    if (Has(t, b)) continue;
                    int i0 = F[t * 3], i1 = F[t * 3 + 1], i2 = F[t * 3 + 2];
                    Vector3 p0 = P[i0], p1 = P[i1], p2 = P[i2];
                    Vector3 q0 = i0 == a ? pb : p0;
                    Vector3 q1 = i1 == a ? pb : p1;
                    Vector3 q2 = i2 == a ? pb : p2;
                    Vector3 n0 = Vector3.Cross(p1 - p0, p2 - p0);
                    Vector3 n1 = Vector3.Cross(q1 - q0, q2 - q0);
                    float l0 = n0.Length(), l1 = n1.Length();
                    if (l1 < 1e-10f) return double.PositiveInfinity;
                    if (l0 > 1e-10f && Vector3.Dot(n0, n1) / (l0 * l1) < opt.MinNormalDot) return double.PositiveInfinity;

                    if (Uv != null)
                    {
                        Vector2 u0 = Uv[i0], u1 = Uv[i1], u2 = Uv[i2];
                        Vector2 v0 = i0 == a ? Uv[b] : u0;
                        Vector2 v1 = i1 == a ? Uv[b] : u1;
                        Vector2 v2 = i2 == a ? Uv[b] : u2;
                        float s0 = Cross2(u1 - u0, u2 - u0);
                        float s1 = Cross2(v1 - v0, v2 - v0);
                        if (s0 * s1 < 0f) return double.PositiveInfinity; // UV ters döndü
                        if (Math.Abs(s0) > 1e-9f && Math.Abs(s1) < Math.Abs(s0) * 0.01f) return double.PositiveInfinity; // UV çöktü
                    }
                }

                double err = Eval(a, pb);
                if (err < 0) err = 0;
                double extra = 0;
                if (Uv != null) extra += opt.UvPenalty * (Uv[a] - Uv[b]).LengthSquared() * L2;
                double mult = 1.0;
                if (W != null && SI != null)
                {
                    extra += opt.SkinPenalty * SkinDist(a, b) * L2;
                    mult += opt.JointProtect * Influences(a) / 3.0;
                }
                return (err + extra) * mult;
            }

            private void Collapse(int a, int b)
            {
                var fl = new List<int>(vf[a]);
                for (int i = 0; i < fl.Count; i++)
                {
                    int t = fl[i];
                    if (Has(t, b))
                    {
                        fDead[t] = true; alive--;
                        for (int k = 0; k < 3; k++) vf[F[t * 3 + k]].Remove(t);
                    }
                    else
                    {
                        for (int k = 0; k < 3; k++) if (F[t * 3 + k] == a) F[t * 3 + k] = b;
                        vf[b].Add(t);
                    }
                }
                vf[a].Clear();
                vDead[a] = true;
                for (int k = 0; k < 10; k++) Q[b * 10 + k] += Q[a * 10 + k];
            }
        }
    }
}
