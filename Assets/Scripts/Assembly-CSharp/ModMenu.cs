// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Fusion;
using GorillaLocomotion;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(60)]
public partial class ModMenu : MonoBehaviour
{
    public enum Kind
    {
        Owner = 0,
        Size = 1,
        OP = 2,
        SizeAndOP = 3,
        Punch = 4
    }

    private class Item
    {
        public string label;
        public Func<bool> on;
        public Func<string> value;
        public Action press;
        public bool repeats;
    }

    private class Page
    {
        public string title;
        public readonly List<Item> items = new List<Item>();
    }

    private class Hit
    {
        public RectTransform rt;
        public Image bg;
        public Text label;
        public Vector2 size;
        public Vector2 basePos;
        public Action act;
        public float pressT;
        public float hoverT;
        public bool hovered;
    }

    private class Row : Hit
    {
        public Item item;
        public Text value;
        public Text arrow;
        public Image track;
        public Image knob;
        public Image glow;
        public Image[] box;
        public Color offCol;
        public Color onCol;
        public float knobT;
    }

    private enum Place
    {
        Held = 0,
        Palm = 1,
        PalmFlipped = 2,
        BackOfHand = 3,
        BesideHand = 4
    }

    private class Marker
    {
        public LineRenderer box;
        public TextMesh label;
        public CharacterController cc;
    }

    private static Kind making;
    private bool leaving;
    private bool wasOff;
    [Header("Which menu")]
    public Kind kind;
    [Tooltip("A shop menu's stand: its board stands here (its face out along this transform's -Z), up while you're near.")]
    public Transform stand;
    [Tooltip("A shop menu's board: how wide it is (m), and how near your head must be for it to be up (m).")]
    public float standWidth = 0.46f;
    [Tooltip("A shop menu's board: how wide it is (m), and how near your head must be for it to be up (m).")]
    public float standReach = 2.4f;
    [Header("Menu")]
    [Tooltip("Your right index fingertip, this far (m) out from the controller along where it points (when the ape's own isn't to be had).")]
    public float fingerLength = 0.07f;
    [Tooltip("A press counts this far (m) in front of a row's face, and this far behind it.")]
    public float pressReach = 0.022f;
    [Tooltip("A press counts this far (m) in front of a row's face, and this far behind it.")]
    public float pressThrough = 0.03f;
    [Tooltip("Seconds before another row can be pressed.")]
    public float pressCooldown = 0.22f;
    [Tooltip("Played on a press (empty: borrowed from one of the map's own buttons).")]
    public AudioClip click;
    [Header("Mods")]
    [Tooltip("Speed boost: the jump multiplier and the cap on launch speed are multiplied by these.")]
    public float boostMultiplier;
    [Header("Mods")]
    [Tooltip("Speed boost: the jump multiplier and the cap on launch speed are multiplied by these.")]
    public float boostMaxJump;
    [Tooltip("Super jump: the same, multiplied again.")]
    public float superJump;
    [Tooltip("Super jump: the same, multiplied again.")]
    public float superMaxJump;
    [Tooltip("Low gravity: this much of the gravity there was.")]
    public float lowGravity = 0.35f;
    [Tooltip("Long arms: your hands reach this much further (m), and the most an arm can stretch grows by this much.")]
    public float longArmReach = 0.28f;
    [Tooltip("Long arms: your hands reach this much further (m), and the most an arm can stretch grows by this much.")]
    public float longArmStretch = 0.6f;
    [Tooltip("Platforms: how wide each one is (m).")]
    public float platformSize = 0.32f;
    [Tooltip("Spawned monsters appear this far away (m), in front of you if there's room.")]
    public float spawnMin = 2.5f;
    [Tooltip("Spawned monsters appear this far away (m), in front of you if there's room.")]
    public float spawnMax = 5f;
    [Tooltip("Fullbright: the ambient light everything gets.")]
    public Color fullbrightAmbient = new Color(0.85f, 0.85f, 0.85f, 1f);
    public Color monsterColour = new Color(1f, 0.18f, 0.12f, 1f);
    public Color playerColour = new Color(0.25f, 0.85f, 1f, 1f);
    private const int PerPage = 7;
    private const float BaseW = 640f;
    private const float Pad = 22f;
    private const float HeaderH = 104f;
    private const float TabsH = 78f;
    private const float DotsH = 30f;
    private const float RowH = 78f;
    private const float RowGap = 8f;
    private const float FootH = 58f;
    private float W = 640f;
    private static readonly float FullH = 849f;
    private float H = FullH;
    private const float SimpleTitleH = 150f;
    private const float SimpleWhereH = 50f;
    private const float SimpleRowH = 118f;
    private const float SimpleRowGap = 20f;
    private const float SimpleFootH = 60f;
    private const float OpW = 1080f;
    private const float OpColGap = 24f;
    private static readonly Color OpLitBand = new Color(0.34f, 0.06f, 0.12f, 1f);
    public const float OpPulse = 1.6f;
    public static readonly Color OpGlow = new Color(0.86f, 0.06f, 0.13f, 1f);
    public static readonly Color OpGlow2 = new Color(0.95f, 0.32f, 0.62f, 1f);
    private static readonly Color SimpleOff = new Color(0.078431375f, 0.078431375f, 0.078431375f, 1f);
    private static readonly Color SimpleOn;
    private static readonly Color SimpleBox = new Color(0.47058824f, 0.47058824f, 0.47058824f, 1f);
    private static readonly Color SimpleBoxOn;
    private static readonly Color SimpleWhere = new Color(0.74509805f, 0.74509805f, 0.74509805f, 1f);
    private ModMenu host;
    private bool extra;
    private static ModMenu makingHost;
    private readonly List<Item> simpleItems = new List<ModMenu.Item>();
    private bool opLook;
    private bool showSize;
    private bool swapping;
    private Outline titleRim;
    private Image frameDots;
    private Image wash;
    private Sprite dotsSprite;
    private Hit swapKey;
    private Sprite backSprite;
    private Font titleFont;
    private Font bodyFont;
    private Sprite square;
    private Color Accent = new Color(1f, 1f, 1f, 1f);
    private Color AccentHot = new Color(1f, 1f, 1f, 1f);
    private static readonly Color KnobOff = new Color(0.43137255f, 0.43137255f, 0.43137255f, 1f);
    private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.55f);
    private static readonly Color DotOff = new Color(1f, 1f, 1f, 0.16f);
    private static readonly Quaternion AimFromGrip = Quaternion.Euler(60f, 0f, 0f);
    private const int SpawnMask = -1797;
    private readonly List<Page> pages = new List<ModMenu.Page>();
    private readonly List<Row> rows = new List<ModMenu.Row>();
    private readonly List<Image> dots = new List<Image>();
    private Hit prev;
    private Hit next;
    private int page;
    private Canvas canvas;
    private Transform root;
    private Text pageText;
    private Text countText;
    private Text footText;
    private Transform cursor;
    private Font font;
    private Sprite rounded;
    private Sprite circle;
    private Sprite soft;
    private Material unlitMat;
    private AudioSource sound;
    private bool shown;
    private bool closing;
    private float anim;
    private float closeAt;
    private float slide;
    private int slideDir = 1;
    private Hit lastTouched;
    private float nextPress;
    private string toast = "";
    private float toastUntil;
    private Transform apeTip;
    private Player gt;
    private Rigidbody body;
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private static readonly string[] PlaceNames = new[] { "HELD", "PALM", "PALM (FLIPPED)", "BACK OF HAND", "BESIDE HAND" };
    private Place place;
    private float sizeM;
    private float upM;
    private float outM;
    private float turnDeg;
    private float flySpeed;
    private const string Prefs = "Gilla.ModMenu.";
    private const float FitM = 0.26f;
    private const float OpFit = 1.15f;
    public static readonly float[] SoldSizes;
    private static readonly ValueTuple<string, float>[] OwnerSizes = new ValueTuple<string, float>[] { ("TINY", 0.1f), ("MINI", 0.25f), ("SMALL", 0.5f), ("SHORT", 0.75f), ("NORMAL", 1f), ("BIG", 1.5f), ("GIANT", 2f) };
    private const float PunchW = 700f;
    private Renderer[] hiddenToy;
    public const float HeldOut = 0.055f;
    public const float HeldAlong = 0.055f;
    public const float HeldUp = 0.25f;
    private static readonly List<ModMenu> rowBoards = new List<ModMenu>();
    private const float RepeatAfter = 0.3f;
    private const float RepeatEvery = 0.07f;
    private float nextRepeat;
    private Hit clicked;
    private bool fly;
    private bool fastFly;
    private bool speed;
    private bool superJumpOn;
    private bool longArms;
    private bool hover;
    private bool noclip;
    private bool floating;
    private bool savedUseGravity;
    private float baseJump;
    private float baseMaxJump;
    private float savedArmLength;
    private Vector3 savedLeftOffset;
    private Vector3 savedRightOffset;
    private LayerMask savedLayers;
    private readonly List<Collider> offColliders = new List<Collider>();
    private bool hadLongArms;
    private bool hadNoclip;
    private const float CeilingFar = 40f;
    private int gravityMode;
    private int gravityNow;
    private Vector3 savedGravity;
    private Vector3? checkpoint;
    private int playerCursor;
    private bool platforms;
    private Material platformMat;
    private Transform platL;
    private Transform platR;
    private readonly List<GameObject> dropped = new List<GameObject>();
    private const int ItemsAtMost = 150;
    private readonly Dictionary<Animator, float> frozenAnims = new Dictionary<Animator, float>();
    private bool fullbright;
    private AmbientMode savedAmbientMode;
    private Color savedSky;
    private Color savedEquator;
    private Color savedGround;
    private bool savedFog;
    private Light savedSun;
    private Light lamp;
    private readonly List<GillaAtmosphere> stilled = new List<GillaAtmosphere>();
    private bool monsterEsp;
    private bool playerEsp;
    private bool tracers;
    private Material espMat;
    private readonly Dictionary<Object, Marker> markers = new Dictionary<Object, ModMenu.Marker>();
    private readonly Dictionary<Object, LineRenderer> lines = new Dictionary<Object, LineRenderer>();
    private readonly List<Object> gone = new List<Object>();
    private readonly HashSet<Object> seen = new HashSet<Object>();
    private readonly Vector3[] corners = new Vector3[16];

    private bool Simple { get { return kind != Kind.Owner; } }

    public bool IsExtraBoard { get { return extra; } }

    private bool OwnersSide
    {
        get
        {
            if (extra)
            {
                if (host != null)
                {
                    return host.kind == Kind.Owner;
                }
            }
            return false;
        }
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)0)]
    private static void Boot()
    {
        MonsterBrain.Frozen = false;
        MonsterBrain.IgnoreLocal = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachIfOwner();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachIfOwner();
    }

    public static void AttachIfOwner()
    {
        Player player = Player.Instance != null ? Player.Instance : Object.FindAnyObjectByType<Player>();
        if (player == null) return;

        Kind? menu = ModMenuAccess.Menu;
        ModMenu[] comps = player.GetComponents<ModMenu>();
        ModMenu x = null;
        for (int i = 0; i < comps.Length; i++)
        {
            ModMenu m = comps[i];
            if (!m.leaving && !m.extra)
            {
                if (menu.HasValue && m.kind == menu.Value && x == null)
                {
                    x = m;
                    continue;
                }
                m.Leave();
            }
        }
        if (x == null && menu.HasValue)
        {
            making = menu.Value;
            makingHost = null;
            x = player.gameObject.AddComponent<ModMenu>();
        }

        Kind[] sides = new Kind[] { Kind.Size, Kind.OP, Kind.Punch } /* ? */;
        for (int s = 0; s < sides.Length; s++)
        {
            Kind k = sides[s];
            bool want;
            if (x != null && x.kind != k)
            {
                want = ModMenuAccess.SideBoard(k);
            }
            else
            {
                want = false;
            }
            ModMenu[] all = player.GetComponents<ModMenu>();
            bool found = false;
            for (int i = 0; i < all.Length; i++)
            {
                ModMenu m = all[i];
                if (!m.leaving && m.extra && m.kind == k)
                {
                    if (want && !found && m.host == x)
                    {
                        found = true;
                        continue;
                    }
                    m.Leave();
                }
            }
            if (!found && want)
            {
                making = k;
                makingHost = x;
                player.gameObject.AddComponent<ModMenu>();
                makingHost = null;
            }
        }
    }

    private void Leave()
    {
        leaving = true;
        if (!extra)
        {
            AllOff();
            NormalSize();
        }
        else
        {
            Hide();
            if (kind == Kind.Punch)
            {
                MonsterPlayer.MenuPunches = false;
            }
            else if (!OwnersSide && host != null)
            {
                if (kind == Kind.OP)
                {
                    host.OpModsOff();
                }
                else if (kind == Kind.Size)
                {
                    NormalSize();
                }
            }
        }
        Destroy(this);
    }

    private static void NormalSize()
    {
        if (MonsterPlayer.Local != null)
        {
            if (!Mathf.Approximately(MonsterPlayer.ApeSize, 1f))
            {
                MonsterPlayer.Local.SetApeSize(1f, false);
            }
        }
    }

    private static Color Grey(int v)
    {
        float f = v / 255f;
        return new Color(f, f, f, 1f);
    }

    private void LoadPlacement()
    {
        int hold = PlayerPrefs.GetInt("Gilla.ModMenu.Hold", 0);
        place = (Place)(hold < 0 ? 0 : Mathf.Min(hold, PlaceNames.Length - 1));
        sizeM = PlayerPrefs.GetFloat("Gilla.ModMenu.Fit", 0.26f);
        upM = PlayerPrefs.GetFloat("Gilla.ModMenu.Up", 0.03f);
        outM = PlayerPrefs.GetFloat("Gilla.ModMenu.Out", 0.05f);
        turnDeg = PlayerPrefs.GetFloat("Gilla.ModMenu.Turn", 0f);
        flySpeed = PlayerPrefs.GetFloat("Gilla.ModMenu.FlySpeed", 9f);
    }

    private void SavePlacement()
    {
        PlayerPrefs.SetInt("Gilla.ModMenu.Hold", (int)place);
        PlayerPrefs.SetFloat("Gilla.ModMenu.Fit", sizeM);
        PlayerPrefs.SetFloat("Gilla.ModMenu.Up", upM);
        PlayerPrefs.SetFloat("Gilla.ModMenu.Out", outM);
        PlayerPrefs.SetFloat("Gilla.ModMenu.Turn", turnDeg);
        PlayerPrefs.SetFloat("Gilla.ModMenu.FlySpeed", flySpeed);
        PlayerPrefs.Save();
    }

    private void Awake()
    {

        host = makingHost;
        kind = making;
        extra = host != null;
        bool ok;
        if (extra)
        {
            ok = ModMenuAccess.SideBoard(kind);
        }
        else
        {
            Kind? menu = ModMenuAccess.Menu;
            ok = menu.HasValue && kind == menu.Value;
        }
        if (ok)
        {
            SetLook();
            gt = GetComponent<Player>();
            body = GetComponent<Rigidbody>();
            spawnPos = transform.position;
            spawnRot = transform.rotation;
            LoadPlacement();
            return;
        }
        Destroy(this);
    }

    private void SetLook()
    {
        float f = GillaBoardLook.Solid.r / 255f;
        AccentHot = new Color(1f, 1f, 1f, 1f);
        Accent = new Color(f, f, f, 1f);
    }

    private void Start()
    {
        if (gt == null)
        {
            if (stand != null)
            {
                gt = Player.Instance != null ? Player.Instance : Object.FindAnyObjectByType<Player>();
                if (gt != null)
                {
                    body = gt.GetComponent<Rigidbody>();
                    spawnPos = gt.transform.position;
                    spawnRot = gt.transform.rotation;
                }
            }
        }
        if (gt == null)
        {
            Destroy(this);
            return;
        }
        baseJump = gt.jumpMultiplier;
        baseMaxJump = gt.maxJumpSpeed;
        MonsterPlayer.RigChanging += RigChanging;
        MonsterPlayer.RigChanged += RigChanged;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Font title = Resources.Load<Font>("Fonts/Gilla_PixelOperator8Bold");
        Font bodyF = Resources.Load<Font>("Fonts/Gilla_PixelOperatorBold");
        if (!(title != null))
        {
            title = font;
        }
        titleFont = title;
        Font.textureRebuilt += PointLetters;
        if (!(bodyF != null))
        {
            bodyF = font;
        }
        bodyFont = bodyF;
        showSize = PlayerPrefs.GetInt("Gilla.ModMenu.ShowSize", 0) == 1;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.name = "ModMenu";
            unlitMat = mat;
        }
        BuildPages();
        if (pages.Count == 0)
        {
            AddPage("MOVEMENT");
        }
        BuildBoard();
        GameObject go = new GameObject("ModMenu Click");
        sound = go.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.spatialBlend = 0f;
        if (click == null)
        {
            click = BorrowClick();
        }
        apeTip = BorrowFingertip();
        ShowPage();
        root.gameObject.SetActive(false);
        cursor.gameObject.SetActive(false);
        if (stand == null)
        {
            Debug.Log("[ModMenu] ready: hold Y (left controller) to open" + (Application.isEditor ? ", or M in the editor" : ""));
        }
    }

    private static AudioClip BorrowClick()
    {
        GillaModeButton mode = Object.FindAnyObjectByType<GillaModeButton>((FindObjectsInactive)1);
        if (mode != null)
        {
            AudioSource[] srcs = mode.GetComponentsInChildren<AudioSource>(true) /* ? */;
            if (srcs != null)
            {
                for (int i = 0; i < srcs.Length; i++)
                {
                    if (srcs[i] != null && srcs[i].clip != null) return srcs[i].clip;
                }
            }
        }
        GillaDoorButton door = Object.FindAnyObjectByType<GillaDoorButton>((FindObjectsInactive)1);
        if (door != null)
        {
            AudioSource[] srcs = door.GetComponentsInChildren<AudioSource>(true) /* ? */;
            if (srcs != null)
            {
                for (int i = 0; i < srcs.Length; i++)
                {
                    if (srcs[i] != null && srcs[i].clip != null) return srcs[i].clip;
                }
            }
        }
        return null;
    }

    private static Transform BorrowFingertip()
    {
        PressKey key = Object.FindAnyObjectByType<PressKey>((FindObjectsInactive)1);
        if (key != null)
        {
            Transform[] tips = key.fingertips /* ? */;
            if (tips != null && tips.Length >= 2 && tips[1] != null)
            {
                return tips[1];
            }
        }
        CosmeticTerminal terminal = Object.FindAnyObjectByType<CosmeticTerminal>((FindObjectsInactive)1);
        if (!(terminal != null)) return null;
        Transform[] t2 = terminal.fingertips /* ? */;
        if (t2 == null || t2.Length < 2) return null;
        if (!(t2[1] != null)) return null;
        return t2[1];
    }

    private void OnDisable()
    {
        if (!extra)
        {
            AllOff();
            return;
        }
        if (root != null)
        {
            shown = false;
            root.gameObject.SetActive(false);
        }
        if (!(cursor != null)) return;
        cursor.gameObject.SetActive(false);
    }

    private void Hide()
    {
        if (root != null)
        {
            shown = false;
            root.gameObject.SetActive(false);
        }
        if (!(cursor != null)) return;
        cursor.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        Font.textureRebuilt -= PointLetters;
        MonsterPlayer.RigChanging -= RigChanging;
        MonsterPlayer.RigChanged -= RigChanged;
        if (!extra)
        {
            AllOff();
        }
        else
        {
            Hide();
        }
        if (root != null) Destroy(root.gameObject);
        if (cursor != null) Destroy(cursor.gameObject);
        if (sound != null) Destroy(sound.gameObject);
        if (unlitMat != null) Destroy(unlitMat);
        if (espMat != null) Destroy(espMat);
        if (platformMat != null) Destroy(platformMat);
        Sprite[] sprites = new Sprite[] { rounded, circle, soft, square, backSprite, dotsSprite };
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite s = sprites[i];
            if (s != null)
            {
                Destroy(s.texture);
                Destroy(s);
            }
        }
    }

    private IEnumerator Fireworks()
    {
        Toast("FIREWORKS");
        Vector3 from = Head().position;
        for (int i = 0; i <= 2; i++)
        {
            float yaw = UnityEngine.Random.Range(0f, 360f);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            float dist = UnityEngine.Random.Range(1f, 2.5f);
            Vector3 start = from + dir * dist - Vector3.up;
            float up = UnityEngine.Random.Range(4f, 6f);
            Vector3 top = start + Vector3.up * up;
            GameObject trail = Emitter(start, 70f, 0.05f, 0.45f);
            float t = 0f;
            while (trail != null)
            {
                t += Time.deltaTime / 0.7f;
                float u = 1f - t;
                float k = Mathf.Clamp01(1f - u * u);
                trail.transform.position = start + (top - start) * k;
                yield return null;
                if (!(t < 1f)) break;
            }
            if (trail != null)
            {
                trail.GetComponent<ParticleSystem>().Stop(true, (ParticleSystemStopBehavior)1);
            }
            Burst(top, 220, 7f, 0.07f, 1.9f, 0.35f);
            if (click != null && sound != null)
            {
                sound.PlayOneShot(click);
            }
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerable<Hit> Pressable()
    {
        if (prev != null) yield return prev;
        if (next != null) yield return next;
        if (swapKey != null) yield return swapKey;
        foreach (Row r in rows)
        {
            if (r.item != null) yield return r;
        }
    }

    private void BuildPages_b__147_70(MonsterCatalog.Entry e)
    {
        Vector3 feet;
        Quaternion facing;
        if (Spot(e.prefab, out feet, out facing))
        {
            NetworkBootstrap x = NetworkBootstrap.Instance;
            NetworkRunner runner = null;
            if (x != null) runner = x.GetComponent<NetworkRunner>() /* ? */;
            if (runner != null && runner.IsRunning)
            {
                if (x.Joining)
                {
                    toast = "STILL JOINING - TRY AGAIN";
                    toastUntil = Time.unscaledTime + 2.2f;
                    return;
                }
                NetworkObject no;
                if (e.prefab.TryGetComponent<NetworkObject>(out no))
                {
                    NetworkSpawnFlags flags = runner.IsSharedModeMasterClient ? (NetworkSpawnFlags)2 : (NetworkSpawnFlags)4;
                    NetworkObject spawned = runner.Spawn(no, new Vector3?(feet), new Quaternion?(facing), null, null, flags);
                    if (spawned == null)
                    {
                        Debug.LogError("[ModMenu] the network didn't take " + e.prefab.name);
                        return;
                    }
                    Toast(Label(e) + " SPAWNED");
                    return;
                }
                Debug.LogError("[ModMenu] " + e.prefab.name + " has no NetworkObject");
                return;
            }
            GameObject go = Object.Instantiate<GameObject>(e.prefab, feet, facing);
            go.name = e.prefab.name;
            toast = Label(e) + " SPAWNED";
        }
        else
        {
            toast = "NO ROOM FOR IT HERE";
        }
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void BuildPages_b__147_71(int index, MonsterCatalog.Entry e)
    {
        BecomeMonster(index, Label(e));
    }

    private static bool BuildPages_b__147_27() { return MonsterBrain.Frozen; }
    private static bool BuildPages_b__147_28() { return MonsterBrain.IgnoreLocal; }
    private static void BuildPages_b__147_29(bool on) { MonsterBrain.IgnoreLocal = on; }
    private static bool BuildPages_b__147_37() { return MonsterPlayer.OwnerPunches; }
    private static void BuildPages_b__147_38(bool on) { MonsterPlayer.OwnerPunches = on; }

    private static string BuildPages_b__147_39()
    {
        if (GillaDummy.All.Count > 0)
        {
            return GillaDummy.All.Count.ToString();
        }
        return "";
    }

    private static string BuildPages_b__147_41() { return Coins.Text; }
    private static string BuildPages_b__147_43() { return Coins.Text; }
    private static string BuildPages_b__147_45() { return Coins.BoltsText; }
    private static string BuildPages_b__147_47() { return Coins.BoltsText; }
    private static string BuildPages_b__147_49() { return "LEVEL " + GillaLevel.Level.ToString(); }
    private static string BuildPages_b__147_51() { return "LEVEL " + GillaLevel.Level.ToString(); }

    private static string BuildPages_b__147_53()
    {
        return Net != null ? Net.RoomLabel : "";
    }

    private static bool PunchItems_b__154_0() { return MonsterPlayer.MenuPunches; }
    private static void PunchItems_b__154_1(bool on) { MonsterPlayer.MenuPunches = on; }

    private static bool SizeItems_b__0(float size)
    {
        return Mathf.Abs(MonsterPlayer.ApeSize - size) < 0.001f;
    }

    private void SizeItems_b__1(float size, string label, bool sold)
    {
        if (MonsterPlayer.Local == null) return;
        string text;
        if (MonsterPlayer.IsMonster && !Owners)
        {
            text = "NOT WHILE YOU'RE A MONSTER";
        }
        else if (MonsterPlayer.Local.SetApeSize(size, true))
        {
            text = "NORMAL SIZE";
            if (size != 1f)
            {
                if (!sold)
                {
                    text = label + "  " + size.ToString("0.##") + "X";
                }
                else
                {
                    text = size.ToString("0.##", CultureInfo.InvariantCulture) + "X SIZE";
                }
            }
        }
        else
        {
            text = "NO ROOM FOR THAT HERE";
        }
        Toast(text);
    }

    private void Toggle_b__0(string label, Func<bool> on, Action<bool> set)
    {
        set(!on());
        string name = Clean(label).ToUpperInvariant();
        bool flying = name == "FLY" || name == "FAST FLY";
        string state = on() ? " ON" : " OFF";
        string hint;
        if (flying & on())
        {
            hint = " - HOLD THE RIGHT TRIGGER";
        }
        else if (on() && name == "PLATFORMS")
        {
            hint = " - HOLD A GRIP";
        }
        else
        {
            hint = "";
            if (on())
            {
                hint = name == "PUNCHING" ? " - MAKE A FIST" : "";
            }
        }
        Toast(name + state + hint);
    }

    private static void MakeSimpleRow_b__0(Row r)
    {
        if (r.item == null) return;
        r.item.press();
    }

    private static void MakeRow_b__0(Row r)
    {
        if (r.item == null) return;
        r.item.press();
    }
}
