// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public static class GillaName
{
	public const int MaxLength = 20;
	private static string meta;

	public static event Action Changed;

	public static string Current
	{
		get
		{
			if (!string.IsNullOrEmpty(meta))
			{
				return meta;
			}
			return Default;
		}
	}

	public static bool FromMeta
	{
		get
		{
			return !string.IsNullOrEmpty(meta);
		}
	}

	public static string Default
	{
		get
		{
			int code = 0;
			string id = NetworkBootstrap.PlayerId;
			if (id.Length < 1)
			{
				code = 0;
			}
			else
			{
				uint hash = 0;
				for (int i = 0; i < id.Length; i++)
				{
					hash = (hash * 31 + (uint)id[i]) & 0x7fffffff;
				}
				code = (int)(hash % 10000);
			}
			return "GILLA" + code.ToString("0000");
		}
	}

	public static void SetMeta(string username)
	{
		string name = (username ?? "").Trim();
		if (name.Length > MaxLength)
		{
			name = name.Substring(0, MaxLength);
		}
		if (name != meta)
		{
			meta = name;
			Changed?.Invoke();
		}
	}

	[RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
	private static void Forget()
	{
		meta = null;
	}
}
