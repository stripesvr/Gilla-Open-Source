// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public partial class GillaCorpse : MonoBehaviour
{
    private void CopyBones_g__Copy_141_0(Transform from, Transform to, int sign, int seed, List<(GillaWoundBend made, Transform other, Matrix4x4 bind)> bends)
    {
        foreach (Transform child in from)
        {
            if (child.name.StartsWith("GoreBone_"))
            {
                if (child.name != "GoreBone_Wall")
                {
                    if (!child.name.Contains("arm_"))
                    {
                        continue;
                    }
                }
                MeshFilter filter;
                MeshRenderer renderer;
                if (child.TryGetComponent<MeshFilter>(out filter) && child.TryGetComponent<MeshRenderer>(out renderer))
                {
                    Mesh whole = filter.sharedMesh;
                    Mesh trimmed = GillaRip.Trim(whole, sign, seed);
                    if (!(trimmed == null))
                    {
                        GameObject go = new GameObject(child.name);
                        go.layer = 2;
                        go.transform.SetParent(to, false);
                        go.transform.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
                        go.transform.localScale = child.localScale;
                        MeshFilter newFilter = go.AddComponent<MeshFilter>();
                        newFilter.sharedMesh = trimmed;
                        MeshRenderer newRenderer = go.AddComponent<MeshRenderer>();
                        newRenderer.sharedMaterials = Copies(renderer.sharedMaterials);
                        newRenderer.shadowCastingMode = renderer.shadowCastingMode;
                        GillaWoundBend bend;
                        if (child.TryGetComponent<GillaWoundBend>(out bend))
                        {
                            GillaWoundBend made = go.AddComponent<GillaWoundBend>();
                            bends.Add((made, bend.other /* ? */, bend.bind /* ? */));
                        }
                    }
                }
                continue;
            }
            if (child.name.StartsWith("Blood"))
            {
                continue;
            }
            if (child.name.StartsWith("Worn_"))
            {
                continue;
            }
            GameObject copy = new GameObject(child.name);
            copy.layer = 2;
            Transform t = copy.transform;
            t.SetParent(to, false);
            t.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
            t.localScale = child.localScale;
            skeleton[child] = t;
            bones[child.name] = t;
            CopyBones_g__Copy_141_0(child, t, sign, seed, bends);
        }
    }
}
