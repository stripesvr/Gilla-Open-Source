// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using PlayFab;

public partial class GillaCloud : MonoBehaviour
{
	private void SaveProfile_b__73_1(PlayFabError error)
	{
		profileUnsaved = true;
		nextProfile = Time.unscaledTime + 20f;
		Debug.LogWarning("[GillaCloud] couldn't save what you wear: " + error.GenerateErrorReport());
	}
}
