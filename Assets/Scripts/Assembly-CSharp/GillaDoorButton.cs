// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using GorillaLocomotion;

[RequireComponent(typeof(BoxCollider))]
public class GillaDoorButton : MonoBehaviour
{
	public GillaDoor door;
	[Tooltip("The cap, which goes into the wall (along this object's -Z) while it's pushed, and shows the door's state.")]
	public Transform cap;
	[Tooltip("How far in front of the cap (m) a hand still pushes it: about a hand's thickness.")]
	public float reach = 0.05f;
	[Tooltip("How far the cap goes in, metres.")]
	public float travel = 0.012f;
	[Tooltip("The cap's colour while the door can be worked.")]
	public Color readyColour = new Color(0.1f, 0.85f, 0.2f, 1f);
	[Tooltip("The cap's colour while the door is moving.")]
	public Color busyColour = new Color(0.9f, 0.08f, 0.06f, 1f);
	[Tooltip("Played as it's pushed (one of them).")]
	public AudioClip[] clickSounds;
	private const float Again = 0.4f;
	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
	private BoxCollider box;
	private Renderer capRenderer;
	private MaterialPropertyBlock block;
	private Vector3 capRest;
	private bool touching;
	private int shown = -1;
	private float nextPush;
	private float depth;

	public bool ShowsBusy
	{
		get { return shown == 1; }
	}

	private void Awake()
	{

		box = GetComponent<BoxCollider>();
		if (cap == null)
		{
			return;
		}
		capRest = cap.localPosition;
		capRenderer = cap.GetComponent<Renderer>();
	}

	private void Update()
	{
		bool busy = door != null ? door.Busy : false;
		Player gt = Player.Instance;
		int hand;
		if (gt != null)
		{
			hand = TouchingHand(gt);
			if (hand >= 0 && !touching && !busy && nextPush <= Time.time)
			{
				Push(hand);
			}
		}
		else
		{
			hand = -1;
		}
		touching = hand >= 0;
		if (!XRSettings.isDeviceActive && !GillaFreecam.On)
		{
			Mouse mouse = Mouse.current;
			if (mouse != null && mouse.leftButton.wasPressedThisFrame && Camera.main != null)
			{
				Camera cam = Camera.main;
				Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
				int layer = gameObject.layer;
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 5f, 1 << layer, QueryTriggerInteraction.Collide))
				{
					bool onBox = hit.collider == box;
					if (!busy && onBox && nextPush <= Time.time)
					{
						Push(-1);
					}
				}
			}
		}
		if (cap == null)
		{
			return;
		}
		Show(busy);
		float target;
		if (touching || Time.time < nextPush - 0.2f)
		{
			target = travel;
		}
		else
		{
			target = 0f;
		}
		depth = Mathf.MoveTowards(depth, target, Time.deltaTime * travel / 0.05f);
		Vector3 forward = transform.forward;
		Vector3 offset = -(forward * depth);
		if (cap.parent != null)
		{
			offset = cap.parent.InverseTransformVector(offset);
		}
		cap.localPosition = capRest + offset;
	}

	private void Show(bool moving)
	{
		int value = moving ? 1 : 0;
		if (shown == value)
		{
			return;
		}
		if (capRenderer == null)
		{
			return;
		}
		shown = value;
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		capRenderer.GetPropertyBlock(block);
		block.SetColor(BaseColorId, moving ? busyColour : readyColour);
		capRenderer.SetPropertyBlock(block);
	}

	private int TouchingHand(Player gt)
	{
		Vector3 center = box.center;
		Vector3 size = box.size;
		for (int i = 0; i < 2; i++)
		{
			Transform follower = i == 0 ? gt.leftHandFollower : gt.rightHandFollower;
			if (follower == null)
			{
				continue;
			}
			Vector3 local = transform.InverseTransformPoint(follower.position);
			float dz = local.z - center.z;
			if (Mathf.Abs(local.x - center.x) > size.x * 0.5f || Mathf.Abs(local.y - center.y) > size.y * 0.5f || dz < -(size.z * 0.5f))
			{
				continue;
			}
			float limit = size.z * 0.5f + reach;
			if (limit < dz && i == 0)
			{
				continue;
			}
			return limit < dz ? -1 : i;
		}
		return -1;
	}

	public void Click()
	{
		if (door != null && door.Busy)
		{
			return;
		}
		if (Time.time < nextPush)
		{
			return;
		}
		Push(-1);
	}

	private void Push(int hand)
	{
		nextPush = Time.time + Again;
		if (door != null)
		{
			door.Press();
		}
		GillaQuests.Report(GillaQuests.Stat.Doors, 1);
		Sfx.Play(clickSounds, transform.position, 0.8f, 0.95f, 1.05f);
		if (hand >= 0)
		{
			XRNode node = hand != 0 ? XRNode.RightHand : XRNode.LeftHand;
			UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
			device.SendHapticImpulse(0, 0.5f, 0.06f);
		}
	}
}
