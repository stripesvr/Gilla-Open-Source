// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaCorpse : MonoBehaviour
{
    private bool NewlyAtRest(int i, out Vector3 at, out Quaternion rot)
    {
        Rigidbody rb = null;
        at = Vector3.zero;
        rot = default(Quaternion);
        if (i != 0 && !IsOff(i))
        {
            return false;
        }
        Transform t = parts[i];
        TornArm arm = ArmOf(i);
        if (t != null && !told[i] && t.TryGetComponent<Rigidbody>(out rb) && !rb.isKinematic)
        {
            if (!(arm != null && arm.InHand))
            {
                Transform endT = t;
                if (arm != null)
                {
                    endT = arm.Link(2);
                }
                Vector3 end = endT.position;
                bool moved;
                if (stillSince[i] <= 0f)
                {
                    moved = true;
                }
                else if ((t.position - stillAt[i]).sqrMagnitude > 0.0004f)
                {
                    moved = true;
                }
                else if ((end - stillEnd[i]).sqrMagnitude > 0.0004f)
                {
                    moved = true;
                }
                else
                {
                    moved = Quaternion.Angle(t.rotation, stillTurned[i]) > 3f;
                }
                if (moved)
                {
                    stillSince[i] = Time.time;
                    stillAt[i] = t.position;
                    stillEnd[i] = end;
                    stillTurned[i] = t.rotation;
                }
                if (Time.time - stillSince[i] < 1.5f)
                {
                    return false;
                }
                told[i] = true;
                at = t.position;
                rot = t.rotation;
                return true;
            }
        }
        stillSince[i] = 0f;
        return false;
    }

    private void Loosen()
    {
        bool held = false;
        foreach (KeyValuePair<ValueTuple<int, bool>, Hold> kv in holds)
        {
            if (kv.Value.corpse == this && kv.Value.part == 0)
            {
                held = true;
                break;
            }
        }
        if (!held)
        {
            Rigidbody rb;
            if (TryGetComponent<Rigidbody>(out rb))
            {
                rb.isKinematic = false;
            }
            told[0] = false;
        }
    }

    public static void Place(int id, int part, Vector3 at, Quaternion rot)
    {
        GillaCorpse c = Find(id);
        if (c == null)
        {
            return;
        }
        int p = part & 15;
        if (!c.Has(p))
        {
            return;
        }
        if (p != 0 && !c.IsOff(p))
        {
            return;
        }
        Transform t = c.parts[p];
        foreach (KeyValuePair<ValueTuple<int, bool>, Hold> kv in holds)
        {
            if (kv.Value.corpse == c && kv.Value.part == p)
            {
                return;
            }
        }
        c.told[p] = true;
        TornArm arm;
        if (t.TryGetComponent<TornArm>(out arm))
        {
            arm.Place(part >> 4, at, rot);
            return;
        }
        t.SetPositionAndRotation(at, rot);
        Rigidbody rb;
        if (!t.TryGetComponent<Rigidbody>(out rb))
        {
            return;
        }
        rb.position = at;
        rb.rotation = rot;
        if (rb.isKinematic)
        {
            return;
        }
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        if (p == 0)
        {
            rb.isKinematic = true;
        }
        else
        {
            rb.Sleep();
        }
    }

    public static void TearOff(PlayerRef by, bool right, int id, int part, Quaternion rot)
    {
        int p = part & 15;
        GillaCorpse c = Find(id);
        if (p < 1 || p > 3 || c == null)
        {
            return;
        }
        if (c.parts[p] == null)
        {
            return;
        }
        c.Detach(p);
        Transform t = c.parts[p];
        Vector3 mid;
        TornArm arm;
        Renderer renderer;
        if (t.TryGetComponent<TornArm>(out arm))
        {
            mid = rot * arm.MiddleOf(part >> 4);
        }
        else
        {
            Vector3 local;
            if (t.TryGetComponent<Renderer>(out renderer))
            {
                Quaternion inv = Quaternion.Inverse(t.rotation);
                local = inv * (renderer.bounds.center - t.position);
            }
            else
            {
                local = Vector3.zero;
            }
            mid = rot * local;
            part = p;
        }
        HoldIt(by, right, id, part, -mid, rot);
    }

    private void Detach(int part)
    {
        TornArm arm = null;
        SkinnedMeshRenderer skinned = null;
        Transform t = parts[part];
        if (t == null)
        {
            return;
        }
        if (off[part])
        {
            return;
        }
        off[part] = true;
        bool tearing = GillaPull.Tearing(GetComponent<Dismemberment>(), (ApePart)(part - 1)) /* ? */;
        if (t.TryGetComponent<SkinnedMeshRenderer>(out skinned))
        {
            t = Cut(part, skinned);
            parts[part] = t;
        }
        t.SetParent(null, true);
        TornPiece piece;
        if (t.TryGetComponent<TornArm>(out arm))
        {
            for (int k = 0; k < 3; k++)
            {
                IgnoreYou(arm.Shape(k));
            }
            Collider[] cols = GetComponentsInChildren<Collider>();
            for (int k = 0; k < cols.Length; k++)
            {
                arm.Ignore(cols[k]);
            }
            piece = arm.Piece;
        }
        else
        {
            MeshFilter filter = t.GetComponent<MeshFilter>();
            BoxCollider collider = t.gameObject.AddComponent<BoxCollider>();
            if (filter != null && filter.sharedMesh != null)
            {
                collider.center = filter.sharedMesh.bounds.center;
                Bounds mb = filter.sharedMesh.bounds;
                collider.size = (mb.extents + mb.extents) * 0.85f;
            }
            Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = part == 1 ? 3f : 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            IgnoreYou(collider);
            if (box != null)
            {
                Physics.IgnoreCollision(collider, box);
            }
            piece = t.gameObject.AddComponent<TornPiece>();
        }
        piece.dropSounds = dropSounds;
        if (stumps[part] != null)
        {
            stumps[part].SetActive(true);
        }
        Transform wound = wounds[part];
        if (!(wound != null))
        {
            return;
        }
        Sfx.Play(tearSounds, wound.position, 1f, 0.9f, 1.08f);
        if (tearing)
        {
            GillaPull.WetSnap(wound.position);
        }
        if (blood != null)
        {
            Vector3 up = Vector3.up;
            Vector3 woundPos = wound.position;
            Transform me = transform;
            Vector3 centre = box != null ? box.center : Vector3.zero;
            Vector3 middle = me.TransformPoint(centre);
            Vector3 dir = ((woundPos - middle).normalized + up).normalized;
            Blood.Burst(wound.position, dir, blood, 0.6f);
            Blood.Spray(wound, dir, blood, 3f, 70, 4, 1f);
        }
        GillaSplatter.Around(wound.position, part == 1 ? 1.2f : 0.9f, null);
        Vector3 scale = transform.lossyScale;
        Vector3 from = wound.position;
        Vector3 outWay = wound.position - transform.position;
        float width = part == 1 ? 0.065f : 0.05f;
        int count = part == 1 ? 18 : 15;
        GillaStrands.Between(wound, t, from, outWay, Vector3.zero, width * scale.y, count, 0, 0f) /* ? */;
    }

    private static Transform Hand(int by, bool right)
    {
        NetworkApe local = NetworkApe.Local;
        int me;
        if (local == null)
        {
            PlayerRef none = PlayerRef.None;
            me = none.PlayerId;
        }
        else
        {
            me = local.Object.InputAuthority.PlayerId /* ? */;
        }
        if (me == by && MonsterPlayer.Local != null)
        {
            if (right)
            {
                return MonsterPlayer.Local.rightFollower;
            }
            return MonsterPlayer.Local.leftFollower;
        }
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape != null && ape.Object.InputAuthority.PlayerId /* ? */ == by)
            {
                return right ? ape.transform /* ? */ : ape.transform /* ? */;
            }
        }
        return null;
    }

    private void LateUpdate()
    {
        if (goneAt <= Time.time)
        {
            int owner = Owner;
            int index = All.Count;
            while (index - 1 >= 0)
            {
                index--;
                if (All[index] == null || All[index].Owner == owner)
                {
                    Gone(index);
                }
                if (index <= 0) break;
            }
            return;
        }
        holdSeam *= Mathf.Exp(Time.deltaTime * -16f);
        for (int i = coming.Count - 1; i >= 0; i--)
        {
            ValueTuple<Task<object>, Transform, Matrix4x4, string, Material> item = coming[i];
            Task<object> made = item.Item1;
            Transform on = item.Item2;
            Matrix4x4 bind = item.Item3;
            string name = item.Item4;
            Material material = item.Item5;
            if (made.IsCompleted)
            {
                coming.RemoveAt(i);
                if (made.IsFaulted)
                {
                    Exception e = made.Exception;
                    Debug.LogError("[GillaCorpse] a bone wouldn't break: " + (e == null ? null : e.ToString()));
                }
                else if (on != null)
                {
                    AddBone(made.Result, on, bind, name, material);
                }
            }
        }
        foreach (KeyValuePair<ValueTuple<int, bool>, Hold> kv in holds)
        {
            Hold hold = kv.Value;
            if (hold.corpse != this)
            {
                continue;
            }
            Transform hand = Hand(kv.Key.Item1, kv.Key.Item2);
            Transform thing = Thing(hold.part);
            if (hand == null || thing == null)
            {
                continue;
            }
            TornArm arm;
            if (!thing.TryGetComponent<TornArm>(out arm))
            {
                Vector3 seam = hold.part != 0 ? Vector3.zero : holdSeam;
                Vector3 position = hand.position + hand.rotation * hold.pos + seam;
                thing.SetPositionAndRotation(position, hand.rotation * hold.rot);
            }
        }
    }

    public GillaCorpse()
    {
        goneAt = float.PositiveInfinity;
        parts = new Transform[4];
        off = new bool[4];
        told = new bool[4];
        stillAt = new Vector3[4];
        stillEnd = new Vector3[4];
        stillTurned = new Quaternion[4];
        stillSince = new float[4];
        wounds = new Transform[4];
        stumps = new GameObject[4];
        flatTill = -1f;
        skeleton = new Dictionary<Transform, Transform>();
        bones = new Dictionary<string, Transform>();
        binds = new Dictionary<string, Matrix4x4>();
        limbs = new List<Collider>[4];
        copies = new Dictionary<Material, Material>();
        furs = new Dictionary<Texture, Texture>();
        coming = new List<ValueTuple<Task<object>, Transform, Matrix4x4, string, Material>>();
    }

    static GillaCorpse()
    {
        All = new List<GillaCorpse>();
        OutOfHunt = 45f;
        AfterHunt = 20f;
        MakeMark = new ProfilerMarker("Gilla.Corpse.Make");
        CopyMark = new ProfilerMarker("Gilla.Corpse.Copy");
        RipMark = new ProfilerMarker("Gilla.Corpse.Rip");
        HalfMark = new ProfilerMarker("Gilla.Corpse.Half");
        InsideMark = new ProfilerMarker("Gilla.Corpse.Inside");
        CopyBonesMark = new ProfilerMarker("Gilla.Corpse.CopyBones");
        BoneMark = new ProfilerMarker("Gilla.Corpse.Bone");
        holds = new Dictionary<ValueTuple<int, bool>, Hold>();
        blastRips = new List<float>();
    }

    private void CopySkeleton_Want(Transform t, Dismemberment from, HashSet<Transform> wanted)
    {
        while (t != null)
        {
            if (!(t != from.transform))
            {
                return;
            }
            if (!wanted.Add(t))
            {
                return;
            }
            t = t.parent;
        }
    }

    private void CopySkeleton_Copy(Transform src, Transform dst, bool inside, HashSet<Transform> wanted, List<ValueTuple<GillaWoundBend, Transform, Matrix4x4>> bends)
    {
        foreach (Transform child in src)
        {
            MeshFilter filter;
            MeshRenderer renderer;
            if (child.name.StartsWith("GoreBone_") && child.TryGetComponent<MeshFilter>(out filter) && child.TryGetComponent<MeshRenderer>(out renderer))
            {
                if (inside)
                {
                    GameObject go = new GameObject(child.name);
                    go.layer = 2;
                    go.transform.SetParent(dst, false);
                    go.transform.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
                    go.transform.localScale = child.localScale;
                    Mesh mesh = filter.sharedMesh;
                    if (mesh.name == "Gore_Wall" || mesh.name == "Gore_Bone")
                    {
                        mesh = UnityEngine.Object.Instantiate<Mesh>(mesh);
                        mesh.name = "Corpse_Wall";
                    }
                    MeshFilter newFilter = go.AddComponent<MeshFilter>();
                    newFilter.sharedMesh = mesh;
                    MeshRenderer newRenderer = go.AddComponent<MeshRenderer>();
                    newRenderer.sharedMaterials = Copies(renderer.sharedMaterials);
                    newRenderer.shadowCastingMode = renderer.shadowCastingMode;
                    newRenderer.enabled = renderer.enabled;
                    GillaWoundBend bend;
                    if (child.TryGetComponent<GillaWoundBend>(out bend))
                    {
                        GillaWoundBend made = go.AddComponent<GillaWoundBend>();
                        bends.Add(new ValueTuple<GillaWoundBend, Transform, Matrix4x4>(made, bend.other /* ? */, bend.bind /* ? */));
                    }
                }
                continue;
            }
            if (wanted.Contains(child))
            {
                GameObject go = new GameObject(child.name);
                go.layer = 2;
                Transform copy = go.transform;
                copy.SetParent(dst, false);
                copy.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
                copy.localScale = child.localScale;
                Vector3 heldPos;
                Vector3 heldScale;
                if (ApeFace.Held(child, out heldPos, out heldScale))
                {
                    copy.localPosition = heldPos;
                    copy.localScale = heldScale;
                }
                skeleton[child] = copy;
                bones[child.name] = copy;
                CopySkeleton_Copy(child, copy, inside, wanted, bends);
            }
        }
    }
}
