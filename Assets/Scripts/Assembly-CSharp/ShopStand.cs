// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR;

[ExecuteAlways]
public class ShopStand : MonoBehaviour
{
    [Tooltip("Which stand this is (each its own stock).")]
    public int index;
    [Tooltip("The big main stand: it shows the pricier pick, bigger.")]
    public bool featured;
    [Tooltip("What it sells, by its catalog name: the season's pack (the LIMITED stands). Empty: the fortnight's stock.")]
    public string sells;
    [Tooltip("Or a whole bundle, by its name (the OG pack's, the VIP's, a tube's): its price, every piece of it worn when it's bought. One for real money shows what comes with it and COMING SOON, and has no orb, till that's set up.")]
    public string bundle;
    [Header("Parts (GillaSetup.Shop builds them)")]
    [Tooltip("The screen: a quad, its front -Z, scaled to the screen's size.")]
    public Renderer screen;
    [Tooltip("The orb you drag, and where its track starts and ends (in the screen's frame, unscaled: TrackFrame).")]
    public Transform orb;
    [Tooltip("The orb you drag, and where its track starts and ends (in the screen's frame, unscaled: TrackFrame).")]
    public Transform trackFrame;
    public Vector3 trackFrom;
    public Vector3 trackTo;
    [Tooltip("Where the hologram stands, and how big it is (m).")]
    public Transform display;
    public float displaySize = 0.32f;
    [Tooltip("How far it floats up and down (m).")]
    public float bob = 0.025f;
    public Material hologram;
    public TextAsset titleFont;
    public TextAsset textFont;
    public AudioClip tickSound;
    public AudioClip buySound;
    public AudioClip errorSound;
    public AudioClip coinSound;
    public AudioClip sparkleSound;
    public AudioClip grabSound;
    [Tooltip("A glow ring that spreads out when it's bought.")]
    public Renderer burst;
    private const int W = 256;
    private const int H = 128;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Color32 Black = new Color32(6, 6, 6, 255);
    private static readonly Color32 Line = GillaBoardLook.Solid;
    private static readonly Color32 Dim = new Color32(96, 96, 96, 255);
    private static readonly Color32 Bright = new Color32(255, 255, 255, 255);
    private static readonly Color32 Coin = new Color32(232, 232, 232, 255);
    private static readonly Color32 White = new Color32(255, 255, 255, 255);
    private static readonly Color32[] Plain = GillaBoardLook.Soft;
    private static readonly Color32[] Picked = GillaBoardLook.White;
    private static readonly Color32[] Warning = GillaBoardLook.Dim;
    private byte id;
    private GameObject item;
    private Texture2D tex;
    private Color32[] px;
    private ScreenFont title;
    private ScreenFont text;
    private MaterialPropertyBlock block;
    private int hand = -1;
    private float along;
    private float ticked;
    private float message;
    private float burstT = -1f;
    private string messageText = "";
    private bool messageBad;
    private bool dirty = true;
    private bool wasGrip0;
    private bool wasGrip1;
    private int lastBalance = -1;
    private Vector3 displayRest = new Vector3(float.NaN, 0f, 0f);
    private static readonly DateTime StockFrom = new DateTime(2026, 11, 2);

    private CosmeticCatalog.Bundle Bundle
    {
        get
        {
            if (string.IsNullOrEmpty(bundle)) return null;
            return CosmeticCatalog.BundleNamed(bundle);
        }
    }

    private static int Fortnight
    {
        get { return (int)((DateTime.UtcNow - StockFrom).TotalDays / 14.0); }
    }

    public static DateTime NextStock
    {
        get { return StockFrom.AddDays((Fortnight + 1) * 14.0); }
    }

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void OnEnable()
    {

        if (screen == null) return;
        Texture2D t = new Texture2D(256, 128, TextureFormat.RGBA32, true);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.name = "ShopStand";
        t.hideFlags = HideFlags.HideAndDontSave;
        tex = t;
        px = new Color32[32768];
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        byte pick;
        if (Bundle == null)
        {
            if (string.IsNullOrEmpty(sells)) pick = Stock(index, featured);
            else pick = CosmeticCatalog.IdOf(sells);
        }
        else
        {
            pick = 0;
        }
        id = pick;
        if (orb != null)
        {
            GameObject go = orb.gameObject;
            bool active = Bundle == null ? true : !Bundle.realMoney;
            go.SetActive(active);
        }
        ShowItem();
        if (Application.isPlaying)
        {
            Coins.Changed += OnCoins;
        }
        Draw();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            Coins.Changed -= OnCoins;
            return;
        }
        if (item != null) DestroyImmediate(item);
        if (tex != null) DestroyImmediate(tex);
    }

    private void OnCoins()
    {
        dirty = true;
    }

    public static byte Stock(int index, bool featured)
    {
        List<byte> list = new List<byte>();
        for (int i = 1; i <= CosmeticCatalog.Count; i++)
        {
            if (CosmeticCatalog.Of(i).prefab != null && !CosmeticCatalog.Of(i).retired)
                list.Add((byte)i);
        }
        if (list.Count == 0) return 0;
        System.Random rng = new System.Random(Fortnight * 7919 + 17);
        int best = 0;
        if (list.Count - 1 > 0)
        {
            for (int i = list.Count - 1; i >= 1; i--)
            {
                int j = rng.Next(i + 1);
                byte a = list[j];
                byte b = list[i];
                list[i] = a;
                list[j] = b;
            }
            if (list.Count > 1)
            {
                int top = 0;
                for (int i = 1; i < Mathf.Min(list.Count, 4); i++)
                {
                    int p = CosmeticCatalog.Of(list[i]).price;
                    if (p > CosmeticCatalog.Of(list[top]).price) top = i;
                }
                best = top;
            }
        }
        if (!featured)
        {
            list.RemoveAt(best);
            best = index % list.Count;
        }
        return list[best];
    }

    private void ShowItem()
    {
        if (item != null)
        {
            if (Application.isPlaying) Destroy(item);
            else DestroyImmediate(item);
        }
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
        if (e == null) return;
        if (e.prefab == null) return;
        if (display == null) return;
        item = CosmeticCatalog.Display(e);
        item.transform.SetParent(display, false);
        if (!Application.isPlaying)
        {
            foreach (Transform t in item.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = Vector3.one;
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        bool stock = string.IsNullOrEmpty(sells);
        foreach (Renderer r in renderers)
        {
            if (stock)
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = hologram;
                r.sharedMaterials = mats;
            }
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
        Bounds bounds = renderers[0].bounds;
        for (int i = 0; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        Vector3 center = display.InverseTransformPoint(bounds.center);
        Vector3 size = display.InverseTransformVector(bounds.size);
        float sz = displaySize;
        float biggest = Mathf.Max(new float[] { Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z), 0.01f });
        float scale = sz / biggest;
        item.transform.localScale = Vector3.one * scale;
        float height = stock ? Mathf.Abs(size.y) * scale : displaySize;
        item.transform.localPosition = Vector3.up * (height * 0.5f) - center * scale;
        if (!stock) return;
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetFloat("_Base", display.position.y);
        mpb.SetFloat("_Height", Mathf.Abs(size.y) * scale);
        foreach (Renderer r in renderers) r.SetPropertyBlock(mpb);
        CosmeticMotion motion = item.GetComponent<CosmeticMotion>();
        if (motion != null)
        {
            System.Reflection.FieldInfo spinField = typeof(CosmeticMotion).GetField("spin", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic); /* ? */
            if (spinField != null && spinField.FieldType == typeof(float)) spinField.SetValue(motion, 200f); /* ? */
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        float dt = Time.deltaTime;
        if (display != null)
        {
            display.Rotate(0f, dt * (featured ? 25f : 35f), 0f, Space.Self);
            if (float.IsNaN(displayRest.x))
            {
                displayRest = display.localPosition;
            }
            display.localPosition = displayRest + Vector3.up * (bob * Mathf.Sin(Time.time * 1.9f + index));
        }
        Drag(dt);
        if (message > 0f)
        {
            message -= dt;
            if (message <= 0f) dirty = true;
        }
        if (Coins.Balance != lastBalance)
        {
            lastBalance = Coins.Balance;
            dirty = true;
        }
        if (burstT >= 0f) Burst(dt);
        if (dirty) Draw();
    }

    private void Drag(float dt)
    {
        if (Bundle != null && Bundle.realMoney) return;
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance; /* ? */
        bool grip0 = Grip(XRNode.LeftHand);
        bool grip1 = Grip(XRNode.RightHand);
        if (hand < 0 && player != null)
        {
            if (grip0 && !wasGrip0 && Near(player.leftHandFollower)) Grab(0);
            else if (grip1 && !wasGrip1 && Near(player.rightHandFollower)) Grab(1);
        }
        bool mouse = false;
        if (!XRSettings.isDeviceActive && Mouse.current != null)
        {
            mouse = Camera.main != null;
            if (mouse && hand < 0)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Camera cam = Camera.main;
                    Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if (orb.TryGetComponent<Collider>(out Collider col))
                    {
                        if (col.Raycast(ray, out RaycastHit hit, 6f)) Grab(2);
                    }
                }
                mouse = true;
            }
        }
        wasGrip1 = grip1;
        wasGrip0 = grip0;
        float a = along;
        if (hand < 0)
        {
            if (a > 0f)
            {
                ticked = 0f;
                a = Mathf.MoveTowards(a, 0f, dt * 2.5f);
                along = a;
                dirty = true;
            }
        }
        else
        {
            bool held;
            Vector3 point;
            if (hand == 0)
            {
                held = grip0;
                point = player.leftHandFollower.position;
            }
            else
            {
                held = grip1;
                if (hand != 1)
                {
                    held = false;
                    if (mouse) held = Mouse.current.leftButton.isPressed;
                }
                if (hand == 0)
                {
                    point = player.leftHandFollower.position;
                }
                else if (hand != 2)
                {
                    point = player.rightHandFollower.position;
                }
                else
                {
                    Camera cam = Camera.main;
                    Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                    Plane plane = new Plane(-trackFrame.forward, trackFrame.position);
                    if (plane.Raycast(ray, out float enter) && enter > 0f) point = ray.GetPoint(enter);
                    else point = orb.position;
                }
            }
            Vector3 local = trackFrame.InverseTransformPoint(point);
            Vector3 dir = trackTo - trackFrom;
            float t = Vector3.Dot(local - trackFrom, dir) / dir.sqrMagnitude;
            along = Mathf.Clamp01(t);
            if (Mathf.Abs(along - ticked) >= 0.1f)
            {
                ticked = Mathf.RoundToInt(along * 10f) / 10f;
                if (tickSound != null)
                {
                    float pitch = ticked * 0.7f;
                    Sfx.Play(new AudioClip[] { tickSound }, orb.position, 0.6f, pitch + 0.75f, pitch + 0.76f);
                }
                if (hand < 2)
                {
                    UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand != 0 ? XRNode.RightHand : XRNode.LeftHand);
                    device.SendHapticImpulse(0, ticked * 0.3f + 0.15f, 0.02f);
                }
            }
            if (!held) Drop();
            a = along;
            dirty = true;
        }
        orb.localPosition = Vector3.Lerp(trackFrom, trackTo, Mathf.Clamp01(a));
    }

    private void Grab(int h)
    {
        hand = h;
        if (grabSound != null)
        {
            Sfx.Play(new AudioClip[] { grabSound }, orb.position, 0.6f, 1f, 1.05f);
        }
        if (h < 2)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(h != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, 0.4f, 0.05f);
        }
    }

    private void Drop()
    {
        int h = hand;
        hand = -1;
        if (along < 0.92f) return;
        CosmeticCatalog.Bundle b = Bundle;
        int coins;
        string what;
        if (b == null)
        {
            CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
            if (e == null) return;
            if (Coins.Owns(id))
            {
                messageText = "ALREADY YOURS";
                messageBad = false;
                dirty = true;
                message = 2f;
                Cosmetics.Wear(id);
                return;
            }
            if (!Coins.Buy(new byte[] { id }, -1))
            {
                messageText = "NOT ENOUGH COINS";
                messageBad = true;
                dirty = true;
                message = 2f;
                GillaToast.NotEnough(h, e.price);
                Play(errorSound, 0.8f);
                return;
            }
            Cosmetics.Wear(id);
            messageText = "YOURS!";
            messageBad = false;
            dirty = true;
            what = e.label;
            coins = e.price;
            message = 2f;
        }
        else
        {
            if (Coins.OwnsAll(b))
            {
                messageText = "ALREADY YOURS";
                messageBad = false;
                dirty = true;
                message = 2f;
                WearAll(b);
                return;
            }
            coins = Coins.PriceOf(b);
            if (!Coins.Buy(b))
            {
                messageText = "NOT ENOUGH COINS";
                messageBad = true;
                dirty = true;
                message = 2f;
                GillaToast.NotEnough(h, coins);
                Play(errorSound, 0.8f);
                return;
            }
            WearAll(b);
            messageText = "YOURS!";
            messageBad = false;
            dirty = true;
            what = b.label;
            message = 2f;
        }
        GillaToast.Bought(h, what, coins);
        Play(buySound, 0.9f);
        Play(coinSound, 0.7f);
        Play(sparkleSound, 0.5f);
        burstT = 0f;
    }

    private static void WearAll(CosmeticCatalog.Bundle b)
    {
        foreach (byte i in CosmeticCatalog.IdsOf(b))
        {
            Cosmetics.Wear(i);
        }
    }

    private void Say(string s, bool bad)
    {
        messageText = s;
        messageBad = bad;
        message = 2f;
        dirty = true;
    }

    private bool Near(Transform hand)
    {
        if (hand == null) return false;
        return Vector3.Distance(hand.position, orb.position) < 0.1f;
    }

    private static bool Grip(XRNode node)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool result = false;
        if (device.isValid)
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float value)) result = value > 0.6f;
            else result = false;
        }
        return result;
    }

    private void Burst(float dt)
    {
        burstT = dt / 0.8f + burstT;
        if (burst != null)
        {
            float t = Mathf.Clamp01(burstT);
            burst.enabled = t < 1f;
            Transform tr = burst.transform;
            float inv = 1f - t;
            float grow = Mathf.Clamp01(1f - inv * inv);
            tr.localScale = Vector3.one * (grow * 1.9000001f + 0.3f);
            if (block == null) block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(inv * 1.8f, inv * 1.8f, inv * 1.8f, inv));
            burst.SetPropertyBlock(block);
        }
        if (burstT >= 1f) burstT = -1f;
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        Sfx.Play(new AudioClip[] { clip }, orb.position, volume, 0.97f, 1.03f);
    }

    private void Draw()
    {
        dirty = false;
        GillaBoardLook.Clear(px, 256, 128);
        GillaBoardLook.Frame(px, 256, 128, 3);
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
        CosmeticCatalog.Bundle b = Bundle;
        if (b != null || e != null)
        {
            bool real = b != null && b.realMoney;
            string label = b != null ? b.label : e.label;
            if (label == null)
            {
                if (real)
                {
                    Finish();
                    return;
                }
            }
            else
            {
                if (featured) Text(text, 128, 6, "FEATURED", Picked, 1, true);
                int y = 10;
                ScreenFont font;
                int scale;
                if (title == null)
                {
                    font = null;
                    scale = 2;
                }
                else
                {
                    int w = title.Measure(label, 2);
                    font = title;
                    scale = w < 245 ? 2 : 1;
                    y = w > 244 ? 15 : 10;
                }
                if (featured) y = 16;
                Text(font, 128, y, label, Plain, scale, true);
                bool owned;
                int price;
                if (b == null)
                {
                    owned = Coins.Owns(id);
                    price = e.price;
                }
                else
                {
                    owned = Coins.OwnsAll(b);
                    price = Coins.PriceOf(b);
                    if (real)
                    {
                        if (b.coins > 0 && !owned) /* ? */
                        {
                            string s = "+" + b.coins.ToString("N0") + " COINS";
                            int x = text == null ? 68 : 128 - (text.Measure(s, 2) + 20) / 2;
                            CoinIcon(x, 44);
                            Text(text, x + 20, 42, s, Picked, 2, false);
                        }
                        Text(text, 128, 82, owned ? "OWNED" : "AT ITS STAND", owned ? Picked : Plain, 2, true);
                        Finish();
                        return;
                    }
                }
                if (!owned)
                {
                    string s = price.ToString("N0");
                    int w = text == null ? 56 : text.Measure(s, 2) + 20;
                    CoinIcon(128 - w / 2, 44);
                    Text(text, 148 - w / 2, 42, s, Coins.Balance < price ? Warning : Plain, 2, false);
                }
                else
                {
                    Text(text, 128, 42, "OWNED", Picked, 2, true);
                }
            }
        }
        int from = Track(trackFrom);
        int to = Track(trackTo);
        for (int x = from; x <= to; x += 4)
        {
            FillRect(new RectInt(x, 92, 2, 2), Dim);
        }
        int knob = Mathf.RoundToInt((to - from) * Mathf.Clamp01(along) + from);
        FillRect(new RectInt(from, 92, Mathf.Max(knob - from, 0), 2), Line);
        Circle(to, 93, 13, along >= 0.92f ? Bright : Line);
        Text(text, to, 66, "DROP", along >= 0.92f ? Picked : Plain, 1, true);
        Text(text, to, 109, "HERE", along >= 0.92f ? Picked : Plain, 1, true);
        if (message > 0f)
        {
            FillRect(new RectInt(20, 60, 216, 18), Black);
            Outline(new RectInt(20, 60, 216, 18), messageBad ? Dim : Line);
            Text(text, 128, 65, messageText, messageBad ? Warning : Plain, 1, true);
        }
        Finish();
    }

    private void Finish()
    {
        tex.SetPixels32(px);
        tex.Apply(true);
        if (block == null) block = new MaterialPropertyBlock();
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        screen.GetPropertyBlock(mpb);
        mpb.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(mpb);
    }

    private int Track(Vector3 local)
    {
        Transform st = screen.transform;
        Vector3 p = st.InverseTransformPoint(trackFrame.TransformPoint(local));
        int x = Mathf.RoundToInt((p.x + 0.5f) * 256f);
        if (x > 254) x = 255;
        return Mathf.Max(x, 0);
    }

    private void Text(ScreenFont font, int x, int y, string s, Color32[] colours, int scale, bool centred = false)
    {
        if (font != null && !string.IsNullOrEmpty(s))
        {
            ScreenFont.Style style = new ScreenFont.Style();
            style.scale = scale;
            style.colours = colours;
            if (centred) font.DrawCentred(px, 256, 128, x, y, s, in style);
            else font.Draw(px, 256, 128, x, y, s, in style);
        }
    }

    private void CoinIcon(int x, int y)
    {
        string[] rows = new string[]
        {
            "..###..",
            ".#####.",
            "##.####",
            "##.####",
            "##.####",
            ".#####.",
            "..###..",
        };
        for (int r = 0; r < 7; r++)
        {
            int yy = y + r * 2;
            int xx = x;
            for (int c = 0; c < 7; c++)
            {
                if (rows[r][c] == '#')
                {
                    Set(xx, yy, Coin);
                    Set(xx + 1, yy, Coin);
                    Set(xx, yy + 1, Coin);
                    Set(xx + 1, yy + 1, Coin);
                }
                xx += 2;
            }
        }
    }

    private void Circle(int cx, int cy, int r, Color32 c)
    {
        for (int i = 0; i < 96; i++)
        {
            float a = (i * 3.1415927f + i * 3.1415927f) / 96f;
            int dx = Mathf.RoundToInt(Mathf.Cos(a) * r);
            int dy = Mathf.RoundToInt(Mathf.Sin(a) * r);
            Set(dx + cx, dy + cy, c);
        }
    }

    private void Set(int x, int y, Color32 c)
    {
        if (y < 128 && x < 256 && (y | x) >= 0)
        {
            px[x + (127 - y) * 256] = c;
        }
    }

    private void FillRect(RectInt r, Color32 c)
    {
        for (int y = r.yMin; y < r.yMax; y++)
        {
            for (int x = r.xMin; x < r.xMax; x++)
            {
                Set(x, y, c);
            }
        }
    }

    private void Outline(RectInt r, Color32 c)
    {
        for (int x = r.xMin; x < r.xMax; x++)
        {
            Set(x, r.yMin, c);
            Set(x, r.yMax - 1, c);
        }
        for (int y = r.yMin; y < r.yMax; y++)
        {
            Set(r.xMin, y, c);
            Set(r.xMax - 1, y, c);
        }
    }
}
