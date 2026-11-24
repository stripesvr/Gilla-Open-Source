// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

[RequireComponent(typeof(PressKey))]
public class GillaLinkButton : MonoBehaviour
{
	[Tooltip("Where it takes you.")]
	public string url = "https://discord.gg/gilla";
	[Tooltip("Seconds it has to be held down before it opens: 0, a poke opens it.")]
	public float hold;
	[Tooltip("What fills along its own X while it's held (its X scale 0 to 1, from its left end), if anything.")]
	public Transform fill;
	[Tooltip("Played where it is as it opens, if anything.")]
	public AudioClip openSound;
	private const float Again = 5;
	private const float Drain = 3;
	private PressKey key;
	private Collider box;
	private float next;
	private float held;
	private bool spent;

	private void OnEnable()
	{

		key = GetComponent<PressKey>();
		box = GetComponent<Collider>();
		key.Pressed += Pressed;
		Show();
	}

	private void OnDisable()
	{
		if (key != null)
		{
			key.Pressed -= Pressed;
		}
	}

	private void Update()
	{
		if (hold <= 0f)
		{
			return;
		}
		if (key.IsDown || MouseOn())
		{
			if (!spent)
			{
				held = held + Time.unscaledDeltaTime;
			}
			else
			{
				held = Mathf.Max(held - Time.unscaledDeltaTime * Drain, 0f);
			}
		}
		else
		{
			spent = false;
			held = Mathf.Max(held - Time.unscaledDeltaTime * Drain, 0f);
		}
		if (hold <= held)
		{
			held = 0f;
			spent = true;
			Open();
		}
		if (fill != null)
		{
			float t = 0f;
			if (hold > 0f)
			{
				t = Mathf.Clamp01(held / hold);
			}
			fill.localScale = new Vector3(t, 1f, 1f);
		}
	}

	private void Pressed(PressKey _)
	{
		if (0f < hold)
		{
			return;
		}
		Open();
	}

	private void Show()
	{
		if (fill == null)
		{
			return;
		}
		float t = 0f;
		if (hold > 0f)
		{
			t = Mathf.Clamp01(held / hold);
		}
		fill.localScale = new Vector3(t, 1f, 1f);
	}

	private bool MouseOn()
	{
		RaycastHit hit;
		if (!XRSettings.isDeviceActive && !GillaFreecam.On)
		{
			Mouse mouse = Mouse.current;
			if (mouse != null && mouse.leftButton.isPressed)
			{
				if (!(Camera.main == null) && !(box == null))
				{
					Camera cam = Camera.main;
					Vector2 pos = Mouse.current.position.ReadValue();
					Ray ray = cam.ScreenPointToRay(new Vector3(pos.x, pos.y, 0f));
					return box.Raycast(ray, out hit, 5f);
				}
			}
		}
		return false;
	}

	private void Open()
	{
		if (Time.unscaledTime < next || string.IsNullOrEmpty(url))
		{
			return;
		}
		next = Time.unscaledTime + Again;
		if (openSound != null)
		{
			Sfx.Play(new AudioClip[] { openSound }, transform.position, 0.6f, 0.98f, 1.02f);
		}
		Debug.Log("[GillaLinkButton] opening " + url);
		Application.OpenURL(url);
	}
}
