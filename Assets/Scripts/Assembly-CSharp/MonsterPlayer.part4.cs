// Reconstructed from the IL2CPP build (decompiled, not the original source).
// NOTE: CS0122 errors on NetworkApe.drawn/owner/ownerRef, GillaCorpse.PlusX() and GillaDummy.hits
// cannot be fixed from this file; those members must be made internal/public in their own files.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Fusion;

public partial class MonsterPlayer : MonoBehaviour
{
    private Vector3 Shove(NetworkApe who, Vector3 swing, float size)
    {
        Vector3 flat = Vector3.ProjectOnPlane(swing, Vector3.up);
        Vector3 away = Vector3.ProjectOnPlane(who.head.position - head.position, Vector3.up);
        Vector3 dir;
        if (flat.sqrMagnitude > swing.sqrMagnitude * 0.1f || away.sqrMagnitude <= 0.0001f)
        {
            dir = flat.normalized;
        }
        else
        {
            dir = away.normalized;
        }
        float speed = Mathf.Clamp(swing.magnitude / Mathf.Max(size, 0.1f), punchShoveLeast, punchShoveMost);
        Vector3 going;
        if (who.drawn == null || who.DownHere)
        {
            going = Vector3.zero;
        }
        else
        {
            going = Vector3.ProjectOnPlane(who.drawn.going, Vector3.up);
        }
        Vector3 v = Vector3.ClampMagnitude(dir * speed + going, punchShoveLimit);
        return v + Vector3.up * 0.5f;
    }

    private Vector3 Swung(Vector3 swing, float dt)
    {
        swungFor += dt;
        if (swungFor > punchSwingLasts || swing.sqrMagnitude >= swung.sqrMagnitude)
        {
            swung = swing;
            swungFor = 0f;
            return swing;
        }
        return swung;
    }

    private void HeldDied()
    {
        if (held.drawn != null)
        {
            held.drawn.Take(Holding(heldRight), heldDeadFor);
        }
        ExpectBody(held.owner, heldRight);
        held = null;
        ripping = false;
    }

    private void ExpectBody(int id, bool right)
    {
        int i = right ? 1 : 0;
        deadBody[i] = id;
        deadSince[i] = Time.time;
    }

    private void AwaitBody()
    {
        for (int i = 0; i < 2; i++)
        {
            if (deadBody[i] < 0)
            {
                continue;
            }
            bool grip = i == 1 ? gripR : gripL;
            if (!grip)
            {
                deadBody[i] = -1;
                continue;
            }
            if (deadSince[i] + heldDeadFor < Time.time)
            {
                deadBody[i] = -1;
                continue;
            }
            if (bodyOf[i] >= 0)
            {
                deadBody[i] = -1;
                continue;
            }
            GillaCorpse corpse = GillaCorpse.Find(deadBody[i]);
            if (corpse != null && corpse.Has(0) && !corpse.IsHalf && deadSince[i] <= corpse.MadeAt)
            {
                HoldBody(corpse, 0, i == 1, false);
                deadBody[i] = -1;
            }
        }
    }

    private void RipHeld()
    {
        int id = held.owner;
        NetworkApe local = NetworkApe.Local;
        Vector3 r = rightFollower.position;
        Vector3 l = leftFollower.position;
        GillaWounds wounds = GillaWounds.On(held.tearing);
        Vector3 plusX = wounds.PlusX();
        Quaternion rot = held.tearing.transform.rotation;
        NetworkApe ape = held;
        PlayerRef by = local != null ? local.ownerRef : PlayerRef.None;
        float dot = Vector3.Dot(r - l, plusX);
        int seed = GillaRip.PickSeed();
        NetworkBool plusInRight = dot > 0f;
        Quaternion rotRight = rot * Quaternion.Inverse(rightFollower.rotation);
        Quaternion rotLeft = rot * Quaternion.Inverse(leftFollower.rotation);
        ape.RPC_Rip(by, (byte)seed, plusInRight, rotRight, rotLeft);
        HalvesInHands(id, dot > 0f);
        ripping = false;
        HeldDied();
        deadBody[0] = -1;
        deadBody[1] = -1;
    }

    private void HalvesInHands(int id, bool plusInRight)
    {
        Buzz(XRNode.LeftHand, 1f, 0.5f);
        Buzz(XRNode.RightHand, 1f, 0.5f);
        for (int i = 0; i < 2; i++)
        {
            bool first = i == 0;
            int half = GillaCorpse.HalfId(id, first != plusInRight ? 1 : -1);
            if (GillaCorpse.Find(half) == null)
            {
                half = -1;
            }
            bodyOf[i] = half;
            bodyPart[i] = 0;
        }
    }

    private void RipBody(GillaCorpse body)
    {
        Vector3 r = rightFollower.position;
        Vector3 l = leftFollower.position;
        Vector3 plusX = body.PlusX();
        int seed = GillaRip.PickSeed();
        Quaternion rot = body.transform.rotation;
        Quaternion rotRight = rot * Quaternion.Inverse(rightFollower.rotation);
        Quaternion rotLeft = rot * Quaternion.Inverse(leftFollower.rotation);
        int id = body.Id;
        NetworkApe local = NetworkApe.Local;
        float dot = Vector3.Dot(r - l, plusX);
        if (local != null && local.Object != null && local.Object.Runner && local.Object.Runner.Exists(local.Object))
        {
            local.RPC_RipCorpse(id, (byte)seed, dot > 0f, rotRight, rotLeft);
        }
        else
        {
            GillaCorpse.RipIt(PlayerRef.None, id, (byte)seed, dot > 0f, rotRight, rotLeft, true);
        }
        HalvesInHands(id, dot > 0f);
    }

    private Transform Holding(bool right)
    {
        Transform t = null;
        if (worn != null)
        {
            t = worn.Grip(right);
        }
        if (t == null)
        {
            t = right ? rightFollower : leftFollower;
        }
        return t;
    }

    private void PickUp(bool right, float reach)
    {
        HandPoints(right, touching);
        NetworkApe ape = OnSomeone(touching, skinTouch * Mathf.Max(transform.localScale.x, 0.4f));
        if (ape == null)
        {
            TakeDummy(right, touching, reach);
            return;
        }
        if (NetworkApe.Local == null)
        {
            return;
        }
        Vector3 hand = Holding(right).position;
        Vector3 off = ape.head.position - hand;
        ape.RPC_Grab(NetworkApe.Local.ownerRef, right, off);
        if (ape.drawn != null)
        {
            ape.drawn.Take(Holding(right), off);
        }
        held = ape;
        heldRight = right;
        heldSince = Time.time;
        swung = Vector3.zero;
        Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.8f, 0.15f);
    }

    private void Drop(Vector3 velocity)
    {
        if (heldDummy != null)
        {
            DropDummy(velocity);
        }
        if (held != null && held.Object != null && held.Object.Runner && held.Object.Runner.Exists(held.Object))
        {
            Vector3 v = Vector3.ClampMagnitude(velocity, hardestThrow);
            held.RPC_Release(held.head.position, v);
            if (held.drawn != null)
            {
                held.drawn.LetGo(held.head.position, v);
            }
            punchIn = Mathf.Max(punchIn, punchAfterThrow);
        }
        held = null;
        ripping = false;
    }

    private void DropBoth(Vector3 velocity)
    {
        Drop(velocity);
        AsSecond(() => DropBoth_b__0(velocity));
    }

    private bool HasSecond
    {
        get
        {
            if (second != null)
            {
                return true;
            }
            return secondDummy != null;
        }
    }

    private void Swap()
    {
        NetworkApe a = held;
        held = second;
        second = a;
        GillaDummy d = heldDummy;
        heldDummy = secondDummy;
        secondDummy = d;
        bool r = heldRight;
        float since = heldSince;
        heldRight = secondRight;
        secondRight = r;
        heldSince = secondSince;
        secondSince = since;
    }

    private void AsSecond(Action act)
    {
        Swap();
        act();
        Swap();
    }

    private bool TakeSecond(bool right, float reach)
    {
        if (!HasSecond)
        {
            if (bodyOf[right ? 1 : 0] < 0 && !SlabIn(right))
            {
                AsSecond(() => TakeSecond_b__0(right, reach));
                return HasSecond;
            }
        }
        return false;
    }

    private void Both(float reach)
    {
        Bodies(reach, false);
        InThisHand();
        AsSecond(InThisHand);
        if (held == null && heldDummy == null)
        {
            Swap();
        }
    }

    private void InThisHand()
    {
        bool grip = heldRight ? gripR : gripL;
        Vector3 throwV = heldRight ? throwR : throwL;
        if (heldDummy != null)
        {
            if (!heldDummy.HeldBy(You, heldRight))
            {
                heldDummy = null;
                return;
            }
            if (grip)
            {
                GillaDummy d = heldDummy;
                Vector3 handPos = Holding(heldRight).position;
                Transform target = d.head != null ? d.head : Holding(heldRight);
                RaycastHit hit;
                Vector3 going;
                if (SlamHit(handPos, target.position, out hit, out going) /* ? */)
                {
                    Vector3 at = hit.point;
                    Vector3 normal = hit.normal;
                    if (d.tearing != null && d.tearing.bloodMaterial != null)
                    {
                        Blood.Burst(at + normal * 0.05f, normal, d.tearing.bloodMaterial, 0.8f);
                    }
                    d.Blasted(Vector3.up * 0.8f - going * 0.2f, 0, false);
                    Buzz(XRNode.LeftHand, 1f, 0.3f);
                    Buzz(XRNode.RightHand, 1f, 0.3f);
                    DummyDied(d);
                }
                return;
            }
            DropDummy(throwV);
            return;
        }
        if (held == null)
        {
            return;
        }
        if (held.Object == null || !held.Object.Runner || !held.Object.Runner.Exists(held.Object))
        {
            held = null;
            return;
        }
        if (grip)
        {
            if (Time.time - heldSince > neverHeldAfter && !held.Busy)
            {
                Drop(Vector3.zero);
                return;
            }
            if (!held.Busy)
            {
                return;
            }
            Vector3 handPos = Holding(heldRight).position;
            RaycastHit hit;
            Vector3 going;
            if (SlamHit(handPos, held.head.position, out hit, out going) /* ? */)
            {
                held.RPC_Slam(hit.point, hit.normal, Vector3.up * 0.8f - going * 0.2f);
                Buzz(XRNode.LeftHand, 1f, 0.3f);
                Buzz(XRNode.RightHand, 1f, 0.3f);
                HeldDied();
            }
            return;
        }
        Drop(throwV);
    }

    private Transform HoldPoint(bool right)
    {
        Transform t = null;
        if (worn != null)
        {
            t = worn.Grip(right);
        }
        if (t == null)
        {
            t = right ? rightFollower : leftFollower;
        }
        return t;
    }

    private static PlayerRef You
    {
        get
        {
            if (NetworkApe.Local != null)
            {
                return NetworkApe.Local.ownerRef;
            }
            return PlayerRef.None;
        }
    }

    private void TakeDummy(bool right, List<Vector3> touching, float reach)
    {
        float dist = 0f;
        Vector3 point = touching[0];
        if (Local != null)
        {
            if (Walled(Local.head.position, point))
            {
                return;
            }
        }
        GillaDummy d = GillaDummy.Nearest(point, reach, out dist);
        if (d == null)
        {
            return;
        }
        Vector3 hand = Holding(right).position;
        if (!OnBody(d.tearing, null, null, touching, skinTouch * Mathf.Max(transform.localScale.x, 0.4f)))
        {
            return;
        }
        PlayerRef by = You;
        Vector3 off;
        if (d.head != null)
        {
            off = d.head.position - hand;
        }
        else
        {
            off = Vector3.up * 0.36f;
        }
        d.Grab(by, right, off);
        heldDummy = d;
        heldRight = right;
        heldSince = Time.time;
        swung = Vector3.zero;
        Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.8f, 0.15f);
    }

    private void HoldingDummy(float reach)
    {
        GillaDummy d = heldDummy;
        if (d == null || !d.HeldBy(You, heldRight))
        {
            heldDummy = null;
            ripping = false;
            return;
        }
        if (!(heldRight ? gripR : gripL))
        {
            Bodies(reach, false);
            DropDummy(heldRight ? throwR : throwL);
            return;
        }
        if (!ripping)
        {
            if (SlammingDummy(d))
            {
                return;
            }
            if (!ripping && PunchingDummy(d))
            {
                return;
            }
        }
        bool otherGrip = heldRight ? gripL : gripR;
        bool otherWas = heldRight ? wasGripL : wasGripR;
        Transform otherHand = heldRight ? leftFollower : rightFollower;
        Transform heldHand = heldRight ? rightFollower : leftFollower;
        if (!otherGrip || otherWas)
        {
            Bodies(reach, !ripping);
            if (!otherGrip)
            {
                ripping = false;
                return;
            }
        }
        else
        {
            bool flag;
            bool take = bodyOf[heldRight ? 0 : 1] >= 0;
            if (!take)
            {
                float partDist;
                bool near = d.PartNear(otherHand.position, reach, out ripPart, out partDist);
                float toMid = (otherHand.position - d.Middle).magnitude;
                if (toMid >= reach || d.hits < punchesBeforeRip)
                {
                    ripWhole = false;
                    if (!near)
                    {
                        take = true;
                    }
                }
                else
                {
                    ripWhole = !near || toMid < partDist * 1.5f;
                }
            }
            if (!take)
            {
                ripping = true;
                ripStart = (otherHand.position - heldHand.position).magnitude;
                ripStartReal = ReallyApart;
                Buzz(heldRight ? XRNode.LeftHand : XRNode.RightHand, ripWhole ? 0.8f : 0.4f, ripWhole ? 0.15f : 0.08f);
                flag = false;
            }
            else
            {
                if (TakeSecond(!heldRight, reach))
                {
                    return;
                }
                flag = !ripping;
            }
            Bodies(reach, flag);
        }
        if (ripping && PulledApart(ripStart, ripStartReal))
        {
            if (!ripWhole)
            {
                d.Tear(ripPart);
                Buzz(XRNode.LeftHand, 1f, 0.3f);
                Buzz(XRNode.RightHand, 1f, 0.3f);
                ripping = false;
                if (!d.Alive)
                {
                    DummyDied(d);
                }
            }
            else
            {
                RipDummy(d);
            }
        }
    }

    private bool PunchingDummy(GillaDummy d)
    {
        float scale = transform.localScale.x;
        bool right = heldRight;
        if (!Fist(!right))
        {
            swung = Vector3.zero;
            return false;
        }
        Vector3 relative = right ? velL - velR : velR - velL;
        Vector3 realRel = right ? realVelL - realVelR : realVelR - realVelL;
        Vector3 rel = realRel.sqrMagnitude > relative.sqrMagnitude ? realRel : relative;
        Vector3 s = Swung(rel, Time.deltaTime);
        if (punchIn <= 0f && scale * punchSwing <= s.magnitude)
        {
            if (d.tearing == null)
            {
                return false;
            }
            Transform fistHand = right ? leftFollower : rightFollower;
            Transform holdHand = right ? rightFollower : leftFollower;
            Vector3 fistOff;
            if (worn != null)
            {
                fistOff = worn.FistFromHand(!right);
            }
            else
            {
                fistOff = Vector3.zero;
            }
            GillaWounds wounds = GillaWounds.On(d.tearing);
            Vector3 at = holdHand.position + RealHand(!right) - RealHand(right) + fistOff;
            if (!wounds.Hit(at, scale * punchReach, s, out var part, out var point))
            {
                if (!wounds.Hit(fistHand.position + fistOff, scale * punchReach, s, out part, out point))
                {
                    return false;
                }
            }
            punchIn = punchEvery;
            swung = Vector3.zero;
            d.Punch(part, point);
            Buzz(right ? XRNode.LeftHand : XRNode.RightHand, 1f, 0.2f);
            Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.5f, 0.1f);
            if (d.Alive)
            {
                return false;
            }
            DummyDied(d);
            return true;
        }
        return false;
    }

    private void DummyDied(GillaDummy d)
    {
        ExpectBody(d.BodyId, heldRight);
        heldDummy = null;
        ripping = false;
    }

    private void RipDummy(GillaDummy d)
    {
        int id = d.BodyId;
        Vector3 r = rightFollower.position;
        Vector3 l = leftFollower.position;
        GillaWounds wounds = GillaWounds.On(d.tearing);
        Vector3 plusX = wounds.PlusX();
        Quaternion rot = d.tearing.transform.rotation;
        PlayerRef by = You;
        int seed = GillaRip.PickSeed();
        Quaternion rotRight = rot * Quaternion.Inverse(rightFollower.rotation);
        bool plusInRight = Vector3.Dot(r - l, plusX) > 0f;
        Quaternion rotLeft = rot * Quaternion.Inverse(leftFollower.rotation);
        d.Rip(by, (byte)seed, plusInRight, rotRight, rotLeft);
        HalvesInHands(id, plusInRight);
        heldDummy = null;
        ripping = false;
        deadBody[0] = -1;
        deadBody[1] = -1;
    }

    private void DropDummy(Vector3 velocity)
    {
        if (heldDummy != null && heldDummy.head != null)
        {
            heldDummy.Release(heldDummy.head.position, Vector3.ClampMagnitude(velocity, hardestThrow));
        }
        heldDummy = null;
        ripping = false;
    }
}
