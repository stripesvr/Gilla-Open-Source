// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GillaWounds : MonoBehaviour
{
    private Transform BendsWith(GoreSection section, Vector3 at, float reach, out Transform bone, out Matrix4x4 bind, out Matrix4x4 otherBind, out List<(Vector3 at, float share)> places)
    {
        bone = null;
        bind = default;
        otherBind = default;
        places = null;
        var says = new Dictionary<Transform, float>();
        var found = new List<(Vector3, BoneWeight, Transform[])>();

        foreach (var (skin, mesh, mats) in whole)
        {
            if (TornSkin(skin))
                continue;
            Vector3[] verts = Vertices(mesh);
            if (!boneWeights.TryGetValue(mesh, out BoneWeight[] weights))
            {
                weights = mesh.boneWeights;
                boneWeights[mesh] = weights;
            }
            if (weights.Length != verts.Length)
                continue;
            Transform[] by = skin.bones;
            int[] pts = SkinPoints(mesh, mats);
            for (int k = 0; k < pts.Length; k++)
            {
                int i = pts[k];
                if ((verts[i] - at).sqrMagnitude > reach * reach)
                    continue;
                if (fitted != null && fitted.sectionOf[i] != (byte)section)
                    continue;
                BoneWeight w = weights[i];
                found.Add((verts[i], w, by));
                Say(w.weight0, w.boneIndex0, says, by);
                Say(w.weight1, w.boneIndex1, says, by);
                Say(w.weight2, w.boneIndex2, says, by);
                Say(w.weight3, w.boneIndex3, says, by);
            }
        }

        float total = 0f;
        Transform best = null;
        Transform other = null;
        float bestW = 0f;
        float otherW = 0f;
        foreach (var kv in says)
        {
            total += kv.Value;
            if (kv.Value > bestW)
            {
                other = best;
                otherW = bestW;
                best = kv.Key;
                bestW = kv.Value;
            }
            else if (kv.Value > otherW)
            {
                other = kv.Key;
                otherW = kv.Value;
            }
        }

        if (best != null)
        {
            if (bones.TryGetValue(best.name, out var entry))
            {
                bone = entry.bone;
                bind = entry.bind;
            }
        }

        if (other == null || otherW < total * 0.04f)
            return null;
        if (!bones.TryGetValue(other.name, out var entry2))
            return null;
        otherBind = entry2.bind;
        places = new List<(Vector3 at, float share)>(128);
        int step = found.Count / 128;
        if (step < 2) step = 1;
        for (int i = 0; i < found.Count; i += step)
        {
            var (pos, w, by) = found[i];
            float a = Of(other, w, by);
            float b = Of(best, w, by);
            if (0f < a + b)
                places.Add((pos, a / (a + b)));
        }
        return other;

        static void Say(float weight, int index, Dictionary<Transform, float> says, Transform[] by)
        {
            if (weight <= 0f || by == null || index < 0 || index >= by.Length || by[index] == null) return; /* ? */
            says.TryGetValue(by[index], out float had);
            says[by[index]] = had + weight;
        }

        static float Of(Transform t, BoneWeight w, Transform[] by)
        {
            float sum = 0f; /* ? */
            if (w.boneIndex0 < by.Length && by[w.boneIndex0] == t) sum += w.weight0;
            if (w.boneIndex1 < by.Length && by[w.boneIndex1] == t) sum += w.weight1;
            if (w.boneIndex2 < by.Length && by[w.boneIndex2] == t) sum += w.weight2;
            if (w.boneIndex3 < by.Length && by[w.boneIndex3] == t) sum += w.weight3;
            return sum;
        }
    }

    private static List<Vector2> Bends(List<(Vector3 at, float share)> places, List<Vector3> verts)
    {
        var result = new List<Vector2>(verts.Count);
        foreach (Vector3 v in verts)
        {
            float sum = 0f;
            float total = 0f;
            foreach (var p in places)
            {
                float d = (p.at - v).sqrMagnitude + 1e-07f;
                float w = 1f / (d * d);
                total += w;
                sum += p.share * w;
            }
            float share = sum / total;
            if (total <= 0f) share = 0f;
            result.Add(new Vector2(share, 0f));
        }
        return result;
    }

    private void KnockOutEyes(GoreSection section, int wound)
    {
        if (section != GoreSection.Head)
            return;
        string[] names = new string[] { "eye_L", "eye_R" };
        for (int i = 0; i < names.Length; i++)
        {
            if (!bones.TryGetValue(names[i], out var entry))
                continue;
            if (entry.bone == null)
                continue;
            bool already = false;
            foreach (var e in eyesOut)
                already |= e.eye == entry.bone;
            if (already)
                continue;
            Vector3 p = entry.bind.inverse.MultiplyPoint3x4(Vector3.zero);
            if (OutOf(p, wound) <= 1.25f)
            {
                ApeFace face = entry.bone.GetComponentInParent<ApeFace>();
                Vector3 scale;
                if (face != null)
                    scale = face.Unswelled(entry.bone);
                else
                    scale = entry.bone.localScale;
                eyesOut.Add((entry.bone, scale));
                entry.bone.localScale = Vector3.zero;
            }
        }
    }

    private void Open()
    {
        if (hurt)
            return;
        hurt = true;
        GillaGoreAssets a = Assets;
        for (int s = 0; s < skins.Length; s++)
        {
            SkinnedMeshRenderer skin = skins[s];
            Material[] mats = skin.sharedMaterials;
            whole.Add((skin, skin.sharedMesh, mats));
            Material[] now = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                now[i] = mats[i];
                if (a == null) continue;
                if (a.wound == null) continue;
                if (mats[i] == null) continue;
                if (!wounded.TryGetValue(mats[i], out Material copy))
                {
                    Material key = mats[i];
                    if (fitted != null)
                        copy = MonsterCopy(mats[i]);
                    else
                        copy = WoundedCopy(mats[i]);
                    wounded[key] = copy;
                    skinMats.Add(copy);
                }
                now[i] = copy;
            }
            skin.sharedMaterials = now;
            Bounds bounds = skin.localBounds;
            skin.sharedMesh = Carrying(skin.sharedMesh);
            if (fitted != null)
                skin.localBounds = bounds;
        }
    }

    private Material MonsterCopy(Material from)
    {
        Material m = new Material(fitted.wound);
        m.name = from.name + "_Wounded";
        m.mainTexture = from.mainTexture;
        m.mainTextureScale = from.mainTextureScale;
        m.mainTextureOffset = from.mainTextureOffset;
        if (from.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", from.GetColor("_BaseColor"));
        if (from.HasProperty("_Smoothness"))
            m.SetFloat("_Smoothness", from.GetFloat("_Smoothness"));
        if (from.HasProperty("_Metallic"))
            m.SetFloat("_Metallic", from.GetFloat("_Metallic"));
        m.SetFloat("_SmoothFromAlpha", from.IsKeywordEnabled("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A") ? 1f : 0f);
        if (from.IsKeywordEnabled("_NORMALMAP") && from.HasProperty("_BumpMap") && from.GetTexture("_BumpMap") != null)
        {
            m.SetTexture("_BumpMap", from.GetTexture("_BumpMap"));
            m.SetFloat("_BumpScale", from.GetFloat("_BumpScale"));
            m.SetFloat("_UseBump", 1f);
        }
        if (from.IsKeywordEnabled("_EMISSION") && from.HasProperty("_EmissionColor"))
        {
            if (from.GetTexture("_EmissionMap") != null)
                m.SetTexture("_EmissionMap", from.GetTexture("_EmissionMap"));
            m.SetColor("_EmissionColor", from.GetColor("_EmissionColor"));
        }
        for (int i = 0; i < armOff.Length; i++)
        {
            if (armOff[i])
                m.SetFloat(i == 2 ? OffLId : OffRId, 1f);
        }
        return m;
    }

    private Material Unwounded(Material m)
    {
        foreach (var kv in wounded)
        {
            if (kv.Value == m)
                return kv.Key;
        }
        return m;
    }

    private static Material WoundedCopy(Material from)
    {
        Material m = new Material(Assets.wound);
        m.name = from.name + "_Wounded";
        m.mainTexture = from.mainTexture;
        if (from.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", from.GetColor("_BaseColor"));
        if (from.HasProperty("_Smoothness"))
            m.SetFloat("_Smoothness", from.GetFloat("_Smoothness"));
        m.SetFloat("_EdgeWidth", from.HasProperty("_EdgeWidth") ? from.GetFloat("_EdgeWidth") : 0.001f);
        if (from.HasProperty("_OutlineColor"))
            m.SetColor("_OutlineColor", from.GetColor("_OutlineColor"));
        if (from.HasProperty("_RimSide"))
            m.SetVector("_RimSide", from.GetVector("_RimSide"));
        if (from.HasProperty("_RimReach"))
            m.SetFloat("_RimReach", from.GetFloat("_RimReach"));
        return m;
    }

    private static Mesh Carrying(Mesh m)
    {
        if (carrying.TryGetValue(m, out Mesh made))
            return made;
        made = UnityEngine.Object.Instantiate<Mesh>(m);
        made.name = m.name;
        made.SetUVs(3, Vertices(m));
        made.SetUVs(2, Normals(m));
        carrying[m] = made;
        return made;
    }

    private void ShowInside(GoreSection section)
    {
        GillaGoreAssets a = Assets;
        if (a == null)
            return;
        if (a.pieces == null)
            return;
        if (showing[(int)section])
            return;
        showing[(int)section] = true;
        Component layerFrom = this;
        if (skins.Length != 0)
            layerFrom = skins[0];
        int layer = layerFrom.gameObject.layer;
        bool armGone = fitted != null ? armOff[(int)section] : false;
        GillaGoreAssets.Piece[] list = a.pieces;
        if (fitted != null)
            list = fitted.pieces;
        for (int i = 0; i < list.Length; i++)
        {
            GillaGoreAssets.Piece p = list[i];
            if (p.section != section) continue;
            if (p.mesh == null) continue;
            if (!bones.TryGetValue(p.bone, out var entry)) continue;
            if (armGone && p.bone.StartsWith("lowerarm")) continue;
            bool isBone = p.material == a.bone;
            GameObject go = new GameObject("GoreBone_" + p.mesh.name);
            go.layer = layer;
            if (isBone)
            {
                if (boneMat == null)
                {
                    Material mat = new Material(a.bone);
                    mat.name = a.bone.name + "_Here";
                    boneMat = mat;
                }
            }
            else
            {
                if (gutMat == null)
                {
                    Material mat = new Material(p.material);
                    mat.name = p.material.name + "_Here";
                    gutMat = mat;
                }
            }
            go.transform.SetParent(entry.bone, false);
            go.transform.SetLocalPositionAndRotation(new Vector3(entry.bind.m03, entry.bind.m13, entry.bind.m23), entry.bind.rotation);
            go.transform.localScale = entry.bind.lossyScale;
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = p.mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = isBone ? boneMat : gutMat;
            renderer.enabled = false;
            Broken b = new Broken();
            b.filter = filter;
            b.whole = p.mesh;
            b.section = section;
            b.gone = p.gone;
            b.thick = isBone ? 0.0045f : 0f;
            b.cut = armGone ? cuts[(int)section] : Vector4.zero;
            breakable.Add(b);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pieces.Add((go, section));
        }
    }

    private void BreakBones(int wound)
    {
        Vector4 w = wounds[wound];
        float axisW = axes[wound].w;
        int index = breakable.Count;
        if (index - 1 < 0)
            return;
        float r = w.w * 1.5f * Mathf.Max(axisW, 1.4f);
        do
        {
            index--;
            Broken b = breakable[index];
            if (b.filter == null)
            {
                breakable.RemoveAt(index);
                continue;
            }
            Bounds box = ShapeOf(b.whole).bounds;
            box.Expand(r + r);
            if (b.left != null)
            {
                Vector4 at = wounds[wound];
                if (!box.Contains(new Vector3(at.x, at.y, at.z)))
                    continue;
            }
            Break(b);
        } while (0 < index);
    }

    private void Break(Broken b)
    {
        BoneShape s = ShapeOf(b.whole);
        BoneOut o = BreakMath(s, wounds, axes, sides, count, b.gone, b.thick, b.cut);
        BreakPut(b, o);
    }
}
