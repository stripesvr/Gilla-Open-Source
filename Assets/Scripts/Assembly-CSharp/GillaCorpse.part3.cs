// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Fusion;

public partial class GillaCorpse : MonoBehaviour
{
    private void Ragdoll(Rigidbody torso, Vector3 velocity)
    {
        Transform head = null;
        Transform upper = null;
        Transform lower = null;
        Transform hand = null;
        List<Collider> all = new List<Collider>();
        all.Add(box);

        if (Has(1) && !off[1])
        {
            if (bones.TryGetValue("head", out head))
            {
                SphereCollider sphere = head.gameObject.AddComponent<SphereCollider>();
                Matrix4x4 bind = binds["head"];
                sphere.center = bind.MultiplyPoint(new Vector3(0f, -0.04f, 0.35f));
                sphere.radius = 0.08f;
                Limb(head, 4f, velocity);
                CharacterJoint headJoint = head.gameObject.AddComponent<CharacterJoint>();
                Swing(headJoint, torso, 25f, 40f, 40f);
                List<Collider> headList = new List<Collider>();
                headList.Add(sphere);
                limbs[1] = headList;
                all.Add(sphere);
            }
        }

        ValueTuple<ApePart, string>[] arms = new ValueTuple<ApePart, string>[2];
        arms[0] = new ValueTuple<ApePart, string>(ApePart.ArmL, "_L");
        arms[1] = new ValueTuple<ApePart, string>(ApePart.ArmR, "_R");
        for (int a = 0; a < arms.Length; a++)
        {
            int part = (int)arms[a].Item1 + 1;
            string side = arms[a].Item2;
            if (!Has(part)) continue;
            if (off[part]) continue;
            if (!bones.TryGetValue("upperarm" + side, out upper)) continue;
            if (!bones.TryGetValue("lowerarm" + side, out lower)) continue;
            if (!bones.TryGetValue("hand" + side, out hand)) continue;

            CapsuleCollider upperCap = Capsule(upper, lower.localPosition.magnitude, 0.034f);
            CapsuleCollider lowerCap = Capsule(lower, hand.localPosition.magnitude + 0.05f, 0.03f);
            Vector3 upperArm = lower.position - upper.position;
            Vector3 foreArm = hand.position - lower.position;
            Rigidbody upperRb = Limb(upper, 3f, velocity);
            Limb(lower, 2f, velocity);
            CharacterJoint shoulder = upper.gameObject.AddComponent<CharacterJoint>();
            Swing(shoulder, torso, 45f, 105f, 75f);
            HingeJoint elbow = lower.gameObject.AddComponent<HingeJoint>();
            elbow.connectedBody = upperRb;
            elbow.anchor = Vector3.zero;
            Vector3 axis = Vector3.Cross(upperArm, foreArm);
            if (axis.sqrMagnitude < upperArm.sqrMagnitude * 0.0001f * foreArm.sqrMagnitude)
            {
                axis = Vector3.Cross(foreArm, transform.forward);
            }
            elbow.axis = lower.InverseTransformDirection(axis.normalized);
            float bent = Vector3.Angle(upperArm, foreArm);
            elbow.useLimits = true;
            JointLimits limits = new JointLimits();
            limits.min = -bent;
            limits.max = 140f - bent;
            limits.bounciness = 0f;
            limits.bounceMinVelocity = 0f;
            limits.contactDistance = 2f; /* ? */
            elbow.limits = limits;

            List<Collider> armList = new List<Collider>();
            armList.Add(upperCap);
            armList.Add(lowerCap);
            limbs[part] = armList;
            all.Add(upperCap);
            all.Add(lowerCap);
        }

        for (int i = 0; i < all.Count; i++)
        {
            if (i != 0)
            {
                IgnoreYou(all[i]); /* ? */
            }
            for (int k = i + 1; k < all.Count; k++)
            {
                Physics.IgnoreCollision(all[i], all[k]);
            }
        }
    }

    private static CapsuleCollider Capsule(Transform bone, float length, float radius)
    {
        CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
        capsule.direction = 1;
        capsule.center = new Vector3(0f, length * 0.5f, 0f);
        capsule.height = radius + radius + length;
        capsule.radius = radius;
        return capsule;
    }

    private static Rigidbody Limb(Transform bone, float mass, Vector3 velocity)
    {
        Rigidbody rb = bone.gameObject.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.6f;
        rb.maxDepenetrationVelocity = 3f;
        rb.linearVelocity = velocity;
        rb.solverIterations = 20;
        rb.solverVelocityIterations = 8;
        rb.maxAngularVelocity = 15f;
        rb.inertiaTensor = mass * 0.02f * Vector3.one;
        return rb;
    }

    private static void Swing(CharacterJoint j, Rigidbody to, float twist, float swing1, float swing2)
    {
        j.connectedBody = to;
        j.anchor = Vector3.zero;
        j.axis = Vector3.up;
        j.swingAxis = Vector3.right;
        j.lowTwistLimit = new SoftJointLimit { limit = -twist, bounciness = 0f, contactDistance = 0f };
        j.highTwistLimit = new SoftJointLimit { limit = twist, bounciness = 0f, contactDistance = 0f };
        j.swing1Limit = new SoftJointLimit { limit = swing1, bounciness = 0f, contactDistance = 0f };
        j.swing2Limit = new SoftJointLimit { limit = swing2, bounciness = 0f, contactDistance = 0f };
        j.enableProjection = true;
        j.projectionDistance = 0.06f;
    }

    private float LimbDistance(int part, Vector3 at)
    {
        if (limbs[part] != null)
        {
            float best = float.MaxValue;
            foreach (Collider col in limbs[part])
            {
                if (col != null)
                {
                    float d = Distance(col, at);
                    if (d <= best)
                    {
                        best = d;
                    }
                }
            }
            return best;
        }
        return Distance(parts[part], at);
    }

    private Transform Cut(int part, SkinnedMeshRenderer skin)
    {
        Transform upper = null;
        Transform lower = null;
        Transform hand = null;
        string name = ((ApePart)(part - 1)).ToString();
        TornArm arm;
        Material[] mats = null;
        if (part == 1)
        {
            arm = null;
        }
        else
        {
            mats = skin.sharedMaterials;
            Vector3 front = transform.forward;
            arm = TornArm.Make(skin, name, part == 2, mats, front, Vector3.zero, Vector3.zero);
        }

        Transform root;
        if (arm != null)
        {
            root = arm.transform;
        }
        else
        {
            root = Baked(skin, name).transform; /* ? */
        }

        skin.enabled = false;
        if (arm != null && ArmBones(part, out upper, out lower, out hand))
        {
            for (int i = hand.childCount - 1; i >= 0; i--)
            {
                if (GoesWith(hand.GetChild(i)))
                {
                    arm.TakeGore(hand.GetChild(i), hand);
                }
            }
        }

        if (limbs[part] == null)
        {
            return root;
        }

        foreach (Collider col in limbs[part])
        {
            if (col == null) continue;
            Transform t = col.transform;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                if (GoesWith(t.GetChild(i)))
                {
                    if (arm != null)
                    {
                        arm.TakeGore(t.GetChild(i), t);
                    }
                    else
                    {
                        t.GetChild(i).SetParent(root, true);
                    }
                }
            }
        }

        for (int i = limbs[part].Count - 1; i >= 0; i--)
        {
            Collider col = limbs[part][i];
            if (col == null) continue;
            GameObject go = col.gameObject;
            Joint[] joints = go.GetComponents<Joint>();
            for (int k = 0; k < joints.Length; k++)
            {
                DestroyImmediate(joints[k]);
            }
            Rigidbody rb;
            if (go.TryGetComponent<Rigidbody>(out rb))
            {
                DestroyImmediate(rb);
            }
            DestroyImmediate(col);
        }
        limbs[part] = null;
        return root;
    }

    private static bool GoesWith(Transform t)
    {
        if (t.name.StartsWith("GoreBone_"))
        {
            return true;
        }
        return t.name.StartsWith("Worn_");
    }

    public static GillaCorpse New(int id)
    {
        GameObject go = new GameObject("Corpse_" + id.ToString());
        go.layer = 2;
        GillaCorpse corpse = go.AddComponent<GillaCorpse>();
        corpse.Id = id;
        corpse.Owner = id;
        All.Add(corpse);
        return corpse;
    }

    public bool RipsHere(Vector3 at, float reach)
    {
        if (Owner == Id && !Ripped)
        {
            if (!Has(0))
            {
                return false;
            }
            if (!(box == null))
            {
                float d = Distance(box, at);
                if (d < reach)
                {
                    for (int p = 1; p <= 3; p++)
                    {
                        if (parts[p] != null)
                        {
                            if (!off[p] && LimbDistance(p, at) * RipBias < d)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
            }
        }
        return false;
    }

    internal Vector3 PlusX()
    {
        Transform spine;
        if (bones.TryGetValue("spine", out spine))
        {
            Matrix4x4 bind = binds["spine"];
            return spine.TransformVector(bind.MultiplyVector(Vector3.right)).normalized;
        }
        return transform.right;
    }

    public static bool RipIt(PlayerRef by, int id, int seed, bool plusInRight, Quaternion rotRight, Quaternion rotLeft, bool inHands)
    {
        GillaCorpse c = Find(id);
        if (c == null) return false;
        if (c.Owner != c.Id) return false;
        if (c.Ripped) return false;
        if (c.ripping) return false;
        if (!c.Has(0)) return false;
        int s = Mathf.Abs(seed); /* ? */
        if (!inHands)
        {
            if (!GillaRip.Ready) return false;
        }
        using (RipMark.Auto())
        {
            Stopwatch clock = Stopwatch.StartNew();
            Vector3 going;
            Rigidbody rb;
            if (c.TryGetComponent<Rigidbody>(out rb) && !rb.isKinematic)
            {
                going = rb.linearVelocity;
            }
            else
            {
                going = Vector3.zero; /* ? */
            }
            Vector3 plusX = c.PlusX();
            Vector3 push = plusX * 1.6f;
            Vector3 plusVel = going + push + UnityEngine.Random.insideUnitSphere * 0.5f; /* ? */
            GillaCorpse plus = c.MakeHalf(1, s % 3, plusVel);
            Vector3 minusVel = going - push + UnityEngine.Random.insideUnitSphere * 0.5f; /* ? */
            GillaCorpse minus = c.MakeHalf(-1, s % 3, minusVel);
            c.Apart(plus, minus, plusX);
            if (inHands)
            {
                Take(by, true, plusInRight ? plus : minus, rotRight);
                Take(by, false, plusInRight ? minus : plus, rotLeft);
            }
            LastRipMs = (float)clock.ElapsedMilliseconds;
        }
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void FreshRips()
    {
        blastRips.Clear();
    }

    private static bool PileUpRip()
    {
        GillaModeRound round = GillaModeRound.Current;
        if (round != null)
        {
            if (round.Live && (int)round.Phase == 1)
            {
                return true;
            }
        }
        float now = Time.time;
        blastRips.RemoveAll(t => PileUpRip_b__133_0(now, t));
        if (blastRips.Count > 2)
        {
            return false;
        }
        blastRips.Add(now);
        return true;
    }

    public static void RipSoon(int id, int seed)
    {
        GillaCorpse c = Find(id);
        if (c == null) return;
        if (c.Owner != c.Id) return;
        if (c.Ripped) return;
        if (c.ripping) return;
        if (!c.Has(0)) return;
        seed = Mathf.Abs(seed) % 3; /* ? */
        if (!GillaRip.Ready) return;
        if (!PileUpRip()) return;
        Stopwatch clock = Stopwatch.StartNew();
        Vector3 going;
        Rigidbody rb;
        if (c.TryGetComponent<Rigidbody>(out rb) && !rb.isKinematic)
        {
            going = rb.linearVelocity;
        }
        else
        {
            going = Vector3.zero; /* ? */
        }
        Vector3 plusX = c.PlusX();
        c.ripping = true;
        Rigidbody[] bodies = c.GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = true;
        }
        GillaCorpse plus;
        using (RipMark.Auto())
        {
            Vector3 vel = going + plusX * 1.6f + Vector3.up * 0.5f;
            plus = c.MakeHalf(1, seed, vel);
        }
        List<ValueTuple<Rigidbody, Vector3, Vector3>> kept = plus.KeepBack();
        float first = (float)clock.ElapsedMilliseconds;
        // The other half is made a moment later (GillaWounds.Later), then the two are pulled apart.
        GillaWounds.Later(() => RipSoon_b__134_0(c, plus, clock, seed, going, plusX, kept, first));
    }

    private void Apart(GillaCorpse plus, GillaCorpse minus, Vector3 plusX)
    {
        Vector3 center;
        if (box != null)
        {
            center = box.bounds.center;
        }
        else
        {
            center = transform.position;
        }

        Collider[] plusCols = plus.GetComponentsInChildren<Collider>();
        for (int i = 0; i < plusCols.Length; i++)
        {
            Collider[] minusCols = minus.GetComponentsInChildren<Collider>();
            for (int k = 0; k < minusCols.Length; k++)
            {
                Physics.IgnoreCollision(plusCols[i], minusCols[k]);
            }
        }
        Hollow(plus, minus); /* ? */

        float size = 1f;
        if (box != null)
        {
            size = box.bounds.extents.y + box.bounds.extents.y;
        }
        Transform plusT = plus.transform;
        Transform minusT = minus.transform;
        Vector3 up = Vector3.up;
        float scale = transform.lossyScale.x; /* ? */
        GillaStrands.Between(plusT, minusT, center, plusX, up, scale, size * 0.09f, 30, size * 0.34f);
        GillaSplatter.Around(center, 1.6f, null /* ? */);
        Sfx.Play(tearSounds, center, 1f, 0.62f, 0.72f);
        Sfx.Play(tearSounds, center, 0.9f, 0.85f, 1f);
        GillaGoreAssets gore = Resources.Load<GillaGoreAssets>("GillaGore");
        if (gore != null)
        {
            Sfx.Play(tearSounds /* ? */, center, 0.8f, 0.8f, 0.9f);
        }
        if (!(blood != null))
        {
            return;
        }
        Blood.Burst(center, Vector3.up, blood, 1.6f);
        ValueTuple<GillaCorpse, Vector3>[] sides = new ValueTuple<GillaCorpse, Vector3>[2];
        sides[0] = new ValueTuple<GillaCorpse, Vector3>(plus, -plusX);
        sides[1] = new ValueTuple<GillaCorpse, Vector3>(minus, plusX);
        for (int i = 0; i < sides.Length; i++)
        {
            GillaCorpse half = sides[i].Item1;
            Vector3 way = sides[i].Item2;
            GameObject anchorGo = new GameObject("BloodAnchor");
            Transform anchor = anchorGo.transform;
            anchor.SetParent(half.transform, false);
            anchor.position = center;
            Vector3 dir = (way + Vector3.up * 0.5f).normalized;
            Blood.Spray(anchor, dir, blood, 3.5f, 90, 5, 1f);
        }
    }

    private List<ValueTuple<Rigidbody, Vector3, Vector3>> KeepBack()
    {
        keptBack = true;
        List<ValueTuple<Rigidbody, Vector3, Vector3>> kept = new List<ValueTuple<Rigidbody, Vector3, Vector3>>();
        Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody rb = bodies[i];
            kept.Add(new ValueTuple<Rigidbody, Vector3, Vector3>(rb, rb.linearVelocity, rb.angularVelocity));
            rb.isKinematic = true;
        }
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
        return kept;
    }

    private void LetGoOn(List<ValueTuple<Rigidbody, Vector3, Vector3>> kept)
    {
        keptBack = false;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
        }
        foreach (ValueTuple<Rigidbody, Vector3, Vector3> k in kept)
        {
            if (k.Item1 == null) continue;
            k.Item1.isKinematic = false;
            k.Item1.linearVelocity = k.Item2;
            k.Item1.angularVelocity = k.Item3;
        }
    }
}
