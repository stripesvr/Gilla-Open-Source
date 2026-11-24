// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;

public class GillaVoxelSplash : MonoBehaviour
{
	public Mesh ball;
	public Mesh drop;
	public Material material;
	[Tooltip("The most of each at once.")]
	public int most = 90;
	private const float Gravity = 9.81f;
	private static readonly int DotsId = Shader.PropertyToID("_Dots");
	private static readonly int BrightId = Shader.PropertyToID("_Bright");
	private static GillaVoxelSplash instance;
	private ParticleSystem balls;
	private ParticleSystem drops;

	private void Awake()
	{

		instance = this;
		balls = Make("Voxel Balls", ball);
		drops = Make("Voxel Drops", drop);
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	private ParticleSystem Make(string name, Mesh mesh)
	{
		if (mesh == null || material == null)
		{
			return null;
		}
		GameObject go = new GameObject(name);
		go.transform.SetParent(transform, false);
		ParticleSystem ps = go.AddComponent<ParticleSystem>();
		ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
		ParticleSystem.MainModule main = ps.main;
		main.loop = false;
		main.playOnAwake = false;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.gravityModifier = 1f;
		main.maxParticles = most;
		main.startRotation3D = false;
		ParticleSystem.EmissionModule emission = ps.emission;
		emission.enabled = false;
		ParticleSystem.ShapeModule shape = ps.shape;
		shape.enabled = false;
		ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
		renderer.renderMode = ParticleSystemRenderMode.Mesh;
		renderer.mesh = mesh;
		renderer.sharedMaterial = material;
		renderer.shadowCastingMode = ShadowCastingMode.Off;
		renderer.receiveShadows = false;
		renderer.alignment = ParticleSystemRenderSpace.World;
		return ps;
	}

	public static void Emit(Vector3 at, float strength)
	{
		bool missing = instance == null;
		if (strength <= 0.05f)
		{
			return;
		}
		if (missing)
		{
			return;
		}
		strength = Mathf.Clamp01(strength);
		GillaSplashPool pool = GillaSplashPool.Near(at, 1f);
		Material mat = (pool != null && pool.surface != null) ? pool.surface.sharedMaterial : null;
		Color dots = mat != null ? mat.GetColor(DotsId) : Color.white;
		Color bright = mat != null ? mat.GetColor(BrightId) : Color.white;
		instance.Burst(instance.balls, at, strength, Mathf.RoundToInt(Mathf.Clamp01(strength) * 14f + 2f), 0.07f, 0.22f, new Color(dots.r, dots.g, dots.b, 0.75f));
		instance.Burst(instance.drops, at, strength, Mathf.RoundToInt(Mathf.Clamp01(strength) * 30f + 4f), 0.03f, 0.1f, new Color(bright.r, bright.g, bright.b, 1f));
	}

	private void Burst(ParticleSystem ps, Vector3 at, float strength, int count, float small, float big, Color colour)
	{
		if (ps == null)
		{
			return;
		}
		float s = Mathf.Clamp01(strength);
		float up = s * 3.4f + 1.6f;
		float side = s * 1.5f + 0.7f;
		float spread = s * 0.38f + 0.12f;
		float sizeScale = s * 0.35f + 0.8f;
		for (int i = 0; i < count; i++)
		{
			Vector2 dir = Random.insideUnitCircle;
			float vy = up * Random.Range(0.45f, 1f);
			ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
			ep.position = at + spread * new Vector3(dir.x, 0f, dir.y);
			ep.velocity = new Vector3(side * dir.x, vy, side * dir.y);
			ep.startSize = Random.Range(small, big) * sizeScale;
			ep.startLifetime = (vy + vy) / Gravity;
			ep.rotation3D = new Vector3(0f, Random.Range(0f, 360f), 0f);
			ep.startColor = colour;
			ps.Emit(ep, 1);
		}
	}
}
