// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class GillaWoundBend : MonoBehaviour
{
	private static readonly int OtherBone = Shader.PropertyToID("_OtherBone");
	public Transform other;
	public Matrix4x4 bind;
	private Renderer shown;
	private MaterialPropertyBlock block;

	private void LateUpdate()
	{
		Bend();
	}

	public void Bend()
	{
		if (shown == null)
		{
			shown = GetComponent<Renderer>();
		}
		if (shown == null)
		{
			return;
		}
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		Matrix4x4 m;
		if (other != null)
		{
			m = other.localToWorldMatrix * bind;
		}
		else
		{
			m = transform.localToWorldMatrix;
		}
		block.SetMatrix(OtherBone, m);
		shown.SetPropertyBlock(block);
	}
}
