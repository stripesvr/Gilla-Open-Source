// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public static class NameTitles
{
    public const int None = 0;
    public const int Owner = 1;
    public const int Creator = 2;
    public const int Beta = 3;
    public const int Vip = 4;
    public const int Dev = 5;

    public static readonly string[] Items = new string[] { "OwnerTag", "CreatorTag", "BetaTag", "VipTag", "DevTag" };
    private static readonly bool[] given = new bool[Items.Length];
    public static bool TestAsPlayer;
    public static readonly (string label, Color32 colour)[] All = new (string label, Color32 colour)[]
    {
        ("OWNER", new Color32(0xff, 0x22, 0x2c, 0xff)),
        ("CONTENT CREATOR", new Color32(0xff, 0x38, 0xc4, 0xff)),
        ("BETA", new Color32(0xbe, 0x8c, 0xff, 0xff)),
        ("VIP", new Color32(0xff, 0xc8, 0x14, 0xff)),
        ("DEV", new Color32(0x6e, 0xc3, 0xff, 0xff)),
    };
    public static readonly string[] Creators = new string[0];
    private const string Key = "Gilla.Title";
    private static int worn = -1;
    public const string VipKey = "Gilla.TitleVipGiven";

    public static void Adopt(string[] owned)
    {
        for (int i = 0; i < given.Length; i++)
        {
            given[i] = owned != null && Array.IndexOf(owned, Items[i]) >= 0;
        }
        GiveVip();
    }

    public static bool Given(int id)
    {
        if (0 < id && id <= given.Length)
        {
            return given[id - 1];
        }
        return false;
    }

    public static int Count
    {
        get { return All.Length; }
    }

    public static string Label(int id)
    {
        if (0 < id && id <= All.Length)
        {
            return All[id - 1].label;
        }
        return "";
    }

    public static Color32 Colour(int id)
    {
        if (0 < id && id <= All.Length)
        {
            return All[id - 1].colour;
        }
        return new Color32(255, 255, 255, 255);
    }

    public static Color32 Light(int id)
    {
        Color32 c = Colour(id);
        return new Color32(
            (byte)(int)(c.r + (255 - c.r) * 0.62f),
            (byte)(int)(c.g + (255 - c.g) * 0.62f),
            (byte)(int)(c.b + (255 - c.b) * 0.62f),
            (byte)(int)(c.a + (255 - c.a) * 0.62f));
    }

    public static int Worn
    {
        get
        {
            if (worn < 0)
            {
                int v = PlayerPrefs.GetInt(Key, 0);
                worn = v < 0 ? 0 : Mathf.Min(v, All.Length);
            }
            return worn;
        }
    }

    public static int Shown
    {
        get
        {
            if (MayWear(Worn))
            {
                return Worn;
            }
            return 0;
        }
    }

    public static void Wear(int id)
    {
        worn = id < 0 ? 0 : Mathf.Min(id, All.Length);
        PlayerPrefs.SetInt(Key, worn);
        PlayerPrefs.Save();
    }

    public static bool MayWear(int id)
    {
        if (0 < id)
        {
            if (All.Length < id)
            {
                return false;
            }
            if (Application.isEditor && !TestAsPlayer)
            {
                return true;
            }
            if (Given(id))
            {
                return true;
            }
            if (id < 4)
            {
                if (id != 1)
                {
                    if (id == 2)
                    {
                        return Array.Exists(Creators, n =>
                        {
                            if (string.IsNullOrEmpty(n))
                            {
                                return false;
                            }
                            return string.Equals(n.Trim(), (ModMenuAccess.MetaUserName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                        });
                    }
                    return false;
                }
            }
            else
            {
                if (id == 4)
                {
                    CosmeticCatalog.Bundle b = CosmeticCatalog.BundleNamed("VIP");
                    if (b != null)
                    {
                        return Coins.OwnsAll(b);
                    }
                    return false;
                }
                if (id != 5)
                {
                    return false;
                }
            }
            if (ModMenuAccess.IsRealOwner)
            {
                return !TestAsPlayer;
            }
        }
        return false;
    }

    public static bool OwnsVip
    {
        get
        {
            if (Given(4))
            {
                return true;
            }
            CosmeticCatalog.Bundle b = CosmeticCatalog.BundleNamed("VIP");
            if (b != null)
            {
                return Coins.OwnsAll(b);
            }
            return false;
        }
    }

    public static void GiveVip()
    {
        if (PlayerPrefs.GetInt(VipKey, 0) != 1)
        {
            if (OwnsVip)
            {
                PlayerPrefs.SetInt(VipKey, 1);
                if (Worn != 0)
                {
                    if (MayWear(Worn))
                    {
                        PlayerPrefs.Save();
                        return;
                    }
                }
                Wear(4);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)0)]
    private static void WatchForVip()
    {
        Coins.Changed -= GiveVip;
        Coins.Changed += GiveVip;
        GiveVip();
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        worn = -1;
        Array.Clear(given, 0, given.Length);
        TestAsPlayer = false;
    }
}
