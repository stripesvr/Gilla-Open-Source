// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GillaMenuStand : MonoBehaviour
{
    [Tooltip("The add-on: GillaPurchases' SKU (none: a stand waiting for a menu, SOON).")]
    public string sku;
    [Tooltip("What it gives your account: ModMenuAccess' item.")]
    public string item;
    [Tooltip("Its name, at the top of its screen.")]
    public string menuName;
    public PressKey buyKey;
    [Tooltip("Its screen, drawn here (W x H pixels).")]
    public Renderer screen;
    [Tooltip("The screen's letters (ScreenFonts: Assets/Gilla/Fonts/PixelOperatorHB8.txt, GillaLCD.txt).")]
    public TextAsset titleFont;
    [Tooltip("The screen's letters (ScreenFonts: Assets/Gilla/Fonts/PixelOperatorHB8.txt, GillaLCD.txt).")]
    public TextAsset textFont;
    public AudioClip buySound;
    public AudioClip denySound;
    public AudioClip equipSound;
    [Tooltip("How long (s) after paying it says CHECKING, while the server asks Meta.")]
    public float checkingFor = 20f;
    public const int W = 256;
    public const int H = 128;
    public static readonly RectInt Button = new RectInt(52, 66, 152, 40);
    private const float AskEvery = 60;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Color32[] Ink = new Color32[]
    {
        new Color32(6, 6, 6, 255),
        new Color32(6, 6, 6, 255)
    };
    private float nextAsk;
    private float checkingUntil;
    private float downUntil;
    private string shown;
    private string drawn;
    private Texture2D tex;
    private Color32[] px;
    private ScreenFont title;
    private ScreenFont text;
    private MaterialPropertyBlock block;

    public string Shown { get { return shown; } }

    public bool Live { get; private set; }

    private void OnEnable()
    {

        if (buyKey != null && Application.isPlaying)
        {
            buyKey.Pressed += OnPress;
        }
        Redraw();
    }

    public void Redraw()
    {
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        drawn = null;
        Update();
    }

    private void OnDisable()
    {
        if (buyKey != null && Application.isPlaying)
        {
            buyKey.Pressed -= OnPress;
        }
    }

    private void OnDestroy()
    {
        if (tex != null)
        {
            if (Application.isPlaying)
                Destroy(tex);
            else
                DestroyImmediate(tex);
        }
    }

    private void Update()
    {
        bool live;
        if (string.IsNullOrEmpty(sku))
        {
            shown = "CLOSED";
            live = false;
        }
        else
        {
            string price = GillaPurchases.PriceOf(sku);
            if (price == null)
            {
                if (Application.isPlaying && nextAsk <= Time.unscaledTime)
                {
                    nextAsk = Time.unscaledTime + AskEvery;
                    GillaPurchases.FetchPrices();
                }
            }
            bool owns = ModMenuAccess.Owns(item);
            bool settled;
            bool checking;
            if (owns)
            {
                settled = true;
                checking = false;
            }
            else
            {
                settled = Time.unscaledTime < checkingUntil;
                checking = settled;
            }
            bool busy = GillaPurchases.Busy;
            if (owns)
            {
                shown = ModMenuAccess.Worn(item) ? "UNEQUIP" : "EQUIP";
            }
            else if (checking)
            {
                shown = "CHECKING";
            }
            else if (busy)
            {
                shown = "PAYING";
            }
            else
            {
                shown = price != null ? price : "CLOSED";
            }
            live = owns;
            if (!settled)
            {
                live = price != null && !busy;
            }
        }
        Live = live;
        if (buyKey != null && Application.isPlaying)
        {
            if (buyKey.gameObject.activeSelf != Live)
            {
                buyKey.gameObject.SetActive(Live);
            }
        }
        float now = Time.unscaledTime;
        float until = downUntil;
        string key = shown + (Live ? "+" : "-") + (until <= now ? "" : "v");
        if (key != drawn)
        {
            drawn = key;
            Draw(shown, Live, now < until);
        }
    }

    public void Press()
    {
        OnPress(buyKey);
    }

    private void OnPress(PressKey key)
    {
        int with = 0;
        if (!Live) return;
        if (GillaPurchases.Busy) return;
        downUntil = Time.unscaledTime + 0.2f;
        Component from = key != null ? (Component)key : this;
        with = GillaToast.NearerHand(from.transform.position);
        if (!ModMenuAccess.Owns(item))
        {
            GillaPurchases.Buy(sku, result =>
            {
                AudioClip clip;
                if (result == GillaPurchases.Result.Failed)
                {
                    GillaToast.Show(with, "PURCHASE FAILED", "IT DID NOT GO THROUGH - TRY AGAIN", true);
                    clip = denySound;
                }
                else if (result == GillaPurchases.Result.Bought)
                {
                    checkingUntil = Time.unscaledTime + checkingFor;
                    ModMenuAccess.SetEquipped(item, true);
                    GillaToast.Show(with, "PURCHASED", menuName + " - HOLD Y", false);
                    clip = buySound;
                }
                else
                {
                    clip = null;
                }
                if (clip == null) return;
                AudioClip[] clips2 = new AudioClip[] { clip };
                Vector3 at2 = transform.position + Vector3.up;
                if (GillaBackroomsSwitch.OnYourSide(at2) && clips2.Length != 0)
                {
                    AudioClip pick2 = clips2[UnityEngine.Random.Range(0, clips2.Length)];
                    if (pick2 == null) return;
                    float t2 = Time.time;
                    /* ? */ // inlined Sfx throttling (Sfx.Room / Sfx.lately) is private to Sfx
                    GameObject go2 = new GameObject("Sfx_" + pick2.name);
                    go2.transform.position = at2;
                    AudioSource src2 = go2.AddComponent<AudioSource>();
                    src2.clip = pick2;
                    src2.volume = 0.8f;
                    src2.pitch = UnityEngine.Random.Range(1f, 1f);
                    src2.spatialBlend = 1f;
                    src2.minDistance = 1f;
                    src2.maxDistance = 25f;
                    src2.rolloffMode = AudioRolloffMode.Linear;
                    src2.dopplerLevel = 0f;
                    src2.Play();
                    float life2 = pick2.length / src2.pitch + 0.1f;
                    /* ? */ // Sfx.ending.Add(t2 + life2) is private to Sfx
                    Destroy(go2, life2);
                }
            });
            return;
        }
        bool worn = ModMenuAccess.Worn(item);
        ModMenuAccess.SetWorn(item, !worn);
        string body = menuName;
        string head = worn ? "UNEQUIPPED" : "EQUIPPED";
        if (!worn)
        {
            body = body + " - HOLD Y";
        }
        GillaToast.Show(with, head, body, false);
        if (equipSound == null) return;
        AudioClip[] clips = new AudioClip[] { equipSound };
        Vector3 at = transform.position + Vector3.up;
        if (GillaBackroomsSwitch.OnYourSide(at) && clips.Length != 0)
        {
            AudioClip pick = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (pick == null) return;
            float t = Time.time;
            /* ? */ // inlined Sfx throttling (Sfx.Room / Sfx.lately) is private to Sfx
            GameObject go = new GameObject("Sfx_" + pick.name);
            go.transform.position = at;
            AudioSource src = go.AddComponent<AudioSource>();
            src.clip = pick;
            src.volume = 0.8f;
            src.pitch = UnityEngine.Random.Range(1f, 1f);
            src.spatialBlend = 1f;
            src.minDistance = 1f;
            src.maxDistance = 25f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.dopplerLevel = 0f;
            src.Play();
            float life = pick.length / src.pitch + 0.1f;
            /* ? */ // Sfx.ending.Add(t + life) is private to Sfx
            Destroy(go, life);
        }
    }

    private void Draw(string label, bool live, bool down)
    {
        if (screen == null) return;
        if (tex == null)
        {
            Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            t.hideFlags = HideFlags.HideAndDontSave;
            t.name = "GillaMenuStand";
            tex = t;
            px = new Color32[W * H];
        }
        GillaBoardLook.Clear(px, W, H);
        GillaBoardLook.Frame(px, W, H, 3);
        string heading = string.IsNullOrEmpty(menuName) ? "CLOSED" : menuName;
        ScreenFont headFont;
        int headScale;
        int headY = 10;
        if (title == null)
        {
            headFont = null;
            headScale = 2;
        }
        else
        {
            int width = title.Measure(heading, 2);
            headFont = title;
            headScale = width < 229 ? 2 : 1;
            headY = width > 228 ? 14 : 10;
        }
        Line(headFont, 128, headY, heading, headScale, GillaBoardLook.White);
        GillaBoardLook.Rule(px, W, H, 10, 245, 34);
        string sub = string.IsNullOrEmpty(sku) ? "A NEW MENU IS ON ITS WAY" : "ONLY WORKS IN THE REGULAR GAME";
        Line(text, 128, 43, sub, 1, GillaBoardLook.Dim);
        int bx = Button.xMin;
        int by = Button.yMin;
        int bw = Button.width;
        int bh = Button.height;
        Color32 colour;
        if (live)
        {
            colour = down ? GillaBoardLook.Dots : GillaBoardLook.Solid;
        }
        else
        {
            colour = GillaBoardLook.Edge;
        }
        PixelFont.Fill(px, W, H, bx + 3, by, bw - 6, bh, colour);
        PixelFont.Fill(px, W, H, bx, by + 3, bw, bh - 6, colour);
        PixelFont.Fill(px, W, H, bx + 1, by + 1, bw - 2, bh - 2, colour);
        int scale;
        if (text == null || text.Measure(label, 2) <= bw - 10)
            scale = 2;
        else
            scale = 1;
        int cap = text != null ? text.cap : 7;
        Color32[] ink = live ? Ink : GillaBoardLook.Soft;
        Line(text, bx + bw / 2, by + (bh - cap * scale) / 2, label, scale, ink);
        tex.SetPixels32(px);
        tex.Apply(false);
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }

    private void Line(ScreenFont font, int x, int y, string line, int scale, Color32[] colours)
    {
        ScreenFont.Style style = default(ScreenFont.Style);
        if (string.IsNullOrEmpty(line)) return;
        if (font == null)
        {
            int width = PixelFont.Measure(line, scale);
            PixelFont.Draw(px, W, H, x - width / 2, y, line, colours[0], scale);
        }
        else
        {
            style = default(ScreenFont.Style);
            style.scale = scale;
            style.colours = colours;
            font.DrawCentred(px, W, H, x, y, line, style);
        }
    }
}
