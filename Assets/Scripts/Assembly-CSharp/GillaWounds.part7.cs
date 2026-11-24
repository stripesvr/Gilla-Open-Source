using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GillaWounds
{
    private static BoneOut BreakMath(BoneShape s, Vector4[] wounds, Vector4[] axes, Vector4[] sides, int count, float gone, float thick, Vector4 cut)
    {
        int n = s.middles.Length;
        float cutSq = (cut - Vector4.zero).sqrMagnitude;
        byte[] state = new byte[n];
        byte[] nearest = new byte[n];
        float[] dist = new float[n];

        for (int t = 0; t < n; t++)
        {
            if (count <= 0) continue;
            float best = 1.5f;
            int bi = -1;
            for (int w = 0; w < count; w++)
            {
                float size = wounds[w].w;
                if (size >= 0.001f)
                {
                    float stretch = axes[w].w;
                    if (stretch <= 1.4f) stretch = 1.4f;
                    Vector3 m = s.middles[t];
                    float reach = size * 1.5f * stretch;
                    Vector3 d = m - (Vector3)wounds[w];
                    if (d.sqrMagnitude <= reach * reach)
                    {
                        float ow = BreakMath_OutOfWound(m, w, wounds, axes, sides) /* ? */;
                        if (ow < best)
                        {
                            bi = w;
                            best = ow;
                        }
                    }
                }
            }
            if (bi >= 0)
            {
                nearest[t] = (byte)bi;
                dist[t] = best;
                byte st;
                if (cutSq < 1e-10f)
                {
                    if (best <= 0.4f)
                    {
                        st = 2;
                    }
                    else
                    {
                        float jag = Jag(s.middles[t], wounds[bi].w);
                        st = (best / gone + jag >= 1f) ? (byte)1 : (byte)2;
                    }
                }
                else
                {
                    Vector3 m = s.middles[t];
                    float jag = Jag(m, wounds[bi].w);
                    st = 2;
                    if (cut.z * m.z + cut.x * m.x + cut.y * m.y + jag * wounds[bi].w * 0.35f <= cut.w)
                    {
                        st = 1;
                    }
                }
                state[t] = st;
            }
        }

        for (int pass = 0; pass < 2; pass++)
        {
            for (int t = 0; t < n; t++)
            {
                if (state[t] == 1)
                {
                    int open = 0;
                    for (int e = 0; e < 3; e++)
                    {
                        int nb = s.across[t * 3 + e];
                        if (nb < 0 || state[nb] != 2) open++;
                    }
                    if (open < 2) state[t] = 2;
                }
            }
        }

        bool[] reached = new bool[n];
        Stack<int> stack = new Stack<int>();
        for (int t = 0; t < n; t++)
        {
            if (state[t] == 1 && (cutSq >= 1e-10f || dist[t] > 1.25f))
            {
                reached[t] = true;
                stack.Push(t);
            }
        }
        while (stack.Count > 0)
        {
            int t = stack.Pop();
            for (int e = 0; e < 3; e++)
            {
                int nb = s.across[t * 3 + e];
                if (nb >= 0 && state[nb] == 1 && !reached[nb])
                {
                    reached[nb] = true;
                    stack.Push(nb);
                }
            }
        }
        for (int t = 0; t < n; t++)
        {
            if (state[t] == 1 && !reached[t]) state[t] = 2;
        }

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> faces = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector4> opening = new List<Vector4>();
        List<Vector2> depth = new List<Vector2>();
        List<Vector3> wayOut = new List<Vector3>();
        List<int> index = new List<int>();

        // Local helper matching <BreakMath>g__Put|185_1(at, face, w, isBreak, byBreak) from metadata.
        int Put(Vector3 at, Vector3 face, int w, float isBreak, float byBreak)
        {
            int id = verts.Count;
            verts.Add(at);
            faces.Add(face.normalized);
            Vector3 axis = new Vector3(axes[w].x, axes[w].y, axes[w].z);
            wayOut.Add((-axis).normalized) /* ? */;
            uv.Add(new Vector2(OutOf(at, wounds[w], axes[w], sides[w]), Jag(at, wounds[w].w))) /* ? */;
            opening.Add(wounds[w]) /* ? */;
            depth.Add(new Vector2(isBreak, byBreak)) /* ? */;
            return id;
        }

        bool[] edge = new bool[s.points];
        for (int t = 0; t < n; t++)
        {
            if (state[t] != 1) continue;
            for (int e = 0; e < 3; e++)
            {
                int nb = s.across[t * 3 + e];
                if (nb >= 0 && state[nb] == 2)
                {
                    edge[s.point[s.corners[t * 3 + e]]] = true;
                    int e2 = e == 2 ? 0 : e + 1;
                    edge[s.point[s.corners[t * 3 + e2]]] = true;
                }
            }
        }

        int[] map = new int[s.points];
        for (int i = 0; i < map.Length; i++) map[i] = -1;

        for (int t = 0; t < n; t++)
        {
            if (state[t] != 1) continue;
            for (int e = 0; e < 3; e++)
            {
                int c = s.corners[t * 3 + e];
                int p = s.point[c];
                int v = map[p];
                if (v < 0)
                {
                    v = Put(s.at[c], s.faces[c], nearest[t], 0f, edge[p] ? 1f : 0f) /* ? */;
                    map[p] = v;
                }
                index.Add(v);
            }
            if (thick > 0f)
            {
                for (int e = 0; e < 3; e++)
                {
                    int nb = s.across[t * 3 + e];
                    if (nb < 0 || state[nb] != 2) continue;
                    int e2 = e == 2 ? 0 : e + 1;
                    int a = s.corners[t * 3 + e];
                    int b = s.corners[t * 3 + e2];
                    Vector3 pa = s.at[a];
                    Vector3 pb = s.at[b];
                    Vector3 na = s.faces[a].normalized;
                    Vector3 nbn = s.faces[b].normalized;
                    Vector3 along = pb - pa;
                    Vector3 side = Vector3.Cross(along, s.faces[a] + s.faces[b]).normalized;
                    if (Vector3.Dot(side, s.middles[t] - (pa + pb) * 0.5f) < 0f)
                    {
                        side = -side;
                    }
                    int w = nearest[t];
                    int i0 = Put(pa, side, w, 1f, 1f) /* ? */;
                    int i1 = Put(pb, side, w, 1f, 1f) /* ? */;
                    Vector3 pb2 = pb - nbn * thick;
                    int i2 = Put(pb2, side, w, 1f, 1f) /* ? */;
                    Vector3 pa2 = pa - na * thick;
                    int i3 = Put(pa2, side, w, 1f, 1f) /* ? */;
                    float f = Vector3.Dot(Vector3.Cross(along, pb2 - pa), side);
                    bool up = f > 0f;
                    index.Add(i0);
                    index.Add(up ? i1 : i2);
                    index.Add(up ? i2 : i1);
                    index.Add(i0);
                    index.Add(up ? i2 : i3);
                    index.Add(up ? i3 : i2);
                }
            }
        }

        BoneOut o = new BoneOut();
        o.verts = verts;
        o.faces = faces;
        o.wayOut = wayOut;
        o.uv = uv;
        o.depth = depth;
        o.opening = opening;
        o.index = index;
        return o;
    }

    private void BreakPut(Broken b, BoneOut o)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Gore_Bone";
        mesh.SetVertices(o.verts);
        mesh.SetNormals(o.faces);
        mesh.SetUVs(0, o.uv);
        mesh.SetUVs(1, o.opening);
        mesh.SetUVs(2, o.depth);
        mesh.SetUVs(3, o.wayOut);
        mesh.SetTriangles(o.index, 0);
        mesh.RecalculateBounds();
        if (b.left != null)
        {
            wallMeshes.Remove(b.left);
            UnityEngine.Object.Destroy(b.left);
        }
        wallMeshes.Add(mesh);
        b.left = mesh;
        b.version++;
        b.filter.sharedMesh = mesh;
        MeshRenderer r;
        if (b.filter.TryGetComponent<MeshRenderer>(out r))
        {
            r.enabled = true;
        }
    }

    private void HoleOf(int i, out Vector3 hole, out Vector3 wayOut, out float across)
    {
        Vector4 axis = axes[i];
        Transform t = walls[i].transform;
        float back = wounds[i].w * axes[i].w * 0.12f;
        Vector3 at = new Vector3(wounds[i].x - axis.x * back, wounds[i].y - axis.y * back, wounds[i].z - axis.z * back);
        hole = t.TransformPoint(at);
        wayOut = t.TransformDirection(new Vector3(-axis.x, -axis.y, -axis.z)).normalized;
        across = wounds[i].w * t.lossyScale.x;
    }

    public int BoneLeftIn(float much)
    {
        int found = 0;
        foreach (Broken b in breakable)
        {
            if (b.left == null) continue;
            Vector3[] vs = b.left.vertices;
            int[] tris = b.left.triangles;
            for (int k = 0; k < tris.Length; k += 3)
            {
                Vector3 p = (vs[tris[k]] + vs[tris[k + 1]] + vs[tris[k + 2]]) / 3f;
                for (int w = 0; w < count; w++)
                {
                    if (wounds[w].w >= 0.001f)
                    {
                        if (OutOf(p, w) < much)
                        {
                            found++;
                            break;
                        }
                    }
                }
            }
        }
        return found;
    }

    private float OutOf(Vector3 p, int i)
    {
        return OutOf(p, wounds[i], axes[i], sides[i]);
    }

    private static float OutOf(Vector3 p, Vector4 wound, Vector4 way, Vector4 side)
    {
        float dx = p.x - wound.x;
        float dy = p.y - wound.y;
        float dz = p.z - wound.z;
        float along = dz * way.z + dx * way.x + dy * way.y;
        dx -= way.x * along;
        dy -= way.y * along;
        dz -= way.z * along;
        float ang = Mathf.Atan2(
            (way.x * side.y - way.y * side.x) * dz + (way.y * side.z - way.z * side.y) * dx + (way.z * side.x - way.x * side.z) * dy,
            side.z * dz + side.x * dx + side.y * dy);
        float wob = WobbleOf(ang, side.w);
        along /= way.w;
        float ry = way.y * along + dy / wob;
        float rx = way.x * along + dx / wob;
        float rz = way.z * along + dz / wob;
        return Mathf.Sqrt(rz * rz + rx * rx + ry * ry) / wound.w;
    }

    public void Holes(out Vector4[] holes, out Vector4[] ways, out Vector4[] holeSides, out int many)
    {
        holes = (Vector4[])wounds.Clone();
        ways = (Vector4[])axes.Clone();
        holeSides = (Vector4[])sides.Clone();
        many = count;
    }

    public Vector3 PlusX()
    {
        Find();
        (Transform bone, Matrix4x4 bind) spine = bones["spine"];
        Vector3 v = spine.bind.MultiplyVector(Vector3.right);
        return spine.bone.TransformVector(v).normalized;
    }

    private static float WobbleOf(float round, float shape)
    {
        float a = Mathf.Sin(round + round + shape);
        float b = Mathf.Sin(round * 3f + shape * 2.1f);
        float c = Mathf.Sin(round * 5f + shape * 3.7f);
        float d = Mathf.Sin(round * 7f + shape * 5.3f);
        float e = Mathf.Sin(round * 11f + shape * 7.9f);
        float f = Mathf.Sin(round * 17f + shape * 11.1f);
        return f * 0.014f + e * 0.022f + d * 0.035f + c * 0.05f + a * 0.16f + 1f + b * 0.1f;
    }

    private static float Jag(Vector3 p, float radius)
    {
        float big = Lumps(p / (radius * 0.55f));
        float small = Lumps(p / (radius * 0.22f) + Vector3.one * 17.3f);
        return (big - 0.5f) * 0.9f + (small - 0.5f) * 0.5f;
    }

    private static float Lumps(Vector3 p)
    {
        int x = (int)Math.Floor(p.x);
        int y = (int)Math.Floor(p.y);
        int z = (int)Math.Floor(p.z);
        float fx = p.x - x;
        float fy = p.y - y;
        float fz = p.z - z;
        fx = Mathf.Clamp01(fx * fx * (3f - (fx + fx)));
        fy = Mathf.Clamp01(fy * fy * (3f - (fy + fy)));
        fz = Mathf.Clamp01(fz * fz * (3f - (fz + fz)));

        Func<int, int, int, float> hash = (hx, hy, hz) =>
        {
            uint h = unchecked((uint)(hx * 0x466f45d) ^ (uint)(hy * 0x127409f) ^ (uint)(hz * 0x4f9ffb7));
            h ^= h >> 13;
            h = unchecked(h * 0x4bf19f61);
            h ^= h >> 16;
            return (h & 0xffff) / 65535f;
        } /* ? */;

        float c000 = hash(x, y, z);
        float c100 = hash(x + 1, y, z);
        float c010 = hash(x, y + 1, z);
        float c110 = hash(x + 1, y + 1, z);
        float c001 = hash(x, y, z + 1);
        float c101 = hash(x + 1, y, z + 1);
        float c011 = hash(x, y + 1, z + 1);
        float c111 = hash(x + 1, y + 1, z + 1);

        float a = Mathf.LerpUnclamped(Mathf.LerpUnclamped(c000, c100, fx), Mathf.LerpUnclamped(c010, c110, fx), fy);
        float b = Mathf.LerpUnclamped(Mathf.LerpUnclamped(c001, c101, fx), Mathf.LerpUnclamped(c011, c111, fx), fy);
        return Mathf.LerpUnclamped(a, b, fz);
    }

    private static BoneShape ShapeOf(Mesh m)
    {
        BoneShape shape;
        bool have;
        lock (boneShapes)
        {
            have = boneShapes.TryGetValue(m, out shape);
            if (!have) shape = null;
        }
        if (!have)
        {
            Vector3[] at = m.vertices;
            Vector3[] faces = m.normals;
            int[] corners = m.triangles;
            Bounds b = m.bounds;
            shape = Shaped(m, at, faces, corners, b);
        }
        return shape;
    }

    private static bool WillShape(Mesh m, out Vector3[] at, out Vector3[] faces, out int[] corners, out Bounds bounds)
    {
        faces = null;
        at = null;
        corners = null;
        bounds = default(Bounds);
        bool has;
        lock (boneShapes)
        {
            has = boneShapes.ContainsKey(m);
        }
        if (!has)
        {
            at = m.vertices;
            faces = m.normals;
            corners = m.triangles;
            bounds = m.bounds;
            return true;
        }
        return false;
    }

    private static BoneShape Shaped(Mesh m, Vector3[] at, Vector3[] faces, int[] corners, Bounds bounds)
    {
        BoneShape shape = new BoneShape();
        shape.at = at;
        shape.faces = faces;
        shape.corners = corners;
        shape.bounds = bounds;
        shape.point = new int[shape.at.Length];
        Dictionary<Vector3Int, int> weld = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < shape.at.Length; i++)
        {
            int kx = (int)Math.Round(shape.at[i].x * 50000f);
            int ky = (int)Math.Round(shape.at[i].y * 50000f);
            int kz = (int)Math.Round(shape.at[i].z * 50000f);
            Vector3Int key = new Vector3Int(kx, ky, kz);
            int id;
            if (!weld.TryGetValue(key, out id))
            {
                id = weld.Count;
                weld[key] = id;
            }
            shape.point[i] = id;
        }
        shape.points = weld.Count;

        int len = shape.corners.Length;
        int tris = len / 3;
        shape.middles = new Vector3[tris];
        shape.across = new int[tris * 3];
        Dictionary<long, int> open = new Dictionary<long, int>();
        if (len >= 3)
        {
            for (int t = 0; t < tris; t++)
            {
                int a = shape.corners[t * 3];
                int b = shape.corners[t * 3 + 1];
                int c = shape.corners[t * 3 + 2];
                shape.middles[t] = (shape.at[a] + shape.at[b] + shape.at[c]) / 3f;
                for (int e = 0; e < 3; e++)
                {
                    int k = t * 3 + e;
                    int e2 = e == 2 ? 0 : e + 1;
                    int p1 = shape.point[shape.corners[k]];
                    int p2 = shape.point[shape.corners[t * 3 + e2]];
                    int lo = Mathf.Min(p1, p2);
                    int hi = Mathf.Max(p1, p2);
                    shape.across[k] = -1;
                    long key = ((long)lo << 32) | (uint)hi;
                    int other;
                    if (open.TryGetValue(key, out other))
                    {
                        shape.across[k] = other / 3;
                        shape.across[other] = t;
                        open.Remove(key);
                    }
                    else
                    {
                        open[key] = k;
                    }
                }
            }
        }
        lock (boneShapes)
        {
            boneShapes[m] = shape;
        }
        return shape;
    }
}
