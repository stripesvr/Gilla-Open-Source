using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Fusion;

public partial class GillaPickaxe : NetworkBehaviour
{
    private bool Held(Vector3 hole, float dt)
    {
        Vector3 pos = transform.position;
        Vector3 along = Along(end);
        Vector3 axis = on.TransformDirection(axisOn).normalized;
        Quaternion rot = transform.rotation;
        Vector3 tipLocal = end != 0 ? edgeTip : spikeTip;
        Vector3 fromHole = pos - hole;

        float target = Vector3.Dot(fromHole, axis) + Vector3.Dot(along, tipLocal);
        float d = depth;
        if (Mathf.Abs(target - d) > 0.012f)
        {
            float rate = (target - d <= 0f) ? pullRate : pushRate;
            target -= (target - d < 0f) ? -0.012f : 0.012f;
            depth = Mathf.MoveTowards(d, target, rate * dt);
            d = depth;
        }
        Into kind = into;
        if (mostIn <= d)
        {
            d = mostIn;
        }
        depth = d;

        bool yanked;
        if (d <= 0f)
        {
            yanked = false;
        }
        else
        {
            Vector3 holeOn = HoleOn;
            Vector3 offset = rot * holeOn;
            Quaternion turn = Quaternion.FromToRotation(-offset, fromHole) * rot;
            Vector3 dir = turn * along;
            float most = kind == Into.Body ? bodyLeverMost : leverMost;
            if (most < Vector3.Angle(axis, dir))
            {
                Vector3 limited = Vector3.RotateTowards(axis, dir, most * Mathf.Deg2Rad, 0f);
                turn = Quaternion.FromToRotation(dir, limited) * turn;
            }
            dir = turn * along;
            Vector3 place = hole - turn * holeOn;
            Vector3 diff = pos - place;
            Vector3 up = turn * Vector3.up;
            float u = Vector3.Dot(diff, up);
            float a = Vector3.Dot(diff, dir);
            Vector3 perp = diff - up * u - dir * a;
            float side = Mathf.Max(Mathf.Abs(u) - 0.26f, 0f);
            float fwd = Mathf.Max(a - 0.35f, 0f);
            if (a <= 0f)
            {
                fwd = -a;
            }
            float yanks = kind == Into.Body ? bodyYanksAt : yanksAt;
            if (fwd + side + perp.magnitude <= yanks)
            {
                Vector3 turned = Vector3.RotateTowards(axis, dir, dt * 0.06981317f, 0f);
                axisOn = on.InverseTransformDirection(turned);
                turnOn = Quaternion.Inverse(on.rotation) * turn;
                pullTo = pos + turn * holeOn;
                if (0f < comeFor)
                {
                    float t = Mathf.Clamp01(comeFor / -0.12f + 1f);
                    place = Vector3.Lerp(comeFrom, place, t);
                    turn = Quaternion.Slerp(comeTurn, turn, comeFor / -0.12f + 1f);
                    comeFor -= dt;
                }
                transform.SetPositionAndRotation(place, turn);
                if (Live && Object != null && Object.HasStateAuthority)
                {
                    Depth = depth;
                    if (kind == Into.Body)
                    {
                        PullTo = pullTo;
                        Pulling = true;
                        return true;
                    }
                }
                return true;
            }
            yanked = true;
        }
        ComesOut(yanked);
        return false;
    }

    private void Pin()
    {
        if (!item.Pinned)
        {
            item.Pinned = true;
            if (!item.Body.isKinematic)
            {
                item.Body.linearVelocity = Vector3.zero;
                item.Body.angularVelocity = Vector3.zero;
            }
        }
        item.Body.isKinematic = true;
        item.Body.interpolation = RigidbodyInterpolation.None;
        Collider[] colliders = item.Colliders;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c != null)
            {
                c.enabled = false;
            }
        }
    }

    private bool StillThere()
    {
        if (on == null)
        {
            return false;
        }
        if (item.Gone)
        {
            return false;
        }
        if (item.Swallowed)
        {
            return false;
        }
        if (into != Into.Body)
        {
            if (into == Into.Monster)
            {
                return monster != null;
            }
            GillaCorpse corpse = GillaCorpse.Find(bodyId);
            if (corpse != null && corpse.Has(0) && stuckAt - 0.5f <= corpse.MadeAt)
            {
                Transform bone = corpse.Bone(on.name);
                if (bone == null)
                {
                    return false;
                }
                on = bone;
                into = Into.Body;
                pulls = bone.GetComponentInParent<Rigidbody>();
                ape = null;
                copy = null;
                dummy = null;
                return true;
            }
            if (copy != null && section != GoreSection.Torso)
            {
                int id = section == GoreSection.Head ? 0 : (section == GoreSection.ArmL ? 1 : 2);
                if (copy.IsTorn((ApePart)id))
                {
                    return false;
                }
            }
            if (!on.gameObject.activeInHierarchy)
            {
                if (hiddenSince < 0f)
                {
                    hiddenSince = Time.time;
                }
                return Time.time - hiddenSince < 1f;
            }
            hiddenSince = -1f;
        }
        return true;
    }

    private float ThroughAt()
    {
        if ((into == Into.Player || into == Into.Dummy) && section != GoreSection.Head)
        {
            float f = section == GoreSection.Torso ? 0.15f : 0.07f;
            return f * on.lossyScale.x;
        }
        return float.MaxValue;
    }

    private void Slides(float dt)
    {
        float was = slidWas;
        slidWas = depth;
        float speed = Mathf.Abs(depth - was) / dt;
        if (speed < 0.05f)
        {
            return;
        }
        if (slideAt <= Time.time)
        {
            slideAt = Time.time + UnityEngine.Random.Range(0.22f, 0.38f);
            Vector3 at = on.TransformPoint(onLocal);
            Sfx.Play(slideSounds, at, Mathf.Clamp01(speed + speed) * 0.8f, 0.92f, 1.1f);
        }
        if (!item.Mine)
        {
            return;
        }
        float amp = Mathf.Clamp(speed, 0.1f, 0.55f);
        XRNode node;
        if (item.Hand == 0)
        {
            node = XRNode.LeftHand;
        }
        else
        {
            if (item.Hand != 1)
            {
                return;
            }
            node = XRNode.RightHand;
        }
        InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, amp, dt * 1.5f);
    }

    private void FixedUpdate()
    {
        if (into != Into.Body) return;
        if (pulls == null) return;
        if (pulls.isKinematic) return;
        if (on == null) return;
        if (item == null) return;
        bool mine = item.Mine;
        if (mine)
        {
            if (item.Hand < 0)
            {
                return;
            }
        }
        else
        {
            if (!Live)
            {
                return;
            }
            if (!Pulling)
            {
                return;
            }
        }
        Vector3 worldPoint = on.TransformPoint(onLocal);
        Vector3 to = mine ? pullTo : PullTo;
        Vector3 delta = to - worldPoint;
        Vector3 dir = transform.rotation * Along(end);
        float a = Vector3.Dot(delta, dir);
        float clamped = Mathf.Clamp(a, -0.05f, 0.05f);
        Vector3 vel = pulls.GetPointVelocity(worldPoint);
        Vector3 force = (delta - dir * a + dir * clamped) * 260f - vel * 22f;
        force = Vector3.ClampMagnitude(force, 70f);
        float mass = pulls.mass;
        pulls.AddForceAtPosition(force * mass, worldPoint, ForceMode.Force);
        pulls.WakeUp();
    }

    private void Harms()
    {
        Into kind = into;
        byte hurt = 2;
        if (section != GoreSection.Head)
        {
            hurt = (byte)(section == GoreSection.Torso ? 1 : 0);
        }
        float radius = end != 0 ? chiselRadius : holeRadius;
        harmed = true;
        if (kind == Into.Player && ape != null)
        {
            ape.RPC_Stab((byte)section, point, radius, hurt);
        }
        else if (kind == Into.Dummy && dummy != null)
        {
            dummy.Stab(section, point, radius, hurt);
        }
        else if (kind == Into.Monster && monster != null)
        {
            float harm = section != GoreSection.Head ? monsterHarm : monsterHeadHarm;
            if (Live && monster.net != null && monster.net.Live)
            {
                NetworkId who = monster.net.Object.Id;
                RPC_Monster(who, (byte)section, point, harm);
            }
            else
            {
                monster.Stabbed(harm, section, point);
            }
        }
        else if (kind == Into.Body)
        {
            GillaCorpse corpse = GillaCorpse.Find(targetId);
            if (corpse != null && corpse.Woundable && on != null)
            {
                Vector3 p = corpse.OnBody(section, on.TransformPoint(onLocal));
                if (Live)
                {
                    RPC_BodyStab(targetId, (byte)section, p, radius);
                }
                else
                {
                    corpse.Stab(section, p, radius);
                }
            }
        }
        if (Live)
        {
            RPC_Bled();
        }
        else
        {
            Bled();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_BodyStab(int id, byte s, Vector3 p, float radius)
    {
        GillaCorpse corpse = GillaCorpse.Find(id);
        if (corpse != null)
        {
            corpse.Stab((GoreSection)s, p, radius);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Monster(NetworkId who, byte s, Vector3 p, float harm)
    {
        NetworkObject obj = null;
        if (Runner != null)
        {
            obj = Runner.FindObject(who);
        }
        MonsterBrain brain = null;
        if (obj != null)
        {
            brain = obj.GetComponent<MonsterBrain>();
        }
        if (brain != null && !brain.Puppet)
        {
            brain.Stabbed(harm, (GoreSection)s, p);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Bled()
    {
        Bled();
    }

    private void Bled()
    {
        harmed = true;
        if (on == null)
        {
            return;
        }
        Vector3 hole = on.TransformPoint(onLocal);
        Vector3 way = -(transform.rotation * Along(end));
        BloodAt(hole, way, 3f, 12, 4, true);
        if (copy != null)
        {
            GillaSplatter.Stain(copy, hole, 0.05f, GillaSplatter.Stuff.Blood);
        }
        else if (into == Into.Body)
        {
            GillaCorpse corpse = GillaCorpse.Find(targetId);
            if (corpse != null)
            {
                corpse.Stain(hole, 0.05f);
            }
        }
        Dismemberment keepOff = copy;
        Action after = () => Bled_b__174_0(hole, keepOff) /* ? */;
        if (KillCam.Local == null)
        {
            GillaWounds.FinishAll();
            after();
        }
        else
        {
            after(); /* ? */ // GillaWounds.after.Enqueue(after) is inaccessible
        }
    }

    private void BloodAt(Vector3 at, Vector3 way, float seconds, int burst, int beats, bool first)
    {
        Material mat = Spray;
        if (mat == null)
        {
            return;
        }
        if (on == null)
        {
            return;
        }
        if (section == GoreSection.Head)
        {
            GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
            if (player != null && on.IsChildOf(player.transform))
            {
                return;
            }
        }
        GameObject go = new GameObject("PickBlood");
        Transform t = go.transform;
        t.SetParent(on, true);
        t.position = at;
        UnityEngine.Object.Destroy(t.gameObject, seconds + 3f);
        if (first)
        {
            Vector3 dir = (way + Vector3.up * 0.15f).normalized;
            Blood.Punch(t, dir, mat);
        }
        Blood.Spray(t, way, mat, seconds, burst, beats, 0.45f);
    }

    private void GoesThrough()
    {
        through = true;
        GillaWounds wounds = null;
        if (copy != null)
        {
            wounds = GillaWounds.On(copy);
        }
        if (wounds == null)
        {
            return;
        }
        Vector3 tip = transform.TransformPoint(end != 0 ? edgeTip : spikeTip);
        Vector3 skin = tip /* ? */; // wounds.NearestSkin(section, wounds.OnBody(on.name, tip)) is inaccessible
        float radius = end != 0 ? chiselRadius : holeRadius;
        if (into == Into.Player && ape != null)
        {
            ape.RPC_Stab((byte)section, skin, radius * 0.8f, 0);
        }
        else if (into == Into.Dummy && dummy != null)
        {
            dummy.Stab(section, skin, radius * 0.8f, 0);
        }
        if (!Live)
        {
            Through(tip);
            return;
        }
        RPC_Through(tip);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Through(Vector3 tip)
    {
        Through(tip);
    }

    private void Through(Vector3 tip)
    {
        through = true;
        Vector3 way = transform.rotation * Along(end);
        BloodAt(tip, way, 1.6f, 16, 2, true);
        Sfx.Play(stabSounds, tip, 0.8f, 0.85f, 0.95f);
        XRNode node;
        if (item.Hand == 0)
        {
            node = XRNode.LeftHand;
        }
        else
        {
            if (item.Hand != 1)
            {
                return;
            }
            node = XRNode.RightHand;
        }
        InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, 0.8f, 0.1f);
    }

    private void ComesOut(bool yanked)
    {
        if (yanked && harmed)
        {
            float radius = end != 0 ? chiselRadius : holeRadius;
            if (into == Into.Player && ape != null)
            {
                ape.RPC_Stab((byte)section, point, radius, 0);
            }
            else if (into == Into.Dummy && dummy != null)
            {
                dummy.Stab(section, point, radius, 0);
            }
        }
        if (Live)
        {
            RPC_Out(yanked);
            return;
        }
        Out(yanked, true);
    }
}
