// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(300)]
public partial class GillaComputer : MonoBehaviour
{
    public Renderer screen;
    [Tooltip("The local player's apes to paint (first-person and mirror copies).")]
    public ApeColor[] targets;
    public PressKey[] keys;
    [Tooltip("Assets/Gilla/Fonts: GillaOS's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: GillaOS's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset textFont;
    public const int CodePage = 0;
    public const int ColorPage = 1;
    public const int SettingsPage = 2;
    public const int MusicPage = 3;   // Redux (GillaComputer.Music.cs)
    private static readonly string[] PageNames = new[] { "CODE", "COLOR", "SETTINGS", "MUSIC" };
    private const int W = 320;
    private const int H = 240;
    private const int MaxCode = 8;
    private const string SaveKey = "GillaColour";
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static int page;
    private static readonly int[] rgb;
    private static int channel;
    private static string typed = "";
    private static bool loaded;
    private static event Action Changed;
    private string shown;
    private Texture2D tex;
    private Color32[] px;
    private MaterialPropertyBlock block;
    private float nextRefresh;
    private ScreenFont title;
    private ScreenFont text;
    private static readonly Color32[] Silver = new[] { new Color32(255, 255, 255, 255), new Color32(200, 200, 200, 255), new Color32(140, 140, 140, 255) };
    private static readonly Color32[] Heading = new[] { new Color32(170, 170, 170, 255), new Color32(205, 205, 205, 255), new Color32(232, 232, 232, 255), new Color32(252, 252, 252, 255) };
    private static readonly Color32[] Milk = new[] { new Color32(255, 255, 255, 255), new Color32(205, 205, 205, 255) };
    private static readonly Color32[] Orange = new[] { new Color32(255, 190, 105, 255), new Color32(240, 120, 25, 255) };
    private static readonly Color32[] Lime = new[] { new Color32(140, 255, 110, 255), new Color32(40, 215, 40, 255) };
    private static readonly Color32[] Grey = new[] { new Color32(226, 226, 226, 255), new Color32(196, 196, 196, 255) };
    private static readonly Color32[] Dim = new[] { new Color32(124, 124, 124, 255), new Color32(94, 94, 94, 255) };
    private static readonly Color32 Shadow = new Color32(48, 48, 48, 255);
    private static readonly Color32 Divider = new Color32(56, 56, 56, 255);
    private static readonly Color32 Cursor = new Color32(116, 116, 116, 255);
    private static readonly Color32[][] Channels = new[] { new[] { new Color32(255, 120, 70, 255), new Color32(255, 60, 20, 255) }, new[] { new Color32(120, 255, 100, 255), new Color32(40, 225, 30, 255) }, new[] { new Color32(90, 90, 255, 255), new Color32(40, 40, 245, 255) } };
    public static readonly Bounds Corner = new Bounds(new Vector3(-9.25f, 1.2f, 62.575f), new Vector3(4.9f, 3.6f, 4.75f));
    private static float nextCornerLook;

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private static Color32 G(byte v)
    {
        return new Color32(v, v, v, 255);
    }

    private void OnEnable()
    {

        if (Application.isPlaying && !loaded && PlayerPrefs.HasKey("GillaColour"))
        {
            int saved = PlayerPrefs.GetInt("GillaColour");
            rgb[0] = (saved / 100) % 10;
            rgb[1] = (saved / 10) % 10;
            rgb[2] = saved % 10;
        }
        if (Application.isPlaying)
        {
            loaded = true;
        }
        Redraw(true);
        if (keys != null)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                PressKey key = keys[i];
                if (key != null)
                {
                    key.Pressed += OnKey;
                }
            }
        }
        Changed += OnChanged;
    }

    private void OnDisable()
    {
        if (keys != null)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                PressKey key = keys[i];
                if (key != null)
                {
                    key.Pressed -= OnKey;
                }
            }
        }
        Changed -= OnChanged;
    }

    private void OnChanged()
    {
        Redraw(false);
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        typed = "";
        loaded = false;
        page = 0;
        channel = 0;
    }

    private void Start()
    {
        if (Application.isPlaying && PlayerPrefs.HasKey("GillaColour"))
        {
            Paint();
            return;
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            LookAtCorner();
        }
        if (Application.isPlaying && nextRefresh <= Time.time)
        {
            nextRefresh = Time.time + 1f;
            Redraw(false);
            return;
        }
    }

    private bool InCorner
    {
        get
        {
            if (screen != null)
            {
                return Corner.Contains(screen.bounds.center);
            }
            return false;
        }
    }

    private void LookAtCorner()
    {
        if (nextCornerLook <= Time.unscaledTime && InCorner)
        {
            nextCornerLook = Time.unscaledTime + 0.5f;
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            Camera cam = Camera.main;
            if (boot == null) return;
            if (cam == null) return;
            if (boot.Joining) return;
            if (boot.Runner != null && boot.Runner.IsRunning) return; /* ? */
            Vector3 pos = cam.transform.position;
            pos.y = Corner.center.y;
            if (!Corner.Contains(pos))
            {
                Debug.Log("[Network] out of the computer's corner in no room: off to the public rooms");
                nextCornerLook = Time.unscaledTime + 10f;
                boot.JoinRoom("");
            }
        }
    }

    private void OnKey(PressKey key)
    {
        int before = Code;
        string id = key.id;
        bool digit = id.Length == 1 && char.IsDigit(id[0]);
        if (id == "UP")
        {
            page = (page + PageNames.Length - 1) % PageNames.Length;
        }
        else if (id == "DOWN")
        {
            page = (page + 1) % PageNames.Length;
        }
        else if (page == CodePage)
        {
            CodeKey(id, InCorner);
        }
        else if (page == ColorPage)
        {
            ColourKey(id, digit);
        }
        else if (page == MusicPage)
        {
            MusicKey(id, digit);
        }
        else
        {
            SettingsKey(id, digit);
        }
        if (Code != before)
        {
            Paint();
            PlayerPrefs.SetInt("GillaColour", Code);
            PlayerPrefs.Save();
            GillaCloud.ProfileChanged();
        }
        Changed?.Invoke();
    }

    private static bool InRoom
    {
        get
        {
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            if (boot != null && !boot.Joining && boot.Runner != null) /* ? */
            {
                if (!boot.Runner.IsRunning) /* ? */
                {
                    return false;
                }
                return boot.Runner.SessionInfo != null; /* ? */
            }
            return false;
        }
    }

    private static void CodeKey(string id, bool cornered)
    {
        if (id == "BACK")
        {
            if (typed.Length < 1)
            {
                return;
            }
            typed = typed.Substring(0, typed.Length - 1);
            return;
        }
        if (id == "ENTER")
        {
            if (NetworkBootstrap.Instance != null)
            {
                NetworkBootstrap.Instance.JoinRoom(typed);
            }
        }
        else if (id == "OPTION 1")
        {
            if (InRoom)
            {
                NetworkBootstrap.Instance.Disconnect();
            }
        }
        else
        {
            if (id.Length != 1) return;
            if (!char.IsLetterOrDigit(id[0])) return;
            if (typed.Length > 7) return;
            typed = typed + id;
            return;
        }
        typed = "";
    }

    private static void ColourKey(string id, bool digit)
    {
        if (id == "OPTION 1")
        {
            channel = 0;
            return;
        }
        if (id == "OPTION 2")
        {
            channel = 1;
        }
        else if (id == "OPTION 3")
        {
            channel = 2;
        }
        else if (id == "ENTER")
        {
            channel = (channel + 1) % 3;
        }
        else if (id == "LEFT")
        {
            rgb[channel] = Mathf.Max(rgb[channel] - 1, 0);
        }
        else if (id == "RIGHT")
        {
            rgb[channel] = Mathf.Min(rgb[channel] + 1, 9);
        }
        else
        {
            if (!digit) return;
            rgb[channel] = id[0] - '0';
        }
    }

    private static void SettingsKey(string id, bool digit)
    {
        if (id == "OPTION 1")
        {
            GillaTurn.Mode = 0;
        }
        else if (id == "OPTION 2")
        {
            GillaTurn.Mode = 1;
        }
        else if (id == "OPTION 3")
        {
            GillaTurn.Mode = 2;
        }
        else if (id == "LEFT")
        {
            GillaTurn.SnapAngle = 45;
        }
        else if (id == "RIGHT")
        {
            GillaTurn.SnapAngle = 90;
        }
        else if (id == "ENTER")
        {
            GillaPlayerPunching.On = !GillaPlayerPunching.On;
        }
        else if (digit)
        {
            GillaTurn.SmoothSpeed = id[0] - '0';
        }
    }

    public static int SavedCode
    {
        get { return PlayerPrefs.GetInt("GillaColour", -1); }
    }

    public static void Adopt(int code)
    {
        if ((uint)code > 999)
        {
            return;
        }
        rgb[0] = code / 100;
        rgb[1] = (code / 10) % 10;
        loaded = true;
        rgb[2] = code % 10;
        PlayerPrefs.SetInt("GillaColour", code);
        PlayerPrefs.Save();
        GillaComputer[] all = UnityEngine.Object.FindObjectsByType<GillaComputer>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            all[i].Paint();
        }
        Changed?.Invoke();
    }

    public int Code
    {
        get { return rgb[0] * 100 + rgb[1] * 10 + rgb[2]; }
    }

    public int Channel
    {
        get { return channel; }
    }

    public int Page
    {
        get { return page; }
    }

    public string Typed
    {
        get { return typed; }
    }

    public void Refresh()
    {
        Redraw(true);
    }

    private void Paint()
    {
        if (targets != null)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                ApeColor target = targets[i];
                if (target != null)
                {
                    target.SetColorCode(rgb[0], rgb[1], rgb[2]);
                }
            }
        }
    }

    private void Redraw(bool force)
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        int players = boot != null ? boot.PlayerCount : 0;
        int total = GillaOnline.Total;
        string kind = boot != null ? boot.KindLabel : "REGULAR";
        string room;
        if (boot != null)
        {
            room = boot.Joining ? "..." : boot.RoomLabel;
        }
        else
        {
            room = "OFFLINE";
        }
        string state = string.Concat(new string[]
        {
            page.ToString(), "|", typed, "|",
            rgb[0].ToString(), rgb[1].ToString(), rgb[2].ToString(), channel.ToString(), "|",
            GillaTurn.Describe(), GillaPlayerPunching.On.ToString(), "|",
            players.ToString(), "|", total.ToString(),
            NetworkBootstrap.PlayerId, kind, room, "|",
            boot != null ? boot.WaitingCode : ""
        });
        if (!force && state == shown && tex != null)
        {
            return;
        }
        shown = state;
        if (tex == null)
        {
            Texture2D t = new Texture2D(320, 240, (TextureFormat)4, true);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            t.hideFlags = (HideFlags)0x34;
            t.name = "GillaOS";
            tex = t;
            px = new Color32[76800];
        }
        for (int i = 0; i < px.Length; i++)
        {
            px[i] = new Color32(2, 2, 4, 255);
        }
        if (title == null)
        {
            title = Load(titleFont);
        }
        if (text == null)
        {
            text = Load(textFont);
        }
        Line(title, 160, 14, "GillaOS", new ScreenFont.Style { scale = 2, slant = 0.25f, colours = Silver, shadow = Shadow }, true);
        ScreenFont.Style selected = new ScreenFont.Style { bold = true, scale = 1, slant = 0f, colours = Milk };
        int y = 46;
        for (int i = 0; i < PageNames.Length; i++)
        {
            if (i == page)
            {
                Line(text, 8, y, ">", selected, false);
            }
            ScreenFont.Style style = i == page ? selected : new ScreenFont.Style { scale = 1, slant = 0f, colours = Dim };
            Line(text, 16, y, PageNames[i], style, false);
            y += 14;
        }
        Fill(76, 40, 1, 104, Divider);
        ScreenFont.Style hint = new ScreenFont.Style { bold = true, scale = 1, slant = 0.3f, colours = Orange };
        if (page == CodePage)
        {
            DrawCode(room);
        }
        else if (page == ColorPage)
        {
            DrawColour(hint);
        }
        else if (page == MusicPage)
        {
            DrawMusic(hint);
        }
        else
        {
            DrawSettings(hint);
        }
        Line(text, 160, 151, "Lobby: " + kind, new ScreenFont.Style { scale = 1, slant = 0f, colours = Lime }, true);
        string suffix;
        if (boot != null)
        {
            suffix = "";
            if (!boot.Joining)
            {
                suffix = boot.IsPublicRoom ? " (PUBLIC)" : "";
            }
        }
        else
        {
            suffix = "";
        }
        Line(text, 160, 162, "Room: " + room + suffix, new ScreenFont.Style { scale = 1, slant = 0f, colours = Lime }, true);
        Line(text, 160, 199, "Players in Room: " + players.ToString(), new ScreenFont.Style { scale = 1, slant = 0f, colours = Lime }, true);
        Line(text, 160, 208, "Online Players: " + total.ToString(), new ScreenFont.Style { scale = 1, slant = 0f, colours = Lime }, true);
        Line(text, 160, 220, NetworkBootstrap.PlayerId, new ScreenFont.Style { scale = 1, slant = 0f, colours = Grey }, true);
        int version = 39;
        Line(text, 160, 230, "v" + version.ToString() + " build " + Build, new ScreenFont.Style { scale = 1, slant = 0f, colours = Grey }, true);
        tex.SetPixels32(px);
        tex.Apply(true);
        if (screen == null)
        {
            return;
        }
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }

    private void DrawCode(string room)
    {
        Line(text, 198, 40, "CODE", new ScreenFont.Style { scale = 2, slant = 0f, along = true, colours = Heading }, true);
        ScreenFont.Style label = new ScreenFont.Style { scale = 1, slant = 0f, colours = Orange };
        ScreenFont.Style value = new ScreenFont.Style { bold = true, scale = 2, slant = 0f, colours = Milk };
        Line(text, 198, 66, "Current Room:", label, true);
        Line(text, 198, 78, room, value, true);
        Line(text, 198, 106, "Custom Room:", label, true);
        int width;
        if (typed.Length < 1)
        {
            width = 0;
        }
        else
        {
            width = Line(text, 198, 118, typed, value, true);
        }
        if (typed.Length < 8)
        {
            Fill(width / 2 + 200, 118, 8, 14, Cursor);
        }
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot != null && boot.WaitingCode != null)
        {
            ScreenFont.Style warn = new ScreenFont.Style { bold = true, scale = 1, slant = 0f, colours = Orange };
            NetworkBootstrap.Lobby kind = boot.WaitingKind;
            string first = "GO BACK TO THE BASE";
            if (kind != NetworkBootstrap.Lobby.Regular)
            {
                first = "PLAY " + NetworkBootstrap.LabelOf(kind);
            }
            Line(text, 198, 133, first, warn, true);
            Line(text, 198, 141, "TO JOIN " + boot.WaitingCode, warn, true);
        }
        int y = InRoom ? 172 : 178;
        Line(text, 160, y, "[<] Backspace", new ScreenFont.Style { scale = 1, slant = 0f, colours = Orange }, true);
        Line(text, 160, y + 9, "[ENTER] Join. Nothing typed: public", new ScreenFont.Style { scale = 1, slant = 0f, colours = Orange }, true);
        if (InRoom)
        {
            Line(text, 160, y + 18, "[OPTION 1] Disconnect", new ScreenFont.Style { scale = 1, slant = 0f, colours = Orange }, true);
        }
    }

    private void DrawColour(ScreenFont.Style hint)
    {
        Line(text, 198, 40, "COLOR", new ScreenFont.Style { scale = 2, slant = 0f, along = true, colours = Heading }, true);
        string[] names = new string[] { "R", "G", "B" };
        ScreenFont.Style selected = new ScreenFont.Style { bold = true, scale = 2, slant = 0f, colours = Milk };
        int y = 66;
        for (int i = 0; i < 3; i++)
        {
            if (i == channel)
            {
                Line(text, 110, y, ">", selected, false);
            }
            Line(text, 132, y, names[i], new ScreenFont.Style { bold = true, scale = 2, slant = 0f, colours = Channels[i] }, false);
            Line(text, 144, y, ":", new ScreenFont.Style { scale = 2, slant = 0f, colours = Channels[i] }, false);
            Line(text, 180, y, rgb[i].ToString(), selected, false);
            y += 21;
        }
        Line(text, 160, 178, "OPTION 1-3: R, G, B. 0-9 sets it", hint, true);
        Line(text, 160, 187, "ENTER for the next color", hint, true);
    }
}
