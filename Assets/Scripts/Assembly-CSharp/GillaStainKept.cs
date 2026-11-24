// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaStainKept : MonoBehaviour
{
	public GillaStains.Sheet sheet;

	private void OnDestroy()
	{
		if (sheet != null)
		{
			sheet.users--;
		}
	}
}
