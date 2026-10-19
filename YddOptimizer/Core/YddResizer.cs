// YddResize.Core - YddResizer.cs
// CodeWalker.Core.dll ile YDD okuma/yazma + LOD üretme boru hattı.
//
// !! DERLENMEDİ / DENENMEDİ !! CodeWalker sınıf ve üye adları hafızadan yazıldı
// (YddFile, Drawable, DrawableModel, DrawableGeometry, VertexBuffer, IndexBuffer, VertexDeclaration).
// Derleme hatası çıkarsa sadece bu dosyadaki CodeWalker çağrılarını, DLL'deki/kaynaktaki gerçek
// üye adlarıyla düzeltin. MeshSimplifier.cs ve VertexLayout.cs CodeWalker'dan bağımsızdır.
//
// Mantık:
//  - Her LOD isteği için orijinal dosya baytlarından TAZE bir kopya yüklenir ve o kopyadaki
//    hi modeller küçültülür (yüzdeler hep orijinal hi'a göre, hata üst üste binmez).
//  - Küçültülmüş modeller hedef dosyadaki ilgili LOD slotuna atanır (yoksa oluşturulur).
//  - Vertex byte'ları yeniden kodlanmaz, kullanılan vertex satırları kopyalanır.

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using CodeWalker.GameFiles;

namespace YddResize.Core
{
    public enum LodLevel { High, Medium, Low, VeryLow }

    public sealed class ResizeRequest
    {
        public LodLevel Level;
        public double Percent;   // orijinal hi üçgen sayısına göre, 0-100
    }

    public enum VertexColorMode
    {
        None,
        SetWhite,        // 255, 255, 255, 255
        SetBlack,        // 0, 0, 0, 255
        SetAlpha255,     // keep RGB, set Alpha to 255
        CustomColor      // custom R, G, B, A
    }

    public sealed class VertexColorConfig
    {
        public bool Enabled;
        public VertexColorMode Mode = VertexColorMode.None;
        public byte R = 255;
        public byte G = 255;
        public byte B = 255;
        public byte A = 255;
    }

    public sealed class ResizeOptions
    {
        public List<ResizeRequest> Requests = new List<ResizeRequest>();
        public float SkinPenalty = 1.0f;
        public float JointProtect = 2.0f;
        public float UvPenalty = 0.5f;
        public VertexColorConfig Color0 = new VertexColorConfig();
        public VertexColorConfig Color1 = new VertexColorConfig();
        public Action<string> Log;
    }

    public sealed class ResizeReport
    {
        public readonly List<string> Lines = new List<string>();
        public int OriginalHiTriangles;
        public readonly Dictionary<LodLevel, int> ResultTriangles = new Dictionary<LodLevel, int>();
        public long BytesBefore;
        public long BytesAfter;
    }

    public static class YddResizer
    {
        // ---------- Yükleme / kaydetme ----------

        public static YddFile LoadYdd(byte[] data)
        {
            var ydd = new YddFile();
            RpfFile.LoadResourceFile(ydd, data, 165);   // ydd resource version 165
            return ydd;
        }

        public static byte[] SaveYdd(YddFile ydd)
        {
            return ydd.Save();
        }

        private static Drawable[] GetDrawables(YddFile ydd)
        {
            if (ydd.Drawables != null) return ydd.Drawables;
            if (ydd.Dict != null && ydd.Dict.Values != null)
            {
                var arr = new Drawable[ydd.Dict.Values.Count];
                ydd.Dict.Values.CopyTo(arr, 0);
                return arr;
            }
            var list = ydd.DrawableDict != null && ydd.DrawableDict.Drawables != null
                ? ydd.DrawableDict.Drawables.data_items : null;
            return list ?? new Drawable[0];
        }

        // ---------- LOD slotları ----------

        private static DrawableModel[] GetModels(Drawable d, LodLevel lvl)
        {
            if (d == null || d.DrawableModels == null) return null;
            switch (lvl)
            {
                case LodLevel.High: return d.DrawableModels.High;
                case LodLevel.Medium: return d.DrawableModels.Med;
                case LodLevel.Low: return d.DrawableModels.Low;
                default: return d.DrawableModels.VLow;
            }
        }

        private static void SetModels(Drawable d, LodLevel lvl, DrawableModel[] models)
        {
            if (d.DrawableModels == null)
            {
                d.DrawableModels = new DrawableModelsBlock();
                d.DrawableModels.Owner = d;
            }
            switch (lvl)
            {
                case LodLevel.High: d.DrawableModels.High = models; break;
                case LodLevel.Medium: d.DrawableModels.Med = models; break;
                case LodLevel.Low: d.DrawableModels.Low = models; break;
                default: d.DrawableModels.VLow = models; break;
            }

            if (lvl == LodLevel.Medium)
            {
                if (d.LodDistMed <= 0) d.LodDistMed = d.LodDistHigh > 0 ? d.LodDistHigh : 9998f;
                if (d.RenderMaskFlagsMed == 0) d.RenderMaskFlagsMed = d.RenderMaskFlagsHigh;
                if (d.RenderMaskMed == 0) d.RenderMaskMed = d.RenderMaskHigh;
                if (d.FlagsMed == 0) d.FlagsMed = d.FlagsHigh;
            }
            else if (lvl == LodLevel.Low)
            {
                if (d.LodDistLow <= 0) d.LodDistLow = d.LodDistHigh > 0 ? d.LodDistHigh : 9998f;
                if (d.RenderMaskFlagsLow == 0) d.RenderMaskFlagsLow = d.RenderMaskFlagsHigh;
                if (d.RenderMaskLow == 0) d.RenderMaskLow = d.RenderMaskHigh;
                if (d.FlagsLow == 0) d.FlagsLow = d.FlagsHigh;
            }
            else if (lvl == LodLevel.VeryLow)
            {
                if (d.LodDistVlow <= 0) d.LodDistVlow = d.LodDistHigh > 0 ? d.LodDistHigh : 9998f;
                if (d.RenderMaskFlagsVlow == 0) d.RenderMaskFlagsVlow = d.RenderMaskFlagsHigh;
                if (d.RenderMaskVlow == 0) d.RenderMaskVlow = d.RenderMaskHigh;
                if (d.FlagsVlow == 0) d.FlagsVlow = d.FlagsHigh;
            }

            d.BuildAllModels();
        }

        // ---------- Sayım / bellek tahmini ----------

        public static int CountTriangles(YddFile ydd, LodLevel lvl)
        {
            int total = 0;
            foreach (var d in GetDrawables(ydd))
            {
                var models = GetModels(d, lvl);
                if (models == null) continue;
                foreach (var m in models)
                    if (m != null && m.Geometries != null)
                        foreach (var g in m.Geometries)
                            if (g != null && g.IndexBuffer != null && g.IndexBuffer.Indices != null)
                                total += g.IndexBuffer.Indices.Length / 3;
            }
            return total;
        }

        // Tahmini bellek: tüm LOD'larda vertex + index buffer baytları toplamı (gerçek streaming
        // boyutunun alt tahmini; shader/doku/skeleton dahil değildir).
        public static long EstimateBytes(YddFile ydd)
        {
            long total = 0;
            foreach (var d in GetDrawables(ydd))
                foreach (LodLevel lvl in Enum.GetValues(typeof(LodLevel)))
                {
                    var models = GetModels(d, lvl);
                    if (models == null) continue;
                    foreach (var m in models)
                        if (m != null && m.Geometries != null)
                            foreach (var g in m.Geometries)
                            {
                                if (g == null) continue;
                                if (g.VertexBuffer != null && g.VertexBuffer.Data1 != null && g.VertexBuffer.Data1.VertexBytes != null)
                                    total += g.VertexBuffer.Data1.VertexBytes.Length;
                                if (g.IndexBuffer != null && g.IndexBuffer.Indices != null)
                                    total += g.IndexBuffer.Indices.Length * 2L;
                            }
                }
            return total;
        }

        // ---------- Ana boru hattı ----------

        public static byte[] Resize(byte[] original, ResizeOptions opt, out ResizeReport report)
        {
            report = new ResizeReport();
            var target = LoadYdd(original);
            var targetDrawables = GetDrawables(target);

            var probe = LoadYdd(original);
            report.OriginalHiTriangles = CountTriangles(probe, LodLevel.High);
            report.BytesBefore = EstimateBytes(probe);

            foreach (var req in opt.Requests)
            {
                double ratio = Math.Max(0.0, Math.Min(1.0, req.Percent / 100.0));
                var src = LoadYdd(original);                 // taze kopya: hep orijinal hi'dan türet
                var srcDrawables = GetDrawables(src);
                int levelTris = 0;

                for (int i = 0; i < srcDrawables.Length && i < targetDrawables.Length; i++)
                {
                    var hi = GetModels(srcDrawables[i], LodLevel.High);
                    if (hi == null || hi.Length == 0) continue;

                    foreach (var model in hi)
                    {
                        if (model == null || model.Geometries == null) continue;
                        foreach (var g in model.Geometries)
                        {
                            if (g == null) continue;
                            levelTris += SimplifyGeometry(g, ratio, opt, req.Level, i);
                        }
                    }
                    SetModels(targetDrawables[i], req.Level, hi);
                }
                report.ResultTriangles[req.Level] = levelTris;
                Log(opt, string.Format("{0}: %{1:0.#} -> {2} üçgen", req.Level, req.Percent, levelTris));
            }

            if (opt.Color0.Enabled || opt.Color1.Enabled)
            {
                foreach (var d in targetDrawables)
                {
                    ApplyVertexColorsToDrawable(d, opt);
                }
            }

            byte[] result = SaveYdd(target);
            report.BytesAfter = EstimateBytes(target);
            return result;
        }

        // Dönen değer: geometrinin sonuç üçgen sayısı
        private static int SimplifyGeometry(DrawableGeometry g, double ratio, ResizeOptions opt, LodLevel lvl, int drawableIndex)
        {
            var vb = g.VertexBuffer;
            var ib = g.IndexBuffer;
            if (vb == null || ib == null || vb.Data1 == null || vb.Data1.VertexBytes == null || ib.Indices == null)
                return ib != null && ib.Indices != null ? ib.Indices.Length / 3 : 0;

            ushort[] idx16 = ib.Indices;
            int triBefore = idx16.Length / 3;
            byte[] vbytes = vb.Data1.VertexBytes;
            int stride = vb.VertexStride;
            if (stride <= 0) return triBefore;
            int vcount = Math.Min((int)vb.VertexCount, vbytes.Length / stride);

            var layout = VertexLayout.Parse(vb.Info.Flags, (ulong)vb.Info.Types, stride);
            if (layout == null || !layout.CanReadPosition)
            {
                Log(opt, string.Format("  atlandı (drawable {0}, {1}): vertex formatı okunamadı", drawableIndex, lvl));
                return triBefore;
            }

            int target = (int)Math.Round(triBefore * ratio);
            if (target >= triBefore) return triBefore;
            if (target < 4) target = 4;

            var input = new SimplifyInput();
            input.Positions = new Vector3[vcount];
            if (layout.CanReadUv) input.Uv = new Vector2[vcount];
            if (layout.CanReadSkin) { input.SkinWeights = new float[vcount * 4]; input.SkinIndices = new byte[vcount * 4]; }
            for (int v = 0; v < vcount; v++)
            {
                input.Positions[v] = layout.ReadPosition(vbytes, v);
                if (input.Uv != null) input.Uv[v] = layout.ReadUv(vbytes, v);
                if (input.SkinWeights != null) layout.ReadSkin(vbytes, v, input.SkinWeights, input.SkinIndices, v);
            }
            input.Indices = new int[idx16.Length];
            for (int i = 0; i < idx16.Length; i++) input.Indices[i] = idx16[i];

            var so = new SimplifyOptions
            {
                TargetTriangles = target,
                SkinPenalty = opt.SkinPenalty,
                JointProtect = opt.JointProtect,
                UvPenalty = opt.UvPenalty
            };
            SimplifyResult r = MeshSimplifier.Run(input, so);
            if (r.AchievedTriangles > target)
                Log(opt, string.Format("  uyarı (drawable {0}, {1}): hedef {2} üçgen, ulaşılan {3} (kilitli vertex: {4})",
                    drawableIndex, lvl, target, r.AchievedTriangles, r.LockedVertices));

            // vertex satırlarını kopyala
            int newCount = r.OldVertexOfNew.Length;
            var newBytes = new byte[newCount * stride];
            for (int i = 0; i < newCount; i++)
                Buffer.BlockCopy(vbytes, r.OldVertexOfNew[i] * stride, newBytes, i * stride, stride);

            var newIdx = new ushort[r.NewIndices.Length];
            for (int i = 0; i < newIdx.Length; i++) newIdx[i] = (ushort)r.NewIndices[i];

            // geri yaz
            vb.Data1.VertexBytes = newBytes;
            if (vb.Data2 != null) vb.Data2.VertexBytes = newBytes;
            ib.Indices = newIdx;
            TrySet(vb.Data1, "VertexCount", newCount);
            TrySet(vb.Data2, "VertexCount", newCount);
            TrySet(vb, "VertexCount", newCount);
            TrySet(ib, "IndicesCount", newIdx.Length);
            TrySet(g, "VerticesCount", newCount);
            TrySet(g, "IndicesCount", newIdx.Length);
            TrySet(g, "TrianglesCount", newIdx.Length / 3);

            return r.AchievedTriangles;
        }

        // Sayaç alanlarının tipi (ushort/uint/int) sürüme göre değişebildiği için yansıma ile atanır.
        private static void TrySet(object o, string name, int value)
        {
            if (o == null) return;
            var t = o.GetType();
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite) { p.SetValue(o, Convert.ChangeType(value, p.PropertyType)); return; }
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) f.SetValue(o, Convert.ChangeType(value, f.FieldType));
        }

        private static void ApplyVertexColorsToDrawable(Drawable d, ResizeOptions opt)
        {
            if (d == null) return;
            var models = d.AllModels;
            if (models == null && d.DrawableModels != null)
            {
                d.BuildAllModels();
                models = d.AllModels;
            }
            if (models == null) return;

            foreach (var m in models)
            {
                if (m == null || m.Geometries == null) continue;
                foreach (var g in m.Geometries)
                {
                    if (g != null) ApplyVertexColors(g, opt);
                }
            }
        }

        private static void ApplyVertexColors(DrawableGeometry g, ResizeOptions opt)
        {
            if (g == null || g.VertexBuffer == null || g.VertexBuffer.Data1 == null || g.VertexBuffer.Data1.VertexBytes == null) return;
            if (!opt.Color0.Enabled && !opt.Color1.Enabled) return;

            var vb = g.VertexBuffer;
            byte[] vbytes = vb.Data1.VertexBytes;
            int stride = vb.VertexStride;
            if (stride <= 0) return;
            int vcount = Math.Min((int)vb.VertexCount, vbytes.Length / stride);

            var layout = VertexLayout.Parse(vb.Info.Flags, (ulong)vb.Info.Types, stride);
            if (layout == null) return;

            int off0 = layout.Offset[4]; // SemColour0
            int off1 = layout.Offset[5]; // SemColour1

            for (int v = 0; v < vcount; v++)
            {
                int baseOff = v * stride;
                if (opt.Color0.Enabled && off0 >= 0 && baseOff + off0 + 4 <= vbytes.Length)
                {
                    ApplyColorAction(vbytes, baseOff + off0, opt.Color0);
                }
                if (opt.Color1.Enabled && off1 >= 0 && baseOff + off1 + 4 <= vbytes.Length)
                {
                    ApplyColorAction(vbytes, baseOff + off1, opt.Color1);
                }
            }
            if (vb.Data2 != null && vb.Data2.VertexBytes != null)
            {
                Buffer.BlockCopy(vbytes, 0, vb.Data2.VertexBytes, 0, Math.Min(vbytes.Length, vb.Data2.VertexBytes.Length));
            }
        }

        private static void ApplyColorAction(byte[] d, int o, VertexColorConfig cfg)
        {
            switch (cfg.Mode)
            {
                case VertexColorMode.SetWhite:
                    d[o] = 255; d[o + 1] = 255; d[o + 2] = 255; d[o + 3] = 255;
                    break;
                case VertexColorMode.SetBlack:
                    d[o] = 0; d[o + 1] = 0; d[o + 2] = 0; d[o + 3] = 255;
                    break;
                case VertexColorMode.SetAlpha255:
                    d[o + 3] = 255;
                    break;
                case VertexColorMode.CustomColor:
                    d[o] = cfg.R; d[o + 1] = cfg.G; d[o + 2] = cfg.B; d[o + 3] = cfg.A;
                    break;
            }
        }

        private static void Log(ResizeOptions o, string s)
        {
            if (o != null && o.Log != null) o.Log(s);
        }
    }
}
