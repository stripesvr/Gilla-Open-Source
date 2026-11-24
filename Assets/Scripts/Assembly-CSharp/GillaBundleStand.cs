// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GillaBundleStand : MonoBehaviour
{
    [Tooltip("The bundle, by its catalog name. Empty: a stand waiting for one.")]
    public string bundleName;
    public Renderer screen;
    [Tooltip("The button, over the place on the screen where it's drawn.")]
    public PressKey buyKey;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset textFont;
    public AudioClip buySound;
    public AudioClip denySound;
    public AudioClip wearSound;
    [Tooltip("How near (m) you come for it to open.")]
    public float reach = 2.8f;
    [Tooltip("How long (s) after paying it says CHECKING, while the server asks Meta.")]
    public float checkingFor = 20f;
    public const int W = 256;
    public const int H = 192;
    public static readonly RectInt Button = new RectInt(52, 138, 152, 40);
    private const float AskEvery = 60;
    private const float OpenFor = 0.55f;
    private const float ShutFor = 0.14f;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Color32[] Ink = new Color32[] { new Color32(6, 6, 6, 255), new Color32(6, 6, 6, 255) };
    private Texture2D tex;
    private Color32[] px;
    private MaterialPropertyBlock block;
    private ScreenFont title;
    private ScreenFont text;
    private string shown;
    private float nextAsk;
    private float checkingUntil;
    private float popT = 1f;
    private bool near;
    private Vector3 fullSize;

    private CosmeticCatalog.Bundle Bundle
    {
        get
        {
            if (string.IsNullOrEmpty(bundleName))
                return null;
            return CosmeticCatalog.BundleNamed(bundleName);
        }
    }

    public string LabelShown { get; private set; }
    public string LinesShown { get; private set; }

    private void OnEnable()
    {

        if (screen == null)
            return;
        fullSize = screen.transform.localScale;
        if (buyKey != null)
            buyKey.Pressed += OnPress;
        shown = null;
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        Show();
    }

    private void OnDisable()
    {
        if (buyKey != null)
            buyKey.Pressed -= OnPress;
        if (screen != null)
        {
            if ((fullSize - Vector3.zero).sqrMagnitude >= 9.9999994e-11f)
            {
                screen.transform.localScale = fullSize;
                screen.enabled = true;
            }
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
        if (!Application.isPlaying)
            return;
        if (screen == null)
            return;
        Camera cam = Camera.main;
        bool isNear;
        if (cam != null)
            isNear = (cam.transform.position - transform.position).sqrMagnitude < reach * reach;
        else
            isNear = false;

        float t;
        bool animate;
        if (near == isNear)
        {
            t = popT;
            animate = t < 1f;
        }
        else
        {
            t = 0f;
            near = isNear;
            popT = 0f;
            animate = true;
        }
        if (animate)
        {
            float dt = Time.deltaTime;
            float s;
            if (!near)
            {
                float p = Mathf.Min(t + dt / ShutFor, 1f);
                s = 1f - p;
                popT = p;
            }
            else
            {
                float p = Mathf.Min(t + dt / OpenFor, 1f);
                popT = p;
                s = ElasticOut(p);
            }
            screen.transform.localScale = new Vector3(s * fullSize.x, s * fullSize.y, fullSize.z);
            screen.enabled = 0.01f < s;
        }
        Show();
    }

    private static float ElasticOut(float t)
    {
        float r = 0f;
        if (0f < t)
        {
            r = 1f;
            if (t < 1f)
            {
                float a = (t * 10f - 0.75f) * 3.1415927f;
                r = Mathf.Pow(2f, t * -10f) * Mathf.Sin((a + a) / 3f) + 1f;
            }
        }
        return r;
    }

    private void Show()
    {
        CosmeticCatalog.Bundle b = Bundle;
        string label;
        bool showKey;
        if (b == null)
        {
            float unused = Time.unscaledTime;
            label = "";
            LabelShown = "";
            showKey = false;
        }
        else
        {
            string price = GillaPurchases.PriceOf(b.sku);
            if (price == null)
            {
                if (Application.isPlaying && nextAsk <= Time.unscaledTime)
                {
                    nextAsk = Time.unscaledTime + AskEvery;
                    GillaPurchases.FetchPrices();
                }
            }
            bool owns = Coins.OwnsAll(b);
            bool checking;
            if (owns)
            {
                checking = false;
                label = "OWNED";
            }
            else if (Time.unscaledTime < checkingUntil)
            {
                checking = true;
                label = "CHECKING";
            }
            else if (GillaPurchases.Busy)
            {
                checking = false;
                label = "PAYING";
            }
            else
            {
                checking = false;
                label = price != null ? price : "CLOSED";
            }
            LabelShown = label;
            showKey = owns && near;
            if (!checking && !owns && near)
                showKey = price != null && !GillaPurchases.Busy;
        }

        if (buyKey != null && Application.isPlaying)
        {
            if (showKey != buyKey.gameObject.activeSelf)
                buyKey.gameObject.SetActive(showKey);
        }

        bool down;
        if (buyKey != null && buyKey.gameObject.activeInHierarchy)
            down = buyKey.IsDown;
        else
            down = false;

        string key = label + down.ToString();
        if (key == shown && tex != null)
            return;
        shown = key;
        Draw(b, label, down);
    }

    private void OnPress(PressKey key)
    {
        CosmeticCatalog.Bundle b = Bundle;
        if (b == null)
            return;
        if (Coins.OwnsAll(b))
        {
            foreach (byte id in CosmeticCatalog.IdsOf(b))
                Cosmetics.Wear(id);
            Play(wearSound);
            return;
        }
        if (GillaPurchases.Busy)
            return;
        if (GillaPurchases.PriceOf(b.sku) == null)
            return;
        int with = GillaToast.NearerHand(key.transform.position);
        GillaPurchases.Buy(b.sku, result =>
        {
            if (result == GillaPurchases.Result.Failed)
            {
                GillaToast.Show(with, "PURCHASE FAILED", "IT DID NOT GO THROUGH - TRY AGAIN", true);
                Play(denySound);
            }
            else if (result == GillaPurchases.Result.Bought)
            {
                checkingUntil = Time.unscaledTime + checkingFor;
                GillaToast.Show(with, "PURCHASED", b.label + " - " + b.coins.ToString("N0") + " COINS ADDED", false);
                Play(buySound);
            }
            else
            {
                Play(null);
            }
        });
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            Sfx.Play(new AudioClip[] { clip }, transform.position + Vector3.up * 0.3f, 0.8f, 1f, 1f);
    }

    private void Draw(CosmeticCatalog.Bundle b, string label, bool down)
    {
        if (tex == null)
        {
            tex = new Texture2D(W, H, (TextureFormat)4, true);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = (HideFlags)0x34;
            tex.name = "GillaBundleStand";
            px = new Color32[W * H];
        }
        GillaBoardLook.Clear(px, W, H);
        GillaBoardLook.Frame(px, W, H, 3);

        string name;
        if (b == null)
            name = "CLOSED";
        else
            name = string.IsNullOrEmpty(b.label) ? b.name : b.label;

        int ty = 11;
        int tscale;
        if (title == null)
        {
            tscale = 2;
        }
        else
        {
            int w = title.Measure(name, 2);
            tscale = w < 229 ? 2 : 1;
            ty = w > 228 ? 15 : 11;
        }
        Line(title, 128, ty, name, new ScreenFont.Style { scale = tscale, colours = GillaBoardLook.White });
        GillaBoardLook.Rule(px, W, H, 10, 245, 33);

        if (b == null)
        {
            Line(text, 128, 86, "A NEW BUNDLE IS ON ITS WAY", new ScreenFont.Style { scale = 1, colours = GillaBoardLook.Dim });
        }
        else
        {
            Line(text, 128, 40, "Includes:", new ScreenFont.Style { scale = 1, colours = GillaBoardLook.Dim });
            List<string> lines = new List<string>();
            foreach (byte id in CosmeticCatalog.IdsOf(b))
            {
                CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
                if (e != null)
                    lines.Add(e.label);
            }
            if (0 < b.coins)
                lines.Add("+" + b.coins.ToString("N0") + " COINS");
            if (!string.IsNullOrEmpty(b.tag))
                lines.Add("ITS NAME TAG");
            if (b.xpBoost)
                lines.Add("1.5X XP FOR GOOD");
            if (b.dailyCoins >= 1)
                lines.Add(b.dailyCoins.ToString() + " COINS A DAY");
            int step = lines.Count < 7 ? 12 : 11;
            int y = 54;
            foreach (string l in lines)
            {
                if (y < 126)
                {
                    Line(text, 128, y, l, new ScreenFont.Style { scale = 1, colours = GillaBoardLook.White });
                    y += step;
                }
            }
            LinesShown = string.Join(" | ", lines);

            int bx = Button.x;
            int by = Button.y;
            int bw = Button.width;
            int bh = Button.height;
            Color32 colour = down ? GillaBoardLook.Dots : GillaBoardLook.Solid;
            PixelFont.Fill(px, W, H, bx + 3, by, bw - 6, bh, colour);
            PixelFont.Fill(px, W, H, bx, by + 3, bw, bh - 6, colour);
            PixelFont.Fill(px, W, H, bx + 1, by + 1, bw - 2, bh - 2, colour);
            Line(text, bx + bw / 2, by + 13, label, new ScreenFont.Style { scale = 2, colours = Ink });
        }

        tex.SetPixels32(px);
        tex.Apply(true);
        if (screen == null)
            return;
        if (block == null)
            block = new MaterialPropertyBlock();
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }

    private void Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style)
    {
        if (!string.IsNullOrEmpty(line))
        {
            if (font == null)
            {
                int scale = style.scale;
                if (scale < 2)
                    scale = 1;
                int w = PixelFont.Measure(line, scale);
                PixelFont.Draw(px, W, H, x - w / 2, y, line, style.colours[0], scale);
            }
            else
            {
                font.DrawCentred(px, W, H, x, y, line, style);
            }
        }
    }
}
