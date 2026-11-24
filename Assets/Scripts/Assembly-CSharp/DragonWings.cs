// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using System;

[DefaultExecutionOrder(210)]
public class DragonWings : MonoBehaviour
{
	[Tooltip("The wings, hinged at the shoulder blade (left, right), and their outer halves, hinged at the wing's wrist.")]
	public Transform wingL;
	[Tooltip("The wings, hinged at the shoulder blade (left, right), and their outer halves, hinged at the wing's wrist.")]
	public Transform wingR;
	[Tooltip("The wings, hinged at the shoulder blade (left, right), and their outer halves, hinged at the wing's wrist.")]
	public Transform tipL;
	[Tooltip("The wings, hinged at the shoulder blade (left, right), and their outer halves, hinged at the wing's wrist.")]
	public Transform tipR;
	[Tooltip("The left wing folded, as a turn (degrees, on its hinge's axes) from spread; the right's is its mirror.")]
	public Vector3 fold = new Vector3(0f, -70f, -35f);
	public Vector3 tipFold = new Vector3(0f, 120f, 0f);
	[Tooltip("Moving faster than this (m/s) up or down, or along, counts as in the air.")]
	public float airUp = 1.6f;
	[Tooltip("Moving faster than this (m/s) up or down, or along, counts as in the air.")]
	public float airAlong = 4f;
	[Tooltip("The spring the wings open and fold on: how stiff, and how quickly it settles (low: springs past more).")]
	public float stiffness = 170f;
	[Tooltip("The spring the wings open and fold on: how stiff, and how quickly it settles (low: springs past more).")]
	public float damping = 11f;
	[Tooltip("How far (degrees) a beat swings, and how fast (beats a second).")]
	public float beat = 26f;
	[Tooltip("How far (degrees) a beat swings, and how fast (beats a second).")]
	public float beatRate = 2.2f;
	private Quaternion restL;
	private Quaternion restR;
	private Quaternion restTipL;
	private Quaternion restTipR;
	private Vector3 lastPos;
	private Vector3 vel;
	private float open;
	private float openSpeed;
	private float airFor;
	private float groundFor;
	private float phase;
	private float flap;
	private bool started;
	private bool ready;

	private void Awake()
	{

		if (wingL != null) restL = wingL.localRotation;
		if (wingR != null) restR = wingR.localRotation;
		if (tipL != null) restTipL = tipL.localRotation;
		if (tipR != null) restTipR = tipR.localRotation;
		ready = true;
	}

	private void OnEnable()
	{

		started = false;
	}

	private void LateUpdate()
	{
		float dt = Time.deltaTime;
		if (dt <= 0f) return;

		Vector3 pos = transform.position;
		Vector3 delta = pos - lastPos;
		if (!started || delta.sqrMagnitude > 25f)
		{
			lastPos = pos;
			delta = pos - lastPos;
			started = true;
			vel = Vector3.zero;
		}

		float k = Mathf.Clamp01(1f - Mathf.Exp(dt * -10f));
		vel = vel + (delta / dt - vel) * k;
		lastPos = pos;

		float target;
		bool air = Mathf.Abs(vel.y) > airUp || (float)Math.Sqrt(vel.x * vel.x + vel.z * vel.z) > airAlong;
		if (air)
		{
			groundFor = 0f;
			airFor += dt;
			target = 1f;
			if (airFor > 0.05f) goto Spring;
		}
		else
		{
			airFor = 0f;
			groundFor += dt;
		}
		target = (groundFor <= 0.35f && open > 0.5f) ? 1f : 0f;

	Spring:
		float h = Mathf.Min(dt, 0.05f);
		int steps = dt > 0.016666668f ? 2 : 1;
		float n = steps;
		float o = open;
		float s = openSpeed;
		do
		{
			steps--;
			s += (h / n) * (stiffness * (target - o) - s * damping);
			o += (h / n) * s;
		} while (steps != 0);
		open = o;
		openSpeed = s;
		float clamped = Mathf.Clamp(o, -0.1f, 1.25f);
		open = clamped;

		float up = Mathf.Clamp01(vel.y / 3f);
		float down = Mathf.Clamp01(vel.y / -3f);
		phase += (up + 0.6f) * (2f * (dt * beatRate * Mathf.PI));
		float openAmount = Mathf.Clamp01(clamped);
		float f = Mathf.Clamp01(1f - Mathf.Exp(dt * -6f));
		flap += ((up + down * 0.15f + 0.1f) * openAmount - flap) * f;
		float swing = flap * Mathf.Sin(phase) * beat;
		Pose(wingL, tipL, restL, restTipL, 1f, swing);
		Pose(wingR, tipR, restR, restTipR, -1f, swing);
	}

	public void Show(float openness)
	{
		if (!ready) Awake();
		open = openness;
		Pose(wingL, tipL, restL, restTipL, 1f, 0f);
		Pose(wingR, tipR, restR, restTipR, -1f, 0f);
	}

	private void Pose(Transform wing, Transform tip, Quaternion rest, Quaternion tipRest, float side, float swing)
	{
		float shut = 1f - open;
		if (wing != null)
		{
			Vector3 e = new Vector3(shut * fold.x, shut * fold.y * side, side * swing + shut * fold.z * side);
			wing.localRotation = rest * Quaternion.Euler(e);
		}
		if (tip != null)
		{
			Vector3 e = new Vector3(shut * tipFold.x, shut * tipFold.y * side, shut * tipFold.z * side);
			tip.localRotation = tipRest * Quaternion.Euler(e);
		}
	}
}
