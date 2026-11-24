// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using Fusion;

[ExecuteAlways]
public class GillaBoltShop : MonoBehaviour
{
    public class Item
    {
        public string name;
        public string label;
        public string line1;
        public string line2;
        public int price;
        public string[] icon;
        public Color32 light = C(250, 110, 95);
        public Color32 mid = C(215, 45, 40);
        public Color32 dark = C(120, 22, 20);
    }

    public static readonly Item[] Catalog = new Item[]
    {
        new Item
        {
            name = "Dynamite",
            label = "DYNAMITE",
            price = 50,
            line1 = "TOP BUTTON LIGHTS IT",
            line2 = "5 SECONDS, THEN BOOM",
            icon = new string[]
            {
                "............SS",
                "...........SFS",
                "..........F.S.",
                ".LLLLLLLLLF...",
                "LMMMMMMMMMMD..",
                "MMMMMMMMMMMD..",
                "MMMMMMMMMMMD..",
                "DDDDDDDDDDDD..",
            },
        },
        new Item
        {
            name = "BearTrap",
            label = "BEAR TRAP",
            price = 35,
            line1 = "SETS WHEN IT LIES FLAT",
            line2 = "KILLS, HOLDS MONSTERS",
            light = new Color32(190, 195, 200, 255),
            mid = new Color32(120, 125, 130, 255),
            dark = new Color32(62, 58, 56, 255),
            icon = new string[]
            {
                "L.L.L..L.L.L..",
                "LML.M..M.LML..",
                "M..........M..",
                "M...DDDD...M..",
                "D...DLLD...D..",
                "MM..DDDD..MM..",
                ".MMMMMMMMMM...",
                "..DDDDDDDD....",
            },
        },
        new Item
        {
            name = "Egg",
            label = "EGG",
            price = 5,
            line1 = "THROW IT AT SOMEONE",
            line2 = "IT BREAKS, THEY WEAR IT",
            light = new Color32(250, 244, 232, 255),
            mid = new Color32(226, 212, 190, 255),
            dark = new Color32(170, 150, 126, 255),
            icon = new string[]
            {
                ".....LL.......",
                "....LLLL......",
                "...LLLLLL.....",
                "..LLLLLMMM....",
                "..LLLLMMMM....",
                "..LLMMMMMD....",
                "...MMMMMD.....",
                "....DDDD......",
            },
        },
    };

    public const float SellsBack = 0.35f;

    [Header("Parts (GillaSetup.Recycler builds them)")]
    public Renderer screen;
    public PressKey buyKey;
    [Tooltip("The BUY key's face.")]
    public Renderer buyCap;
    [Tooltip("The key that goes to the next thing for sale.")]
    public PressKey nextKey;
    public Renderer nextCap;
    [Tooltip("Where what's bought drops in: the recycler's tray.")]
    public Transform tray;
    public TextAsset titleFont;
    public TextAsset textFont;
    public AudioClip buySound;
    public AudioClip denySound;
    [Tooltip("Between one buy and the next (s): a held finger doesn't empty your bolts.")]
    public float buyEvery = 1.2f;

    private const int W = 256;
    private const int H = 144;
    private const int KW = 96;
    private const int KH = 32;

    private static readonly Color32 Black = new Color32(2, 6, 4, 255);
    private static readonly Color32 Scan = new Color32(4, 12, 8, 255);
    private static readonly Color32 Line = new Color32(40, 70, 52, 255);
    private static readonly Color32 Bracket = new Color32(90, 120, 100, 255);
    private static readonly Color32 Mint = new Color32(80, 230, 170, 255);
    private static readonly Color32 White = new Color32(235, 245, 240, 255);
    private static readonly Color32 Red = new Color32(255, 80, 70, 255);
    private static readonly Color32[] GreenText = { new Color32(170, 255, 190, 255), new Color32(60, 235, 110, 255) };
    private static readonly Color32[] DimText = { new Color32(40, 150, 80, 255), new Color32(20, 110, 55, 255) };
    private static readonly Color32[] WhiteText = { new Color32(250, 255, 252, 255), new Color32(190, 210, 200, 255) };
    private static readonly Color32[] MintText = { new Color32(190, 255, 225, 255), new Color32(80, 230, 170, 255) };
    private static readonly Color32[] RedText = { new Color32(255, 170, 160, 255), new Color32(240, 70, 60, 255) };
    private static readonly Color32[] BoltText = { new Color32(240, 246, 252, 255), new Color32(160, 182, 205, 255) };

    private GillaMapVote.Canvas canvas;
    private GillaMapVote.Canvas cap;
    private GillaMapVote.Canvas nextFace;
    private ScreenFont title;
    private ScreenFont text;
    private int selected;
    private int shownBolts = -1;
    private string toast;
    private Color32[] toastColours;
    private float toastUntil;
    private float nextBuy;
    private float pressedUntil;
    private bool dirty = true;
    private float nextTurn;
    private float nextPressedUntil;
    [Tooltip("The next key's click.")]
    public AudioClip nextSound;

    public static int SellBack(string name)
    {
        for (int i = 0; i < Catalog.Length; i++)
        {
            Item item = Catalog[i];
            if (item.name == name)
            {
                int v = Mathf.RoundToInt(item.price * 0.35f);
                if (1 < v) return v;
                return 1;
            }
        }
        return 0;
    }

    public static bool Sells(string label)
    {
        return Array.Exists(Catalog, i => i.label == label);
    }

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void OnEnable()
    {

        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        canvas = screen != null ? new GillaMapVote.Canvas(screen, 256, 144) : null;
        cap = buyCap != null ? new GillaMapVote.Canvas(buyCap, 96, 32) : null;
        nextFace = nextCap != null ? new GillaMapVote.Canvas(nextCap, 96, 32) : null;
        dirty = true;
        Draw();
        if (!Application.isPlaying) return;
        if (buyKey != null) buyKey.Pressed += OnBuy;
        if (nextKey != null) nextKey.Pressed += OnNext;
        Coins.Changed += OnCoins;
    }

    private void OnDisable()
    {
        if (buyKey != null) buyKey.Pressed -= OnBuy;
        if (nextKey != null) nextKey.Pressed -= OnNext;
        Coins.Changed -= OnCoins;
    }

    private void OnNext(PressKey key)
    {
        if (Time.time < nextTurn) return;
        if (Catalog.Length == 0) return;
        nextTurn = Time.time + 0.25f;
        toast = null;
        selected = (selected + 1) % Catalog.Length;
        nextPressedUntil = Time.time + 0.2f;
        dirty = true;
        Play(nextSound);
    }

    private void OnCoins()
    {
        dirty = true;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (toast != null && toastUntil < Time.time)
        {
            toast = null;
            dirty = true;
        }
        if (0f < pressedUntil && pressedUntil < Time.time)
        {
            pressedUntil = 0f;
            dirty = true;
        }
        if (0f < nextPressedUntil && nextPressedUntil < Time.time)
        {
            nextPressedUntil = 0f;
            dirty = true;
        }
        if (Coins.Bolts == shownBolts)
        {
            if (!dirty) return;
        }
        else
        {
            dirty = true;
        }
        Draw();
    }

    private void OnBuy(PressKey key)
    {
        string why = null;
        if (Time.time < nextBuy) return;
        if (Catalog.Length == 0) return;
        nextBuy = Time.time + buyEvery;
        pressedUntil = Time.time + 0.25f;
        Item item = Catalog[Mathf.Clamp(selected, 0, Catalog.Length - 1)];
        int bolts = Coins.Bolts;
        int need = item.price - bolts;
        string msg;
        if (need == 0 || item.price < bolts)
        {
            if (Spawn(item, out why))
            {
                Coins.SpendBolts(item.price, item.name);
                Say(item.label + " IN THE TRAY", MintText);
                Play(buySound);
                return;
            }
            msg = why;
        }
        else
        {
            msg = "NEED " + need.ToString() + " MORE BOLTS";
        }
        Say(msg, RedText);
        Play(denySound);
    }

    private void Say(string s, Color32[] colours)
    {
        toast = s;
        toastColours = colours;
        dirty = true;
        toastUntil = Time.time + 3f;
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        AudioClip[] clips = new AudioClip[] { clip };
        Component at = buyKey != null ? (Component)buyKey : this;
        Sfx.Play(clips, at.transform.position, 0.8f, 0.97f, 1.03f);
    }

    private bool Spawn(Item item, out string why)
    {
        why = null;
        GillaSpawnables sp = GillaSpawnables.Get();
        GameObject prefab = null;
        if (sp != null)
        {
            if (item.name == "Dynamite") prefab = sp.dynamite;
            else if (item.name == "BearTrap") prefab = sp.bearTrap;
            else if (item.name == "Egg") prefab = sp.egg;
        }
        if (prefab == null)
        {
            why = "SOLD OUT IN THIS BUILD";
            return false;
        }
        Vector3 pos;
        if (tray != null) pos = tray.position + Vector3.up * 0.06f;
        else pos = transform.position + Vector3.up;
        Quaternion rot;
        if (tray != null) rot = Quaternion.LookRotation(tray.right, Vector3.up);
        else rot = Quaternion.identity;

        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (boot != null) runner = boot.GetComponent<NetworkRunner>() /* ? */;
        if (runner != null && runner.IsRunning)
        {
            if (boot.Joining)
            {
                why = "STILL JOINING - TRY AGAIN";
                return false;
            }
            NetworkObject netObj;
            if (prefab.TryGetComponent<NetworkObject>(out netObj))
            {
                NetworkObject spawned = runner.Spawn(netObj, pos, rot, null, null, (NetworkSpawnFlags)4);
                if (spawned != null) return true;
                why = "THAT DIDN'T WORK";
                return false;
            }
            why = "THAT DIDN'T WORK";
            return false;
        }
        GameObject go = UnityEngine.Object.Instantiate<GameObject>(prefab, pos, rot);
        go.name = prefab.name;
        return true;
    }

    private void Draw()
    {
        dirty = false;
        shownBolts = Coins.Bolts;
        if (canvas != null) DrawScreen(canvas);
        if (cap != null) DrawKey(cap);
        if (nextFace != null) DrawNext(nextFace);
    }

    private void DrawScreen(GillaMapVote.Canvas c)
    {
        c.Clear(Black, Scan);
        c.Outline(new RectInt(0, 0, 256, 144), Line);
        c.Text(title, 8, 6, "[ BOLT SHOP ]", GreenText, 1, false);
        string bolts = Coins.BoltsText;
        int w = text == null ? 30 : text.Measure(bolts, 2);
        c.Text(text, 248 - w, 5, bolts, BoltText, 2, false);
        BoltIcon(c, 230 - w, 6, 2);
        c.Fill(new RectInt(6, 24, 244, 1), Line);
        if (Catalog.Length != 0)
        {
            Item item = Catalog[Mathf.Clamp(selected, 0, Catalog.Length - 1)];
            if (item != null)
            {
                Brackets(c, new RectInt(8, 30, 108, 82), Mint);
                Icon(c, item, 62 - item.icon[0].Length * 3 / 2, 38, 3);
                c.Text(text, 62, 72, item.label, WhiteText, 1, true);
                string price = item.price.ToString();
                int pw = text == null ? 20 : text.Measure(price, 2);
                int half = (pw + 18) / 2;
                BoltIcon(c, 62 - half, 89, 2);
                c.Text(text, 80 - half, 88, price, Coins.Bolts < item.price ? RedText : MintText, 2, false);
                c.Text(title, 124, 34, item.label, GreenText, 1, false);
                c.Text(text, 124, 54, item.line1, DimText, 1, false);
                c.Text(text, 124, 66, item.line2, DimText, 1, false);
                c.Text(text, 124, 78, "DROPS IN THE TRAY", DimText, 1, false);
                int back = PaysBack(item);
                if (0 < back)
                {
                    c.Text(text, 124, 90, "RECYCLE IT: " + back.ToString() + " BACK", DimText, 1, false);
                }
                for (int i = 0; i < Catalog.Length; i++)
                {
                    c.Fill(new RectInt(124 + i * 10, 102, 6, 6), i == selected ? Mint : Line);
                }
            }
        }
        c.Fill(new RectInt(6, 118, 244, 1), Line);
        string s;
        Color32[] cols;
        if (toast == null)
        {
            cols = GreenText;
            s = "POKE NEXT OR BUY BELOW";
        }
        else
        {
            s = toast;
            cols = toastColours;
            if (cols == null) cols = GreenText;
        }
        c.Text(text, 128, 124, s, cols, 1, true);
        c.Show();
    }

    private static int PaysBack(Item item)
    {
        GillaSpawnables sp = GillaSpawnables.Get();
        GameObject prefab = null;
        if (sp != null)
        {
            if (item.name == "Dynamite") prefab = sp.dynamite;
            else if (item.name == "BearTrap") prefab = sp.bearTrap;
            else if (item.name == "Egg") prefab = sp.egg;
        }
        GillaItem gi = null;
        if (prefab != null) gi = prefab.GetComponent<GillaItem>();
        if (gi != null)
        {
            return SellBack(item.name) /* ? */;
        }
        return 0;
    }

    private void DrawNext(GillaMapVote.Canvas c)
    {
        float p = nextPressedUntil;
        Color32 col;
        if (0f < p)
        {
            c.Clear(new Color32(10, 40, 28, 255), new Color32(14, 48, 34, 255));
            col = Mint;
        }
        else
        {
            c.Clear(Black, Scan);
            col = White;
        }
        c.Outline(new RectInt(2, 2, 92, 28), col);
        c.Outline(new RectInt(3, 3, 90, 26), col);
        c.Text(title, 48, 9, "NEXT", 0f < p ? MintText : WhiteText, 2, true);
        c.Show();
    }

    private void DrawKey(GillaMapVote.Canvas c)
    {
        float p = pressedUntil;
        bool affordable;
        if (Catalog.Length == 0)
        {
            affordable = false;
        }
        else
        {
            Item item = Catalog[Mathf.Clamp(selected, 0, Catalog.Length - 1)];
            if (item == null) affordable = false;
            else affordable = item.price <= Coins.Bolts;
        }
        Color32 a;
        Color32 b;
        if (p <= 0f)
        {
            a = Black;
            b = Scan;
        }
        else
        {
            b = new Color32(14, 48, 34, 255);
            a = new Color32(10, 40, 28, 255);
        }
        c.Clear(a, b);
        Color32 col;
        if (affordable) col = p <= 0f ? White : Mint;
        else col = Line;
        c.Outline(new RectInt(2, 2, 92, 28), col);
        c.Outline(new RectInt(3, 3, 90, 26), col);
        Color32[] cols;
        if (affordable) cols = p <= 0f ? WhiteText : MintText;
        else cols = DimText;
        c.Text(title, 48, 9, "BUY", cols, 2, true);
        c.Show();
    }

    private static void Brackets(GillaMapVote.Canvas c, RectInt r, Color32 col)
    {
        int x0 = r.xMin;
        int y0 = r.yMin;
        int x1 = Mathf.Max(x0, x0 + r.width);
        int y1 = Mathf.Max(y0, y0 + r.height);
        ValueTuple<int, int, int, int>[] corners = new ValueTuple<int, int, int, int>[4];
        corners[0] = new ValueTuple<int, int, int, int>(x0, y0, 1, 1);
        corners[1] = new ValueTuple<int, int, int, int>(x1 - 1, y0, -1, 1);
        corners[2] = new ValueTuple<int, int, int, int>(x0, y1 - 1, 1, -1);
        corners[3] = new ValueTuple<int, int, int, int>(x1 - 1, y1 - 1, -1, -1);
        for (int i = 0; i < corners.Length; i++)
        {
            int x = corners[i].Item1;
            int y = corners[i].Item2;
            int dx = corners[i].Item3;
            int dy = corners[i].Item4;
            for (int k = 0; k < 8; k++)
            {
                c.Set(x + k * dx, y, col);
                c.Set(x, y + k * dy, col);
            }
        }
    }

    private static void Icon(GillaMapVote.Canvas c, Item item, int x, int y, int scale)
    {
        string[] icon = item.icon;
        for (int row = 0; row < icon.Length; row++)
        {
            int px = x;
            for (int i = 0; i < icon[row].Length; i++)
            {
                char ch = icon[row][i];
                Color32? col;
                switch (ch)
                {
                    case 'D': col = item.dark; break;
                    case 'F': col = new Color32(70, 64, 60, 255); break;
                    case 'S': col = new Color32(255, 210, 70, 255); break;
                    case 'M': col = item.mid; break;
                    case 'L': col = item.light; break;
                    default: col = null; break;
                }
                if (col.HasValue)
                {
                    c.Fill(new RectInt(px, y + row * scale, scale, scale), col.Value);
                }
                px += scale;
            }
        }
    }

    private static void BoltIcon(GillaMapVote.Canvas c, int x, int y, int scale)
    {
        string[] rows = new string[]
        {
            "..###..",
            ".#####.",
            "##LLL##",
            "#LL.LL#",
            "##LLL##",
            ".#####.",
            "..###..",
        };
        Color32 edge = new Color32(150, 160, 170, 255);
        for (int row = 0; row < 7; row++)
        {
            int px = x;
            for (int i = 0; i < 7; i++)
            {
                char ch = rows[row][i];
                if (ch != '.')
                {
                    Color32 col = ch == '#' ? edge : new Color32(215, 225, 232, 255);
                    c.Fill(new RectInt(px, y + row * scale, scale, scale), col);
                }
                px += scale;
            }
        }
    }
}
