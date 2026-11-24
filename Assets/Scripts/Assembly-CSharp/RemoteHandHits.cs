// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class RemoteHandHits : MonoBehaviour
{
	public Transform left;
	public Transform right;
	[Tooltip("How near a surface a hand counts as on it (m). The locomotion keeps hands 0.05 m off.")]
	public float reach = 0.075f;
	[Tooltip("Slower than this (m/s) and a hand settling on something makes no sound.")]
	public float minSpeed = 0.4f;
	[Tooltip("Hand speed (m/s) for a full-volume hit.")]
	public float hardHitSpeed = 3f;
	public float volume = 0.7f;
	[Tooltip("Ignore re-touches this soon after the last one (hands sliding along a wall).")]
	public float minInterval = 0.08f;
	[Tooltip("A touch only counts if the hand was off everything at least this long first (s): a hand resting on the floor jitters in and out of reach.")]
	public float minAirTime = 0.1f;
	private const int Solid = -5893;
	private AudioSource[] sources;
	private int next;
	private bool wasL;
	private bool wasR;
	private bool started;
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
		sources = new AudioSource[3];
		for (int i = 0; i < sources.Length; i++)
		{
			GameObject go = new GameObject("HandHit" + i.ToString());
			go.transform.SetParent(transform, false);
			AudioSource src = go.AddComponent<AudioSource>();
			src.playOnAwake = false;
			src.spatialBlend = 1f;
			src.minDistance = 0.6f;
			src.maxDistance = 15f;
			src.rolloffMode = AudioRolloffMode.Linear;
			src.dopplerLevel = 0f;
			sources[i] = src;
		}
	}

	private void LateUpdate()
	{
		if (left == null || right == null || sources == null)
		{
			return;
		}
		float dt = Time.deltaTime;
		if (!started)
		{
			lastL = left.position;
			lastR = right.position;
			started = true;
			return;
		}
		dt = Mathf.Max(dt, 0.0001f);
		coolL -= dt;
		coolR -= dt;
		Hand(left.position, ref wasL, ref lastL, ref coolL, ref offL, ref speedL, dt);
		Hand(right.position, ref wasR, ref lastR, ref coolR, ref offR, ref speedR, dt);
	}

	private void Hand(Vector3 pos, ref bool was, ref Vector3 last, ref float cool, ref float off, ref float lastSpeed, float dt)
	{
		Vector3 delta = pos - last;
		float prevSpeed = lastSpeed;
		last = pos;
		float speed = delta.magnitude / dt;
		lastSpeed = speed;
		float hitSpeed = Mathf.Max(speed, prevSpeed);
		bool touching;
		if (speed <= 30f)
		{
			float r = reach * Mathf.Max(transform.lossyScale.y, 0.2f);
			var surface = Surfaces.Under(pos, r, Solid);
			touching = surface != null;
			if (touching)
			{
				if (!was && cool <= 0f && minAirTime <= off && minSpeed <= hitSpeed)
				{
					AudioSource src = sources[next];
					next = (next + 1) % sources.Length;
					src.transform.position = pos;
					float t = hitSpeed / hardHitSpeed;
					float k;
					if (t < 0f)
					{
						k = 0.35f;
					}
					else if (t > 1f)
					{
						k = 1f;
					}
					else
					{
						k = t * 0.65f + 0.35f;
					}
					Surfaces.Play(src, surface, volume * k, 0.92f, 1.08f);
					cool = minInterval;
				}
				off = 0f;
			}
			else
			{
				off = off + dt;
			}
		}
		else
		{
			touching = false;
		}
		was = touching;
	}
}
