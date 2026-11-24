// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaFireGlow : MonoBehaviour
{
	public Light lamp;
	public Renderer embers;
	[ColorUsage(false, true)]
	public Color glow = new Color(3f, 1.1f, 0.3f, 1f);
	public float seed = 1f;
	private MaterialPropertyBlock block;
	private float baseIntensity;

	private void Start()
	{
		block = new MaterialPropertyBlock();
		baseIntensity = lamp != null ? lamp.intensity : 1f;
	}

	private void Update()
	{
		float t = Time.time;
		float slow = Mathf.PerlinNoise(seed, t * 1.1f);
		float fast = Mathf.PerlinNoise(seed + 7.3f, t * 6.5f);
		float k = slow * 0.2f + 0.74f + fast * 0.12f;
		if (lamp != null)
		{
			lamp.intensity = k * baseIntensity;
		}
		if (embers != null)
		{
			embers.GetPropertyBlock(block);
			block.SetColor("_BaseColor", glow * k);
			embers.SetPropertyBlock(block);
		}
	}
}
