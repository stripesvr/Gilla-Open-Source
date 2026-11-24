// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

public static partial class Blood
{
    static Blood()
    {
        BloodMark = new ProfilerMarker("Gilla.Blood");
        Dark = new Color(0.306f, 0.02f, 0.027f, 1f);
        Bright = new Color(0.612f, 0.094f, 0.075f, 1f);
    }

    public static ParticleSystem Particles(string name, Transform at, Vector3 dir, Material mat, float seconds, int most)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(at, false);
        obj.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.duration = seconds;
        main.loop = false;
        main.startSize3D = true;
        main.startColor = new ParticleSystem.MinMaxGradient(Dark, Bright);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = most;
        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        Keyframe[] keys = new Keyframe[3];
        keys[0] = new Keyframe(0f, 1f);
        keys[1] = new Keyframe(0.7f, 1f);
        keys[2] = new Keyframe(1f, 0f);
        AnimationCurve curve = new AnimationCurve(keys);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.alignment = ParticleSystemRenderSpace.Velocity;
        renderer.mesh = Drop();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        renderer.enableGPUInstancing = false;
        Object.Destroy(obj, seconds + 2f);
        return ps;
    }

    public static AnimationCurve Pulses(int count)
    {
        AnimationCurve curve = new AnimationCurve();
        if (count > 0)
        {
            float value = 1f;
            float step = 1f / ((float)count + 0.5f);
            for (int i = 0; i < count; i++)
            {
                float time = step * (float)i;
                curve.AddKey(new Keyframe(time, value));
                curve.AddKey(new Keyframe(step * 0.55f + time, value * 0.1f));
                value *= 0.78f;
            }
        }
        curve.AddKey(new Keyframe(1f, 0f));
        return curve;
    }

    public static Mesh Drop()
    {
        if (s_drop != null)
        {
            return s_drop;
        }
        Vector3[] verts = new Vector3[12];
        verts[0] = new Vector3(-1f, 1.618034f, 0f);
        verts[1] = new Vector3(1f, 1.618034f, 0f);
        verts[2] = new Vector3(-1f, -1.618034f, 0f);
        verts[3] = new Vector3(1f, -1.618034f, 0f);
        verts[4] = new Vector3(0f, -1f, 1.618034f);
        verts[5] = new Vector3(0f, 1f, 1.618034f);
        verts[6] = new Vector3(0f, -1f, -1.618034f);
        verts[7] = new Vector3(0f, 1f, -1.618034f);
        verts[8] = new Vector3(1.618034f, 0f, -1f);
        verts[9] = new Vector3(1.618034f, 0f, 1f);
        verts[10] = new Vector3(-1.618034f, 0f, -1f);
        verts[11] = new Vector3(-1.618034f, 0f, 1f);
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = verts[i].normalized * 0.5f;
        }
        int[] tris = new int[60]
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        } /* ? */;
        Color32[] colors = new Color32[verts.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color32(255, 255, 255, 255);
        }
        Mesh mesh = new Mesh();
        mesh.name = "BloodDrop";
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.colors32 = colors;
        s_drop = mesh;
        s_drop.RecalculateNormals();
        return s_drop;
    }
}
