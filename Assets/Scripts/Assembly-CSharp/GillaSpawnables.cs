// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaSpawnables : ScriptableObject
{
	public GameObject dynamite;
	public GameObject dummy;
	public GameObject egg;
	public GameObject bearTrap;
	public GameObject pickaxe;
	private static GillaSpawnables loaded;

	public static GillaSpawnables Get()
	{
		if (loaded == null)
		{
			loaded = Resources.Load<GillaSpawnables>("GillaSpawnables");
		}
		return loaded;
	}
}
