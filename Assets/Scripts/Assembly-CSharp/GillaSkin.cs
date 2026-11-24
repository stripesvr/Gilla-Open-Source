// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class GillaSkin
{
    private class Points
    {
        public int count;
        public int[] bone;
        public float[] weight;
        public Vector3[] at;
        public Vector3[] way;
        public Vector3[][] shape;
    }

    private static float[] shapeOn = new float[8];
    private const int Ways = 4;
    private static readonly Dictionary<Mesh, Points> made = new Dictionary<Mesh, Points>();
    private static readonly List<Mesh> gone = new List<Mesh>();
    private const int Most = 800;
    private static readonly List<SkinnedMeshRenderer> found = new List<SkinnedMeshRenderer>();
    private static Matrix4x4[] boneTo = new Matrix4x4[64];
    private static bool[] boneHas = new bool[64];
    private static float[] nearD = new float[8];
    private static Vector3[] nearP = new Vector3[8];
    private static Vector3[] nearN = new Vector3[8];

    public static float Nearest(Component body, List<Vector3> at)
    {
        if (at == null || body == null || at.Count == 0)
        {
            return float.PositiveInfinity;
        }
        if (nearD.Length < at.Count)
        {
            nearD = new float[at.Count];
            nearP = new Vector3[at.Count];
            nearN = new Vector3[at.Count];
        }
        for (int i = 0; i < at.Count; i++)
        {
            nearD[i] = float.MaxValue;
        }
        found.Clear();
        body.GetComponentsInChildren<SkinnedMeshRenderer>(false, found);
        bool any = false;
        foreach (SkinnedMeshRenderer smr in found)
        {
            if (!smr.enabled || smr.forceRenderingOff) continue;
            if (smr.sharedMesh == null) continue;
            if (smr.name == "SonarGhost") continue;
            if (smr.GetComponentInParent<CosmeticToy>() != null) continue;

            Points p = Of(smr.sharedMesh, smr.sharedMaterials);
            Transform[] bones = smr.bones;
            if (p == null || bones == null || bones.Length == 0) continue;

            int shapes = p.shape == null ? 0 : p.shape.Length;
            if (shapeOn.Length < shapes)
            {
                shapeOn = new float[shapes];
            }
            bool anyShape = false;
            for (int s = 0; s < shapes; s++)
            {
                shapeOn[s] = smr.GetBlendShapeWeight(s) / 100f;
                anyShape |= shapeOn[s] != 0f;
            }
            if (boneTo.Length < bones.Length)
            {
                boneTo = new Matrix4x4[bones.Length];
                boneHas = new bool[bones.Length];
            }
            for (int b = 0; b < bones.Length; b++)
            {
                boneHas[b] = bones[b] != null;
                if (boneHas[b])
                {
                    boneTo[b] = bones[b].localToWorldMatrix;
                }
            }
            for (int k = 0; k < p.count; k++)
            {
                Vector3 pos = Vector3.zero;
                float total = 0f;
                for (int j = k * 4; j < k * 4 + 4; j++)
                {
                    float w = p.weight[j];
                    if (w > 0f)
                    {
                        int b = p.bone[j];
                        if (b < bones.Length && boneHas[b])
                        {
                            Vector3 v = p.at[j];
                            if (anyShape && shapes > 0)
                            {
                                for (int s = 0; s < shapes; s++)
                                {
                                    if (shapeOn[s] != 0f)
                                    {
                                        v += p.shape[s][j] * shapeOn[s];
                                    }
                                }
                            }
                            pos += boneTo[b].MultiplyPoint3x4(v) * w;
                            total += w;
                        }
                    }
                }
                if (total > 0f)
                {
                    Vector3 point = pos / total;
                    for (int i = 0; i < at.Count; i++)
                    {
                        float d = (point - at[i]).sqrMagnitude;
                        if (d < nearD[i])
                        {
                            nearD[i] = d;
                            nearP[i] = point;
                            Vector3 n = Vector3.zero;
                            for (int j = k * 4; j < k * 4 + 4; j++)
                            {
                                float w = p.weight[j];
                                if (w > 0f)
                                {
                                    int b = p.bone[j];
                                    if (b < bones.Length && boneHas[b])
                                    {
                                        n += boneTo[b].MultiplyVector(p.way[j]) * w;
                                    }
                                }
                            }
                            nearN[i] = n;
                        }
                    }
                    any = true;
                }
            }
        }
        if (!any)
        {
            return float.PositiveInfinity;
        }
        if (at.Count < 1)
        {
            return float.PositiveInfinity;
        }
        float result = float.PositiveInfinity;
        for (int i = 0; i < at.Count; i++)
        {
            float d = nearD[i];
            Vector3 q = at[i];
            float signed = Vector3.Dot(q - nearP[i], nearN[i]) >= 0f ? Mathf.Sqrt(d) : -Mathf.Sqrt(d);
            result = Mathf.Min(result, signed);
        }
        return result;
    }

    private static Points Of(Mesh m, Material[] mats)
    {
        Points p;
        if (made.TryGetValue(m, out p))
        {
            return p;
        }
        if (made.Count >= 48)
        {
            gone.Clear();
            foreach (Mesh key in made.Keys)
            {
                if (key == null)
                {
                    gone.Add(key);
                }
            }
            foreach (Mesh g in gone)
            {
                made.Remove(g);
            }
        }
        p = null;
        bool readable = m.isReadable;
        if (mats != null && readable)
        {
            HashSet<int> used = new HashSet<int>();
            for (int sub = 0; sub < m.subMeshCount; sub++)
            {
                if (sub >= mats.Length) break;
                string name = "";
                if (mats[sub] != null)
                {
                    name = mats[sub].name;
                }
                if (!name.Contains("Eye") && !name.Contains("Flesh") && !name.Contains("Bone"))
                {
                    int[] tris = m.GetTriangles(sub);
                    for (int t = 0; t < tris.Length; t++)
                    {
                        used.Add(tris[t]);
                    }
                }
            }
            Vector3[] verts = m.vertices;
            Vector3[] normals = m.normals;
            BoneWeight[] weights = m.boneWeights;
            Matrix4x4[] bindposes = m.bindposes;
            if (used.Count > 0 && weights.Length == verts.Length && bindposes.Length != 0)
            {
                List<int> sorted = new List<int>(used);
                sorted.Sort();
                int step = Math.Max(1, (int)(sorted.Count / 800f));
                List<int> boneList = new List<int>();
                List<float> weightList = new List<float>();
                List<Vector3> atList = new List<Vector3>();
                List<Vector3> wayList = new List<Vector3>();
                List<int> vertList = new List<int>();
                int[] idx = new int[4];
                float[] w = new float[4];
                int count = 0;
                for (int index = 0; index < sorted.Count; index += step)
                {
                    int v = sorted[index];
                    BoneWeight bw = weights[v];
                    idx[0] = bw.boneIndex0;
                    idx[1] = bw.boneIndex1;
                    idx[2] = bw.boneIndex2;
                    idx[3] = bw.boneIndex3;
                    w[0] = bw.weight0;
                    w[1] = bw.weight1;
                    w[2] = bw.weight2;
                    w[3] = bw.weight3;
                    bool ok = false;
                    for (int j = 0; j < 4; j++)
                    {
                        bool valid = w[j] > 0f && idx[j] >= 0 && idx[j] < bindposes.Length;
                        ok = valid || ok;
                    }
                    if (ok)
                    {
                        Vector3 normal = v < normals.Length ? normals[v] : Vector3.up;
                        for (int j = 0; j < 4; j++)
                        {
                            bool valid = w[j] > 0f && idx[j] >= 0 && idx[j] < bindposes.Length;
                            boneList.Add(valid ? idx[j] : 0);
                            weightList.Add(valid ? w[j] : 0f);
                            atList.Add(valid ? bindposes[idx[j]].MultiplyPoint3x4(verts[v]) : Vector3.zero);
                            wayList.Add(valid ? bindposes[idx[j]].MultiplyVector(normal) : Vector3.zero);
                        }
                        vertList.Add(v);
                        count++;
                    }
                }
                if (count > 0)
                {
                    p = new Points();
                    p.count = count;
                    p.bone = boneList.ToArray();
                    p.weight = weightList.ToArray();
                    p.at = atList.ToArray();
                    p.way = wayList.ToArray();
                    if (m.blendShapeCount > 0)
                    {
                        p.shape = new Vector3[m.blendShapeCount][];
                        Vector3[] delta = new Vector3[verts.Length];
                        for (int s = 0; s < m.blendShapeCount; s++)
                        {
                            int frames = m.GetBlendShapeFrameCount(s);
                            m.GetBlendShapeFrameVertices(s, frames - 1, delta, null, null);
                            Vector3[] arr = new Vector3[count * 4];
                            int i = 0;
                            for (int k = 0; k < count; k++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (p.weight[i + j] > 0f)
                                    {
                                        int b = p.bone[i + j];
                                        int vi = vertList[k];
                                        arr[i + j] = bindposes[b].MultiplyVector(delta[vi]);
                                    }
                                }
                                i += 4;
                            }
                            p.shape[s] = arr;
                        }
                    }
                }
            }
        }
        made[m] = p;
        return p;
    }
}
