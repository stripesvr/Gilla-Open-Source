// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

public static partial class GillaRip
{
    public const int Seeds = 3;
    private const float Band = 0.014f;
    private const float FineSkin = 0.01f;
    private const float FineInside = 0.016f;
    private const float MostOff = 0.01f;
    public const float Deep = 0.006f;
    private const float PastFlesh = 0.004f;
    private static readonly Dictionary<ValueTuple<string, int, int>, Task<Half>> cuts = new Dictionary<ValueTuple<string, int, int>, Task<GillaRip.Half>>();
    private static Task queue = Task.CompletedTask;
    private static readonly ProfilerMarker CutMark = new ProfilerMarker("Gilla.Rip.Cut");
    private static readonly ProfilerMarker MeshMark = new ProfilerMarker("Gilla.Rip.Mesh");
    private static int made;

    public class Half
    {
        internal Built builtSkin;
        internal Built builtInside;
        private Mesh skin_;
        private Mesh inside_;
        public Vector3 middle;
        public float across;
        public float took;
        public int loops;
        public int covered;
        public int open;

        public Mesh skin
        {
            get
            {
                if (skin_ != null)
                {
                    return skin_;
                }
                skin_ = Make(builtSkin);
                return skin_;
            }
        }

        public Mesh inside
        {
            get
            {
                if (inside_ != null || builtInside == null)
                {
                    return inside_;
                }
                inside_ = Make(builtInside);
                return inside_;
            }
        }
    }

    internal class Built
    {
        public string name;
        public List<Vector3> at;
        public List<Vector3> normals;
        public List<Vector3> third;
        public List<Vector3> fourth;
        public List<Vector2> uv;
        public List<Vector4> second;
        public List<Vector2> secondShort;
        public BoneWeight[] weights;
        public Matrix4x4[] binds;
        public List<int>[] tris;
        public bool workOutNormals;
        public float flipNormals = 1f;
        public List<Vector2> thirdShort;
    }

    private class Source
    {
        public string name;
        public Vector3[] at;
        public Vector3[] faces;
        public Vector2[] uv;
        public BoneWeight[] weights;
        public Matrix4x4[] binds;
        public int[][] tris;
    }

    private class Shape
    {
        public readonly List<Vector3> at = new List<Vector3>();
        public readonly List<Vector3> faces = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<BoneWeight> weights = new List<BoneWeight>();
        public List<int>[] tris;

        public int Between(int a, int b, float t)
        {
            at.Add(Vector3.Lerp(at[a], at[b], t));
            faces.Add(Vector3.Lerp(faces[a], faces[b], t).normalized);
            uv.Add(Vector2.Lerp(uv[a], uv[b], t));
            weights.Add(Mix(weights[a], weights[b], t));
            return at.Count - 1;
        }

        public void Refine(Func<Vector3, Vector3, bool> splits, int passes)
        {
            if (passes <= 0)
            {
                return;
            }
            int pass = 0;
            bool changed;
            do
            {
                Dictionary<long, int> mids = new Dictionary<long, int>();
                changed = false;
                for (int t = 0; t < tris.Length; t++)
                {
                    List<int> old = tris[t];
                    List<int> now = new List<int>(old.Count);

                    int Mid(int x, int y)
                    {
                        long key = x < y ? (((long)x << 32) | (uint)y) : (((long)y << 32) | (uint)x);
                        int found;
                        if (mids.TryGetValue(key, out found))
                        {
                            return found;
                        }
                        if (splits(at[x], at[y]))
                        {
                            found = Between(Mathf.Min(x, y), Mathf.Max(x, y), 0.5f);
                        }
                        else
                        {
                            found = -1;
                        }
                        mids[key] = found;
                        return found;
                    }

                    void Add(int x, int y, int z)
                    {
                        now.Add(x);
                        now.Add(y);
                        now.Add(z);
                    }

                    for (int i = 0; i < old.Count; i += 3)
                    {
                        int a = old[i];
                        int b = old[i + 1];
                        int c = old[i + 2];
                        int ab = Mid(a, b);
                        int bc = Mid(b, c);
                        int ca = Mid(c, a);
                        int count = (ab >= 0 ? 1 : 0) + (bc >= 0 ? 1 : 0) + (ca >= 0 ? 1 : 0);
                        if (count == 3)
                        {
                            Add(a, ab, ca);
                            Add(ab, b, bc);
                            Add(ca, bc, c);
                            Add(ab, bc, ca);
                            changed = true;
                        }
                        else if (count == 1)
                        {
                            if (ab < 0)
                            {
                                if (bc >= 0)
                                {
                                    Add(a, b, bc);
                                    Add(a, bc, c);
                                }
                                else
                                {
                                    Add(a, b, ca);
                                    Add(b, c, ca);
                                }
                            }
                            else
                            {
                                Add(a, ab, c);
                                Add(ab, b, c);
                            }
                            changed = true;
                        }
                        else if (count == 2)
                        {
                            if (ca < 0)
                            {
                                Add(ab, b, bc);
                                Add(a, ab, bc);
                                Add(a, bc, c);
                            }
                            else if (ab < 0)
                            {
                                Add(bc, c, ca);
                                Add(b, bc, ca);
                                Add(b, ca, a);
                            }
                            else
                            {
                                Add(ca, a, ab);
                                Add(c, ca, ab);
                                Add(c, ab, b);
                            }
                            changed = true;
                        }
                        else
                        {
                            Add(a, b, c);
                        }
                    }
                    tris[t] = now;
                }
                pass++;
            }
            while (changed && pass != passes);
        }

        public List<ValueTuple<int, int>> Clip(Func<Vector3, float> into)
        {
            List<ValueTuple<int, int>> rim = new List<ValueTuple<int, int>>();
            Dictionary<long, int> on = new Dictionary<long, int>();

            int Cross(int keep, int gone)
            {
                long key = ((long)keep << 32) | (uint)gone;
                int found;
                if (on.TryGetValue(key, out found))
                {
                    return found;
                }
                float lo = 0f;
                float hi = 1f;
                for (int k = 8; k != 0; k--)
                {
                    float mid = (hi + lo) * 0.5f;
                    if (into(Vector3.Lerp(at[keep], at[gone], mid)) >= 0f)
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid;
                    }
                }
                found = Between(keep, gone, lo);
                on[key] = found;
                return found;
            }

            float[] values = new float[at.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = into(at[i]);
            }
            for (int t = 0; t < tris.Length; t++)
            {
                List<int> old = tris[t];
                List<int> now = new List<int>(old.Count);
                for (int i = 0; i < old.Count; i += 3)
                {
                    int a = old[i];
                    int b = old[i + 1];
                    int c = old[i + 2];
                    float vb = values[b];
                    float vc = values[c];
                    int count = (values[a] >= 0f ? 1 : 0) + (vb >= 0f ? 1 : 0) + (vc >= 0f ? 1 : 0);
                    if (count == 0)
                    {
                        continue;
                    }
                    if (count == 3)
                    {
                        now.Add(a);
                        now.Add(b);
                        now.Add(c);
                        continue;
                    }
                    int n2 = a;
                    int n1 = c;
                    int lone = b;
                    if ((vb >= 0f) == (count != 1))
                    {
                        n2 = c;
                        n1 = b;
                        lone = a;
                        if ((vc >= 0f) != (count != 1))
                        {
                            n2 = b;
                            n1 = a;
                            lone = c;
                        }
                    }
                    int e1;
                    int e2;
                    if (count == 1)
                    {
                        e2 = Cross(lone, n1);
                        e1 = Cross(lone, n2);
                        now.Add(lone);
                        now.Add(e2);
                        now.Add(e1);
                    }
                    else
                    {
                        e1 = Cross(n1, lone);
                        e2 = Cross(n2, lone);
                        now.Add(e1);
                        now.Add(n1);
                        now.Add(n2);
                        now.Add(e1);
                        now.Add(n2);
                        now.Add(e2);
                    }
                    rim.Add(new ValueTuple<int, int>(e2, e1));
                }
                tris[t] = now;
            }
            return rim;
        }
    }

    private static bool Bowl_b__33_1(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude > 0.00025600003f;
    }

    private static Half PrepareMesh_b__0(Source source, int side, int tear)
    {
        return Work(source, side, tear);
    }

    private static GillaWounds.BoneShape PrepareShape_b__0(Mesh m, Vector3[] at, Vector3[] faces, int[] corners, Bounds bounds)
    {
        return (GillaWounds.BoneShape)typeof(GillaWounds).GetMethod("Shaped", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { m, at, faces, corners, bounds }); /* ? */
    }

    private static Half Cut_b__0(Source read, int sign, int seed)
    {
        return Work(read, sign, seed);
    }

    private static bool Work_b__0(float gone, Vector3 a, Vector3 b)
    {
        if ((a - b).sqrMagnitude <= 0.0001f)
        {
            return false;
        }
        float least = Mathf.Min(Mathf.Abs(a.x), Mathf.Abs(b.x));
        if (gone <= least)
        {
            return a.x * b.x < 0f;
        }
        return true;
    }

    private static float Work_g__Over_1(Vector3 p, int sign, int seed)
    {
        return Into(p, sign, seed);
    }

    private static int Bowl_b__0(Shape s, List<int> a, List<int> b)
    {
        float areaB = Mathf.Abs(Area(s, b));
        return Mathf.Abs(Area(s, a)).CompareTo(areaB);
    }

    private static int Loops_g__Point_0(Shape s, Dictionary<Vector3Int, List<int>> cells, int v)
    {
        Vector3 p = s.at[v];
        int cx = (int)(p.x / 0.0002f);
        int cy = (int)(p.y / 0.0002f);
        int cz = (int)(p.z / 0.0002f);
        for (int dx = -1; dx != 2; dx++)
        {
            for (int dy = -1; dy != 2; dy++)
            {
                for (int dz = -1; dz != 2; dz++)
                {
                    List<int> list;
                    if (cells.TryGetValue(new Vector3Int(cx + dx, cy + dy, cz + dz), out list))
                    {
                        foreach (int idx in list)
                        {
                            if ((s.at[idx] - p).sqrMagnitude < 2.4999998e-09f)
                            {
                                return idx;
                            }
                        }
                    }
                }
            }
        }
        Vector3Int cell = new Vector3Int(cx, cy, cz);
        List<int> own;
        if (!cells.TryGetValue(cell, out own))
        {
            own = new List<int>(1);
            cells[cell] = own;
        }
        own.Add(v);
        return v;
    }

    private static void Loops_g__TakeEdge_1(Dictionary<int, List<int>> onward, int a, int b)
    {
        onward[a].Remove(b);
        onward[b].Remove(a);
    }
}
