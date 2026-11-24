// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class GillaCorpse : MonoBehaviour
{
    private struct Hold
    {
        public GillaCorpse corpse;
        public int part;
        public Vector3 pos;
        public Quaternion rot;
    }

    public const int Body = 0;
    public const int Parts = 4;
    public const int LinkShift = 4;
    private const int PartOnly = 15;
    public static readonly List<GillaCorpse> All = new List<GillaCorpse>();
    public static float OutOfHunt = 45f;
    public static float AfterHunt = 20f;
    private float goneAt = float.PositiveInfinity;
    private readonly Transform[] parts = new Transform[4];
    private readonly bool[] off = new bool[4];
    private readonly bool[] told = new bool[4];
    private readonly Vector3[] stillAt = new Vector3[4];
    private readonly Vector3[] stillEnd = new Vector3[4];
    private readonly Quaternion[] stillTurned = new Quaternion[4];
    private readonly float[] stillSince = new float[4];
    private const float StillFor = 1.5f;
    private const float StillWithin = 0.02f;
    private const float StillTurn = 3f;
    private readonly Transform[] wounds = new Transform[4];
    private readonly GameObject[] stumps = new GameObject[4];
    private BoxCollider box;
    private Rigidbody[] weights;
    private Vector3 flatWay;
    private float flatTill = -1f;
    private const float FlatFor = 1.5f;
    private const float FlatRate = 6f;
    private const float FlatFastest = 5f;
    private readonly Dictionary<Transform, Transform> skeleton = new Dictionary<Transform, Transform>();
    private readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
    private readonly Dictionary<string, Matrix4x4> binds = new Dictionary<string, Matrix4x4>();
    private readonly List<Collider>[] limbs = new List<Collider>[4];
    private readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    private readonly Dictionary<Texture, Texture> furs = new Dictionary<Texture, Texture>();
    private Material blood;
    private AudioClip[] tearSounds;
    private AudioClip[] dropSounds;
    private Vector4[] holes;
    private Vector4[] holeWays;
    private Vector4[] holeSides;
    private int holeCount;
    private GillaWounds hurtHere;
    private Vector3 holdSeam;
    private Vector3 outOfDoor;
    private readonly List<(Task<object> made, Transform on, Matrix4x4 bind, string name, Material material)> coming = new List<ValueTuple<Task<object>, Transform, Matrix4x4, string, Material>>();
    private bool ripping;
    private bool keptBack;
    private static readonly ProfilerMarker MakeMark = new ProfilerMarker("Gilla.Corpse.Make");
    private static readonly ProfilerMarker CopyMark = new ProfilerMarker("Gilla.Corpse.Copy");
    private static readonly ProfilerMarker RipMark = new ProfilerMarker("Gilla.Corpse.Rip");
    private static readonly ProfilerMarker HalfMark = new ProfilerMarker("Gilla.Corpse.Half");
    private static readonly ProfilerMarker InsideMark = new ProfilerMarker("Gilla.Corpse.Inside");
    private static readonly ProfilerMarker CopyBonesMark = new ProfilerMarker("Gilla.Corpse.CopyBones");
    private static readonly ProfilerMarker BoneMark = new ProfilerMarker("Gilla.Corpse.Bone");
    private static readonly Dictionary<(int, bool), Hold> holds = new Dictionary<ValueTuple<int, bool>, GillaCorpse.Hold>();
    public const float RipBias = 1.5f;
    private const int RipsAtOnce = 3;
    private const float RipWindow = 1.5f;
    private static readonly List<float> blastRips = new List<float>();

    public int Id { get; private set; }
    public int Owner { get; private set; }
    public bool IsHalf { get { return Owner != Id; } }
    public bool Ripped { get; private set; }
    public float MadeAt { get; private set; }
    public bool OnATimer { get { return goneAt != float.PositiveInfinity; } }
    public bool BonesComing { get { return coming.Count > 0; } }
    public static float LastRipMs { get; private set; }
    public Dictionary<Transform, Transform> Skeleton { get { return skeleton; } }

    public Vector3 HeadNow
    {
        get
        {
            if (!bones.TryGetValue("head", out Transform head))
                return MiddleNow;
            Matrix4x4 bind = binds["head"];
            return head.TransformPoint(bind.MultiplyPoint3x4(new Vector3(0f, -0.04f, 0.35f)));
        }
    }

    public Vector3 MiddleNow
    {
        get
        {
            if (box != null) return box.bounds.center;
            return transform.position;
        }
    }

    public Vector3 FrontNow
    {
        get
        {
            if (bones.TryGetValue("spine", out Transform spine))
            {
                Matrix4x4 bind = binds["spine"];
                return spine.TransformVector(bind.MultiplyVector(Vector3.down)).normalized;
            }
            return transform.forward;
        }
    }

    public Vector3 GoingNow
    {
        get
        {
            if (TryGetComponent(out Rigidbody rb) && !rb.isKinematic)
                return rb.linearVelocity;
            return Vector3.zero;
        }
    }

    private static bool PileUpRip_b__133_0(float now, float t)
    {
        return now - t > 1.5f;
    }

    private static void RipSoon_b__134_0(GillaCorpse c, GillaCorpse plus, Stopwatch clock, int seed, Vector3 going, Vector3 plusX, List<(Rigidbody rb, Vector3 going, Vector3 spin)> kept, float first)
    {
        if (c == null) return;
        if (plus == null) return;
        using (RipMark.Auto())
        {
            clock.Restart();
            GillaCorpse minus = c.MakeHalf(-1, seed, going - plusX * 1.6f + Vector3.up * 0.5f);
            plus.LetGoOn(kept);
            c.Apart(plus, minus, plusX);
            float ms = clock.ElapsedMilliseconds;
            LastRipMs = Mathf.Max(first, ms);
        }
    }

    private static IEnumerable<Vector3> Corners(Bounds b)
    {
        for (int k = 0; k < 8; k++)
        {
            yield return new Vector3(
                b.center.x + b.extents.x * ((k & 1) != 0 ? 1f : -1f),
                b.center.y + b.extents.y * ((k & 2) != 0 ? 1f : -1f),
                b.center.z + b.extents.z * ((k & 4) != 0 ? 1f : -1f));
        }
    }

    public static int HalfId(int id, int sign)
    {
        return (sign < 1 ? 200000 : 100000) + id;
    }

    public void GoIn(float seconds)
    {
        goneAt = Time.time + seconds;
    }

    public static int Part(ApePart p)
    {
        return (int)p + 1;
    }

    public static GillaCorpse Make(Dismemberment from, int id, byte torn, Vector3 velocity, bool quiet = false)
    {
        using (MakeMark.Auto())
        {
            if (from == null) return null;
            GillaPull.Done(from);
            foreach (ApePart p in Enum.GetValues(typeof(ApePart)))
            {
                if ((NetworkApe.TornBit(p) & torn) != 0 && !from.IsTorn(p))
                    from.Tear(p, Vector3.up + Vector3.up + Random.insideUnitSphere * 0.8f);
            }

            GillaCorpse c = Find(id);
            if (c != null && (c.parts[0] != null || c.Ripped))
            {
                Clear(id);
                c = null;
            }
            if (c == null) c = New(id);

            GameObject go = c.gameObject;
            go.transform.SetPositionAndRotation(from.transform.position, from.transform.rotation);
            c.parts[0] = go.transform;
            c.MadeAt = Time.time;
            c.blood = from.bloodMaterial;
            c.tearSounds = from.tearSounds;
            c.dropSounds = from.dropSounds;
            GillaWounds w = from.GetComponent<GillaWounds>();
            if (w != null)
            {
                w.Finish();
                w.Holes(out c.holes, out c.holeWays, out c.holeSides, out c.holeCount);
            }

            var ids = new Dictionary<SkinnedMeshRenderer, int>();
            foreach (var part in from.parts)
            {
                if (part.renderer != null) ids[part.renderer] = (int)part.id + 1;
            }

            c.CopySkeleton(from, true);
            Bounds bounds = default;
            bool any = false;
            foreach (SkinnedMeshRenderer smr in from.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled) continue;
                if (smr.name == "SonarGhost") continue;
                if (smr.name == "SuitTrim") continue;
                if (smr.GetComponentInParent<CosmeticToy>() != null) continue;
                SkinnedMeshRenderer copy = c.CopySkin(smr);
                if (ids.TryGetValue(smr, out int slot))
                    c.parts[slot] = copy.transform;
                else
                    c.Measure(smr, ref bounds, ref any);
            }

            c.Wear(from);
            foreach (var part in from.parts)
            {
                int i = (int)part.id + 1;
                if (part.joint != null)
                {
                    GameObject wound = new GameObject("Wound_" + part.id.ToString());
                    Transform wt = wound.transform;
                    wt.SetParent(go.transform, false);
                    wt.SetPositionAndRotation(part.joint.position, part.joint.rotation);
                    c.wounds[i] = wt;
                }
                if (part.stump != null)
                {
                    GameObject stump = Instantiate(part.stump, part.stump.transform.position, part.stump.transform.rotation, go.transform);
                    stump.transform.localScale = part.stump.transform.lossyScale;
                    foreach (Transform t in stump.GetComponentsInChildren<Transform>(true))
                        t.gameObject.layer = 2;
                    stump.SetActive(part.torn);
                    c.stumps[i] = stump;
                }
                if (!part.torn && part.renderer != null && part.renderer.enabled && !any)
                {
                    Vector3 center = go.transform.InverseTransformPoint(part.renderer.bounds.center);
                    bounds = new Bounds(center, Vector3.zero);
                    any = true;
                }
            }

            foreach (GameObject piece in from.TakePieces())
            {
                if (piece == null) continue;
                int i;
                if (piece.name == "Torn_Head") i = 1;
                else if (piece.name == "Torn_ArmL") i = 2;
                else if (piece.name == "Torn_ArmR") i = 3;
                else
                {
                    Destroy(piece);
                    continue;
                }
                if (c.parts[i] != null && c.parts[i] != piece.transform)
                {
                    Destroy(piece);
                    continue;
                }
                c.Keep(i, piece);
            }

            Vector3 spin = go.transform.right * Random.Range(-1f, 1f) * 3f + go.transform.forward * Random.Range(-1f, 1f) * 1.5f;
            if (spin.sqrMagnitude < 1f)
                spin = go.transform.right * 2.5f;
            c.Weigh(bounds, velocity, spin);
            Transform root = go.transform;
            Bounds world = c.WorldBox();
            Rigidbody rb = go.GetComponent<Rigidbody>();
            c.outOfDoor = GillaDoor.OutOfSlabs(root, world, rb, out Vector3 outOf) ? outOf : Vector3.zero;

            if (!quiet && (torn == 0 || velocity.sqrMagnitude > 0.25f))
            {
                Vector3 at = go.transform.TransformPoint(bounds.center);
                Sfx.Play(c.tearSounds, at, 1f, 0.7f, 0.9f);
                if (c.blood != null)
                    Blood.Burst(at, Vector3.up, c.blood, 1f);
            }

            for (int i = 0; i < 3; i++)
            {
                if (c.off[i + 1])
                    IgnorePart(c.box, c.parts[i + 1]);
            }

            TornPiece tp = go.AddComponent<TornPiece>();
            tp.dropSounds = c.dropSounds; /* ? */
            return c;
        }
    }

    public static GillaCorpse Limp(Dismemberment from, int id, Vector3 velocity, Vector3 way)
    {
        GillaCorpse c = New(id);
        All.Remove(c);
        GameObject go = c.gameObject;
        go.name = "Limp_" + id.ToString();
        go.transform.SetPositionAndRotation(from.transform.position, from.transform.rotation);
        c.parts[0] = go.transform;
        c.MadeAt = Time.time;
        c.dropSounds = from.dropSounds;

        var ids = new Dictionary<SkinnedMeshRenderer, int>();
        foreach (var part in from.parts)
        {
            if (part.renderer != null) ids[part.renderer] = (int)part.id + 1;
        }

        c.CopySkeleton(from, false);
        Bounds bounds = default;
        bool any = false;
        foreach (SkinnedMeshRenderer smr in from.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!smr.enabled) continue;
            if (smr.name == "SonarGhost") continue;
            if (smr.GetComponentInParent<CosmeticToy>() != null) continue;
            if (ids.TryGetValue(smr, out int slot))
                c.parts[slot] = c.skeleton[smr.transform];
            else
                c.Measure(smr, ref bounds, ref any);
        }

        c.Weigh(bounds, velocity, Vector3.zero);
        TornPiece tp = go.AddComponent<TornPiece>();
        tp.dropSounds = c.dropSounds; /* ? */
        c.weights = go.GetComponentsInChildren<Rigidbody>();
        c.FallsFlat(way);
        return c;
    }

    public void FallsFlat(Vector3 way)
    {
        flatWay = Vector3.ProjectOnPlane(way, Vector3.up);
        flatWay = flatWay.sqrMagnitude > 0.0001f ? flatWay.normalized : Vector3.zero;
        flatTill = Time.time + 1.5f;
    }

    private void FixedUpdate()
    {
        if (flatTill < 0f) return;
        if (flatTill < Time.time
            || !bones.TryGetValue("spine", out Transform spine)
            || !TryGetComponent(out Rigidbody rb)
            || rb.isKinematic)
        {
            flatTill = -1f;
            return;
        }
        Matrix4x4 bind = binds["spine"];
        Vector3 fwd = spine.TransformVector(bind.MultiplyVector(Vector3.forward)).normalized;
        Vector3 flat = Vector3.ProjectOnPlane(fwd, Vector3.up);
        if (fwd.y > 0.3f && flatWay != Vector3.zero /* ? */)
            flat = flatWay;
        if (flat.sqrMagnitude < 0.0001f) return;
        Vector3 axis = Vector3.Cross(fwd, flat);
        if (axis.sqrMagnitude < 1e-06f) return;
        Vector3 dir = axis.normalized;
        float angle = Vector3.Angle(fwd, flat);
        rb.angularVelocity = dir * Mathf.Min(angle * Mathf.Deg2Rad * 6f, 5f);
    }

    public Vector3 HandNow(bool right)
    {
        if (bones.TryGetValue(right ? "hand_R" : "hand_L", out Transform hand))
            return hand.position;
        return MiddleNow;
    }

    public void Shove(Vector3 by)
    {
        if (weights == null) return;
        for (int i = 0; i < weights.Length; i++)
        {
            Rigidbody rb = weights[i];
            if (rb != null && !rb.isKinematic)
                rb.AddForce(by, ForceMode.VelocityChange);
        }
    }

    private void Measure(SkinnedMeshRenderer smr, ref Bounds bounds, ref bool any)
    {
        Mesh mesh = new Mesh();
        smr.BakeMesh(mesh, false);
        mesh.RecalculateBounds();
        foreach (Vector3 corner in Corners(mesh.bounds))
        {
            Vector3 p = transform.InverseTransformPoint(smr.transform.position + smr.transform.rotation * corner);
            if (!any)
            {
                bounds = new Bounds(p, Vector3.zero);
                any = true;
            }
            else
            {
                bounds.Encapsulate(p);
            }
        }
        Destroy(mesh);
    }
}
