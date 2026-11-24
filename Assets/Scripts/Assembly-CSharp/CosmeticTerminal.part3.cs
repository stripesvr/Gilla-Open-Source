// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class CosmeticTerminal : MonoBehaviour
{
    public CosmeticTerminal()
    {
        wakeRange = 2.8f;
        tiles = new Tile[8];
        leaving = new List<Tile>();
        shown = new List<byte>();
        shownBundles = new List<int>();
        shownTitles = new List<int>();
        cart = new byte[8];
        tryOn = new byte[8];
        pickedBundle = -1;
        drumAt = -1;
        reboot = -1f;
        bannerText = "";
        hovered = "";
        pressedThing = "";
        down = new bool[2];
        downOn = new string[] { "", "" };
        dirty = true;
        lastBalance = -1;
    }

    static CosmeticTerminal()
    {
        BaseMap = Shader.PropertyToID("_BaseMap");
        Categories = new ValueTuple<string, CosmeticSlot>[]
        {
            ("BUNDLES", (CosmeticSlot)255),
            ("SKINS", CosmeticSlot.Skin),
            ("TOYS", CosmeticSlot.Toy),
            ("HATS", CosmeticSlot.Head),
            ("FACE", CosmeticSlot.Face),
            ("EYES", CosmeticSlot.Eyes),
            ("CLOTHING", CosmeticSlot.Body),
            ("BACK", CosmeticSlot.Back),
            ("NAME TAG", (CosmeticSlot)254),
        };
        Filters = new string[] { "SHOW: ALL", "SHOW: OWNED", "SHOW: SHOP" };
        ShowRect = new RectInt(150, 4, 120, 18);
        UpRect = new RectInt(382, 28, 16, 46);
        DownRect = new RectInt(382, 196, 16, 46);
        CartRect = new RectInt(404, 6, 100, 104);
        WalletRect = new RectInt(404, 114, 100, 32);
        ButtonRect = new RectInt(404, 158, 100, 88);
        Black = new Color32(6, 6, 6, 255);
        Scan = new Color32(13, 13, 13, 255);
        Line = GillaBoardLook.Solid;
        Dim = new Color32(96, 96, 96, 255);
        Dark = new Color32(40, 40, 40, 255);
        Bright = new Color32(255, 255, 255, 255);
        White = new Color32(255, 255, 255, 255);
        Warn = new Color32(150, 150, 150, 255);
        Grey = new Color32(70, 70, 70, 255);
        Coin = new Color32(232, 232, 232, 255);
        Plain = GillaBoardLook.Soft;
        Picked = GillaBoardLook.White;
        DimText = GillaBoardLook.Dim;
        Warning = GillaBoardLook.Dim;
        DarkText = new Color32[] { new Color32(6, 6, 6, 255), new Color32(6, 6, 6, 255) };
    }

    private static float PopEase(float p)
    {
        if (p <= 0f) return 0f;
        if (p >= 1f) return 1f;
        return Mathf.Pow(2f, p * -10f) * Mathf.Sin((p * 10f - 0.75f) * 2.0943952f) + 1f;
    }

    private void DrawUi()
    {
        bool ready = false;
        GillaBoardLook.Frame(px, 512, 256, 3);
        Text(title, 10, 6, "GILLA-LAB", Plain, 2, false);
        Text(text, ShowRect.x + 6, ShowRect.y + 5, "CHANGER", DimText, 1, false);
        Clipped(new RectInt(10, 30, 116, 190), DrawUi_b__148_0);

        int n = Categories.Length;
        if (n < 6) n = 5;
        int thumb = n != 0 ? 920 / n : 0;
        if (thumb < 9) thumb = 8;
        FillRect(new RectInt(4, Mathf.RoundToInt(barAt * (184 - thumb)) + 32, 3, thumb), Line);

        Clipped(new RectInt(131, 27, 248, 216), DrawUi_b__148_1);

        if (shown.Count == 0)
        {
            Text(text, 256, 112, "NOTHING OWNED YET", Plain, 1, true);
            Text(text, 256, 126, "BUY THEM ON THE STANDS", DimText, 1, true);
        }
        if (MaxScroll() > 0)
        {
            Button(UpRect, "", "up", 1);
            Arrow(UpRect, true);
            Button(DownRect, "", "down", 1);
            Arrow(DownRect, false);
            Text(text, UpRect.x + 8, 112, (scroll + 1).ToString() ?? "", Plain, 1, true);
            Text(text, UpRect.x + 8, 122, "/", Plain, 1, true);
            Text(text, UpRect.x + 8, 132, (MaxScroll() + 1).ToString() ?? "", Plain, 1, true);
        }

        Outline(CartRect, Dim);
        Text(text, CartRect.x + 6, CartRect.y + 5, "YOUR STUFF", Plain, 1, false);
        string[] lines = new string[] { "PICK ONE YOU", "OWN, THEN", "HOLD THE BUTTON", "TO WEAR IT.", "", "BUY MORE ON", "THE STANDS." };
        int ly = 20;
        for (int i = 0; i < lines.Length; i++)
        {
            Text(text, CartRect.x + 6, ly + CartRect.y, lines[i], i < 4 ? Plain : DimText, 1, false);
            ly += 11;
        }

        Outline(WalletRect, Line);
        CoinIcon(WalletRect.x + 6, WalletRect.y + 5);
        Text(text, WalletRect.x + 17, WalletRect.y + 5, Fit(Coins.Text, WalletRect.width - 22, 1), Plain, 1, false);
        BoltIcon(WalletRect.x + 6, WalletRect.y + 19);
        Text(text, WalletRect.x + 17, WalletRect.y + 19, Fit(Coins.BoltsText, WalletRect.width - 22, 1), Plain, 1, false);

        string action = ButtonAction(out ready);
        int fill = Mathf.RoundToInt(holdFill * ButtonRect.width);
        if (ready && holdFill > 0f)
        {
            FillRect(new RectInt(ButtonRect.x, ButtonRect.y, fill, ButtonRect.height), Dim);
        }
        Outline(ButtonRect, ready ? Line : (action == "NEED MORE" ? Warn : Grey));
        Outline(new RectInt(ButtonRect.x + 2, ButtonRect.y + 2, ButtonRect.width - 4, ButtonRect.height - 4), ready ? Dim : Dark);
        Color32[] cols;
        if (ready) cols = Plain;
        else if (action == "NEED MORE") cols = Warning;
        else cols = new Color32[] { Grey, Grey };
        Text(text, ButtonRect.x + ButtonRect.width / 2, ButtonRect.y + 26, action, cols, 2, true);
        if (ready)
        {
            Text(text, ButtonRect.x + ButtonRect.width / 2, ButtonRect.y + 56, "HOLD", Plain, 1, true);
        }

        if (banner > 0f)
        {
            FillRect(new RectInt(142, 112, 222, 24), Black);
            Outline(new RectInt(142, 112, 222, 24), bannerBad ? Warn : Line);
            Text(text, 253, 118, bannerText, bannerBad ? Warning : Plain, 2, true);
        }
    }

    private void DrawTile(Tile t, RectInt place, int i)
    {
        if (t.id == 0 && t.title == 0) return;
        int x = Mathf.RoundToInt(t.at.x) + place.x;
        int y = Mathf.RoundToInt(t.at.y) + place.y;
        int w = place.width;
        int h = place.height;
        if (t.title > 0)
        {
            TitleTile(t, new RectInt(x, y, w, h), i);
            return;
        }

        CosmeticCatalog.Entry entry = CosmeticCatalog.Of(t.id);
        CosmeticCatalog.Bundle b = CosmeticCatalog.BundleAt(t.bundle);
        bool sel;
        bool owned;
        bool wearing;
        if (b == null)
        {
            sel = t.id == picked;
            owned = Coins.Owns(t.id);
            wearing = Cosmetics.IsWearing(t.id);
        }
        else
        {
            sel = t.bundle == pickedBundle;
            owned = Coins.OwnsAll(b);
            wearing = CosmeticCatalog.IdsOf(b).TrueForAll(Cosmetics.IsWearing);
        }
        bool hover = hovered == "tile" + i.ToString();
        TileOutline(new RectInt(x, y, w, h), PopEase(t.pop), sel, hover);

        string shownLabel = b != null ? b.label /* ? */ : entry.label;
        string[] lines = Wrap(shownLabel, w - 4);
        int cx = x + w / 2;
        for (int j = 0; j < lines.Length; j++)
        {
            Text(text, cx, h + y - 20 - (lines.Length - 1 - j) * 9, lines[j], sel ? Picked : Plain, 1, true);
        }

        if (owned)
        {
            Text(text, cx, h + y - 10, wearing ? "WORN" : "OWNED", wearing ? Picked : Plain, 1, true);
            return;
        }

        int price = b == null ? entry.price : Coins.PriceOf(b);
        string s = price.ToString("N0");
        int bw = text == null ? 40 : text.Measure(s, 1) + 14;
        Color32 col;
        if (b == null)
            col = cart[(int)entry.slot] == t.id ? Bright : White;
        else
            col = sel ? Bright : White;
        int bx = cx - bw / 2;
        int bottom = y + h;
        FillRect(new RectInt(bx, bottom - 11, bw, 10), col);
        CoinIcon(bx + 2, bottom - 10);
        Text(text, bx + 11, bottom - 9, s, DarkText, 1, false);
    }

    private void Clipped(RectInt clip, Action draw)
    {
        if (underneath == null || underneath.Length != px.Length)
            underneath = new Color32[px.Length];
        Array.Copy(px, underneath, px.Length);
        draw();
        int yMax = clip.yMax;
        int xMax = clip.xMax;
        for (int row = 0; row < 256; row++)
        {
            int start = (255 - row) * 512;
            if (row < clip.yMin || row >= yMax)
            {
                Array.Copy(underneath, start, px, start, 512);
            }
            else
            {
                Array.Copy(underneath, start, px, start, clip.xMin);
                Array.Copy(underneath, start + xMax, px, start + xMax, 512 - xMax);
            }
        }
    }

    private void TileOutline(RectInt r, float k, bool sel, bool hover)
    {
        Color32 c = sel ? Bright : (hover ? Line : Dim);
        int hw = Mathf.RoundToInt(r.width * 0.5f * Mathf.Clamp01(k));
        int hh = Mathf.RoundToInt(r.height * 0.5f * Mathf.Clamp01(k));
        int bw = hw * 2;
        if (bw <= 1) return;
        int bh = hh * 2;
        if (bh <= 1) return;
        int rx = r.x + r.width / 2 - hw;
        int ry = r.y + r.height / 2 - hh;
        RectInt box = new RectInt(rx, ry, bw, bh);
        Outline(box, c);
        if (sel)
        {
            Outline(new RectInt(rx + 1, ry + 1, bw - 2, bh - 2), Bright);
        }
        var corners = new ValueTuple<int, int, int, int>[]
        {
            (rx, ry, 1, 1),
            (box.xMax - 1, ry, -1, 1),
            (rx, box.yMax - 1, 1, -1),
            (box.xMax - 1, box.yMax - 1, -1, -1),
        };
        foreach (var (cx, cy, dx, dy) in corners)
        {
            int ax = cx;
            int ay = cy;
            for (int s = 0; s < 5; s++)
            {
                Color32 cc = sel ? Bright : Line;
                Set(ax, cy, cc);
                Set(cx, ay, cc);
                ay += dy;
                ax += dx;
            }
        }
    }

    private void TitleTile(Tile t, RectInt r, int i)
    {
        bool sel = pickedTitle == t.title;
        bool mayWear = NameTitles.MayWear(t.title);
        int worn = NameTitles.Worn;
        int titleId = t.title;
        bool hover = hovered == "tile" + i.ToString();
        TileOutline(r, PopEase(t.pop), sel, hover);
        Color32 c = NameTitles.Colour(t.title);
        string[] words = NameTitles.Label(t.title).Split(' ');
        int boxH = words.Length * 10 + 16;
        int top = r.y + (r.height - words.Length * 10 - 28) / 2;
        FillRect(new RectInt(r.x + 4, top, r.width - 8, boxH), new Color32(10, 10, 10, 255));
        Outline(new RectInt(r.x + 4, top, r.width - 8, boxH), c);
        int ly = top + 4;
        for (int j = 0; j < words.Length; j++)
        {
            string word = Fit(words[j], r.width - 10, 1);
            Text(text, r.x + r.width / 2, ly, word, new Color32[] { NameTitles.Light(t.title), c }, 1, true);
            ly += 10;
        }
        int bottom = Mathf.Max(top, top + boxH);
        string name = Fit(GillaName.Current.ToUpperInvariant(), r.width - 10, 1);
        int cx = r.x + r.width / 2;
        Text(text, cx, bottom - 12, name, new Color32[] { White, White }, 1, true);
        string label;
        Color32[] cols;
        if (mayWear)
        {
            if (worn == titleId)
            {
                cols = Picked;
                label = "WORN";
            }
            else
            {
                cols = Plain;
                label = "EQUIP";
            }
        }
        else
        {
            cols = new Color32[] { Grey, Grey };
            label = "LOCKED";
        }
        Text(text, cx, r.y + r.height - 10, label, cols, 1, true);
    }

    private void Glitch(float k)
    {
        System.Random rng = new System.Random((int)(k * 40f));
        int bands = Mathf.RoundToInt(14f - Mathf.Clamp01(k) * 14f);
        for (int b = 0; b < bands; b++)
        {
            int y0 = rng.Next(256);
            int end = rng.Next(2, 9) + y0;
            int shift = rng.Next(-24, 25);
            if (end > 255) end = 256;
            for (int y = y0; y < end; y++)
            {
                Color32[] row = new Color32[512];
                Array.Copy(px, (255 - y) * 512, row, 0, 512);
                for (int x = 0; x < 512; x++)
                {
                    int src = (x - shift) % 512;
                    if (src < 0) src += 512;
                    Color32 c = row[src];
                    if (rng.Next(5) == 0) c = Line;
                    px[(255 - y) * 512 + x] = c;
                }
            }
        }
        if (k >= 0.12f) return;
        for (int i = 0; i < px.Length; i++)
        {
            Color32 c = px[i];
            px[i] = new Color32(
                (byte)(c.r + (White.r - c.r) * 0.6f),
                (byte)(c.g + (White.g - c.g) * 0.6f),
                (byte)(c.b + (White.b - c.b) * 0.6f),
                (byte)(c.a + (White.a - c.a) * 0.6f));
        }
    }

    private void Button(RectInt r, string label, string id, int scale)
    {
        if (hovered == id)
        {
            FillRect(r, Dark);
        }
        if (pressFlash > 0f && pressedThing == id)
        {
            FillRect(r, White);
        }
        Outline(r, hovered == id ? Line : Dim);
        if (label.Length > 0)
        {
            Text(text, r.x + r.width / 2, r.y + 5, label, Plain, scale, true);
        }
    }

    private void Arrow(RectInt r, bool up)
    {
        int cy = r.y + r.height / 2;
        for (int s = 0; s < 5; s++)
        {
            int y = up ? cy + s - 2 : cy - s + 2;
            FillRect(new RectInt(r.x + r.width / 2 - s, y, 1 + 2 * s, 1), Line);
        }
    }

    private void BoltIcon(int x, int y)
    {
        string[] rows = new string[] { "..###..", ".#####.", "##LLL##", "#LL.LL#", "##LLL##", ".#####.", "..###.." };
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (rows[row][col] == '#')
                    Set(x + col, row + y, new Color32(150, 150, 150, 255));
                else if (rows[row][col] == 'L')
                    Set(x + col, row + y, new Color32(215, 215, 215, 255));
            }
        }
    }

    private void CoinIcon(int x, int y)
    {
        string[] rows = new string[] { "..###..", ".#####.", "##.####", "##.####", "##.####", ".#####.", "..###.." };
        for (int row = 0; row < 7; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (rows[row][col] == '#')
                {
                    Set(x + col, row + y, Coin);
                }
                else if (row >= 2 && row <= 4 && col == 2 && rows[row][col] == '.')
                {
                    Set(x + 2, row + y, new Color32(120, 120, 120, 255));
                }
            }
        }
    }

    private string[] Wrap(string s, int width)
    {
        if (text != null && text.Measure(s, 1) > width)
        {
            int mid = Mathf.Min(s.Length / 2 + 3, s.Length - 1);
            int cut = s.LastIndexOf(' ', mid);
            if (cut > 0 || (cut = s.IndexOf(' ')) > 0)
            {
                string[] two = new string[2];
                two[0] = Fit(s.Substring(0, cut), width, 1);
                two[1] = Fit(s.Substring(cut + 1), width, 1);
                return two;
            }
        }
        return new string[] { Fit(s, width, 1) };
    }

    private string Fit(string s, int width, int scale)
    {
        if (text == null) return s;
        while (s.Length >= 2 && text.Measure(s, scale) > width)
        {
            s = s.Substring(0, s.Length - 1);
        }
        return s;
    }

    private void Text(ScreenFont font, int x, int y, string s, Color32[] colours, int scale, bool centred)
    {
        if (font == null || string.IsNullOrEmpty(s)) return;
        ScreenFont.Style style = new ScreenFont.Style();
        style.scale = scale;
        style.colours = colours;
        if (centred)
            font.DrawCentred(px, 512, 256, x, y, s, style);
        else
            font.Draw(px, 512, 256, x, y, s, style);
    }

    private void Set(int x, int y, Color32 c)
    {
        if (y < 256 && x < 512 && (y | x) >= 0)
        {
            px[(255 - y) * 512 + x] = c;
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
            Set(x, r.y, c);
            Set(x, yMax - 1, c);
        }
        for (int y = r.yMin; y != yMax; y++)
        {
            Set(r.x, y, c);
            Set(xMax - 1, y, c);
        }
    }

    private static RectInt TileRect(int i)
    {
        return new RectInt((i % 4) * 62 + 132, (i / 4) * 108 + 28, 58, 104);
    }

    private static bool Inside(RectInt r, Vector2Int p)
    {
        if (p.x >= r.xMin && p.x < r.xMax && p.y >= r.yMin)
        {
            return p.y < r.yMax;
        }
        return false;
    }

    private Vector2 Size
    {
        get
        {
            return new Vector2(screen.transform.lossyScale.x, screen.transform.lossyScale.y);
        }
    }

    private Vector2Int ToPixel(Vector3 local)
    {
        Vector2 size = Size;
        return new Vector2Int(
            Mathf.FloorToInt((local.x / size.x + 0.5f) * 512f),
            Mathf.FloorToInt((0.5f - local.y / size.y) * 256f));
    }

    private Vector3 ToLocal(Vector2Int p)
    {
        Vector2 size = Size;
        return new Vector3(((p.x + 0.5f) / 512f - 0.5f) * size.x, (0.5f - (p.y + 0.5f) / 256f) * size.y, 0f);
    }

    private Vector3 ToLocal(Vector2 p)
    {
        Vector2 size = Size;
        return new Vector3(((p.x + 0.5f) / 512f - 0.5f) * size.x, (0.5f - (p.y + 0.5f) / 256f) * size.y, 0f);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null)
        {
            Sfx.Play(new AudioClip[] { clip }, screen.transform.position, volume, 0.97f, 1.03f);
        }
    }
}
