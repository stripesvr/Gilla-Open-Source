// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class FollowRotation : MonoBehaviour
{
	public Transform source;

	private void LateUpdate()
	{
		if (source != null)
		{
			transform.rotation = source.rotation;
		}
	}
}
