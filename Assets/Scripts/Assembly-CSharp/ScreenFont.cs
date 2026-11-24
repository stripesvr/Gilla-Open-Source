// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ScreenFont
{
    private struct Glyph
    {
        public int advance;
        public int x;
        public int y;
        public int w;
        public int h;
        public int[] rows;
    }

    public struct Style
    {
        public int scale;
        public float slant;
        public bool bold;
        public Color32[] colours;
        public bool along;
        public Color32 shadow;
    }

    public readonly int size;
    public readonly int ascent;
    public readonly int descent;
    public readonly int cap;
    private readonly Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();
    private readonly Glyph space;

    public ScreenFont(string baked)
    {
        string[] lines = baked.Split('\n');
        int i = 0;
        while (i < lines.Length)
        {
            if (!lines[i].StartsWith("#"))
            {
                break;
            }
            i++;
        }
        string[] header = lines[i].Split(' ');
        size = int.Parse(header[1]);
        ascent = int.Parse(header[3]);
        descent = int.Parse(header[5]);
        cap = int.Parse(header[7]);
        for (int j = i + 1; j < lines.Length; j++)
        {
            string[] parts = lines[j].Trim().Split(' ');
            if (parts.Length > 5)
            {
                if (!parts[0].StartsWith("#"))
                {
                    int advance = int.Parse(parts[1]);
                    int gx = int.Parse(parts[2]);
                    int gy = int.Parse(parts[3]);
                    int gw = int.Parse(parts[4]);
                    int gh = int.Parse(parts[5]);
                    int[] rows = new int[gh];
                    for (int k = 0; k < gh; k++)
                    {
                        rows[k] = Convert.ToInt32(parts[k + 6], 16);
                    }
                    Glyph g = new Glyph();
                    g.advance = advance;
                    g.x = gx;
                    g.y = gy;
                    g.w = gw;
                    g.h = gh;
                    g.rows = rows;
                    glyphs[(char)int.Parse(parts[0])] = g;
                }
            }
        }
        glyphs.TryGetValue(' ', out space);
    }

    private Glyph Find(char c)
    {
        Glyph g;
        if (glyphs.TryGetValue(c, out g))
        {
            return g;
        }
        return space;
    }

    public int Measure(string text, int scale = 1)
    {
        int total = 0;
        for (int i = 0; i < text.Length; i++)
        {
            total += Find(text[i]).advance;
        }
        return total * scale;
    }

    public int Draw(Color32[] px, int w, int h, int x, int y, string text, in Style s)
    {
        int scale = s.scale;
        if (scale < 2)
        {
            scale = 1;
        }
        int width = Measure(text, scale);
        if (s.shadow.a != 0)
        {
            Pass(px, w, h, x, y, text, in s, scale, width, 1, s.shadow);
        }
        Pass(px, w, h, x, y, text, in s, scale, width, 0, default(Color32));
        return width;
    }

    public int DrawCentred(Color32[] px, int w, int h, int cx, int y, string text, in Style s)
    {
        int scale = s.scale;
        if (scale < 2)
        {
            scale = 1;
        }
        int width = Measure(text, scale);
        return Draw(px, w, h, cx - width / 2, y, text, in s);
    }

    private void Pass(Color32[] px, int w, int h, int x, int y, string text, in Style s, int scale, int width, int offset, Color32 solid)
    {
        int baseline = y + ascent * scale;
        int top = baseline - cap * scale;
        int bottom = baseline - 1;
        int span = bottom - top;
        int widthM = width - 1;
        if (span < 2)
        {
            span = 1;
        }
        if (widthM < 2)
        {
            widthM = 1;
        }
        int pen = x;
        for (int index = 0; index < text.Length; index++)
        {
            Glyph g = Find(text[index]);
            for (int r = 0; r < g.h; r++)
            {
                if (g.w > 0)
                {
                    int gx = scale * g.x;
                    int colX = offset + pen + gx;
                    int alongX = (pen - x) + gx;
                    for (int c = 0; c < g.w; c++)
                    {
                        if (((g.rows[r] >> ((g.w - 1 - c) & 31)) & 1) != 0 && scale > 0)
                        {
                            for (int sy = 0; sy < scale; sy++)
                            {
                                int py = y + (g.y + r) * scale + sy;
                                int py2 = py + offset;
                                if (py2 > -1 && py2 < h)
                                {
                                    float slant = s.slant;
                                    Color32 colour = solid;
                                    if (offset < 1)
                                    {
                                        if (!s.along)
                                        {
                                            colour = Gradient(s.colours, (float)(py - top) / (float)span);
                                        }
                                        else
                                        {
                                            colour = default(Color32);
                                        }
                                    }
                                    int startX = colX + (int)(float)(int)(slant * (float)(bottom - py) + 0.5f);
                                    for (int bx = 0; bx < (s.bold ? 1 : 0) + scale; bx++)
                                    {
                                        int xx = startX + bx;
                                        if (xx > -1 && xx < w)
                                        {
                                            Color32 col = colour;
                                            int rowY = py2;
                                            if (offset == 0)
                                            {
                                                rowY = py;
                                                if (s.along)
                                                {
                                                    col = Gradient(s.colours, (float)(alongX + bx) / (float)widthM);
                                                }
                                            }
                                            px[xx + (h - 1 - rowY) * w] = col;
                                        }
                                    }
                                }
                            }
                        }
                        colX += scale;
                        alongX += scale;
                    }
                }
            }
            pen += g.advance * scale;
        }
    }

    public static Color32 Gradient(Color32[] stops, float t)
    {
        if (stops == null || stops.Length == 0)
        {
            return new Color32(255, 0, 0, 255);
        }
        int n = stops.Length;
        if (n - 1 == 0)
        {
            return stops[0];
        }
        float f = Mathf.Clamp01(t) * (float)(n - 1);
        int i = (int)f;
        if (n - 2 <= i)
        {
            i = n - 2;
        }
        Color32 a = stops[i];
        Color32 b = stops[i + 1];
        float k = Mathf.Clamp01(f - (float)i);
        return new Color32(
            (byte)(int)(k * (float)(b.r - a.r) + (float)a.r),
            (byte)(int)(k * (float)(b.g - a.g) + (float)a.g),
            (byte)(int)(k * (float)(b.b - a.b) + (float)a.b),
            (byte)(int)(k * (float)(b.a - a.a) + (float)a.a));
    }
}
