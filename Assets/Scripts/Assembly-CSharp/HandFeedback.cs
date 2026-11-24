// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.XR;
using GorillaLocomotion;

[DefaultExecutionOrder(50)]
[RequireComponent(typeof(Player))]
public class HandFeedback : MonoBehaviour
{
	public AudioClip footstep;
	[Range(0, 1)]
	public float volume = 0.7f;
	[Tooltip("Random pitch range around 1, so the same sound never repeats exactly.")]
	public float pitchMin = 0.88f;
	[Tooltip("Random pitch range around 1, so the same sound never repeats exactly.")]
	public float pitchMax = 1.12f;
	[Tooltip("Hand speed (m/s) for a full-volume, full-strength hit.")]
	public float hardHitSpeed = 3f;
	[Tooltip("Ignore re-touches this soon after the last one (hands sliding along a wall).")]
	public float minInterval = 0.08f;
	[Tooltip("A touch only counts if the hand was off everything at least this long first (s).")]
	public float minAirTime = 0.1f;
	[Tooltip("...and was moving at least this fast (m/s) as it came down.")]
	public float minSpeed = 0.35f;
	[Header("Vibration")]
	[Range(0, 1)]
	public float hapticMin = 0.12f;
	[Range(0, 1)]
	public float hapticMax = 0.6f;
	public float hapticSeconds = 0.05f;
	private Player gt;
	private AudioSource[] sources;
	private int next;
	private bool wasL;
	private bool wasR;
	private Vector3 lastL;
	private Vector3 lastR;
	private float coolL;
	private float coolR;
	private float offL;
	private float offR;
	private float speedL;
	private float speedR;

	private void Start()
	{
		gt = GetComponent<Player>();
		sources = new AudioSource[4];
		for (int i = 0; i < sources.Length; i++)
		{
			GameObject go = new GameObject("HandStep" + i.ToString());
			go.transform.SetParent(transform, false);
			AudioSource src = go.AddComponent<AudioSource>();
			src.playOnAwake = false;
			src.spatialBlend = 1f;
			src.minDistance = 0.6f;
			src.maxDistance = 15f;
			src.rolloffMode = AudioRolloffMode.Linear;
			sources[i] = src;
		}
		lastL = gt.leftHandFollower.position;
		lastR = gt.rightHandFollower.position;
	}

	private void LateUpdate()
	{
		float dt = Time.deltaTime;
		if (dt <= 0.0001f)
		{
			dt = 0.0001f;
		}
		coolL -= dt;
		coolR -= dt;
		Step(gt.leftHandFollower, gt.wasLeftHandTouching, ref wasL, ref lastL, ref coolL, ref offL, ref speedL, XRNode.LeftHand, dt);
		Step(gt.rightHandFollower, gt.wasRightHandTouching, ref wasR, ref lastR, ref coolR, ref offR, ref speedR, XRNode.RightHand, dt);
	}

	private void Step(Transform hand, bool touching, ref bool was, ref Vector3 last, ref float cool, ref float off, ref float lastSpeed, XRNode node, float dt)
	{
		Vector3 at = hand.position;
		Vector3 delta = at - last;
		float prevSpeed = lastSpeed;
		last = at;
		float speed = delta.magnitude / dt;
		lastSpeed = speed;
		speed = System.Math.Max(speed, prevSpeed);
		float newOff;
		if (touching)
		{
			newOff = 0f;
			if (!was && cool <= 0f && minAirTime <= off && minSpeed <= speed)
			{
				float strength = Mathf.Clamp01(speed / hardHitSpeed);
				Collider surface = Surfaces.Under(at, gt.minimumRaycastDistance + 0.03f, gt.locomotionEnabledLayers.value);
				Play(at, surface, Mathf.Clamp01(strength) * 0.65f + 0.35f);
				InputDevice device = InputDevices.GetDeviceAtXRNode(node);
				if (device.isValid)
				{
					device.SendHapticImpulse(0, hapticMin + (hapticMax - hapticMin) * Mathf.Clamp01(strength), hapticSeconds);
				}
				newOff = 0f;
				cool = minInterval;
			}
		}
		else
		{
			newOff = off + dt;
		}
		off = newOff;
		was = touching;
	}

	private void Play(Vector3 at, Collider surface, float loud)
	{
		AudioSource src = sources[next];
		next = (next + 1) % sources.Length;
		src.transform.position = at;
		if (!Surfaces.Play(src, surface, volume * loud, pitchMin, pitchMax))
		{
			if (footstep != null)
			{
				src.pitch = Random.Range(pitchMin, pitchMax);
				src.PlayOneShot(footstep, volume * loud);
			}
		}
	}
}
