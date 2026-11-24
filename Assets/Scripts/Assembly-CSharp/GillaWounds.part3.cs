// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class GillaWounds : MonoBehaviour
{
    private static int[] SkinTriangles(Mesh m, Material[] mats)
    {
        int[] cached;
        if (skinTriangles.TryGetValue(m, out cached))
        {
            return cached;
        }
        List<int> list = new List<int>();
        for (int i = 0; i < m.subMeshCount; i++)
        {
            if (i >= mats.Length) break;
            string name = "";
            if (mats[i] != null)
            {
                name = mats[i].name;
            }
            if (!name.Contains("Eye") && !name.Contains("Flesh") && !name.Contains("Bone"))
            {
                list.AddRange(m.GetTriangles(i));
            }
        }
        cached = list.ToArray();
        skinTriangles[m] = cached;
        return cached;
    }

    private static Vector3[] Vertices(Mesh m)
    {
        Vector3[] v;
        if (vertices.TryGetValue(m, out v))
        {
            return v;
        }
        v = m.vertices;
        vertices[m] = v;
        return v;
    }

    private static Vector3[] Normals(Mesh m)
    {
        Vector3[] v;
        if (normals.TryGetValue(m, out v))
        {
            return v;
        }
        v = m.normals;
        normals[m] = v;
        return v;
    }

    private static float RadiusOf(GoreSection s)
    {
        if (s == GoreSection.Head) return 0.032f;
        if (s == GoreSection.Torso) return 0.035f;
        return 0.024f;
    }

    private static float LengthOf(GoreSection s)
    {
        if (s == GoreSection.Head) return 2f;
        if (s == GoreSection.Torso) return 2.2f;
        return 1.6f;
    }

    public void Add(GoreSection section, Vector3 point, float radius)
    {
        using (AddMark.Auto())
        {
            int wound = Land(section, point, ref radius);
            if (wound < 0)
            {
                return;
            }
            foreach (Soon s in soons)
            {
                if (s.at == wound)
                {
                    s.gone = true;
                }
            }
            using (BonesMark.Auto())
            {
                ShowInside(section);
                BreakBones(wound);
            }
            if (radius > 0.001f)
            {
                using (CraterMark.Auto())
                {
                    Wall(wound, section);
                }
            }
            Draw(wound, wounds[wound], axes[wound], sides[wound]);
            Uniforms();
            KnockOutEyes(section, wound);
        }
    }

    private int Land(GoreSection section, Vector3 point, ref float radius)
    {
        Find();
        if ((section == GoreSection.Head && Headless) || Gone(section))
        {
            return -1;
        }
        Open();
        int rx = (int)Math.Round((double)(point.x * 9000f));
        int ry = (int)Math.Round((double)(point.y * 9000f));
        int rz = (int)Math.Round((double)(point.z * 9000f));
        System.Random rng = new System.Random(rx * 397 + ry * 619 + rz * 1021);
        if (radius <= 0f)
        {
            float baseRadius = RadiusOf(section);
            radius = baseRadius * (Mathf.Clamp01((float)rng.NextDouble()) * 0.42000008f + 0.78f);
        }
        float length = LengthOf(section) * (Mathf.Clamp01((float)rng.NextDouble()) * 0.34999996f + 0.8f);
        Vector3 inward = Inward(section, point);
        if (radius > 0.001f && fitted != null)
        {
            FitToLimb(section, point, ref radius, ref length);
        }
        else if (radius > 0.001f && section > GoreSection.Torso)
        {
            FitToBody(point, inward, ref radius, ref length);
        }
        Vector3 at = point + inward * (radius * length * 0.12f);
        for (int i = 0; i < count; i++)
        {
            Vector4 w = wounds[i];
            if (Vector3.Distance(new Vector3(w.x, w.y, w.z), at) < w.w * 0.7f)
            {
                wounds[i].w = Mathf.Min(w.w * 1.25f, radius * 1.8f);
                return i;
            }
        }
        int slot;
        if (fitted != null)
        {
            slot = FreeSlot();
            if (slot >= 0)
            {
                landedOn[slot] = section;
                stumps[slot] = false;
            }
            else
            {
                int nearest = 0;
                for (int i = 1; i < count; i++)
                {
                    Vector3 a = new Vector3(wounds[nearest].x, wounds[nearest].y, wounds[nearest].z) - at;
                    Vector3 b = new Vector3(wounds[i].x, wounds[i].y, wounds[i].z) - at;
                    if (a.sqrMagnitude > b.sqrMagnitude)
                    {
                        nearest = i;
                    }
                }
                wounds[nearest].w = Mathf.Min(wounds[nearest].w * 1.2f, radius + radius);
                return nearest;
            }
        }
        else
        {
            if (count < 6)
            {
                slot = count;
                count = count + 1;
            }
            else
            {
                slot = next;
            }
            next = (next + 1) % 6;
        }
        wounds[slot] = new Vector4(at.x, at.y, at.z, radius);
        axes[slot] = new Vector4(inward.x, inward.y, inward.z, length);
        float d1 = (float)rng.NextDouble();
        float d2 = (float)rng.NextDouble();
        float d3 = (float)rng.NextDouble();
        Vector3 side = Vector3.Cross(inward, new Vector3(d1 - 0.5f, d2 - 0.5f, d3 - 0.5f)).normalized;
        float turn = (float)rng.NextDouble();
        sides[slot] = new Vector4(side.x, side.y, side.z, turn * 3.1415927f + turn * 3.1415927f);
        return slot;
    }

    private void Draw(int at, Vector4 wound, Vector4 axis, Vector4 side)
    {
        drawnOn[at] = true;
        drawn[at] = wound;
        drawnAxes[at] = axis;
        drawnSides[at] = side;
    }

    private void Uniforms()
    {
        foreach (Material mat in skinMats)
        {
            for (int i = 0; i < 6; i++)
            {
                bool on = drawnOn[i];
                mat.SetVector(WoundIds[i], on ? drawn[i] : Vector4.zero);
                mat.SetVector(AxisIds[i], on ? drawnAxes[i] : new Vector4(0f, 0f, 1f, 1f));
                mat.SetVector(SideIds[i], on ? drawnSides[i] : new Vector4(1f, 0f, 0f, 0f));
                for (int j = 0; j < 3; j++)
                {
                    mat.SetVector(SpotIds[i * 3 + j], on ? Spot(i, j) : Vector4.zero);
                }
            }
        }
    }

    public void AddSoon(GoreSection section, Vector3 point, bool burst)
    {
        if (KillCam.Local == null)
        {
            Add(section, point, -1f);
            if (!burst)
            {
                return;
            }
            Burst(section, point);
            return;
        }
        if (!AddedEnough)
        {
            if (waiting.Count < 1 || adding)
            {
                AddNow(section, point, burst);
                return;
            }
        }
        waiting.Enqueue((this, section, point, burst));
        if (busy.Contains(this))
        {
            return;
        }
        busy.Add(this);
    }

    private void AddNow(GoreSection section, Vector3 point, bool burst)
    {
        if (addFrame == Time.frameCount)
        {
            addFor.Start();
        }
        else
        {
            addFrame = Time.frameCount;
            addFor.Restart();
        }
        AddTimed(section, point, burst);
        addFor.Stop();
    }

    private void AddTimed(GoreSection section, Vector3 point, bool burst)
    {
        using (AddMark.Auto())
        {
            float radius = -1f;
            int at = Land(section, point, ref radius);
            if (at < 0)
            {
                return;
            }
            foreach (Soon s in soons)
            {
                if (s.at == at)
                {
                    s.gone = true;
                }
            }
            Soon soon = new Soon();
            soon.at = at;
            soon.section = section;
            soon.burst = burst;
            soon.point = point;
            OnItsWay(soon, radius);
        }
    }

    private void OnItsWay(Soon s, float radius)
    {
        int at = s.at;
        serials = serials + 1;
        s.serial = serials;
        s.wound = wounds[at];
        s.axis = axes[at];
        s.side = sides[at];
        ShowInside(s.section);
        Mark(at);
        if (radius > 0.001f)
        {
            CraterIn c = Prep(at, s.section);
            s.crater = c;
            if (c != null)
            {
                s.made = Task.Run(() => OnItsWay_b__0(c));
            }
        }
        soons.Add(s);
        if (busy.Contains(this))
        {
            return;
        }
        busy.Add(this);
    }

    private int FreeSlot()
    {
        int n = count;
        if (n > 0)
        {
            for (int i = 0; i < n; i++)
            {
                if (wounds[i].w <= 0f)
                {
                    return i;
                }
            }
            if (n > 5)
            {
                return -1;
            }
        }
        count = n + 1;
        return n;
    }

    private void FitToLimb(GoreSection section, Vector3 point, ref float radius, ref float length)
    {
        MonsterGore.Limb limb = LimbNear(section, point);
        if (limb != null)
        {
            radius = Mathf.Max(Mathf.Min(radius, limb.radius * 0.75f), 0.006f);
            float most = ((limb.radius + limb.radius) * 0.65f) / (radius * 1.12f);
            length = Mathf.Max(Mathf.Min(length, most), 0.6f);
        }
    }

    private MonsterGore.Limb LimbNear(GoreSection section, Vector3 point)
    {
        MonsterGore.Limb[] limbs = fitted.limbs;
        MonsterGore.Limb best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < limbs.Length; i++)
        {
            MonsterGore.Limb limb = limbs[i];
            if (limb.section == section)
            {
                Vector3 near;
                float d = ToSegment(point, limb.from, limb.to, out near);
                if (d < bestD)
                {
                    best = limb;
                    bestD = d;
                }
            }
        }
        return best;
    }

    public void ArmTorn(GoreSection section, Vector3 cut, Vector3 way, GameObject piece)
    {
        Find();
        Open();
        armOff[(int)section] = true;
        cuts[(int)section] = new Vector4(way.x, way.y, way.z, Vector3.Dot(cut, way));
        string suffix = section == GoreSection.ArmL ? "_L" : "_R";
        (Transform bone, Matrix4x4 bind) upper;
        bones.TryGetValue("upperarm" + suffix, out upper);
        for (int i = 0; i < count; i++)
        {
            GameObject wall = null;
            Vector4 w = wounds[i];
            if (w.w > 0f && landedOn[i] == section)
            {
                Vector3 at = new Vector3(w.x, w.y, w.z);
                if (-(w.w * axes[i].w) < Vector3.Dot(at - cut, way))
                {
                    wounds[i].w = 0f;
                    drawnOn[i] = false;
                    wall = walls[i];
                    walls[i] = null;
                    if (!(wall == null))
                    {
                        GillaWoundBend bend;
                        if (wall.TryGetComponent<GillaWoundBend>(out bend))
                        {
                            if (bend.other != null)
                            {
                                bend.other = null;
                                bend.Bend();
                            }
                        }
                        pieces.RemoveAll(p => p.go == wall);
                        Off(wall, piece);
                    }
                }
            }
        }
        int index = breakable.Count;
        while (index - 1 >= 0)
        {
            index = index - 1;
            Broken b = breakable[index];
            if (!(b.filter == null) && b.section == section)
            {
                bool handled = false;
                if (upper.bone != null && b.filter.transform.parent == upper.bone)
                {
                    b.cut = new Vector4(way.x, way.y, way.z, Vector3.Dot(cut, way));
                    if (KillCam.Local == null)
                    {
                        if (b.left != null)
                        {
                            Break(b);
                        }
                    }
                    else if (b.left != null)
                    {
                        toBreak.Add(b);
                    }
                    handled = true;
                }
                if (!handled)
                {
                    GameObject go = b.filter.gameObject;
                    pieces.RemoveAll(p => p.go == go);
                    breakable.RemoveAt(index);
                    Off(go, piece);
                }
            }
            if (index < 1) break;
        }
        (Transform bone, Matrix4x4 bind) lower;
        if (bones.TryGetValue("lowerarm" + suffix, out lower))
        {
            if (lower.bone != null)
            {
                Transform[] all = lower.bone.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t.name == "WoundBlood")
                    {
                        Off(t.gameObject, piece);
                    }
                }
            }
        }
        if (toBreak.Count > 0)
        {
            if (!busy.Contains(this))
            {
                busy.Add(this);
            }
        }
        Uniforms();
        foreach (Material mat in skinMats)
        {
            mat.SetFloat(section == GoreSection.ArmL ? OffLId : OffRId, 1f);
        }
    }

    public void Stump(GoreSection section, Vector3 cut, Vector3 inward, float radius)
    {
        Find();
        Open();
        int wound = FreeSlot();
        if (wound < 0)
        {
            wound = next;
            next = (wound + 1) % 6;
        }
        int rx = (int)Math.Round((double)(cut.x * 9000f));
        int ry = (int)Math.Round((double)(cut.y * 9000f));
        int rz = (int)Math.Round((double)(cut.z * 9000f));
        System.Random rng = new System.Random(rx * 397 + ry * 619 + rz * 1021);
        landedOn[wound] = section;
        stumps[wound] = true;
        Vector3 at = cut + inward * (radius * 1.25f * 0.12f);
        wounds[wound] = new Vector4(at.x, at.y, at.z, radius);
        axes[wound] = new Vector4(inward.x, inward.y, inward.z, 1.25f);
        float d1 = (float)rng.NextDouble();
        float d2 = (float)rng.NextDouble();
        float d3 = (float)rng.NextDouble();
        Vector3 side = Vector3.Cross(inward, new Vector3(d1 - 0.5f, d2 - 0.5f, d3 - 0.5f)).normalized;
        float turn = (float)rng.NextDouble();
        sides[wound] = new Vector4(side.x, side.y, side.z, turn * 3.1415927f + turn * 3.1415927f);
        foreach (Soon s in soons)
        {
            if (s.at == wound)
            {
                s.gone = true;
            }
        }
        if (KillCam.Local == null)
        {
            ShowInside(section);
            BreakBones(wound);
            Wall(wound, section);
            Draw(wound, wounds[wound], axes[wound], sides[wound]);
            Uniforms();
            return;
        }
        Soon soon = new Soon();
        soon.at = wound;
        soon.section = section;
        soon.point = cut;
        soon.stump = true;
        OnItsWay(soon, radius);
    }

    public Material[] PieceLook(int[] slots, Material flesh)
    {
        Find();
        Open();
        Material[] shared = skins[0].sharedMaterials;
        Material[] result = new Material[slots.Length + 1];
        for (int i = 0; i < slots.Length; i++)
        {
            int slot = slots[i];
            result[i] = slot < shared.Length ? shared[slot] : flesh;
        }
        result[slots.Length] = flesh;
        return result;
    }

    public static bool Coming
    {
        get
        {
            if (busy.Count < 1)
            {
                return waiting.Count > 0;
            }
            return true;
        }
    }

    public static bool Pending
    {
        get
        {
            if (busy.Count > 0)
            {
                return true;
            }
            if (waiting.Count > 0)
            {
                return true;
            }
            return after.Count > 0;
        }
    }

    private static bool Waits(GillaWounds w)
    {
        foreach (var item in waiting)
        {
            if (item.on == w)
            {
                return true;
            }
        }
        return false;
    }

    private static void AddWaiting(bool budget, GillaWounds only)
    {
        adding = true;
        int n = waiting.Count;
        if (n > 0)
        {
            do
            {
                if (budget && AddedEnough) break;
                var item = waiting.Dequeue();
                if (!(item.on == null))
                {
                    if (only != null && item.on != only)
                    {
                        waiting.Enqueue(item);
                    }
                    else
                    {
                        item.on.AddNow(item.section, item.point, item.burst);
                    }
                }
                n = n - 1;
            } while (n != 0);
        }
        adding = false;
    }

    public static void ThenAll(Action then)
    {
        if (busy.Count == 0 && after.Count == 0 && waiting.Count == 0)
        {
            then();
            return;
        }
        if (KillCam.Local == null)
        {
            FinishAll();
            then();
        }
        else
        {
            after.Enqueue(then);
        }
    }
}
