// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Cosmetics
{
	public const int Slots = 8;
	private static readonly byte[] worn = new byte[8];
	private static bool loaded;

	public static byte Head
	{
		get { return In(CosmeticSlot.Head); }
	}

	public static byte Face
	{
		get { return In(CosmeticSlot.Face); }
	}

	public static string WornNames
	{
		get
		{
			Load();
			List<string> names = new List<string>();
			for (int i = 0; i < worn.Length; i++)
			{
				byte id = worn[i];
				if (CosmeticCatalog.Of(id) != null)
				{
					names.Add(CosmeticCatalog.Of(id).name);
				}
			}
			if (names.Count > 0)
			{
				return string.Join(",", names);
			}
			return "none";
		}
	}

	public static byte In(CosmeticSlot slot)
	{
		Load();
		return worn[(int)slot];
	}

	public static void Wear(string name)
	{
		Wear(CosmeticCatalog.IdOf(name));
	}

	public static void Wear(byte id)
	{
		var entry = CosmeticCatalog.Of(id);
		if (entry != null)
		{
			Set(entry.slot, id);
		}
	}

	public static void TakeOff(CosmeticSlot slot)
	{
		Set(slot, 0);
	}

	public static bool IsWearing(byte id)
	{
		Load();
		if (id == 0)
		{
			return false;
		}
		return Array.IndexOf<byte>(worn, id) >= 0;
	}

	private static void Set(CosmeticSlot slot, byte id)
	{
		Load();
		worn[(int)slot] = id;
		PlayerPrefs.SetInt(Key(slot), id);
		PlayerPrefs.Save();
		GillaCloud.ProfileChanged();
	}

	public static void Adopt(string[] names)
	{
		Load();
		Array.Clear(worn, 0, 8);
		for (int i = 0; i < names.Length; i++)
		{
			byte id = CosmeticCatalog.IdOf(names[i]);
			var entry = CosmeticCatalog.Of(id);
			if (entry != null && string.IsNullOrEmpty(entry.wornWith))
			{
				worn[(int)entry.slot] = id;
			}
		}
		for (int i = 0; i < 8; i++)
		{
			PlayerPrefs.SetInt(Key((CosmeticSlot)i), worn[i]);
		}
		PlayerPrefs.Save();
	}

	private static string Key(CosmeticSlot slot)
	{
		return "Gilla.Cosmetic." + slot.ToString();
	}

	private static void Load()
	{
		if (loaded)
		{
			return;
		}
		loaded = true;
		for (int i = 0; i < 8; i++)
		{
			int id = PlayerPrefs.GetInt(Key((CosmeticSlot)i), 0);
			var entry = CosmeticCatalog.Of((byte)id);
			byte value;
			if (entry == null || i != (int)entry.slot)
			{
				value = 0;
			}
			else
			{
				if (!string.IsNullOrEmpty(entry.wornWith))
				{
					id = 0;
				}
				value = (byte)id;
			}
			worn[i] = value;
		}
	}
}
