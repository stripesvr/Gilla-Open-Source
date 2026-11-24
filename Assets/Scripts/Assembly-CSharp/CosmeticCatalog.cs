// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gilla/Cosmetic Catalog")]
public class CosmeticCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string name;
        [Tooltip("Its name on the shop's screens.")]
        public string label;
        public CosmeticSlot slot;
        [Tooltip("What it costs, in coins.")]
        public int price = 300;
        [Tooltip("The model (every slot but Eyes).")]
        public GameObject prefab;
        [Tooltip("Eyes: the ape's eye texture it swaps in.")]
        public Texture2D eyeTexture;
        [Tooltip("Eyes: how big the eyeballs are (1 = the ape's own).")]
        public float eyeScale = 1f;
        [Tooltip("Eyes: how far the eyeballs are turned out from straight ahead, each to its own side, in degrees (0 = as the ape's own).")]
        public float eyeTurn;
        [Tooltip("Eyes: a material of its own, in place of the ape's (the VIP's glowing yellow eyes). None: eyeTexture on the ape's own material.")]
        public Material eyeMaterial;
        [Tooltip("Owned by whoever owns this other cosmetic (by name) too. The VIP's eyes, split off its suit on 2026-11-07, come with the suit, so whoever had VIP has them.")]
        public string comesWith;
        [Tooltip("Worn with this skin (by name) and never on its own: not in the shop, the GILLA-LAB or its bundle's list (the VIP's tie, part of the suit since 2026-11-07).")]
        public string wornWith;
        [Tooltip("Not for sale: any more (its bundle was scrapped), or ever (it's given: the Discord's samurai hat). It keeps its number, and whoever has it keeps it.")]
        public bool retired;
        [Tooltip("Face: worn, the mouth doesn't move with the voice (a pacifier). The eyes carry on.")]
        public bool shutsMouth;
    }

    [Serializable]
    public class Bundle
    {
        public string name;
        [Tooltip("Its name on the shop's screens.")]
        public string label;
        [Tooltip("Its cosmetics, by name.")]
        public string[] items = new string[0];
        [Tooltip("What the lot cost in coins, before every bundle was for real money (2026-11-06): kept, but nothing charges it.")]
        public int price = 5000;
        [Tooltip("Coins that come with it (the VIP bundle's 1,000), once.")]
        public int coins;
        [Tooltip("Bought with real money (Meta's in-app purchases), not coins.")]
        public bool realMoney;
        [Tooltip("Its add-on on Meta's Developer Dashboard (real money), exactly: the server's too.")]
        public string sku;
        [Tooltip("What its price says in the editor, which has no Meta. In the game it's Meta's word, in the player's own money.")]
        public string usd;
        [Tooltip("A name tag that comes with it: NameTitles' item for it (VIP's).")]
        public string tag;
        [Tooltip("For good, once its add-on's yours: every XP half as much again, halves rounded up (VIP's: the user, 2026-11-06, \"1.5x permanent xp boost\"). The server goes by the add-on's SKU in your account: GillaLevel.ForYou shows the same.")]
        public bool xpBoost;
        [Tooltip("For good, once its add-on's yours: this many daily coins in place of the usual 100 (VIP's 150). 0: the usual.")]
        public int dailyCoins;
    }

    public Entry[] cosmetics = new Entry[0];
    public Bundle[] bundles = new Bundle[0];
    private static CosmeticCatalog loaded;

    public static int Count
    {
        get
        {
            if (Get() != null)
            {
                return Get().cosmetics.Length;
            }
            return 0;
        }
    }

    public static int BundleCount
    {
        get
        {
            if (Get() == null) return 0;
            if (Get().bundles == null) return 0;
            return Get().bundles.Length;
        }
    }

    public static CosmeticCatalog Get()
    {
        if (loaded == null)
        {
            loaded = Resources.Load<CosmeticCatalog>("CosmeticCatalog");
        }
        return loaded;
    }

    public static Entry Of(int id)
    {
        CosmeticCatalog c = Get();
        bool ok = c != null;
        Entry result = null;
        if (id > 0 && ok)
        {
            if (c.cosmetics.Length < id) result = null;
            else result = c.cosmetics[id - 1];
        }
        return result;
    }

    public static byte IdOf(string name)
    {
        CosmeticCatalog c = Get();
        if (c == null) return 0;
        for (int i = 0; i < c.cosmetics.Length; i++)
        {
            if (c.cosmetics[i].name == name)
            {
                return (byte)(i + 1);
            }
        }
        return 0;
    }

    public static Bundle BundleAt(int i)
    {
        if (i >= 0 && i < BundleCount)
        {
            return Get().bundles[i];
        }
        return null;
    }

    public static Bundle BundleNamed(string name)
    {
        for (int i = 0; i < BundleCount; i++)
        {
            if (Get().bundles[i].name == name)
            {
                return Get().bundles[i];
            }
        }
        return null;
    }

    public static Bundle BundleWithSku(string sku)
    {
        for (int i = 0; i < BundleCount; i++)
        {
            if (Get().bundles[i].sku == sku)
            {
                return Get().bundles[i];
            }
        }
        return null;
    }

    public static List<byte> IdsOf(Bundle b)
    {
        List<byte> ids = new List<byte>();
        if (b != null)
        {
            for (int i = 0; i < b.items.Length; i++)
            {
                byte id = IdOf(b.items[i]);
                if (id != 0)
                {
                    ids.Add(id);
                }
            }
        }
        return ids;
    }

    public static List<Entry> WornWith(string skin)
    {
        List<Entry> list = new List<Entry>();
        CosmeticCatalog c = Get();
        if (c != null && !string.IsNullOrEmpty(skin))
        {
            Entry[] arr = c.cosmetics;
            for (int i = 0; i < arr.Length; i++)
            {
                Entry item = arr[i];
                if (item != null && item.wornWith == skin && item.prefab != null)
                {
                    list.Add(item);
                }
            }
        }
        return list;
    }

    public static GameObject Display(Entry e)
    {
        if (e == null) return null;
        if (e.prefab == null) return null;
        GameObject go = UnityEngine.Object.Instantiate<GameObject>(e.prefab);
        go.name = e.prefab.name;
        if (e.slot == CosmeticSlot.Toy)
        {
            MonoBehaviour[] behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour mb = behaviours[i];
                if (!(mb is CosmeticMotion) && !(mb is CosmeticFloppy))
                {
                    mb.enabled = false;
                }
            }
        }
        else if (e.slot == CosmeticSlot.Skin)
        {
            foreach (Transform child in go.transform)
            {
                if (child.name != "Display")
                {
                    child.gameObject.SetActive(false);
                }
            }
            MonoBehaviour[] behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }
        }
        ParticleSystem[] particles = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].gameObject.SetActive(false);
        }
        TrailRenderer[] trails = go.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            trails[i].enabled = false;
        }
        Light[] lights = go.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = false;
        }
        AudioSource[] sources = go.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i].enabled = false;
        }
        return go;
    }
}
