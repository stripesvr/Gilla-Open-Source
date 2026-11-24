using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class CosmeticWearer : MonoBehaviour
{
    static CosmeticWearer()
    {
        BaseMap = Shader.PropertyToID("_BaseMap");
        OverEyes = new string[] { "Glasses", "Goggles", "Shades", "Visor", "Mask" };
    }

    private static T SkinField<T>(object o, string name) where T : class /* ? */
    {
        if (o == null)
        {
            return null;
        }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = o.GetType();
        FieldInfo f = type.GetField(name, flags);
        if (f != null)
        {
            return f.GetValue(o) as T;
        }
        PropertyInfo p = type.GetProperty(name, flags);
        if (p != null)
        {
            return p.GetValue(o) as T;
        }
        return null;
    }

    private void WearSuit(CosmeticSkin info)
    {
        Mesh[] infoSuits = SkinField<Mesh[]>(info, "suits"); /* ? */
        string[] infoSuitUnder = SkinField<string[]>(info, "suitUnder"); /* ? */
        if (infoSuits == null /* ? */ || infoSuitUnder == null /* ? */)
        {
            return;
        }
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < infoSuits.Length && i < infoSuitUnder.Length; i++)
        {
            int index = i;
            SkinnedMeshRenderer under = Array.Find(renderers, p => WearSuit_b__1(info, index, p));
            if (under == null)
            {
                continue;
            }
            if (infoSuits[i] == null)
            {
                continue;
            }
            string name = "Suit_" + under.name;
            Mesh mesh = infoSuits[i];
            Material[] mats = new Material[1];
            mats[0] = info.body;
            Over(under, name, mesh, mats);
        }

        SkinnedMeshRenderer skinRenderer = Array.Find(renderers, p => WearSuit_b__64_0(p));
        if (skinOnly)
        {
            return;
        }
        Mesh infoTrimMesh = SkinField<Mesh>(info, "trimMesh"); /* ? */
        if (!(infoTrimMesh /* ? */ != null))
        {
            return;
        }
        if (!(skinRenderer != null))
        {
            return;
        }
        Mesh trimMesh = infoTrimMesh;
        Material infoTrim = SkinField<Material>(info, "trim"); /* ? */
        Material[] materials = new Material[2];
        materials[0] = info.body;
        materials[1] = infoTrim /* ? */ != null ? infoTrim : info.body;
        Over(skinRenderer, "SuitTrim", trimMesh, materials);
    }

    private void Over(SkinnedMeshRenderer under, string name, Mesh mesh, Material[] materials)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(under.transform.parent, false);
        go.transform.localPosition = under.transform.localPosition;
        go.transform.localRotation = under.transform.localRotation;
        go.transform.localScale = under.transform.localScale;
        go.layer = under.gameObject.layer;
        SkinnedMeshRenderer suit = go.AddComponent<SkinnedMeshRenderer>();
        suit.sharedMesh = mesh;
        suit.bones = under.bones;
        suit.rootBone = under.rootBone;
        suit.localBounds = under.localBounds;
        suit.sharedMaterials = materials;
        suit.shadowCastingMode = under.shadowCastingMode;
        suit.updateWhenOffscreen = under.updateWhenOffscreen;
        suit.quality = under.quality;
        suit.skinnedMotionVectors = under.skinnedMotionVectors;
        suit.enabled = under.enabled;
        suits.Add((suit, under));
        worn[6].Add(go);
    }

    private Material Unwounded(Material m)
    {
        GillaWounds wounds = null;
        if (body != null)
        {
            if (body.TryGetComponent<GillaWounds>(out wounds))
            {
                MethodInfo method = typeof(GillaWounds).GetMethod("Unwounded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Material) }, null); /* ? */
                if (method != null)
                {
                    m = (Material)method.Invoke(wounds, new object[] { m }); /* ? */
                }
            }
        }
        return m;
    }

    private void KeepSkin()
    {
        foreach ((Renderer r, int slot) in furSlots)
        {
            if (r == null)
            {
                continue;
            }
            if (!look.Covers(r))
            {
                continue;
            }
            Material[] mats = r.sharedMaterials;
            if (slot >= mats.Length)
            {
                continue;
            }
            if (!(mats[slot] != look.body))
            {
                continue;
            }
            if (!(look.body != null))
            {
                continue;
            }
            if (!beforeSkin.ContainsKey(r))
            {
                return; /* ? */
            }
            if (Unwounded(mats[slot]) != look.body)
            {
                Material[] before = beforeSkin[r];
                before[slot] = Unwounded(mats[slot]);
            }
            mats[slot] = look.body;
            r.sharedMaterials = mats;
            shownFur = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void Tint()
    {
        if (colour == null)
        {
            colour = FindColour();
        }
        Color fur;
        if (colour != null)
        {
            fur = colour.furColor;
        }
        else
        {
            fur = new Color(0.3f, 0.585f, 0.155f, 1f);
        }
        if (fur == shownFur)
        {
            return;
        }
        shownFur = fur;
        if (furBlock == null)
        {
            furBlock = new MaterialPropertyBlock();
        }
        if (look != null)
        {
            if (look.furTint)
            {
                foreach ((Renderer r, int slot) in furSlots)
                {
                    if (r == null)
                    {
                        continue;
                    }
                    r.GetPropertyBlock(furBlock, slot);
                    furBlock.SetColor(CosmeticSkin.FurColor, fur);
                    r.SetPropertyBlock(furBlock, slot);
                }
            }
        }
        for (int i = 0; i < worn.Length; i++)
        {
            if (i == 6)
            {
                continue;
            }
            foreach (GameObject go in worn[i])
            {
                if (go == null)
                {
                    continue;
                }
                Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    Renderer r = renderers[j];
                    if (r.sharedMaterial == null)
                    {
                        continue;
                    }
                    if (r.sharedMaterial.HasProperty(CosmeticSkin.FurColor))
                    {
                        r.GetPropertyBlock(furBlock);
                        furBlock.SetColor(CosmeticSkin.FurColor, fur);
                        r.SetPropertyBlock(furBlock);
                    }
                }
            }
        }
    }

    private ApeColor FindColour()
    {
        ApeColor found = GetComponentInParent<ApeColor>();
        if (found == null)
        {
            found = GetComponentInChildren<ApeColor>(true);
        }
        if (found != null)
        {
            return found;
        }
        ApeColor[] all = UnityEngine.Object.FindObjectsByType<ApeColor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            ApeColor c = all[i];
            if (c.body != null)
            {
                if (c.body.transform.IsChildOf(transform))
                {
                    return c;
                }
            }
            Renderer[] parts = c.parts;
            if (parts != null && parts.Length > 0)
            {
                for (int j = 0; j < parts.Length; j++)
                {
                    Renderer p = parts[j];
                    if (p != null)
                    {
                        if (p.transform.IsChildOf(transform))
                        {
                            return c;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void Eyes()
    {
        foreach ((Transform bone, Vector3 rest) in eyeBones)
        {
            if (bone != null)
            {
                bone.localScale = rest * eyeSize;
            }
        }
        if (Face != null)
        {
            Face.EyesOut = eyeOut;
        }
        if (eyes == null)
        {
            return;
        }
        if (eyeSlot < 0)
        {
            return;
        }
        Material[] mats = eyes.sharedMaterials;
        Material mat;
        if (look != null && look.eyes != null)
        {
            mat = look.eyes;
        }
        else
        {
            mat = eyeMat;
        }
        if (mat != null)
        {
            if (eyeBefore == null)
            {
                eyeBefore = mats[eyeSlot];
            }
            mats[eyeSlot] = mat;
            eyes.sharedMaterials = mats;
        }
        else if (eyeBefore != null)
        {
            mats[eyeSlot] = eyeBefore;
            eyes.sharedMaterials = mats;
            eyeBefore = null;
        }
        if (eyeBlock == null)
        {
            eyeBlock = new MaterialPropertyBlock();
        }
        eyes.GetPropertyBlock(eyeBlock, eyeSlot);
        if (eyeTex != null && mat == null)
        {
            eyeBlock.SetTexture(BaseMap, eyeTex);
        }
        else
        {
            eyeBlock.Clear();
        }
        eyes.SetPropertyBlock(eyeBlock, eyeSlot);
    }

    private bool TornFor(string fx)
    {
        if (body != null)
        {
            if (fx == "FX_Head")
            {
                if (body.IsTorn(ApePart.Head))
                {
                    return true;
                }
            }
            if (fx == "FX_Hand_L")
            {
                if (body.IsTorn(ApePart.ArmL))
                {
                    return true;
                }
            }
            if (fx == "FX_Hand_R")
            {
                return body.IsTorn(ApePart.ArmR);
            }
        }
        return false;
    }

    private void PoseToy(float dt)
    {
        Transform target;
        bool hide;
        if (toyState == 1)
        {
            target = toyLeft;
            hide = toyLeft == null;
        }
        else if (toyState == 2)
        {
            target = toyRight;
            hide = toyRight == null;
        }
        else if (toyState == 3)
        {
            target = null;
            hide = true;
        }
        else
        {
            target = null;
            hide = false;
        }

        if (hide == toy.gameObject.activeSelf)
        {
            toy.gameObject.SetActive(!hide);
        }
        if (hide)
        {
            toyOnBody = false;
            toyWasAway = true;
            return;
        }

        Transform t = toy.transform;
        if (target == null)
        {
            if (toy.kind == ToyKind.Monkey && handL != null)
            {
                if (t.parent != transform)
                {
                    t.SetParent(transform, true);
                }
                CosmeticToy monkey = toy;
                Vector3 at;
                if (knucklesL != null)
                {
                    at = Vector3.Lerp(handL.position, knucklesL.position, 0.5f);
                }
                else
                {
                    at = handL.position;
                }
                monkey.Sit(at, -transform.forward);
                toyOnBody = false;
                return;
            }
        }

        if (target != null)
        {
            if (t.parent != transform)
            {
                t.SetParent(transform, true);
            }
            toy.PoseIn(target, toyState == 1 ? 1f : -1f);
            toyOnBody = false;
            return;
        }

        if (!toyOnBody)
        {
            toyOnBody = true;
            t.SetParent(spineBone, true);
            toyFromPos = t.localPosition;
            toyFromRot = t.localRotation;
            toyHome = 0f;
            if (toyWasAway)
            {
                toyHome = 1f;
                toyPop = 0f;
                t.localPosition = Vector3.zero;
                t.localRotation = toyRest;
            }
            toyWasAway = false;
        }

        if (toyPop < 1f)
        {
            toyPop = Mathf.Min(toyPop + dt / 0.7f, 1f);
            float p = toyPop;
            float e = 0f;
            if (p > 0f)
            {
                e = 1f;
                if (p < 1f)
                {
                    e = Mathf.Pow(2f, p * -10f) * Mathf.Sin((p - 0.075f) * 6.2831855f / 0.3f) + 1f;
                }
            }
            t.localScale = Vector3.one * e;
        }

        if (toyHome >= 1f)
        {
            return;
        }
        toyHome = Mathf.Min(toyHome + dt / 0.6f, 1f);
        float h = toyHome;
        float k = 0f;
        if (h > 0f)
        {
            k = 1f;
            if (h < 1f)
            {
                k = Mathf.Pow(2f, h * -10f) * Mathf.Sin((h - 0.075f) * 6.2831855f / 0.3f) + 1f;
            }
        }
        t.localPosition = Vector3.LerpUnclamped(toyFromPos, Vector3.zero, k);
        t.localRotation = Quaternion.SlerpUnclamped(toyFromRot, toyRest, k);
    }

    private void OnTorn(int part, GameObject piece)
    {
        foreach (GameObject go in worn[6])
        {
            if (go != null)
            {
                if (TornFor(go.name))
                {
                    go.SetActive(false);
                }
            }
        }
        if (part == 0)
        {
            CosmeticSlot[] slots = new CosmeticSlot[] { CosmeticSlot.Head, CosmeticSlot.Face };
            for (int s = 0; s < slots.Length; s++)
            {
                List<GameObject> list = worn[(int)slots[s]];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] == null)
                    {
                        continue;
                    }
                    if (piece != null)
                    {
                        list[i].transform.SetParent(piece.transform, true);
                        Layer(list[i], piece.layer);
                    }
                    else
                    {
                        list[i].SetActive(false);
                    }
                }
                if (piece != null)
                {
                    list.Clear();
                }
            }
        }
    }

    public void PutBack()
    {
        for (int slot = 0; slot < worn.Length; slot++)
        {
            List<GameObject> list = worn[slot];
            if (list.Count < 1)
            {
                if ((slot & ~4) != 2 && ids[slot] != 0)
                {
                    Put((CosmeticSlot)slot, ids[slot], true);
                }
            }
            else
            {
                foreach (GameObject go in list)
                {
                    if (go != null)
                    {
                        go.SetActive(true);
                    }
                }
            }
        }
    }

    private static void Layer(GameObject go, int layer)
    {
        Transform[] transforms = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            transforms[i].gameObject.layer = layer;
        }
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].allowOcclusionWhenDynamic = false;
        }
    }

    private static Quaternion OnBone(SkinnedMeshRenderer skin, Transform bone)
    {
        int index = Array.IndexOf(skin.bones, bone);
        if (index >= 0)
        {
            if (!(skin.sharedMesh == null))
            {
                Matrix4x4 m = skin.sharedMesh.bindposes[index].inverse;
                return Quaternion.Inverse(m.rotation) * Quaternion.Inverse(skin.transform.localRotation);
            }
        }
        return Quaternion.identity;
    }

    private static Quaternion OnHead(SkinnedMeshRenderer skin, Transform head)
    {
        return OnBone(skin, head);
    }
}
