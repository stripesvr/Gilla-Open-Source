// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(-200)]
public class MonsterDread : MonoBehaviour
{
	[Tooltip("No shake past this far from the nearest monster (m).")]
	public float far = 10f;
	[Tooltip("Full shake and border this close (m).")]
	public float near = 2f;
	[Tooltip("Full shake: how far the view moves (m).")]
	public float position = 0.006f;
	[Tooltip("Full shake: how far the view turns (degrees).")]
	public float rotation = 0.6f;
	[Tooltip("How quick the shake is (Hz, roughly).")]
	public float frequency = 9f;
	[Header("Red, colour fringes and scribbles")]
	[Tooltip("They start to show at this distance (m) and are at their strongest by `near`. How red, how much the colours split, how grainy, and the scribbles' size, number and speed are on Resources/DreadPost and Resources/DreadOverlay.")]
	public float overlayFar = 12f;
	private Vector3 offPos;
	private Quaternion offRot = Quaternion.identity;
	private float dread;
	private float border;
	private Renderer overlay;
	private Material overlayMat;
	private Volume post;
	private static readonly int DensityId = Shader.PropertyToID("_Density");
	private UniversalAdditionalCameraData camData;
	private bool camLooked;

	private void OnEnable()
	{

		Application.onBeforeRender += Shake;
	}

	private void OnDisable()
	{
		Application.onBeforeRender -= Shake;
		Undo();
	}

	private void Start()
	{
		VolumeProfile profile = Resources.Load<VolumeProfile>("DreadPost");
		if (profile != null)
		{
			GameObject go = new GameObject("DreadPost");
			go.transform.SetParent(transform, false);
			post = go.AddComponent<Volume>();
			post.isGlobal = true;
			post.sharedProfile = profile;
			post.priority = 10f;
			post.weight = 0f;
			post.enabled = false;
		}
		Material source = Resources.Load<Material>("DreadOverlay");
		if (source == null)
		{
			return;
		}
		GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		quad.name = "DreadOverlay";
		Destroy(quad.GetComponent<Collider>());
		quad.transform.SetParent(transform, false);
		quad.transform.localScale = Vector3.one * 0.5f;
		quad.layer = gameObject.layer;
		overlay = quad.GetComponent<Renderer>();
		overlayMat = new Material(source);
		overlay.sharedMaterial = overlayMat;
		overlay.shadowCastingMode = ShadowCastingMode.Off;
		overlay.receiveShadows = false;
		overlay.enabled = false;
	}

	private void OnDestroy()
	{
		if (overlayMat != null)
		{
			Destroy(overlayMat);
		}
	}

	private void Update()
	{
		Undo();
		float nearest;
		if (MonsterPlayer.IsMonster)
		{
			nearest = float.MaxValue;
		}
		else
		{
			nearest = float.MaxValue;
			foreach (MonsterBrain brain in MonsterBrain.All)
			{
				if (brain != null && brain.isActiveAndEnabled
					&& GillaBackroomsSwitch.SameSide(brain.transform.position, transform.position))
				{
					float d = Vector3.Distance(brain.transform.position, transform.position);
					nearest = System.Math.Min(d, nearest);
				}
			}
		}
		float t = Mathf.InverseLerp(near, far, nearest);
		float target = (1f - t) * (1f - t);
		dread = Mathf.MoveTowards(dread, target, Time.deltaTime * 2f);
		float o = Mathf.Clamp01(1f - Mathf.InverseLerp(near, overlayFar, nearest));
		float smooth = Mathf.SmoothStep(0f, 1f, o);
		border = Mathf.MoveTowards(border, smooth, Time.deltaTime * 1.5f);
		if (post != null)
		{
			post.enabled = border > 0.001f;
			post.weight = border;
		}
		PostProcessing(post != null ? post.enabled : false);
		if (overlay == null)
		{
			return;
		}
		overlay.enabled = border > 0.01f;
		if (overlay.enabled)
		{
			overlayMat.SetFloat(DensityId, border);
		}
	}

	private void PostProcessing(bool on)
	{
		if (!camLooked)
		{
			camLooked = true;
			Camera cam;
			if (!TryGetComponent<Camera>(out cam))
			{
				cam = GetComponentInChildren<Camera>();
			}
			if (cam == null)
			{
				cam = Camera.main;
			}
			if (cam != null)
			{
				cam.TryGetComponent<UniversalAdditionalCameraData>(out camData);
			}
		}
		if (camData != null)
		{
			if (camData.renderPostProcessing != on)
			{
				camData.renderPostProcessing = on;
			}
		}
	}

	private void Shake()
	{
		Undo();
		if (dread < 0.001f)
		{
			return;
		}
		if (KillCam.Local != null && KillCam.Local.Busy)
		{
			return;
		}
		float t = Time.time * frequency;
		Vector3 n = new Vector3(Mathf.PerlinNoise(t, 0.1f), Mathf.PerlinNoise(0.3f, t), Mathf.PerlinNoise(t, 0.7f)) * 2f - Vector3.one;
		offPos = n * (position * dread);
		offRot = Quaternion.Euler(n.y * rotation * dread, n.z * rotation * dread, dread * rotation * n.x * 0.5f);
		transform.localPosition = transform.localPosition + offPos;
		transform.localRotation = transform.localRotation * offRot;
	}

	private void Undo()
	{
		if (offPos == Vector3.zero && Quaternion.Dot(offRot, Quaternion.identity) > 0.999999f)
		{
			return;
		}
		transform.localPosition = transform.localPosition - offPos;
		transform.localRotation = transform.localRotation * Quaternion.Inverse(offRot);
		offPos = Vector3.zero;
		offRot = Quaternion.identity;
	}
}
