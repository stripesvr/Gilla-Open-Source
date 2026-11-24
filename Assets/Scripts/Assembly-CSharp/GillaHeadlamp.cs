// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

public class GillaHeadlamp : MonoBehaviour
{
	[Tooltip("How far it reaches (m), how wide (degrees), how bright.")]
	public float range = 14f;
	[Tooltip("How far it reaches (m), how wide (degrees), how bright.")]
	public float angle = 55f;
	[Tooltip("How far it reaches (m), how wide (degrees), how bright.")]
	public float intensity = 3f;
	public Color colour = new Color(1f, 0.93f, 0.8f, 1f);
	public AudioClip clickOn;
	public AudioClip clickOff;
	private static GillaHeadlamp instance;
	private Light lamp;
	private Transform head;
	private bool was;

	public static bool Lit { get; private set; }

	private void Start()
	{
		Camera playerCam = GetComponentInChildren<Camera>() /* ? */;
		Component source;
		if (Camera.main != null)
		{
			source = Camera.main;
		}
		else
		{
			source = this;
			if (playerCam != null /* ? */)
			{
				source = playerCam /* ? */;
			}
		}
		head = source.transform;
		lamp = MakeLamp(head, range, angle, intensity, colour);
		Lit = false;
		instance = this;
	}

	private void OnDestroy()
	{
		GillaHeadlamp current = instance;
		Lit = false;
		if (current == this)
		{
			instance = null;
		}
	}

	public static void Set(bool on)
	{
		if (instance != null)
		{
			if (Lit != on)
			{
				if (!MonsterPlayer.IsMonster)
				{
					instance.Switch();
				}
			}
		}
	}

	public static Light MakeLamp(Transform head, float range, float angle, float intensity, Color colour)
	{
		GameObject go = new GameObject("Headlamp");
		go.transform.SetParent(head, false);
		go.transform.localPosition = new Vector3(0f, -0.06f, 0.08f);
		Light light = go.AddComponent<Light>();
		light.type = LightType.Spot;
		light.range = range;
		light.spotAngle = angle;
		light.innerSpotAngle = angle * 0.55f;
		light.intensity = intensity;
		light.color = colour;
		light.shadows = LightShadows.None;
		light.renderMode = LightRenderMode.ForcePixel;
		light.enabled = false;
		return light;
	}

	private void Update()
	{
		bool pressed = Pressed();
		if (pressed && !was)
		{
			if (!MonsterPlayer.IsMonster)
			{
				Switch();
			}
		}
		was = pressed;
		if (MonsterPlayer.IsMonster)
		{
			Lit = false;
		}
		if (!(lamp != null))
		{
			return;
		}
		bool enabled = lamp.enabled;
		bool lit = Lit;
		if (enabled == lit)
		{
			return;
		}
		lamp.enabled = lit;
	}

	private void Switch()
	{
		Lit = !Lit;
		AudioClip clip = Lit ? clickOn : clickOff;
		if (clip != null)
		{
			AudioClip[] clips = new AudioClip[1];
			clips[0] = clip;
			Sfx.Play(clips, head.position, 0.4f, 0.95f, 1.05f);
		}
		InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		device.SendHapticImpulse(0, 0.2f, 0.03f);
	}

	private static bool Pressed()
	{
		bool value = false;
		bool result;
		if (GillaDynamite.InHand(1 /* ? */))
		{
			result = false;
		}
		else
		{
			InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			if (device.isValid)
			{
				if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out value))
				{
					return value;
				}
			}
			Keyboard keyboard = Keyboard.current;
			result = false;
			if (!XRSettings.isDeviceActive && keyboard != null)
			{
				result = keyboard.lKey.isPressed;
			}
		}
		return result;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Forget()
	{
		Lit = false;
	}
}
