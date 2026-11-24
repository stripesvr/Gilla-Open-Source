// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public static class PixelFont
{
    public const int W = 5;
    public const int H = 7;
    public const string AtlasChars = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ:.!?-><*/#+^~'=()[]";
    public const int AtlasCell = 8;
    public const int AtlasCols = 16;
    private static readonly Dictionary<char, byte[]> glyphs;

    public static bool Lit(char c, int x, int y)
    {
        byte[] rows;
        if (glyphs.TryGetValue(char.ToUpperInvariant(c), out rows))
        {
            return ((rows[y] >> (4 - x)) & 1) != 0;
        }
        return false;
    }

    public static int Measure(string text, int scale = 1)
    {
        int result = 0;
        if (text.Length != 0)
        {
            result = (text.Length * 6 - 1) * scale;
        }
        return result;
    }

    public static void Draw(Color32[] px, int w, int h, int x, int y, string text, Color32 top, Color32 bottom)
    {
        for (int index = 0; index < text.Length; index++)
        {
            int cx = x + index * 6;
            for (int row = 0; row < 7; row++)
            {
                float t = Mathf.Clamp01(row / 6f);
                for (int col = 0; col < 5; col++)
                {
                    if (Lit(text[index], col, row) && row + y < h && cx + col < w && cx + col >= 0 && row + y >= 0)
                    {
                        px[(h - 1 - (y + row)) * w + cx + col] = new Color32(
                            (byte)(int)(t * (bottom.r - top.r) + top.r),
                            (byte)(int)(t * (bottom.g - top.g) + top.g),
                            (byte)(int)(t * (bottom.b - top.b) + top.b),
                            (byte)(int)(t * (bottom.a - top.a) + top.a));
                    }
                }
            }
        }
    }

    public static void Draw(Color32[] px, int w, int h, int x, int y, string text, Color32 colour, int scale = 1)
    {
        for (int index = 0; index < text.Length; index++)
        {
            int cx = x + index * scale * 6;
            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    bool lit = Lit(text[index], col, row);
                    if (scale > 0 && lit)
                    {
                        for (int sy = 0; sy < scale; sy++)
                        {
                            int py = y + row * scale + sy;
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int pxX = cx + col * scale + sx;
                                if (py < h && pxX < w && pxX >= 0 && py >= 0)
                                {
                                    px[(h - 1 - py) * w + pxX] = colour;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static void Fill(Color32[] px, int w, int h, int x, int y, int rw, int rh, Color32 colour)
    {
        int y0 = Mathf.Max(y, 0);
        int y1 = Mathf.Min(y + rh, h);
        int x0 = Mathf.Max(x, 0);
        int x1 = Mathf.Min(x + rw, w);
        for (int row = y0; row < y1; row++)
        {
            for (int col = x0; col < x1; col++)
            {
                px[(h - 1 - row) * w + col] = colour;
            }
        }
    }

    public static Color32[] AtlasPixels(out int w, out int h)
    {
        w = 128;
        h = ((AtlasChars.Length + 15) / 16) * 8;
        Color32[] px = new Color32[h * w];
        for (int i = 0; i < AtlasChars.Length; i++)
        {
            Draw(px, w, h, (i % 16) * 8 + 1, (i / 16) * 8, AtlasChars[i].ToString(), new Color32(255, 255, 255, 255), 1);
        }
        return px;
    }

    public static Rect AtlasRect(char c, int atlasW, int atlasH)
    {
        int idx = Mathf.Max(AtlasChars.IndexOf(char.ToUpperInvariant(c)), 0);
        float cw = 8f / atlasW;
        float ch = 8f / atlasH;
        return new Rect(cw * (idx % 16), 1f - ch * (idx / 16 + 1), cw, ch);
    }

    static PixelFont()
    {
        Dictionary<char, byte[]> d = new Dictionary<char, byte[]>();
        d.Add('$', new byte[7] /* ? DAT_06132940 */);
        d.Add(',', new byte[7] /* ? DAT_06132a18 */);
        d.Add('0', new byte[7] /* ? DAT_06132910 */);
        d.Add('1', new byte[7] /* ? DAT_06132880 */);
        d.Add('2', new byte[7] /* ? DAT_06132850 */);
        d.Add('3', new byte[7] /* ? DAT_061329c8 */);
        d.Add('4', new byte[7] /* ? DAT_06132930 */);
        d.Add('5', new byte[7] /* ? DAT_061329f0 */);
        d.Add('6', new byte[7] /* ? DAT_06132890 */);
        d.Add('7', new byte[7] /* ? DAT_06132a58 */);
        d.Add('8', new byte[7] /* ? DAT_061328b0 */);
        d.Add('9', new byte[7] /* ? DAT_06132a08 */);
        d.Add('A', new byte[7] /* ? DAT_06132938 */);
        d.Add('B', new byte[7] /* ? DAT_06132a00 */);
        d.Add('C', new byte[7] /* ? DAT_061329d8 */);
        d.Add('D', new byte[7] /* ? DAT_06132a38 */);
        d.Add('E', new byte[7] /* ? DAT_06132a28 */);
        d.Add('F', new byte[7] /* ? DAT_061328f0 */);
        d.Add('G', new byte[7] /* ? DAT_061329e8 */);
        d.Add('H', new byte[7] /* ? DAT_061328e0 */);
        d.Add('I', new byte[7] /* ? DAT_06132900 */);
        d.Add('J', new byte[7] /* ? DAT_06132870 */);
        d.Add('K', new byte[7] /* ? DAT_061328f8 */);
        d.Add('L', new byte[7] /* ? DAT_061328a0 */);
        d.Add('M', new byte[7] /* ? DAT_06132a60 */);
        d.Add('N', new byte[7] /* ? DAT_06132988 */);
        d.Add('O', new byte[7] /* ? DAT_061329c0 */);
        d.Add('P', new byte[7] /* ? DAT_06132948 */);
        d.Add('Q', new byte[7] /* ? DAT_06132868 */);
        d.Add('R', new byte[7] /* ? DAT_06132a20 */);
        d.Add('S', new byte[7] /* ? DAT_06132998 */);
        d.Add('T', new byte[7] /* ? DAT_061329f8 */);
        d.Add('U', new byte[7] /* ? DAT_061329a8 */);
        d.Add('V', new byte[7] /* ? DAT_06132888 */);
        d.Add('W', new byte[7] /* ? DAT_06132928 */);
        d.Add('X', new byte[7] /* ? DAT_061328b8 */);
        d.Add('Y', new byte[7] /* ? DAT_06132828 */);
        d.Add('Z', new byte[7] /* ? DAT_06132a50 */);
        d.Add(':', new byte[7] /* ? DAT_06132970 */);
        d.Add('.', new byte[] { 0, 0, 0, 0, 0, 0x0C, 0x0C });
        d.Add('!', new byte[7] /* ? DAT_06132838 */);
        d.Add('?', new byte[7] /* ? DAT_06132980 */);
        d.Add('-', new byte[] { 0, 0, 0, 0x1F, 0, 0, 0 });
        d.Add('>', new byte[7] /* ? DAT_06132950 */);
        d.Add('<', new byte[7] /* ? DAT_06132898 */);
        d.Add('*', new byte[7] /* ? DAT_06132918 */);
        d.Add('/', new byte[7] /* ? DAT_06132990 */);
        d.Add('#', new byte[7] /* ? DAT_06132a40 */);
        d.Add('+', new byte[7] /* ? DAT_06132960 */);
        d.Add('^', new byte[7] /* ? DAT_061328c8 */);
        d.Add('~', new byte[7] /* ? DAT_06132978 */);
        d.Add('[', new byte[7] /* ? DAT_061328c0 */);
        d.Add(']', new byte[7] /* ? DAT_06132968 */);
        d.Add('\'', new byte[7] /* ? DAT_061329a0 */);
        d.Add('_', new byte[] { 0, 0, 0, 0, 0, 0, 0x1F });
        d.Add('=', new byte[] { 0, 0, 0x1F, 0, 0x1F, 0, 0 });
        d.Add('(', new byte[7] /* ? DAT_061329e0 */);
        d.Add(')', new byte[7] /* ? DAT_061328d0 */);
        glyphs = d;
    }
}
