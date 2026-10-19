// YddResize.Core - VertexLayout.cs
// GTA5 vertex declaration (Flags + Types) -> ofsetler. Sadece okuma amaçlı:
// pozisyon, UV0, skin weight/index. Vertex byte'ları asla yeniden kodlanmaz, satır olarak kopyalanır.
//
// UYARI: Bileşen tipi numaraları ve boyutları CodeWalker'daki VertexComponentType enum'una göre
// hafızadan yazıldı. Parse() sonunda hesaplanan stride, geometrinin gerçek stride'ına EŞİT değilse
// null döner ve o geometri atlanır (güvenlik kontrolü). Tablo yanlışsa log'da "atlandı" görürsünüz,
// o zaman SizeOfType() tablosunu CodeWalker kaynağındaki enum ile karşılaştırıp düzeltin.

using System;
using System.Numerics;

namespace YddResize.Core
{
    public sealed class VertexLayout
    {
        // Semantic sırası: 0 Position, 1 BlendWeights, 2 BlendIndices, 3 Normal, 4 Colour0, 5 Colour1,
        // 6..13 TexCoord0..7, 14 Tangent, 15 Binormal
        public const int SemPosition = 0, SemWeights = 1, SemIndices = 2, SemUv0 = 6;

        public int Stride;
        public readonly int[] Offset = new int[16];
        public readonly int[] Type = new int[16];

        // Tip numaraları (4 bit): 1 Half2, 2 Float, 3 Half4, 4 FloatUnused, 5 Float2, 6 Float3,
        // 7 Float4, 8 UByte4, 9 Colour, 10 RGBA8SNorm
        private static int SizeOfType(int t)
        {
            switch (t)
            {
                case 1: return 4;
                case 2: return 4;
                case 3: return 8;
                case 4: return 4;
                case 5: return 8;
                case 6: return 12;
                case 7: return 16;
                case 8: return 4;
                case 9: return 4;
                case 10: return 4;
                default: return -1;
            }
        }

        public static VertexLayout Parse(uint flags, ulong types, int expectedStride)
        {
            var l = new VertexLayout();
            for (int i = 0; i < 16; i++) l.Offset[i] = -1;
            int off = 0;
            for (int i = 0; i < 16; i++)
            {
                if (((flags >> i) & 1u) == 0) continue;
                int t = (int)((types >> (4 * i)) & 0xFul);
                int size = SizeOfType(t);
                if (size <= 0) return null;
                l.Offset[i] = off;
                l.Type[i] = t;
                off += size;
            }
            l.Stride = off;
            if (off != expectedStride) return null;
            return l;
        }

        public bool CanReadPosition { get { return Offset[SemPosition] >= 0 && Type[SemPosition] == 6; } }

        public bool CanReadUv
        {
            get
            {
                int t = Type[SemUv0];
                return Offset[SemUv0] >= 0 && (t == 1 || t == 5);
            }
        }

        public bool CanReadSkin
        {
            get
            {
                int tw = Type[SemWeights], ti = Type[SemIndices];
                return Offset[SemWeights] >= 0 && Offset[SemIndices] >= 0
                    && (tw == 8 || tw == 9) && (ti == 8 || ti == 9);
            }
        }

        public Vector3 ReadPosition(byte[] d, int v)
        {
            int o = v * Stride + Offset[SemPosition];
            return new Vector3(BitConverter.ToSingle(d, o), BitConverter.ToSingle(d, o + 4), BitConverter.ToSingle(d, o + 8));
        }

        public Vector2 ReadUv(byte[] d, int v)
        {
            int o = v * Stride + Offset[SemUv0];
            if (Type[SemUv0] == 5)
                return new Vector2(BitConverter.ToSingle(d, o), BitConverter.ToSingle(d, o + 4));
            return new Vector2(HalfToFloat(BitConverter.ToUInt16(d, o)), HalfToFloat(BitConverter.ToUInt16(d, o + 2)));
        }

        // Ağırlıklar bayt sırasıyla okunur (/255). Colour tipinde kanal sırası farklıysa sadece
        // skin cezası biraz yanlış hesaplanır; sonuç dosyası etkilenmez.
        public void ReadSkin(byte[] d, int v, float[] w4, byte[] i4, int dstIndex)
        {
            int ow = v * Stride + Offset[SemWeights];
            int oi = v * Stride + Offset[SemIndices];
            for (int k = 0; k < 4; k++)
            {
                w4[dstIndex * 4 + k] = d[ow + k] / 255f;
                i4[dstIndex * 4 + k] = d[oi + k];
            }
        }

        public static float HalfToFloat(ushort h)
        {
            int s = (h >> 15) & 1;
            int e = (h >> 10) & 0x1F;
            int m = h & 0x3FF;
            float val;
            if (e == 0) val = m * 5.9604645e-8f;
            else if (e == 31) return m == 0 ? (s == 1 ? float.NegativeInfinity : float.PositiveInfinity) : float.NaN;
            else val = (1f + m / 1024f) * (float)Math.Pow(2, e - 15);
            return s == 1 ? -val : val;
        }
    }
}
