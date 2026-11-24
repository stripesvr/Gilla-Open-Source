// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.SharedModels;
using Fusion;

public partial class GillaCloud : MonoBehaviour
{
    [Serializable]
    public class Reply
    {
        public string ticket;
        public string player;
        public string refused;
        public int coins;
        public int bolts;
        public int xp;
        public int day;
        public int dailyGiven;
        public long nextDaily;
        public string[] owned;
        public int[] claimed;
        public bool banned;   // Redux: only set in answers to admin look-ups
    }

    // Redux: the title comes from Assets/Resources/PlayFabSharedSettings.asset, not the
    // original game's title, and nothing talks to the original sign-in server.
    public static string TitleId { get { return PlayFabSettings.staticSettings.TitleId; } }
    private const int SignInTimeout = 20;
    private const string QueueKey = "Gilla.Cloud.Queue";
    private const string ProfileKey = "Gilla.Cloud.ProfileUnsaved";
    private const float SendEvery = 3;
    private const float SendRetry = 20;
    private const float ProfileEvery = 5;
    private const float SignInFirstRetry = 30;
    private const float SignInMostRetry = 600;
    private const float SignInGiveUpAfter = 60;
    private static GillaCloud instance;
    private string metaId;
    private Action<Action<string>> askProof;
    private readonly List<string> queue = new List<string>();
    private int sendingCount;
    private bool signingIn;
    private bool attempting;
    private bool profileUnsaved;
    private bool applying;
    private float nextSend;
    private float nextProfile;
    private float nextState;
    private float nextPing;
    private float nextSync;
    private const float StateEvery = 1800;

    public static bool SignedIn { get; private set; }
    public static bool Banned { get; private set; }
    public static string Problem { get; private set; } = "";
    public static int ReportsSent { get; private set; }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        instance = null;
        SignedIn = false;
        Banned = false;
        Problem = "";
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)1)]
    private static void Boot()
    {
        // Redux: also in the editor and on PC (the original skipped the editor), but only once
        // a PlayFab title is set.
        if (string.IsNullOrEmpty(PlayFabSettings.staticSettings.TitleId))
        {
            Debug.LogWarning("[GillaCloud] no PlayFab Title ID set (Assets/Resources/PlayFabSharedSettings): staying offline");
            return;
        }
        Make();
        Coins.Infinite = false;
    }

    // Redux: with no Meta account (PC, editor) sign in with this device's ID after a moment.
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (string.IsNullOrEmpty(metaId))
        {
            BeginSignIn();
        }
    }

    private static void Make()
    {
        GameObject go = new GameObject("GillaCloud");
        UnityEngine.Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<GillaCloud>();
        string saved = PlayerPrefs.GetString("Gilla.Cloud.Queue", "");
        string[] parts = saved.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string op = parts[i];
            if (op.Length > 2)
            {
                instance.queue.Add(op);
            }
        }
    }

    public static void MetaUser(string id, Action<Action<string>> proof)
    {
        if (instance == null) return;
        instance.metaId = id;
        instance.askProof = proof;
        instance.BeginSignIn();
    }

    private void BeginShiftAuth()
    {
        ShiftAuth.PlayFabTicket = PlayFabSettings.staticPlayer.ClientSessionTicket;
        if (NetworkBootstrap.ShiftAuthNeeded && !ShiftAuth.HasDevKey && !Banned)
        {
            ShiftAuth.Begin(metaId, GillaName.Current, BeginShiftAuth_b__41_0);
        }
    }

    public static void Earned(int bolts)
    {
        Enqueue("E:" + bolts.ToString());
    }

    public static void SpentBolts(string item)
    {
        Enqueue("S:" + item);
    }

    public static void RedeemPurchases(string sku)
    {
        if (instance != null && !string.IsNullOrEmpty(sku))
        {
            if (instance.queue.Contains("P:" + sku)) return;
            Enqueue("P:" + sku);
        }
    }

    public static void OwnerGave(int coins, int bolts, int xp)
    {
        Enqueue(string.Concat(new string[] { "G:", coins.ToString(), ":", bolts.ToString(), ":", xp.ToString() }));
    }

    public static void BoughtBundle(string name)
    {
        Enqueue("U:" + name);
    }

    public static void Bought(List<string> names)
    {
        Enqueue("B:" + string.Join(",", names));
    }

    public static void Reported(string player, string comment)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(comment);
        Enqueue("R:" + player + ":" + Convert.ToBase64String(bytes));
    }

    public static void QuestClaimed(int day, int quest)
    {
        Enqueue("Q:" + day.ToString() + ":" + quest.ToString());
    }

    public static void ProfileChanged()
    {
        if (instance == null) return;
        if (!instance.applying)
        {
            instance.profileUnsaved = true;
            instance.nextProfile = Time.unscaledTime + 5f;
            PlayerPrefs.SetInt("Gilla.Cloud.ProfileUnsaved", 1);
            PlayerPrefs.Save();
        }
    }

    // Redux: buy a former real-money add-on with coins; the server checks the price and balance.
    public static void BuyAddon(string sku, Action<bool> done)
    {
        if (instance == null || !SignedIn)
        {
            if (done != null) done(false);
            return;
        }
        Dictionary<string, object> args = new Dictionary<string, object>();
        args["sku"] = sku;
        instance.Call("BuyAddon", args,
            reply =>
            {
                bool ok = string.IsNullOrEmpty(reply.refused);
                if (!ok) Debug.LogWarning("[GillaCloud] the server refused " + sku + ": " + reply.refused);
                instance.Adopt(reply);
                if (done != null) done(ok);
            },
            why =>
            {
                Debug.LogWarning("[GillaCloud] couldn't buy " + sku + ": " + why);
                if (done != null) done(false);
            });
    }

    // Redux: an owner's admin action on another player's account. The server checks that the
    // caller is an owner before doing anything; the answer is the target's account.
    public static void Admin(string action, string target, Dictionary<string, object> extra, Action<Reply> done, Action<string> failed)
    {
        if (instance == null || !SignedIn)
        {
            if (failed != null) failed("not signed in");
            return;
        }
        Dictionary<string, object> args = extra != null ? new Dictionary<string, object>(extra) : new Dictionary<string, object>();
        args["action"] = action;
        args["target"] = target;
        instance.Call("Admin", args, done, failed);
    }

    // Redux: ask for the account right now (used after changes made elsewhere).
    public static void SyncNow()
    {
        if (instance != null) instance.nextSync = 0f;
    }

    private static void Enqueue(string op)
    {
        if (instance == null) return;
        List<string> q = instance.queue;
        int last = 0;
        int add = 0;
        if (op[0] == 'E' && q.Count > instance.sendingCount && q[q.Count - 1][0] == 'E'
            && int.TryParse(q[q.Count - 1].Substring(2), out last)
            && int.TryParse(op.Substring(2), out add))
        {
            int sum = add + last;
            q[q.Count - 1] = "E:" + sum.ToString();
        }
        else
        {
            q.Add(op);
        }
        instance.SaveQueue();
    }

    private void SaveQueue()
    {
        PlayerPrefs.SetString("Gilla.Cloud.Queue", string.Join("|", queue));
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (!SignedIn) return;
        // Redux: heartbeat for the admin site's "online now" list, every 20 seconds.
        if (nextPing <= Time.unscaledTime)
        {
            nextPing = Time.unscaledTime + 20f;
            Dictionary<string, object> who = new Dictionary<string, object>();
            who["name"] = GillaName.Current;
            who["room"] = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.RoomLabel : "";
            // The heartbeat's answer is the account as the server has it now, so anything
            // changed on the admin site (coins, bolts, XP, roles) arrives within 20 seconds.
            Call("Ping", who, Adopt, _ => { });
        }
        // Redux: read the account every 5 seconds, so coins, bolts, XP, roles and bans changed
        // on the admin site or by an owner in game take effect almost at once. A banned
        // account's call is refused by PlayFab, which is how a ban reaches a player mid-game.
        if (nextSync <= Time.unscaledTime && sendingCount == 0)
        {
            nextSync = Time.unscaledTime + 5f;
            Call("Sync", null, Adopt, _ => { });
        }
        if (sendingCount == 0)
        {
            if (queue.Count > 0 && nextSend <= Time.unscaledTime)
            {
                SendNext();
            }
            if (sendingCount == 0 && queue.Count == 0 && nextState <= Time.unscaledTime)
            {
                nextState = Time.unscaledTime + 1800f;
                AskForState();
            }
        }
        if (profileUnsaved && nextProfile <= Time.unscaledTime)
        {
            SaveProfile();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && SignedIn && profileUnsaved)
        {
            SaveProfile();
        }
    }

    private void BeginSignIn()
    {
        if (!SignedIn && !signingIn && !Banned)
        {
            StartCoroutine(SignIn());
        }
    }

    private IEnumerator SignIn()
    {
        signingIn = true;
        float wait = 30f;
        while (!SignedIn && !Banned)
        {
            attempting = true;
            float giveUp = Time.unscaledTime + 60f;
            Attempt();
            while (attempting && Time.unscaledTime < giveUp)
            {
                yield return null;
            }
            if (SignedIn || Banned) break;
            Debug.LogWarning(string.Format("[GillaCloud] couldn't sign in ({0}): trying again in {1:0} s", Problem, wait));
            yield return new WaitForSecondsRealtime(wait);
            wait = Mathf.Min(wait + wait, 600f);
        }
        signingIn = false;
    }

    // Redux: log in to PlayFab directly. The original posted the Meta ID and a Meta nonce to
    // signin.gillavr.com, which answered with a session ticket for the original title.
    private void Attempt()
    {
        LoginWithCustomIDRequest request = new LoginWithCustomIDRequest();
        request.CustomId = string.IsNullOrEmpty(metaId) ? "device-" + SystemInfo.deviceUniqueIdentifier : "meta-" + metaId;
        request.CreateAccount = true;
        PlayFabClientAPI.LoginWithCustomID(request,
            result => Arrive(result.PlayFabId),
            error =>
            {
                if (error.Error == PlayFabErrorCode.AccountBanned)
                {
                    BeBanned();
                    return;
                }
                Failed(error.GenerateErrorReport());
            },
            null, null);
    }

    private void Failed(string why)
    {
        Problem = why;
        attempting = false;
    }

    private void BeBanned()
    {
        Banned = true;
        attempting = false;
        PlayFabClientAPI.ForgetAllCredentials();
        Debug.LogWarning("[GillaCloud] this account is banned");
        ModMenuAccess.Adopt(null);
        NameTitles.Adopt(null);
        NetworkRunner runner = null;
        if (NetworkBootstrap.Instance != null)
        {
            runner = NetworkBootstrap.Instance.GetComponent<NetworkRunner>(); /* ? */
        }
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown(true, 0, false);
        }
    }

    private void Arrive(string player)
    {
        attempting = false;
        SignedIn = true;
        Problem = "";
        NetworkBootstrap.SetPlayerId(player);
        BeginShiftAuth();
        Debug.Log("[GillaCloud] signed in as " + player);
        nextState = Time.unscaledTime + 1800f;
        AskForState();
        if (!Application.isEditor)
        {
            GillaPurchases.RedeemOwned();
        }
        LoadProfile();
        UpdateUserTitleDisplayNameRequest request = new UpdateUserTitleDisplayNameRequest();
        request.DisplayName = GillaName.Current;
        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            _ => { },
            error => Debug.LogWarning("[GillaCloud] couldn't save your name: " + error.GenerateErrorReport()),
            null, null);
    }

    private void SessionLost()
    {
        SignedIn = false;
        PlayFabClientAPI.ForgetAllCredentials();
        BeginSignIn();
    }

    private void AskForState()
    {
        Call("GetState", null, Adopt, why => Debug.LogWarning("[GillaCloud] couldn't read your account: " + why));
    }

    private void SendReport(string op)
    {
        string[] parts = op.Split(':');
        byte[] bytes = Convert.FromBase64String(parts[2]);
        string comment = Encoding.UTF8.GetString(bytes);
        sendingCount = 1;
        ReportPlayerClientRequest request = new ReportPlayerClientRequest();
        request.ReporteeId = parts[1];
        request.Comment = comment;
        PlayFabClientAPI.ReportPlayer(request, SendReport_b__69_0, SendReport_b__69_1, null, null);
    }

    private void SendNext()
    {
        string op = queue[0];
        if (op[0] == 'R')
        {
            SendReport(op);
            return;
        }
        Dictionary<string, object> args = new Dictionary<string, object>();
        int count = 1;
        string function;
        string[] parts = op.Split(':');
        if (op[0] == 'E')
        {
            function = "Earn";
            int amount = int.Parse(parts[1]);
            while (count < queue.Count && queue[count][0] == 'E')
            {
                int idx = count;
                count = idx + 1;
                amount = int.Parse(queue[idx].Substring(2)) + amount;
            }
            args["amount"] = amount;
        }
        else if (op[0] == 'B')
        {
            function = "Buy";
            args["names"] = parts[1].Split(',');
        }
        else if (op[0] == 'U')
        {
            function = "BuyBundle";
            args["bundle"] = parts[1];
        }
        else if (op[0] == 'G')
        {
            function = "OwnerGive";
            args["coins"] = int.Parse(parts[1]);
            args["bolts"] = int.Parse(parts[2]);
            args["xp"] = int.Parse(parts[3]);
        }
        else if (op[0] == 'S')
        {
            function = "SpendBolts";
            args["item"] = parts[1];
        }
        else if (op[0] == 'P')
        {
            function = "RedeemPurchases";
            if (parts.Length > 1 && parts[1].Length > 0)
            {
                args["skus"] = new string[] { parts[1] };
            }
        }
        else
        {
            function = "ClaimQuest";
            args["day"] = int.Parse(parts[1]);
            args["quest"] = int.Parse(parts[2]);
        }
        sendingCount = count;
        Call(function, args,
            reply =>
            {
                queue.RemoveRange(0, count);
                sendingCount = 0;
                SaveQueue();
                if (!string.IsNullOrEmpty(reply.refused))
                {
                    Debug.LogWarning("[GillaCloud] the server refused " + function + ": " + reply.refused);
                }
                nextSend = Time.unscaledTime + 3f;
                Adopt(reply);
            },
            why =>
            {
                sendingCount = 0;
                nextSend = Time.unscaledTime + 20f;
                Debug.LogWarning("[GillaCloud] " + function + " didn't get through, it'll go again: " + why);
            });
    }

    private void Adopt(Reply reply)
    {
        if (reply.day != 0)
        {
            ModMenuAccess.Adopt(reply.owned);
            NameTitles.Adopt(reply.owned);
            if (queue.Count < 1 && sendingCount < 1)
            {
                Coins.Adopt(reply.coins, reply.owned, reply.bolts);
                if (reply.dailyGiven > 0)
                {
                    Debug.Log(string.Format("[GillaCloud] your daily {0} coins are in", reply.dailyGiven));
                }
                GillaLevel.Adopt(reply.xp);
                GillaQuests.Adopt(reply.day, reply.claimed);
            }
        }
    }

    private void LoadProfile()
    {
        GetUserDataRequest request = new GetUserDataRequest();
        List<string> keys = new List<string>();
        keys.Add("Worn");
        keys.Add("Colour");
        request.Keys = keys;
        PlayFabClientAPI.GetUserData(request, LoadProfile_b__72_0,
            error => Debug.LogWarning("[GillaCloud] couldn't read what you wear: " + error.GenerateErrorReport()),
            null, null);
    }

    private void SaveProfile()
    {
        profileUnsaved = false;
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("Worn", Cosmetics.WornNames);
        if (GillaComputer.SavedCode > -1)
        {
            data["Colour"] = GillaComputer.SavedCode.ToString();
        }
        UpdateUserDataRequest request = new UpdateUserDataRequest();
        request.Data = data;
        PlayFabClientAPI.UpdateUserData(request, SaveProfile_b__73_0, SaveProfile_b__73_1, null, null);
    }

    private void Call(string function, Dictionary<string, object> args, Action<Reply> done, Action<string> failed)
    {
        if (PlayFabSettings.staticPlayer.IsClientLoggedIn())
        {
            ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest();
            request.FunctionName = function;
            request.FunctionParameter = args;
            PlayFabClientAPI.ExecuteCloudScript(request,
                result =>
                {
                    if (result.Error != null)
                    {
                        string message = result.Error.Message ?? "";
                        if (result.Error.Error == "CloudScriptNotFound" || message.Contains("No function named"))
                        {
                            Debug.LogWarning("[GillaCloud] the server's script has no " + function + ": upload CloudScript/gilla-cloudscript.js to PlayFab");
                            Reply none = new Reply();
                            none.refused = "no " + function + " on the server";
                            done(none);
                            return;
                        }
                        failed(result.Error.Error + ": " + result.Error.Message);
                        return;
                    }
                    ISerializerPlugin serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer, "");
                    Reply reply = JsonUtility.FromJson<Reply>(serializer.SerializeObject(result.FunctionResult));
                    if (reply == null)
                    {
                        failed(function + " gave no answer");
                    }
                    else
                    {
                        done(reply);
                    }
                },
                error =>
                {
                    failed(error.GenerateErrorReport());
                    if (error.Error == (PlayFabErrorCode)1074)
                    {
                        SessionLost();
                    }
                    else if (error.Error == PlayFabErrorCode.AccountBanned && !Banned)
                    {
                        BeBanned();   // Redux: banned while playing
                    }
                },
                null, null);
        }
        else
        {
            failed("not logged in");
        }
    }

    private void BeginShiftAuth_b__41_0(ShiftAuth.Outcome outcome)
    {
        if (outcome.Kind != ShiftAuth.FailKind.Banned) return;
        BeBanned();
    }

    private void SendReport_b__69_0(ReportPlayerClientResult result)
    {
        queue.RemoveAt(0);
        sendingCount = 0;
        SaveQueue();
        ReportsSent = ReportsSent + 1;
        nextSend = Time.unscaledTime + 3f;
        Debug.Log(string.Format("[GillaCloud] report sent, {0} left today", result.SubmissionsRemaining));
    }

    private void SendReport_b__69_1(PlayFabError error)
    {
        sendingCount = 0;
        nextSend = Time.unscaledTime + 20f;
        PlayFabErrorCode code = error.Error;
        if (code == (PlayFabErrorCode)1074)
        {
            SessionLost();
            return;
        }
        if (error.HttpCode > 499 || code == (PlayFabErrorCode)1123 || code == (PlayFabErrorCode)2)
        {
            Debug.LogWarning("[GillaCloud] a report didn't get through, it'll go again: " + error.GenerateErrorReport());
            return;
        }
        Debug.LogWarning("[GillaCloud] PlayFab refused a report, dropped: " + error.GenerateErrorReport());
        queue.RemoveAt(0);
        SaveQueue();
    }

    private void LoadProfile_b__72_0(GetUserDataResult result)
    {
        if (PlayerPrefs.GetInt("Gilla.Cloud.ProfileUnsaved", 0) == 1 || result.Data == null || result.Data.Count == 0)
        {
            profileUnsaved = true;
            return;
        }
        applying = true;
        UserDataRecord worn;
        if (result.Data.TryGetValue("Worn", out worn))
        {
            Cosmetics.Adopt(worn.Value.Split(','));
        }
        UserDataRecord colour;
        if (result.Data.TryGetValue("Colour", out colour))
        {
            int code;
            if (int.TryParse(colour.Value, out code))
            {
                GillaComputer.Adopt(code);
            }
        }
        applying = false;
    }

    private void SaveProfile_b__73_0(UpdateUserDataResult _)
    {
        if (profileUnsaved) return;
        PlayerPrefs.SetInt("Gilla.Cloud.ProfileUnsaved", 0);
        PlayerPrefs.Save();
    }
}
