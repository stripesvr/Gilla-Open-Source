// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public static class GillaMutes
{
	private const string Key = "Gilla.Muted";
	private const int MostKept = 200;
	private static List<string> ids;
	private static HashSet<string> set;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Forget()
	{
		ids = null;
		set = null;
	}

	private static void Load()
	{
		if (set == null)
		{
			ids = new List<string>();
			set = new HashSet<string>();
			string[] parts = PlayerPrefs.GetString(Key, "").Split(',');
			for (int i = 0; i < parts.Length; i++)
			{
				string s = parts[i];
				if (s.Length > 0)
				{
					if (set.Add(s))
					{
						ids.Add(s);
					}
				}
			}
		}
	}

	public static bool IsMuted(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		Load();
		return set.Contains(id);
	}

	public static bool Toggle(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		Load();
		bool wasMuted = set.Contains(id);
		if (wasMuted)
		{
			set.Remove(id);
			ids.Remove(id);
		}
		else
		{
			set.Add(id);
			ids.Add(id);
			while (ids.Count > MostKept)
			{
				set.Remove(ids[0]);
				ids.RemoveAt(0);
			}
		}
		PlayerPrefs.SetString(Key, string.Join(",", ids));
		PlayerPrefs.Save();
		return !wasMuted;
	}
}
