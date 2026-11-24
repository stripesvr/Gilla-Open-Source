// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VoiceBoost : MonoBehaviour
{
	[Tooltip("How many times louder: 2.5 is about +8 dB, 4 about +12 dB.")]
	[Range(1, 6)]
	public float gain = 4f;
	private const float Knee = 0.6f;

	private void OnAudioFilterRead(float[] data, int channels)
	{
		for (int i = 0; i < data.Length; i++)
		{
			float s = gain * data[i];
			float result = s;
			if (Mathf.Abs(s) > Knee)
			{
				float shaped = (float)Math.Tanh((Mathf.Abs(s) - Knee) / (1f - Knee)) * (1f - Knee) + Knee;
				result = s >= 0f ? shaped : -shaped;
			}
			data[i] = result;
		}
	}
}
