// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GillaWounds : MonoBehaviour
{
    private void Find()
    {
        if (copy != null) return;
        if (fitted != null) return;
        if (corpse != null && skins != null) return;

        copy = GetComponent<Dismemberment>();
        if (copy != null)
        {
            copy.PartsReset += Clear;
            copy.PartTorn += PartTorn;
        }

        List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
        SkinnedMeshRenderer[] found = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < found.Length; i++)
        {
            SkinnedMeshRenderer skin = found[i];
            if (skin.name == "SonarGhost") continue;
            if (skin.name == "SuitTrim") continue;
            if (skin.sharedMesh == null) continue;
            if (skin.GetComponentInParent<CosmeticToy>() != null) continue;

            list.Add(skin);
            Matrix4x4[] bindposes = skin.sharedMesh.bindposes;
            Transform[] skinBones = skin.bones;
            for (int b = 0; b < skinBones.Length; b++)
            {
                if (b >= bindposes.Length) break;
                if (skinBones[b] != null)
                {
                    if (!bones.ContainsKey(skinBones[b].name))
                    {
                        bones[skinBones[b].name] = (skinBones[b], bindposes[b]);
                    }
                }
            }
        }
        skins = list.ToArray();
    }

    private void OnDestroy()
    {
        if (copy != null)
        {
            copy.PartsReset -= Clear;
            copy.PartTorn -= PartTorn;
        }
        soons.Clear();
        toBreak.Clear();
        breaking = null;
        breakingUpTo = serials;
        brokenUpTo = serials;
        busy.Remove(this);
        Array.Clear(drawnOn, 0, 6);
        count = 0;
        next = 0;
        foreach (var e in eyesOut)
        {
            if (e.eye != null)
            {
                e.eye.localScale = e.was;
            }
        }
        eyesOut.Clear();
        skinMats.Clear();
        if (boneMat != null) Destroy(boneMat);
        if (gutMat != null) Destroy(gutMat);
        if (wallMat != null) Destroy(wallMat);
        wallMat = null;
        gutMat = null;
        boneMat = null;
        foreach (Mesh m in wallMeshes)
        {
            if (m != null) Destroy(m);
        }
        wallMeshes.Clear();
        foreach (var p in pieces)
        {
            if (p.go != null) Destroy(p.go);
        }
        pieces.Clear();
        breakable.Clear();
        Array.Clear(showing, 0, showing.Length);
        Array.Clear(armOff, 0, armOff.Length);
        Array.Clear(cuts, 0, cuts.Length);
        foreach (var w in whole)
        {
            if (w.skin != null)
            {
                w.skin.sharedMesh = w.mesh;
                w.skin.sharedMaterials = w.mats;
            }
        }
        whole.Clear();
        foreach (Material mat in wounded.Values)
        {
            if (mat != null) Destroy(mat);
        }
        wounded.Clear();
        hurt = false;
        if (copy != null)
        {
            copy.SetCaps();
        }
    }

    private void PartTorn(ApePart part, GameObject piece)
    {
        Find();
        GoreSection section = part == ApePart.Head ? GoreSection.Head : (part == ApePart.ArmL ? GoreSection.ArmL : GoreSection.ArmR);
        Transform parent = PartRoot(part);
        int index = pieces.Count;
        while (index - 1 >= 0)
        {
            index--;
            GameObject go = pieces[index].go;
            if (go == null)
            {
                pieces.RemoveAt(index);
            }
            else if (pieces[index].section == section)
            {
                Off(go, piece);
                pieces.RemoveAt(index);
            }
            else if (parent != null)
            {
                if (go.transform.IsChildOf(parent))
                {
                    go.transform.SetParent(parent.parent, true);
                }
                GillaWoundBend bend;
                if (go.TryGetComponent<GillaWoundBend>(out bend))
                {
                    if (bend.other != null)
                    {
                        if (bend.other.IsChildOf(parent))
                        {
                            bend.other = null;
                            bend.Bend();
                        }
                    }
                }
            }
            if (index <= 0) break;
        }
        if (parent != null)
        {
            Transform[] all = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == "WoundBlood")
                {
                    Off(all[i].gameObject, piece);
                }
            }
        }
    }

    private static void Off(GameObject go, GameObject piece)
    {
        if (piece != null)
        {
            TornArm arm;
            if (piece.TryGetComponent<TornArm>(out arm))
            {
                arm.TakeGore(go.transform, go.transform.parent);
            }
            else
            {
                go.transform.SetParent(piece.transform, true);
            }
            TornPiece torn;
            if (piece.TryGetComponent<TornPiece>(out torn))
            {
                MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterials = torn.Own(renderers[i].sharedMaterials);
                }
            }
            return;
        }
        go.transform.SetParent(null, true);
        go.SetActive(false);
        Destroy(go);
    }

    private Transform PartRoot(ApePart part)
    {
        string name = part == ApePart.Head ? "head" : (part == ApePart.ArmL ? "upperarm_L" : "upperarm_R");
        (Transform bone, Matrix4x4 bind) found;
        if (!bones.TryGetValue(name, out found))
        {
            found.bone = null;
        }
        return found.bone;
    }

    public bool Gone(GoreSection s)
    {
        if (fitted != null)
        {
            return armOff[(int)s];
        }
        int p = 0;
        if (s != GoreSection.Head)
        {
            if (s == GoreSection.Torso) return false;
            p = s == GoreSection.ArmL ? 1 : 2;
        }
        return Off((ApePart)p);
    }

    private bool Off(ApePart p)
    {
        if (corpse != null)
        {
            return corpse.IsOff(GillaCorpse.Part(p));
        }
        if (copy == null) return false;
        return copy.IsTorn(p);
    }

    private bool TornSkin(SkinnedMeshRenderer skin)
    {
        if (copy != null)
        {
            Dismemberment.Part[] parts = copy.parts;
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].renderer == skin)
                    {
                        return parts[i].torn;
                    }
                }
            }
        }
        return false;
    }

    private void LateUpdate()
    {
        foreach (var e in eyesOut)
        {
            if (e.eye != null)
            {
                e.eye.localScale = Vector3.zero;
            }
        }
    }

    internal Vector3 World(string bone, Vector3 body)
    {
        var b = bones[bone];
        return b.bone.TransformPoint(b.bind.MultiplyPoint3x4(body));
    }

    internal Vector3 OnBody(string bone, Vector3 world)
    {
        var b = bones[bone];
        Matrix4x4 inv = b.bind.inverse;
        return inv.MultiplyPoint3x4(b.bone.InverseTransformPoint(world));
    }

    public Transform BoneAt(GoreSection section, Vector3 point, out Vector3 local)
    {
        Find();
        var b = BoneOf(section, point);
        local = b.Item2.MultiplyPoint3x4(point);
        return b.Item1;
    }

    private static float ToSegment(Vector3 at, Vector3 a, Vector3 b, out Vector3 nearest)
    {
        Vector3 ab = b - a;
        float len = ab.sqrMagnitude;
        float t = 0f;
        if (len > 1e-08f)
        {
            float f = Vector3.Dot(at - a, ab) / len;
            if (f >= 0f)
            {
                t = f;
                if (f > 1f) t = 1f;
            }
        }
        nearest = a + ab * t;
        Vector3 d = at - nearest;
        return (float)Math.Sqrt(d.z * d.z + d.x * d.x + d.y * d.y);
    }

    public bool Hit(Vector3 at, float reach, Vector3 speed, out GoreSection section, out Vector3 point)
    {
        point = Vector3.zero;
        string on;
        Vector3 surface;
        Vector3 normal;
        float best;
        if (Shape(at, reach, out section, out on, out surface, out normal, out best)
            && (Vector3.Dot(speed, surface - at) >= 0f || best <= 0f))
        {
            var b = bones[on];
            GoreSection s = section;
            Matrix4x4 inv = b.bind.inverse;
            Vector3 local = inv.MultiplyPoint3x4(b.bone.InverseTransformPoint(surface));
            point = NearestSkin(s, local);
            return true;
        }
        return false;
    }

    public bool Surface(Vector3 at, float reach, out GoreSection section, out Vector3 world, out Vector3 body, out Vector3 normal, out float inside)
    {
        body = Vector3.zero;
        inside = 0f;
        string on;
        float best;
        bool ok = Shape(at, reach, out section, out on, out world, out normal, out best);
        if (ok)
        {
            inside = -best;
            body = OnBody(on, world);
        }
        return ok;
    }

    public Vector3 WorldOn(GoreSection section, Vector3 point)
    {
        Vector3 local;
        Transform bone = BoneAt(section, point, out local);
        if (bone != null)
        {
            return bone.TransformPoint(local);
        }
        return point;
    }

    private bool Shape(Vector3 at, float reach, out GoreSection section, out string on, out Vector3 surface, out Vector3 normal, out float best)
    {
        Find();
        section = GoreSection.Torso;
        on = null;
        surface = at;
        normal = Vector3.up;
        best = reach;
        if (!bones.ContainsKey("head")) return false;
        if (!bones.ContainsKey("spine")) return false;
        if (!bones.ContainsKey("upperarm_L")) return false;
        if (!bones.ContainsKey("upperarm_R")) return false;

        float size = bones["spine"].bone.lossyScale.x;
        float nearestD = reach;
        GoreSection nearestSection = section;
        string nearestBone = null;
        Vector3 nearestWay = Vector3.up;
        Vector3 nearestAt = at;

        void Try(Vector3 a, Vector3 b, float radius, GoreSection s, string bone, Vector3 p, float scale,
            ref float nd, ref GoreSection ns, ref string nb, ref Vector3 nw, ref Vector3 na)
        {
            Vector3 near;
            float d = ToSegment(p, a, b, out near) - radius * scale; /* ? */
            if (d < nd)
            {
                Vector3 way = p - near;
                way = way.sqrMagnitude > 1e-12f ? way.normalized : Vector3.up;
                nd = d;
                ns = s;
                nb = bone;
                nw = way;
                na = near + way * (radius * scale);
            }
        }

        if (!Off(ApePart.Head))
        {
            Vector3 a = World("head", new Vector3(0f, -0.04f, 0.345f));
            Vector3 b = World("head", new Vector3(0f, -0.02f, 0.37f));
            Try(a, b, 0.082f, GoreSection.Head, "head", at, size, ref nearestD, ref nearestSection, ref nearestBone, ref nearestWay, ref nearestAt); /* ? */
        }
        Vector3 s1 = World("spine", new Vector3(0f, 0.004f, 0.075f));
        Vector3 s2 = World("spine", new Vector3(0f, 0.004f, 0.205f));
        Try(s1, s2, 0.078f, GoreSection.Torso, "spine", at, size, ref nearestD, ref nearestSection, ref nearestBone, ref nearestWay, ref nearestAt); /* ? */

        (ApePart part, GoreSection sec, string suffix)[] arms = new (ApePart, GoreSection, string)[2];
        arms[0] = (ApePart.ArmL, GoreSection.ArmL, "_L");
        arms[1] = (ApePart.ArmR, GoreSection.ArmR, "_R");
        for (int i = 0; i < arms.Length; i++)
        {
            GoreSection sec = arms[i].sec;
            string suffix = arms[i].suffix;
            if (Off(arms[i].part)) continue;
            if (!bones.ContainsKey("lowerarm" + suffix)) continue;
            if (!bones.ContainsKey("hand" + suffix)) continue;
            Transform upper = bones["upperarm" + suffix].bone;
            Transform lower = bones["lowerarm" + suffix].bone;
            Transform hand = bones["hand" + suffix].bone;
            string upperName = "upperarm" + suffix;
            Try(upper.position, lower.position, 0.04f, sec, upperName, at, size, ref nearestD, ref nearestSection, ref nearestBone, ref nearestWay, ref nearestAt); /* ? */
            string lowerName = "lowerarm" + suffix;
            Vector3 lp = lower.position;
            Vector3 lp2 = lower.position;
            Vector3 hp = hand.position;
            Try(lp, lp2 + (hp - lp2) * 0.72f, 0.034f, sec, lowerName, at, size, ref nearestD, ref nearestSection, ref nearestBone, ref nearestWay, ref nearestAt); /* ? */
        }

        section = nearestSection;
        on = nearestBone;
        surface = nearestAt;
        normal = nearestWay;
        best = nearestD;
        return on != null;
    }

    private static string PartName(GoreSection section)
    {
        switch (section)
        {
            case GoreSection.Head: return "Head"; /* ? */
            case GoreSection.Torso: return "Torso"; /* ? */
            case GoreSection.ArmL: return "ArmL"; /* ? */
            default: return "ArmR";
        }
    }

    internal Vector3 NearestSkin(GoreSection section, Vector3 point)
    {
        string name = PartName(section);
        Vector3 result = point;
        if (fitted != null)
        {
            Vector3[] verts = Vertices(fittedMesh);
            byte[] sectionOf = fitted.sectionOf;
            int[] points = SkinPoints(fittedMesh, null);
            float bestD = float.MaxValue;
            for (int i = 0; i < points.Length; i++)
            {
                int v = points[i];
                if (sectionOf[v] == (byte)section)
                {
                    Vector3 p = verts[v];
                    float d = (p - point).sqrMagnitude;
                    if (d < bestD)
                    {
                        bestD = d;
                        result = p;
                    }
                }
            }
            return result;
        }
        float nearest = float.MaxValue;
        for (int s = 0; s < skins.Length; s++)
        {
            SkinnedMeshRenderer skin = skins[s];
            if (!skin.name.EndsWith(name)) continue;
            Vector3[] verts = Vertices(skin.sharedMesh);
            int[] points = SkinPoints(skin.sharedMesh, skin.sharedMaterials);
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = verts[points[i]];
                float d = (p - point).sqrMagnitude;
                if (d < nearest)
                {
                    result = p;
                    nearest = d;
                }
            }
        }
        return result;
    }

    private static int[] SkinPoints(Mesh m, Material[] mats)
    {
        int[] found;
        if (skinPoints.TryGetValue(m, out found))
        {
            return found;
        }
        HashSet<int> set = new HashSet<int>();
        for (int i = 0; i < m.subMeshCount; i++)
        {
            if (i >= mats.Length) break;
            string matName = "";
            if (mats[i] != null)
            {
                matName = mats[i].name;
            }
            if (!matName.Contains("Eye") && !matName.Contains("Flesh") && !matName.Contains("Bone"))
            {
                int[] tris = m.GetTriangles(i);
                for (int t = 0; t < tris.Length; t++)
                {
                    set.Add(tris[t]);
                }
            }
        }
        found = new int[set.Count];
        set.CopyTo(found);
        skinPoints[m] = found;
        return found;
    }
}
