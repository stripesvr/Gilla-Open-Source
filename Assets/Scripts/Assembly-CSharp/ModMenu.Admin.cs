// Redux addition (not in the original game): the owner menu's admin pages.
// Pick a player in the room, then ban, grant or inspect them. Every action is carried out by
// the server (CloudScript "Admin"), which first checks that the caller's account is an owner;
// the menu only asks and shows the answer.
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class ModMenu
{
    private int adminPick;
    private string adminInfoFor;
    private GillaCloud.Reply adminInfo;

    private NetworkApe AdminTarget()
    {
        List<NetworkApe> list = NetworkApe.All;
        int n = 0;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) n++;
        if (n == 0) return null;
        int want = ((adminPick % n) + n) % n;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null) continue;
            if (want-- == 0) return list[i];
        }
        return null;
    }

    private string AdminName()
    {
        NetworkApe a = AdminTarget();
        if (a == null) return "NOBODY";
        string name = string.IsNullOrEmpty(a.PlayerName) ? "UNNAMED" : a.PlayerName.ToUpperInvariant();
        return a == NetworkApe.Local ? name + " (YOU)" : name;
    }

    private GillaCloud.Reply AdminKnown()
    {
        NetworkApe a = AdminTarget();
        if (a == null || adminInfo == null || adminInfoFor != a.AccountId) return null;
        return adminInfo;
    }

    private bool AdminHas(string marker)
    {
        GillaCloud.Reply r = AdminKnown();
        return r != null && r.owned != null && Array.IndexOf(r.owned, marker) >= 0;
    }

    private string AdminNumber(Func<GillaCloud.Reply, int> pick)
    {
        GillaCloud.Reply r = AdminKnown();
        return r == null ? "?" : pick(r).ToString("N0");
    }

    private void AdminDo(string action, string done, Dictionary<string, object> args = null)
    {
        NetworkApe a = AdminTarget();
        if (a == null)
        {
            Toast("NO PLAYER PICKED");
            return;
        }
        string id = a.AccountId;
        if (string.IsNullOrEmpty(id))
        {
            Toast("THAT PLAYER HAS NO ACCOUNT YET");
            return;
        }
        GillaCloud.Admin(action, id, args,
            reply =>
            {
                if (!string.IsNullOrEmpty(reply.refused))
                {
                    Toast(reply.refused.ToUpperInvariant());
                    return;
                }
                adminInfo = reply;
                adminInfoFor = id;
                if (done != null) Toast(done);
            },
            why => Toast("THE SERVER DIDN'T ANSWER"));
    }

    private void AdminGive(string what, int amount)
    {
        Dictionary<string, object> args = new Dictionary<string, object>();
        args[what] = amount;
        AdminDo("give", "GAVE " + amount.ToString("N0") + " " + what.ToUpperInvariant(), args);
    }

    private void AdminBan(int hours)
    {
        Dictionary<string, object> args = new Dictionary<string, object>();
        args["hours"] = hours;
        args["reason"] = "Banned by an owner in game";
        AdminDo("ban", hours > 0 ? "BANNED FOR " + hours + " H" : "BANNED FOR GOOD", args);
    }

    private void AdminRole(string role, string marker, string label)
    {
        bool on = !AdminHas(marker);
        Dictionary<string, object> args = new Dictionary<string, object>();
        args["role"] = role;
        args["on"] = on;
        AdminDo("role", label + (on ? " GIVEN" : " TAKEN"), args);
    }

    private void AdminPages()
    {
        Page p = AddPage("PLAYERS");
        Setting(p, "PLAYER", AdminName, () => { adminPick++; AdminDo("info", null); });
        Action(p, "PREVIOUS PLAYER", () => { adminPick--; AdminDo("info", null); }, null);
        Action(p, "LOOK UP ACCOUNT", () => AdminDo("info", "ACCOUNT READ"), null);
        Setting(p, "COINS", () => AdminNumber(r => r.coins), () => AdminDo("info", null));
        Setting(p, "BOLTS", () => AdminNumber(r => r.bolts), () => AdminDo("info", null));
        Setting(p, "XP", () => AdminNumber(r => r.xp), () => AdminDo("info", null));
        Setting(p, "BANNED", () => { GillaCloud.Reply r = AdminKnown(); return r == null ? "?" : (r.banned ? "YES" : "NO"); }, () => AdminDo("info", null));

        p = AddPage("BAN");
        Setting(p, "PLAYER", AdminName, () => { adminPick++; AdminDo("info", null); });
        Action(p, "BAN 1 HOUR", () => AdminBan(1), null);
        Action(p, "BAN 1 DAY", () => AdminBan(24), null);
        Action(p, "BAN 1 WEEK", () => AdminBan(168), null);
        Action(p, "BAN FOR GOOD", () => AdminBan(0), null);
        Action(p, "UNBAN", () => AdminDo("unban", "UNBANNED"), null);

        p = AddPage("GRANT");
        Setting(p, "PLAYER", AdminName, () => { adminPick++; AdminDo("info", null); });
        Action(p, "+1,000 COINS", () => AdminGive("coins", 1000), () => AdminNumber(r => r.coins));
        Action(p, "+10,000 COINS", () => AdminGive("coins", 10000), () => AdminNumber(r => r.coins));
        Action(p, "+100 BOLTS", () => AdminGive("bolts", 100), () => AdminNumber(r => r.bolts));
        Action(p, "+1,000 XP", () => AdminGive("xp", 1000), () => AdminNumber(r => r.xp));
        Action(p, "TAKE 1,000 COINS", () => AdminGive("coins", -1000), () => AdminNumber(r => r.coins));
        Toggle(p, "OP MENU", () => AdminHas("OPMenu"), on => AdminRole("op", "OPMenu", "OP MENU"));
        Toggle(p, "SIZE MENU", () => AdminHas("SizeMenu"), on => AdminRole("size", "SizeMenu", "SIZE MENU"));
        Toggle(p, "PUNCH MENU", () => AdminHas("PunchMenu"), on => AdminRole("punch", "PunchMenu", "PUNCH MENU"));
        Toggle(p, "OWNER", () => AdminHas("ModMenu"), on => AdminRole("owner", "ModMenu", "OWNER"));

        p = AddPage("DEBUG");
        Setting(p, "FPS", () => Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f)).ToString(), () => { });
        Setting(p, "ROOM", () => NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.RoomLabel : "NONE", () => { });
        Setting(p, "LOBBY", () => NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Kind.ToString().ToUpperInvariant() : "NONE", () => { });
        Setting(p, "PLAYERS", () => NetworkApe.All.Count.ToString(), () => { });
        Setting(p, "MONSTERS", () => MonsterBrain.All.Count.ToString(), () => { });
        Setting(p, "MASTER CLIENT", () =>
        {
            NetworkBootstrap b = NetworkBootstrap.Instance;
            return b != null && b.Runner != null && b.Runner.IsRunning ? (b.Runner.IsSharedModeMasterClient ? "YOU" : "SOMEONE ELSE") : "NO ROOM";
        }, () => { });
        Setting(p, "ACCOUNT", () => GillaCloud.SignedIn ? "SIGNED IN" : (string.IsNullOrEmpty(GillaCloud.Problem) ? "SIGNING IN" : "NOT SIGNED IN"), () => { });
        Setting(p, "YOUR ID", () => string.IsNullOrEmpty(NetworkBootstrap.PlayerId) ? "NONE" : NetworkBootstrap.PlayerId, () => { });
        Setting(p, "BUILD", () => Application.version + " / " + Application.unityVersion, () => { });
        Action(p, "REFRESH MY ACCOUNT", () => { GillaCloud.SyncNow(); Toast("ASKED THE SERVER"); }, null);
    }
}
