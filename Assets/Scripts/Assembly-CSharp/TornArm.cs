// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(50)]
public partial class TornArm : MonoBehaviour
{
    public const int Upper = 0;
    public const int Fore = 1;
    public const int Palm = 2;
    public const int Links = 3;
    private const float Jumped = 1f;
    private const float Trailing = 0.01f;
    private const float Rolls = 240f;
    private readonly Transform[] links = new Transform[3];
    private readonly Rigidbody[] bodies = new Rigidbody[3];
    private readonly CapsuleCollider[] shapes = new CapsuleCollider[3];
    private readonly float[] middles = new float[3];
    private readonly Dictionary<Transform, Transform> copied = new Dictionary<Transform, Transform>();
    private SkinnedMeshRenderer skin;
    private int held = -1;
    private int heldBy;
    private bool heldRight;
    private Vector3 heldAt;
    private Quaternion heldTurned;
    private float heldRoll;
    private Vector3 hingeOnUpper;
    private Vector3 hingeOnFore;
    private static readonly Dictionary<Mesh, bool[]> skinnedOn = new Dictionary<Mesh, bool[]>();

    public TornPiece Piece { get; private set; }

    public bool InHand
    {
        get { return held >= 0; }
    }

    public Transform HeldLink
    {
        get { return links[Mathf.Max(held, 0)]; }
    }

    public Transform Link(int i)
    {
        if (i > 1) i = 2;
        return links[Mathf.Max(i, 0)];
    }

    public Collider Shape(int i)
    {
        if (i > 1) i = 2;
        return shapes[Mathf.Max(i, 0)];
    }

    public Vector3 MiddleOf(int i)
    {
        if (i > 1) i = 2;
        return Vector3.up * middles[Mathf.Max(i, 0)];
    }

    public Vector3 MiddleNow
    {
        get { return HeldLink.position + HeldLink.rotation * MiddleOf(Mathf.Max(held, 0)); }
    }

    public static TornArm Make(SkinnedMeshRenderer from, string name, bool left, Material[] materials, Vector3 front, Vector3 going, Vector3 spin)
    {
        if (from == null) return null;
        if (from.sharedMesh == null) return null;
        string side = left ? "_L" : "_R";
        string upperName = "upperarm" + side;
        string foreName = "lowerarm" + side;
        string handName = "hand" + side;
        Transform[] bones = from.bones;
        Transform upper = null;
        Transform fore = null;
        Transform hand = null;
        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            if (bone == null) continue;
            if (bone.name == upperName) upper = bone;
            else if (bone.name == foreName) fore = bone;
            else if (bone.name == handName) hand = bone;
        }
        if (upper == null || fore == null || hand == null) return null;

        float size = Size(upper);
        Vector3 foreScale = fore.localScale;
        Vector3 handScale = hand.localScale;
        float length = fore.localPosition.magnitude;
        float length2 = hand.localPosition.magnitude;
        float sizeFore = size * Mathf.Abs(foreScale.x);
        Vector3 shoulder = upper.position;
        Vector3 toElbow = upper.rotation * (fore.localPosition * size);
        Vector3 toWrist = fore.rotation * (hand.localPosition * sizeFore);

        GameObject go = new GameObject(name);
        go.layer = 2;
        TornArm arm = go.AddComponent<TornArm>();
        arm.AddLink(0, go.transform, shoulder, upper.rotation, size, length, 0.034f, 1f, going, spin, shoulder);

        GameObject foreGo = new GameObject(fore.name);
        Vector3 elbow = shoulder + toElbow;
        foreGo.layer = 2;
        arm.AddLink(1, foreGo.transform, elbow, fore.rotation, sizeFore, length2, 0.03f, 0.7f, going, spin, shoulder);

        GameObject handGo = new GameObject(hand.name);
        float sizeHand = sizeFore * Mathf.Abs(handScale.x);
        Vector3 wrist = elbow + toWrist;
        handGo.layer = 2;
        arm.AddLink(2, handGo.transform, wrist, hand.rotation, sizeHand, 0.1f, 0.03f, 0.3f, going, spin, shoulder);

        arm.Piece = go.GetComponent<TornPiece>();
        for (int i = 1; i < 3; i++)
        {
            TornPiece piece = arm.links[i].GetComponent<TornPiece>();
            /* ? */ // piece.owner = arm.Piece;
            piece.enabled = false;
        }
        Physics.IgnoreCollision(arm.shapes[0], arm.shapes[2]);
        arm.copied[upper] = arm.links[0];
        arm.copied[fore] = arm.links[1];
        arm.copied[hand] = arm.links[2];

        Vector3 a = elbow - shoulder;
        Vector3 b = wrist - elbow;
        Vector3 axis = Vector3.Cross(a, b);
        if (axis.sqrMagnitude < a.sqrMagnitude * 0.0076f * b.sqrMagnitude)
        {
            axis = Vector3.Cross(b, front);
        }
        if (axis.sqrMagnitude < 1e-08f)
        {
            axis = arm.links[1].right;
        }

        HingeJoint hinge = arm.links[1].gameObject.AddComponent<HingeJoint>();
        hinge.connectedBody = arm.bodies[0];
        hinge.anchor = Vector3.zero;
        hinge.axis = arm.links[1].InverseTransformDirection(axis.normalized);
        float angle = Vector3.Angle(a, b);
        hinge.useLimits = true;
        JointLimits limits = new JointLimits();
        limits.min = -angle;
        limits.max = 140f - angle;
        limits.bounciness = 0f;
        limits.bounceMinVelocity = 0f;
        hinge.limits = limits;
        arm.hingeOnUpper = arm.links[0].InverseTransformDirection(axis.normalized);
        arm.hingeOnFore = hinge.axis;

        CharacterJoint j = arm.links[2].gameObject.AddComponent<CharacterJoint>();
        typeof(GillaCorpse).GetMethod("Swing", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { j, arm.bodies[1], 50f, 65f, 30f }); /* ? */
        arm.Skin(from, bones, hand, materials, size, (sizeHand * 0.1f + size * length + sizeFore * length2) / size);
        return arm;
    }

    private static float Size(Transform bone)
    {
        Vector3 s = bone.lossyScale;
        float x = Mathf.Abs(s.x);
        float y = Mathf.Abs(s.y);
        float z = Mathf.Abs(s.z);
        return Mathf.Max(Mathf.Min(x, y), Mathf.Min(z, Mathf.Max(x, y)));
    }

    private void AddLink(int i, Transform t, Vector3 at, Quaternion turned, float size, float length, float radius, float mass, Vector3 going, Vector3 spin, Vector3 about)
    {
        t.SetPositionAndRotation(at, turned);
        t.localScale = Vector3.one * size;
        links[i] = t;
        middles[i] = size * length * 0.5f;
        shapes[i] = (CapsuleCollider)typeof(GillaCorpse).GetMethod("Capsule", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { t, length, radius }); /* ? */
        Rigidbody body = t.gameObject.AddComponent<Rigidbody>();
        body.mass = mass;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.8f;
        body.maxDepenetrationVelocity = 3f;
        body.maxAngularVelocity = 20f;
        body.solverIterations = 10;
        body.solverVelocityIterations = 2;
        body.sleepThreshold = 0.02f;
        body.inertiaTensor = mass * 0.02f * Vector3.one;
        body.linearVelocity = going + Vector3.Cross(spin, at - about);
        body.angularVelocity = spin;
        bodies[i] = body;
        t.gameObject.AddComponent<TornPiece>();
    }

    private void Skin(SkinnedMeshRenderer from, Transform[] bones, Transform end, Material[] materials, float sizeUp, float reach)
    {
        Mesh mesh = from.sharedMesh;
        bool[] on = SkinnedOn(mesh);
        Transform[] mapped = new Transform[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            Transform bone = bones[i];
            if (bone == null) continue;
            if (i >= on.Length || !on[i]) continue;
            Transform copy;
            if (!copied.TryGetValue(bone, out copy))
            {
                if (!bone.IsChildOf(end))
                {
                    GameObject go = new GameObject(bone.name);
                    go.layer = 2;
                    copy = go.transform;
                    copy.SetParent(links[0], false);
                    copy.SetPositionAndRotation(bone.position, bone.rotation);
                    copy.localScale = Vector3.one * (Size(bone) / sizeUp);
                    copied[bone] = copy;
                    mapped[i] = copy;
                    continue;
                }
                copy = CopyUnderHand(bone);
            }
            mapped[i] = copy;
        }
        for (int i = 0; i < mapped.Length; i++)
        {
            if (mapped[i] == null)
            {
                mapped[i] = links[0];
            }
        }
        skin = gameObject.AddComponent<SkinnedMeshRenderer>();
        skin.sharedMesh = mesh;
        skin.sharedMaterials = materials;
        skin.bones = mapped;
        skin.rootBone = links[0];
        skin.localBounds = new Bounds(Vector3.zero, Vector3.one * reach + Vector3.one * reach);
        skin.shadowCastingMode = from.shadowCastingMode;
        GillaSplatter.CarryOver(from, skin);
    }

    private Transform CopyUnderHand(Transform bone)
    {
        Transform found;
        if (copied.TryGetValue(bone, out found))
        {
            return found;
        }
        GameObject go = new GameObject(bone.name);
        go.layer = 2;
        Transform t = go.transform;
        Transform parent = CopyUnderHand(bone.parent);
        t.SetParent(parent, false);
        t.SetLocalPositionAndRotation(bone.localPosition, bone.localRotation);
        t.localScale = bone.localScale;
        copied[bone] = t;
        return t;
    }

    private static bool[] SkinnedOn(Mesh mesh)
    {
        bool[] on;
        if (skinnedOn.TryGetValue(mesh, out on))
        {
            return on;
        }
        on = new bool[mesh.bindposeCount];
        var weights = mesh.GetAllBoneWeights();
        for (int i = 0; i < weights.Length; i++)
        {
            BoneWeight1 w = weights[i];
            if (w.weight > 0f && w.boneIndex < on.Length)
            {
                on[w.boneIndex] = true;
            }
        }
        skinnedOn[mesh] = on;
        return on;
    }

    public void TakeGore(Transform gore, Transform on)
    {
        Transform target;
        if (on != null && copied.TryGetValue(on, out target))
        {
            gore.SetParent(target, false);
        }
        else
        {
            gore.SetParent(links[0], true);
        }
        GillaWoundBend bend;
        if (!gore.TryGetComponent<GillaWoundBend>(out bend))
        {
            return;
        }
        if (bend.other == null)
        {
            return;
        }
        Transform other;
        bend.other = copied.TryGetValue(bend.other, out other) ? other : null;
        bend.Bend();
    }

    public void Ignore(Collider other)
    {
        if (other == null) return;
        for (int i = 0; i < shapes.Length; i++)
        {
            Collider c = shapes[i];
            if (c != null)
            {
                Physics.IgnoreCollision(c, other);
            }
        }
    }

    public int Nearest(Vector3 at, out float away)
    {
        int best = 0;
        away = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            if (shapes[i] == null) continue;
            Vector3 p = shapes[i].ClosestPoint(at);
            float d = (p - at).magnitude;
            if (d < away)
            {
                best = i;
                away = d;
            }
        }
        return best;
    }

    public void AddMeshes(List<MeshFilter> to)
    {
        for (int i = 1; i < 3; i++)
        {
            if (links[i] != null)
            {
                to.AddRange(links[i].GetComponentsInChildren<MeshFilter>());
            }
        }
    }

    public void CopyMaterials(Func<Material[], Material[]> copies)
    {
        skin.sharedMaterials = copies(skin.sharedMaterials);
        for (int i = 0; i < 3; i++)
        {
            if (links[i] != null)
            {
                MeshRenderer[] renderers = links[i].GetComponentsInChildren<MeshRenderer>();
                for (int k = 0; k < renderers.Length; k++)
                {
                    MeshRenderer r = renderers[k];
                    r.sharedMaterials = copies(r.sharedMaterials);
                }
            }
        }
    }

    public Bounds WorldBox
    {
        get
        {
            Bounds box = new Bounds(links[0].position, Vector3.zero);
            for (int i = 1; i < 3; i++)
            {
                box.Encapsulate(links[i].position);
            }
            box.Encapsulate(links[2].TransformPoint(Vector3.up * 0.1f));
            box.Expand(0.1f);
            return box;
        }
    }

    public void OutOfDoors()
    {
        Vector3 before = links[0].position;
        if (!GillaDoor.OutOfSlabs(links[0], WorldBox, bodies[0]))
        {
            return;
        }
        Vector3 after = links[0].position;
        for (int i = 1; i < 3; i++)
        {
            Transform t = links[i];
            t.position = (after - before) + t.position;
            bodies[i].position = links[i].position;
        }
    }

    public void Hold(int link, int by, bool right, Vector3 pos, Quaternion rot)
    {
        if (link > 1) link = 2;
        link = Mathf.Max(link, 0);
        if (held >= 0 && held != link)
        {
            bodies[held].isKinematic = false;
        }
        held = link;
        heldBy = by;
        heldRight = right;
        heldAt = pos;
        heldTurned = rot;
        heldRoll = 0f;
        bodies[link].isKinematic = true;
        Transform hand = (Transform)typeof(GillaCorpse).GetMethod("Hand", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { by, right }); /* ? */
        if (hand != null)
        {
            Vector3 at = hand.position + hand.rotation * pos;
            Put(link, at, hand.rotation * rot);
        }
    }

    public void LetGo(Vector3 at, Quaternion rot, Vector3 velocity, Vector3 spin)
    {
        int link = Mathf.Max(held, 0);
        held = -1;
        Put(link, at, rot);
        Rigidbody body = bodies[link];
        body.isKinematic = false;
        body.linearVelocity = velocity;
        body.angularVelocity = spin;
        for (int i = 0; i < 3; i++)
        {
            bodies[i].WakeUp();
        }
    }

    public void Place(int link, Vector3 at, Quaternion rot)
    {
        int i0 = link > 1 ? 2 : link;
        if (link < 1)
        {
            Put(0, at, rot);
        }
        else
        {
            i0 = Mathf.Max(i0, 0);
            links[i0].SetPositionAndRotation(at, rot);
            bodies[i0].position = at;
            bodies[i0].rotation = rot;
        }
        for (int i = 0; i < 3; i++)
        {
            if (!bodies[i].isKinematic)
            {
                bodies[i].linearVelocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
                bodies[i].Sleep();
            }
        }
    }

    private void Put(int link, Vector3 at, Quaternion turned)
    {
        Vector3 from = links[link].position;
        Quaternion delta = turned * Quaternion.Inverse(links[link].rotation);
        for (int i = 0; i < 3; i++)
        {
            Transform t = links[i];
            Vector3 offset = delta * (t.position - from);
            Quaternion rotation = delta * links[i].rotation;
            t.SetPositionAndRotation(at + offset, rotation);
            bodies[i].position = links[i].position;
            bodies[i].rotation = links[i].rotation;
        }
    }
}
