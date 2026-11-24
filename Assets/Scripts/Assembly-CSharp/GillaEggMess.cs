// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaEggMess : MonoBehaviour
{
	[Tooltip("How long it lies there (s), and how long it takes to shrink away after.")]
	public float stays = 14f;
	[Tooltip("How long it lies there (s), and how long it takes to shrink away after.")]
	public float shrinks = 1.5f;
	private float t;
	private Vector3 size;

	private void Start()
	{
		size = transform.localScale;
	}

	public void Fling(Vector3 way, float speed)
	{
		Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < bodies.Length; i++)
		{
			Rigidbody rb = bodies[i];
			Vector3 dir = (way + Random.insideUnitSphere * 0.9f).normalized;
			float k = Random.Range(0.45f, 1.1f);
			rb.linearVelocity = dir * speed * k;
			rb.angularVelocity = Random.insideUnitSphere * 18f;
		}
	}

	private void Update()
	{
		t += Time.deltaTime;
		if (t < stays)
		{
			return;
		}
		float f = 1f - (t - stays) / Mathf.Max(shrinks, 0.01f);
		if (f <= 0f)
		{
			Destroy(gameObject, 0f);
			return;
		}
		transform.localScale = f * size;
	}
}
