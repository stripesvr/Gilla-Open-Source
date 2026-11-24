// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusion;
using UnityEngine;

[ExecuteAlways]
public partial class GillaMapVote : MonoBehaviour
{
    [Serializable]
    public class MapEntry
    {
        public string label = "MAP";
        [Tooltip("A picture of it (readable: drawn into its panel).")]
        public Texture2D preview;
        [Tooltip("Its cover art, Lethal Ape Redux's way (a shot of it, its name big across it), full colour on its panel over where the picture is (GillaSetup.MapCover); with none, the picture's drawn on the screen.")]
        public Texture2D cover;
        [Tooltip("Where the apes start, one each in turn.")]
        public Transform[] apeSpawns = new Transform[0];
        [Tooltip("Where the monster starts.")]
        public Transform monsterSpawn;
    }

    internal class Canvas
    {
        private readonly int w;
        private readonly int h;
        private readonly Texture2D tex;
        private readonly Color32[] px;
        private readonly Renderer screen;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

        public Canvas(Renderer r, int width, int height)
        {
            screen = r;
            w = width;
            h = height;
            Texture2D t = new Texture2D(width, height, (TextureFormat)4, true);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            t.name = "GillaMapVote";
            t.hideFlags = Application.isPlaying ? HideFlags.None : (HideFlags)0x34;
            tex = t;
            px = new Color32[h * w];
        }

        public void Clear(Color32 a, Color32 b)
        {
            for (int y = 0; y < h; y++)
            {
                Color32 c = (y & 1) != 0 ? b : a;
                for (int x = 0; x < w; x++)
                {
                    px[x + y * w] = c;
                }
            }
        }

        public void Set(int x, int y, Color32 c)
        {
            if ((y | x) >= 0 && x < w && y < h)
            {
                px[x + (h - 1 - y) * w] = c;
            }
        }

        public void Fill(RectInt r, Color32 c)
        {
            for (int y = r.yMin; y < r.yMax; y++)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    Set(x, y, c);
                }
            }
        }

        public void Outline(RectInt r, Color32 c)
        {
            int xMax = r.xMax;
            int yMax = r.yMax;
            for (int x = r.xMin; x < xMax; x++)
            {
                Set(x, r.yMin, c);
                Set(x, yMax - 1, c);
            }
            for (int y = r.yMin; y < yMax; y++)
            {
                Set(r.xMin, y, c);
                Set(xMax - 1, y, c);
            }
        }

        public void Text(ScreenFont font, int x, int y, string s, Color32[] colours, int scale, bool centred = false)
        {
            if (font != null && !string.IsNullOrEmpty(s))
            {
                ScreenFont.Style style = new ScreenFont.Style { scale = scale, colours = colours };
                if (centred)
                {
                    font.DrawCentred(px, w, h, x, y, s, style);
                }
                else
                {
                    font.Draw(px, w, h, x, y, s, style);
                }
            }
        }

        public void Picture(Color32[] src, int sw, int sh, int x, int y, int scale, float bright)
        {
            float t = Mathf.Clamp01(bright);
            for (int row = 0; row < sh; row++)
            {
                int dx = x;
                for (int col = 0; col < sw; col++)
                {
                    Color32 c = src[col + (sh - 1 - row) * sw];
                    if (bright < 1f)
                    {
                        c = Color32.LerpUnclamped(Black, c, t);
                    }
                    for (int j = 0; j < scale; j++)
                    {
                        for (int i = 0; i < scale; i++)
                        {
                            Set(dx + i, y + row * scale + j, c);
                        }
                    }
                    dx += scale;
                }
            }
        }

        public Texture2D Bake()
        {
            tex.SetPixels32(px);
            tex.Apply(true);
            return tex;
        }

        public void Show()
        {
            if (screen == null)
            {
                return;
            }
            tex.SetPixels32(px);
            tex.Apply(true);
            screen.GetPropertyBlock(block);
            block.SetTexture(BaseMap, tex);
            screen.SetPropertyBlock(block);
        }
    }

    [Header("Parts (GillaSetup.MapVote builds them)")]
    public MapEntry[] maps = new GillaMapVote.MapEntry[0];
    [Tooltip("Poke one to vote for its map (the same order as the maps).")]
    public PressKey[] panels = new PressKey[0];
    public Renderer[] panelScreens = new Renderer[0];
    [Tooltip("Each panel's cover: a quad over its screen's picture (the same order).")]
    public Renderer[] panelCovers = new Renderer[0];
    [Tooltip("The board over them.")]
    public Renderer board;
    [Tooltip("Where you're put back in the lobby.")]
    public Transform lobby;
    [Tooltip("The round on the network (Gilla_ModeRound): the room's master client spawns it.")]
    public NetworkObject roundPrefab;
    [Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset textFont;
    [Tooltip("The monster the hunter plays (MonsterCatalog's name).")]
    public string monsterName = "Grinner";
    public AudioClip tickSound;
    public AudioClip startSound;
    public AudioClip apesWinSound;
    private const int PW = 256;
    private const int PH = 208;
    private const int BW = 512;
    private const int BH = 128;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Color32 Black = new Color32(6, 6, 6, 255);
    private static readonly Color32 Scan = new Color32(13, 13, 13, 255);
    private static readonly Color32 Lit = GillaBoardLook.Dots;
    private static readonly Color32 Mid = new Color32(96, 96, 96, 255);
    private static readonly Color32 Low = new Color32(40, 40, 40, 255);
    private static readonly Color32 Picked = new Color32(255, 255, 255, 255);
    private static readonly Color32 Grey = new Color32(90, 90, 90, 255);
    private static readonly Color32[] HeadText = GillaBoardLook.White;
    private static readonly Color32[] WhiteText = GillaBoardLook.Soft;
    private static readonly Color32[] PickedText = GillaBoardLook.White;
    private static readonly Color32[] DimText = GillaBoardLook.Dim;
    private Canvas boardCanvas;
    private Canvas[] panelCanvas = new GillaMapVote.Canvas[0];
    private Color32[][] pictures = new Color32[0][];
    private ScreenFont title;
    private ScreenFont text;
    private int seenRound = -1;
    private int lastTick = -1;
    private byte seenPhase = 255;
    private bool inHunt;
    private bool becameMonster;
    private bool outOfIt;
    private float nextCheck;
    private float nextDraw;
    private string drawn;
    public const string TogetherIn = "BACKROOMS";
    private static MaterialPropertyBlock coverBlock;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public static GillaMapVote Instance { get; private set; }

    public string ShownLine { get; private set; }

    public static bool OutOfHunt
    {
        get
        {
            if (!(Instance != null))
            {
                return false;
            }
            if (!Instance.inHunt)
            {
                return false;
            }
            return Instance.outOfIt;
        }
    }

    public static bool HuntingApes
    {
        get
        {
            GillaModeRound round = GillaModeRound.Current;
            if (Instance != null && Instance.inHunt && round != null)
            {
                if (round.Live && round.Phase == 1)
                {
                    if (round.Monster == round.Runner.LocalPlayer)
                    {
                        return round.Apes().Item1 > 0;
                    }
                }
            }
            return false;
        }
    }

    // NetworkBootstrap.runner and KillCam.Heal() are not accessible from here; reach them by reflection.
    private static NetworkRunner RunnerOf(NetworkBootstrap boot) /* ? */
    {
        if (boot == null)
        {
            return null;
        }
        FieldInfo f = typeof(NetworkBootstrap).GetField("runner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return f != null ? f.GetValue(boot) as NetworkRunner : null;
    }

    private static void HealOf(KillCam cam) /* ? */
    {
        if (cam == null)
        {
            return;
        }
        MethodInfo m = typeof(KillCam).GetMethod("Heal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (m != null)
        {
            m.Invoke(cam, null);
        }
    }

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void Awake()
    {

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {

        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        if (board != null)
        {
            boardCanvas = new Canvas(board, 512, 128);
        }
        panelCanvas = panelScreens.Select(r => r != null ? new Canvas(r, 256, 208) : null).ToArray();
        pictures = maps.Select(m => m.preview != null ? (m.preview.isReadable ? m.preview.GetPixels32() : null) : null).ToArray();
        drawn = null;
        Draw(null);
        if (!Application.isPlaying)
        {
            return;
        }
        foreach (PressKey key in panels)
        {
            if (key != null)
            {
                key.Pressed += OnPanel;
            }
        }
        KillCam.Died += OnDied;
        KillCam.Dying += OnDying;
    }

    private void OnDisable()
    {
        foreach (PressKey key in panels)
        {
            if (key != null)
            {
                key.Pressed -= OnPanel;
            }
        }
        KillCam.Died -= OnDied;
        KillCam.Dying -= OnDying;
        if (Application.isPlaying)
        {
            GillaCorpse.ClearAll();
            LeftMode();
        }
    }

    private void LeftMode()
    {
        if (!inHunt)
        {
            return;
        }
        inHunt = false;
        becameMonster = false;
        outOfIt = false;
        if (MonsterPlayer.IsMonster && MonsterPlayer.Local != null)
        {
            MonsterPlayer.Local.Become(0);
        }
        if (KillCam.Local != null)
        {
            if (KillCam.Local.respawnAt == lobby)
            {
                KillCam.Local.respawnAt = null;
            }
        }
        Debug.Log("[GillaMapVote] left BE THE MONSTER mid-hunt: out of it, an ape, coming back at the base");
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = boot != null ? RunnerOf(boot) /* ? */ : null;
        GillaModeRound round = null;
        if (runner != null && runner.IsRunning)
        {
            if (!boot.Joining && boot.Kind == NetworkBootstrap.Lobby.Monster)
            {
                if (runner.IsSharedModeMasterClient && boot.Settled && nextCheck <= Time.unscaledTime)
                {
                    nextCheck = Time.unscaledTime + 2f;
                    MakeRound(runner);
                }
                round = GillaModeRound.Current;
            }
            else
            {
                round = null;
            }
        }
        if (round != null)
        {
            if (!round.Live)
            {
                round = null;
            }
        }
        Follow(round);
        if (GillaCorpse.All.Count > 0)
        {
            if (round == null || round.Phase != 1)
            {
                GillaCorpse.ClearHunts();
            }
        }
        if (Time.unscaledTime < nextDraw)
        {
            return;
        }
        nextDraw = Time.unscaledTime + 0.2f;
        Draw(round);
    }

    private void MakeRound(NetworkRunner runner)
    {
        if (GillaModeRound.Current != null)
        {
            if (GillaModeRound.Current.Live)
            {
                return;
            }
        }
        if (roundPrefab == null)
        {
            Debug.LogError("[GillaMapVote] no round prefab");
            return;
        }
        runner.Spawn(roundPrefab, (Vector3?)Vector3.zero, (Quaternion?)Quaternion.identity, null, null, (NetworkSpawnFlags)2);
        Debug.Log("[GillaMapVote] made the round in '" + runner.SessionInfo.Name + "', as its master client");
    }

    public void VoteFor(int map)
    {
        GillaModeRound round = GillaModeRound.Current;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (round != null)
        {
            if (round.Live && round.Phase == 0)
            {
                if (boot != null)
                {
                    if (boot.Kind == NetworkBootstrap.Lobby.Monster)
                    {
                        round.RPC_Vote((byte)map, default(RpcInfo));
                    }
                }
            }
        }
    }

    private void OnPanel(PressKey key)
    {
        int index = Array.IndexOf(panels, key);
        GillaModeRound round = GillaModeRound.Current;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (round != null)
        {
            if (round.Live && round.Phase == 0)
            {
                if (boot != null)
                {
                    if (boot.Kind == NetworkBootstrap.Lobby.Monster)
                    {
                        round.RPC_Vote((byte)index, default(RpcInfo));
                    }
                }
            }
        }
    }

    private void Follow(GillaModeRound round)
    {
        if (round == null)
        {
            if (inHunt)
            {
                ComeBack();
                return;
            }
            return;
        }
        PlayerRef me = round.Runner.LocalPlayer;
        if (round.Phase == 1 && round.Round != seenRound)
        {
            seenRound = round.Round;
            NetworkDictionary<PlayerRef, byte> players = round.Players;
            if (players.TryGet(me, out byte role))
            {
                int number = ApeNumber(round, me);
                GoIn(round, role == 3, number);
            }
        }
        if (round.Phase == 0 && inHunt)
        {
            ComeBack();
        }
        if (inHunt && round.Phase == 1)
        {
            if (round.Monster == me)
            {
                if (MonsterPlayer.IsMonster)
                {
                    becameMonster = true;
                }
                else if (becameMonster && !outOfIt)
                {
                    outOfIt = true;
                    round.RPC_Out(default(RpcInfo));
                }
            }
        }
        if (seenPhase != round.Phase)
        {
            if (seenPhase != 255)
            {
                Announce(round);
            }
            seenPhase = round.Phase;
            if (round.Phase == 2 && inHunt)
            {
                if (!MonsterPlayer.IsMonster)
                {
                    if (KillCam.Local != null)
                    {
                        if (!KillCam.Local.Busy)
                        {
                            HealOf(KillCam.Local); /* ? */
                        }
                    }
                }
            }
        }
        int tick;
        if (round.Phase != 0 || round.VoteEnds <= 0f)
        {
            tick = -1;
        }
        else
        {
            tick = (int)(float)(int)(round.VoteEnds - round.Now) /* ? */;
        }
        if (tick != lastTick)
        {
            lastTick = tick;
            if ((uint)(tick - 1) < 5)
            {
                if (tickSound != null && board != null)
                {
                    AudioClip[] clips = new AudioClip[1];
                    clips[0] = tickSound;
                    Sfx.Play(clips, board.transform.position, 0.7f, 1f, 1f);
                }
            }
        }
    }

    public static Vector3 Beside(Vector3 spawn, int number)
    {
        if (number > 0)
        {
            float radius = Mathf.Sqrt(number) * 0.55f;
            float angle = number * 137.5f * 0.017453292f;
            Vector3 p = spawn + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (!Physics.Linecast(spawn + Vector3.up * 0.9f, p + Vector3.up * 0.9f, -16133, QueryTriggerInteraction.Ignore))
            {
                if (!Physics.CheckSphere(p + Vector3.up * 0.9f, 0.3f, -16133, QueryTriggerInteraction.Ignore))
                {
                    Vector3 floor = GillaTeleport.FloorUnder(p + Vector3.up * 1.2f, 3f);
                    if ((floor - (p + Vector3.up * 1.2f)).sqrMagnitude >= 1E-10f)
                    {
                        return floor;
                    }
                }
            }
        }
        return spawn;
    }

    private void GoIn(GillaModeRound round, bool hunter, int number)
    {
        MapEntry entry = null;
        Transform spawn = null;
        bool exact = true;
        bool noEntry = true;
        if (maps.Length != 0)
        {
            entry = maps[Mathf.Min(round.Map, maps.Length - 1)];
            if (entry != null)
            {
                noEntry = false;
                if (hunter)
                {
                    spawn = entry.monsterSpawn;
                    exact = true;
                }
                else
                {
                    bool together = entry.label == "BACKROOMS" && entry.apeSpawns.Length != 0 && entry.apeSpawns[0] != null;
                    if (together)
                    {
                        spawn = entry.apeSpawns[0];
                        exact = false;
                    }
                    else if (entry.apeSpawns.Length == 0)
                    {
                        spawn = null;
                        exact = true;
                    }
                    else
                    {
                        spawn = entry.apeSpawns[number % entry.apeSpawns.Length];
                        exact = true;
                    }
                }
            }
        }
        if (spawn == null)
        {
            Debug.LogWarning("[GillaMapVote] no spawn in map " + round.Map.ToString());
            return;
        }
        if (MonsterPlayer.IsMonster && MonsterPlayer.Local != null)
        {
            MonsterPlayer.Local.Become(0);
        }
        if (KillCam.Local != null && !KillCam.Local.Busy)
        {
            HealOf(KillCam.Local); /* ? */
        }
        Vector3 feet = exact ? spawn.position : Beside(spawn.position, number);
        GillaTeleport.To(feet, spawn.forward);
        inHunt = true;
        becameMonster = false;
        outOfIt = false;
        if (KillCam.Local != null)
        {
            KillCam.Local.respawnAt = lobby;
        }
        string label = noEntry ? "?" : entry.label;
        string what;
        if (hunter)
        {
            if (MonsterPlayer.Local != null)
            {
                int monster = MonsterNumber();
                if (monster > 0)
                {
                    MonsterPlayer.Local.Become(monster);
                }
            }
            what = "monster";
        }
        else
        {
            what = "ape " + (number + 1).ToString();
        }
        Debug.Log("[GillaMapVote] into " + label + " as the " + what);
    }

    private void ComeBack()
    {
        if (MonsterPlayer.IsMonster && MonsterPlayer.Local != null)
        {
            MonsterPlayer.Local.Become(0);
        }
        KillCam cam = KillCam.Local;
        if (cam != null)
        {
            if (cam.Busy)
            {
                return;
            }
        }
        inHunt = false;
        if (cam != null)
        {
            HealOf(KillCam.Local); /* ? */
            KillCam.Local.respawnAt = null;
        }
        if (lobby == null)
        {
            return;
        }
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        float side = -1.6f;
        NetworkRunner bootRunner = RunnerOf(boot); /* ? */
        if (boot != null && bootRunner != null)
        {
            PlayerRef me = bootRunner.LocalPlayer;
            side = (me.PlayerId % 5 - 2) * 0.8f;
        }
        Vector3 pos = lobby.position;
        Vector3 right = lobby.right;
        GillaTeleport.To(pos + side * right, lobby.forward);
    }

    private void OnDying()
    {
        GillaModeRound round = GillaModeRound.Current;
        NetworkApe ape = NetworkApe.Local;
        if (inHunt && !outOfIt)
        {
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            if (round != null)
            {
                if (round.Live && round.Phase == 1)
                {
                    if (round.Monster != round.Runner.LocalPlayer)
                    {
                        if (ape != null && boot != null)
                        {
                            if (boot.tearing == null)
                            {
                                return;
                            }
                            var copies = boot.tearing.copies;
                            if (copies == null || copies.Length == 0)
                            {
                                return;
                            }
                            ape.LeaveBody();
                        }
                    }
                }
            }
        }
    }

    private void OnDied()
    {
        if (inHunt && !outOfIt)
        {
            GillaModeRound round = GillaModeRound.Current;
            if (round != null)
            {
                if (round.Live && round.Phase == 1)
                {
                    if (round.Monster != round.Runner.LocalPlayer)
                    {
                        outOfIt = true;
                        round.RPC_Out(default(RpcInfo));
                    }
                }
            }
        }
    }

    private void Announce(GillaModeRound round)
    {
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        if (round.Phase == 1)
        {
            if (startSound != null && inHunt)
            {
                AudioClip[] clips = new AudioClip[1];
                clips[0] = startSound;
                Sfx.Play(clips, pos, 0.9f, 0.95f, 1.05f);
            }
        }
        if (round.Phase == 2 && round.Winner != 1)
        {
            if (apesWinSound != null)
            {
                AudioClip[] clips = new AudioClip[1];
                clips[0] = apesWinSound;
                if (!inHunt)
                {
                    if (board != null)
                    {
                        pos = board.transform.position;
                    }
                }
                Sfx.Play(clips, pos, 0.8f, 1f, 1f);
            }
        }
    }

    private static int ApeNumber(GillaModeRound round, PlayerRef me)
    {
        IEnumerable<KeyValuePair<PlayerRef, byte>> players = round.Players;
        List<int> ids = players
            .Where(kv => kv.Value != 3)
            .Select(kv => kv.Key.PlayerId)
            .OrderBy(i => i)
            .ToList();
        return Mathf.Max(ids.IndexOf(me.PlayerId), 0);
    }

    private int MonsterNumber()
    {
        MonsterCatalog catalog = MonsterCatalog.Get();
        if (catalog == null)
        {
            return 0;
        }
        for (int i = 0; i < catalog.monsters.Length; i++)
        {
            if (catalog.monsters[i].name == monsterName)
            {
                return i + 1;
            }
        }
        return 0;
    }
}
