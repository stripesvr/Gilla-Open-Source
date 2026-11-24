// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public static class GillaLevel
{
    private const string SaveKey = "Gilla.XP";
    private static bool loaded;
    private static int xp;

    public static event Action Changed;
    public static event Action<int, int> Gained;

    public static int Xp
    {
        get
        {
            Load();
            return xp;
        }
    }

    public static int Level
    {
        get { return LevelAt(Xp); }
    }

    public static int IntoLevel
    {
        get
        {
            int total = Xp;
            return total - TotalFor(LevelAt(Xp));
        }
    }

    public static int ForNext
    {
        get { return Need(LevelAt(Xp)); }
    }

    public static int Need(int level)
    {
        return level * 100;
    }

    // Redux: past level ~6,500 the 32-bit sum wrapped negative, so LevelAt looped forever for big XP
    // totals (it froze Play in the editor on an account with owner-granted XP). Worked in 64 bits now
    // and capped, so any XP total gives an answer.
    public const int MaxLevel = 6000;

    public static int TotalFor(int level)
    {
        long total = (long)level * (level - 1) * 50L;
        return total > int.MaxValue ? int.MaxValue : (int)total;
    }

    private static int LevelAt(int total)
    {
        int level = 1;
        while (level < MaxLevel && TotalFor(level + 1) <= total)
        {
            level++;
        }
        return level;
    }

    public static int ForYou(int xp)
    {
        if (0 < xp)
        {
            if (GillaPurchases.XpBoost)
            {
                xp = (xp * 3 + 1) / 2;
            }
        }
        return xp;
    }

    public static int Add(int amount)
    {
        Load();
        if (amount < 1)
        {
            return 0;
        }
        int gained = ForYou(amount);
        int before = LevelAt(Xp);
        xp += gained;
        PlayerPrefs.SetInt(SaveKey, xp);
        PlayerPrefs.Save();
        if (Changed != null)
        {
            Changed();
        }
        int levelsUp = LevelAt(Xp) - before;
        if (Gained != null)
        {
            Gained(gained, levelsUp);
        }
        return levelsUp;
    }

    public static void Adopt(int total)
    {
        Load();
        int value = Mathf.Max(total, 0);
        if (value != xp)
        {
            xp = value;
            PlayerPrefs.SetInt(SaveKey, value);
            PlayerPrefs.Save();
            if (Changed != null)
            {
                Changed();
                return;
            }
        }
    }

    private static void Load()
    {
        if (!loaded)
        {
            loaded = true;
            xp = Mathf.Max(PlayerPrefs.GetInt(SaveKey, 0), 0);
        }
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        loaded = false;
    }
}
