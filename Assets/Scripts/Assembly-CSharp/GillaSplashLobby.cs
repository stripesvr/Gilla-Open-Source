// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

[ExecuteAlways]
public partial class GillaSplashLobby : MonoBehaviour
{
    [Serializable]
    public class MapEntry
    {
        public string label = "MAP";
        [Tooltip("Its plan (readable: drawn into its panel).")]
        public Texture2D preview;
        [Tooltip("Its cover art, Lethal Ape Redux's way (a shot of it, its name big across it), full colour on its panel over where the picture is (GillaSetup.MapCover); with none, the picture's drawn on the screen.")]
        public Texture2D cover;
    }

    [Header("Parts (GillaSetup.Splash builds them)")]
    public MapEntry[] maps = new GillaSplashLobby.MapEntry[0];
    [Tooltip("Poke one to vote for its map (the same order as the maps).")]
    public PressKey[] panels = new PressKey[0];
    public Renderer[] panelScreens = new Renderer[0];
    [Tooltip("Each panel's cover: a quad over its screen's picture (the same order).")]
    public Renderer[] panelCovers = new Renderer[0];
    [Tooltip("The lobby's board over the panels.")]
    public Renderer board;
    [Tooltip("The boards of the docks, each by its gate, and the finish room, in the course's order: a level starts at its own and ends at the next.")]
    public Renderer[] roomBoards = new Renderer[0];
    [Tooltip("Where you're put in the lobby (the yard pod's PLAY puts you there too).")]
    public Transform lobby;
    [Tooltip("Each level's dock, where its rounds start.")]
    public Transform[] starts = new Transform[0];
    [Tooltip("Each level's end, in the world: the next dock, the last level's the finish room. Your head in it, you're through.")]
    public Bounds[] finishes = new Bounds[0];
    [Tooltip("The gate across each dock's way out onto its level.")]
    public GillaSplashGate[] gates = new GillaSplashGate[0];
    [Tooltip("The game on the network (Gilla_SplashGame): the room's master client spawns it.")]
    public NetworkObject gamePrefab;
    [Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset textFont;
    public AudioClip tickSound;
    public AudioClip goSound;
    public AudioClip throughSound;
    public AudioClip outSound;
    public AudioClip winSound;
    [Tooltip("The dark rounds (GillaDarkness): its shader (here, so a build has it), and what you hear as the lights go.")]
    public Shader darkShader;
    public AudioClip lightsOutSound;
    private const int PW = 256;
    private const int PH = 208;
    private const int BW = 512;
    private const int BH = 128;
    private static readonly Color32 Black = new Color32(6, 6, 6, 255);
    private static readonly Color32 Scan = new Color32(13, 13, 13, 255);
    private static readonly Color32 Lit = GillaBoardLook.Dots;
    private static readonly Color32 Mid = new Color32(96, 96, 96, 255);
    private static readonly Color32 Low = new Color32(40, 40, 40, 255);
    private static readonly Color32 Picked = new Color32(255, 255, 255, 255);
    private static readonly Color32[] HeadText = GillaBoardLook.White;
    private static readonly Color32[] WhiteText = GillaBoardLook.Soft;
    private static readonly Color32[] PickedText = GillaBoardLook.White;
    private static readonly Color32[] DimText = GillaBoardLook.Dim;
    private GillaMapVote.Canvas boardCanvas;
    private GillaMapVote.Canvas[] roomCanvas = new GillaMapVote.Canvas[0];
    private GillaMapVote.Canvas[] panelCanvas = new GillaMapVote.Canvas[0];
    private Color32[][] pictures = new Color32[0][];
    private ScreenFont title;
    private ScreenFont text;
    private int seenStarts = -1;
    private int lastTick = -1;
    private byte seenPhase = 255;
    private bool inGame;
    private bool through;
    private bool wantStart;
    private bool wantBack;
    private GillaDarkness dark;
    private float nextCheck;
    private float nextDraw;
    private string drawn;

    public static GillaSplashLobby Current { get; private set; }

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void OnEnable()
    {

        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        boardCanvas = board != null ? new GillaMapVote.Canvas(board, 512, 128) : null;
        roomCanvas = roomBoards.Select(r => r != null ? new GillaMapVote.Canvas(r, 512, 128) : null).ToArray();
        panelCanvas = panelScreens.Select(r => r != null ? new GillaMapVote.Canvas(r, 256, 208) : null).ToArray();
        pictures = maps.Select(m => m.preview != null && m.preview.isReadable ? m.preview.GetPixels32() : null).ToArray();
        drawn = null;
        Draw(null);
        if (Application.isPlaying)
        {
            Current = this;
            for (int i = 0; i < panels.Length; i++)
            {
                PressKey key = panels[i];
                if (key != null)
                {
                    key.Pressed += OnPanel;
                }
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            PressKey key = panels[i];
            if (key != null)
            {
                key.Pressed -= OnPanel;
            }
        }
        if (Current == this)
        {
            Current = null;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (boot != null)
        {
            runner = NetworkRunner.Instances.FirstOrDefault() /* ? */;
        }
        GillaSplashGame game = null;
        if (runner != null && runner.IsRunning)
        {
            if (!boot.Joining && boot.Kind == NetworkBootstrap.Lobby.Splash)
            {
                if (runner.IsSharedModeMasterClient && boot.Settled && nextCheck <= Time.unscaledTime)
                {
                    nextCheck = Time.unscaledTime + 2f;
                    MakeGame(runner);
                }
                game = GillaSplashGame.Current;
            }
            else
            {
                game = null;
            }
        }
        if (game != null && !game.Live)
        {
            game = null;
        }
        Follow(game, boot);
        for (int i = 0; i < gates.Length; i++)
        {
            if (gates[i] != null)
            {
                GillaSplashGate gate = gates[i];
                bool open = game != null && game.Phase == 2 && i == game.Level;
                gate.Set(open);
            }
        }
        if (dark == null)
        {
            GameObject go = new GameObject("Darkness");
            dark = go.AddComponent<GillaDarkness>();
            dark.transform.SetParent(transform, false);
            dark.shader = darkShader;
            dark.sound = lightsOutSound;
        }
        if (game != null && inGame && (game.Phase == 1 || game.Phase == 2 || game.Phase == 3))
        {
            dark.Level = GillaSplashGame.Dimness(game.Round);
            dark.Lamp = game.Dark;
        }
        else
        {
            dark.Level = 0f;
            dark.Lamp = false;
        }
        if (Time.unscaledTime < nextDraw)
        {
            return;
        }
        nextDraw = Time.unscaledTime + 0.2f;
        Draw(game);
    }

    private void MakeGame(NetworkRunner runner)
    {
        if (GillaSplashGame.Current != null && GillaSplashGame.Current.Live)
        {
            return;
        }
        if (gamePrefab == null)
        {
            Debug.LogError("[GillaSplashLobby] no game prefab");
            return;
        }
        runner.Spawn(gamePrefab, Vector3.zero, Quaternion.identity, null, null, (NetworkSpawnFlags)2);
        Debug.Log("[GillaSplashLobby] made the game in '" + runner.SessionInfo.Name + "', as its master client");
    }

    public void VoteFor(int map)
    {
        GillaSplashGame game = GillaSplashGame.Current;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (game == null) return;
        if (game.Live && game.Phase == 0)
        {
            if (boot != null && boot.Kind == NetworkBootstrap.Lobby.Splash)
            {
                game.RPC_Vote((byte)map);
            }
        }
    }

    private void OnPanel(PressKey key)
    {
        int map = Array.IndexOf(panels, key);
        GillaSplashGame game = GillaSplashGame.Current;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (game == null) return;
        if (game.Live && game.Phase == 0)
        {
            if (boot != null && boot.Kind == NetworkBootstrap.Lobby.Splash)
            {
                game.RPC_Vote((byte)map);
            }
        }
    }

    private void Follow(GillaSplashGame game, NetworkBootstrap boot)
    {
        if (wantBack)
        {
            if (KillCam.Local == null || !KillCam.Local.Busy)
            {
                Back();
            }
        }
        if (game == null)
        {
            if (inGame && boot != null && boot.Kind == NetworkBootstrap.Lobby.Splash)
            {
                Back();
            }
            inGame = false;
            return;
        }
        PlayerRef me = game.Runner.LocalPlayer;
        byte state;
        bool has = game.Players.TryGet(me, out state);
        bool tryStart;
        if (game.Starts == seenStarts)
        {
            tryStart = wantStart;
        }
        else
        {
            bool fresh = seenStarts < 0 ? game.Phase == 1 : true;
            seenStarts = game.Starts;
            bool want = false;
            if (has && fresh && state == 1)
            {
                want = game.Phase == 1;
            }
            wantStart = want;
            tryStart = want;
        }
        if (tryStart && (KillCam.Local == null || !KillCam.Local.Busy))
        {
            wantStart = false;
            if (has && state == 1 && game.Phase != 0 && game.Phase != 4)
            {
                GoToStart(game, me);
            }
        }

        bool running = inGame && game.Phase == 2;
        if (running && has && state == 1 && !through)
        {
            if (Camera.main != null && game.Level < finishes.Length)
            {
                int level = game.Level;
                if (finishes[level].Contains(Camera.main.transform.position))
                {
                    if (KillCam.Local == null || !KillCam.Local.Busy)
                    {
                        through = true;
                        game.RPC_Finished();
                        Play(throughSound, 0.8f);
                    }
                }
            }
        }

        bool mine = inGame && has;
        if (mine && state == 3)
        {
            inGame = false;
            Play(outSound, 0.8f);
            Back();
            mine = inGame && has;
        }
        if (mine && state == 4)
        {
            if (KillCam.Local == null || !KillCam.Local.Busy)
            {
                inGame = false;
                through = false;
                Play(outSound, 0.8f);
            }
        }
        if (inGame && game.Phase == 0)
        {
            inGame = false;
            Back();
        }
        if (seenPhase != game.Phase)
        {
            if (seenPhase != 255 && inGame)
            {
                if (game.Phase == 2)
                {
                    Play(goSound, 0.9f);
                }
                if (game.Phase == 4 && has && state != 3)
                {
                    Play(winSound, 0.9f);
                }
            }
            seenPhase = game.Phase;
        }
        int tick = -1;
        if (game.Phase == 0 && game.VoteEnds > 0f)
        {
            float ends = game.VoteEnds;
            if (ends > 0f)
            {
                tick = (int)(ends - game.Now);
            }
        }
        else if (game.Phase == 1)
        {
            float ends = game.PhaseEnds;
            if (ends > 0f)
            {
                tick = (int)(ends - game.Now);
            }
        }
        if (tick != lastTick)
        {
            lastTick = tick;
            if (tick >= 1 && tick <= 5 && tickSound != null)
            {
                Play(tickSound, 0.7f);
            }
        }
    }

    private void GoToStart(GillaSplashGame game, PlayerRef me)
    {
        Transform start = game.Level < starts.Length ? starts[game.Level] : null;
        if (start == null)
        {
            return;
        }
        if (KillCam.Local != null && KillCam.Local.Busy)
        {
            return;
        }
        List<int> ids = game.Players
            .Where(kv => kv.Value == 1)
            .Select(kv => kv.Key.PlayerId)
            .OrderBy(i => i)
            .ToList();
        int index = Mathf.Max(ids.IndexOf(me.PlayerId), 0);
        int row = index / 5;
        int col = index % 5;
        Vector3 feet = start.position + start.right * ((col - 2) * 1.1f) - start.forward * (row * 1.4f);
        GillaTeleport.To(feet, start.forward);
        if (KillCam.Local != null)
        {
            KillCam.Local.SendMessage("Heal", SendMessageOptions.DontRequireReceiver) /* ? */;
        }
        wantBack = false;
        inGame = true;
        through = false;
        Debug.Log(string.Format("[GillaSplashLobby] to the start for round {0}", game.Round));
    }

    private void Back()
    {
        through = false;
        if (lobby == null)
        {
            return;
        }
        if (KillCam.Local != null)
        {
            wantBack = KillCam.Local.Busy;
            if (wantBack)
            {
                return;
            }
        }
        else
        {
            wantBack = false;
        }
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        float side = -1.6f;
        if (boot != null)
        {
            NetworkRunner runner = NetworkRunner.Instances.FirstOrDefault() /* ? */;
            if (runner != null)
            {
                PlayerRef me = runner.LocalPlayer;
                side = (me.PlayerId % 5 - 2) * 0.8f;
            }
        }
        Vector3 feet = lobby.position + lobby.right * side;
        GillaTeleport.To(feet, lobby.forward);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }
        Component from = Camera.main != null ? (Component)Camera.main : this;
        Vector3 at = from.transform.position;
        Sfx.Play(new AudioClip[] { clip }, at, volume, 1f, 1f);
    }

    private string Winners(GillaSplashGame game)
    {
        List<string> names = game.Players
            .Where(kv => kv.Value != 3 && game.Runner.ActivePlayers.Contains(kv.Key))
            .Select(kv =>
            {
                NetworkApe ape = NetworkApe.Find(kv.Key);
                if (ape != null && ape != null && !string.IsNullOrEmpty(ape.PlayerName))
                {
                    return ape.PlayerName.ToUpperInvariant();
                }
                return "PLAYER " + kv.Key.PlayerId.ToString();
            })
            .ToList();
        int count = names.Count;
        string result = "NO ONE";
        if (count != 0)
        {
            if (count == 1)
            {
                result = names[0];
            }
            else
            {
                result = count.ToString() + " PLAYERS";
            }
        }
        return result;
    }
}
