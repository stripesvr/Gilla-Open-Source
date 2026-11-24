// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class Spin : MonoBehaviour
{
	public float degreesPerSecond = 35f;

	private void Update()
	{
		Transform t = transform;
		Quaternion step = Quaternion.Euler(0f, degreesPerSecond * Time.deltaTime, 0f);
		Quaternion rotA = t.rotation;
		Quaternion inverse = Quaternion.Inverse(t.rotation);
		Quaternion rotB = t.rotation;
		t.rotation = rotA * (rotB * (step * inverse));
	}
}
