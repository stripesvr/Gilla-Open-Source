// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AmbientLoop : MonoBehaviour
{
	private void OnEnable()
	{
		AudioSource source = GetComponent<AudioSource>();
		if (source.clip == null)
		{
			return;
		}
		source.loop = true;
		source.time = Random.Range(0f, source.clip.length);
		source.Play();
	}
}
