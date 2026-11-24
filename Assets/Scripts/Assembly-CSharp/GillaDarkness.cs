// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;

public class GillaDarkness : MonoBehaviour
{
	public float Level;
	public bool Lamp;
	public Shader shader;
	public AudioClip sound;
	private const float FlickerSeconds = 0.9f;
	private const float LightComesOnAt = 0.65f;
	private const float BackSeconds = 0.6f;
	private const float Dim = 0.2f;
	private static readonly int DarkId = Shader.PropertyToID("_Dark");
	private static readonly int BeamId = Shader.PropertyToID("_Beam");
	private Renderer shell;
	private Material mat;
	private bool on;
	private bool wasLit;
	private bool lampDone;
	private float since;
	private float beam;
	private float target;
	private float from;

	public static float Amount { get; private set; }

	private void OnDisable()
	{
		Level = 0f;
		Lamp = false;
		target = 0f;
		from = 0f;
		if (on && !wasLit)
		{
			GillaHeadlamp.Set(false);
		}
		on = false;
		Amount = 0f;
		if (shell != null)
		{
			shell.enabled = false;
		}
	}

	private void OnDestroy()
	{
		if (shell != null)
		{
			Destroy(shell.gameObject);
		}
		if (mat != null)
		{
			Destroy(mat);
		}
	}

	private void Update()
	{
		float dt = Time.deltaTime;
		if (!Mathf.Approximately(Level, target))
		{
			if (target < Level && sound != null && Camera.main != null)
			{
				AudioClip[] clips = new AudioClip[1];
				clips[0] = sound;
				Sfx.Play(clips, Camera.main.transform.position, 0.85f, 1f, 1f);
			}
			since = 0f;
			target = Level;
			from = Amount;
		}

		bool tick = true;
		if (Lamp != on)
		{
			on = Lamp;
			if (on)
			{
				lampDone = false;
				wasLit = GillaHeadlamp.Lit;
			}
			else if (!wasLit)
			{
				GillaHeadlamp.Set(false);
			}
			else
			{
				since += dt;
				tick = false;
			}
		}
		if (tick)
		{
			since += dt;
			if (on && !lampDone && since >= LightComesOnAt)
			{
				lampDone = true;
				GillaHeadlamp.Set(true);
			}
		}

		if (target < from)
		{
			Amount = Mathf.MoveTowards(Amount, target, dt / BackSeconds);
		}
		else
		{
			float v = target;
			if (since < FlickerSeconds)
			{
				float noise = Mathf.PerlinNoise(Time.time * 17f, 3.7f);
				if (noise <= (since / -FlickerSeconds) * 0.5f + 0.62f)
				{
					v = from;
				}
			}
			Amount = v;
		}

		beam = Mathf.MoveTowards(beam, GillaHeadlamp.Lit ? 1f : 0f, dt * 12f);
		Show();
	}

	private void Show()
	{
		if (Amount <= 0.001f)
		{
			if (shell != null)
			{
				shell.enabled = false;
			}
			return;
		}
		if (shell == null && !Build())
		{
			return;
		}
		shell.enabled = true;
		mat.SetFloat(DarkId, 1f - Mathf.Clamp01(Amount) * 0.8f);
		mat.SetFloat(BeamId, beam);
	}

	private bool Build()
	{
		Camera cam = Camera.main;
		if (cam == null)
		{
			return false;
		}
		if (shader == null)
		{
			return false;
		}
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		go.name = "Darkness";
		Destroy(go.GetComponent<Collider>());
		go.transform.SetParent(cam.transform, false);
		go.transform.localScale = Vector3.one * 0.6f;
		go.layer = cam.gameObject.layer;
		mat = new Material(shader);
		shell = go.GetComponent<Renderer>();
		shell.sharedMaterial = mat;
		shell.shadowCastingMode = ShadowCastingMode.Off;
		shell.receiveShadows = false;
		return true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Forget()
	{
		Amount = 0f;
	}
}
