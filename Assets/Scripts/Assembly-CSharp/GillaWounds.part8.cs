// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

public partial class GillaWounds : MonoBehaviour
{
    public GillaWounds()
    {
        bones = new Dictionary<string, (Transform bone, Matrix4x4 bind)>();
        wounds = new Vector4[6];
        axes = new Vector4[6];
        sides = new Vector4[6];
        walls = new GameObject[6];
        showing = new bool[6];
        armOff = new bool[6];
        cuts = new Vector4[6];
        landedOn = new GoreSection[6];
        stumps = new bool[6];
        pieces = new List<(GameObject go, GoreSection section)>();
        wounded = new Dictionary<Material, Material>();
        whole = new List<(SkinnedMeshRenderer skin, Mesh mesh, Material[] mats)>();
        eyesOut = new List<(Transform eye, Vector3 was)>();
        skinMats = new List<Material>();
        wallMeshes = new List<Mesh>();
        breakable = new List<Broken>();
        drawnOn = new bool[6];
        drawn = new Vector4[6];
        drawnAxes = new Vector4[6];
        drawnSides = new Vector4[6];
        soons = new List<Soon>();
        toBreak = new HashSet<Broken>();
    }

    static GillaWounds()
    {
        AddMark = new ProfilerMarker("Gilla.Wound");
        BonesMark = new ProfilerMarker("Gilla.Wound.Bones");
        CraterMark = new ProfilerMarker("Gilla.Wound.Crater");
        BurstMark = new ProfilerMarker("Gilla.Wound.Burst");
        PutMark = new ProfilerMarker("Gilla.Wound.Put");
        WoundIds = new int[]
        {
            Shader.PropertyToID("_Wound0"), Shader.PropertyToID("_Wound1"), Shader.PropertyToID("_Wound2"),
            Shader.PropertyToID("_Wound3"), Shader.PropertyToID("_Wound4"), Shader.PropertyToID("_Wound5")
        };
        AxisIds = new int[]
        {
            Shader.PropertyToID("_Axis0"), Shader.PropertyToID("_Axis1"), Shader.PropertyToID("_Axis2"),
            Shader.PropertyToID("_Axis3"), Shader.PropertyToID("_Axis4"), Shader.PropertyToID("_Axis5")
        };
        SideIds = new int[]
        {
            Shader.PropertyToID("_Side0"), Shader.PropertyToID("_Side1"), Shader.PropertyToID("_Side2"),
            Shader.PropertyToID("_Side3"), Shader.PropertyToID("_Side4"), Shader.PropertyToID("_Side5")
        };
        SpotIds = Enumerable.Range(0, 18).Select(i => Shader.PropertyToID("_Spot" + i) /* ? */).ToArray();
        carrying = new Dictionary<Mesh, Mesh>();
        vertices = new Dictionary<Mesh, Vector3[]>();
        normals = new Dictionary<Mesh, Vector3[]>();
        boneShapes = new Dictionary<Mesh, BoneShape>();
        skinPoints = new Dictionary<Mesh, int[]>();
        skinTriangles = new Dictionary<Mesh, int[]>();
        boneWeights = new Dictionary<Mesh, BoneWeight[]>();
        boxes = new Dictionary<Mesh, Bounds>();
        OffLId = Shader.PropertyToID("_OffL");
        OffRId = Shader.PropertyToID("_OffR");
        busy = new List<GillaWounds>();
        after = new Queue<Action>();
        waiting = new Queue<(GillaWounds on, GoreSection section, Vector3 point, bool burst)>();
        addFor = new Stopwatch();
        addFrame = -1;
        putFor = new Stopwatch();
    }

    private void Burst(GoreSection section, Vector3 point)
    {
        using (BurstMark.Auto())
        {
            Find();
            if (copy != null)
            {
                if (!copy.spawnPieces) return;
            }
            else if (fitted == null)
            {
                return;
            }
            if (Gone(section)) return;
            var found = BoneOf(section, point);
            if (found.bone == null) return;

            Vector3 at = found.bone.TransformPoint(found.bind.MultiplyPoint3x4(point));
            Vector3 inner = point + Inward(section, point) * 0.02f;
            Vector3 innerAt = found.bone.TransformPoint(found.bind.MultiplyPoint3x4(inner));
            Vector3 outward = (at - innerAt).normalized;

            Material blood;
            if (Assets != null && Assets.spray != null)
                blood = Assets.spray;
            else if (copy != null)
                blood = copy.bloodMaterial;
            else
                blood = null;

            AudioClip[] clips;
            if (Assets != null && Assets.punchSounds != null && Assets.punchSounds.Length != 0)
                clips = Assets.punchSounds;
            else if (copy != null)
                clips = copy.tearSounds;
            else
                clips = null;

            float scale = 1f;
            if (fitted != null)
            {
                float s = found.bone.lossyScale.x / fitted.units / 1.8f;
                scale = 0.5f;
                if (0.5f <= s)
                {
                    scale = s;
                    if (3f < s) scale = 3f;
                }
            }

            if (section == GoreSection.Head)
            {
                var player = GorillaLocomotion.Player.Instance;
                if (player != null && transform.IsChildOf(player.transform))
                {
                    Sfx.Play(clips, at, 1f, 0.93f, 1.07f);
                    return;
                }
            }

            if (blood != null)
            {
                Transform t = new GameObject("WoundBlood").transform;
                t.SetParent(found.bone, true);
                t.position = at;
                Blood.Punch(t, (outward + Vector3.up * 0.15f).normalized, blood);
                Destroy(t.gameObject, 5f);
            }

            GillaGoreAssets assets = Assets;
            if (assets != null && assets.chunk != null)
            {
                Chunks(at, outward, assets.chunk, assets.chunkMaterial, 11, scale * 0.009f, scale * 0.028f, 1f, 38f);
                if (assets.chipMaterial != null)
                {
                    Chunks(at, outward, assets.chunk, assets.chipMaterial, 5, scale * 0.004f, scale * 0.012f, 1.25f, 38f);
                }
            }
            Sfx.Play(clips, at, 1f, 0.93f, 1.07f);
        }
    }

    private static void Chunks(Vector3 at, Vector3 outward, Mesh looks, Material material, int many, float smallest, float biggest, float fast, float spread)
    {
        int n = Mathf.RoundToInt(Blood.Share(many) * many);
        if (n < 2) n = 1;

        GameObject obj = new GameObject("GoreChunks");
        obj.transform.SetPositionAndRotation(at, Quaternion.LookRotation(outward, Vector3.up));
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(fast * 1.2f, fast * 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(smallest, biggest);
        main.startRotation3D = true;
        var spin = new ParticleSystem.MinMaxCurve(0f, 6.2831855f);
        main.startRotationZ = spin;
        main.startRotationY = spin;
        main.startRotationX = spin;
        main.gravityModifier = 1.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = n;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)n) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spread;
        shape.radius = 0.03f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        var turn = new ParticleSystem.MinMaxCurve(-8f, 8f);
        rot.z = turn;
        rot.y = turn;
        rot.x = turn;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        var curve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 1f),
            new Keyframe(0.8f, 1f),
            new Keyframe(1f, 0f)
        });
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.Medium;
        collision.collidesWith = -5893;
        collision.enableDynamicColliders = false;
        collision.dampen = 0.75f;
        collision.bounce = 0.12f;
        collision.lifetimeLoss = 0f;

        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = looks;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
        ps.Play();
        Destroy(obj, 5f);
    }

    public void Clear()
    {
        soons.Clear();
        toBreak.Clear();
        breaking = null;
        breakingUpTo = serials;
        brokenUpTo = serials;
        busy.Remove(this);
        Array.Clear(drawnOn, 0, 6);
        count = 0;
        next = 0;

        foreach (var e in eyesOut)
        {
            if (e.eye != null) e.eye.localScale = e.was;
        }
        eyesOut.Clear();
        skinMats.Clear();

        if (boneMat != null) Destroy(boneMat);
        if (gutMat != null) Destroy(gutMat);
        if (wallMat != null) Destroy(wallMat);
        wallMat = null;
        gutMat = null;
        boneMat = null;

        foreach (Mesh m in wallMeshes)
        {
            if (m != null) Destroy(m);
        }
        wallMeshes.Clear();

        foreach (var p in pieces)
        {
            if (p.go != null) Destroy(p.go);
        }
        pieces.Clear();
        breakable.Clear();

        Array.Clear(showing, 0, showing.Length);
        Array.Clear(armOff, 0, armOff.Length);
        Array.Clear(cuts, 0, cuts.Length);

        foreach (var w in whole)
        {
            if (w.skin != null)
            {
                w.skin.sharedMesh = w.mesh;
                w.skin.sharedMaterials = w.mats;
            }
        }
        whole.Clear();

        foreach (Material mat in wounded.Values)
        {
            if (mat != null) Destroy(mat);
        }
        wounded.Clear();
        hurt = false;
        if (copy != null)
        {
            copy.SetCaps();
        }
    }

    private static void Shape_g__Try_109_0(Vector3 a, Vector3 b, float weight, GoreSection section, string bone,
        ref Vector3 at, ref float size, ref float nearestD, ref GoreSection nearestSection, ref string nearestBone,
        ref Vector3 nearestWay, ref Vector3 nearestAt)
    {
        Vector3 closest;
        float d = ToSegment(at, a, b, out closest) - size * weight;
        if (d < nearestD)
        {
            nearestD = d;
            nearestSection = section;
            nearestBone = bone;
            Vector3 diff = at - closest;
            Vector3 way = diff.sqrMagnitude <= 1e-10f ? Vector3.up : diff.normalized;
            nearestWay = way;
            nearestAt = closest + way * (size * weight);
        }
    }

    private static float Land_g__R_120_0(ref System.Random rng)
    {
        return (float)rng.NextDouble();
    }
}
