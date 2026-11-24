// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public class GillaAtmosphere : MonoBehaviour
{
	[Serializable]
	public struct Air
	{
		public string name;
		public Bounds box;
		public Color fog;
		public float density;
		[Tooltip("Trilight ambient: what faces up, sideways and down gets.")]
		public Color sky;
		[Tooltip("Trilight ambient: what faces up, sideways and down gets.")]
		public Color equator;
		[Tooltip("Trilight ambient: what faces up, sideways and down gets.")]
		public Color ground;
	}

	public Air indoors;
	public Air[] zones = new Air[0];
	[Tooltip("Seconds to ease most of the way into a new space's air.")]
	public float ease = 0.8f;
	private const float DarkDensity = 0.1f;

	public Air At(Vector3 p)
	{
		for (int i = 0; i < zones.Length; i++)
		{
			Air z = zones[i];
			if (z.box.Contains(p))
			{
				return z;
			}
		}
		return indoors;
	}

	public void ApplyAt(Vector3 p)
	{
		Apply(At(p), 1f);
	}

	private void Start()
	{
		Camera cam = Camera.main;
		Vector3 p = cam != null ? cam.transform.position : transform.position;
		Apply(At(p), 1f);
	}

	private void LateUpdate()
	{
		Camera cam = Camera.main;
		if (cam == null) return;
		Air a = At(cam.transform.position);
		float dt = Time.deltaTime;
		float e = ease;
		if (ease <= 0.01f) e = 0.01f;
		Apply(a, 1f - Mathf.Exp((dt * -3f) / e));
	}

	private static void Apply(Air a, float k)
	{
		float dark = GillaDarkness.Amount;
		RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, Color.Lerp(a.fog, Color.black, dark), k);
		RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, Mathf.Lerp(a.density, DarkDensity, dark), k);
		RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, a.sky, k);
		RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, a.equator, k);
		RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, a.ground, k);
	}
}
