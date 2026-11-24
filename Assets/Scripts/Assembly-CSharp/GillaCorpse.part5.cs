using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class GillaCorpse
{
    private void Keep(int i, GameObject piece)
    {
        parts[i] = piece.transform;
        off[i] = true;
        TornArm arm;
        if (piece.TryGetComponent<TornArm>(out arm))
        {
            arm.CopyMaterials(Copies);
        }
        else
        {
            MeshRenderer[] renderers = piece.GetComponentsInChildren<MeshRenderer>();
            for (int k = 0; k < renderers.Length; k++)
            {
                MeshRenderer r = renderers[k];
                r.sharedMaterials = Copies(r.sharedMaterials);
            }
        }
        TornPiece tp;
        if (piece.TryGetComponent<TornPiece>(out tp))
        {
            tp.enabled = false; /* ? */
        }
        IgnorePart(box, piece.transform);
    }

    private GameObject Baked(SkinnedMeshRenderer smr, string name)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Corpse_" + name;
        smr.BakeMesh(mesh, false);
        mesh.RecalculateBounds();
        GameObject go = new GameObject(name);
        go.layer = 2;
        Transform t = go.transform;
        Vector3 position = smr.transform.position;
        Quaternion rotation = smr.transform.rotation;
        t.SetPositionAndRotation(position, rotation);
        go.transform.SetParent(transform, true);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = Copies(smr.sharedMaterials);
        GillaSplatter.CarryOver(smr, go.GetComponent<MeshRenderer>());
        return go;
    }

    private Texture Own(Texture2D fur)
    {
        Texture have;
        if (furs.TryGetValue(fur, out have))
        {
            return have;
        }
        if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None)
        {
            return fur;
        }
        bool skip = fur.mipmapCount > 1;
        int shift = skip ? 1 : 0;
        int w = fur.width >> shift;
        if (w < 2) w = 1;
        int h = fur.height >> shift;
        if (h < 2) h = 1;
        Texture2D tex = new Texture2D(w, h, fur.format, fur.mipmapCount - shift, false);
        tex.name = fur.name + "_Corpse";
        tex.filterMode = fur.filterMode;
        tex.wrapMode = fur.wrapMode;
        for (int m = 0; m < tex.mipmapCount; m++)
        {
            Graphics.CopyTexture(fur, 0, shift + m, tex, 0, m);
        }
        furs[fur] = tex;
        return tex;
    }

    private Material[] Copies(Material[] mats)
    {
        Material[] result = new Material[mats.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            Material copy;
            if (!copies.TryGetValue(mats[i], out copy))
            {
                Material source = mats[i];
                copy = new Material(source);
                copies[source] = copy;
                Texture main = copy.mainTexture;
                if (main != null && main.GetType() == typeof(Texture2D))
                {
                    if (main.name.StartsWith("Gilla_Fur_"))
                    {
                        copy.mainTexture = Own((Texture2D)main);
                    }
                }
            }
            result[i] = copy;
        }
        return result;
    }

    private static void IgnorePart(Collider with, Transform part)
    {
        if (with == null) return;
        if (part == null) return;
        TornArm arm;
        if (part.TryGetComponent<TornArm>(out arm))
        {
            arm.Ignore(with);
        }
        else
        {
            Collider col;
            if (part.TryGetComponent<Collider>(out col))
            {
                Physics.IgnoreCollision(with, col);
            }
        }
    }

    public void IgnoreYou(Collider col)
    {
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player == null) return;
        Collider[] cols = player.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            Physics.IgnoreCollision(col, cols[i]);
        }
    }

    public static void Clear(int id)
    {
        for (int index = All.Count - 1; index >= 0; index--)
        {
            if (All[index] == null || All[index].Owner == id)
            {
                Gone(index);
            }
        }
    }

    public static void ClearAll()
    {
        for (int i = All.Count - 1; i >= 0; i--)
        {
            Gone(i);
        }
        holds.Clear();
    }

    public static void ClearHunts()
    {
        for (int index = All.Count - 1; index >= 0; index--)
        {
            if (All[index] == null)
            {
                Gone(index);
            }
            else if (All[index].goneAt == float.PositiveInfinity)
            {
                All[index].goneAt = AfterHunt + Time.time;
            }
        }
    }

    private static void Gone(int i)
    {
        GillaCorpse c = All[i];
        All.RemoveAt(i);
        if (c == null) return;
        List<(int, bool)> drop = new List<(int, bool)>();
        foreach (KeyValuePair<(int, bool), Hold> kv in holds)
        {
            if (kv.Value.corpse == c)
            {
                drop.Add(kv.Key);
            }
        }
        foreach ((int, bool) key in drop)
        {
            holds.Remove(key);
        }
        List<MeshFilter> meshes = new List<MeshFilter>(c.GetComponentsInChildren<MeshFilter>());
        for (int p = 1; p < 4; p++)
        {
            if (c.off[p] && c.parts[p] != null)
            {
                meshes.AddRange(c.parts[p].GetComponentsInChildren<MeshFilter>());
                TornArm arm;
                if (c.parts[p].TryGetComponent<TornArm>(out arm))
                {
                    arm.AddMeshes(meshes);
                }
                UnityEngine.Object.Destroy(c.parts[p].gameObject);
            }
        }
        foreach (MeshFilter mf in meshes)
        {
            if (mf != null && mf.sharedMesh != null)
            {
                if (mf.sharedMesh.name.StartsWith("Corpse_") || mf.sharedMesh.name.StartsWith("Torn_"))
                {
                    UnityEngine.Object.Destroy(mf.sharedMesh);
                }
            }
        }
        foreach (Material m in c.copies.Values)
        {
            if (m != null) UnityEngine.Object.Destroy(m);
        }
        foreach (Texture t in c.furs.Values)
        {
            if (t != null) UnityEngine.Object.Destroy(t);
        }
        UnityEngine.Object.Destroy(c.gameObject);
    }

    private void OnDestroy()
    {
        All.Remove(this);
    }

    public static GillaCorpse Find(int id)
    {
        foreach (GillaCorpse c in All)
        {
            if (c != null && c.Id == id)
            {
                return c;
            }
        }
        return null;
    }

    public bool IsOff(int part)
    {
        if (part >= 1 && part <= 3)
        {
            return off[part];
        }
        return false;
    }

    public Transform PartTransform(int part)
    {
        if ((uint)part < 4)
        {
            return parts[part];
        }
        return null;
    }

    public bool Has(int part)
    {
        if ((uint)part > 3) return false;
        return parts[part] != null;
    }

    public Transform Thing(int part)
    {
        if (!IsOff(part))
        {
            return transform;
        }
        return parts[part];
    }

    public TornArm ArmOf(int part)
    {
        if (!IsOff(part)) return null;
        if (!(parts[part] != null)) return null;
        TornArm arm;
        if (parts[part].TryGetComponent<TornArm>(out arm))
        {
            return arm;
        }
        return null;
    }

    private bool ArmBones(int part, out Transform up, out Transform lo, out Transform hand)
    {
        string side = part == 2 ? "_L" : "_R";
        lo = null;
        hand = null;
        if (!bones.TryGetValue("upperarm" + side, out up)) return false;
        if (!bones.TryGetValue("lowerarm" + side, out lo)) return false;
        return bones.TryGetValue("hand" + side, out hand);
    }

    public Transform HeadBone
    {
        get
        {
            Transform t;
            if (!bones.TryGetValue("head", out t))
            {
                t = null;
            }
            return t;
        }
    }

    public Transform Bone(string name)
    {
        Transform t;
        if (!bones.TryGetValue(name, out t))
        {
            t = null;
        }
        return t;
    }

    public int LinkOn(int part, Vector3 at, out Transform on)
    {
        Transform up, lo, hand;
        on = parts[part];
        if (part == 1 || !ArmBones(part, out up, out lo, out hand))
        {
            return 0;
        }
        float dUp = ToLine(at, up.position, lo.position);
        float dLo = ToLine(at, lo.position, hand.position);
        Vector3 handPos = hand.position;
        Vector3 tip = hand.position + hand.up * 0.1f * hand.lossyScale.x;
        float dHand = ToLine(at, handPos, tip);
        if (dUp <= dLo && dUp <= dHand)
        {
            on = up;
            return 0;
        }
        on = dHand < dLo ? hand : lo;
        return dLo <= dHand ? 1 : 2;
    }

    private static float ToLine(Vector3 at, Vector3 a, Vector3 b)
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
        Vector3 d = a + ab * t - at;
        return (float)Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z);
    }

    private void LimbStill(int part, bool still)
    {
        if (limbs[part] == null) return;
        foreach (Collider col in limbs[part])
        {
            Rigidbody rb = col != null ? col.attachedRigidbody : null;
            if (rb == null) continue;
            if (rb.isKinematic != still)
            {
                rb.isKinematic = still;
                if (!still)
                {
                    rb.WakeUp();
                }
            }
        }
    }

    public static GillaCorpse BodyIn(int by, bool right)
    {
        Hold h;
        if (!holds.TryGetValue((by, right), out h) || h.part != 0)
        {
            h.corpse = null;
        }
        return h.corpse;
    }

    public bool InAHand
    {
        get
        {
            foreach (KeyValuePair<(int, bool), Hold> kv in holds)
            {
                if (kv.Value.corpse == this && kv.Value.part == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static bool Nearest(Vector3 at, float reach, out GillaCorpse corpse, out int part)
    {
        corpse = null;
        part = -1;
        foreach (GillaCorpse c in All)
        {
            if (c == null) continue;
            for (int p = 0; p < 4; p++)
            {
                if (c.parts[p] == null) continue;
                float d;
                if (p == 0)
                {
                    d = Distance(c.box, at) + 0.1f;
                }
                else if (!c.off[p])
                {
                    d = c.LimbDistance(p, at);
                }
                else
                {
                    d = Distance(c.parts[p], at);
                }
                if (d < reach)
                {
                    corpse = c;
                    part = p;
                    reach = d;
                }
            }
        }
        return corpse != null;
    }

    public int NearestOn(Vector3 at, float reach)
    {
        int best = 0;
        for (int p = 1; p < 4; p++)
        {
            if (parts[p] == null) continue;
            if (!off[p])
            {
                float d = LimbDistance(p, at);
                if (d < reach)
                {
                    reach = d;
                    best = p;
                }
            }
        }
        return best;
    }

    private static float Distance(Collider col, Vector3 at)
    {
        if (col == null)
        {
            return float.MaxValue;
        }
        Vector3 d = col.ClosestPoint(at) - at;
        return (float)Math.Sqrt(d.x * d.x + d.y * d.y + d.z * d.z);
    }

    private static float Distance(Transform t, Vector3 at)
    {
        float dist = 0f;
        TornArm arm;
        Collider col;
        Renderer rend;
        if (t.TryGetComponent<TornArm>(out arm))
        {
            arm.Nearest(at, out dist);
        }
        else if (t.TryGetComponent<Collider>(out col))
        {
            dist = Distance(col, at);
        }
        else
        {
            if (t.TryGetComponent<Renderer>(out rend))
            {
                Bounds b = rend.bounds;
                dist = b.SqrDistance(at);
            }
            else
            {
                Vector3 d = t.position - at;
                dist = d.x * d.x + d.y * d.y + d.z * d.z;
            }
            dist = Mathf.Sqrt(dist);
        }
        return dist;
    }

    public static void HoldIt(PlayerRef by, bool right, int id, int part, Vector3 pos, Quaternion rot)
    {
        GillaCorpse c = Find(id);
        if (c == null) return;
        int p = part & PartOnly;
        if (!c.Has(p)) return;
        Transform thing = c.Thing(p);
        if (!c.IsOff(p))
        {
            p = 0;
        }
        List<(int, bool)> drop = new List<(int, bool)>();
        foreach (KeyValuePair<(int, bool), Hold> kv in holds)
        {
            if (kv.Value.corpse == c && c.Thing(kv.Value.part) == thing)
            {
                drop.Add(kv.Key);
            }
        }
        foreach ((int, bool) key in drop)
        {
            holds.Remove(key);
        }
        holds[(by.PlayerId, right)] = new Hold { corpse = c, part = p, pos = pos, rot = rot };
        TornArm arm;
        if (thing.TryGetComponent<TornArm>(out arm))
        {
            arm.Hold(part >> LinkShift, by.PlayerId, right, pos, rot);
            return;
        }
        Rigidbody rb;
        if (!thing.TryGetComponent<Rigidbody>(out rb)) return;
        rb.isKinematic = true;
    }

    public static void LetGo(PlayerRef by, bool right, Vector3 at, Quaternion rot, Vector3 velocity, Vector3 spin)
    {
        Hold h;
        if (!holds.TryGetValue((by.PlayerId, right), out h)) return;
        holds.Remove((by.PlayerId, right));
        if (h.corpse == null) return;
        if (!h.corpse.Has(h.part)) return;
        Transform thing = h.corpse.Thing(h.part);
        h.corpse.told[h.part] = false;
        TornPiece tp;
        if (thing.TryGetComponent<TornPiece>(out tp))
        {
            tp.Again();
        }
        TornArm arm;
        if (thing.TryGetComponent<TornArm>(out arm))
        {
            arm.LetGo(at, rot, velocity, spin);
            return;
        }
        thing.SetPositionAndRotation(at, rot);
        Rigidbody rb;
        if (!thing.TryGetComponent<Rigidbody>(out rb)) return;
        rb.position = at;
        rb.rotation = rot;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        rb.angularVelocity = spin;
        rb.WakeUp();
    }
}
