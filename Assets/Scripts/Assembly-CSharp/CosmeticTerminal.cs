// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

[ExecuteAlways]
public partial class CosmeticTerminal : MonoBehaviour
{
    private class Tile
    {
        public GameObject root;
        public GameObject model;
        public byte id;
        public int title;
        public int bundle = -1;
        public float pop;
        public Vector2 at;
        public Vector2 to;
        public RectInt place;
    }

    [Header("Parts (GillaSetup.Shop builds them)")]
    [Tooltip("The screen: a quad, its front -Z, scaled to the screen's size.")]
    public Renderer screen;
    [Tooltip("In the screen's place and turn, unscaled: the 3D tiles go under it.")]
    public Transform tileRoot;
    public ScrollDrum drum;
    public TryOnTube tube;
    [Tooltip("Coins thrown out of the screen when something's bought (they land and shrink away).")]
    public ParticleSystem buyCoins;
    [Tooltip("The points that poke it (the first-person ape's index fingertips), left then right.")]
    public Transform[] fingertips;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset textFont;
    [Tooltip("The tiles' wire boxes, and the brighter one for the one picked (unlit).")]
    public Material wire;
    [Tooltip("The tiles' wire boxes, and the brighter one for the one picked (unlit).")]
    public Material wirePicked;
    [Tooltip("It boots when your head comes this close (m), and goes dark past WakeRange + 1.5.")]
    public float wakeRange = 2.8f;
    [Header("Sounds (Assets/Audio/Shop)")]
    public AudioClip clickSound;
    public AudioClip hoverSound;
    public AudioClip bootSound;
    public AudioClip offSound;
    public AudioClip buySound;
    public AudioClip errorSound;
    public AudioClip coinSound;
    public AudioClip equipSound;
    public AudioClip sparkleSound;
    public AudioClip toggleSound;
    public AudioClip fillSound;
    private const int W = 512;
    private const int H = 256;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private const CosmeticSlot BundleSlot = (CosmeticSlot)255;
    private const CosmeticSlot TitleSlot = (CosmeticSlot)254;
    private static readonly (string label, CosmeticSlot slot)[] Categories = new[] { ("BUNDLES", (CosmeticSlot)255), ("SKINS", (CosmeticSlot)6), ("TOYS", (CosmeticSlot)7), ("HATS", (CosmeticSlot)0), ("FACE", (CosmeticSlot)1), ("EYES", (CosmeticSlot)2), ("CLOTHING", (CosmeticSlot)3), ("BACK", (CosmeticSlot)5), ("NAME TAG", (CosmeticSlot)254) };
    private static readonly string[] Filters = new[] { "SHOW: ALL", "SHOW: OWNED", "SHOW: SHOP" };
    private static readonly RectInt ShowRect = new RectInt(150, 4, 120, 18);
    private const int CatX = 12;
    private const int CatY = 32;
    private const int CatW = 108;
    private const int CatH = 32;
    private const int CatStep = 38;
    private const int CatRows = 5;
    private const int GridX = 132;
    private const int GridY = 28;
    private const int TileW = 58;
    private const int TileH = 104;
    private const int TileGap = 4;
    private const int Cols = 4;
    private const int Rows = 2;
    private static readonly RectInt UpRect = new RectInt(382, 28, 16, 46);
    private static readonly RectInt DownRect = new RectInt(382, 196, 16, 46);
    private static readonly RectInt CartRect = new RectInt(404, 6, 100, 104);
    private static readonly RectInt WalletRect = new RectInt(404, 114, 100, 32);
    private static readonly RectInt ButtonRect = new RectInt(404, 158, 100, 88);
    private static readonly Color32 Black = new Color32(6, 6, 6, 255);
    private static readonly Color32 Scan = new Color32(13, 13, 13, 255);
    private static readonly Color32 Line = GillaBoardLook.Solid;
    private static readonly Color32 Dim = new Color32(96, 96, 96, 255);
    private static readonly Color32 Dark = new Color32(40, 40, 40, 255);
    private static readonly Color32 Bright = new Color32(255, 255, 255, 255);
    private static readonly Color32 White = new Color32(255, 255, 255, 255);
    private static readonly Color32 Warn = new Color32(150, 150, 150, 255);
    private static readonly Color32 Grey = new Color32(70, 70, 70, 255);
    private static readonly Color32 Coin = new Color32(232, 232, 232, 255);
    private static readonly Color32[] Plain = GillaBoardLook.Soft;
    private static readonly Color32[] Picked = GillaBoardLook.White;
    private static readonly Color32[] DimText = GillaBoardLook.Dim;
    private static readonly Color32[] Warning = GillaBoardLook.Dim;
    private static readonly Color32[] DarkText = new[] { new Color32(6, 6, 6, 255), new Color32(6, 6, 6, 255) };
    private const float GlideRate = 12f;
    private Texture2D tex;
    private Color32[] px;
    private MaterialPropertyBlock block;
    private ScreenFont title;
    private ScreenFont text;
    private readonly Tile[] tiles = new CosmeticTerminal.Tile[8];
    private readonly List<Tile> leaving = new List<CosmeticTerminal.Tile>();
    private Color32[] underneath;
    private int glideDir;
    private float catAt;
    private float firstAt;
    private readonly List<byte> shown = new List<byte>();
    private readonly List<int> shownBundles = new List<int>();
    private readonly List<int> shownTitles = new List<int>();
    private readonly byte[] cart = new byte[8];
    private readonly byte[] tryOn = new byte[8];
    private int category;
    private int filter;
    private int scroll;
    private float barAt;
    private int catFirst;
    private byte picked;
    private int pickedBundle = -1;
    private int pickedTitle;
    private int drumAt = -1;
    private float awake;
    private bool wanted;
    private float holdFill;
    private float banner;
    private float reboot = -1f;
    private float pressFlash;
    private string bannerText = "";
    private bool bannerBad;
    private string hovered = "";
    private string pressedThing = "";
    private readonly bool[] down = new bool[2];
    private readonly string[] downOn = new[] { "", "" };
    private bool mouseDown;
    private bool dirty = true;
    private int lastBalance = -1;
    private const float PopSeconds = 1.4f;
    private const float PopStagger = 0.1f;

    private static bool Refill_b__135_0(byte id)
    {
        return CosmeticCatalog.Of(id).slot == CosmeticSlot.Skin;
    }

    private static bool IsBundles(int c)
    {
        return Categories[c].Item2 == BundleSlot;
    }

    private static bool IsTitles(int c)
    {
        return Categories[c].Item2 == TitleSlot;
    }

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void OnEnable()
    {

        if (Application.isPlaying) return;
        if (screen == null) return;
        Texture2D t = new Texture2D(512, 256, TextureFormat.RGBA32, true);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.name = "GillaLab";
        t.hideFlags = HideFlags.DontSave;
        tex = t;
        px = new Color32[131072];
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        awake = 0f;
        Draw();
    }

    private void Start()
    {
        if (!Application.isPlaying) return;
        Texture2D t = new Texture2D(512, 256, TextureFormat.RGBA32, true);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.name = "GillaLab";
        tex = t;
        px = new Color32[131072];
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = MakeTile(i);
        }
        Coins.Changed += OnCoins;
        Refill();
        SyncDrum();
        catAt = category;
        firstAt = catFirst;
        Draw();
    }

    private void OnDestroy()
    {
        Coins.Changed -= OnCoins;
    }

    private void OnCoins()
    {
        dirty = true;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        float dt = Time.deltaTime;
        Transform head = Camera.main != null ? Camera.main.transform : null;
        float dist;
        if (head != null)
            dist = Vector3.Distance(head.position, screen.transform.position);
        else
            dist = 99f;

        bool want;
        if (dist < wakeRange) want = true;
        else want = wanted && dist < wakeRange + 1.5f;

        if (wanted != want)
        {
            wanted = want;
            Play(want ? bootSound : offSound, 0.7f);
            if (want)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i].pop = i * -0.1f;
                }
            }
        }
        float seconds = want ? 0.8f : 0.3f;
        float target = want ? 1f : 0f;

        float before = awake;
        awake = Mathf.MoveTowards(awake, target, dt / seconds);
        if (awake != before) dirty = true;

        if (awake <= 0f && before <= 0f)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                ShowTile(tiles[i], TileRect(i), 0f, dt);
            }
            for (int i = leaving.Count - 1; i >= 0; i--)
            {
                Tile t = leaving[i];
                ShowTile(t, t.place, 0f, dt);
                UnityEngine.Object.Destroy(t.root);
                leaving.RemoveAt(i);
                dirty = true;
            }
            return;
        }

        if (drum != null && drum.Step != drumAt)
        {
            Rolled(drum.Step);
        }
        if (awake >= 1f) Touch(dt);
        if (banner > 0f)
        {
            banner -= dt;
            if (banner <= 0f) dirty = true;
        }
        if (reboot >= 0f)
        {
            dirty = true;
            reboot += dt / 0.7f;
            if (reboot >= 1f) reboot = -1f;
        }
        if (pressFlash > 0f)
        {
            dirty = true;
            pressFlash -= dt;
        }
        if (Coins.Balance != lastBalance)
        {
            lastBalance = Coins.Balance;
            dirty = true;
        }
        GlideTabs(dt);
        ShowTiles(awake, dt);
        if (!dirty) return;
        Draw();
    }

    private void Touch(float dt)
    {
        string hover = "";
        for (int i = 0; i < 2; i++)
        {
            if (fingertips == null || i >= fingertips.Length) continue;
            if (fingertips[i] == null) continue;
            Vector3 local = tileRoot.InverseTransformPoint(fingertips[i].position);
            string a = Hit(ToPixel(local), local);
            float z = local.z;
            bool touching = z < 0.03f;
            if (touching && z > -0.08f && a.Length > 0) hover = a;
            bool press = touching && z > -0.015f && a.Length > 0;
            if (press)
            {
                if (!down[i] || (a != downOn[i] && a != "button"))
                {
                    Press(a, i);
                }
                if (a == "button")
                {
                    Hold(dt, i);
                }
            }
            down[i] = press;
            downOn[i] = press ? a : "";
        }

        if (!XRSettings.isDeviceActive && Mouse.current != null && Camera.main != null)
        {
            Camera cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane plane = new Plane(-tileRoot.forward, tileRoot.position);
            bool pressed = Mouse.current.leftButton.isPressed;
            float enter;
            if (plane.Raycast(ray, out enter) && enter < 6f)
            {
                Vector3 local = tileRoot.InverseTransformPoint(ray.GetPoint(enter));
                string h = Hit(ToPixel(local), local);
                if (h.Length > 0) hover = h;
                if (pressed)
                {
                    if (!mouseDown && h.Length > 0)
                    {
                        Press(h, -1);
                    }
                    if (h == "button")
                    {
                        Hold(dt, -1);
                    }
                }
            }
            mouseDown = pressed;
        }

        if (!down[0] && !down[1] && !mouseDown && holdFill > 0f)
        {
            dirty = true;
            holdFill = Mathf.MoveTowards(holdFill, 0f, dt * 3f);
        }

        if (hover != hovered)
        {
            if (hover.StartsWith("cat") || hover.StartsWith("tile"))
            {
                Play(hoverSound, 0.25f);
            }
            hovered = hover;
            dirty = true;
        }
    }

    private string Hit(Vector2Int at, Vector3 p)
    {
        for (int k = 0; k < 5; k++)
        {
            int index = catFirst + k;
            if (index < Categories.Length && Inside(new RectInt(12, 32 + 38 * k, 108, 32), at))
            {
                return "cat" + index.ToString();
            }
        }
        for (int i = 0; i < tiles.Length; i++)
        {
            Tile t = tiles[i];
            if ((t.id != 0 || t.title != 0) && Inside(TileRect(i), at))
            {
                return "tile" + i.ToString();
            }
        }
        if (Inside(UpRect, at)) return "up";
        if (Inside(DownRect, at)) return "down";
        if (Inside(ButtonRect, at)) return "button";
        if (Inside(CartRect, at)) return "cart";
        return "";
    }

    private void Press(string thing, int finger)
    {
        pressedThing = thing;
        dirty = true;
        pressFlash = 0.15f;
        if (finger >= 0)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(finger != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, 0.35f, 0.04f);
        }
        if (thing == "button") return;
        Play(clickSound, 0.8f);
        if (thing == "show")
        {
            scroll = 0;
            filter = (filter + 1) % Filters.Length;
            Refill();
            SyncDrum();
            Play(toggleSound, 0.5f);
            return;
        }
        if (thing.StartsWith("cat"))
        {
            SelectCategory(int.Parse(thing.Substring(3)), false, 0);
            return;
        }
        int dir;
        int to;
        if (thing == "up")
        {
            dir = -1;
            to = Mathf.Max(scroll - 1, 0);
        }
        else if (thing == "down")
        {
            int max = MaxScroll();
            dir = 1;
            to = scroll + 1 <= max ? scroll + 1 : max;
        }
        else
        {
            if (!thing.StartsWith("tile")) return;
            Tile t = tiles[int.Parse(thing.Substring(4))];
            if (t.title > 0)
            {
                PickTitle(t.title);
                return;
            }
            if (t.bundle >= 0)
            {
                PickBundle(t.bundle);
                return;
            }
            Pick(t.id);
            return;
        }
        scroll = to;
        glideDir = dir;
        Refill();
        SyncDrum();
    }

    private void SelectCategory(int c, bool fromWheel, int row = 0)
    {
        int prev = category;
        int last = Categories.Length - 1;
        int cat = c < 0 ? 0 : Mathf.Min(c, last);
        category = cat;
        int first = Mathf.Min(catFirst, cat);
        if (cat < catFirst || cat >= first + 5)
        {
            catFirst = cat < first + 5 ? first : cat - 4;
        }
        scroll = row;
        if (cat != prev)
        {
            ClearPick();
        }
        Refill();
        if (!fromWheel)
        {
            SyncDrum();
        }
        dirty = true;
    }

    private void ClearPick()
    {
        picked = 0;
        pickedBundle = -1;
        pickedTitle = 0;
        Array.Clear(cart, 0, cart.Length);
    }

    private List<ValueTuple<int, int>> WheelPlaces()
    {
        List<ValueTuple<int, int>> list = new List<ValueTuple<int, int>>();
        for (int c = 0; c < Categories.Length; c++)
        {
            int n = CountIn(c);
            int rows = (n + 3) / 4;
            if (rows < 3) rows = 2;
            for (int r = 0; r < rows - 1; r++)
            {
                list.Add(new ValueTuple<int, int>(c, r));
            }
        }
        return list;
    }

    private void SyncDrum()
    {
        if (drum == null) return;
        List<ValueTuple<int, int>> places = WheelPlaces();
        drum.max = Mathf.Max(places.Count - 1, 0);
        int at = Mathf.Max(places.IndexOf(new ValueTuple<int, int>(category, scroll)), 0);
        drumAt = at;
        drum.Set(at);
    }

    private void Rolled(int at)
    {
        List<ValueTuple<int, int>> places = WheelPlaces();
        int prev = drumAt;
        drumAt = at;
        glideDir = prev < at ? 1 : -1;
        if (at >= 0 && at < places.Count)
        {
            ValueTuple<int, int> place = places[at];
            if (category == place.Item1)
            {
                if (scroll != place.Item2)
                {
                    scroll = place.Item2;
                    Refill();
                }
            }
            else
            {
                SelectCategory(place.Item1, true, place.Item2);
            }
        }
        glideDir = 0;
    }

    private void GlideTabs(float dt)
    {
        float k = 1f - Mathf.Exp(dt * -12f);
        float cat = category;
        if (catAt != cat)
        {
            catAt = Mathf.Abs(catAt - cat) < 0.01f ? cat : Mathf.Lerp(catAt, cat, k);
            dirty = true;
        }
        float first = catFirst;
        if (firstAt != first)
        {
            firstAt = Mathf.Abs(firstAt - first) < 0.01f ? first : Mathf.Lerp(firstAt, first, k);
            dirty = true;
        }
        float bar;
        if (drum != null && drum.max > 0f)
        {
            bar = Mathf.Clamp01((float)drum.Step / drum.max); /* ? */
        }
        else
        {
            bar = 0f;
            if (Categories.Length > 1)
            {
                bar = catAt / (Categories.Length - 1);
            }
        }
        if (Mathf.Abs(bar - barAt) > 0.001f)
        {
            barAt = bar;
            dirty = true;
        }
    }

    private int CountIn(int c)
    {
        CosmeticSlot slot = Categories[c].Item2;
        if (slot == BundleSlot)
        {
            if (CosmeticCatalog.BundleCount > 0)
            {
                int n = 0;
                for (int i = 0; i < CosmeticCatalog.BundleCount; i++)
                {
                    var b = CosmeticCatalog.BundleAt(i);
                    if (CosmeticCatalog.IdsOf(b).Count > 0)
                    {
                        if (Shows(Coins.OwnsAll(b))) n++;
                    }
                }
                return n;
            }
        }
        else if (slot == TitleSlot)
        {
            int n = 0;
            for (int t = 1; t <= NameTitles.Count; t++)
            {
                if (Shows(NameTitles.MayWear(t))) n++;
            }
            return n;
        }
        else
        {
            if (CosmeticCatalog.Count > 0)
            {
                int n = 0;
                for (int id = 1; id <= CosmeticCatalog.Count; id++)
                {
                    var e = CosmeticCatalog.Of(id);
                    if (e.slot == slot && string.IsNullOrEmpty(e.wornWith))
                    {
                        bool own = Coins.Owns((byte)id);
                        if (filter != (own ? 2 : 1))
                        {
                            if (!e.retired || Coins.Owns((byte)id))
                            {
                                n++;
                            }
                        }
                    }
                }
                return n;
            }
        }
        return 0;
    }

    private bool Shows(bool own)
    {
        return (filter != 1 || own) && (filter != 2 || !own);
    }

    private void Unequip()
    {
        CosmeticSlot slot = Categories[category].Item2;
        bool took = false;
        if (slot == BundleSlot)
        {
            var b = CosmeticCatalog.BundleAt(pickedBundle);
            List<byte> wearing = b == null ? new List<byte>() : CosmeticCatalog.IdsOf(b).FindAll(Cosmetics.IsWearing);
            if (wearing.Count != 0)
            {
                foreach (byte id in wearing)
                {
                    Cosmetics.TakeOff(CosmeticCatalog.Of(id).slot);
                }
                took = true;
            }
        }
        else if (slot == TitleSlot)
        {
            if (NameTitles.Worn != 0)
            {
                NameTitles.Wear(0);
                Play(equipSound, 0.6f);
                bannerText = "UNEQUIPPED";
                bannerBad = false;
                dirty = true;
                banner = 1.6f;
                return;
            }
        }
        else
        {
            if (Cosmetics.In(slot) != 0)
            {
                Cosmetics.TakeOff(slot);
                took = true;
            }
        }
        if (took)
        {
            Play(equipSound, 0.6f);
            bannerText = "UNEQUIPPED";
            bannerBad = false;
            banner = 1.6f;
            dirty = true;
            UpdateTryOn();
            dirty = true;
            return;
        }
        bannerText = "NOTHING ON";
        bannerBad = true;
        banner = 1.6f;
        dirty = true;
        Play(errorSound, 0.5f);
    }

    private void Pick(byte id)
    {
        if (CosmeticCatalog.Of(id) != null)
        {
            byte prev = picked;
            ClearPick();
            if (prev != id)
            {
                picked = id;
                GillaQuests.Report(GillaQuests.Stat.TryOn, 1);
            }
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].id == id) tiles[i].pop = 0.6f;
            }
            UpdateTryOn();
            dirty = true;
        }
    }

    private void PickBundle(int i)
    {
        if (CosmeticCatalog.BundleAt(i) != null)
        {
            int prev = pickedBundle;
            ClearPick();
            if (prev != i)
            {
                pickedBundle = i;
                GillaQuests.Report(GillaQuests.Stat.TryOn, 1);
            }
            for (int k = 0; k < tiles.Length; k++)
            {
                if (tiles[k].bundle == i) tiles[k].pop = 0.6f;
            }
            UpdateTryOn();
            dirty = true;
        }
    }

    private void PickTitle(int t)
    {
        int prev = pickedTitle;
        ClearPick();
        if (prev != t)
        {
            pickedTitle = t;
        }
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].title == t) tiles[i].pop = 0.6f;
        }
        dirty = true;
    }

    private void Hold(float dt, int finger)
    {
        bool ok;
        string action = ButtonAction(out ok);
        float before = holdFill;
        if (!ok)
        {
            if (before == 0f && action == "NEED MORE")
            {
                bannerText = "NOT ENOUGH COINS";
                bannerBad = true;
                dirty = true;
                banner = 1.6f;
                Play(errorSound, 0.7f);
                holdFill = 0.001f;
            }
            return;
        }
        holdFill = Mathf.Min(dt / 0.9f + before, 1f);
        if (before == 0f)
        {
            Play(fillSound, 0.5f);
        }
        if (finger >= 0)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(finger != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, holdFill * 0.3f + 0.1f, dt * 1.5f);
        }
        dirty = true;
        if (before < 1f && holdFill >= 1f)
        {
            Act(action);
        }
    }

    private string ButtonAction(out bool ok)
    {
        ok = true;
        string result;
        if (pickedTitle < 1)
        {
            var b = CosmeticCatalog.BundleAt(pickedBundle);
            if (b != null && Coins.OwnsAll(b))
            {
                bool all = CosmeticCatalog.IdsOf(b).TrueForAll(Cosmetics.IsWearing);
                return all ? "TAKE OFF" : "EQUIP";
            }
            if (CosmeticCatalog.Of(picked) != null && Coins.Owns(picked))
            {
                return Cosmetics.IsWearing(picked) ? "TAKE OFF" : "EQUIP";
            }
            if (b == null && picked == 0 && WearingHere())
            {
                return "TAKE OFF";
            }
            result = "PICK ONE";
        }
        else
        {
            if (NameTitles.MayWear(pickedTitle))
            {
                return NameTitles.Worn != pickedTitle ? "EQUIP" : "TAKE OFF";
            }
            result = "LOCKED";
        }
        ok = false;
        return result;
    }

    private bool WearingHere()
    {
        CosmeticSlot slot = Categories[category].Item2;
        if (slot == BundleSlot) return false;
        if (slot == TitleSlot) return NameTitles.Worn != 0;
        return Cosmetics.In(slot) != 0;
    }
}
