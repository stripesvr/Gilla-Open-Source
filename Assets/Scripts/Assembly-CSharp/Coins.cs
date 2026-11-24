// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Coins
{
    private const string BalanceKey = "Gilla.Coins";
    private const string OwnedKey = "Gilla.Owned";
    private const string BoltsKey = "Gilla.Bolts";
    public const int Starting = 500;
    public static bool Infinite = true;
    private static bool loaded;
    private static int balance;
    private static int bolts;
    private static readonly HashSet<string> owned = new HashSet<string>();

    public static event Action Changed;

    public static int Balance
    {
        get
        {
            Load();
            if (Infinite)
            {
                return int.MaxValue;
            }
            return balance;
        }
    }

    public static string Text
    {
        get
        {
            if (Infinite)
            {
                return "INF";
            }
            return Balance.ToString("N0");
        }
    }

    public static int Bolts
    {
        get
        {
            Load();
            return bolts;
        }
    }

    public static string BoltsText
    {
        get { return Bolts.ToString("N0"); }
    }

    public static void AddBolts(int amount)
    {
        Load();
        bolts = Mathf.Max(bolts + amount, 0);
        Save();
        if (0 < amount)
        {
            GillaCloud.Earned(amount);
        }
    }

    public static void AddBoltsHere(int amount)
    {
        Load();
        bolts = Mathf.Max(bolts + amount, 0);
        Save();
    }

    public static void SpendBolts(int amount, string item)
    {
        Load();
        bolts = Mathf.Max(bolts - amount, 0);
        Save();
        GillaCloud.SpentBolts(item);
    }

    public static bool Owns(byte id)
    {
        Load();
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
        if (e == null)
        {
            return false;
        }
        if (owned.Contains(e.name) || OwnerHas(e))
        {
            return true;
        }
        if (string.IsNullOrEmpty(e.comesWith))
        {
            return false;
        }
        return owned.Contains(e.comesWith);
    }

    private static bool OwnerHas(CosmeticCatalog.Entry e)
    {
        if (ModMenuAccess.Menu == ModMenu.Kind.Owner)
        {
            CosmeticCatalog catalog = CosmeticCatalog.Get();
            CosmeticCatalog.Bundle[] bundles = catalog.bundles;
            if (bundles != null)
            {
                for (int i = 0; i < bundles.Length; i++)
                {
                    string[] items = bundles[i].items;
                    if (items != null)
                    {
                        if (Array.IndexOf(items, e.name) > -1)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    public static bool OwnsName(string name)
    {
        Load();
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }
        return owned.Contains(name);
    }

    public static int PriceOf(CosmeticCatalog.Bundle b)
    {
        List<byte> ids = CosmeticCatalog.IdsOf(b);
        int remaining = 0;
        int total = 0;
        foreach (byte id in ids)
        {
            CosmeticCatalog.Entry entry = CosmeticCatalog.Of(id);
            total += entry.price;
            if (!Owns(id))
            {
                remaining += entry.price;
            }
        }
        if (total < 1)
        {
            return 0;
        }
        if (remaining < 1)
        {
            return 0;
        }
        float f = ((float)remaining * (float)b.price) / (float)total;
        int result = (int)Math.Round((double)f);
        if (result < 2)
        {
            result = 1;
        }
        return result;
    }

    public static bool OwnsAll(CosmeticCatalog.Bundle b)
    {
        List<byte> ids = CosmeticCatalog.IdsOf(b);
        if (0 < ids.Count)
        {
            return ids.TrueForAll(Owns);
        }
        return false;
    }

    public static bool Buy(CosmeticCatalog.Bundle b)
    {
        if (b == null || b.realMoney)
        {
            return false;
        }
        Load();
        List<CosmeticCatalog.Entry> list = new List<CosmeticCatalog.Entry>();
        foreach (byte id in CosmeticCatalog.IdsOf(b))
        {
            CosmeticCatalog.Entry item = CosmeticCatalog.Of(id);
            if (item != null && !owned.Contains(item.name))
            {
                list.Add(item);
            }
        }
        if (list.Count != 0)
        {
            int price = PriceOf(b);
            if (Balance < price)
            {
                return false;
            }
            if (!Infinite)
            {
                balance -= price;
            }
            foreach (CosmeticCatalog.Entry entry in list)
            {
                owned.Add(entry.name);
            }
            Save();
            GillaCloud.BoughtBundle(b.name);
        }
        return true;
    }

    public static void EditorGiven(CosmeticCatalog.Bundle b)
    {
        Load();
        if (b == null)
        {
            return;
        }
        if (!string.IsNullOrEmpty(b.sku) && owned.Contains(b.sku))
        {
            return;
        }
        if (OwnsAll(b))
        {
            if (string.IsNullOrEmpty(b.tag))
            {
                return;
            }
            if (owned.Contains(b.tag))
            {
                return;
            }
        }
        foreach (byte id in CosmeticCatalog.IdsOf(b))
        {
            CosmeticCatalog.Entry entry = CosmeticCatalog.Of(id);
            if (entry != null && !owned.Contains(entry.name))
            {
                owned.Add(entry.name);
            }
        }
        if (!string.IsNullOrEmpty(b.tag))
        {
            owned.Add(b.tag);
        }
        if (!string.IsNullOrEmpty(b.sku))
        {
            owned.Add(b.sku);
        }
        balance = b.coins + balance;
        Save();
    }

    public static bool Buy(IEnumerable<byte> ids, int price = -1)
    {
        Load();
        List<CosmeticCatalog.Entry> list = new List<CosmeticCatalog.Entry>();
        int total = 0;
        foreach (byte id in ids)
        {
            CosmeticCatalog.Entry item = CosmeticCatalog.Of(id);
            if (item != null && !owned.Contains(item.name) && !list.Contains(item))
            {
                list.Add(item);
                total += item.price;
            }
        }
        int count = list.Count;
        if (price >= 0 && 0 < count)
        {
            total = price;
        }
        if (count != 0)
        {
            if (Balance < total)
            {
                return list.Count == 0;
            }
            if (!Infinite)
            {
                balance -= total;
            }
            foreach (CosmeticCatalog.Entry entry in list)
            {
                owned.Add(entry.name);
            }
            Save();
            List<string> names = list.ConvertAll(x => x.name);
            GillaCloud.Bought(names);
        }
        return true;
    }

    public static void Add(int amount)
    {
        Load();
        balance = Mathf.Max(balance + amount, 0);
        Save();
    }

    public static void Adopt(int coins, string[] names, int boltCount = -1)
    {
        Load();
        balance = Mathf.Max(coins, 0);
        if (-1 < boltCount)
        {
            bolts = boltCount;
        }
        owned.Clear();
        if (names != null)
        {
            owned.UnionWith(names);
        }
        Save();
    }

    private static void Load()
    {
        if (loaded)
        {
            return;
        }
        loaded = true;
        balance = PlayerPrefs.GetInt(BalanceKey, Starting);
        bolts = PlayerPrefs.GetInt(BoltsKey, 0);
        string saved = PlayerPrefs.GetString(OwnedKey, "");
        string[] parts = saved.Split(',');
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (0 < part.Length)
            {
                owned.Add(part);
            }
        }
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(BalanceKey, balance);
        PlayerPrefs.SetInt(BoltsKey, bolts);
        PlayerPrefs.SetString(OwnedKey, string.Join(",", owned));
        PlayerPrefs.Save();
        if (Changed != null)
        {
            Changed();
        }
    }
}
