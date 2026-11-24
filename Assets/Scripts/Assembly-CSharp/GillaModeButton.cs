// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using GorillaLocomotion;

[RequireComponent(typeof(BoxCollider))]
public class GillaModeButton : MonoBehaviour
{
	[Tooltip("The mode's lobby: MONSTER for BE THE MONSTER's, SPLASH for DON'T SPLASH's, empty for the regular game's (NetworkBootstrap.KindOf).")]
	public string room = "MONSTER";
	[Tooltip("Where you're put: your feet there, facing along its forward.")]
	public Transform arrive;
	[Tooltip("The cap, which goes in (along this object's -Z) while it's pushed.")]
	public Transform cap;
	[Tooltip("A lamp by it that shows whether it can be pushed (readyColour) or was just pushed (busyColour); with none, the cap does.")]
	public Renderer lamp;
	[Tooltip("How far in front of the cap (m) a hand still pushes it.")]
	public float reach = 0.05f;
	[Tooltip("How far the cap goes in, metres.")]
	public float travel = 0.01f;
	public Color readyColour = new Color(1.8f, 1.75f, 1.6f, 1f);
	[Tooltip("For a moment after it's pushed.")]
	public Color busyColour = new Color(2.2f, 0.16f, 0.1f, 1f);
	[Tooltip("Played as it's pushed (one of them).")]
	public AudioClip[] clickSounds;
	private const float Again = 1.5f;
	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
	private BoxCollider box;
	private Renderer shows;
	private MaterialPropertyBlock block;
	private Vector3 capRest;
	private bool touching;
	private int shown = -1;
	private float nextPush;
	private float depth;

	private bool Busy
	{
		get { return Time.time < nextPush; }
	}

	public bool ShowsBusy
	{
		get { return shown == 1; }
	}

	private void Awake()
	{

		box = GetComponent<BoxCollider>();
		if (lamp != null)
		{
			shows = lamp;
		}
		else
		{
			shows = cap != null ? cap.GetComponent<Renderer>() : null;
		}
		if (cap != null)
		{
			capRest = cap.localPosition;
		}
	}

	private void Update()
	{
		Player gt = Player.Instance;
		int hand;
		if (gt != null)
		{
			hand = TouchingHand(gt);
			if (hand >= 0 && !touching && nextPush <= Time.time)
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
				Vector2 mp = Mouse.current.position.ReadValue();
				Ray ray = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
				int mask = 1 << gameObject.layer;
				RaycastHit hit;
				if (Physics.Raycast(ray, out hit, 5f, mask, QueryTriggerInteraction.Collide))
				{
					if (hit.collider == box && nextPush <= Time.time)
					{
						Push(-1);
					}
				}
			}
		}

		Show(Time.time < nextPush);
		if (cap == null)
		{
			return;
		}
		float target;
		if (touching || Time.time < nextPush - 1.5f + 0.2f)
		{
			target = travel;
		}
		else
		{
			target = 0f;
		}
		float step = Time.deltaTime * travel / 0.05f;
		float next = depth + (target - depth >= 0f ? step : -step);
		if (Mathf.Abs(target - depth) <= step)
		{
			next = target;
		}
		depth = next;

		Vector3 forward = transform.forward;
		Vector3 offset = -(forward * depth);
		if (cap.parent != null)
		{
			offset = cap.parent.InverseTransformVector(offset);
		}
		cap.localPosition = capRest + offset;
	}

	public void Click()
	{
		if (Time.time < nextPush)
		{
			return;
		}
		Push(-1);
	}

	private void Show(bool busy)
	{
		if (shown == (busy ? 1 : 0))
		{
			return;
		}
		if (shows == null)
		{
			return;
		}
		shown = busy ? 1 : 0;
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		shows.GetPropertyBlock(block);
		block.SetColor(BaseColorId, busy ? busyColour : readyColour);
		shows.SetPropertyBlock(block);
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
			if (size.x * 0.5f < Mathf.Abs(local.x - center.x) || size.y * 0.5f < Mathf.Abs(local.y - center.y) || dz < -(size.z * 0.5f))
			{
				continue;
			}
			float limit = size.z * 0.5f + reach / transform.lossyScale.z;
			if (limit < dz)
			{
				if (i == 0)
				{
					continue;
				}
				return -1;
			}
			return i;
		}
		return -1;
	}

	private void Push(int hand)
	{
		nextPush = Time.time + 1.5f;
		Sfx.Play(clickSounds, transform.position, 0.8f, 0.95f, 1.05f);
		if (hand >= 0)
		{
			UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand != 0 ? XRNode.RightHand : XRNode.LeftHand);
			device.SendHapticImpulse(0, 0.5f, 0.06f);
		}
		if (arrive != null)
		{
			GillaTeleport.To(arrive.position, arrive.forward);
		}
		NetworkBootstrap boot = NetworkBootstrap.Instance;
		NetworkBootstrap.Lobby kind = NetworkBootstrap.KindOf(room);
		string note;
		if (boot != null)
		{
			if (boot.Kind == kind && boot.Runner != null && (boot.Runner.IsRunning || boot.Joining)) /* ? */
			{
				note = "";
			}
			else
			{
				string waiting = boot.TakeWaiting(kind);
				boot.JoinLobby(kind, waiting);
				note = ", joining its room";
			}
		}
		else
		{
			note = "";
		}
		string name = room.Length < 1 ? "REGULAR" : room;
		Debug.Log("[GillaModeButton] off to the " + name + " lobby" + note);
	}
}
