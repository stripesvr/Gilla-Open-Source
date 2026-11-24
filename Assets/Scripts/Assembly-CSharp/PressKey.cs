// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class PressKey : MonoBehaviour
{
	[Tooltip("What the key types: a digit, or a word like ENTER, UP, DOWN, CLEAR.")]
	public string id;
	[Tooltip("The points that press it (the ends of the first-person ape's index fingers), left then right.")]
	public Transform[] fingertips;
	[Tooltip("How thick a fingertip is, metres: it is on the key once it comes this close to it.")]
	public float fingerRadius = 0.008f;
	[Tooltip("How far above the key's top a fingertip still holds it down, metres.")]
	public float hover = 0.015f;
	[Tooltip("How far the key goes down while pressed, along its own -Y.")]
	public float travel = 0.01f;
	[Tooltip("The key's own meshes (not its label): they show Pressed Material while it is down.")]
	public Renderer[] caps;
	public Material pressedMaterial;
	public event Action<PressKey> Pressed;
	private const float MinDownTime = 0.12f;
	private const float Release = 0.004f;
	private const float DepthBelow = 0.05f;
	private const float NearEnough = 1f;
	private static AudioClip click;
	private static AudioSource voice;
	private static int lastPress = -1;
	private Vector3 rest;
	private BoxCollider box;
	private Material[][] upMaterials;
	private Material[][] downMaterials;
	private bool touching;
	private bool shownDown;
	private float depth;
	private float heldUntil;

	public bool IsDown
	{
		get
		{
			if (touching)
			{
				return true;
			}
			return Time.time < heldUntil;
		}
	}

	private void Awake()
	{

		rest = transform.localPosition;
		box = GetComponent<BoxCollider>();
		if (caps == null)
		{
			caps = new Renderer[0];
		}
		upMaterials = new Material[caps.Length][];
		downMaterials = new Material[caps.Length][];
		for (int i = 0; i < caps.Length; i++)
		{
			if (caps[i] == null)
			{
				continue;
			}
			upMaterials[i] = caps[i].sharedMaterials;
			downMaterials[i] = new Material[upMaterials[i].Length];
			for (int j = 0; j < downMaterials[i].Length; j++)
			{
				downMaterials[i][j] = pressedMaterial != null ? pressedMaterial : upMaterials[i][j];
			}
		}
	}

	private void Update()
	{
		if (depth <= 0f && !touching && heldUntil <= Time.time && !shownDown && !FingerNear())
		{
			if (XRSettings.isDeviceActive)
			{
				return;
			}
			if (Mouse.current == null)
			{
				return;
			}
			if (!Mouse.current.leftButton.wasPressedThisFrame)
			{
				return;
			}
		}
		int hand = TouchingHand(touching ? 0.004f : 0f);
		if (hand >= 0 && !touching)
		{
			Press(hand);
		}
		touching = hand >= 0;
		if (!XRSettings.isDeviceActive && !GillaFreecam.On && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
		{
			Camera cam = Camera.main;
			Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 5f, 1 << gameObject.layer, QueryTriggerInteraction.Collide))
			{
				if (hit.collider.gameObject == gameObject)
				{
					Press(-1);
				}
			}
		}
		float target = IsDown ? travel : 0f;
		depth = Mathf.MoveTowards(depth, target, Time.deltaTime * travel / 0.04f);
		Transform t = transform;
		t.localPosition = rest - transform.localRotation * Vector3.up * depth;
		ShowDown(IsDown);
	}

	private bool FingerNear()
	{
		if (fingertips == null)
		{
			return false;
		}
		Vector3 pos = transform.position;
		for (int i = 0; i < fingertips.Length; i++)
		{
			Transform tip = fingertips[i];
			if (tip != null && (tip.position - pos).sqrMagnitude < 1f)
			{
				return true;
			}
		}
		return false;
	}

	private int TouchingHand(float slack)
	{
		if (fingertips == null || box == null)
		{
			return -1;
		}
		Matrix4x4 parentMatrix = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
		Matrix4x4 local = parentMatrix * Matrix4x4.TRS(rest, transform.localRotation, transform.localScale);
		Matrix4x4 inv = local.inverse;
		float scale = transform.lossyScale.y;
		Vector3 center = box.center;
		Vector3 size = box.size;
		float side = (fingerRadius + slack) / scale;
		float halfY = size.y * 0.5f;
		float zLimit = side + size.z * 0.5f;
		float top = (fingerRadius + hover + slack) / scale + halfY;
		float bottom = halfY - 0.05f / scale;
		for (int i = 0; i < fingertips.Length; i++)
		{
			if (fingertips[i] == null)
			{
				continue;
			}
			Vector3 p = inv.MultiplyPoint3x4(fingertips[i].position) - center;
			if (Mathf.Abs(p.x) <= side + size.x * 0.5f && Mathf.Abs(p.z) <= zLimit && p.y <= top && p.y >= bottom)
			{
				return i;
			}
		}
		return -1;
	}

	public void Click()
	{
		heldUntil = Time.time + 0.12f;
		if (Pressed != null)
		{
			Pressed(this);
		}
		Sound(transform.position);
	}

	private void Press(int hand)
	{
		heldUntil = Time.time + 0.12f;
		if (Pressed != null)
		{
			Pressed(this);
		}
		Sound(transform.position);
		if (hand >= 0)
		{
			UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(hand != 0 ? XRNode.RightHand : XRNode.LeftHand);
			device.SendHapticImpulse(0, 0.25f, 0.03f);
		}
	}

	private void ShowDown(bool down)
	{
		if (shownDown == down || upMaterials == null)
		{
			return;
		}
		shownDown = down;
		for (int i = 0; i < caps.Length; i++)
		{
			if (caps[i] != null)
			{
				Material[] mats = upMaterials[i];
				if (mats != null)
				{
					if (down)
					{
						mats = downMaterials[i];
					}
					caps[i].sharedMaterials = mats;
				}
			}
		}
	}

	private static void Sound(Vector3 at)
	{
		if (voice == null)
		{
			GameObject go = new GameObject("KeySounds");
			DontDestroyOnLoad(go);
			voice = go.AddComponent<AudioSource>();
			voice.playOnAwake = false;
			voice.spatialBlend = 1f;
			voice.priority = 0;
			voice.dopplerLevel = 0f;
			voice.minDistance = 1f;
			voice.maxDistance = 20f;
		}
		voice.transform.position = at;
		GillaSounds sounds = GillaSounds.Get();
		if (sounds != null)
		{
			AudioClip[] clips = sounds.keyPresses;
			if (clips != null && clips.Length != 0)
			{
				int index;
				if (clips.Length == 1)
				{
					index = 0;
				}
				else
				{
					index = (lastPress + UnityEngine.Random.Range(0, clips.Length - 1) + 1) % clips.Length;
				}
				lastPress = index;
				voice.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
				voice.PlayOneShot(clips[index], sounds.keyPressVolume);
				return;
			}
		}
		voice.pitch = 1f;
		voice.PlayOneShot(ClickSound(), 0.5f);
	}

	private static AudioClip ClickSound()
	{
		if (click != null)
		{
			return click;
		}
		float[] data = new float[1102];
		System.Random rng = new System.Random(3);
		for (int i = 0; i < 1102; i++)
		{
			float t = (float)i / 44100f;
			float env = Mathf.Exp(t * -180f);
			float tone = Mathf.Sin(t * 13823.008f);
			float noise = (float)rng.NextDouble();
			data[i] = env * (tone * 0.6f + (noise + noise - 1f) * 0.4f);
		}
		click = AudioClip.Create("KeyClick", 1102, 1, 44100, false);
		click.SetData(data, 0);
		return click;
	}
}
