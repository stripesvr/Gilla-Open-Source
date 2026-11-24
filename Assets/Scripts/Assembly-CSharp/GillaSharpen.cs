// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public static class GillaSharpen
{
	public const float Strength = 0.75f;
	private static readonly int Id = Shader.PropertyToID("_GillaSharpen");

	public static void Set(float strength)
	{
		Shader.SetGlobalFloat(Id, strength);
	}

	[RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)1)]
	private static void Boot()
	{
		Shader.SetGlobalFloat(Id, 0.75f);
	}
}
