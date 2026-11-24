// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;

public class Debris : MonoBehaviour
{
    private ParticleSystem chips;
    private ParticleSystem dust;
    private static Material material;
    private static Mesh cube;
    private const int Solid = -5893;
    private static readonly Color ChipLight = new Color(0.5f, 0.48f, 0.45f, 1f);
    private static readonly Color ChipDark = new Color(0.22f, 0.21f, 0.2f, 1f);
    private static readonly Color DustLight = new Color(0.66f, 0.64f, 0.6f, 1f);
    private static readonly Color DustDark = new Color(0.46f, 0.44f, 0.41f, 1f);

    public static Debris On(Component owner)
    {
        Debris debris = owner.GetComponent<Debris>();
        if (debris != null)
        {
            return debris;
        }
        return owner.gameObject.AddComponent<Debris>();
    }

    public void Chips(Vector3 at, Vector3 outward, int count, float force = 1f, float spread = 0.08f)
    {
        ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
        if (count > 0 && Ensure())
        {
            Vector3 side = Side(outward);
            p = new ParticleSystem.EmitParams();
            do
            {
                p.position = at + Random.insideUnitSphere * spread;
                float o = Random.Range(0.4f, 1.3f);
                float s = Random.Range(-0.6f, 0.6f);
                float u = Random.Range(-0.1f, 0.9f);
                p.velocity = (outward * o + side * s + Vector3.up * u) * force;
                p.startSize = Random.Range(0.014f, 0.034f);
                p.rotation3D = Random.insideUnitSphere * 180f;
                p.angularVelocity3D = Random.insideUnitSphere * 720f;
                p.startColor = Color.Lerp(ChipDark, ChipLight, Random.value);
                p.startLifetime = Random.Range(0.9f, 1.7f);
                chips.Emit(p, 1);
                count--;
            } while (count != 0);
        }
    }

    public void Puff(Vector3 at, Vector3 outward, Vector3 side, float width, int count, float force = 1f)
    {
        ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
        if (count > 0 && Ensure())
        {
            p = new ParticleSystem.EmitParams();
            do
            {
                float w = Random.Range(-0.5f, 0.5f) * width;
                p.position = at + side * w + Random.insideUnitSphere * 0.05f;
                float o = Random.Range(0.3f, 1.1f);
                float s = Random.Range(-0.3f, 0.3f);
                float u = Random.Range(0f, 0.35f);
                p.velocity = (outward * o + side * s + Vector3.up * u) * force;
                p.startSize = Random.Range(0.06f, 0.14f);
                p.rotation3D = Random.insideUnitSphere * 180f;
                p.angularVelocity3D = Random.insideUnitSphere * 120f;
                p.startColor = Color.Lerp(DustDark, DustLight, Random.value);
                p.startLifetime = Random.Range(0.5f, 1.1f);
                dust.Emit(p, 1);
                count--;
            } while (count != 0);
        }
    }

    private static Vector3 Side(Vector3 outward)
    {
        Vector3 c = Vector3.Cross(outward, Vector3.up);
        if (c.sqrMagnitude <= 0.0001f)
        {
            return Vector3.right;
        }
        return c.normalized;
    }

    private bool Ensure()
    {
        if (chips != null)
        {
            return true;
        }
        if (material == null)
        {
            material = Resources.Load<Material>("Gilla_Debris");
        }
        if (material == null)
        {
            return false;
        }
        if (cube == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube = go.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }
        chips = Make("Chips", 1.4f, 400, true, new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 1f),
            new Keyframe(0.75f, 1f),
            new Keyframe(1f, 0f)
        }));
        dust = Make("Dust", -0.03f, 300, false, new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 0.5f),
            new Keyframe(0.25f, 1f),
            new Keyframe(1f, 0f)
        }));
        ParticleSystem.LimitVelocityOverLifetimeModule limit = dust.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0f;
        limit.dampen = 0.08f;
        return true;
    }

    private ParticleSystem Make(string name, float gravity, int max, bool collide, AnimationCurve size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startRotation3D = true;
        main.gravityModifier = gravity;
        main.maxParticles = max;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, size);
        if (collide)
        {
            ParticleSystem.CollisionModule col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.World;
            col.quality = ParticleSystemCollisionQuality.Low;
            col.collidesWith = Solid;
            col.enableDynamicColliders = false;
            col.bounce = 0.3f;
            col.dampen = 0.5f;
            col.lifetimeLoss = 0f;
            col.radiusScale = 0.5f;
        }
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = cube;
        r.alignment = ParticleSystemRenderSpace.World;
        r.sharedMaterial = material;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.BlendProbes;
        ps.Play();
        return ps;
    }
}
