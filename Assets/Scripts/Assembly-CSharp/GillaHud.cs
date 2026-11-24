// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Fusion;

public class GillaHud : MonoBehaviour
{
    private class Message
    {
        public string top;
        public string bottom;
        public Color32[] topColours;
        public Color32[] bottomColours;
    }

    private struct Clock
    {
        public int key;
        public int bucket;
        public bool on;
    }

    public const int W = 320;
    public const int H = 80;
    public const float Ahead = 1.1f;
    public const float Below = 0.2f;
    public const float Wide = 0.6f;
    public const float ShowFor = 3f;
    public const float FadeIn = 0.2f;
    public const float FadeOut = 0.5f;
    public const float Every = 30f;

    [Tooltip("Gilla/Hud: drawn over everything, faded by _Alpha.")]
    public Material material;
    [Tooltip("Assets/Gilla/Fonts: the big line's (PixelOperatorHB8), the small one's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the big line's (PixelOperatorHB8), the small one's (GillaLCD).")]
    public TextAsset textFont;

    private static readonly Color32[] Gold = new Color32[] { new Color32(255, 240, 170, 255), new Color32(230, 170, 50, 255) };
    private static readonly Color32[] Lime = new Color32[] { new Color32(140, 255, 110, 255), new Color32(40, 215, 40, 255) };
    private static readonly Color32[] Milk = new Color32[] { new Color32(255, 255, 255, 255), new Color32(235, 225, 230, 255) };
    private static readonly Color32[] Red = new Color32[] { new Color32(255, 130, 110, 255), new Color32(235, 50, 40, 255) };
    private static readonly Color32[] Blue = new Color32[] { new Color32(150, 210, 255, 255), new Color32(50, 130, 255, 255) };
    private static readonly Color32[] Grey = new Color32[] { new Color32(200, 198, 205, 255), new Color32(160, 158, 165, 255) };
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int Alpha = Shader.PropertyToID("_Alpha");

    private readonly Queue<Message> waiting = new Queue<Message>();
    private Message current;
    private float shownAt;
    private Renderer picture;
    private Texture2D tex;
    private Color32[] px;
    private MaterialPropertyBlock block;
    private ScreenFont title;
    private ScreenFont text;
    private bool[] questDone;
    private float nextQuestLook;
    private Clock hunt;
    private Clock race;

    public static GillaHud Instance { get; private set; }

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    public Renderer Picture { get { return picture; } }

    public string Showing
    {
        get
        {
            if (current != null)
            {
                return current.top + " / " + current.bottom;
            }
            return null;
        }
    }

    public string Last { get; private set; }

    public float Shown { get; private set; }

    private void Awake()
    {

        if (!Application.isPlaying) return;
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }
    }

    private void OnEnable()
    {

        Instance = this;
        GillaLevel.Gained += OnGained;
    }

    private void OnDisable()
    {
        GillaLevel.Gained -= OnGained;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        if (picture != null)
        {
            Destroy(picture.gameObject);
        }
        if (tex != null)
        {
            Destroy(tex);
        }
    }

    private void OnGained(int amount, int levelsUp)
    {
        string top = "+" + amount.ToString() + " XP";
        string bottom;
        Color32[] topColours;
        Color32[] bottomColours;
        if (levelsUp < 1)
        {
            topColours = Gold;
            bottomColours = Grey;
            bottom = "QUEST CLAIMED";
        }
        else
        {
            int level = GillaLevel.Level;
            bottom = "LEVEL " + level.ToString() + "!";
            topColours = Gold;
            bottomColours = Lime;
        }
        Show(top, bottom, topColours, bottomColours);
    }

    public void Show(string top, string bottom)
    {
        Show(top, bottom, Milk, Grey);
    }

    private void Show(string top, string bottom, Color32[] topColours, Color32[] bottomColours)
    {
        if (waiting.Count > 2)
        {
            waiting.Dequeue();
        }
        Message m = new Message();
        m.top = top;
        m.bottom = bottom;
        m.topColours = topColours;
        m.bottomColours = bottomColours;
        waiting.Enqueue(m);
        Last = top + " / " + bottom;
    }

    public void Clear()
    {
        waiting.Clear();
        current = null;
        race = default(Clock);
        hunt = default(Clock);
        Shown = 0f;
        if (picture != null)
        {
            picture.enabled = false;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (picture == null && !Attach()) return;
        LookAtQuests();
        LookAtClocks();
        if (current == null)
        {
            if (waiting.Count > 0)
            {
                current = waiting.Dequeue();
                shownAt = Time.unscaledTime;
                Draw(current);
            }
            if (current == null)
            {
                if (picture.enabled)
                {
                    picture.enabled = false;
                }
                Shown = 0f;
                return;
            }
        }

        float t = Time.unscaledTime - shownAt;
        if (t >= ShowFor)
        {
            current = null;
            Shown = 0f;
            picture.enabled = false;
            return;
        }
        Shown = Mathf.Clamp01(Mathf.Min(t / FadeIn, (ShowFor - t) / FadeOut));
        picture.enabled = true;
        picture.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        block.SetFloat(Alpha, Shown);
        picture.SetPropertyBlock(block);
    }

    private bool Attach()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        if (material == null) return false;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "GillaHudPicture";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, -Below, Ahead);
        go.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -Below, Ahead), Vector3.up);
        go.transform.localScale = new Vector3(Wide, 0.15f, 1f);
        picture = go.GetComponent<Renderer>();
        picture.sharedMaterial = material;
        picture.shadowCastingMode = ShadowCastingMode.Off;
        picture.receiveShadows = false;
        picture.enabled = false;
        block = new MaterialPropertyBlock();
        Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.name = "GillaHud";
        tex = t;
        px = new Color32[W * H];
        if (title == null)
        {
            title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        }
        if (text == null)
        {
            text = textFont != null ? new ScreenFont(textFont.text) : null;
        }
        return true;
    }

    private void LookAtQuests()
    {
        if (Time.unscaledTime < nextQuestLook) return;
        nextQuestLook = Time.unscaledTime + 0.5f;
        bool[] before = questDone;
        if (before == null || before.Length < 3) /* ? */
        {
            questDone = new bool[3];
        }
        for (int i = 0; i < 3; i++)
        {
            bool flag;
            if (GillaQuests.DoneAt(i) && !GillaQuests.ClaimedAt(i))
            {
                flag = true;
                if (before != null && !questDone[i])
                {
                    GillaQuests.Quest quest = GillaQuests.QuestAt(i);
                    int xp = GillaLevel.ForYou(quest.xp);
                    Show("QUEST DONE", "+" + xp.ToString() + " XP AT THE BOARD", Lime, Gold);
                    flag = true;
                }
            }
            else
            {
                flag = false;
            }
            questDone[i] = flag;
        }
    }

    private void LookAtClocks()
    {
        byte role = 0;
        GillaModeRound round = GillaModeRound.Current;
        if (round != null && round.Object != null && round.Object.Runner && round.Object.IsValid /* ? */
            && (int)round.Phase == 1 && round.Runner != null)
        {
            NetworkDictionary<PlayerRef, byte> players = round.Players;
            if (players.TryGet(round.Runner.LocalPlayer, out role) && (role | 2) == 3)
            {
                TimeLeft(false, round.Round, round.HuntEnds - round.Now, role != 3 ? 2 : 1);
            }
            else
            {
                hunt.on = false;
            }
        }
        else
        {
            hunt.on = false;
        }

        GillaSplashGame game = GillaSplashGame.Current;
        if (game != null && game.Object != null && game.Object.Runner && game.Object.IsValid /* ? */
            && (int)game.Phase == 2 && game.Runner != null)
        {
            NetworkDictionary<PlayerRef, byte> players = game.Players;
            if (players.TryGet(game.Runner.LocalPlayer, out role) && role == 1)
            {
                TimeLeft(true, game.Starts, game.PhaseEnds - game.Now, 0);
                return;
            }
        }
        race.on = false;
    }

    public void TimeLeft(bool isRace, int key, float left, int role = 0)
    {
        ref Clock clock = ref (isRace ? ref race : ref hunt);
        float clamped = Math.Max(0f, left);
        string bottom = isRace ? "LEFT TO REACH THE END" : "LEFT IN THE HUNT";
        int bucket = (int)Math.Floor(clamped / Every);
        if (clock.on && clock.key == key)
        {
            if (bucket > 0 && bucket < clock.bucket)
            {
                string m = Minutes(bucket * 30);
                Show(m, bottom, bucket == 1 ? Red : Milk, Grey);
            }
            clock.bucket = bucket;
            return;
        }
        clock.key = key;
        clock.bucket = bucket;
        clock.on = true;

        string top;
        Color32[] colours;
        if (role == 0 || isRace)
        {
            if (left <= 1f) return;
            top = Minutes((int)Math.Floor(left));
            colours = left <= 30f ? Red : Milk;
        }
        else
        {
            top = role == 1 ? "YOU ARE THE MONSTER" : "YOU ARE THE SURVIVOR";
            string prefix = "";
            if (left > 1f)
            {
                prefix = Minutes((int)Math.Floor(left)) + " ";
            }
            bottom = prefix + bottom;
            colours = role == 1 ? Red : Blue;
        }
        Show(top, bottom, colours, Grey);
    }

    private static string Minutes(int seconds)
    {
        return (seconds / 60).ToString() + ":" + (seconds % 60).ToString("00");
    }

    private void Draw(Message m)
    {
        for (int i = 0; i < px.Length; i++)
        {
            px[i] = new Color32(0, 0, 0, 0);
        }
        int full = Measure(title, m.top, 3);
        int titleScale = full <= 288 ? 3 : 2;
        int titleWidth = Measure(title, m.top, titleScale);
        int textWidth = Measure(text, m.bottom, 2);
        int width = Math.Max(titleWidth, textWidth) + 28;
        if (width > 315) width = 316;
        PixelFont.Fill(px, W, H, (W - width) / 2, 3, width, 74, new Color32(4, 4, 8, 228));

        ScreenFont.Style topStyle = new ScreenFont.Style
        {
            scale = titleScale,
            slant = 0.25f,
            shadow = new Color32(10, 10, 14, 255),
            colours = m.topColours
        };
        Line(title, 160, full > 288 ? 13 : 7, m.top, topStyle);

        ScreenFont.Style bottomStyle = new ScreenFont.Style
        {
            scale = 2,
            slant = 0f,
            shadow = new Color32(10, 10, 14, 255),
            colours = m.bottomColours
        };
        Line(text, 160, 50, m.bottom, bottomStyle);

        tex.SetPixels32(px);
        tex.Apply(false);
    }

    private static int Measure(ScreenFont font, string s, int scale)
    {
        if (font != null)
        {
            return font.Measure(s, scale);
        }
        return PixelFont.Measure(s, scale);
    }

    private void Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style)
    {
        if (font == null)
        {
            int scale = style.scale;
            if (scale < 2) scale = 1;
            int width = PixelFont.Measure(line, scale);
            PixelFont.Draw(px, W, H, x - width / 2, y, line, style.colours[0], scale);
        }
        else
        {
            font.DrawCentred(px, W, H, x, y, line, style);
        }
    }
}
