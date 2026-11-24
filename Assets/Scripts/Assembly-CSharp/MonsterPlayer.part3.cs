using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public partial class MonsterPlayer
{
    private Vector3 RealHand(bool right)
    {
        if (right)
        {
            Vector3 pos = rightTarget.position;
            Quaternion rot = rightTarget.rotation;
            return pos + rot * locomotion.rightHandOffset;
        }
        else
        {
            Vector3 pos = leftTarget.position;
            Quaternion rot = leftTarget.rotation;
            return pos + rot * locomotion.leftHandOffset;
        }
    }

    private float ReallyApart
    {
        get
        {
            Vector3 l = RealHand(false);
            Vector3 r = RealHand(true);
            return (l - r).magnitude;
        }
    }

    private bool PulledApart(float drawnFrom, float reallyFrom)
    {
        Vector3 l = leftFollower.position;
        Vector3 r = rightFollower.position;
        float really = ReallyApart;
        float need = pullApart;
        float apart = (l - r).magnitude - drawnFrom;
        if (apart <= really - reallyFrom)
        {
            apart = really - reallyFrom;
        }
        return need * transform.localScale.x <= apart;
    }

    private void Grabbing(float reach)
    {
        SlabInHand();
        if (HasSecond)
        {
            Bodies(reach, false);
            InThisHand();
            AsSecond(new Action(InThisHand));
            if (held == null && heldDummy == null)
            {
                NetworkApe a = held;
                held = second;
                second = a;
                GillaDummy dm = heldDummy;
                heldDummy = secondDummy;
                secondDummy = dm;
                bool r = heldRight;
                float since = heldSince;
                heldRight = secondRight;
                secondRight = r;
                heldSince = secondSince;
                secondSince = since;
            }
            return;
        }

        if (heldDummy != null)
        {
            GrabbingDummy(reach);
            return;
        }

        if (held != null && held.Object != null && held.Object.Runner && held.Object.Runner.Exists(held.Object) /* ? */)
        {
            bool grip = heldRight ? gripR : gripL;
            bool release = false;
            Vector3 throwV = Vector3.zero;
            if (!grip)
            {
                Bodies(reach, false);
                throwV = heldRight ? throwR : throwL;
                release = true;
            }
            else if (Time.time - heldSince > neverHeldAfter && !held.Busy)
            {
                throwV = Vector3.zero;
                release = true;
            }

            if (release)
            {
                if (heldDummy != null)
                {
                    DropDummy(throwV);
                }
                if (held != null && held.Object != null && held.Object.Runner && held.Object.Runner.Exists(held.Object) /* ? */)
                {
                    throwV = Vector3.ClampMagnitude(throwV, hardestThrow);
                    held.RPC_Release(held.head.position, throwV);
                    if (held.Drawn /* ? */ != null)
                    {
                        held.Drawn /* ? */.LetGo(held.head.position, throwV);
                    }
                    punchIn = Mathf.Max(punchIn, punchAfterThrow);
                }
                held = null;
                ripping = false;
                return;
            }

            if (!ripping)
            {
                if (held.Busy && Slamming())
                {
                    return;
                }
                if (!ripping && Punching())
                {
                    return;
                }
            }

            bool otherGrip = heldRight ? gripL : gripR;
            bool otherWas = heldRight ? wasGripL : wasGripR;
            Transform otherHand = heldRight ? leftFollower : rightFollower;
            Transform holdHand = heldRight ? rightFollower : leftFollower;

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
                bool bodiesArg;
                bool takeSecond = false;
                if (bodyOf[heldRight ? 0 : 1] < 0)
                {
                    float partDist;
                    bool near = Part(held, otherHand.position, reach, out ripPart, out partDist);
                    float d = (otherHand.position - (held.head.position + held.MiddleFromEyes)).magnitude;
                    if (reach <= d || held.Punches /* ? */ < punchesBeforeRip)
                    {
                        ripWhole = false;
                        if (!near)
                        {
                            takeSecond = true;
                        }
                    }
                    else
                    {
                        ripWhole = !near || d < partDist * 1.5f;
                    }
                    if (!takeSecond)
                    {
                        ripping = true;
                        ripStart = (otherHand.position - holdHand.position).magnitude;
                        ripStartReal = ReallyApart;
                        Buzz(heldRight ? XRNode.LeftHand : XRNode.RightHand, ripWhole ? 0.8f : 0.4f, ripWhole ? 0.15f : 0.08f);
                    }
                }
                else
                {
                    takeSecond = true;
                }

                if (takeSecond)
                {
                    if (TakeSecond(!heldRight, reach))
                    {
                        return;
                    }
                    bodiesArg = !ripping;
                }
                else
                {
                    bodiesArg = false;
                }
                Bodies(reach, bodiesArg);
            }

            if (!ripping)
            {
                return;
            }
            if (!PulledApart(ripStart, ripStartReal))
            {
                return;
            }
            if (ripWhole)
            {
                RipHeld();
                return;
            }
            held.RPC_Tear((byte)ripPart);
            int owner = held.Owner.PlayerId /* ? */;
            awaitPart = GillaCorpse.Part(ripPart);
            awaitBody = owner;
            awaitRight = !heldRight;
            awaitUntil = Time.time + 1f;
            Buzz(XRNode.LeftHand, 1f, 0.3f);
            Buzz(XRNode.RightHand, 1f, 0.3f);
            ripping = false;
            if (ripPart != ApePart.Head)
            {
                return;
            }
            HeldDied();
            return;
        }

        held = null;
        ripping = false;
        if (gripL && !wasGripL)
        {
            closedL = Time.time;
        }
        if (gripR && !wasGripR)
        {
            closedR = Time.time;
        }
        if (gripL && Time.time - closedL <= graspFor)
        {
            if (bodyOf[0] < 0 && !SlabIn(false))
            {
                PickUp(false, reach);
            }
        }
        if (gripR && Time.time - closedR <= graspFor)
        {
            if (bodyOf[1] < 0 && !SlabIn(true))
            {
                if (held == null && heldDummy == null)
                {
                    PickUp(true, reach);
                }
                else
                {
                    TakeSecond(true, reach);
                }
            }
        }
        if (held == null && heldDummy == null)
        {
            TakeSlabs(reach);
        }
        Bodies(reach, held == null && heldDummy == null);
        if (held == null && heldDummy == null)
        {
            FreePunching();
        }
    }

    private void GrabbingDummy(float reach)
    {
        GillaDummy d = heldDummy;
        if (d == null || !d.HeldBy(You, heldRight))
        {
            heldDummy = null;
            ripping = false;
            return;
        }

        bool grip = heldRight ? gripR : gripL;
        if (!grip)
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
        Transform holdHand = heldRight ? rightFollower : leftFollower;

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
            bool bodiesArg;
            bool takeSecond = false;
            if (bodyOf[heldRight ? 0 : 1] < 0)
            {
                float partDist;
                bool near = d.PartNear(otherHand.position, reach, out ripPart, out partDist);
                float dist = (otherHand.position - d.Middle).magnitude;
                if (reach <= dist || d.Hits /* ? */ < punchesBeforeRip)
                {
                    ripWhole = false;
                    if (!near)
                    {
                        takeSecond = true;
                    }
                }
                else
                {
                    ripWhole = !near || dist < partDist * 1.5f /* ? */;
                }
                if (!takeSecond)
                {
                    ripping = true;
                    ripStart = (otherHand.position - holdHand.position).magnitude;
                    ripStartReal = ReallyApart;
                    Buzz(heldRight ? XRNode.LeftHand : XRNode.RightHand, ripWhole ? 0.8f : 0.4f, ripWhole ? 0.15f : 0.08f);
                }
            }
            else
            {
                takeSecond = true;
            }

            if (takeSecond)
            {
                if (TakeSecond(!heldRight, reach))
                {
                    return;
                }
                bodiesArg = !ripping;
            }
            else
            {
                bodiesArg = false;
            }
            Bodies(reach, bodiesArg);
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

    private void ShowPull()
    {
        float scale = pullApart * transform.localScale.x;
        bool done = false;
        if (ripping && (held != null || heldDummy != null))
        {
            Dismemberment tearing = held != null ? held.tearing : heldDummy.tearing;
            Transform holding = Holding(!heldRight);
            Transform otherHand = heldRight ? leftFollower : rightFollower;
            Transform holdHand = heldRight ? rightFollower : leftFollower;
            if (ripWhole)
            {
                GillaPull.MineMiddle(tearing, holding, otherHand, holdHand, scale);
            }
            else
            {
                GillaPull.Mine(tearing, null, ripPart, holding, otherHand, holdHand, scale);
            }
            done = true;
        }
        if (!done)
        {
            if (ripBody < 0)
            {
                return;
            }
            if (ripBodyPart == 0)
            {
                return;
            }
            GillaCorpse body = GillaCorpse.Find(ripBody);
            Transform holding = Holding(ripBodyRight);
            Transform a = ripBodyRight ? rightFollower : leftFollower;
            Transform b = ripBodyRight ? leftFollower : rightFollower;
            GillaPull.Mine(null, body, (ApePart)(ripBodyPart - 1), holding, a, b, scale);
        }

        float strain = GillaPull.MineStrain;
        if (strain < 0.05f)
        {
            return;
        }
        strain = strain * strain * 0.5f;
        Buzz(XRNode.LeftHand, strain, 0.05f);
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.isValid)
        {
            device.SendHapticImpulse(0, strain, 0.05f);
        }
    }

    private bool SlamHit(Vector3 middle, Vector3 eyes, out RaycastHit hit, out Vector3 swing)
    {
        hit = default(RaycastHit);
        Vector3 real = heldRight ? realVelR : realVelL;
        Vector3 vel = heldRight ? velR : velL;
        swing = real.sqrMagnitude <= vel.sqrMagnitude ? vel : real;
        float least = slamSwing;
        float speed = swing.magnitude;
        least *= transform.localScale.x;
        if (speed < least)
        {
            return false;
        }
        if (transform.localScale.x * SlamFastest < speed)
        {
            return false;
        }
        if (Time.time - heldSince < 0.2f)
        {
            return false;
        }
        float hr = heldReach;
        float size = ApeSize;
        float dt = Time.deltaTime;
        Vector3[] from = new Vector3[3];
        Transform hand = heldRight ? rightFollower : leftFollower;
        from[0] = hand.position;
        from[1] = middle;
        from[2] = eyes;
        Vector3 dir = swing / speed;
        float off = Mathf.Max(hr * size - 0.08f, 0f);
        for (int i = 0; i < from.Length; i++)
        {
            Vector3 origin = from[i] - dir * 0.1f;
            if (Physics.SphereCast(origin, 0.08f, dir, out hit, speed * dt * 1.5f + off + 0.1f, Floor, QueryTriggerInteraction.Ignore)
                && least <= Vector3.Dot(-hit.normal, swing))
            {
                return true;
            }
        }
        return false;
    }

    private bool Slamming()
    {
        Vector3 middle = Holding(heldRight).position;
        Vector3 eyes = held.head.position;
        RaycastHit hit;
        Vector3 swing;
        bool slammed = SlamHit(middle, eyes, out hit, out swing);
        if (slammed)
        {
            held.RPC_Slam(hit.point, hit.normal, Vector3.up * 0.8f - swing * 0.2f);
            Buzz(XRNode.LeftHand, 1f, 0.3f);
            Buzz(XRNode.RightHand, 1f, 0.3f);
            HeldDied();
        }
        return slammed;
    }

    private bool SlammingDummy(GillaDummy d)
    {
        Vector3 middle = Holding(heldRight).position;
        Transform eyesAt = d.head != null ? d.head : Holding(heldRight);
        Vector3 eyes = eyesAt.position;
        RaycastHit hit;
        Vector3 swing;
        bool slammed = SlamHit(middle, eyes, out hit, out swing);
        if (slammed)
        {
            if (d.tearing != null && d.tearing.bloodMaterial != null)
            {
                Blood.Burst(hit.point + hit.normal * 0.05f, hit.normal, d.tearing.bloodMaterial, 0.8f);
            }
            d.Blasted(Vector3.up * 0.8f - swing * 0.2f, 0, false);
            Buzz(XRNode.LeftHand, 1f, 0.3f);
            Buzz(XRNode.RightHand, 1f, 0.3f);
            DummyDied(d);
        }
        return slammed;
    }

    private bool Punching()
    {
        GoreSection section = GoreSection.Head;
        Vector3 point = Vector3.zero;
        float size = transform.localScale.x;
        bool right = heldRight;
        if (!Fist(!right))
        {
            swung = Vector3.zero;
            return false;
        }
        Vector3 v = right ? velL - velR : velR - velL;
        Vector3 rv = right ? realVelL - realVelR : realVelR - realVelL;
        Vector3 rel = v.sqrMagnitude < rv.sqrMagnitude ? rv : v;
        Vector3 swing = Swung(rel, Time.deltaTime);
        if (punchIn <= 0f && size * punchSwing <= swing.magnitude)
        {
            if (held.tearing == null)
            {
                return false;
            }
            Transform fistHand = right ? leftFollower : rightFollower;
            Transform holdHand = right ? rightFollower : leftFollower;
            Vector3 off;
            if (worn != null)
            {
                off = worn.FistFromHand(!right);
            }
            else
            {
                off = Vector3.zero;
            }
            GillaWounds wounds = GillaWounds.On(held.tearing);
            Vector3 at = holdHand.position + RealHand(!right) - RealHand(right) + off;
            if (!wounds.Hit(at, size * punchReach, swing, out section, out point))
            {
                if (!wounds.Hit(fistHand.position + off, size * punchReach, swing, out section, out point))
                {
                    return false;
                }
            }
            punchIn = punchEvery;
            swung = Vector3.zero;
            held.RPC_Punch((byte)section, point);
            Buzz(right ? XRNode.LeftHand : XRNode.RightHand, 1f, 0.2f);
            Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.5f, 0.1f);
            int punches = held.Punches /* ? */;
            KillCam cam = KillCam.Local;
            int toKill = cam != null ? cam.punchesToKill : 3;
            if (punches < toKill)
            {
                return false;
            }
            HeldDied();
            return true;
        }
        return false;
    }

    private void FreePunching()
    {
        GoreSection section = GoreSection.Head;
        Vector3 point = Vector3.zero;
        float size = transform.localScale.x;
        for (int i = 0; i < 2; i++)
        {
            bool right = i == 1;
            Vector3 swing = armSwung[i];
            if (Fist(right) && !SlabIn(right) && punchIn <= 0f && ripBody < 0 && bodyOf[i] < 0)
            {
                if (size * freePunchSwing <= swing.magnitude)
                {
                    if (punchSwingLasts + punchSwingLasts <= Time.time - armLostAt[i])
                    {
                        Vector3 off;
                        if (worn != null)
                        {
                            off = worn.FistFromHand(right);
                        }
                        else
                        {
                            off = Vector3.zero;
                        }
                        Vector3 real = RealHand(right);
                        Transform hand = right ? rightFollower : leftFollower;
                        Vector3 drawn = hand.position + off;
                        NetworkApe who = Punchable(real + off, drawn, size * punchReach, swing, out section, out point);
                        float lasts;
                        if (who == null)
                        {
                            if (!PunchDoor(RealHand(right) + off, swing, size))
                            {
                                continue;
                            }
                            punchIn = punchEvery;
                            armSwung[i] = Vector3.zero;
                            lasts = 0.25f;
                        }
                        else
                        {
                            armSwung[i] = Vector3.zero;
                            if (MenuShoves)
                            {
                                MenuShove(who, Shove(who, swing, size));
                            }
                            else
                            {
                                punchIn = punchEvery;
                                Vector3 going = Shove(who, swing, size);
                                who.RPC_PunchDown((byte)section, point, going);
                            }
                            lasts = 0.2f;
                        }
                        Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 1f, lasts);
                    }
                }
            }
        }
    }

    private bool MenuShove(NetworkApe who, Vector3 going)
    {
        if (who == null || 0f < punchIn)
        {
            return false;
        }
        punchIn = MenuShoveEvery;
        who.RPC_Shove(going);
        return true;
    }

    public float PunchReadyIn
    {
        get { return punchIn; }
    }

    private bool Fist(bool right)
    {
        if (!TestAnyHandPunches)
        {
            if (right)
            {
                if (!gripR)
                {
                    return false;
                }
                return wasGripR;
            }
            if (!gripL)
            {
                return false;
            }
            return wasGripL;
        }
        return true;
    }

    private void ArmSwing(int i, Vector3 real, float dt)
    {
        if (TestGrips < 0)
        {
            if (!Tracked(i == 1 ? XRNode.RightHand : XRNode.LeftHand))
            {
                armLostAt[i] = Time.time;
            }
        }
        Vector3 local = transform.InverseTransformPoint(real);
        Vector3 moved = transform.TransformVector(local - armWas[i]);
        armWas[i] = local;
        Vector3 v = moved / dt;
        if (Time.time - armSwungAt[i] <= punchSwingLasts)
        {
            if (v.sqrMagnitude < armSwung[i].sqrMagnitude)
            {
                return;
            }
        }
        armSwung[i] = v;
        armSwungAt[i] = Time.time;
    }

    public float ArmSwungLately
    {
        get
        {
            float a = armSwung[0].magnitude;
            float b = armSwung[1].magnitude;
            return Mathf.Max(a, b);
        }
    }

    private static bool LandsOn(NetworkApe a)
    {
        if (a != null)
        {
            if (!MenuShoves)
            {
                return true;
            }
            if (!a.SaysNoToPunches)
            {
                return !a.DownHere;
            }
        }
        return false;
    }

    private NetworkApe Punchable(Vector3 real, Vector3 drawn, float reach, Vector3 swing, out GoreSection section, out Vector3 point)
    {
        section = GoreSection.Torso;
        point = Vector3.zero;
        GoreSection sec;
        Vector3 pt;
        KillCam cam = KillCam.Local;
        int toKill = cam != null ? cam.punchesToKill : 3;
        Vector3 scale = transform.localScale;
        Vector3 from = head.position;
        bool walled = Walled(from, real);
        float near = scale.x * PunchNear;
        NetworkApe best = null;
        float bestD = float.MaxValue;
        foreach (NetworkApe a in NetworkApe.All)
        {
            if (!CanTouch(a))
            {
                continue;
            }
            if ((a.Busy && !a.DownHere) || toKill <= a.Punches /* ? */)
            {
                continue;
            }
            if (a.tearing == null)
            {
                continue;
            }
            if (!LandsOn(a))
            {
                continue;
            }
            float d = (a.head.position - drawn).sqrMagnitude;
            if (bestD <= d || near * near < d)
            {
                continue;
            }
            GillaWounds wounds = GillaWounds.On(a.tearing);
            if ((!walled && wounds.Hit(real, reach, swing, out sec, out pt)) || wounds.Hit(drawn, reach, swing, out sec, out pt))
            {
                section = sec;
                point = pt;
                best = a;
                bestD = d;
            }
        }
        return best;
    }
}
