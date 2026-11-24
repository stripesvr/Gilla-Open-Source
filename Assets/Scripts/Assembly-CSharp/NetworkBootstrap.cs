// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using Fusion;
using Fusion.Photon.Realtime;
using Photon.Realtime;
using Photon.Voice.Unity;
using Photon.Voice.Fusion;

public class NetworkBootstrap : MonoBehaviour
{
    public enum Lobby
    {
        Regular = 0,
        Monster = 1,
        Splash = 2
    }

    public NetworkObject apePrefab;
    [Tooltip("What every room's name starts with: this, the version, the kind of lobby and the code.")]
    public string roomName = "Gilla";
    public const int NetVersion = 39;
    public const int PublicCodeLength = 5;
    public const string CodeChars = "BCDFGHJKLMNPQRSTVWXZ23456789";
    public static int TestRoomSize;
    public static int FullRoomsMet;
    public const string PlayTestRooms = "Library/GillaPlayTestRooms.txt";
    [Header("Local rig (what the others see you do)")]
    public Transform head;
    [Tooltip("The collision-corrected hand followers, so your hands stop on walls for the others too.")]
    public Transform leftHand;
    [Tooltip("The collision-corrected hand followers, so your hands stop on walls for the others too.")]
    public Transform rightHand;
    public ApeColor localColor;
    public MicLevel mic;
    [Tooltip("Your ape's finger posers, left then right: what your fingers are doing.")]
    public HandPoser fingersL;
    [Tooltip("Your ape's finger posers, left then right: what your fingers are doing.")]
    public HandPoser fingersR;
    [Tooltip("Tears your own apes: which parts you have lost.")]
    public DismemberGroup tearing;
    [Tooltip("Sends your microphone to the room's voice chat. It owns the mic while you are in a room (the Quest lets one thing record at a time), so the mouth's MicLevel follows its loudness then.")]
    public Recorder voice;
    private const string KindProperty = "kind";
    public const float SettleSeconds = 5;
    private float joinedAt;
    private const string PlayerIdKey = "GillaPlayerId";
    private static string playerId;
    private NetworkRunner runner;
    private string joining;
    private string queued;
    private Lobby queuedKind;
    private bool waitingOnAuth;
    private string pendingCode;
    private int fullTries;
    private const int MostFullTries = 5;
    private static readonly System.Random pick = new System.Random();
    private bool micAllowed;

    // Anti-cheat removed for the Redux test build: no auth server, so never needed.
    public static bool ShiftAuthNeeded
    {
        get { return false; }
    }

    public static string KindCode(Lobby kind)
    {
        if (kind == Lobby.Monster) return "MONSTER";
        if (kind == Lobby.Splash) return "SPLASH";
        return "";
    }

    public static Lobby KindOf(string code)
    {
        if (code == "MONSTER") return Lobby.Monster;
        if (code == "SPLASH") return Lobby.Splash;
        return Lobby.Regular;
    }

    public static string NewPublicCode()
    {
        char[] chars = new char[PublicCodeLength];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = CodeChars[pick.Next(CodeChars.Length)];
        }
        return new string(chars);
    }

    public static string RoomTag { get { return ""; } }

    private static bool InPlayTest { get { return false; } }

    public static NetworkBootstrap Instance { get; private set; }

    public string RoomLabel { get; private set; } = "OFFLINE";

    public bool IsPublicRoom
    {
        get
        {
            if (runner != null && runner.IsRunning)
            {
                SessionInfo info = runner.SessionInfo;
                if (info != null) return info.IsVisible;
            }
            return false;
        }
    }

    public Lobby Kind { get; private set; }

    public string KindLabel { get { return LabelOf(Kind); } }

    public static string LabelOf(Lobby kind)
    {
        if (kind == Lobby.Monster) return "BE THE MONSTER";
        if (kind == Lobby.Splash) return "DON'T SPLASH";
        return "REGULAR";
    }

    public Lobby Going { get { return queued != null ? queuedKind : Kind; } }

    public string WaitingCode { get; private set; }

    public Lobby WaitingKind { get; private set; }

    public string TakeWaiting(Lobby kind)
    {
        string code = WaitingCode;
        string result = "";
        if (code != null && WaitingKind == kind)
        {
            WaitingCode = null;
            result = code;
        }
        return result;
    }

    private Lobby RoomKind(Lobby otherwise)
    {
        if (runner != null)
        {
            SessionInfo info = runner.SessionInfo;
            if (info != null && info.Properties != null && info.Properties.TryGetValue(KindProperty, out SessionProperty prop))
            {
                if (prop.IsInt)
                {
                    otherwise = (Lobby)(int)prop;
                }
            }
        }
        return otherwise;
    }

    public bool Joining { get; private set; }

    public NetworkRunner Runner { get { return runner; } }

    public int PlayerCount
    {
        get
        {
            if (runner != null && runner.IsRunning)
            {
                SessionInfo info = runner.SessionInfo;
                if (info != null) return info.PlayerCount;
            }
            return 0;
        }
    }

    public bool Settled
    {
        get
        {
            if (!Joining && runner != null && runner.IsRunning)
            {
                return Time.unscaledTime - joinedAt >= SettleSeconds;
            }
            return false;
        }
    }

    public static string PlayerId
    {
        get
        {
            if (playerId == null)
            {
                if (PlayerPrefs.HasKey(PlayerIdKey))
                {
                    playerId = PlayerPrefs.GetString(PlayerIdKey);
                    return playerId;
                }
                if (!Application.isPlaying)
                {
                    return "0000000000000000";
                }
                Guid guid = Guid.NewGuid();
                playerId = BitConverter.ToString(guid.ToByteArray(), 0, 8).Replace("-", "");
                PlayerPrefs.SetString(PlayerIdKey, playerId);
                PlayerPrefs.Save();
            }
            return playerId;
        }
    }

    public static void SetPlayerId(string id)
    {
        if (!string.IsNullOrEmpty(id) && id != playerId)
        {
            playerId = id;
            PlayerPrefs.SetString(PlayerIdKey, id);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {

        Instance = this;
        ShiftAuth.OnSessionLost += OnAuthLost;
        if (GetComponent<GillaOnline>() == null)
        {
            gameObject.AddComponent<GillaOnline>();
        }
    }

    private void OnDestroy()
    {
        ShiftAuth.OnSessionLost -= OnAuthLost;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnAuthLost()
    {
        Debug.LogWarning("[Network] auth session lost, leaving room");
        RoomLabel = "OFFLINE";
        if (runner == null) return;
        if (runner.IsRunning)
        {
            runner.Shutdown(true, ShutdownReason.Ok, false);
            if (voice != null && mic != null)
            {
                mic.Restart();
            }
        }
    }

    private void Start()
    {
        micAllowed = Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO");
        if (!micAllowed)
        {
            Permission.RequestUserPermission("android.permission.RECORD_AUDIO");
        }
        if (tearing != null)
        {
            Dismemberment[] copies = tearing.copies;
            if (copies != null)
            {
                for (int i = 0; i < copies.Length; i++)
                {
                    Dismemberment copy = copies[i];
                    if (copy != null && copy.parts != null)
                    {
                        if (copy.GetComponent<CosmeticWearer>() == null)
                        {
                            CosmeticWearer wearer = copy.gameObject.AddComponent<CosmeticWearer>();
                            Dismemberment.Part[] parts = copy.parts;
                            bool noHead = !Array.Exists(parts, p => p.id == 0 && p.renderer != null); /* ? */
                            // wearer.isLocal = true; /* ? */ CosmeticWearer has no 'isLocal' member
                            // wearer.noHead = noHead; /* ? */ CosmeticWearer has no 'noHead' member
                        }
                    }
                }
            }
        }
        if (ShiftAuth.HasDevKey)
        {
            ShiftAuth.Begin(null);
        }
        if (GillaIntegrity.Cleared)
        {
            WaitingCode = null;
            JoinLobby(Going, "");
            return;
        }
        StartCoroutine(JoinOnceCleared());
    }

    private IEnumerator JoinOnceCleared()
    {
        yield return GillaIntegrity.WaitForVerdict();
        if (GillaIntegrity.Cleared)
        {
            WaitingCode = null;
            JoinLobby(Going, "");
        }
    }

    private IEnumerator JoinAfterAuth()
    {
        while (!ShiftAuth.IsLoggedIn)
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }
        waitingOnAuth = false;
        JoinLobby(Kind, pendingCode);
    }

    private IEnumerator FetchTicket(TaskCompletionSource<string> ticket)
    {
        yield return ShiftAuth.RefreshPhotonTicket((ok, fresh) => ticket.TrySetResult(ok ? fresh : ShiftAuth.PhotonTicket));
    }

    public static Fusion.Photon.Realtime.AuthenticationValues TicketAuth(string ticket)
    {
        Fusion.Photon.Realtime.AuthenticationValues auth = new Fusion.Photon.Realtime.AuthenticationValues();
        auth.AuthType = Fusion.Photon.Realtime.CustomAuthenticationType.Custom;
        auth.AddAuthParameter("token", ticket ?? "");
        auth.AddAuthParameter("appId", "gilla");
        auth.AddAuthParameter("sessionId", ShiftAuth.SessionId ?? "");
        return auth;
    }

    private void Update()
    {
        if (!micAllowed && Permission.HasUserAuthorizedPermission("android.permission.RECORD_AUDIO"))
        {
            micAllowed = true;
            if (voice != null)
            {
                voice.RestartRecording();
            }
        }
        if (voice == null) return;
        if (mic == null) return;
        if (!mic.externalOnly) return;
        float level;
        if (voice.LevelMeter == null)
        {
            level = 0f;
        }
        else
        {
            level = voice.LevelMeter.CurrentAvgAmp; /* ? */
        }
        if (level <= 1e-07f) level = 1e-07f;
        float log = Mathf.Log10(level);
        float minDb = mic.minDb;
        float result = 0f;
        if (minDb != mic.maxDb)
        {
            float t = (log * 20f - minDb) / (mic.maxDb - minDb);
            if (t >= 0f)
            {
                result = 1f;
                if (t <= 1f) result = t;
            }
        }
        mic.ExternalLevel = result;
    }

    public void JoinRoom(string code)
    {
        WaitingCode = null;
        JoinLobby(Going, code);
    }

    public async void Disconnect()
    {
        if (Joining || runner == null || !runner.IsRunning) return;
        WaitingCode = null;
        Joining = true;
        await runner.Shutdown(true, ShutdownReason.Ok, false);
        if (this == null) return;
        string code = queued;
        Joining = false;
        if (code == null)
        {
            RoomLabel = "NOT CONNECTED";
            if (mic != null && voice != null)
            {
                mic.Restart();
            }
            Debug.Log("[Network] disconnected: in no room till you join one or leave the computer's corner");
        }
        else
        {
            queued = null;
            JoinLobby(queuedKind, code);
        }
    }

    public async void JoinLobby(Lobby kind, string code)
    {
        string prefix = null;
        if (code == null) code = "";
        if (GillaCloud.Banned)
        {
            Debug.LogWarning("[Network] not joining: this account is banned");
            return;
        }
        if (!GillaIntegrity.Cleared)
        {
            Debug.LogWarning(string.Format("[Network] not joining: the start-up checks haven't cleared this build ({0} {1})", GillaIntegrity.Result, GillaIntegrity.Codes));
            return;
        }
        if (ShiftAuthNeeded && !ShiftAuth.IsLoggedIn)
        {
            pendingCode = code;
            Kind = kind;
            if (!waitingOnAuth)
            {
                waitingOnAuth = true;
                Debug.Log("[Network] waiting on auth");
                StartCoroutine(JoinAfterAuth());
            }
            return;
        }
        if (Joining)
        {
            queued = (code == joining && kind == Kind) ? null : code;
            queuedKind = kind;
            return;
        }
        joining = code;
        Joining = true;
        string cameFrom;
        if (kind == Kind && runner != null)
        {
            cameFrom = "";
            if (runner.IsRunning && !IsPublicRoom)
            {
                cameFrom = RoomLabel;
            }
        }
        else
        {
            cameFrom = "";
        }
        Kind = kind;
        bool goingPublic = code.Length == 0;
        string kindCode = KindCode(kind);
        prefix = string.Concat(new string[] { roomName, RoomTag, "-v", NetVersion.ToString(), "-" });

        if (runner != null)
        {
            await runner.Shutdown(true, ShutdownReason.Ok, false);
            if (this == null) return;
        }

        Fusion.Photon.Realtime.AuthenticationValues auth = null;
        if (ShiftAuthNeeded)
        {
            if (ShiftAuth.HasDevKey)
            {
                SetPlayerId(ShiftAuth.PlayerId);
            }
            TaskCompletionSource<string> ticket = new TaskCompletionSource<string>();
            StartCoroutine(FetchTicket(ticket));
            string fresh = await ticket.Task;
            if (this == null) return;
            auth = TicketAuth(fresh);
        }

        GameObject runnerObject = new GameObject("Runner " + prefix + code);
        runnerObject.transform.SetParent(transform, false);
        runner = runnerObject.AddComponent<NetworkRunner>();
        bool voiced = false;
        if (voice != null)
        {
            FusionVoiceClient voiceClient = runnerObject.AddComponent<FusionVoiceClient>();
            voiceClient.PrimaryRecorder = voice;
            if (mic != null)
            {
                mic.externalOnly = true;
            }
            voiced = true;
        }
        NetworkRunner startRunner = runner;
        Dictionary<string, SessionProperty> properties = new Dictionary<string, SessionProperty>();
        properties.Add(KindProperty, (SessionProperty)(int)kind);
        StartGameArgs args = new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = goingPublic ? null : prefix + code,
            SessionNameGenerator = goingPublic ? new Func<string>(() => prefix + NewPublicCode()) : null,
            CustomLobbyName = prefix + (kindCode.Length > 0 ? kindCode + "-" : "") + "lobby",
            SessionProperties = properties,
            MatchmakingMode = new Fusion.Photon.Realtime.MatchmakingMode?((Fusion.Photon.Realtime.MatchmakingMode)0),
            IsVisible = new bool?(goingPublic),
            PlayerCount = TestRoomSize < 1 ? (int?)null : new int?(TestRoomSize),
            AuthValues = auth,
            SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>(),
            ObjectProvider = runnerObject.AddComponent<NetworkObjectProviderDefault>()
        };
        StartGameResult result = await startRunner.StartGame(args);
        if (this == null) return;

        string next = queued;
        Joining = false;
        if (next != null)
        {
            queued = null;
            JoinLobby(queuedKind, next);
            return;
        }
        if (result.Ok)
        {
            fullTries = 0;
            Lobby roomKind = RoomKind(kind);
            if (!goingPublic && roomKind != kind)
            {
                Debug.Log(string.Concat(new string[] { "[Network] '", code, "' is a room of ", LabelOf(roomKind), ", and you're in ", LabelOf(kind) }));
                WaitingCode = code;
                WaitingKind = roomKind;
                JoinLobby(kind, cameFrom);
            }
            else
            {
                joinedAt = Time.unscaledTime;
                string name = runner.SessionInfo.Name;
                string label = name;
                if (name.StartsWith(prefix))
                {
                    label = name.Substring(prefix.Length);
                }
                RoomLabel = label;
                runner.Spawn(apePrefab, new Vector3?(Vector3.zero), new Quaternion?(Quaternion.identity), new PlayerRef?(runner.LocalPlayer), null, (NetworkSpawnFlags)0);
                string region = runner.SessionInfo.Region;
                int playerNumber = runner.LocalPlayer.PlayerId;
                string line = string.Format("[Network] joined '{0}' (region {1}) as player {2}, ", name, region, playerNumber);
                int count = runner.SessionInfo.PlayerCount;
                Debug.Log(line + string.Format("{0} in the room", count));
            }
        }
        else
        {
            int reason = (int)result.ShutdownReason;
            Debug.LogWarning(string.Format("[Network] could not join room '{0}{1}': {2} {3}", new object[] { prefix, code, result.ShutdownReason, result.ErrorMessage }));
            if ((reason - 5U < 2 || reason == 10) && goingPublic && fullTries < MostFullTries)
            {
                fullTries++;
                if (reason == 10)
                {
                    FullRoomsMet++;
                }
                JoinLobby(kind, "");
            }
            else
            {
                fullTries = 0;
                RoomLabel = reason == 10 ? "ROOM FULL" : "OFFLINE";
                if (mic != null && voiced)
                {
                    mic.Restart();
                }
            }
        }
    }
}
