// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public static class GillaEars
{
	private static AudioListener last;

	public static AudioListener Listening
	{
		get
		{
			if (last != null && last.isActiveAndEnabled)
			{
				return last;
			}
			last = null;
			AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
			for (int i = 0; i < listeners.Length; i++)
			{
				AudioListener listener = listeners[i];
				if (listener.isActiveAndEnabled)
				{
					last = listener;
					break;
				}
			}
			return last;
		}
	}
}
