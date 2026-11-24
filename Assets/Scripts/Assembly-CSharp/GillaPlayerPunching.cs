// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public static class GillaPlayerPunching
{
	private const string Key = "Gilla.PlayerPunching";
	private static bool loaded;
	private static bool on = true;

	public static bool On
	{
		get
		{
			Load();
			return on;
		}
		set
		{
			Load();
			on = value;
			PlayerPrefs.SetInt(Key, value ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	private static void Load()
	{
		if (!loaded)
		{
			loaded = true;
			on = PlayerPrefs.GetInt(Key, 1) != 0;
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Forget()
	{
		loaded = false;
	}
}
