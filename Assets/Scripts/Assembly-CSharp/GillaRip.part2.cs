// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;

public static partial class GillaRip
{
    private static object CallWounds(string name, object[] args) /* ? GillaWounds members are not accessible from here */
    {
        MethodInfo[] methods = typeof(GillaWounds).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name == name && methods[i].GetParameters().Length == args.Length)
            {
                return methods[i].Invoke(null, args);
            }
        }
        throw new MissingMethodException("GillaWounds", name);
    }

    private static float WoundsJag(Vector3 p, float size)
    {
        return (float)CallWounds("Jag", new object[] { p, size }) /* ? */;
    }

    private static float WoundsOutOf(Vector3 p, Vector4 hole, Vector4 axis, Vector4 side)
    {
        return (float)CallWounds("OutOf", new object[] { p, hole, axis, side }) /* ? */;
    }

    private static float WoundsLumps(Vector3 p)
    {
        return (float)CallWounds("Lumps", new object[] { p }) /* ? */;
    }

    private static bool WoundsWillShape(Mesh m, out Vector3[] at, out Vector3[] faces, out int[] corners, out Bounds bounds)
    {
        object[] args = new object[] { m, null, null, null, default(Bounds) };
        bool ok = (bool)CallWounds("WillShape", args) /* ? */;
        at = (Vector3[])args[1];
        faces = (Vector3[])args[2];
        corners = (int[])args[3];
        bounds = (Bounds)args[4];
        return ok;
    }

    private static Task<T> Next<T>(Func<T> work)
    {
        Task<T> t = (queue ?? Task.CompletedTask).ContinueWith(_ => work());
        queue = t;
        return t;
    }

    private static int Loops_b__2(Dictionary<int, List<int>> onward, int x, int y)
    {
        int a = onward[x].Count % 2 != 1 ? 1 : 0;
        int b = onward[y].Count % 2 != 1 ? 1 : 0;
        return a.CompareTo(b);
    }

    private static Built Bones_b__0(GillaWounds.BoneShape s, float gone, float thick, int sign, int seed,
        Vector4[] holes, Vector4[] axes, Vector4[] sides, int holeCount, Vector3 faceAt, float faceAcross)
    {
        int n = s.middles.Length;
        byte[] state = new byte[n];
        float visible = 0.01f;
        for (int i = 0; i < n; i++)
        {
            Vector3 p = s.middles[i];
            float into = Into(p, sign, seed);
            float jag = WoundsJag(p, 0.03f);
            bool hit = false;
            bool near = false;
            for (int h = 0; h < holeCount; h++)
            {
                if (holes[h].w >= 0.001f)
                {
                    float o = WoundsOutOf(p, holes[h], axes[h], sides[h]);
                    near |= o < 1.5f;
                    if (o > 0.4f && o / gone + WoundsJag(p, holes[h].w) >= 1f)
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
                int count = 0;
                for (int k = 0; k < 3; k++)
                {
                    int o = s.across[i * 3 + k];
                    if (o < 0 || state[o] != 2) count++;
                }
                if (count < 2) state[i] = 2;
            }
        }

        List<Vector3> verts = new List<Vector3>();
        List<Vector3> faces = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector4> opening = new List<Vector4>();
        List<Vector2> depth = new List<Vector2>();
        List<Vector3> wayOut = new List<Vector3>();
        List<int> tris = new List<int>();
        Vector4 open = new Vector4(faceAt.x, faceAt.y, faceAt.z, faceAcross);
        Vector3 way = Vector3.right * -sign;

        int BonePut(Vector3 at, Vector3 face, float isBreak, float byBreak,
            List<Vector3> pVerts, List<Vector3> pFaces, List<Vector2> pUv, List<Vector4> pOpening, Vector4 pOpen,
            List<Vector2> pDepth, int pSign, int pSeed, float pVisible, List<Vector3> pWayOut, Vector3 pWay) /* ? local Put reconstructed */
        {
            pVerts.Add(at);
            pFaces.Add(face.normalized);
            pUv.Add(new Vector2(isBreak, byBreak));
            pOpening.Add(pOpen);
            pDepth.Add(new Vector2(Into(at, pSign, pSeed), pVisible));
            pWayOut.Add(pWay);
            return pVerts.Count - 1;
        }

        bool[] edge = new bool[s.points];
        for (int i = 0; i < n; i++)
        {
            if (state[i] != 1) continue;
            for (int k = 0; k < 3; k++)
            {
                int o = s.across[i * 3 + k];
                if (o >= 0 && state[o] == 2)
                {
                    int next = k == 2 ? 0 : k + 1;
                    edge[s.point[s.corners[i * 3 + k]]] = true;
                    edge[s.point[s.corners[i * 3 + next]]] = true;
                }
            }
        }

        int[] map = new int[s.points];
        for (int i = 0; i < map.Length; i++) map[i] = -1;

        for (int i = 0; i < n; i++)
        {
            if (state[i] != 1) continue;
            for (int k = 0; k < 3; k++)
            {
                int c = s.corners[i * 3 + k];
                int pt = s.point[c];
                int idx = map[pt];
                if (idx < 0)
                {
                    idx = BonePut(s.at[c], s.faces[c], 0f, edge[pt] ? 1f : 0f,
                        verts, faces, uv, opening, open, depth, sign, seed, visible, wayOut, way) /* ? */;
                    map[pt] = idx;
                }
                tris.Add(idx);
            }
            if (thick > 0f)
            {
                for (int k = 0; k < 3; k++)
                {
                    int o = s.across[i * 3 + k];
                    if (o < 0 || state[o] != 2) continue;
                    int next = k == 2 ? 0 : k + 1;
                    int c0 = s.corners[i * 3 + k];
                    int c1 = s.corners[i * 3 + next];
                    Vector3 a = s.at[c0];
                    Vector3 b = s.at[c1];
                    Vector3 na = s.faces[c0].normalized;
                    Vector3 nb = s.faces[c1].normalized;
                    Vector3 d = b - a;
                    Vector3 nrm = Vector3.Cross(d, s.faces[c0] + s.faces[c1]).normalized;
                    Vector3 outward = Vector3.Dot(nrm, s.middles[o] - (a + b) * 0.5f) >= 0f ? nrm : -nrm;
                    Vector3 b2 = b - nb * thick;
                    Vector3 a2 = a - na * thick;
                    int i0 = BonePut(a, outward, 1f, 1f, verts, faces, uv, opening, open, depth, sign, seed, visible, wayOut, way) /* ? */;
                    int i1 = BonePut(b, outward, 1f, 1f, verts, faces, uv, opening, open, depth, sign, seed, visible, wayOut, way) /* ? */;
                    int i2 = BonePut(b2, outward, 1f, 1f, verts, faces, uv, opening, open, depth, sign, seed, visible, wayOut, way) /* ? */;
                    int i3 = BonePut(a2, outward, 1f, 1f, verts, faces, uv, opening, open, depth, sign, seed, visible, wayOut, way) /* ? */;
                    float f = Vector3.Dot(Vector3.Cross(d, b2 - a), outward);
                    tris.Add(i0);
                    tris.Add(f <= 0f ? i2 : i1);
                    tris.Add(f <= 0f ? i1 : i2);
                    tris.Add(i0);
                    tris.Add(f <= 0f ? i3 : i2);
                    tris.Add(f <= 0f ? i2 : i3);
                }
            }
        }

        if (tris.Count == 0)
        {
            return null;
        }
        Built built = new Built();
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

    internal static Mesh Make(Built b)
    {
        MeshMark.Begin();
        Mesh mesh = new Mesh();
        mesh.name = b.name;
        if (b.at.Count > 65000)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }
        mesh.SetVertices(b.at);
        if (b.normals != null) mesh.SetNormals(b.normals);
        mesh.SetUVs(0, b.uv);
        if (b.second != null) mesh.SetUVs(1, b.second);
        if (b.secondShort != null) mesh.SetUVs(1, b.secondShort);
        if (b.third != null) mesh.SetUVs(2, b.third);
        if (b.thirdShort != null) mesh.SetUVs(2, b.thirdShort);
        if (b.fourth != null) mesh.SetUVs(3, b.fourth);
        if (b.workOutNormals)
        {
            mesh.SetUVs(4, new List<Vector2>(new Vector2[b.at.Count]));
        }
        if (b.weights != null)
        {
            mesh.boneWeights = b.weights;
            mesh.bindposes = b.binds;
        }
        mesh.subMeshCount = b.tris.Length;
        for (int i = 0; i < b.tris.Length; i++)
        {
            mesh.SetTriangles(b.tris[i], i);
        }
        if (b.workOutNormals)
        {
            mesh.RecalculateNormals();
            if (b.flipNormals < 0f)
            {
                Vector3[] normals = mesh.normals;
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = -normals[i];
                }
                mesh.normals = normals;
            }
        }
        mesh.RecalculateBounds();
        MeshMark.End();
        return mesh;
    }

    private static Source Read(Mesh m)
    {
        Source src = new Source();
        src.name = m.name;
        src.at = m.vertices;
        src.faces = m.normals;
        src.uv = m.uv;
        src.weights = m.boneWeights;
        src.binds = m.bindposes;
        src.tris = new int[m.subMeshCount][];
        for (int i = 0; i < src.tris.Length; i++)
        {
            src.tris[i] = m.GetTriangles(i);
        }
        return src;
    }

    public static void Prepare(Dismemberment body)
    {
        if (body == null) return;
        SkinnedMeshRenderer[] renderers = body.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SkinnedMeshRenderer r = renderers[i];
            if (r.sharedMesh != null)
            {
                if (r.name.EndsWith("Torso") || r.name.EndsWith("Head"))
                {
                    PrepareMesh(r.sharedMesh);
                }
            }
        }

        CosmeticCatalog catalog = CosmeticCatalog.Get();
        if (catalog != null && catalog.cosmetics != null)
        {
            for (int i = 0; i < catalog.cosmetics.Length; i++)
            {
                CosmeticCatalog.Entry entry = catalog.cosmetics[i];
                CosmeticSkin skin = null;
                if (entry != null && entry.prefab != null)
                {
                    skin = entry.prefab.GetComponentInChildren<CosmeticSkin>(true);
                }
                if (skin == null) continue;
                SkinnedMeshRenderer[] skinRenderers = skin.GetComponentsInChildren<SkinnedMeshRenderer>(true); /* ? meshes/names fields */
                for (int k = 0; k < skinRenderers.Length; k++)
                {
                    Mesh mesh = skinRenderers[k].sharedMesh;
                    if (mesh != null && mesh.isReadable)
                    {
                        string n = skinRenderers[k].name;
                        if (n != null && (n.EndsWith("Torso") || n.EndsWith("Head")))
                        {
                            PrepareMesh(mesh);
                        }
                    }
                }
            }
        }

        GillaGoreAssets assets = Resources.Load<GillaGoreAssets>("GillaGore");
        if (assets != null && assets.pieces != null) /* ? field names */
        {
            for (int i = 0; i < assets.pieces.Length; i++)
            {
                var piece = assets.pieces[i];
                if (piece.mesh != null)
                {
                    if (piece.section != GoreSection.ArmL && piece.section != GoreSection.ArmR) /* ? owner == body check */
                    {
                        PrepareShape(piece.mesh);
                    }
                }
            }
        }
    }

    private static void PrepareMesh(Mesh mesh)
    {
        Source source = null;
        for (int tear = 0; tear < 3; tear++)
        {
            int[] sidesArr = new int[] { 1, -1 };
            for (int j = 0; j < sidesArr.Length; j++)
            {
                int side = sidesArr[j];
                var key = (mesh.name, side, tear);
                if (!cuts.ContainsKey(key))
                {
                    if (source == null) source = Read(mesh);
                    Source src = source;
                    int t = tear;
                    cuts[key] = Next(() => Work(src, side, t) /* ? */);
                }
            }
        }
    }

    public static bool Ready
    {
        get { return queue.IsCompleted; }
    }

    public static int PickSeed()
    {
        List<int> ready = new List<int>(3);
        for (int seed = 0; seed < 3; seed++)
        {
            bool any = false;
            bool all = true;
            foreach (var kv in cuts)
            {
                if (seed == kv.Key.Item3)
                {
                    all &= kv.Value.IsCompleted;
                    any = true;
                }
            }
            if (all && any)
            {
                ready.Add(seed);
            }
        }
        if (ready.Count < 1) return 0;
        return ready[UnityEngine.Random.Range(0, ready.Count)];
    }

    public static void MakeOneMore()
    {
        if (made < cuts.Count * 2)
        {
            int i = 0;
            foreach (var kv in cuts)
            {
                Task<Half> task = kv.Value;
                if (!task.IsCompleted || task.IsFaulted) continue;
                if (i++ < made / 2) continue;
                Half half = task.Result;
                if ((made & 1) == 0)
                {
                    if (half.skin == null)
                    {
                        Debug.LogWarning("[GillaRip] no skin for " + kv.Key.ToString());
                    }
                }
                else
                {
                    if (half.inside == null)
                    {
                        Debug.LogWarning("[GillaRip] no flesh for " + kv.Key.ToString());
                    }
                }
                made++;
                break;
            }
        }
    }

    private static void PrepareShape(Mesh m)
    {
        Vector3[] at;
        Vector3[] faces;
        int[] corners;
        Bounds bounds;
        if (WoundsWillShape(m, out at, out faces, out corners, out bounds))
        {
            Next(() => PrepareShape_b__0(m, at, faces, corners, bounds));
        }
    }

    private static float Line(float y, float z, int seed)
    {
        float a = WoundsLumps(new Vector3(y / 0.06f, z / 0.06f, (seed * 3.7f) / 0.06f));
        float b = WoundsLumps(new Vector3(y / 0.017f, z / 0.017f, (seed * 3.7f) / 0.017f) + Vector3.one * 9.1f);
        return (a - 0.5f) * 0.012f + (b - 0.5f) * 0.006f;
    }

    private static float Into(Vector3 p, int sign, int seed)
    {
        return (p.x - Line(p.y, p.z, seed)) * sign;
    }

    private static BoneWeight Mix(BoneWeight a, BoneWeight b, float t)
    {
        Span<int> bone = stackalloc int[8];
        Span<float> much = stackalloc float[8];
        float u = 1f - t;
        int n = 0;
        float w0 = u * a.weight0;
        if (w0 > 0f)
        {
            bone[0] = a.boneIndex0;
            much[0] = w0;
            n = 1;
        }
        n = AddBone(bone, much, n, a.boneIndex1, u * a.weight1);
        n = AddBone(bone, much, n, a.boneIndex2, u * a.weight2);
        n = AddBone(bone, much, n, a.boneIndex3, u * a.weight3);
        n = AddBone(bone, much, n, b.boneIndex0, b.weight0 * t);
        n = AddBone(bone, much, n, b.boneIndex1, b.weight1 * t);
        n = AddBone(bone, much, n, b.boneIndex2, b.weight2 * t);
        n = AddBone(bone, much, n, b.boneIndex3, b.weight3 * t);
        if (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (much[i] < much[j])
                    {
                        float tw = much[i]; much[i] = much[j]; much[j] = tw;
                        int tb = bone[i]; bone[i] = bone[j]; bone[j] = tb;
                    }
                }
            }
            float sum = 0f;
            int keep = Mathf.Min(n, 4);
            for (int i = 0; i < keep; i++) sum += much[i];
            if (sum > 0f)
            {
                BoneWeight r = new BoneWeight();
                if (n >= 1) { r.weight0 = much[0] / sum; r.boneIndex0 = bone[0]; }
                if (n >= 2) { r.weight1 = much[1] / sum; r.boneIndex1 = bone[1]; }
                if (n >= 3) { r.weight2 = much[2] / sum; r.boneIndex2 = bone[2]; }
                if (n >= 4) { r.weight3 = much[3] / sum; r.boneIndex3 = bone[3]; }
                return r;
            }
        }
        return a;
    }

    private static int AddBone(Span<int> bone, Span<float> much, int n, int which, float how)
    {
        if (how > 0f)
        {
            for (int i = 0; i < n; i++)
            {
                if (bone[i] == which)
                {
                    much[i] += how;
                    return n;
                }
            }
            bone[n] = which;
            much[n] = how;
            n++;
        }
        return n;
    }

    public static Half Cut(Mesh source, int sign, int seed)
    {
        using (CutMark.Auto())
        {
            var key = (source.name, sign, seed);
            Task<Half> task;
            if (!cuts.TryGetValue(key, out task))
            {
                Source read = Read(source);
                task = Task.Run(() => Work(read, sign, seed) /* ? */);
                cuts[key] = task;
            }
            return task.Result;
        }
    }
}
