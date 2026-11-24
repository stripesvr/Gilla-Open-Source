using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Fusion;
using Fusion.Photon.Realtime;
using ExitGames.Client.Photon;
using Photon.Realtime;

public class GillaOnline : MonoBehaviour
{
    public const string Property = "online";
    public const float Every = 60;
    public const float Soonest = 15;
    private const float GiveUp = 15;
    private float nextLook;
    private float lastLook = -999f;
    private bool looking;
    private NetworkRunner roomOf;
    private Photon.Realtime.LoadBalancingClient client; /* ? */
    private const float Fresh = 120;
    private static int lastCount = -1;
    private static float lastCountAt = -999f;
    private static readonly FieldInfo CloudField = typeof(NetworkRunner).GetField("_cloudServices", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BootRunnerField = typeof(NetworkBootstrap).GetField("runner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); /* ? */

    public static string LastLook { get; private set; } = "";
    public static string TellReply { get; private set; } = "";

    private static NetworkRunner BootRunner(NetworkBootstrap boot) /* ? */
    {
        if (boot == null || BootRunnerField == null)
        {
            return null;
        }
        return BootRunnerField.GetValue(boot) as NetworkRunner;
    }

    public static int Total
    {
        get
        {
            NetworkRunner runner = null;
            if (NetworkBootstrap.Instance != null)
            {
                runner = BootRunner(NetworkBootstrap.Instance); /* ? */
            }
            bool stale;
            if (lastCount < 0)
            {
                stale = true;
            }
            else
            {
                stale = 120f <= Time.unscaledTime - lastCountAt;
            }
            if (!(runner == null))
            {
                if (runner.IsRunning && runner.SessionInfo != null)
                {
                    int players = runner.SessionInfo.PlayerCount;
                    int told = Told(runner);
                    if (told >= 0)
                    {
                        lastCount = told;
                        lastCountAt = Time.unscaledTime;
                        return Mathf.Max(told, players);
                    }
                    if (stale)
                    {
                        return players;
                    }
                    if (players < lastCount)
                    {
                        return lastCount;
                    }
                    return players;
                }
            }
            return stale ? 0 : lastCount;
        }
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        lastCount = -1;
        lastCountAt = -999f;
    }

    public static int Told(NetworkRunner runner)
    {
        SessionProperty value = null;
        if (runner != null)
        {
            if (runner.SessionInfo != null && runner.SessionInfo.Properties != null
                && runner.SessionInfo.Properties.TryGetValue("online", out value)
                && value != null && value.IsInt)
            {
                return (int)value;
            }
        }
        return -1;
    }

    private void Update()
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (boot != null)
        {
            runner = BootRunner(boot); /* ? */
        }
        if (!looking)
        {
            if (!(runner == null))
            {
                if (runner.IsRunning)
                {
                    if (boot.Settled && runner.IsSharedModeMasterClient)
                    {
                        if (runner != roomOf)
                        {
                            roomOf = runner;
                            nextLook = Mathf.Min(nextLook, lastLook + 15f);
                        }
                        if (nextLook <= Time.unscaledTime)
                        {
                            nextLook = Time.unscaledTime + 60f;
                            StartCoroutine(Look(runner));
                            return;
                        }
                    }
                }
            }
        }
    }

    private static Photon.Realtime.AppSettings ToRealtimeSettings(FusionAppSettings config) /* ? */
    {
        Photon.Realtime.AppSettings s = new Photon.Realtime.AppSettings();
        s.AppIdRealtime = config.AppIdRealtime;
        s.AppVersion = config.AppVersion;
        s.FixedRegion = config.FixedRegion;
        s.Server = config.Server;
        s.Port = config.Port;
        s.UseNameServer = config.UseNameServer;
        s.Protocol = (ExitGames.Client.Photon.ConnectionProtocol)(int)config.Protocol;
        s.EnableLobbyStatistics = config.EnableLobbyStatistics;
        return s;
    }

    private static Photon.Realtime.AuthenticationValues ToRealtimeAuth(Fusion.Photon.Realtime.AuthenticationValues auth) /* ? */
    {
        if (auth == null)
        {
            return null;
        }
        Photon.Realtime.AuthenticationValues a = new Photon.Realtime.AuthenticationValues(auth.UserId);
        a.AuthType = (Photon.Realtime.CustomAuthenticationType)(int)auth.AuthType;
        a.AuthGetParameters = auth.AuthGetParameters;
        if (auth.AuthPostData != null)
        {
            a.SetAuthPostData(auth.AuthPostData as string);
        }
        return a;
    }

    private IEnumerator Look(NetworkRunner runner)
    {
        string refused = null;
        int lobbies = 0;
        int lobbyCount = 0;
        int appStats = 0;
        Photon.Realtime.LoadBalancingClient c = null; /* ? */

        looking = true;
        lastLook = Time.unscaledTime;
        float began = Time.realtimeSinceStartup;
        Fusion.Photon.Realtime.AuthenticationValues auth = null;
        if (NetworkBootstrap.ShiftAuthNeeded)
        {
            string ticket = null;
            yield return ShiftAuth.RefreshPhotonTicket((bool ok, string fresh) => { ticket = ok ? fresh : null; });
            if (string.IsNullOrEmpty(ticket))
            {
                Finish("no ShiftAuth ticket, so not this time");
                yield break;
            }
            auth = NetworkBootstrap.TicketAuth(ticket);
        }

        FusionAppSettings config = PhotonAppSettings.Global.AppSettings.GetCopy();
        Photon.Realtime.AppSettings settings = ToRealtimeSettings(config); /* ? */
        c = new Photon.Realtime.LoadBalancingClient(settings.Protocol); /* ? */
        client = c;
        c.AuthValues = ToRealtimeAuth(auth); /* ? */
        refused = null;
        appStats = -1;
        lobbies = -1;
        lobbyCount = 0;
        c.OpResponseReceived += (OperationResponse response) =>
        {
            if (response.OperationCode == 221)
            {
                if (response.ReturnCode != 0)
                {
                    refused = response.ReturnCode.ToString() + " " + response.DebugMessage;
                    return;
                }
                int[] counts = response[229] as int[];
                if (counts == null)
                {
                    lobbies = 0;
                }
                else
                {
                    int sum = 0;
                    for (int i = 0; i < counts.Length; i++)
                    {
                        sum += counts[i];
                    }
                    lobbies = sum;
                    lobbyCount = counts.Length;
                }
            }
        };
        c.EventReceived += (EventData e) =>
        {
            if (e.Code == 226)
            {
                appStats = c.PlayersInRoomsCount;
            }
        };
        if (!c.ConnectUsingSettings(settings))
        {
            client = null;
            Finish("couldn't start a connection");
            yield break;
        }

        float until = Time.realtimeSinceStartup + 15f;
        float onMaster = -1f;
        bool asked = false;

        while (lobbies < 0 && Time.realtimeSinceStartup < until && c.State != Photon.Realtime.ClientState.Disconnected)
        {
            c.Service();
            if (!asked && c.State == Photon.Realtime.ClientState.ConnectedToMasterServer)
            {
                onMaster = Time.realtimeSinceStartup - began;
                asked = c.LoadBalancingPeer.SendOperation(221, new Dictionary<byte, object>(), SendOptions.SendReliable); /* ? */
            }
            if (refused != null && appStats >= 0)
            {
                break;
            }
            yield return null;
        }

        Photon.Realtime.DisconnectCause cause = c.DisconnectedCause;
        if (c.State != Photon.Realtime.ClientState.Disconnected)
        {
            c.Disconnect();
        }
        float bye = Time.realtimeSinceStartup + 3f;

        while (c.State != Photon.Realtime.ClientState.Disconnected && Time.realtimeSinceStartup < bye)
        {
            c.Service();
            yield return null;
        }

        // Realtime 4's LoadBalancingClient has no Handler; no ConnectionHandler is created for a manually serviced client.
        c.LoadBalancingPeer.StopThread(); /* ? */
        client = null;

        int count = lobbies;
        if (count >= 0 || (count = appStats) >= 0)
        {
            lastCount = count;
            lastCountAt = Time.unscaledTime;
        }

        string timing = string.Format("{0:0.0} s", Time.realtimeSinceStartup - began);
        string masterPart = "";
        if (0f <= onMaster)
        {
            masterPart = string.Format(", on the master server at {0:0.0} s", onMaster);
        }
        timing = timing + masterPart;

        if (count < 0)
        {
            string refusedPart = "";
            if (refused != null)
            {
                refusedPart = "lobby stats refused: " + refused + ", ";
            }
            string askedPart = "asked";
            if (!asked)
            {
                askedPart = "never on the master server: " + cause.ToString();
            }
            Finish(string.Concat(new string[] { "no count (", refusedPart, askedPart, ", ", timing, ")" }));
            yield break;
        }

        string note;
        if (runner != null && runner.IsRunning && runner.IsSharedModeMasterClient && runner.SessionInfo != null
            && NetworkBootstrap.Instance /* ? */ != null && BootRunner(NetworkBootstrap.Instance) /* ? */ == runner)
        {
            if (Told(runner) == count)
            {
                note = ", as the room was told already";
            }
            else
            {
                note = Tell(runner, count) ? "" : ", not told: no way to the room's Photon client";
            }
        }
        else
        {
            note = ", not told: the room's been left or has a new master";
        }

        string lobbyText;
        if (lobbies < 0)
        {
            lobbyText = refused != null ? refused : "none";
        }
        else
        {
            lobbyText = lobbies.ToString() + " in " + lobbyCount.ToString() + " lobbies";
        }
        string appText = "none yet";
        if (appStats >= 0)
        {
            appText = appStats.ToString();
        }
        Finish(string.Format("{0} in rooms (lobby stats: {1}, app stats: {2}; {3}){4}", new object[] { count, lobbyText, appText, timing, note }));
    }

    private static bool Tell(NetworkRunner runner, int count)
    {
        Photon.Realtime.LoadBalancingClient client = RoomClient(runner); /* ? */
        if (client == null || client.CurrentRoom /* ? */ == null)
        {
            return false;
        }
        Photon.Realtime.Room room = client.CurrentRoom; /* ? */
        Action<OperationResponse> answer = null;
        answer = (OperationResponse r) =>
        {
            if (r.OperationCode != 252)
            {
                return;
            }
            client.OpResponseReceived -= answer;
            string reply = "accepted";
            if (r.ReturnCode != 0)
            {
                reply = r.ReturnCode.ToString() + " " + r.DebugMessage;
            }
            TellReply = reply;
        };
        client.OpResponseReceived += answer;
        TellReply = "sent";
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props.Add("online", count);
        if (room.SetCustomProperties(props) /* ? */)
        {
            return true;
        }
        client.OpResponseReceived -= answer;
        TellReply = "not sent";
        return false;
    }

    private static Photon.Realtime.LoadBalancingClient RoomClient(NetworkRunner runner) /* ? */
    {
        if (CloudField != null)
        {
            if (runner != null)
            {
                object cloud = CloudField.GetValue(runner);
                if (cloud != null)
                {
                    PropertyInfo communicatorProp = cloud.GetType().GetProperty("Communicator", BindingFlags.Instance | BindingFlags.Public);
                    if (communicatorProp == null)
                    {
                        return null;
                    }
                    object communicator = communicatorProp.GetValue(cloud);
                    if (communicator != null)
                    {
                        PropertyInfo clientProp = communicator.GetType().GetProperty("Client", BindingFlags.Instance | BindingFlags.Public);
                        if (clientProp == null)
                        {
                            return null;
                        }
                        return clientProp.GetValue(communicator) as Photon.Realtime.LoadBalancingClient; /* ? */
                    }
                }
            }
        }
        return null;
    }

    private void Finish(string what)
    {
        bool changed = what != LastLook;
        LastLook = what;
        looking = false;
        if (changed)
        {
            Debug.Log("[Online] " + what);
            return;
        }
    }

    private void OnDestroy()
    {
        if (client == null)
        {
            return;
        }
        if (client.IsConnected)
        {
            client.Disconnect();
        }
        // Realtime 4's LoadBalancingClient has no Handler; stop the peer's background thread instead.
        client.LoadBalancingPeer.StopThread(); /* ? */
        client = null;
    }
}
