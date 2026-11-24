// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaHover : MonoBehaviour
{
	[Tooltip("How far it goes up and down from where it was put, metres.")]
	public float height = 0.02f;
	[Tooltip("Seconds for one bob up and down.")]
	public float period = 3.5f;
	[Tooltip("Degrees a second it turns about its up as it floats (0: it doesn't).")]
	public float spin;
	[Tooltip("Degrees it swings to and fro about its forward, as a hung tag (0: it doesn't), and how fast (radians a second).")]
	public float sway;
	[Tooltip("Degrees it swings to and fro about its forward, as a hung tag (0: it doesn't), and how fast (radians a second).")]
	public float swaySpeed = 2f;
	[Tooltip("A slow drift as it floats (Animal Company's bundle animals): degrees it rocks about its X, Y and Z, each at its own pace.")]
	public Vector3 drift;
	private Vector3 rest;
	private Quaternion restTurn;
	private float turned;

	private void Awake()
	{

		rest = transform.localPosition;
		restTurn = transform.localRotation;
	}

	private void Update()
	{
		Transform t = transform;
		t.localPosition = rest + Vector3.up * (height * Mathf.Sin(Time.time * 2f * Mathf.PI / period));
		if (spin == 0f && sway == 0f && drift == Vector3.zero)
		{
			return;
		}
		turned = Mathf.Repeat(turned + spin * Time.deltaTime, 360f);
		float time = Time.time;
		Quaternion wobble = Quaternion.Euler(
			drift.x * Mathf.Sin(time * 0.67f),
			drift.y * Mathf.Sin(time * 0.43f + 1.1f),
			drift.z * Mathf.Sin(time * 0.81f + 2.3f));
		Quaternion swing = Quaternion.Euler(0f, turned, sway * Mathf.Sin(time * swaySpeed));
		transform.localRotation = restTurn * swing * wobble;
	}
}
