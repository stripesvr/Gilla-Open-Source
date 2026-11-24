// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[DefaultExecutionOrder(70)]
public class MonsterStoop : MonoBehaviour
{
	public Transform root;
	public Transform neck;
	public Transform head;
	public Transform upL;
	public Transform loL;
	public Transform endL;
	public Transform upR;
	public Transform loR;
	public Transform endR;
	[Tooltip("How far the top of its skull is over its head bone, m.")]
	public float topOverHead = 0.4f;
	[Tooltip("How far under a ceiling it keeps the top of its skull, m.")]
	public float clearance = 0.1f;
	[Tooltip("How far ahead it looks for one, m.")]
	public float lookAhead = 1.4f;
	[Tooltip("The most it comes down, m.")]
	public float mostDown = 1.35f;
	[Tooltip("How far it leans forward when it's right down, degrees.")]
	public float mostLean = 32f;
	private float ceiling = float.PositiveInfinity;
	private float nextLook;
	private Animator anim;
	private const int Solid = -5893;
	private const float LookEvery = 0.08f;
	private const float Speed = 3.2f;

	public float Down { get; private set; }

	private void Awake()
	{

		anim = GetComponentInChildren<Animator>();
	}

	private void Look()
	{
		RaycastHit hit;
		ceiling = float.PositiveInfinity;
		nextLook = Time.time + LookEvery;
		Vector3 pos = transform.position;
		Vector3 up = Vector3.up;
		for (int i = 0; i < 3; i++)
		{
			Vector3 origin = pos + up + transform.forward * (lookAhead * i * 0.5f);
			if (Physics.Raycast(origin, Vector3.up, out hit, 4f, Solid, QueryTriggerInteraction.Ignore))
			{
				ceiling = Mathf.Min(ceiling, hit.point.y);
			}
		}
	}

	private void LateUpdate()
	{
		if (head == null) return;
		if (root == null) return;
		if (anim != null && !anim.enabled) return;
		if (nextLook <= Time.time)
		{
			Look();
		}
		float target = Mathf.Clamp(head.position.y + topOverHead + clearance - ceiling, 0f, mostDown);
		Down = Mathf.MoveTowards(Down, target, Time.deltaTime * Speed);
		if (Down < 0.001f) return;
		float t = Mathf.Clamp01(Down / 0.6f);
		float lean = mostLean * t;
		Vector3 hockL = endL.position;
		Vector3 hockR = endR.position;
		Quaternion turnL = endL.rotation;
		Quaternion turnR = endR.rotation;
		Vector3 side = transform.right;
		root.position = root.position + Vector3.down * Down;
		root.rotation = Quaternion.AngleAxis(lean, side) * root.rotation;
		if (neck != null)
		{
			neck.rotation = Quaternion.AngleAxis(lean * -0.7f, side) * neck.rotation;
		}
		Leg(upL, loL, endL, hockL, turnL, -side);
		Leg(upR, loR, endR, hockR, turnR, side);
	}

	private void Leg(Transform up, Transform lo, Transform end, Vector3 hock, Quaternion turn, Vector3 side)
	{
		float a = transform.position.y + 1.25f;
		float b = transform.position.y + 1.6f;
		float t = Mathf.InverseLerp(a, b, hock.y);
		Vector3 target = hock + Vector3.down * (t * Down);
		typeof(MonsterAvatar).GetMethod("Reach", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
			?.Invoke(null, new object[] { up, lo, end, target, side, transform.forward, side.z /* ? */ }); /* ? */
		end.rotation = turn;
	}
}
