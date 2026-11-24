// Redux addition: the jukebox's music screen, built onto the jukebox in 3D.
// A MUSIC button sits on the jukebox's top. Press it and a screen rises out of the top, leaning
// toward you: a search bar, a keyboard, song cards and a now-playing bar. It is part of the
// jukebox, so it goes wherever the jukebox is carried.
// Point with your index finger: a laser shows whenever you aim at the screen or the button, and
// the trigger clicks. You can also touch the keys, or click on PC.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public class GillaMusicPanel : MonoBehaviour
{
    private const float W = 640f;
    private const float H = 760f;
    private const float TouchDepth = 0.03f;
    private const float KeyDepth = 0.008f;
    private static readonly Quaternion AimFromGrip = Quaternion.Euler(60f, 0f, 0f);   // same aim as the mod menu
    private const float LaserReach = 4f;

    // colours
    private static readonly Color PanelCol = new Color(0.055f, 0.055f, 0.07f, 0.98f);
    private static readonly Color Card = new Color(0.12f, 0.12f, 0.15f, 1f);
    private static readonly Color CardHot = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color KeyCol = new Color(0.17f, 0.17f, 0.21f, 1f);
    private static readonly Color Accent = new Color(0.42f, 0.48f, 0.54f, 1f);     // storm grey
    private static readonly Color AccentHot = new Color(0.53f, 0.59f, 0.65f, 1f);
    private static readonly Color Dim = new Color(1f, 1f, 1f, 0.5f);
    private static readonly Color Faint = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color Full = new Color(0.3f, 0.85f, 0.45f, 1f);
    private static readonly Color Preview = new Color(1f, 0.72f, 0.2f, 1f);

    private class Key
    {
        public RectTransform rt;
        public Image bg;
        public Text label;
        public Action act;
        public bool inside;
        public bool hovered;
        public float hoverT, pressT;
        public Color col, hot;
        public Transform cap;
        public float scale;
        public float baseZ;
    }

    private static GillaMusicPanel instance;
    private Transform box;
    private Transform screenHinge;
    private Canvas panel;
    private PressKey musicKey;          // a real key on the boombox, cloned from its MUTE key
    private Collider musicKeyCollider;
    private Font readFont;              // smooth font for song names
    private RawImage[] resultArt = new RawImage[3];
    private RawImage nowArt;
    private Text nowArtist;
    private Key stopKey;
    private Key loopKey;
    private readonly List<Key> keys = new List<Key>();
    private readonly List<RectTransform> surfaces = new List<RectTransform>();   // aiming anywhere here shows the laser
    private Text searchText, statusText, nowText;
    private Image nowDot;
    private readonly Key[] resultKeys = new Key[3];
    private readonly Text[] resultTitle = new Text[3], resultArtist = new Text[3], resultBadge = new Text[3];
    private readonly Image[] resultBadgeBg = new Image[3];
    private string typed = "";
    private Font titleFont, bodyFont;
    private Sprite round, pill, soft;
    private float pressedAt;
    private readonly LineRenderer[] lasers = new LineRenderer[2];
    private readonly Transform[] laserDots = new Transform[2];
    private readonly bool[] triggerWas = new bool[2];
    private bool open;
    private float rise;
    private Material caseMat, rimMat, capMat;
    private float scale;
    private Transform[] tips;

    public static void Attach(Transform jukebox)
    {
        if (instance != null || jukebox == null) return;
        GameObject go = new GameObject("GillaMusicPanel");
        instance = go.AddComponent<GillaMusicPanel>();
        instance.box = jukebox;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Forget() { instance = null; }

    private void Start()
    {
        titleFont = Resources.Load<Font>("Fonts/Gilla_PixelOperator8Bold");
        bodyFont = Resources.Load<Font>("Fonts/Gilla_PixelOperatorBold");
        readFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        round = ModMenu.RoundedSprite(64, 14f, 1.5f, false);
        pill = ModMenu.RoundedSprite(64, 31f, 1.5f, false);
        soft = ModMenu.RoundedSprite(128, 40f, 30f, false);
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        caseMat = new Material(lit) { color = new Color(0.03f, 0.03f, 0.035f) };
        caseMat.SetFloat("_Smoothness", 0.85f);
        rimMat = new Material(lit) { color = new Color(0.55f, 0.56f, 0.6f) };
        rimMat.SetFloat("_Metallic", 0.9f);
        rimMat.SetFloat("_Smoothness", 0.75f);
        capMat = new Material(lit) { color = new Color(0.1f, 0.1f, 0.12f) };
        capMat.SetFloat("_Smoothness", 0.5f);

        PressKey anyKey = FindAnyObjectByType<PressKey>(FindObjectsInactive.Include);
        if (anyKey != null) tips = anyKey.fingertips;   // the same fingertips the game's keyboards use

        Bounds b = GillaBoomboxGrab.LocalBounds;
        if (b.size == Vector3.zero) b = new Bounds(new Vector3(0, 1f, 0), new Vector3(0.6f, 0.41f, 0.165f));
        scale = (b.size.x * 0.96f) / W;

        MakeButton(b);
        MakeScreen(b);
        MakeLasers();
        GillaMusic.Changed += Refresh;
        Refresh();
        Place(0f);
    }

    private void OnDestroy() { GillaMusic.Changed -= Refresh; }

    // ---- building blocks ----

    private GameObject Block(string name, Transform parent, Vector3 localPos, Vector3 size, Material m)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        Renderer r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    private Canvas MakeCanvas(string name, Transform parent, float w, float h, float s)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 6f;
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one * s;
        return c;
    }

    private Image Img(Transform parent, Sprite s, Color c, Vector2 at, Vector2 size)
    {
        GameObject go = new GameObject("Img");
        Image img = go.AddComponent<Image>();
        go.transform.SetParent(parent, false);
        img.sprite = s;
        img.type = s != null && s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = c;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = size;
        return img;
    }

    private Text Txt(Transform parent, string s, Vector2 at, Vector2 size, int fontSize, Color c, TextAnchor a, Font f)
    {
        GameObject go = new GameObject("Txt");
        Text t = go.AddComponent<Text>();
        go.transform.SetParent(parent, false);
        t.font = f;
        t.text = s;
        t.fontSize = fontSize;
        t.color = c;
        t.alignment = a;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 8;
        t.resizeTextMaxSize = fontSize;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = size;
        return t;
    }

    private Key MakeKey(Canvas canvas, string label, Vector2 at, Vector2 size, int fontSize, Color col, Color hot, Sprite shape, Action act)
    {
        Key k = new Key();
        k.scale = canvas.transform.localScale.x;
        k.col = col;
        k.hot = hot;
        Transform t = canvas.transform;
        Image under = Img(t, shape, new Color(0f, 0f, 0f, 0f), at, size);
        k.cap = under.transform;
        k.bg = Img(t, shape, col, at, size);
        k.rt = k.bg.rectTransform;
        k.baseZ = -KeyDepth / k.scale - 0.5f;
        k.rt.localPosition = new Vector3(at.x, at.y, k.baseZ);
        k.label = Txt(k.rt, label, Vector2.zero, new Vector2(size.x - 12f, size.y - 6f), fontSize, Color.white, TextAnchor.MiddleCenter, bodyFont);
        k.act = act;
        keys.Add(k);
        return k;
    }

    // ---- the MUSIC key ----

    // A copy of the boombox's own MUTE key, set next to it and labelled MUSIC, so it is a real
    // 3D push-button that presses like the rest of the boombox.
    private void MakeButton(Bounds b)
    {
        GillaBoombox bb = box.GetComponent<GillaBoombox>();
        Transform mute = bb != null && bb.muteKey != null ? bb.muteKey.transform : box.Find("Mute");
        if (mute == null) return;
        // made switched off and placed first: the key script remembers where it sits when it starts
        bool muteOn = mute.gameObject.activeSelf;
        mute.gameObject.SetActive(false);
        GameObject copy = Instantiate(mute.gameObject, mute.parent);
        mute.gameObject.SetActive(muteOn);
        copy.name = "MusicKey";
        Bounds mb = new Bounds(mute.position, Vector3.zero);
        foreach (Renderer r in mute.GetComponentsInChildren<Renderer>()) mb.Encapsulate(r.bounds);
        // the empty spot at the left front of the top: MUTE's place, mirrored across the middle
        Vector3 local = box.InverseTransformPoint(mute.position);
        local.x = 2f * b.center.x - local.x;
        copy.transform.position = box.TransformPoint(local);
        copy.transform.rotation = mute.rotation;
        copy.SetActive(true);
        musicKey = copy.GetComponent<PressKey>();
        if (musicKey != null)
        {
            musicKey.id = "MUSIC";
            musicKey.Pressed += _ => Toggle();
        }
        musicKeyCollider = copy.GetComponentInChildren<Collider>();
        foreach (PixelLabel label in copy.GetComponentsInChildren<PixelLabel>(true))
        {
            label.text = "MUSIC";
            label.colour = new Color32(255, 240, 240, 255);
            label.background = new Color32(107, 122, 137, 255);
            label.Redraw();
        }
        // a clear MUSIC label printed on the key's top
        // sits right on the key's top face (its cap), so it neither floats above it nor sinks into it
        Bounds kb = new Bounds(copy.transform.position, Vector3.zero);
        bool anyCap = false;
        PressKey capsOf = copy.GetComponent<PressKey>();
        if (capsOf != null && capsOf.caps != null)
        {
            foreach (Renderer r in capsOf.caps)
            {
                if (r == null) continue;
                if (!anyCap) { kb = r.bounds; anyCap = true; } else kb.Encapsulate(r.bounds);
            }
        }
        if (!anyCap) foreach (Renderer r in copy.GetComponentsInChildren<Renderer>()) kb.Encapsulate(r.bounds);
        GameObject lg = new GameObject("MusicKeyLabel");
        lg.transform.SetParent(copy.transform, true);
        lg.transform.position = new Vector3(kb.center.x, kb.max.y + 0.0004f, kb.center.z);
        lg.transform.rotation = Quaternion.LookRotation(Vector3.down, box.rotation * Vector3.forward) ;
        lg.transform.rotation = Quaternion.LookRotation(-Vector3.up, -(box.rotation * Vector3.back));
        Canvas lc = lg.AddComponent<Canvas>();
        lc.renderMode = RenderMode.WorldSpace;
        lg.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 6f;
        RectTransform lrt = (RectTransform)lg.transform;
        lrt.sizeDelta = new Vector2(200f, 60f);
        float across = Mathf.Max(kb.size.x, kb.size.z);
        lrt.localScale = Vector3.one * (across * 0.85f / 200f) / Mathf.Max(copy.transform.lossyScale.x, 0.0001f);
        Text kt = Txt(lg.transform, "MUSIC", Vector2.zero, new Vector2(200f, 60f), 48, Color.white, TextAnchor.MiddleCenter, titleFont);
    }

    // ---- the screen ----

    private void MakeScreen(Bounds b)
    {
        GameObject hinge = new GameObject("MusicScreenHinge");
        hinge.transform.SetParent(box, false);
        hinge.transform.localPosition = new Vector3(b.center.x, b.max.y + 0.005f, b.max.z - 0.012f);
        screenHinge = hinge.transform;

        float w = W * scale, h = H * scale;
        GameObject body = new GameObject("MusicScreen");
        body.transform.SetParent(screenHinge, false);
        body.transform.localPosition = new Vector3(0, h * 0.5f + 0.025f, 0);
        // slim glossy case with a thin metal frame, and a short neck down to the jukebox
        // the back of the screen, a little smaller than the face so nothing sticks out past its rounded corners
        Block("Case", body.transform, new Vector3(0, 0, 0.008f), new Vector3(w - 0.03f, h - 0.03f, 0.014f), caseMat);
        // a full-size rounded back plate, so from behind you see a clean shell, not the keys poking through
        Canvas back = MakeCanvas("MusicPanelBack", body.transform, W, H, scale);
        back.transform.localPosition = new Vector3(0f, 0f, 0.0162f);
        back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Img(back.transform, round, Accent, Vector2.zero, new Vector2(W, H));
        Img(back.transform, round, new Color(0.03f, 0.03f, 0.035f, 1f), Vector2.zero, new Vector2(W - 8f, H - 8f));

        panel = MakeCanvas("MusicPanel", body.transform, W, H, scale);
        surfaces.Add((RectTransform)panel.transform);
        Transform t = panel.transform;
        Img(t, round, Accent, Vector2.zero, new Vector2(W, H));                       // red outline
        Img(t, round, PanelCol, Vector2.zero, new Vector2(W - 8f, H - 8f));

        // header
        float y = H * 0.5f - 44f;
        // a Mac-style close button: small red circle with an x, in the top-right corner
        Key close = MakeKey(panel, "", new Vector2(W * 0.5f - 44f, y), new Vector2(34f, 34f), 20, Accent, AccentHot, pill, Toggle);
        Text x = Txt(close.rt, "×", new Vector2(0f, 1f), new Vector2(34f, 34f), 30, new Color(0.1f, 0.12f, 0.15f, 1f), TextAnchor.MiddleCenter, readFont);
        x.fontStyle = FontStyle.Bold;
        Txt(t, "GILLA RADIO", new Vector2(-W * 0.5f + 222f, y), new Vector2(380f, 48f), 32, Color.white, TextAnchor.MiddleLeft, titleFont);
        Img(t, null, Faint, new Vector2(0, y - 34f), new Vector2(W - 40f, 2f));

        // search bar
        y -= 74f;
        Img(t, pill, Card, new Vector2(-60f, y), new Vector2(W - 160f, 56f));
        searchText = Txt(t, "", new Vector2(-60f, y), new Vector2(W - 210f, 40f), 26, Color.white, TextAnchor.MiddleLeft, readFont);
        MakeKey(panel, "SEARCH", new Vector2(W * 0.5f - 74f, y), new Vector2(118f, 56f), 24, Accent, AccentHot, pill, () => { if (typed.Trim().Length > 0) GillaMusic.Search(typed); });

        // keyboard
        string[] rows = { "1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
        float ky = y - 62f;
        float kw = 54f, kh = 46f, gap = 6f;
        foreach (string row in rows)
        {
            float x0 = -(row.Length * (kw + gap) - gap) * 0.5f + kw * 0.5f;
            for (int i = 0; i < row.Length; i++)
            {
                char ch = row[i];
                MakeKey(panel, ch.ToString(), new Vector2(x0 + i * (kw + gap), ky), new Vector2(kw, kh), 26, KeyCol, CardHot, round, () => Type(ch));
            }
            ky -= kh + gap;
        }
        MakeKey(panel, "SPACE", new Vector2(-70f, ky), new Vector2(300f, kh), 22, KeyCol, CardHot, round, () => Type(' '));
        MakeKey(panel, "DEL", new Vector2(150f, ky), new Vector2(120f, kh), 22, KeyCol, CardHot, round, Back);
        ky -= kh + 14f;

        statusText = Txt(t, "", new Vector2(0, ky), new Vector2(W - 60f, 22f), 18, Dim, TextAnchor.MiddleCenter, bodyFont);
        ky -= 30f;

        // song cards: cover, title, artist, play button
        for (int i = 0; i < resultKeys.Length; i++)
        {
            int n = i;
            Key k = MakeKey(panel, "", new Vector2(0, ky), new Vector2(W - 40f, 64f), 20, Card, CardHot, round, () => GillaMusic.Pick(n));
            resultKeys[i] = k;
            float half = (W - 40f) * 0.5f;
            resultArt[i] = Art(k.rt, new Vector2(-half + 36f, 0), 52f);
            resultTitle[i] = Txt(k.rt, "", new Vector2(-half + 72f + 200f, 11f), new Vector2(400f, 28f), 22, Color.white, TextAnchor.MiddleLeft, readFont);
            resultTitle[i].fontStyle = FontStyle.Bold;
            resultArtist[i] = Txt(k.rt, "", new Vector2(-half + 72f + 200f, -14f), new Vector2(400f, 22f), 17, Dim, TextAnchor.MiddleLeft, readFont);
            resultBadgeBg[i] = Img(k.rt, pill, Accent, new Vector2(half - 32f, 0), new Vector2(32f, 32f));
            Img(resultBadgeBg[i].rectTransform, PlayIcon(), Color.white, new Vector2(1.5f, 0f), new Vector2(13f, 14f));
            resultBadge[i] = Txt(k.rt, "", new Vector2(half - 96f, -14f), new Vector2(80f, 18f), 14, Dim, TextAnchor.MiddleRight, readFont);
            ky -= 70f;
        }

        // now playing: cover, title, artist, STOP
        float by = -H * 0.5f + 44f;
        Img(t, null, Faint, new Vector2(0, by + 40f), new Vector2(W - 40f, 2f));
        nowArt = Art(t, new Vector2(-W * 0.5f + 52f, by), 60f);
        nowDot = Img(t, pill, new Color(0, 0, 0, 0), new Vector2(-W * 0.5f + 96f, by + 12f), new Vector2(1f, 1f));
        nowText = Txt(t, "", new Vector2(-W * 0.5f + 108f + 125f, by + 11f), new Vector2(250f, 28f), 21, Color.white, TextAnchor.MiddleLeft, readFont);
        nowText.fontStyle = FontStyle.Bold;
        nowArtist = Txt(t, "", new Vector2(-W * 0.5f + 108f + 125f, -12f + by), new Vector2(250f, 22f), 16, Dim, TextAnchor.MiddleLeft, readFont);
        // STOP: a red rounded button with clean bold text, greyed out while nothing plays
        // same pixel font as SEARCH, centred on the now-playing line
        stopKey = MakeKey(panel, "STOP", new Vector2(W * 0.5f - 76f, by + 7f), new Vector2(112f, 46f), 24, Accent, AccentHot, round, GillaMusic.StopForAll);
        stopKey.label.rectTransform.anchoredPosition = new Vector2(0f, 2f);   // the pixel font sits low
        // LOOP: lit while on, for the whole room
        loopKey = MakeKey(panel, "LOOP", new Vector2(W * 0.5f - 196f, by + 7f), new Vector2(112f, 46f), 24, Card, CardHot, round, () => GillaMusic.LoopForAll(!GillaMusic.Loop));
        loopKey.label.rectTransform.anchoredPosition = new Vector2(0f, 2f);
    }

    // a rounded square for an album cover, dark until the cover arrives
    private RawImage Art(Transform parent, Vector2 at, float size)
    {
        // a rounded window: everything inside is clipped to the rounded shape
        Image window = Img(parent, round, new Color(0.2f, 0.2f, 0.24f, 1f), at, new Vector2(size, size));
        window.gameObject.name = "CoverWindow";
        Mask mask = window.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;   // the grey shows while the cover loads
        Txt(window.rectTransform, "♪", Vector2.zero, new Vector2(size, size), (int)(size * 0.6f), Dim, TextAnchor.MiddleCenter, readFont);
        GameObject go = new GameObject("Cover");
        RawImage ri = go.AddComponent<RawImage>();
        go.transform.SetParent(window.rectTransform, false);
        ri.raycastTarget = false;
        RectTransform rt = ri.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
        ri.color = new Color(1, 1, 1, 0);
        return ri;
    }

    private static Sprite play;

    // a right-pointing triangle, smooth-edged
    private static Sprite PlayIcon()
    {
        if (play != null) return play;
        const int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                // inside if left of the two slanted edges and right of x = 6
                float fx = x + 0.5f, fy = y + 0.5f;
                float top = (fy - n * 0.5f) - (n * 0.5f - 4f) * (1f - (fx - 6f) / (n - 10f));
                float bottom = -(fy - n * 0.5f) - (n * 0.5f - 4f) * (1f - (fx - 6f) / (n - 10f));
                float d = Mathf.Max(Mathf.Max(top, bottom), 6f - fx);
                byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        play = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return play;
    }

    private static void ShowArt(RawImage ri, string url)
    {
        Texture2D tex = GillaMusic.Cover(url);
        ri.texture = tex;
        ri.color = tex != null ? Color.white : new Color(1, 1, 1, 0);
    }

    private void MakeLasers()
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.7f, 0.76f, 0.82f) };
        Material dotM = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.white };
        for (int i = 0; i < 2; i++)
        {
            GameObject lg = new GameObject(i == 0 ? "MusicLaserL" : "MusicLaserR");
            LineRenderer lr = lg.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = 0.004f;
            lr.endWidth = 0.0015f;
            lr.sharedMaterial = m;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            lasers[i] = lr;
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "MusicLaserDot";
            DestroyImmediate(dot.GetComponent<Collider>());
            dot.transform.localScale = Vector3.one * 0.012f;
            dot.GetComponent<Renderer>().sharedMaterial = dotM;
            dot.SetActive(false);
            laserDots[i] = dot.transform;
        }
    }

    // ---- behaviour ----

    private void Type(char c)
    {
        if (typed.Length >= 30) return;
        if (c == ' ' && (typed.Length == 0 || typed.EndsWith(" "))) return;
        typed += c;
    }

    private void Back()
    {
        if (typed.Length > 0) typed = typed.Substring(0, typed.Length - 1);
    }

    private void Toggle() { open = !open; }

    // 0: folded face-down on the jukebox's top; 1: standing up, leaning 12 degrees toward you
    private void Place(float r)
    {
        float tilt = Mathf.Lerp(-90f, -12f, Mathf.SmoothStep(0f, 1f, r));
        screenHinge.localRotation = Quaternion.Euler(tilt, 0f, 0f);
        screenHinge.gameObject.SetActive(r > 0.02f);
    }

    private void Refresh()
    {
        if (searchText == null) return;
        bool blink = (Time.unscaledTime % 1f) < 0.55f;
        searchText.text = typed.Length > 0 ? typed + (blink ? "|" : "") : "Search for a song...";
        searchText.color = typed.Length > 0 ? Color.white : Dim;
        if (GillaMusic.Searching) statusText.text = "SEARCHING...";
        else if (GillaMusic.Results.Count > 0) statusText.text = "";
        else statusText.text = GillaMusic.Status.Length > 0 ? GillaMusic.Status : "";
        for (int i = 0; i < resultKeys.Length; i++)
        {
            bool has = i < GillaMusic.Results.Count;
            resultKeys[i].rt.gameObject.SetActive(has);
            resultKeys[i].cap.gameObject.SetActive(has);
            if (!has) continue;
            GillaMusic.Result r = GillaMusic.Results[i];
            resultTitle[i].text = r.title ?? "";
            resultArtist[i].text = r.artist ?? "";
            ShowArt(resultArt[i], r.art);
            resultBadge[i].text = "";
            resultBadgeBg[i].color = resultKeys[i].hovered ? AccentHot : Accent;
        }
        bool playing = GillaMusic.NowPlaying.Length > 0;
        string now = GillaMusic.NowPlaying;
        int dash = now.IndexOf(" - ");
        nowText.text = playing ? (dash > 0 ? now.Substring(dash + 3) : now) : "Nothing playing";
        nowArtist.text = playing && dash > 0 ? now.Substring(0, dash) : (playing ? "" : "Pick a song to play it here");
        ShowArt(nowArt, playing ? GillaMusic.NowArt : "");
        if (loopKey != null)
        {
            loopKey.col = GillaMusic.Loop ? Accent : Card;
            loopKey.hot = GillaMusic.Loop ? AccentHot : CardHot;
        }
        if (stopKey != null)
        {
            stopKey.col = playing ? Accent : new Color(0.25f, 0.25f, 0.29f, 1f);
            stopKey.hot = playing ? AccentHot : new Color(0.3f, 0.3f, 0.34f, 1f);
        }
        nowText.color = playing ? Color.white : Dim;
        nowDot.color = new Color(0, 0, 0, 0);
    }

    private void LateUpdate()
    {
        if (box == null) { Destroy(gameObject); return; }
        float target = open ? 1f : 0f;
        if (!Mathf.Approximately(rise, target))
        {
            rise = Mathf.MoveTowards(rise, target, Time.deltaTime / 0.35f);
            Place(rise);
        }
        if (open)
        {
            Camera cam = Camera.main;
            if (cam != null && (cam.transform.position - box.position).sqrMagnitude > 25f) open = false;   // walked away
        }
        foreach (Key k in keys) k.hovered = false;
        Lasers();
        Touches();
        Clicks();
        Animate();
        Refresh();
    }

    private bool Live(RectTransform rt)
    {
        if (!rt.gameObject.activeInHierarchy) return false;
        if (rt.IsChildOf(screenHinge)) return rise > 0.95f;
        return true;
    }

    // Where a ray meets a flat UI rect, if it does.
    private static bool Hits(RectTransform rt, Ray ray, float reach, out float dist)
    {
        Plane p = new Plane(rt.forward, rt.position);
        if (!p.Raycast(ray, out dist) || dist > reach) return false;
        Vector3 local = rt.InverseTransformPoint(ray.GetPoint(dist));
        Vector2 half = rt.rect.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
    }

    private Transform Fingertip(int i)
    {
        if (tips != null && tips.Length > i && tips[i] != null && tips[i].gameObject.activeInHierarchy) return tips[i];
        GorillaLocomotion.Player rig = GorillaLocomotion.Player.Instance;
        return rig != null ? (i == 1 ? rig.rightHandTransform : rig.leftHandTransform) : null;
    }

    // VR: a laser from each index finger, on whenever it points at the screen or the button.
    private void Lasers()
    {
        GorillaLocomotion.Player rig = GorillaLocomotion.Player.Instance;
        bool vr = XRSettings.isDeviceActive;
        for (int i = 0; i < 2; i++)
        {
            LineRenderer lr = lasers[i];
            Transform hand = rig != null ? (i == 1 ? rig.rightHandTransform : rig.leftHandTransform) : null;
            Transform tip = Fingertip(i);
            if (!vr || hand == null || tip == null) { lr.enabled = false; laserDots[i].gameObject.SetActive(false); continue; }
            Vector3 from = tip.position;
            Vector3 dir = (hand.rotation * AimFromGrip) * Vector3.forward;
            Ray ray = new Ray(from, dir);
            float surfaceDist = float.MaxValue;
            foreach (RectTransform s in surfaces)
            {
                float d;
                if (Live(s) && Hits(s, ray, LaserReach, out d) && d < surfaceDist) surfaceDist = d;
            }
            UnityEngine.XR.InputDevice dev = InputDevices.GetDeviceAtXRNode(i == 1 ? XRNode.RightHand : XRNode.LeftHand);
            float tv = 0f;
            bool trig = dev.isValid && dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out tv) && tv > 0.7f;
            RaycastHit keyHit = default;
            bool onMusicKey = musicKeyCollider != null && musicKeyCollider.Raycast(ray, out keyHit, LaserReach) && keyHit.distance < surfaceDist;
            if (onMusicKey)
            {
                lr.enabled = true;
                lr.SetPosition(0, from);
                lr.SetPosition(1, keyHit.point);
                laserDots[i].gameObject.SetActive(true);
                laserDots[i].position = keyHit.point;
                if (trig && !triggerWas[i] && musicKey != null) musicKey.Click();
            }
            else if (surfaceDist < float.MaxValue)
            {
                float kd;
                Key hit = Aimed(ray, LaserReach, out kd);
                if (hit != null) hit.hovered = true;
                Vector3 end = from + dir * surfaceDist;
                lr.enabled = true;
                lr.SetPosition(0, from);
                lr.SetPosition(1, end);
                laserDots[i].gameObject.SetActive(true);
                laserDots[i].position = end;
                if (trig && !triggerWas[i] && hit != null) Press(hit);
            }
            else
            {
                lr.enabled = false;
                laserDots[i].gameObject.SetActive(false);
            }
            triggerWas[i] = trig;
        }
    }

    private Key Aimed(Ray ray, float reach, out float dist)
    {
        Key best = null;
        dist = reach;
        foreach (Key key in keys)
        {
            float d;
            if (!Live(key.rt) || !Hits(key.rt, ray, dist, out d)) continue;
            best = key;
            dist = d;
        }
        return best;
    }

    // Touch: a fingertip pushed into a key presses it once.
    private void Touches()
    {
        foreach (Key key in keys)
        {
            if (!Live(key.rt)) { key.inside = false; continue; }
            bool now = false;
            for (int h = 0; h < 2; h++)
            {
                Transform tip = Fingertip(h);
                if (tip == null) continue;
                Vector3 local = key.rt.InverseTransformPoint(tip.position);
                Vector2 half = key.rt.rect.size * 0.5f;
                float depth = Mathf.Abs(local.z) * key.rt.lossyScale.z;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && depth <= TouchDepth) now = true;
            }
            if (now && !key.inside) Press(key);
            key.inside = now;
        }
    }

    // PC: hover and left click.
    private void Clicks()
    {
        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (mouse == null || cam == null) return;
        float d;
        Key key = Aimed(cam.ScreenPointToRay(mouse.position.ReadValue()), 8f, out d);
        if (key == null) return;
        key.hovered = true;
        if (mouse.leftButton.wasPressedThisFrame) Press(key);
    }

    private void Press(Key key)
    {
        if (Time.unscaledTime - pressedAt < 0.12f) return;
        pressedAt = Time.unscaledTime;
        key.pressT = 1f;
        key.act?.Invoke();
        Refresh();
    }

    // keys light up when aimed at and sink in when pressed
    private void Animate()
    {
        float dt = Time.deltaTime;
        foreach (Key k in keys)
        {
            k.hoverT = Mathf.MoveTowards(k.hoverT, k.hovered ? 1f : 0f, dt / 0.08f);
            k.pressT = Mathf.MoveTowards(k.pressT, 0f, dt / 0.15f);
            Color c = Color.Lerp(k.col, k.hot, k.hoverT);
            k.bg.color = Color.Lerp(c, Color.white, k.pressT * 0.4f);
            float sink = k.pressT * KeyDepth * 0.7f / k.scale;
            Vector3 p = k.rt.localPosition;
            p.z = k.baseZ + sink;
            k.rt.localPosition = p;
        }
    }
}
