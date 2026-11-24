// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class GillaBlast : MonoBehaviour
{
	[Header("Reach (m)")]
	[Tooltip("Anyone this near is blown apart: every wound, both arms off, dead, their body thrown.")]
	public float killRadius = 1.8f;
	[Tooltip("...this near, wounded (two of the punch's wounds within half way out from killRadius, else one) and thrown.")]
	public float hurtRadius = 3.6f;
	[Tooltip("...this near, thrown; the monsters hurt; things lying about thrown too.")]
	public float throwRadius = 7f;
	[Tooltip("How fast it throws you: at throwRadius, and at its heart (m/s).")]
	public Vector2 throwSpeed = new Vector2(2.5f, 15f);
	[Tooltip("How fast it throws the things lying about at its heart (m/s; nothing at throwRadius).")]
	public float thingSpeed = 11f;
	[Tooltip("A monster's harm (MonsterBrain.health): all of it out to half monsterRadius, falling off to nothing at monsterRadius (Animal Company's falloff).")]
	public float monsterDamage = 120f;
	public float monsterRadius = 4.5f;
	[Tooltip("How hard it shoves a monster at its heart (m/s).")]
	public float monsterShove = 7f;
	[Tooltip("Its buzz in your hands reaches this far (m).")]
	public float feltRadius = 16f;
	[Header("Look")]
	[Tooltip("The explosion's cubes: Gilla/VoxelFx moves, colours and thins them out by how old this tells it it is.")]
	public MeshRenderer voxels;
	[Tooltip("They're all gone by this long after it goes off (s), and it isn't drawn after that.")]
	public float voxelSeconds = 2.8f;
	[Tooltip("The flash's light: from flashDelay, its brightness (times its own) over flashSeconds while its reach grows out to flashRange.")]
	public Light flash;
	public AnimationCurve flashCurve = new AnimationCurve(new Keyframe[]
	{
		new Keyframe(0f, 1f),
		new Keyframe(0.297f, 0.38f),
		new Keyframe(1f, 0f)
	});
	public float flashDelay = 0.02f;
	public float flashSeconds = 0.35f;
	public float flashRange = 18.75f;
	[Tooltip("The bang (one at random), heard far off.")]
	public AudioSource boom;
	public AudioClip[] booms;
	[Tooltip("Gone after this long (s): once the bang's died away.")]
	public float lasts = 4.5f;
	public const int Solid = -5893;
	private const int Thrown = -1793;
	private float t;
	private float flashMax;
	private MaterialPropertyBlock block;
	private static readonly int AgeId = Shader.PropertyToID("_Age");
	private static readonly int TurnId = Shader.PropertyToID("_Turn");
	private static readonly ProfilerMarker BlastMark = new ProfilerMarker("Gilla.Blast");
	private const int ShownAtOnce = 5;
	private const float ShownOver = 0.3f;
	private static int shownLately;
	private static float shownFrom = -10f;
	private const int TrapWounds = 3;

	public static void Go(GillaBlast prefab, Vector3 at, Vector3 seen)
	{
		BlastMark.Begin();
		if (Time.time - shownFrom > ShownOver)
		{
			shownFrom = Time.time;
			shownLately = 0;
		}
		shownLately++;
		GillaBlast blast;
		if (shownLately < ShownAtOnce + 1)
		{
			blast = Instantiate<GillaBlast>(prefab, seen, Quaternion.identity);
			blast.name = prefab.name;
		}
		else
		{
			blast = prefab;
		}
		blast.HitYou(at);
		blast.HitDummies(at);
		blast.HitMonsters(at);
		blast.HitThings(at);
		blast.Rumble(at);
		GillaDynamite.ChainFrom(at);
		BlastMark.End();
	}

	private void Start()
	{
		flashMax = flash != null ? flash.intensity : 0f;
		if (voxels != null)
		{
			block = new MaterialPropertyBlock();
			float turn = Random.value;
			float angle = turn * Mathf.PI + turn * Mathf.PI;
			block.SetVector(TurnId, new Vector4(Mathf.Cos(angle), Mathf.Sin(angle), 0f, 0f));
		}
		if (boom != null && booms != null && booms.Length != 0)
		{
			boom.clip = booms[Random.Range(0, booms.Length)];
			boom.pitch = Random.Range(0.93f, 1.05f);
			boom.Play();
		}
		Update();
	}

	private void Update()
	{
		t += Time.deltaTime;
		if (voxels != null && block != null)
		{
			voxels.enabled = t < voxelSeconds;
			block.SetFloat(AgeId, t);
			voxels.SetPropertyBlock(block);
		}
		if (flash != null)
		{
			float bright = 0f;
			float k = (t - flashDelay) / flashSeconds;
			if (k >= 0f && k <= 1f)
			{
				bright = flashCurve.Evaluate(k);
			}
			flash.intensity = bright * flashMax;
			flash.range = Mathf.Clamp01(k) * flashRange;
			flash.enabled = bright > 0.001f;
		}
		if (t < lasts)
		{
			return;
		}
		Destroy(gameObject, 0f);
	}

	private void HitYou(Vector3 at)
	{
		KillCam cam = KillCam.Local;
		if (cam == null)
		{
			return;
		}
		if (cam.head == null)
		{
			return;
		}
		Vector3 middle = cam.head.position + Vector3.down * cam.heldEyesOver * MonsterPlayer.ApeSize;
		Vector3 off = middle - at;
		float dist = off.magnitude;
		if (dist > throwRadius)
		{
			return;
		}
		bool blocked = Physics.Linecast(at, middle, Solid, QueryTriggerInteraction.Ignore);
		Vector3 dir = dist > 0.05f ? off / dist : Vector3.up;
		dir = (dir + Vector3.up * 0.55f).normalized;
		float speed = Mathf.Lerp(throwSpeed.x, throwSpeed.y, Mathf.Pow(1f - dist / throwRadius, 0.8f));
		Vector3 velocity = dir * speed * (blocked ? 0.3f : 1f);
		if (MonsterPlayer.IsMonster)
		{
			cam.Shoved(velocity * 0.35f);
			return;
		}
		int many;
		bool dies;
		if (!blocked && dist < killRadius)
		{
			many = 6;
			dies = true;
		}
		else if (!blocked && dist < hurtRadius)
		{
			dies = false;
			many = dist >= (hurtRadius + killRadius) * 0.5f ? 1 : 2;
		}
		else
		{
			many = 0;
			dies = false;
		}
		Wound(middle, at, many, dies);
		cam.Blasted(velocity, many, dies);
	}

	public static void CrushYou(Vector3 from, Vector3 velocity)
	{
		KillCam cam = KillCam.Local;
		if (cam == null)
		{
			return;
		}
		if (cam.head == null)
		{
			return;
		}
		Vector3 middle = cam.head.position + Vector3.down * cam.heldEyesOver * MonsterPlayer.ApeSize;
		Wound(middle, from, 6, true);
		cam.Blasted(velocity, 6, true);
	}

	public static void CrushDummy(GillaDummy d, Vector3 from, Vector3 velocity)
	{
		GillaWounds wounds = GillaWounds.On(d.tearing);
		PlaceWounds(wounds, d.Middle, from, 6, 1f, d.Wound);
		byte torn = (byte)(NetworkApe.TornBit(ApePart.ArmR) | NetworkApe.TornBit(ApePart.ArmL));
		d.Blasted(velocity, torn, true);
	}

	public static void TrapYou(Vector3 from)
	{
		KillCam cam = KillCam.Local;
		if (cam == null)
		{
			return;
		}
		if (cam.head == null)
		{
			return;
		}
		Vector3 middle = cam.head.position + Vector3.down * cam.heldEyesOver * MonsterPlayer.ApeSize;
		Wound(middle, from, TrapWounds, false);
		cam.Blasted(Vector3.zero, cam.punchesToKill, false);
	}

	public static void TrapDummy(GillaDummy d, Vector3 from)
	{
		GillaWounds wounds = GillaWounds.On(d.tearing);
		PlaceWounds(wounds, d.Middle, from, TrapWounds, 1f, d.Wound);
		d.Blasted(Vector3.zero, 0, false);
	}

	private static void Wound(Vector3 middle, Vector3 at, int many, bool dies)
	{
		NetworkBootstrap boot = NetworkBootstrap.Instance;
		DismemberGroup group = null;
		if (boot != null)
		{
			group = boot.tearing;
		}
		if (group == null)
		{
			return;
		}
		if (many > 0 && group.copies != null)
		{
			Dismemberment copy = null;
			for (int i = 0; i < group.copies.Length; i++)
			{
				Dismemberment c = group.copies[i];
				if (c != null && c.spawnPieces)
				{
					copy = c;
					break;
				}
			}
			if (copy != null)
			{
				GillaWounds wounds = GillaWounds.On(copy);
				PlaceWounds(wounds, middle, at, many, MonsterPlayer.ApeSize, (s, p) => { NetworkApe.WoundYou((byte)s, p); });
				if (dies)
				{
					NetworkApe.TearYou(ApePart.ArmL);
					NetworkApe.TearYou(ApePart.ArmR);
				}
			}
		}
	}

	private static void PlaceWounds(GillaWounds wounds, Vector3 middle, Vector3 at, int many, float size, Action<GoreSection, Vector3> put)
	{
		GoreSection section = GoreSection.Head;
		Vector3 point = Vector3.zero;
		if (wounds == null)
		{
			return;
		}
		Vector3 dir = (at - middle).normalized;
		if (many > 0 && many * 4 > 0)
		{
			int placed = 0;
			int tries = 1;
			bool more;
			do
			{
				Vector3 d = (dir + Random.insideUnitSphere * 0.9f).normalized;
				float lift = Random.Range(-0.15f, 0.3f);
				Vector3 p = middle + d * 0.32f * size + Vector3.up * lift * size;
				if (wounds.Hit(p, size * 0.45f, middle - p, out section, out point))
				{
					put(section, point);
					placed++;
				}
				more = tries < many * 4;
				tries++;
			}
			while (placed < many && more);
		}
	}

	private void HitDummies(Vector3 at)
	{
		GillaDummy[] all = GillaDummy.All.ToArray();
		for (int i = 0; i < all.Length; i++)
		{
			GillaDummy d = all[i];
			if (d == null) continue;
			if (!d.Alive || !d.Mine) continue;
			if (d.tearing == null) continue;
			Vector3 middle = d.Middle;
			Vector3 off = middle - at;
			float dist = off.magnitude;
			if (dist > throwRadius) continue;
			if (Physics.Linecast(at, middle, Solid, QueryTriggerInteraction.Ignore)) continue;
			Vector3 dir = dist > 0.05f ? off / dist : Vector3.up;
			dir = (dir + Vector3.up * 0.55f).normalized;
			int many;
			if (dist < killRadius)
			{
				many = 6;
			}
			else if (dist >= hurtRadius)
			{
				many = 0;
			}
			else if (dist >= (killRadius + hurtRadius) * 0.5f)
			{
				many = 1;
			}
			else
			{
				many = 2;
			}
			GillaWounds wounds = GillaWounds.On(d.tearing);
			PlaceWounds(wounds, middle, at, many, 1f, d.Wound);
			byte torn = (byte)(NetworkApe.TornBit(ApePart.ArmR) | NetworkApe.TornBit(ApePart.ArmL));
			float speed = Mathf.Lerp(throwSpeed.x, throwSpeed.y, Mathf.Pow(1f - dist / throwRadius, 0.8f));
			if (dist >= killRadius)
			{
				torn = 0;
			}
			d.Blasted(dir * speed, torn, dist < killRadius);
		}
	}

	private void HitMonsters(Vector3 at)
	{
		RaycastHit hit;
		Material mat = null;
		NetworkBootstrap boot = NetworkBootstrap.Instance;
		if (boot != null && boot.tearing != null)
		{
			Dismemberment[] copies = boot.tearing.copies;
			if (copies != null && copies.Length != 0 && copies[0] != null)
			{
				mat = boot.tearing.copies[0].bloodMaterial;
			}
		}
		MonsterBrain[] all = MonsterBrain.All.ToArray();
		for (int i = 0; i < all.Length; i++)
		{
			MonsterBrain m = all[i];
			if (m == null) continue;
			if (m.Dead) continue;
			Vector3 middle = m.Middle;
			Vector3 off = middle - at;
			float dist = off.magnitude;
			float raw = 2f - (dist + dist) / monsterRadius;
			if (!(raw >= 0f)) continue;
			float f = Mathf.Clamp01(raw);
			if (!(raw > 1f || raw > 0f)) continue;
			bool clear = true;
			if (Physics.Linecast(at, middle, out hit, Solid, QueryTriggerInteraction.Ignore))
			{
				clear = hit.transform.IsChildOf(m.transform);
			}
			Vector3 dir;
			float mult;
			if (dist <= 0.05f)
			{
				dir = m.transform.forward;
			}
			else
			{
				dir = off / dist;
			}
			if (clear)
			{
				if (mat != null)
				{
					Blood.Burst(middle - dir * 0.25f, dir, mat, Mathf.Clamp01(f) * 0.5f + 0.3f);
				}
				if (m.Puppet) continue;
				mult = 1f;
			}
			else
			{
				if (m.Puppet) continue;
				mult = 0.25f;
			}
			if (dist > 0.05f && clear)
			{
				mult = 1f;
			}
			dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
			Vector3 push = f * dir * monsterShove * (clear ? 1f : 0.3f);
			m.Blasted(f * monsterDamage * mult, push, at, clear ? f : 0f);
		}
	}

	private void HitThings(Vector3 at)
	{
		RaycastHit hit;
		GillaItem item;
		HashSet<Rigidbody> seen = new HashSet<Rigidbody>();
		Rigidbody mine = null;
		if (KillCam.Local != null)
		{
			mine = KillCam.Local.body;
		}
		Collider[] hits = Physics.OverlapSphere(at, throwRadius, Thrown, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < hits.Length; i++)
		{
			Rigidbody body = hits[i].attachedRigidbody;
			if (body == null) continue;
			if (body.isKinematic) continue;
			if (body == mine) continue;
			if (!seen.Add(body)) continue;
			if (body.TryGetComponent<GillaItem>(out item))
			{
				if (!item.Mine) continue;
				if (!(item.Hand < 0 && !item.Swallowed && !item.Gone)) continue;
			}
			Vector3 centre = body.worldCenterOfMass;
			Vector3 off = centre - at;
			float dist = off.magnitude;
			if (dist > throwRadius) continue;
			if (Physics.Linecast(at, body.worldCenterOfMass, out hit, Solid, QueryTriggerInteraction.Ignore))
			{
				if (hit.rigidbody != body) continue;
			}
			Vector3 dir = dist > 0.05f ? off / dist : Vector3.up;
			dir = (dir + Vector3.up * 0.4f).normalized;
			float k = 1f - dist / throwRadius;
			body.AddForce(dir * thingSpeed * k, ForceMode.VelocityChange);
			body.AddTorque(k * Random.insideUnitSphere * 12f, ForceMode.VelocityChange);
		}
	}

	private void Rumble(Vector3 at)
	{
		KillCam cam = KillCam.Local;
		if (cam == null)
		{
			return;
		}
		if (cam.head == null)
		{
			return;
		}
		float k = 1f - Vector3.Distance(cam.head.position, at) / feltRadius;
		if (k > 0f)
		{
			XRNode[] nodes = new XRNode[] { XRNode.LeftHand, XRNode.RightHand };
			float c = Mathf.Min(k, 1f);
			float duration = 0.15f;
			float amplitude = 0.25f;
			if (k >= 0f)
			{
				duration = c * 0.35f + 0.15f;
				amplitude = c * 0.75f + 0.25f;
			}
			for (int i = 0; i < nodes.Length; i++)
			{
				InputDevice device = InputDevices.GetDeviceAtXRNode(nodes[i]);
				device.SendHapticImpulse(0, amplitude, duration);
			}
		}
	}
}
