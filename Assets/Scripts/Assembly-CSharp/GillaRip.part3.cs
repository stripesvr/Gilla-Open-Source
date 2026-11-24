// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static partial class GillaRip
{
    private static GillaRip.Half Work(GillaRip.Source source, int sign, int seed)
    {
        Stopwatch watch = Stopwatch.StartNew();
        Shape s = new Shape();
        s.at.AddRange(source.at);
        s.faces.AddRange(source.faces);
        s.uv.AddRange(source.uv);
        s.weights.AddRange(source.weights);
        s.tris = new List<int>[source.tris.Length];
        float gone = 0.024f;
        float facing = 0f;
        for (int t = 0; t < s.tris.Length; t++)
        {
            int[] tri = source.tris[t];
            List<int> kept = new List<int>(tri.Length / 2);
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = s.at[tri[i]];
                Vector3 b = s.at[tri[i + 1]];
                Vector3 c = s.at[tri[i + 2]];
                Vector3 face = s.faces[tri[i]];
                float fs = (float)sign;
                float limit = -gone;
                if (limit <= a.x * fs || limit <= b.x * fs || limit <= c.x * fs)
                {
                    kept.Add(tri[i]);
                    kept.Add(tri[i + 1]);
                    kept.Add(tri[i + 2]);
                }
                facing += Vector3.Dot(Vector3.Cross(b - a, c - a), face);
            }
            s.tris[t] = kept;
        }

        float Over(Vector3 p)
        {
            return Into(p, sign, seed); /* ? */
        }

        s.Refine((p, q) => Work_b__0(gone, p, q), 5);
        List<ValueTuple<int, int>> rim = s.Clip(p => Over(p));
        Half half = new Half();
        half.builtSkin = Skin(s, source, p => Over(p));
        float dir = -1f;
        if (0f <= facing)
        {
            dir = 1f;
        }
        Bowl(s, rim, source, half, sign, seed, dir);
        half.took = (float)watch.ElapsedMilliseconds;
        return half;
    }

    private static GillaRip.Built Skin(GillaRip.Shape s, GillaRip.Source source, Func<Vector3, float> over)
    {
        int[] remap = new int[s.at.Count];
        for (int i = 0; i < remap.Length; i++)
        {
            remap[i] = -1;
        }
        List<Vector3> at = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector2> depth = new List<Vector2>();
        List<BoneWeight> weights = new List<BoneWeight>();
        List<int>[] tris = new List<int>[s.tris.Length];
        for (int t = 0; t < tris.Length; t++)
        {
            tris[t] = new List<int>(s.tris[t].Count);
            foreach (int v in s.tris[t])
            {
                if (remap[v] < 0)
                {
                    remap[v] = at.Count;
                    at.Add(s.at[v]);
                    normals.Add(s.faces[v]);
                    uv.Add(s.uv[v]);
                    weights.Add(s.weights[v]);
                    float o = over(s.at[v]);
                    depth.Add(new Vector2(Mathf.Max(o, 0f), 0f));
                }
                tris[t].Add(remap[v]);
            }
        }
        Built built = new Built();
        built.name = source.name + "_Half";
        built.at = at;
        built.normals = normals;
        built.uv = uv;
        built.secondShort = depth;
        built.third = normals;
        built.fourth = at;
        built.weights = weights.ToArray();
        built.binds = source.binds;
        built.tris = tris;
        return built;
    }
}
