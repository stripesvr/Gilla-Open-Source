// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Reflection;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class Blood
{
    private static Mesh s_drop;
    private static GillaGoreAssets s_gore;
    private static readonly ProfilerMarker BloodMark = new ProfilerMarker("Gilla.Blood");
    private static readonly Color Dark = new Color(0.306f, 0.02f, 0.027f, 1f);
    private static readonly Color Bright = new Color(0.612f, 0.094f, 0.075f, 1f);
    private const int IgnoreLayers = 1796;
    private const float Budget = 3000f;
    private const float Lately = 1.2f;
    private const float Least = 0.12f;
    private const float Thin = 0.35f;
    private static float let;
    private static float letAt;

    private static GillaGoreAssets Gore
    {
        get
        {
            if (s_gore != null)
            {
                return s_gore;
            }
            s_gore = Resources.Load<GillaGoreAssets>("GillaGore");
            return s_gore;
        }
    }

    private static Material RoomLit(Material given)
    {
        if (Gore != null && Gore.spray != null)
        {
            given = Gore.spray;
        }
        return given;
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Fresh()
    {
        let = 0f;
        letAt = 0f;
    }

    public static float Share(float want)
    {
        float now = Time.unscaledTime;
        let *= Mathf.Exp(Mathf.Max(now - letAt, 0f) / -Lately);
        letAt = now;
        float share = 1f;
        if (let + want > Budget)
        {
            share = Mathf.Clamp((Budget - let) / Mathf.Max(want, 1f), Least, 1f);
        }
        let += share * want;
        return share;
    }

    private static short Some(float n)
    {
        return (short)Mathf.Max(Mathf.RoundToInt(n), 1);
    }

    private static MethodInfo s_chunks;

    private static void WoundChunks(Vector3 at, Vector3 dir, Mesh mesh, Material mat, int count, float minSize, float maxSize, float speed, float spread)
    {
        if (s_chunks == null)
        {
            s_chunks = typeof(GillaWounds).GetMethod("Chunks", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                new Type[] { typeof(Vector3), typeof(Vector3), typeof(Mesh), typeof(Material), typeof(int), typeof(float), typeof(float), typeof(float), typeof(float) }, null);
        }
        if (s_chunks != null)
        {
            s_chunks.Invoke(null, new object[] { at, dir, mesh, mat, count, minSize, maxSize, speed, spread });
        }
    }

    public static void Spray(Transform at, Vector3 dir, Material mat, float seconds, int burst, int pulses, float fine = 1f)
    {
        using (BloodMark.Auto())
        {
            float duration = Mathf.Max(seconds, 0.2f);
            float want = Mathf.Min(duration * fine * 140f * fine + burst, 600f);
            float share = Share(want);
            GameObject obj = new GameObject("Blood");
            obj.transform.SetParent(at, false);
            Vector3 forward = dir.sqrMagnitude <= 1e-06f ? Vector3.up : dir;
            obj.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (pulses < 2)
            {
                pulses = 1;
            }
            AnimationCurve pulse = Pulses(pulses);
            AnimationCurve half = new AnimationCurve();
            Keyframe[] keys = pulse.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                half.AddKey(new Keyframe(keys[i].time, keys[i].value * 0.5f));
            }
            ParticleSystem.MainModule main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, half, pulse);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(fine * 0.006f, fine * 0.017f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(fine * 0.006f, fine * 0.017f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(fine * 0.016f, fine * 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(Dark, Bright);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Some(share * 600f);
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(fine * 280f * fine * share, pulse);
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Some(share * burst)) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.025f;
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve shrink = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0f, 1f),
                new Keyframe(0.7f, 1f),
                new Keyframe(1f, 0f)
            });
            sol.size = new ParticleSystem.MinMaxCurve(1f, shrink);
            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.collidesWith = ~IgnoreLayers;
            collision.enableDynamicColliders = false;
            if (share < 1f)
            {
                collision.quality = ParticleSystemCollisionQuality.Low;
            }
            collision.dampen = 0.9f;
            collision.bounce = 0.05f;
            collision.lifetimeLoss = 0f;
            collision.radiusScale = 0.5f;
            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.alignment = ParticleSystemRenderSpace.Velocity;
            renderer.mesh = Drop();
            renderer.sharedMaterial = RoomLit(mat);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.enableGPUInstancing = false;
            ps.Play();
            UnityEngine.Object.Destroy(obj, duration + 2f);
        }
    }

    public static void Burst(Vector3 at, Vector3 dir, Material mat, float size = 1f)
    {
        using (BloodMark.Auto())
        {
            Material m = RoomLit(mat);
            if (m == null)
            {
                return;
            }
            if (dir.sqrMagnitude < 1e-06f)
            {
                dir = Vector3.up;
            }
            float share = Share(size * 335f);
            GameObject go = new GameObject("BloodBurst");
            Transform t = go.transform;
            t.position = at;
            UnityEngine.Object.Destroy(t.gameObject, 5f);
            float amount = share * size;
            ParticleSystem ps = Particles("Blood", t, dir, m, 0.4f, Mathf.RoundToInt(amount * 240f));
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 7f);
            ParticleSystem.MinMaxCurve thick = new ParticleSystem.MinMaxCurve(0.0035f, 0.011f);
            main.startSizeY = thick;
            main.startSizeX = thick;
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.008f, 0.032f);
            main.gravityModifier = 1.2f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, Some(amount * 190f)),
                new ParticleSystem.Burst(0.05f, Some(amount * 45f))
            });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 62f;
            shape.radius = 0.06f;
            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.collidesWith = ~IgnoreLayers;
            collision.enableDynamicColliders = false;
            collision.dampen = 0.9f;
            collision.bounce = 0.05f;
            collision.lifetimeLoss = 0f;
            collision.radiusScale = 0.5f;
            ps.Play();
            if (share < Thin)
            {
                return;
            }
            ps = Particles("Blood", t, dir, m, 0.1f, Mathf.RoundToInt(amount * 100f));
            main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            ParticleSystem.MinMaxCurve fineSize = new ParticleSystem.MinMaxCurve(0.0014f, 0.0034f);
            main.startSizeY = fineSize;
            main.startSizeX = fineSize;
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.002f, 0.007f);
            main.gravityModifier = 0.5f;
            emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(amount * 100f)) });
            shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 75f;
            shape.radius = 0.07f;
            ps.Play();
            if (Gore == null || Gore.chunk == null)
            {
                return;
            }
            WoundChunks(at, dir, Gore.chunk, Gore.chunkMaterial, Mathf.RoundToInt(amount * 26f), 0.009f, 0.034f, 1.35f, 60f); /* ? */
            if (Gore.chipMaterial == null)
            {
                return;
            }
            WoundChunks(at, dir, Gore.chunk, Gore.chipMaterial, Mathf.RoundToInt(amount * 12f), 0.004f, 0.013f, 1.5f, 60f); /* ? */
        }
    }

    public static void Yolk(Vector3 at, Vector3 dir)
    {
        using (BloodMark.Auto())
        {
            Material mat = RoomLit(null);
            if (mat != null)
            {
                if (dir.sqrMagnitude < 1e-06f)
                {
                    dir = Vector3.up;
                }
                GameObject go = new GameObject("EggBurst");
                Transform t = go.transform;
                t.position = at;
                UnityEngine.Object.Destroy(t.gameObject, 3f);
                ParticleSystem ps = Particles("Yolk", t, dir, mat, 0.2f, 40);
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.6f, 0.08f, 1f), new Color(0.98f, 0.93f, 0.72f, 1f));
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 3f);
                ParticleSystem.MinMaxCurve xy = new ParticleSystem.MinMaxCurve(0.004f, 0.012f);
                main.startSizeY = xy;
                main.startSizeX = xy;
                main.startSizeZ = new ParticleSystem.MinMaxCurve(0.008f, 0.024f);
                main.gravityModifier = 1.2f;
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 34) });
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 60f;
                shape.radius = 0.03f;
                ps.Play();
            }
        }
    }

    public static void Punch(Transform at, Vector3 dir, Material mat)
    {
        using (BloodMark.Auto())
        {
            if (dir.sqrMagnitude < 1e-06f)
            {
                dir = Vector3.up;
            }
            float share = Share(135f);
            AnimationCurve maxCurve = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0f, 1f),
                new Keyframe(0.1f, 1f),
                new Keyframe(0.28f, 0.1f),
                new Keyframe(1f, 0.05f)
            });
            AnimationCurve minCurve = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0f, 0.3f),
                new Keyframe(0.1f, 0.3f),
                new Keyframe(0.28f, 0.03f),
                new Keyframe(1f, 0.02f)
            });
            ParticleSystem ps = Particles("Blood", at, dir, mat, 0.7f, Some(share * 90f));
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.8f, minCurve, maxCurve);
            ParticleSystem.MinMaxCurve xy = new ParticleSystem.MinMaxCurve(0.003f, 0.0085f);
            main.startSizeY = xy;
            main.startSizeX = xy;
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.007f, 0.024f);
            main.gravityModifier = 1.2f;
            ParticleSystem.EmissionModule emission = ps.emission;
            AnimationCurve rate = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, 0f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0f)
            });
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(share * 36f, rate);
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Some(share * 62f)) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = 0.03f;
            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.collidesWith = ~IgnoreLayers;
            collision.enableDynamicColliders = false;
            collision.dampen = 0.9f;
            collision.bounce = 0.05f;
            collision.lifetimeLoss = 0f;
            collision.radiusScale = 0.5f;
            ps.Play();
            if (share >= Thin)
            {
                ps = Particles("Blood", at, dir, mat, 0.1f, 45);
                main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.8f);
                ParticleSystem.MinMaxCurve fineSize = new ParticleSystem.MinMaxCurve(0.0014f, 0.0032f);
                main.startSizeY = fineSize;
                main.startSizeX = fineSize;
                main.startSizeZ = new ParticleSystem.MinMaxCurve(0.002f, 0.006f);
                main.gravityModifier = 0.5f;
                emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, Some(share * 45f)) });
                shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 58f;
                shape.radius = 0.035f;
                ps.Play();
            }
        }
    }
}
