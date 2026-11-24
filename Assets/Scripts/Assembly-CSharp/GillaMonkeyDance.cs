// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaMonkeyDance : MonoBehaviour
{
	[Tooltip("The song he bumps his head to: where it is says where he is in the beat.")]
	public GillaTvSong song;
	[Tooltip("The song's beats a minute.")]
	public float bpm = 117f;
	[Tooltip("Where in the song its first beat falls (s).")]
	public float firstBeat = 0.511f;
	[Tooltip("How far his head bumps down on the beat (degrees).")]
	public float nod = 16f;
	[Tooltip("How far his head tips side to side over two beats (degrees).")]
	public float tip = 4f;
	[Tooltip("How much of the bump his chest takes (degrees).")]
	public float chest = 2.5f;
	private Transform spine;
	private Transform neck;
	private Transform head;
	private Transform[] posed;
	private Quaternion[] rest;
	private Vector3 leftLocal;
	private Vector3 aheadLocal;
	private bool ready;

	public float Beat
	{
		get
		{
			float t;
			if (song != null && song.Length > 0f)
			{
				t = song.SongTime;
			}
			else
			{
				t = Time.time;
			}
			return (t - firstBeat) * bpm / 60f;
		}
	}

	private void Awake()
	{

		Transform[] all = GetComponentsInChildren<Transform>(true);
		Transform armL = null;
		Transform armR = null;
		Transform eyeL = null;
		Transform eyeR = null;
		for (int i = 0; i < all.Length; i++)
		{
			Transform t = all[i];
			switch (t.name)
			{
				case "head": head = t; break;
				case "neck": neck = t; break;
				case "spine": spine = t; break;
				case "upperarm_L": armL = t; break;
				case "upperarm_R": armR = t; break;
				case "eye_L": eyeL = t; break;
				case "eye_R": eyeR = t; break;
			}
		}

		ready = spine != null && neck != null && head != null && armL != null && armR != null;
		if (!ready)
		{
			Debug.LogWarning("[MonkeyDance] " + name + " hasn't the ape's bones to bump his head with");
			return;
		}

		posed = new Transform[] { spine, neck, head };
		rest = new Quaternion[posed.Length];
		for (int i = 0; i < posed.Length; i++)
		{
			rest[i] = posed[i].localRotation;
		}

		Vector3 up = (neck.position - spine.position).normalized;
		Vector3 left = Vector3.ProjectOnPlane(armL.position - armR.position, up).normalized;
		Vector3 ahead;
		if (eyeL != null && eyeR != null)
		{
			ahead = (eyeL.position + eyeR.position) * 0.5f - head.position;
		}
		else
		{
			ahead = transform.forward;
		}
		ahead = Vector3.ProjectOnPlane(ahead, up);
		ahead = Vector3.ProjectOnPlane(ahead, left).normalized;
		leftLocal = transform.InverseTransformDirection(left);
		aheadLocal = transform.InverseTransformDirection(ahead);
	}

	public static float Bump(float beat)
	{
		float c = Mathf.Cos((beat - (int)beat) * 6.2831855f) * 0.5f + 0.5f;
		return c * c;
	}

	private void LateUpdate()
	{
		if (!ready)
		{
			return;
		}
		float beat = Beat;
		float bump = Bump(beat);
		for (int i = 0; i < posed.Length; i++)
		{
			posed[i].localRotation = rest[i];
		}
		Vector3 right = -transform.TransformDirection(leftLocal);
		Vector3 ahead = transform.TransformDirection(aheadLocal);
		spine.rotation = Quaternion.AngleAxis(bump * chest, right) * spine.rotation;
		neck.rotation = Quaternion.AngleAxis(bump * nod * 0.4f, right) * neck.rotation;
		head.rotation = Quaternion.AngleAxis(Mathf.Sin(beat * Mathf.PI) * tip, ahead) * Quaternion.AngleAxis(bump * nod * 0.6f, right) * head.rotation;
	}
}
