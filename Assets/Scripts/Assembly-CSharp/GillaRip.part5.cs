using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

public static partial class GillaRip
{
    static GillaRip()
    {
        cuts = new Dictionary<ValueTuple<string, int, int>, Task<GillaRip.Half>>();
        queue = Task.CompletedTask;
        CutMark = new ProfilerMarker("Gilla.Rip.Cut");
        MeshMark = new ProfilerMarker("Gilla.Rip.Mesh");
    }

    // GillaWounds.ShapeOf / Jag / OutOf are not accessible from GillaRip in the reconstructed
    // GillaWounds; they were internal in the original build. These bridges call them by reflection.
    // WoundsJag and WoundsOutOf are defined in another part of this partial class.
    private const BindingFlags WoundsFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static MethodInfo WoundsMethod(string name, int args) /* ? */
    {
        foreach (MethodInfo m in typeof(GillaWounds).GetMethods(WoundsFlags))
        {
            if (m.Name == name && m.GetParameters().Length == args)
            {
                return m;
            }
        }
        return null;
    }

    private static GillaWounds.BoneShape WoundsShapeOf(Mesh whole) /* ? */
    {
        return (GillaWounds.BoneShape)WoundsMethod("ShapeOf", 1).Invoke(null, new object[] { whole });
    }

    private static float Area(GillaRip.Shape s, List<int> loop)
    {
        if (loop.Count < 1)
        {
            return 0f;
        }
        float sum = 0f;
        int i = 0;
        do
        {
            Vector3 a = s.at[loop[i]];
            i++;
            Vector3 b = s.at[loop[i % loop.Count]];
            sum += a.y * b.z - a.z * b.y;
        } while (i < loop.Count);
        return sum * 0.5f;
    }

    private static bool Inside(GillaRip.Shape s, List<int> loop, Vector2 p)
    {
        if (loop.Count < 1)
        {
            return false;
        }
        bool inside = false;
        int j = loop.Count - 1;
        for (int i = 0; i < loop.Count; j = i, i++)
        {
            Vector3 a = s.at[loop[i]];
            Vector3 b = s.at[loop[j]];
            if ((p.y < a.z) == (b.z <= p.y) &&
                p.x < a.y + (p.y - a.z) * (b.y - a.y) / (b.z - a.z))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    private static void EarClip(GillaRip.Shape c, int start, int count)
    {
        float Turn(Vector2 a, Vector2 b, Vector2 d)
        {
            return (b.x - a.x) * (d.y - a.y) - (b.y - a.y) * (d.x - a.x);
        }

        List<int> idx = new List<int>(count);
        for (int k = 0; k < count; k++)
        {
            idx.Add(start + k);
        }
        if (count >= 1)
        {
            float area = 0f;
            for (int k = 1; k <= count; k++)
            {
                Vector3 a = c.at[idx[k - 1]];
                Vector3 b = c.at[idx[k == count ? 0 : k]];
                area += a.y * b.z - a.z * b.y;
            }
            if (area < 0f)
            {
                idx.Reverse();
            }
        }

        List<int> outTris = c.tris[0];
        int n = idx.Count;
        int from = 0;
        while (n > 3)
        {
            int ear = -1;
            for (int k = 0; k < n && ear < 0; k++)
            {
                int i = (k + from) % n;
                int prev = (n - 1 + i) % n;
                int next = (i + 1) % n;
                Vector3 pa = c.at[idx[prev]];
                Vector3 pb = c.at[idx[i]];
                Vector3 pd = c.at[idx[next]];
                Vector2 a = new Vector2(pa.y, pa.z);
                Vector2 b = new Vector2(pb.y, pb.z);
                Vector2 d = new Vector2(pd.y, pd.z);
                if (Turn(a, b, d) > 1e-9f)
                {
                    bool blocked = false;
                    for (int j = 0; j < n; j++)
                    {
                        if (j == next || j == i || j == prev) continue;
                        Vector3 pp = c.at[idx[j]];
                        Vector2 p = new Vector2(pp.y, pp.z);
                        if (Turn(a, b, p) >= 0f && Turn(b, d, p) >= 0f && Turn(d, a, p) >= 0f)
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (!blocked)
                    {
                        ear = i;
                    }
                }
            }
            if (ear < 0)
            {
                float best = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    Vector3 pa = c.at[idx[(n + i - 1) % n]];
                    Vector3 pb = c.at[idx[i]];
                    Vector3 pd = c.at[idx[i + 1 == n ? 0 : i + 1]];
                    float t = (pb.y - pa.y) * (pd.z - pa.z) - (pb.z - pa.z) * (pd.y - pa.y);
                    if (t > best)
                    {
                        best = t;
                        ear = i;
                    }
                }
            }
            outTris.Add(idx[(n - 1 + ear) % n]);
            outTris.Add(idx[ear]);
            from = ear + 1;
            outTris.Add(idx[from % n]);
            idx.RemoveAt(ear);
            n = idx.Count;
        }
        outTris.Add(idx[0]);
        outTris.Add(idx[1]);
        outTris.Add(idx[2]);
    }

    private static void Flip(GillaRip.Shape c)
    {
        float Across(int i, int j, int k)
        {
            Vector3 a = c.at[i];
            Vector3 b = c.at[j];
            Vector3 o = c.at[k];
            float ux = a.y - o.y;
            float uy = a.z - o.z;
            float vx = b.y - o.y;
            float vy = b.z - o.z;
            float cross = Mathf.Abs(ux * vy - uy * vx);
            if (cross <= 1e-12f) cross = 1e-12f;
            return (ux * vx + uy * vy) / cross;
        }

        float Turn(Vector2 a, Vector2 b, Vector2 d)
        {
            return (b.x - a.x) * (d.y - a.y) - (b.y - a.y) * (d.x - a.x);
        }

        List<int> tris = c.tris[0];
        int pass = 0;
        while (true)
        {
            Dictionary<long, int> edges = new Dictionary<long, int>();
            HashSet<int> done = new HashSet<int>();
            bool changed = false;
            for (int t = 0; t < tris.Count; t += 3)
            {
                for (int k = 0; k < 3; k++)
                {
                    int a = tris[t + k];
                    int b = tris[t + (k == 2 ? 0 : k + 1)];
                    int o = tris[t + (k + 2) % 3];
                    long key = b > a
                        ? ((long)a << 32) | (uint)b
                        : ((long)b << 32) | (uint)a;
                    if (edges.TryGetValue(key, out int other))
                    {
                        edges.Remove(key);
                        int tri = other / 3 * 3;
                        if (!done.Contains(tri))
                        {
                            int r = other % 3;
                            int oa = tris[other];
                            int ob = tris[tri + (r + 1) % 3];
                            int oo = tris[tri + (r + 2) % 3];
                            if (Across(a, b, o) + Across(oa, ob, oo) < -1e-5f)
                            {
                                Vector3 p0 = c.at[a];
                                Vector3 p1 = c.at[b];
                                Vector3 pc = c.at[o];
                                Vector3 pd = c.at[oo];
                                float dx = pd.y - pc.y;
                                float dz = pd.z - pc.z;
                                if (((p0.z - pc.z) * dx - (p0.y - pc.y) * dz) *
                                    ((p1.z - pc.z) * dx - (p1.y - pc.y) * dz) < 0f)
                                {
                                    tris[t] = o;
                                    tris[t + 1] = a;
                                    tris[t + 2] = oo;
                                    tris[tri] = oo;
                                    tris[tri + 1] = b;
                                    tris[tri + 2] = o;
                                    done.Add(t);
                                    done.Add(tri);
                                    changed = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        edges[key] = t + k;
                    }
                }
            }
            if (!changed) return;
            pass++;
            if (pass == 40) return;
        }
    }

    public static Task<object> Bones(Mesh whole, float gone, float thick, int sign, int seed, Vector4[] holes, Vector4[] axes, Vector4[] sides, int holeCount, Vector3 faceAt, float faceAcross)
    {
        GillaWounds.BoneShape s = WoundsShapeOf(whole); /* ? */
        return Task.Run<object>(() => Bones_b__0(s, gone, thick, sign, seed, holes, axes, sides, holeCount, faceAt, faceAcross));
    }

    public static Mesh BoneMesh(object made)
    {
        if (made is GillaRip.Built built)
        {
            return Make(built);
        }
        return null;
    }

    private static GillaRip.Built Bone(GillaWounds.BoneShape s, float gone, float thick, int sign, int seed, Vector4[] holes, Vector4[] axes, Vector4[] sides, int holeCount, Vector3 faceAt, float faceAcross)
    {
        int n = s.middles.Length;
        byte[] state = new byte[n];
        float visible = 0.01f;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = s.middles[i];
            float into = Into(p, sign, seed);
            float jag = WoundsJag(p, 0.03f); /* ? */
            bool hit = false;
            bool near = false;
            for (int h = 0; h < holeCount; h++)
            {
                if (holes[h].w >= 0.001f)
                {
                    float o = WoundsOutOf(p, holes[h], axes[h], sides[h]); /* ? */
                    near |= o < 1.5f;
                    if (0.4f < o && 1f <= o / gone + WoundsJag(p, holes[h].w)) /* ? */
                    {
                        continue;
                    }
                    hit = true;
                }
            }
            into += jag * 0.004f;
            if (into < 0f || hit)
            {
                state[i] = 2;
            }
            else if (!(visible <= into && !near))
            {
                state[i] = 1;
            }
        }

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < n; i++)
            {
                if (state[i] != 1) continue;
                int open = 0;
                for (int k = 0; k < 3; k++)
                {
                    int nb = s.across[i * 3 + k];
                    if (nb < 0 || state[nb] != 2) open++;
                }
                if (open < 2) state[i] = 2;
            }
        }

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> faces = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector4> opening = new List<Vector4>();
        List<Vector2> depth = new List<Vector2>();
        List<Vector3> wayOut = new List<Vector3>();
        List<int> tris = new List<int>();
        Vector3 way = Vector3.right * (float)(-sign);
        Vector4 openAt = new Vector4(faceAt.x, faceAt.y, faceAt.z, faceAcross);

        int Put(Vector3 pos, Vector3 normal, float u, float v)
        {
            verts.Add(pos);
            faces.Add(normal);
            uv.Add(new Vector2(u, v));
            opening.Add(openAt);
            float d = Into(pos, sign, seed) / visible;
            depth.Add(new Vector2(Mathf.Clamp01(d) * 0.34f, 0.5f));
            wayOut.Add(way);
            return verts.Count - 1;
        }

        bool[] edge = new bool[s.points];
        for (int i = 0; i < n; i++)
        {
            if (state[i] != 1) continue;
            for (int k = 0; k < 3; k++)
            {
                int nb = s.across[i * 3 + k];
                if (nb >= 0 && state[nb] == 2)
                {
                    edge[s.point[s.corners[i * 3 + k]]] = true;
                    edge[s.point[s.corners[i * 3 + (k == 2 ? 0 : k + 1)]]] = true;
                }
            }
        }

        int[] map = new int[s.points];
        for (int i = 0; i < map.Length; i++)
        {
            map[i] = -1;
        }

        for (int i = 0; i < n; i++)
        {
            if (state[i] != 1) continue;
            for (int k = 0; k < 3; k++)
            {
                int c = s.corners[i * 3 + k];
                int pt = s.point[c];
                int v = map[pt];
                if (v < 0)
                {
                    v = Put(s.at[c], s.faces[c], 0f, edge[pt] ? 1f : 0f);
                    map[pt] = v;
                }
                tris.Add(v);
            }
            if (thick > 0f)
            {
                for (int k = 0; k < 3; k++)
                {
                    int nb = s.across[i * 3 + k];
                    if (nb < 0 || state[nb] != 2) continue;
                    int c0 = s.corners[i * 3 + k];
                    int c1 = s.corners[i * 3 + (k == 2 ? 0 : k + 1)];
                    Vector3 a = s.at[c0];
                    Vector3 b = s.at[c1];
                    Vector3 n0 = s.faces[c0].normalized;
                    Vector3 n1 = s.faces[c1].normalized;
                    Vector3 e = b - a;
                    Vector3 wall = Vector3.Cross(e, s.faces[c0] + s.faces[c1]).normalized;
                    if (Vector3.Dot(wall, s.middles[nb] - (a + b) * 0.5f) < 0f)
                    {
                        wall = -wall;
                    }
                    Vector3 bIn = b - n1 * thick;
                    Vector3 aIn = a - n0 * thick;
                    int i0 = Put(a, wall, 1f, 1f);
                    int i1 = Put(b, wall, 1f, 1f);
                    int i2 = Put(bIn, wall, 1f, 1f);
                    int i3 = Put(aIn, wall, 1f, 1f);
                    bool flip = Vector3.Dot(wall, Vector3.Cross(e, bIn - a)) <= 0f;
                    tris.Add(i0);
                    tris.Add(flip ? i2 : i1);
                    tris.Add(flip ? i1 : i2);
                    tris.Add(i0);
                    tris.Add(flip ? i3 : i2);
                    tris.Add(flip ? i2 : i3);
                }
            }
        }

        if (tris.Count == 0)
        {
            return null;
        }
        GillaRip.Built built = new GillaRip.Built();
        built.name = "Corpse_Bone";
        built.at = verts;
        built.normals = faces;
        built.uv = uv;
        built.second = opening;
        built.thirdShort = depth;
        built.fourth = wayOut;
        built.tris = new List<int>[] { tris };
        return built;
    }

    public static Mesh Trim(Mesh whole, int sign, int seed)
    {
        Vector3[] verts = whole.vertices;
        int[] tri = whole.triangles;
        List<int> keep = new List<int>(tri.Length);
        for (int t = 0; t < tri.Length; t += 3)
        {
            Vector3 a = verts[tri[t]];
            Vector3 b = verts[tri[t + 1]];
            Vector3 d = verts[tri[t + 2]];
            Vector3 p = (a + b + d) / 3f;
            float x = sign * p.x;
            if (-0.0015f <= x)
            {
                if (x < 0.0175f && Into(p, sign, seed) < 0.008f)
                {
                    continue;
                }
                keep.Add(tri[t]);
                keep.Add(tri[t + 1]);
                keep.Add(tri[t + 2]);
            }
        }
        if (keep.Count == 0)
        {
            return null;
        }
        Mesh mesh = UnityEngine.Object.Instantiate<Mesh>(whole);
        mesh.name = "Corpse_Wall";
        mesh.SetTriangles(keep, 0);
        return mesh;
    }

    private static Vector2Int Bowl_SquareOf_33_2(Vector2 p)
    {
        return new Vector2Int((int)(p.x / 0.008), (int)(p.y / 0.008));
    }
}
