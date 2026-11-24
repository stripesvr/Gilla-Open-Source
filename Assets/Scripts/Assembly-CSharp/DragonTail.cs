// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[DefaultExecutionOrder(210)]
public class DragonTail : MonoBehaviour
{
	[Tooltip("Its segments, root to tip, each one the child of the one before.")]
	public Transform[] segments = new Transform[0];
	[Tooltip("The swish: degrees at the tip, and swishes a second, standing still; going fast it's this much more.")]
	public float swish = 14f;
	[Tooltip("The swish: degrees at the tip, and swishes a second, standing still; going fast it's this much more.")]
	public float swishRate = 0.7f;
	[Tooltip("The swish: degrees at the tip, and swishes a second, standing still; going fast it's this much more.")]
	public float fastBoost = 1.6f;
	[Tooltip("How much it drags behind as you move (degrees per m/s), and the most.")]
	public float drag = 7f;
	[Tooltip("How much it drags behind as you move (degrees per m/s), and the most.")]
	public float maxDrag = 35f;
	private Quaternion[] rest;
	private Vector3 lastPos;
	private Vector3 vel;
	private float phase;
	private bool started;

	private void Awake()
	{

		rest = new Quaternion[segments.Length];
		for (int i = 0; i < segments.Length; i++)
		{
			if (segments[i] != null)
			{
				rest[i] = segments[i].localRotation;
			}
		}
	}

	private void OnEnable()
	{

		started = false;
	}

	private void LateUpdate()
	{
		float dt = Time.deltaTime;
		if (dt <= 0f) return;
		if (segments.Length == 0) return;

		Vector3 pos = transform.position;
		if (!started || (pos - lastPos).sqrMagnitude > 25f)
		{
			lastPos = pos;
			started = true;
			vel = Vector3.zero;
		}
		Vector3 delta = pos - lastPos;
		float k = Mathf.Clamp01(1f - Mathf.Exp(dt * -6f));
		vel = vel + (delta / dt - vel) * k;
		lastPos = pos;

		float fast = Mathf.Clamp01(vel.magnitude * 0.25f);
		phase += dt * swishRate * Mathf.PI * 2f * (fast * fastBoost + 1f);

		Vector3 local = transform.InverseTransformDirection(vel);
		float side = Mathf.Clamp(-(local.x * drag), -maxDrag, maxDrag);
		float up = Mathf.Clamp(local.y * drag * 0.5f, -maxDrag, maxDrag);

		int n = segments.Length;
		if (n > 0)
		{
			float fn = n;
			for (int i = 0; i < n; i++)
			{
				if (segments[i] == null) continue;
				float s = Mathf.Sin(phase + i * -0.55f);
				float sw = ((i + 1f) / fn) * (fast + 1f) * swish * s / fn;
				Quaternion bend = Quaternion.Euler(up / fn, 0f, side / fn + sw + sw);
				segments[i].localRotation = rest[i] * bend;
			}
		}
	}
}
