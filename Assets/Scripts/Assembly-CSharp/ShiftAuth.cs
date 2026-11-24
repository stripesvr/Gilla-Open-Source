// Anti-cheat removed for the Gilla Redux test build.
// The original ShiftAuth (about 1,500 lines) logged in to the original game's auth server,
// attested the device with a hardware key, scanned for hooks and mod loaders, locked out and
// closed the game on a "security violation", verified other players' proofs and reported kicks.
// None of that runs here. This file keeps only the names the rest of the game refers to.
// The original is backed up in D:\GillaUnity\removed\anticheat.
using System;
using System.Collections;

public static class ShiftAuth
{
    public enum FailKind
    {
        None = 0,
        Network = 1,
        VersionMismatch = 2,
        SecurityViolation = 3,
        Banned = 4,
        AlreadyIn = 5,
        Slow = 6
    }

    public struct Outcome
    {
        public bool Success;
        public string Error;
        public FailKind Kind;
    }

    public const string AppId = "gilla";
    public const string Build = "BETA";
    public const string DevKeyPref = "Gilla.ShiftAuth.DevKey";

    public static bool IsLoggedIn;
    public static bool IsOffline = true;
    public static string PlayerId;
    public static string Username;
    public static string MetaUserId;
    public static string SessionId;
    public static string PhotonTicket;
    public static string MetaSig;
    public static string AssetKey;
    public static string PlayFabTicket;

#pragma warning disable 0067
    public static event Action OnReady;
    public static event Action OnSessionLost;
#pragma warning restore 0067

    public static bool HasDevKey { get { return false; } }
    public static string DevKey { get { return ""; } }
    public static bool Trusted { get { return true; } }

    public static void RecheckTrust() { }

    public static void Begin(Action<Outcome> done)
    {
        if (done != null) done(new Outcome { Success = true, Error = null, Kind = FailKind.None });
    }

    public static void Begin(string metaUserId, string displayName, Action<Outcome> done)
    {
        MetaUserId = metaUserId;
        Username = displayName;
        Begin(done);
    }

    public static void Logout() { }

    public static IEnumerator RefreshPhotonTicket(Action<bool, string> done)
    {
        if (done != null) done(false, null);
        yield break;
    }

    public static IEnumerator VerifyPeer(string pfid, string sid, string sig, Action<bool> done)
    {
        if (done != null) done(true);
        yield break;
    }

    public static IEnumerator ReportKick(string targetPlayFabId, string targetName, string reason, string details, string roomName)
    {
        yield break;
    }
}
