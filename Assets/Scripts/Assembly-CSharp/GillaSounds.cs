// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gilla/Sounds")]
public class GillaSounds : ScriptableObject
{
	[Serializable]
	public class SurfaceSet
	{
		public SurfaceKind kind;
		public AudioClip[] clips = new AudioClip[0];
		[Range(0, 2)]
		public float volume = 1f;
	}

	public SurfaceSet[] surfaces = new SurfaceSet[0];
	[Tooltip("A key going down (PressKey), a different one each time if there's more than one (Assets/Audio/Keys/Gilla_Key_1-8).")]
	public AudioClip[] keyPresses = new AudioClip[0];
	[Range(0, 1)]
	public float keyPressVolume = 0.6f;
	private static GillaSounds loaded;

	public static GillaSounds Get()
	{
		if (loaded == null)
		{
			loaded = Resources.Load<GillaSounds>("GillaSounds");
		}
		return loaded;
	}

	public SurfaceSet For(SurfaceKind kind)
	{
		SurfaceSet fallback = null;
		foreach (SurfaceSet set in surfaces)
		{
			if (set != null && set.clips != null && set.clips.Length != 0)
			{
				if (set.kind == kind)
				{
					return set;
				}
				if (set.kind == SurfaceKind.Concrete)
				{
					fallback = set;
				}
			}
		}
		return fallback;
	}
}
