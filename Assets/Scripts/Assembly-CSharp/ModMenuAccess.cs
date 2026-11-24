// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public static class ModMenuAccess
{
    public const string Item = "ModMenu";
    public const string SizeItem = "SizeMenu";
    public const string OpItem = "OPMenu";
    public const string PunchItem = "PunchMenu";
    public static readonly string[] OwnerNames = new string[] { "Diggleburb" };
    private static string hand;
    private const string OwnerEquipKey = "Gilla.ModMenu.OwnerEquipped.";
    private const string EquipKey = "Gilla.ModMenu.Equipped.";

    public static string MetaUserName { get; set; }
    public static bool Granted { get; private set; }
    public static bool HasSize { get; private set; }
    public static bool HasOp { get; private set; }
    public static bool HasPunch { get; private set; }

    public static bool IsOwner
    {
        get
        {
            if (Granted || Application.isEditor) return true;
            return IsOwnerName(MetaUserName);
        }
    }

    public static bool IsRealOwner
    {
        get
        {
            if (Application.isEditor) return true;
            return IsOwnerName(MetaUserName);
        }
    }

    public static ModMenu.Kind? Menu
    {
        get
        {
            if (OwnerHere) return ModMenu.Kind.Owner;
            return KindOf(BoughtOn());
        }
    }

    public static ModMenu.Kind? KindOf(string item)
    {
        if (item == "SizeMenu") return ModMenu.Kind.Size;
        if (item == "OPMenu") return ModMenu.Kind.OP;
        if (item == "PunchMenu") return ModMenu.Kind.Punch;
        return null;
    }

    public static string ItemOf(string sku)
    {
        if (sku == "gilla_size_menu") return "SizeMenu";
        if (sku == "gilla_op_menu") return "OPMenu";
        if (sku == "gilla_punch_menu") return "PunchMenu";
        return null;
    }

    private static string BoughtOn()
    {
        if (hand != null && Here(hand) && Equipped(hand))
        {
            return hand;
        }
        hand = null;
        string[] items = new string[] { "OPMenu", "SizeMenu", "PunchMenu" };
        foreach (string item in items)
        {
            if (Here(item) && Equipped(item))
            {
                hand = item;
                return item;
            }
        }
        return null;
    }

    public static bool SideBoard(ModMenu.Kind kind)
    {
        string item = ItemFor(kind);
        if (item != null)
        {
            if (OwnerHere) return OwnerEquipped(item);
            if (Here(item) && Equipped(item))
            {
                return BoughtOn() != item;
            }
        }
        return false;
    }

    public static string ItemFor(ModMenu.Kind kind)
    {
        if (kind == ModMenu.Kind.Size) return "SizeMenu";
        if (kind == ModMenu.Kind.OP) return "OPMenu";
        if (kind == ModMenu.Kind.Punch) return "PunchMenu";
        return null;
    }

    private static bool OwnerHere { get { return IsOwner; } }
    private static bool SizeHere { get { return HasSize; } }
    private static bool OpHere { get { return HasOp; } }
    private static bool PunchHere { get { return HasPunch; } }

    private static bool Here(string item)
    {
        if (item == "SizeMenu") return SizeHere;
        if (item == "OPMenu") return OpHere;
        if (item == "PunchMenu") return PunchHere;
        return false;
    }

    public static bool HasOwnerMenu { get { return OwnerHere; } }

    public static bool OwnerEquipped(string item)
    {
        return PlayerPrefs.GetInt(OwnerEquipKey + item, 0) == 1;
    }

    public static void SetOwnerEquipped(string item, bool on)
    {
        if (!string.IsNullOrEmpty(item))
        {
            if (OwnerEquipped(item) != on)
            {
                string key = OwnerEquipKey + item;
                string word = on ? " put on" : " taken off";
                PlayerPrefs.SetInt(key, on ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log("[ModMenu] " + item + word + " beside the owner's menu");
                ModMenu.AttachIfOwner();
                return;
            }
        }
    }

    public static bool OwnerHasOp
    {
        get
        {
            if (OwnerHere) return OwnerEquipped("OPMenu");
            return false;
        }
    }

    public static bool OwnerHasSize
    {
        get
        {
            if (OwnerHere) return OwnerEquipped("SizeMenu");
            return false;
        }
    }

    public static bool OwnerHasPunch
    {
        get
        {
            if (OwnerHere) return OwnerEquipped("PunchMenu");
            return false;
        }
    }

    public static bool OwnerHas(ModMenu.Kind kind)
    {
        if (OwnerHere) return SideBoard(kind);
        return false;
    }

    public static bool Worn(string item)
    {
        if (OwnerHere) return OwnerEquipped(item);
        if (Here(item)) return Equipped(item);
        return false;
    }

    public static void SetWorn(string item, bool on)
    {
        if (OwnerHere)
        {
            SetOwnerEquipped(item, on);
            return;
        }
        SetEquipped(item, on);
    }

    public static bool Equipped(string item)
    {
        return PlayerPrefs.GetInt(EquipKey + item, 1) == 1;
    }

    public static void SetEquipped(string item, bool on)
    {
        if (!string.IsNullOrEmpty(item))
        {
            if (Equipped(item) != on)
            {
                string key = EquipKey + item;
                string word = on ? " equipped" : " unequipped";
                PlayerPrefs.SetInt(key, on ? 1 : 0);
                PlayerPrefs.Save();
                Debug.Log("[ModMenu] " + item + word);
            }
        }
        ModMenu.AttachIfOwner();
    }

    public static bool OffHere(ModMenu.Kind kind)
    {
        if (kind != ModMenu.Kind.Owner)
        {
            if (NetworkBootstrap.Instance != null)
            {
                return NetworkBootstrap.Instance.Kind != NetworkBootstrap.Lobby.Regular;
            }
        }
        return false;
    }

    public static bool Owns(string item)
    {
        if (string.IsNullOrEmpty(item)) return false;
        if (!OwnerHere) return Here(item);
        return true;
    }

    public static bool MenuWorksHere
    {
        get
        {
            ModMenu.Kind? menu = Menu;
            if (!menu.HasValue) return false;
            return !OffHere(menu.Value);
        }
    }

    public static void Adopt(string[] owned)
    {
        bool owner = Has(owned, "ModMenu");
        bool size = Has(owned, "SizeMenu");
        bool op = Has(owned, "OPMenu");
        bool punch = Has(owned, "PunchMenu");
        if (owner == Granted && size == HasSize && op == HasOp && punch == HasPunch)
        {
            return;
        }
        Granted = owner;
        HasSize = size;
        HasOp = op;
        HasPunch = punch;
        Debug.Log(string.Concat(new string[]
        {
            "[ModMenu] the server says your account has: ",
            owner ? "owner " : "",
            size ? "size " : "",
            op ? "op " : "",
            punch ? "punch " : "",
            (!owner && !size && !op && !punch) ? "no " : "",
            "menu"
        }));
        ModMenu.AttachIfOwner();
    }

    private static bool Has(string[] owned, string item)
    {
        if (owned == null) return false;
        return Array.IndexOf<string>(owned, item) >= 0;
    }

    private static bool IsOwnerName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            for (int i = 0; i < OwnerNames.Length; i++)
            {
                string owner = OwnerNames[i];
                if (!string.IsNullOrEmpty(owner))
                {
                    if (string.Equals(owner.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Forget()
    {
        HasPunch = false;
        HasOp = false;
        HasSize = false;
        hand = null;
        Granted = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Report()
    {
        string id = NetworkBootstrap.PlayerId;
        Debug.Log(string.Format("[ModMenu] player id {0}, owner: {1}", id, IsOwner));
    }
}
