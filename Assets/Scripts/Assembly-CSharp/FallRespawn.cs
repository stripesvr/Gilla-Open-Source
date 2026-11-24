// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.SceneManagement;
using GorillaLocomotion;

public class FallRespawn : MonoBehaviour
{
	[Tooltip("How far under the lowest floor (m) your head may fall before you're put back.")]
	public float fallBelow = 5f;
	private const int NotFloors = 7940;
	private Player gt;
	private Transform head;
	private float killY = float.NegativeInfinity;
	private float tall = 1f;
	private Vector3 spawnFeet;
	private Vector3 spawnFacing;

	[RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)0)]
	private static void Boot()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
		Attach();
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Attach();
	}

	private static void Attach()
	{
		Player p = Player.Instance != null ? Player.Instance : Object.FindAnyObjectByType<Player>();
		if (p == null) return;
		if (p.GetComponent<FallRespawn>() != null) return;
		p.gameObject.AddComponent<FallRespawn>();
	}

	private void Start()
	{
		gt = GetComponent<Player>();
		Component src = this;
		if (gt != null && gt.headCollider != null)
		{
			src = gt.headCollider;
		}
		head = src.transform;
		spawnFeet = transform.position;
		spawnFacing = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
		GillaTeleport.To(spawnFeet, spawnFacing);
		Collider[] colliders = Object.FindObjectsByType<Collider>((FindObjectsInactive)1, (FindObjectsSortMode)0);
		float lowest = float.PositiveInfinity;
		for (int i = 0; i < colliders.Length; i++)
		{
			Collider c = colliders[i];
			if (c.isTrigger) continue;
			if (((1 << c.gameObject.layer) & NotFloors) != 0) continue;
			if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) continue;
			if (c.transform.IsChildOf(transform)) continue;
			Bounds b = c.bounds;
			if (b.center.y - b.extents.y <= lowest)
			{
				lowest = b.center.y - b.extents.y;
			}
		}
		if (!float.IsInfinity(lowest))
		{
			killY = lowest - fallBelow;
		}
	}

	private void Update()
	{
		RaycastHit hit = default;
		if (head == null) return;
		Vector3 pos = head.position;
		if (Physics.Raycast(pos, Vector3.down, out hit, 4f, -7941, (QueryTriggerInteraction)1))
		{
			tall = pos.y - hit.point.y;
		}
		if (pos.y <= killY)
		{
			if (KillCam.Local != null && KillCam.Local.Busy) return;
			if (!MonsterPlayer.IsMonster && KillCam.Local != null)
			{
				KillCam.Local.Fell();
				return;
			}
			Put(spawnFeet + Vector3.up * tall);
		}
	}

	private void Put(Vector3 eyes)
	{
		Rigidbody rb = null;
		Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
		if (gt != null && fwd.sqrMagnitude > 0.0001f && spawnFacing.sqrMagnitude > 0.0001f)
		{
			gt.Turn(Vector3.SignedAngle(fwd, spawnFacing, Vector3.up) /* ? */);
		}
		Transform t = transform;
		t.position = t.position + (eyes - head.position);
		if (TryGetComponent<Rigidbody>(out rb))
		{
			rb.position = transform.position;
			if (!rb.isKinematic)
			{
				rb.linearVelocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
		Physics.SyncTransforms();
		if (gt != null)
		{
			gt.InitializeValues();
		}
	}
}
