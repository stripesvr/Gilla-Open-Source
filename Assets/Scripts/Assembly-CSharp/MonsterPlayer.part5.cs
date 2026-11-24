using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using Fusion;

public partial class MonsterPlayer : MonoBehaviour
{
    private void Bodies(float reach, bool grab)
    {
        GillaCorpse corpse = null;
        Transform link = null;
        int part = 0;
        for (int i = 0; i < 2; i++)
        {
            bool right = i == 1;
            bool grip = right ? gripR : gripL;
            bool wasGrip = right ? wasGripR : wasGripL;
            if (!grip && wasGrip)
            {
                if (bodyOf[i] >= 0)
                {
                    LetGoBody(right, right ? throwR : throwL);
                }
                if (ripBody >= 0 && ripBodyRight == right)
                {
                    ripBody = -1;
                }
            }
            if (!(grab && grip && !wasGrip)) continue;
            if (bodyOf[i] >= 0) continue;
            if ((held != null && heldRight == right) || SlabIn(right)) continue;
            Transform hand = right ? rightFollower : leftFollower;
            if (!GillaCorpse.Nearest(hand.position, reach, out corpse, out part)) continue;

            bool doRip = false;
            bool whole = false;
            int other = 1 - i;
            if (bodyOf[other] == corpse.Id && bodyPart[other] == 0)
            {
                whole = corpse.RipsHere(hand.position, reach);
                if (whole)
                {
                    doRip = true;
                }
                else
                {
                    if (part == 0)
                    {
                        part = corpse.NearestOn(hand.position, reach);
                    }
                    if (part != 0 && !corpse.IsOff(part))
                    {
                        whole = false;
                        doRip = true;
                    }
                }
            }
            if (doRip)
            {
                ripBodyRight = right;
                ripBody = corpse.Id;
                ripBodyPart = whole ? 0 : part;
                ripBodyStart = Vector3.Distance(leftFollower.position, rightFollower.position);
                ripBodyStartReal = ReallyApart;
                Buzz(right ? XRNode.RightHand : XRNode.LeftHand, whole ? 0.8f : 0.4f, whole ? 0.15f : 0.08f);
                continue;
            }
            HoldBody(corpse, part, right, false);
        }

        if (ripBody < 0)
        {
            return;
        }
        GillaCorpse c = GillaCorpse.Find(ripBody);
        if (c == null || bodyOf[ripBodyRight ? 0 : 1] != ripBody || !c.Has(ripBodyPart) || c.IsOff(ripBodyPart))
        {
            ripBody = -1;
            return;
        }
        if (!PulledApart(ripBodyStart, ripBodyStartReal))
        {
            return;
        }
        if (ripBodyPart == 0)
        {
            ripBody = -1;
            RipBody(c);
            return;
        }
        Transform handT = ripBodyRight ? rightFollower : leftFollower;
        int linkIndex = c.LinkOn(ripBodyPart, handT.position, out link);
        (Vector3, Quaternion) inHand = InHand(ripBodyRight, link, false);
        int code = ripBodyPart | (linkIndex << 4);
        NetworkApe local = NetworkApe.Local;
        if (local != null && local.Object != null && local.Object.HasStateAuthority /* ? */)
        {
            local.RPC_TearCorpse(c.Id, (byte)code, ripBodyRight, inHand.Item2);
        }
        else
        {
            GillaCorpse.TearOff(PlayerRef.None, ripBodyRight, c.Id, code, inHand.Item2);
        }
        int idx = ripBodyRight ? 1 : 0;
        bodyOf[idx] = c.Id;
        bodyPart[idx] = ripBodyPart;
        ripBody = -1;
        Buzz(XRNode.LeftHand, 1f, 0.3f);
        Buzz(XRNode.RightHand, 1f, 0.3f);
    }

    public void Await(int body, int part, bool right)
    {
        awaitBody = body;
        awaitPart = part;
        awaitRight = right;
        awaitUntil = Time.time + 1f;
    }

    private void AwaitTorn()
    {
        if (awaitBody >= 0)
        {
            bool grip = awaitRight ? gripR : gripL;
            if (grip && Time.time <= awaitUntil)
            {
                GillaCorpse c = GillaCorpse.Find(awaitBody);
                if (c == null) return;
                if (!c.IsOff(awaitPart)) return;
                if (!c.Has(awaitPart)) return;
                if (bodyOf[awaitRight ? 1 : 0] >= 0) return;
                HoldBody(c, awaitPart, awaitRight, true);
            }
            awaitBody = -1;
        }
    }

    private void TellAtRest()
    {
        if (nextAtRest <= Time.time)
        {
            nextAtRest = Time.time + 0.5f;
            NetworkApe you = NetworkApe.Local;
            if (you == null) return;
            if (you.Object == null) return;
            NetworkObject obj = you.Object;
            if (!obj.Runner) return;
            if (!obj.Runner.IsRunning /* ? */) return;
            MethodInfo newlyAtRest = typeof(GillaCorpse).GetMethod("NewlyAtRest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); /* ? */
            foreach (GillaCorpse c in GillaCorpse.All)
            {
                if (c == null) continue;
                for (int i = 0; i < 4; i++)
                {
                    object[] args = new object[] { i, null, null };
                    if (newlyAtRest != null && (bool)newlyAtRest.Invoke(c, args)) /* ? */
                    {
                        Vector3 at = (Vector3)args[1];
                        Quaternion rot = (Quaternion)args[2];
                        you.RPC_PlaceCorpse(c.Id, (byte)i, at, rot);
                        TornArm arm = c.ArmOf(i);
                        if (arm != null)
                        {
                            for (int k = 1; k < 3; k++)
                            {
                                you.RPC_PlaceCorpse(c.Id, (byte)((k << 4) | i), arm.Link(k).position, arm.Link(k).rotation);
                            }
                        }
                    }
                }
            }
            TellSlabsAtRest(you);
        }
    }

    private (Vector3, Quaternion) InHand(bool right, Transform thing, bool byMiddle)
    {
        Transform hand = right ? rightFollower : leftFollower;
        Quaternion inv = Quaternion.Inverse(hand.rotation);
        Vector3 offset;
        if (byMiddle)
        {
            if (thing.TryGetComponent<Renderer>(out Renderer renderer))
            {
                offset = thing.position - renderer.bounds.center;
            }
            else
            {
                offset = Vector3.zero;
            }
        }
        else
        {
            offset = thing.position - hand.position;
        }
        return (inv * offset, inv * thing.rotation);
    }

    private void HoldBody(GillaCorpse c, int part, bool right, bool byMiddle)
    {
        float d = 0f;
        int part0 = c.IsOff(part) ? part : 0;
        TornArm arm = c.ArmOf(part0);
        int linkIndex;
        if (arm != null)
        {
            Transform hand = right ? rightFollower : leftFollower;
            linkIndex = arm.Nearest(hand.position, out d);
        }
        else
        {
            linkIndex = 0;
        }
        Transform thing = arm != null ? arm.Link(linkIndex) : c.Thing(part);
        (Vector3, Quaternion) inHand = InHand(right, thing, byMiddle && arm == null);
        Vector3 pos = inHand.Item1;
        if (byMiddle && arm != null)
        {
            pos = -(inHand.Item2 * arm.MiddleOf(linkIndex));
        }
        int code = part0 | (linkIndex << 4);
        NetworkApe local = NetworkApe.Local;
        if (local != null && local.Object != null && local.Object.Runner && local.Object.Runner.IsRunning /* ? */)
        {
            local.RPC_HoldCorpse(c.Id, (byte)code, right, pos, inHand.Item2);
        }
        else
        {
            GillaCorpse.HoldIt(PlayerRef.None, right, c.Id, code, pos, inHand.Item2);
        }
        int me = right ? 1 : 0;
        int other = me ^ 1;
        if (bodyOf[other] == c.Id && bodyPart[other] == part0)
        {
            bodyOf[other] = -1;
        }
        bodyOf[me] = c.Id;
        bodyPart[me] = part0;
        Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.6f, 0.1f);
    }

    private void LetGoBody(bool right, Vector3 velocity)
    {
        int idx = right ? 1 : 0;
        GillaCorpse c = GillaCorpse.Find(bodyOf[idx]);
        int part = bodyPart[idx];
        bodyOf[idx] = -1;
        if (c == null) return;
        if (!c.Has(part)) return;
        TornArm arm = c.ArmOf(part);
        Transform thing = arm != null ? arm.HeldLink : c.Thing(part);
        velocity = Vector3.ClampMagnitude(velocity, maxBodyThrow);
        NetworkApe local = NetworkApe.Local;
        if (local != null && local.Object != null && local.Object.Runner && local.Object.Runner.IsRunning /* ? */)
        {
            local.RPC_LetGoCorpse(right, thing.position, thing.rotation, velocity, Vector3.zero);
            return;
        }
        GillaCorpse.LetGo(PlayerRef.None, right, thing.position, thing.rotation, velocity, Vector3.zero);
    }

    public bool Ripping
    {
        get { return ripping; }
    }

    public bool RippingWhole
    {
        get
        {
            if (ripping)
            {
                return ripWhole;
            }
            return false;
        }
    }

    public (int, int) BodyIn(bool right)
    {
        int idx = right ? 1 : 0;
        return (bodyOf[idx], bodyPart[idx]);
    }

    private void Killing(float reach, float dt)
    {
        float d = 0f;
        killCooldown -= dt;
        if (killCooldown > 0f)
        {
            return;
        }
        if (reach <= 0.3f)
        {
            reach = 0.3f;
        }
        NetworkApe ape = null;
        if (gripL && !wasGripL)
        {
            ape = NearestBody(leftFollower.position, reach);
        }
        if (ape == null && gripR && !wasGripR)
        {
            ape = NearestBody(rightFollower.position, reach);
        }
        if (ape == null)
        {
            ape = NearestBody(head.position, reach * 0.7f);
        }
        if (ape == null)
        {
            GillaDummy dummy = GillaDummy.Nearest(head.position, reach * 0.7f, out d);
            if (dummy == null && gripL && !wasGripL)
            {
                dummy = GillaDummy.Nearest(leftFollower.position, reach, out d);
            }
            if (dummy == null && gripR && !wasGripR)
            {
                dummy = GillaDummy.Nearest(rightFollower.position, reach, out d);
            }
            if (dummy != null)
            {
                dummy.Blasted(Vector3.zero, 0, false);
                killCooldown = 1f;
            }
            return;
        }
        ape.RPC_Kill();
        killCooldown = 1f;
        Buzz(XRNode.LeftHand, 1f, 0.3f);
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.isValid)
        {
            device.SendHapticImpulse(0, 1f, 0.3f);
        }
    }

    private static NetworkApe Nearest(Vector3 at, float reach)
    {
        NetworkApe best = null;
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null) continue;
            if (!ape.IsLocal && !ape.IsMonster && !ape.Busy && ape.head != null)
            {
                float dist = Vector3.Distance(ape.head.position, at);
                if (dist < reach)
                {
                    reach = dist;
                    best = ape;
                }
            }
        }
        return best;
    }

    private static bool CanTouch(NetworkApe a)
    {
        if (a == null) return false;
        if (a.IsLocal || a.IsMonster) return false;
        if (a.head == null) return false;
        GillaModeRound round = GillaModeRound.Current;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        object drawn = typeof(NetworkApe).GetField("drawn", flags)?.GetValue(a); /* ? */
        if (drawn != null && drawn is UnityEngine.Object drawnObj ? drawnObj != null : drawn != null)
        {
            object inHandVal = drawn.GetType().GetProperty("InHand", flags)?.GetValue(drawn); /* ? */
            if (inHandVal is bool inHandFlag && inHandFlag)
            {
                return false;
            }
        }
        object ownerRefVal = typeof(NetworkApe).GetField("ownerRef", flags)?.GetValue(a); /* ? */
        if (round != null && ownerRefVal is PlayerRef ownerRef && round.IsOut(ownerRef))
        {
            return false;
        }
        object ownerVal = typeof(NetworkApe).GetField("owner", flags)?.GetValue(a); /* ? */
        GillaCorpse c = ownerVal is int owner ? GillaCorpse.Find(owner) : null;
        if (c != null && c.Has(0))
        {
            return 3f <= Time.time - c.MadeAt;
        }
        return true;
    }

    private static NetworkApe OnSomeone(List<Vector3> touching, float touch)
    {
        if (touching.Count == 0)
        {
            return null;
        }
        if (Local != null)
        {
            if (Walled(Local.head.position, touching[0]))
            {
                return null;
            }
        }
        NetworkApe best = null;
        float bestSqr = float.MaxValue;
        foreach (NetworkApe a in NetworkApe.All)
        {
            if (!CanTouch(a)) continue;
            if (!a.Busy || a.LetGoHere || a.DownHere)
            {
                float sqr = (a.head.position - touching[0]).sqrMagnitude;
                if (sqr <= 6.25f)
                {
                    if (OnBody(a.GetComponentInChildren<Dismemberment>() /* ? */, a.leftHand /* ? */, a.rightHand /* ? */, touching, touch) && sqr < bestSqr)
                    {
                        best = a;
                        bestSqr = sqr;
                    }
                }
            }
        }
        return best;
    }

    private static bool OnBody(Dismemberment body, Transform left, Transform right, List<Vector3> touching, float touch)
    {
        foreach (Vector3 p in touching)
        {
            if (left != null && Vector3.Distance(left.position, p) < touch)
            {
                return true;
            }
            if (right != null && Vector3.Distance(right.position, p) < touch)
            {
                return true;
            }
        }
        if (body == null)
        {
            return false;
        }
        float nearest = GillaSkin.Nearest(body, touching);
        if (nearest != float.PositiveInfinity)
        {
            return nearest < touch;
        }
        GillaWounds wounds = GillaWounds.On(body);
        if (wounds == null)
        {
            return false;
        }
        foreach (Vector3 p in touching)
        {
            if (wounds.Hit(p, touch, Vector3.zero, out GoreSection hitSection, out Vector3 hitAt))
            {
                return true;
            }
        }
        return false;
    }

    private void HandPoints(bool right, List<Vector3> into)
    {
        into.Clear();
        if (worn != null)
        {
            if (worn.Hand(right, out Vector3 a, out Vector3 b, out Vector3 c))
            {
                into.Add(b);
                into.Add(a);
                into.Add(c);
                return;
            }
        }
        Transform hand = right ? rightFollower : leftFollower;
        into.Add(hand.position);
    }
}
