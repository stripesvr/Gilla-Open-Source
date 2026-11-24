// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaSplashGate : MonoBehaviour
{
	[Tooltip("The doors (they go up by `rise`).")]
	public Transform[] doors = new Transform[0];
	[Tooltip("What stops you while they're down.")]
	public Collider block;
	public float rise = 2.7f;
	[Tooltip("Seconds to go all the way up or down.")]
	public float seconds = 1.1f;
	public AudioClip[] moveSounds;
	private Vector3[] rest;
	private float open;
	private bool want;

	public bool Open
	{
		get { return open >= 1f; }
	}

	private void Awake()
	{

		rest = new Vector3[doors.Length];
		for (int i = 0; i < doors.Length; i++)
		{
			if (doors[i] != null)
			{
				rest[i] = doors[i].localPosition;
			}
		}
	}

	public void Set(bool up)
	{
		if (want != up)
		{
			want = up;
			if (moveSounds != null && moveSounds.Length != 0)
			{
				Sfx.Play(moveSounds, transform.position, 0.8f, 0.95f, 1.05f);
			}
		}
	}

	private void Update()
	{
		float target = want ? 1f : 0f;
		open = Mathf.MoveTowards(open, target, Time.deltaTime / Mathf.Max(0.05f, seconds));
		float t = open;
		for (int i = 0; i < doors.Length; i++)
		{
			if (doors[i] != null)
			{
				doors[i].localPosition = rest[i] + Vector3.up * (t * t * (3f - 2f * t) * rise);
			}
		}
		if (block != null)
		{
			block.enabled = open < 0.9f;
		}
	}
}
