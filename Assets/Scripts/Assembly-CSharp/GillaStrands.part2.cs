// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaStrands : MonoBehaviour
{
    private static void TorsoCap(SkinnedMeshRenderer torso, Transform[] bones, BoneWeight[] weights, Vector3 near, float reach, Vector3 into, int many, List<Spot> found)
    {
        Mesh m = torso.sharedMesh;
        m.GetTriangles(readTris, m.subMeshCount - 1);
        m.GetVertices(readAt);
        if (readWorld.Length < readAt.Count)
        {
            readWorld = new Vector3[readAt.Count];
        }
        Matrix4x4[] bindposes = m.bindposes;
        foreach (int v in readTris)
        {
            BoneWeight w = weights[v];
            Vector3 local = readAt[v];
            Vector3 world = (bones[w.boneIndex0].localToWorldMatrix * bindposes[w.boneIndex0]).MultiplyPoint3x4(local) * w.weight0;
            if (w.weight1 > 0.001f)
            {
                world += (bones[w.boneIndex1].localToWorldMatrix * bindposes[w.boneIndex1]).MultiplyPoint3x4(local) * w.weight1;
            }
            if (w.weight2 > 0.001f)
            {
                world += (bones[w.boneIndex2].localToWorldMatrix * bindposes[w.boneIndex2]).MultiplyPoint3x4(local) * w.weight2;
            }
            if (w.weight3 > 0.001f)
            {
                world += (bones[w.boneIndex3].localToWorldMatrix * bindposes[w.boneIndex3]).MultiplyPoint3x4(local) * w.weight3;
            }
            readWorld[v] = world;
        }
        int kept = 0;
        for (int i = 2; i < readTris.Count; i += 3)
        {
            Vector3 centre = (readWorld[readTris[i - 2]] + readWorld[readTris[i - 1]] + readWorld[readTris[i]]) / 3f;
            if ((centre - near).sqrMagnitude <= reach * reach)
            {
                int t0 = readTris[i - 2];
                int t1 = readTris[i - 1];
                int t2 = readTris[i];
                readTris[kept] = t0;
                readTris[kept + 1] = t1;
                readTris[kept + 2] = t2;
                kept += 3;
            }
        }
        readTris.RemoveRange(kept, readTris.Count - kept);
        if (readTris.Count > 8)
        {
            Places(readTris, into, null, many, found);
        }
    }

    private static Vector3 MiddleOf(List<int> tris, out float round)
    {
        float total = 0f;
        Vector3 sum = Vector3.zero;
        for (int i = 2; i < tris.Count; i += 3)
        {
            Vector3 p0 = readWorld[tris[i - 2]];
            Vector3 p1 = readWorld[tris[i - 1]];
            Vector3 p2 = readWorld[tris[i]];
            float area = Vector3.Cross(p1 - p0, p2 - p0).magnitude;
            total += area;
            sum += (p0 + p1 + p2) * area / 3f;
        }
        Vector3 middle;
        if (total <= 0f)
        {
            middle = readWorld[tris[0]];
        }
        else
        {
            middle = sum / total;
        }
        float most = 0f;
        foreach (int v in tris)
        {
            float d = (readWorld[v] - middle).sqrMagnitude;
            if (most <= d)
            {
                most = d;
            }
        }
        round = Mathf.Sqrt(most);
        return middle;
    }

    private static void Places(List<int> tris, Vector3 into, Transform on, int many, List<Spot> found)
    {
        Dictionary<long, int> edges = new Dictionary<long, int>(tris.Count);
        for (int t = 0; t + 2 < tris.Count; t += 3)
        {
            for (int k = 0; k < 3; k++)
            {
                int i0 = tris[t + k];
                int i1 = tris[t + (k == 2 ? 0 : k + 1)];
                long key = i0 < i1 ? (((long)i0 << 32) | (uint)i1) : (((long)i1 << 32) | (uint)i0);
                int count;
                edges.TryGetValue(key, out count);
                edges[key] = count + 1;
            }
        }
        List<(Vector3, Vector3)> rim = new List<(Vector3, Vector3)>();
        foreach (KeyValuePair<long, int> pair in edges)
        {
            if (pair.Value == 1)
            {
                rim.Add((readWorld[(int)(pair.Key >> 32)], readWorld[(int)(uint)pair.Key]));
            }
        }
        float[] areas = new float[tris.Count / 3];
        float total = 0f;
        for (int i = 0, j = 2; i < areas.Length; i++, j += 3)
        {
            Vector3 p0 = readWorld[tris[j - 2]];
            Vector3 p1 = readWorld[tris[j - 1]];
            Vector3 p2 = readWorld[tris[j]];
            total += Vector3.Cross(p1 - p0, p2 - p0).magnitude;
            areas[i] = total;
        }
        if (total > 0f && many > 0)
        {
            for (int n = 0; n != many; n++)
            {
                int pick = Array.BinarySearch(areas, total * UnityEngine.Random.value);
                pick = pick ^ (pick >> 31);
                if (areas.Length - 1 <= pick)
                {
                    pick = areas.Length - 1;
                }
                float r1 = UnityEngine.Random.value;
                float r2 = UnityEngine.Random.value;
                float u = 1f - r1;
                float v = 1f - r2;
                if (r1 + r2 <= 1f)
                {
                    u = r1;
                    v = r2;
                }
                int baseIndex = pick * 3;
                Vector3 p0 = readWorld[tris[baseIndex]];
                Vector3 p1 = readWorld[tris[baseIndex + 1]];
                Vector3 p2 = readWorld[tris[baseIndex + 2]];
                float w = 1f - u - v;
                Vector3 point = p0 * u + p1 * v + p2 * w;
                float room = float.MaxValue;
                foreach ((Vector3, Vector3) edge in rim)
                {
                    float d = ToSegment(point, edge.Item1, edge.Item2);
                    if (d <= room)
                    {
                        room = d;
                    }
                }
                int corner;
                if (u < v || u < w)
                {
                    corner = w <= v ? 1 : 2;
                }
                else
                {
                    corner = 0;
                }
                Spot spot = new Spot();
                spot.at = point;
                spot.into = into;
                spot.on = on;
                spot.room = room;
                spot.vertex = tris[corner + baseIndex];
                found.Add(spot);
            }
        }
    }

    private static float ToSegment(Vector3 p, Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        float len = d.sqrMagnitude;
        float t = 0f;
        if (len > 1e-12f)
        {
            float f = Vector3.Dot(p - from, d) / len;
            if (f >= 0f)
            {
                t = f;
                if (f > 1f)
                {
                    t = 1f;
                }
            }
        }
        return (p - (from + d * t)).magnitude;
    }

    private Spot Pick(List<Spot> on, Kind kind, List<Vector3> rim)
    {
        float best = -1f;
        float want = Want(kind);
        Spot chosen = new Spot();
        if (want > -1f)
        {
            for (int tries = 0; ; tries++)
            {
                Spot s = on[UnityEngine.Random.Range(0, on.Count)];
                float room = s.room;
                if (room < 0f)
                {
                    float most = float.MaxValue;
                    foreach (Vector3 r in rim)
                    {
                        float d = (r - s.at).sqrMagnitude;
                        if (d <= most)
                        {
                            most = d;
                        }
                    }
                    room = Mathf.Sqrt(most);
                }
                if (best < room)
                {
                    chosen = s;
                    best = room;
                }
                if (tries >= 15 || !(best < want))
                {
                    break;
                }
            }
        }
        chosen.room = best;
        return chosen;
    }

    private Spot Nearest(List<Spot> on, Vector3 to, Kind kind)
    {
        float want = Want(kind);
        Spot chosen = on[0];
        float best = float.MaxValue;
        foreach (Spot s in on)
        {
            float d = (s.at - to).sqrMagnitude;
            if (s.room >= 0f && s.room < want * 0.8f)
            {
                d += size * size * 0.04f;
            }
            if (d < best)
            {
                chosen = s;
                best = d;
            }
        }
        return chosen;
    }

    private float Want(Kind kind)
    {
        float w = 0.055f;
        if (kind != Kind.Sheet)
        {
            w = kind == Kind.Rope ? 0.032f : 0.016f;
        }
        return size * w;
    }

    private static BoneWeight[] WeightsOf(Mesh m)
    {
        BoneWeight[] w;
        if (weightsOf.TryGetValue(m, out w))
        {
            return w;
        }
        w = m.boneWeights;
        weightsOf[m] = w;
        return w;
    }

    private static SkinnedMeshRenderer TorsoOf(Transform side)
    {
        Transform root = null;
        Component c = side.GetComponentInParent<Dismemberment>();
        if (c != null)
        {
            root = c.transform;
        }
        else
        {
            c = side.GetComponentInParent<GillaCorpse>();
            if (c != null)
            {
                root = c.transform;
            }
        }
        if (root == null)
        {
            return null;
        }
        SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < skins.Length; i++)
        {
            SkinnedMeshRenderer s = skins[i];
            if (s.name == "Gilla_Torso" && s.sharedMesh != null && s.sharedMesh.isReadable && s.sharedMesh.subMeshCount > 1)
            {
                return s;
            }
        }
        return null;
    }

    private static Transform Steady(Transform side)
    {
        if (side.parent != null && side.GetComponentInParent<Dismemberment>() != null)
        {
            return side.parent;
        }
        return side;
    }

    private End OnSkin(Transform[] bones, BoneWeight by, Vector3 at, Transform steady)
    {
        float total = by.weight0 + by.weight1 + by.weight2;
        if (total < 0.0001f || bones[by.boneIndex0] == null)
        {
            return Rigid(steady, at);
        }
        End e = new End();
        e.b0 = Anchor(bones[by.boneIndex0]);
        e.p0 = bones[by.boneIndex0].InverseTransformPoint(at);
        e.w0 = by.weight0 / total;
        e.b1 = -1;
        e.b2 = -1;
        if (by.weight1 > 0.001f && bones[by.boneIndex1] != null)
        {
            e.b1 = Anchor(bones[by.boneIndex1]);
            e.w1 = by.weight1 / total;
            e.p1 = bones[by.boneIndex1].InverseTransformPoint(at);
        }
        if (by.weight2 > 0.001f && bones[by.boneIndex2] != null)
        {
            e.b2 = Anchor(bones[by.boneIndex2]);
            e.w2 = by.weight2 / total;
            e.p2 = bones[by.boneIndex2].InverseTransformPoint(at);
        }
        return e;
    }

    private void Build()
    {
        int vertexCount = 0;
        int indexCount = 0;
        for (int i = 0; i < strands.Length; i++)
        {
            Strand s = strands[i];
            s.firstVertex = vertexCount;
            indexCount += s.sides * 96;
            vertexCount += s.sides * 17;
        }
        dropFirst = vertexCount;
        vertexCount += DropRound.Length * 14;
        indexCount += DropTris.Length * 14;
        verts = new Vertex[vertexCount];
        ushort[] data = new ushort[indexCount];
        int at = 0;
        for (int i = 0; i < strands.Length; i++)
        {
            Strand s = strands[i];
            int sides = s.sides;
            for (int r = 0; r < 16; r++)
            {
                int rowStart = s.firstVertex + sides * r;
                for (int j = 0; j < sides; j++)
                {
                    int a0 = rowStart + j;
                    int next = (j + 1) % sides;
                    data[at] = (ushort)a0;
                    data[at + 1] = (ushort)(a0 + sides);
                    data[at + 2] = (ushort)(rowStart + next);
                    data[at + 3] = (ushort)(rowStart + next);
                    data[at + 4] = (ushort)(a0 + sides);
                    data[at + 5] = (ushort)(rowStart + sides + next);
                    at += 6;
                }
            }
            float v = UnityEngine.Random.value;
            for (int r = 0; r < 17; r++)
            {
                for (int j = 0; j < s.sides; j++)
                {
                    int idx = j + s.firstVertex + r * s.sides;
                    verts[idx].uv.x = v + r * 0.035f;
                    verts[idx].uv.y = v * 3.7f + Mathf.Abs((j + j) / (float)s.sides - 1f) * 0.5f;
                }
            }
        }
        for (int d = 0; d < 14; d++)
        {
            int first = dropFirst + d * DropRound.Length;
            int k = 0;
            for (; k < DropTris.Length; k++)
            {
                data[at + k] = (ushort)(DropTris[k] + first);
            }
            at += k;
            for (int n = 0; n < DropRound.Length; n++)
            {
                verts[first + n].uv.x = d * 0.13f;
                verts[first + n].uv.y = n * 0.09f;
            }
        }
        Mesh m = new Mesh();
        m.name = "Strands";
        mesh = m;
        mesh.MarkDynamic();
        VertexAttributeDescriptor[] attributes = new VertexAttributeDescriptor[4];
        attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
        attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0);
        attributes[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0);
        attributes[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0);
        mesh.SetVertexBufferParams(vertexCount, attributes);
        mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
        mesh.SetIndexBufferData(data, 0, 0, indexCount, (MeshUpdateFlags)9);
        mesh.subMeshCount = 1;
        mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles), (MeshUpdateFlags)9);
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = gore.strand;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        renderer.allowOcclusionWhenDynamic = false;
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        live.Remove(this);
    }
}
