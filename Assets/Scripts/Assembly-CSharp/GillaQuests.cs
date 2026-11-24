// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public static class GillaQuests
{
    public enum Stat
    {
        Recycle = 0,
        RecycleGold = 1,
        RecycleDiamond = 2,
        RecycleBolts = 3,
        Doors = 4,
        Glass = 5,
        FlashlightSeconds = 6,
        Stow = 7,
        Metres = 8,
        MonsterRound = 9,
        Caught = 10,
        TryOn = 11,
        SplashRace = 12
    }

    public sealed class Quest
    {
        public readonly GillaQuests.Stat stat;
        public readonly int goal;
        public readonly int xp;
        public readonly string text;

        public Quest(GillaQuests.Stat stat, int goal, int xp, string text)
        {
            this.text = text;
            this.stat = stat;
            this.goal = goal;
            this.xp = xp;
        }
    }

    public static readonly GillaQuests.Quest[] Pool = new GillaQuests.Quest[]
    {
        new Quest(Stat.Recycle, 5, 200, "RECYCLE 5 THINGS"),
        new Quest(Stat.RecycleGold, 1, 150, "RECYCLE A GOLD BAR"),
        new Quest(Stat.RecycleDiamond, 1, 300, "RECYCLE A DIAMOND"),
        new Quest(Stat.RecycleBolts, 50, 200, "RECYCLE FOR 50 BOLTS"),
        new Quest(Stat.Doors, 10, 125, "OPEN 10 HAZARD DOORS"),
        new Quest(Stat.Glass, 3, 125, "SMASH 3 WINDOWS"),
        new Quest(Stat.FlashlightSeconds, 180, 150, "USE YOUR FLASHLIGHT 3 MIN"),
        new Quest(Stat.Stow, 3, 125, "PUT 3 THINGS IN YOUR PACK"),
        new Quest(Stat.Metres, 1000, 175, "TRAVEL 1000 METRES"),
        new Quest(Stat.MonsterRound, 1, 200, "PLAY BE THE MONSTER"),
        new Quest(Stat.Caught, 1, 100, "GET CAUGHT BY A MONSTER"),
        new Quest(Stat.SplashRace, 1, 100, "PLAY DON'T SPLASH"),
    };
    private static readonly int[] Repurposed = new int[] { 11 };
    public const int Count = 3;
    public const int CoinsEach = 50;
    private const string SaveKey = "Gilla.Quests";
    public static event Action Changed;
    private static int day = -1;
    private static readonly GillaQuests.Quest[] today = new GillaQuests.Quest[3];
    private static readonly int[] progress = new int[3];
    private static readonly bool[] claimed = new bool[3];
    private static float lastSave;
    private static bool unsaved;

    public static int Day
    {
        get
        {
            TimeSpan span = DateTime.UtcNow - new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)span.TotalDays;
        }
    }

    public static TimeSpan UntilNew
    {
        get
        {
            return DateTime.UtcNow.Date.AddDays(1.0) - DateTime.UtcNow;
        }
    }

    public static GillaQuests.Quest QuestAt(int i)
    {
        Load();
        return today[i];
    }

    public static int ProgressAt(int i)
    {
        Load();
        return Mathf.Min(progress[i], today[i].goal);
    }

    public static bool DoneAt(int i)
    {
        Load();
        return progress[i] >= today[i].goal;
    }

    public static bool ClaimedAt(int i)
    {
        Load();
        return claimed[i];
    }

    public static void Report(GillaQuests.Stat stat, int amount = 1)
    {
        if (amount < 1)
        {
            return;
        }
        Load();
        bool changed = false;
        for (int i = 0; i < 3; i++)
        {
            if (today[i].stat == stat && !claimed[i] && progress[i] < today[i].goal)
            {
                changed = true;
                progress[i] = Mathf.Min(progress[i] + amount, today[i].goal);
            }
        }
        if (!changed)
        {
            return;
        }
        unsaved = true;
        if ((stat == Stat.Metres || stat == Stat.FlashlightSeconds) && Time.unscaledTime - lastSave <= 10f)
        {
        }
        else
        {
            Save();
        }
        if (Changed != null)
        {
            Changed();
        }
    }

    public static int Claim(int i)
    {
        Load();
        if ((uint)i > 2)
        {
            return -1;
        }
        if (claimed[i])
        {
            return -1;
        }
        if (progress[i] < today[i].goal)
        {
            return -1;
        }
        claimed[i] = true;
        Save();
        int level = GillaLevel.Add(today[i].xp);
        Coins.Add(50);
        GillaCloud.QuestClaimed(day, Array.IndexOf<Quest>(Pool, today[i]));
        if (Changed != null)
        {
            Changed();
        }
        return level;
    }

    public static void Adopt(int serverDay, int[] claimedPool)
    {
        Load();
        if (claimedPool != null && day == serverDay)
        {
            bool changed = false;
            for (int i = 0; i < 3; i++)
            {
                if (claimed[i])
                {
                    continue;
                }
                int poolIndex = Array.IndexOf<Quest>(Pool, today[i]);
                if (Array.IndexOf<int>(claimedPool, poolIndex) < 0)
                {
                    continue;
                }
                claimed[i] = true;
                changed = true;
                progress[i] = today[i].goal;
            }
            if (!changed)
            {
                return;
            }
            Save();
            if (Changed != null)
            {
                Changed();
            }
        }
    }

    private static void Load()
    {
        int d = Day;
        if (day == d)
        {
            return;
        }
        day = d;
        Pick(today, d, false);
        Array.Clear(progress, 0, 3);
        Array.Clear(claimed, 0, 3);
        string saved = PlayerPrefs.GetString("Gilla.Quests", "");
        string[] parts = saved.Split('|');
        int savedDay;
        if (parts.Length > 2 && int.TryParse(parts[0], out savedDay) && savedDay == d)
        {
            Quest[] into;
            if (parts.Length < 4)
            {
                into = new Quest[3];
                Pick(into, d, true);
            }
            else
            {
                into = today;
            }
            string[] progressParts = parts[1].Split(',');
            for (int i = 0; i < 3; i++)
            {
                int idx = Array.IndexOf<Quest>(today, into[i]);
                if (idx < 0)
                {
                    continue;
                }
                if (i < progressParts.Length)
                {
                    int.TryParse(progressParts[i], out progress[idx]);
                }
                if (i < parts[2].Length)
                {
                    claimed[idx] = parts[2][i] == '1';
                }
                if (parts.Length < 4)
                {
                    int poolIndex = Array.IndexOf<Quest>(Pool, today[idx]);
                    if (Array.IndexOf<int>(Repurposed, poolIndex) >= 0)
                    {
                        progress[idx] = 0;
                    }
                }
            }
        }
        if (Changed != null)
        {
            Changed();
        }
    }

    private static bool Retired(GillaQuests.Quest q)
    {
        return q.stat == Stat.Stow;
    }

    private static void Pick(GillaQuests.Quest[] into, int d, bool withRetired)
    {
        int[] order = new int[Pool.Length];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        uint seed = (uint)d * 0x9E3779B1u + 12345u;
        for (int i = order.Length - 1; i > 0; i--)
        {
            seed = seed * 1664525u + 1013904223u;
            int j = (int)(seed % (uint)(i + 1));
            int tmp = order[i];
            order[i] = order[j];
            order[j] = tmp;
        }
        int count = 0;
        bool hasRecycle = false;
        for (int i = 0; i < order.Length; i++)
        {
            Quest q = Pool[order[i]];
            if (!withRetired && Retired(q))
            {
                continue;
            }
            bool isRecycle = (int)q.stat < 4;
            if (isRecycle && hasRecycle)
            {
                continue;
            }
            hasRecycle = isRecycle || hasRecycle;
            into[count] = q;
            count++;
            if (count == 3)
            {
                return;
            }
        }
    }

    private static void Save()
    {
        if (day < 0)
        {
            return;
        }
        string value = string.Concat(new string[]
        {
            day.ToString(), "|",
            progress[0].ToString(), ",",
            progress[1].ToString(), ",",
            progress[2].ToString(), "|",
            claimed[0] ? "1" : "0",
            claimed[1] ? "1" : "0",
            claimed[2] ? "1" : "0",
            "|2"
        });
        PlayerPrefs.SetString("Gilla.Quests", value);
        PlayerPrefs.Save();
        lastSave = Time.unscaledTime;
        unsaved = false;
    }

    public static void Flush()
    {
        if (unsaved)
        {
            Save();
        }
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        day = -1;
        unsaved = false;
    }
}
