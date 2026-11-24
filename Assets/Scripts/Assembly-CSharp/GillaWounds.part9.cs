// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public partial class GillaWounds
{
    private static float Stump_R(ref Random rng)
    {
        return (float)rng.NextDouble();
    }

    private static float Spot_Frac(float x)
    {
        return x - (float)(int)x;
    }

    private static void BendsWith_Say(float amount, int index, Dictionary<Transform, float> says, Transform[] by)
    {
        float had = 0f;
        if (0f < amount)
        {
            if (index < by.Length)
            {
                if (!(by[index] == null))
                {
                    says.TryGetValue(by[index], out had);
                    says[by[index]] = had + amount;
                }
            }
        }
    }

    private static float BendsWith_Of(Transform t, BoneWeight w, Transform[] by)
    {
        float sum = 0f;
        if (w.boneIndex0 < by.Length && by[w.boneIndex0] == t)
        {
            sum = w.weight0 + 0f;
        }
        if (w.boneIndex1 < by.Length && by[w.boneIndex1] == t)
        {
            sum += w.weight1;
        }
        if (w.boneIndex2 < by.Length && by[w.boneIndex2] == t)
        {
            sum += w.weight2;
        }
        if (w.boneIndex3 < by.Length && by[w.boneIndex3] == t)
        {
            sum += w.weight3;
        }
        return sum;
    }

    private static float BreakMath_OutOfWound(Vector3 p, int i, Vector4[] wounds, Vector4[] axes, Vector4[] sides)
    {
        return OutOf(p, wounds[i], axes[i], sides[i]);
    }

    private static int BreakMath_Put(Vector3 p, Vector3 face, Vector2 uvAt, int i,
        Vector4[] wounds, Vector4[] axes, Vector4[] sides,
        List<Vector3> verts, List<Vector3> faces, List<Vector2> uv,
        List<Vector4> opening, List<Vector2> depth, List<Vector3> wayOut)
    {
        Vector4 w = wounds[i];
        Vector4 ax = axes[i];
        verts.Add(p);
        faces.Add(face);
        uv.Add(uvAt);
        float reach = w.w * ax.w;
        float under = reach * 0.12f;
        opening.Add(new Vector4(w.x - ax.x * under, w.y - ax.y * under, w.z - ax.z * under, wounds[i].w));
        float d = ((p.z - w.z) * ax.z + (p.x - w.x) * ax.x + (p.y - w.y) * ax.y + under) / (reach * 1.12f);
        float t = 0f;
        if (0f <= d)
        {
            t = d;
            if (1f < d) t = 1f;
        }
        float o = BreakMath_OutOfWound(p, i, wounds, axes, sides);
        depth.Add(new Vector2(t, o));
        wayOut.Add(new Vector3(-ax.x, -ax.y, -ax.z));
        return verts.Count - 1;
    }

    private static float Lumps_At(int dx, int dy, int dz, int x, int y, int z)
    {
        uint h = (uint)((y + dy) * 0x127409f) ^ (uint)((x + dx) * 0x466f45d) ^ (uint)((z + dz) * 0x4f9ffb7);
        h = (h ^ (h >> 13)) * 0x4bf19f61;
        return (float)((h & 0xffff) ^ (h >> 16)) / 65535f;
    }
}
