// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.XR;

public class MonsterAnimEvents : MonoBehaviour
{
	public MonsterBrain brain;
	public AudioClip[] footsteps;
	[Range(0, 1)]
	public float footstepVolume = 1f;
	[Tooltip("Bigger monsters stomp lower: 1 = as recorded.")]
	public float footstepPitch = 1f;
	[Tooltip("How far its footsteps carry (m): full within a couple of metres, about a third by 8 m, a faint thud out near here - and the further off, the more of what reaches you is the building's echo.")]
	public float footstepRange = 45f;
	public AudioClip[] grabSounds;
	[Tooltip("Its jumpscare: screamed in your face the moment it grabs you (KillGrab), loud (Skull3: the user's screams.mp3).")]
	public AudioClip[] jumpscareSounds;
	public float jumpscareVolume = 1f;
	[Tooltip("Its kill has its own cue for the scream (KillScream), later than the grab: Skull3 screams as he holds you up to his face, through the stab and the bite.")]
	public bool screamOnCue;
	public AudioClip[] biteSounds;
	[Tooltip("Claws through your chest (KillStab).")]
	public AudioClip[] stabSounds;
	[Tooltip("You hitting the floor: thrown, or knocked over (KillLand).")]
	public AudioClip[] landSounds;
	[Tooltip("Its claws digging in at a hazard door's foot (the Pry clip's PryScrape).")]
	public AudioClip[] clawSounds;
	[Tooltip("Its palms slamming a shut hazard door before it pries it (the Pry clip's PryBang).")]
	public AudioClip[] bangSounds;
	[Tooltip("Your arm torn off in its kill (KillTear: Gullet grinds your right arm off before it eats you).")]
	public AudioClip[] tearSounds;
	public bool stepsByHand;
	private float lastStep;
	private static AnimationCurve farFall;
	private static AnimationCurve farWet;
	private static AudioListener ears;
	private const int Walls = -5893;
	public const float ScreamVolume = 0.5f;

	public void Footstep(AnimationEvent e)
	{
		if (stepsByHand) return;
		if (e != null)
		{
			if (e.animatorClipInfo.clip != null && e.animatorClipInfo.weight < 0.5f) return;
		}
		Vector3 at = transform.position;
		float volume = footstepVolume;
		float loud = 1f;
		if (brain != null && brain.Hurt != null)
		{
			loud = brain.Hurt.StepLoud();
		}
		Step(at, volume * loud);
	}

	public void Stomp(Vector3 at, float hard)
	{
		float scale;
		if (hard >= 0f)
		{
			scale = 1f;
			if (hard <= 1f) scale = hard * 0.39999998f + 0.6f;
		}
		else
		{
			scale = 0.6f;
		}
		Step(at, footstepVolume * scale);
	}

	private void Step(Vector3 at, float volume)
	{
		if (ears == null) ears = Object.FindAnyObjectByType<AudioListener>();
		if (ears != null)
		{
			if (ShopRooms.Holds(ears.transform.position)) return;
		}
		if (Time.time - lastStep < 0.08f) return;
		lastStep = Time.time;
		AudioSource src = Play(footsteps, volume, footstepPitch * Random.Range(0.92f, 1.06f), 1f);
		if (src == null) return;
		src.transform.position = at;
		if (farFall == null)
		{
			farFall = new AnimationCurve(new Keyframe[]
			{
				new Keyframe(0f, 1f),
				new Keyframe(0.035f, 1f),
				new Keyframe(0.18f, 0.35f),
				new Keyframe(0.45f, 0.12f),
				new Keyframe(0.78f, 0.04f),
				new Keyframe(1f, 0f)
			});
		}
		if (farWet == null)
		{
			farWet = new AnimationCurve(new Keyframe[]
			{
				new Keyframe(0f, 1f),
				new Keyframe(0.25f, 1.1f),
				new Keyframe(1f, 1.1f)
			});
		}
		src.rolloffMode = AudioRolloffMode.Custom;
		src.maxDistance = footstepRange;
		src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, farFall);
		src.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, farWet);
		src.priority = 64;
		if (Muffled(src.transform.position + Vector3.up * 0.3f))
		{
			AudioLowPassFilter lp = src.gameObject.AddComponent<AudioLowPassFilter>();
			lp.cutoffFrequency = 500f;
			src.volume = src.volume * 0.8f;
		}
	}

	private bool Muffled(Vector3 at)
	{
		RaycastHit hit;
		if (ears == null) ears = Object.FindAnyObjectByType<AudioListener>();
		if (ears == null) return false;
		Vector3 start = ears.transform.position;
		Transform self = brain != null ? brain.transform : transform;
		if (Physics.Linecast(start, at, out hit, Walls, QueryTriggerInteraction.Ignore))
		{
			if (!hit.collider.transform.IsChildOf(self)) return true;
		}
		if (!Physics.Linecast(at, start, out hit, Walls, QueryTriggerInteraction.Ignore)) return false;
		return !hit.collider.transform.IsChildOf(self);
	}

	public void KillGrab()
	{
		Play(grabSounds, 1f, Random.Range(0.95f, 1.05f), 6f);
		if (screamOnCue) return;
		KillScream();
	}

	public void KillScream()
	{
		KillCam cam = KillCam.Local;
		if (brain == null) return;
		if (cam == null) return;
		if (cam.Busy && !(cam.Killer != brain))
		{
			AudioSource src = Play(jumpscareSounds, jumpscareVolume * ScreamVolume, 1f, 10f);
			if (src != null)
			{
				src.spatialBlend = 0f;
			}
		}
	}

	public void KillBite()
	{
		Play(biteSounds, 1f, Random.Range(0.9f, 1.05f), 6f);
		if (brain != null)
		{
			brain.OnBite();
		}
	}

	public void KillBlink()
	{
		Play(footsteps, footstepVolume, footstepPitch * Random.Range(0.74f, 0.84f), 8f);
		if (brain != null && KillCam.Local != null)
		{
			if (KillCam.Local.Busy && KillCam.Local.Killer == brain)
			{
				typeof(KillCam).GetField("blinkTill", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.SetValue(KillCam.Local, Time.time + 0.13f); /* ? */
			}
		}
	}

	public void KillGrind()
	{
		Play(footsteps, footstepVolume * 0.55f, footstepPitch * Random.Range(0.5f, 0.6f), 6f);
	}

	public void KillTwist()
	{
		Play(grabSounds, 1f, Random.Range(0.62f, 0.72f), 6f);
		if (brain != null && KillCam.Local != null)
		{
			if (KillCam.Local.Busy && KillCam.Local.Killer == brain)
			{
				KillCam.Local.Twist(95f, 0.13f);
			}
		}
	}

	public void KillNip()
	{
		Play(biteSounds, 0.8f, Random.Range(1.05f, 1.2f), 6f);
		Buzz(XRNode.LeftHand, 0.75f, 0.1f);
		Buzz(XRNode.RightHand, 0.75f, 0.1f);
	}

	public void KillStab()
	{
		Play(stabSounds, 1f, Random.Range(0.7f, 0.8f), 6f);
		Buzz(XRNode.LeftHand, 0.9f, 0.25f);
		Buzz(XRNode.RightHand, 0.9f, 0.25f);
	}

	public void KillTear()
	{
		Play(tearSounds, 1f, Random.Range(0.85f, 0.95f), 6f);
		Buzz(XRNode.RightHand, 1f, 0.4f);
		if (brain != null)
		{
			brain.OnTear();
		}
	}

	public void KillThrow()
	{
		Buzz(XRNode.LeftHand, 0.4f, 0.12f);
		Buzz(XRNode.RightHand, 0.4f, 0.12f);
	}

	public void KillLand()
	{
		Play(landSounds, 1f, Random.Range(0.85f, 0.95f), 6f);
		Buzz(XRNode.LeftHand, 0.7f, 0.15f);
		Buzz(XRNode.RightHand, 0.7f, 0.15f);
	}

	public void PryScrape()
	{
		Play(clawSounds, 1f, Random.Range(0.95f, 1.05f), 6f);
	}

	public void PryBang()
	{
		Play(bangSounds, 1f, Random.Range(0.92f, 1.04f), 15f);
	}

	private AudioSource Play(AudioClip[] clips, float volume, float pitch, float range)
	{
		if (clips != null && clips.Length != 0)
		{
			if (GillaBackroomsSwitch.OnYourSide(transform.position))
			{
				AudioClip clip = clips[Random.Range(0, clips.Length)];
				if (!(clip == null))
				{
					GameObject obj = new GameObject("MonsterSfx_" + clip.name);
					obj.transform.position = transform.position;
					AudioSource src = obj.AddComponent<AudioSource>();
					src.clip = clip;
					src.volume = volume;
					src.pitch = pitch;
					src.spatialBlend = 1f;
					src.minDistance = range;
					src.maxDistance = range * 3f;
					src.rolloffMode = AudioRolloffMode.Linear;
					src.dopplerLevel = 0f;
					src.Play();
					Object.Destroy(obj, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
					return src;
				}
			}
		}
		return null;
	}

	private static void Buzz(XRNode node, float amp, float seconds)
	{
		InputDevice device = InputDevices.GetDeviceAtXRNode(node);
		if (device.isValid)
		{
			device.SendHapticImpulse(0, Mathf.Clamp01(amp), seconds);
		}
	}
}
