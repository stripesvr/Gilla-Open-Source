// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaRecycler : MonoBehaviour
{
    private ParticleSystem MakeSparks()
    {
        Material mat = Resources.Load<Material>("Gilla_Spark");
        if (mat == null)
        {
            return null;
        }
        GameObject go = new GameObject("Sparks");
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.gravityModifier = 1.3f;
        main.maxParticles = 160;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0.95f, 0.75f, 1f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.15f, 1f), 0.35f);
        colorKeys[2] = new GradientColorKey(new Color(0.75f, 0.18f, 0.03f, 1f), 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        gradient.SetKeys(colorKeys, alphaKeys);
        col.color = gradient;
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        ParticleSystem.CollisionModule collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.quality = ParticleSystemCollisionQuality.Low;
        collision.collidesWith = -5893;
        collision.bounce = 0.35f;
        collision.dampen = 0.45f;
        collision.lifetimeLoss = 0.2f;
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.velocityScale = 0.035f;
        psr.lengthScale = 1.2f;
        psr.sharedMaterial = mat;
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;
        ps.Play();
        return ps;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null && screen != null)
        {
            // The decompiled body inlined Sfx's private Room/lately/ending bookkeeping;
            // route through Sfx's public entry point instead.
            Sfx.Play(new AudioClip[] { clip }, screen.transform.position, volume, 1f, 1f); /* ? */
        }
    }

    private void Draw()
    {
        dirty = false;
        if (px == null) return;
        if (tex == null) return;
        if (screen == null) return;
        int i = 0;
        for (int y = 0; y < 256; y++)
        {
            Color32 c = (y & 1) == 0 ? Black : Scan;
            for (int x = 0; x < 512; x++)
            {
                px[i + x] = c;
            }
            i += 512;
        }
        if (1f <= awake)
        {
            DrawUi();
        }
        else
        {
            Text(title, 256, 116, "GILLA-RECYCLE", new Color32[] { Dark, Dark }, 3, true);
            if (0f < awake)
            {
                int lines = Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, awake) * 256f);
                DrawUi();
                for (int y = lines; y < 256; y++)
                {
                    for (int x = 0; x < 512; x++)
                    {
                        px[(255 - y) * 512 + x] = (y & 1) == 0 ? Black : Scan;
                    }
                }
                if (lines > 253) lines = 254;
                FillRect(new RectInt(0, lines, 512, 2), Green);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(true);
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }

    private void DrawUi()
    {
        Outline(new RectInt(2, 2, 508, 252), Dim);
        for (int l = 0; 300 + l < 492; l += 12)
        {
            FillRect(new RectInt(300 + l, 2, 7, 2), Green);
        }
        Text(title, 10, 6, "GILLA-RECYCLE", GreenText, 2, false);
        Outline(PriceRect, Green);
        Outline(new RectInt(PriceRect.xMin + 3, PriceRect.yMin + 3, PriceRect.width, PriceRect.height), Dark);
        Text(text, PriceRect.xMin + 6, PriceRect.yMin + 5, "PAYS IN BOLTS", GreenText, 1, false);
        int row = 22;
        for (int i = 0; i < prices.Length; i++)
        {
            int top = PriceRect.yMin;
            Icon(prices[i].label, PriceRect.xMin + 6, row + top - 1);
            Text(text, PriceRect.xMin + 24, row + top, Fit(prices[i].label, 84, 1), GreenText, 1, false);
            string n = prices[i].coins.ToString();
            int w = text == null ? 12 : text.Measure(n, 1);
            Text(text, PriceRect.xMax - w - 6, row + top, n, BoltText, 1, false);
            BoltIcon(PriceRect.xMax - w - 16, row + top, 1);
            row += 21;
        }
        Outline(WalletRect, Green);
        Text(text, WalletRect.xMin + 6, WalletRect.yMin + 5, "YOUR BOLTS", GreenText, 1, false);
        BoltIcon(WalletRect.xMin + 8, WalletRect.yMin + 30, 2);
        Text(text, WalletRect.xMin + 28, WalletRect.yMin + 26, Fit(Coins.BoltsText, WalletRect.width - 34, 3), GreenText, 3, false);
        float s = shown;
        Outline(MainRect, s < 1f ? Green : Yellow);
        if (1f <= s && 0.3f < flash)
        {
            Outline(new RectInt(MainRect.xMin + 1, MainRect.yMin + 1, MainRect.width - 2, MainRect.height - 2), Yellow);
        }
        int cx = MainRect.xMin + MainRect.width / 2;
        if (shown < 0f)
        {
            DrawIdle(cx);
            return;
        }
        DrawWork(cx);
    }

    private void DrawIdle(int cx)
    {
        Text(text, cx, MainRect.yMin + 14, "THROW ITEMS IN", GreenText, 2, true);
        int x = (int)(Time.time * 30f) % 26 + MainRect.xMin + 30;
        while (x < MainRect.xMax - 30)
        {
            Chevron(x, MainRect.yMin + 52, MainRect.xMax - 90 < x ? Green : Dim);
            x += 26;
        }
        Text(text, cx, MainRect.yMin + 82, "FIND THEM ROUND THE MAP", DimText, 1, true);
        FillRect(new RectInt(MainRect.xMin + 10, MainRect.yMin + 100, MainRect.width - 20, 1), Dim);
        Text(text, MainRect.xMin + 10, MainRect.yMin + 108, "LAST RECYCLED", GreenText, 1, false);
        if (log.Count == 0)
        {
            Text(text, cx, MainRect.yMin + 150, "NOTHING YET", DimText, 2, true);
        }
        int row = 129;
        for (int i = 0; i < log.Count; i++)
        {
            int y = row + MainRect.yMin;
            Icon(log[i].label, MainRect.xMin + 12, y);
            Text(text, MainRect.xMin + 30, y - 3, Fit(log[i].label, 190, 2), GreenText, 2, false);
            string s = "+" + log[i].coins.ToString();
            int w = text == null ? 24 : text.Measure(s, 2);
            Text(text, MainRect.xMax - w - 10, y - 3, s, BoltText, 2, false);
            BoltIcon(MainRect.xMax - w - 22, y, 1);
            row += 26;
        }
    }

    private void DrawWork(int cx)
    {
        Text(text, cx, MainRect.yMin + 14, Fit(current.label, MainRect.width - 20, 3), GreenText, 3, true);
        if (1f <= shown)
        {
            string s = "+" + counted.ToString();
            int off;
            if (text == null)
            {
                off = -67;
            }
            else
            {
                off = -((text.Measure(s, 6) + 34) / 2);
            }
            BoltIcon(off + cx, MainRect.yMin + 76, 4);
            Text(text, off + cx + 34, MainRect.yMin + 64, s, BoltText, 6, false);
            Text(text, cx, MainRect.yMin + 124, "BOLTS", BoltText, 2, true);
            if (paid)
            {
                Text(text, cx, MainRect.yMin + 158, current.mine ? "ADDED TO YOUR BOLTS" : "INTO THEIR BOLTS", GreenText, 1, true);
            }
            if (waiting.Count < 1)
            {
                return;
            }
            Text(text, cx, MainRect.yMin + 186, "+" + waiting.Count.ToString() + " MORE GOING IN", DimText, 1, true);
            return;
        }
        int dots = (int)(shown * 6f) % 4;
        Text(text, cx, MainRect.yMin + 62, "RECYCLING" + new string('.', dots) + new string(' ', 3 - dots), GreenText, 2, true);
        int left = MainRect.xMin;
        int top = MainRect.yMin;
        int width = MainRect.width;
        Outline(new RectInt(left + 24, top + 92, width - 48, 22), Green);
        int fill = Mathf.RoundToInt(Mathf.Clamp01(shown) * (width - 52));
        int t = (int)(Time.time * 40f);
        int bottom = Mathf.Max(top + 92, top + 114);
        for (int y = top + 94; y < bottom - 2; y++)
        {
            for (int x = left + 26; x < left + fill + 26; x++)
            {
                Set(x, y, ((t + x + y) / 6 & 1) == 0 ? Green : Dim);
            }
        }
    }

    private void Chevron(int x, int y, Color32 c)
    {
        for (int i = 0; i < 10; i++)
        {
            FillRect(new RectInt(x + i, y + i, 4, 1), c);
            FillRect(new RectInt(x + i, y + 19 - i, 4, 1), c);
        }
    }

    private void Icon(string label, int x, int y)
    {
        if (Icons.TryGetValue(label, out var icon))
        {
            for (int row = 0; row < icon.rows.Length; row++)
            {
                string line = icon.rows[row];
                for (int i = 0; i < line.Length; i++)
                {
                    char ch = line[i];
                    if (ch != '.')
                    {
                        Color32 c;
                        if (ch == 'L') c = icon.l;
                        else if (ch == 'M') c = icon.m;
                        else c = icon.d;
                        Set(x + i, y + row, c);
                    }
                }
            }
        }
        else
        {
            FillRect(new RectInt(x + 2, y + 1, 8, 6), Dim);
        }
    }

    private void BoltIcon(int x, int y, int scale)
    {
        string[] rows = new string[]
        {
            "..###..",
            ".#####.",
            "##LLL##",
            "#LL.LL#",
            "##LLL##",
            ".#####.",
            "..###.."
        };
        for (int row = 0; row < 7; row++)
        {
            for (int i = 0; i < 7; i++)
            {
                char ch = rows[row][i];
                Color32? c;
                if (ch == '#') c = new Color32(150, 160, 170, 255);
                else if (ch == 'L') c = new Color32(215, 225, 232, 255);
                else c = null;
                if (c.HasValue)
                {
                    FillRect(new RectInt(x + i * scale, y + row * scale, scale, scale), c.Value);
                }
            }
        }
    }

    private string Fit(string s, int width, int scale)
    {
        if (text == null || string.IsNullOrEmpty(s))
        {
            return s;
        }
        for (; ; s = s.Substring(0, s.Length - 1))
        {
            if (s.Length < 2) return s;
            if (text.Measure(s, scale) <= width) return s;
        }
    }

    private void Text(ScreenFont font, int x, int y, string s, Color32[] colours, int scale, bool centred)
    {
        if (font != null && !string.IsNullOrEmpty(s))
        {
            ScreenFont.Style style = new ScreenFont.Style { scale = scale, colours = colours };
            if (centred)
            {
                font.DrawCentred(px, 512, 256, x, y, s, style);
            }
            else
            {
                font.Draw(px, 512, 256, x, y, s, style);
            }
        }
    }

    private void Set(int x, int y, Color32 c)
    {
        if (y < 256 && x < 512 && (y | x) >= 0)
        {
            px[x + (255 - y) * 512] = c;
        }
    }

    private void FillRect(RectInt r, Color32 c)
    {
        for (int y = r.yMin; y != r.yMax; y++)
        {
            for (int x = r.xMin; x != r.xMax; x++)
            {
                Set(x, y, c);
            }
        }
    }

    private void Outline(RectInt r, Color32 c)
    {
        int xMax = r.xMax;
        int yMax = r.yMax;
        for (int x = r.xMin; x != xMax; x++)
        {
            Set(x, r.yMin, c);
            Set(x, yMax - 1, c);
        }
        for (int y = r.yMin; y != yMax; y++)
        {
            Set(r.xMin, y, c);
            Set(xMax - 1, y, c);
        }
    }

    public GillaRecycler()
    {
        front = Vector3.forward;
        spinners = new Transform[0];
        axes = new Vector3[0];
        rates = new float[0];
        lampSlot = -1;
        workSpeed = 380f;
        wakeRange = 6f;
        idleSpeed = 30f;
        going = new List<Going>();
        waiting = new Queue<(string label, int coins, bool mine)>();
        log = new List<(string label, int coins)>();
        shown = -1f;
        dirty = true;
        lampColour = new Color(0.12f, 0.55f, 0.2f, 1f);
        lastBalance = -1;
        prices = new (string label, int coins)[0];
    }
}
