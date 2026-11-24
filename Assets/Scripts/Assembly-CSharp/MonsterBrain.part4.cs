// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class MonsterBrain : MonoBehaviour
{
    private bool Watched()
    {
        parts[0] = face != null ? face.position : transform.position + Vector3.up * 1.2f;
        parts[1] = Middle;
        parts[2] = transform.position + Vector3.up * 0.25f;

        bool any = false;
        foreach (Target t in targets)
        {
            if (t.head == null)
                continue;
            ValueTuple<int, int> key = Key(t);
            if (LooksAtMe(t.head))
            {
                marked[key] = Time.time;
                any = true;
            }
            else if (marked.ContainsKey(key) && CanSee(t.head))
            {
                marked[key] = Time.time;
            }
        }

        if (marked.Count < 1)
            return any;

        unmark.Clear();
        foreach (KeyValuePair<ValueTuple<int, int>, float> kv in marked)
        {
            if (Time.time - kv.Value > FreeAfter)
                unmark.Add(kv.Key);
        }
        foreach (ValueTuple<int, int> k in unmark)
            marked.Remove(k);
        return any;
    }

    private bool LooksAtMe(Transform head)
    {
        Vector3 a = head.position;
        Vector3 forward = head.forward;
        for (int i = 0; i < parts.Length; i++)
        {
            Vector3 p = parts[i];
            Vector3 d = p - a;
            float dist = d.magnitude;
            if (dist >= 0.01f && dist <= gazeRange)
            {
                float angle = Vector3.Angle(forward, d);
                if (angle <= gazeAngle && Unblocked(a, p) && Unblocked(p, a))
                    return true;
            }
        }
        return false;
    }

    private bool Unblocked(Vector3 a, Vector3 b)
    {
        RaycastHit hit;
        if (!Physics.Linecast(a, b, out hit, Solid, QueryTriggerInteraction.Ignore))
            return true;
        return hit.collider.transform.IsChildOf(transform);
    }

    private void Hold(float dt)
    {
        speed = 0f;
        yawVel = 0f;
        stoodT = 0f;
        if (cc.enabled)
        {
            vy = cc.isGrounded ? -1f : vy - gravity * dt;
            cc.Move(Vector3.up * (vy * dt));
        }
        AnimPace = 0f;
        if (animator == null)
            return;
        AnimSpeed = animator.GetFloat(SpeedId);
        animator.SetFloat(PaceId, 0f);
    }

    private Vector3 Weep(float dt, bool preyOk, ref float want)
    {
        bool keep = false;
        bool chase = false;
        if (state == State.Chase)
        {
            ValueTuple<int, int> key = Key(prey);
            if (marked.ContainsKey(key) && preyOk)
            {
                if (Time.time < easiestAt)
                    chase = true;
                else
                    keep = true;
            }
        }

        if (!chase)
        {
            easiestAt = Time.time + EasiestEvery;
            Target t;
            if (NearestMarked(out t))
            {
                if (!keep || !Key(t).Equals(Key(prey)))
                {
                    trail.Clear();
                    repathAt = 0f;
                }
                prey = t;
                hasPrey = true;
                if (state != State.Chase)
                {
                    Chasing = true;
                    state = State.Chase;
                    listening = false;
                    stateT = 0f;
                    unreachT = 0f;
                }
            }
            else if (!keep)
            {
                if (state == State.Wander)
                {
                    if (wait > 0f)
                    {
                        wait -= dt;
                        Glance(wait, 0.7f);
                        return transform.position;
                    }
                }
                else
                {
                    state = State.Wander;
                    Chasing = false;
                    stateT = 0f;
                    listening = false;
                    hasPrey = false;
                    PickWander(null);
                    wait = 0f;
                }
                Vector3 pos = transform.position;
                float dx = wanderTarget.x - pos.x;
                float dz = wanderTarget.z - pos.z;
                if (Mathf.Sqrt(dx * dx + dz * dz) < 0.6f || Stuck(dt))
                {
                    PickWander(null);
                    wait = UnityEngine.Random.Range(1.5f, 4f);
                }
                want = walkSpeed * gait;
                return wanderTarget;
            }
        }

        bool seen = CanSee(prey.head);
        if (seen)
            lastSeen = prey.head.position;
        want = runSpeed;
        lookWant = prey.head.position;
        lookWeightWant = 1f;
        float len;
        if (Way(prey.head, out len))
        {
            unreachT = 0f;
        }
        else
        {
            unreachT += dt;
            if (unreachT > 1f)
            {
                state = State.Wander;
                Chasing = false;
                stateT = 0f;
                listening = false;
                hasPrey = false;
                PickWander(null);
                return wanderTarget;
            }
        }

        if (seen && InReach(prey.head))
        {
            bool close;
            if (grabSocket != null && Vector3.Distance(prey.head.position, grabSocket.position) < grabRange)
            {
                close = true;
            }
            else
            {
                Vector3 hp = prey.head.position;
                Vector3 mp = transform.position;
                float dx = hp.x - mp.x;
                float dz = hp.z - mp.z;
                float reach = Mathf.Max(grabRange * 0.6f, OnTopOf);
                close = Mathf.Sqrt(dx * dx + dz * dz) < reach;
            }
            if (close)
            {
                ValueTuple<int, int> key = Key(prey);
                StartKill(prey);
                if (state == State.Kill)
                    marked.Remove(key);
            }
        }
        return Hunt(prey.head, seen);
    }

    private bool NearestMarked(out Target best)
    {
        best = default(Target);
        if (marked.Count == 0)
            return false;
        float bestD = float.MaxValue;
        foreach (Target t in targets)
        {
            ValueTuple<int, int> key = Key(t);
            if (!marked.ContainsKey(key))
                continue;
            Vector3 hp = t.head.position;
            Vector3 mp = transform.position;
            float dx = hp.x - mp.x;
            float dz = hp.z - mp.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            float len;
            if (d < bestD && CanSee(t.head) && InReach(t.head) && Way(t.head, out len))
            {
                best = t;
                bestD = d;
            }
        }
        return bestD < float.MaxValue;
    }

    private void GatherTargets()
    {
        targets.Clear();
        if (net != null && net.Live)
        {
            foreach (NetworkApe ape in NetworkApe.All)
            {
                if (ape == null)
                    continue;
                if (ape.head == null || ape.IsMonster || ape.Busy)
                    continue;
                bool local = ape.IsLocal;
                Transform head;
                if (local)
                {
                    head = KillCam.Local != null ? KillCam.Local.head : ape.head;
                    if (KillCam.Local == null || KillCam.Local.Busy || IgnoreLocal)
                        continue;
                }
                else
                {
                    head = ape.head;
                }
                if (!GillaBackroomsSwitch.SameSide(head.position, transform.position))
                    continue;
                Target t = new Target();
                t.head = head;
                t.player = ape.Object.InputAuthority /* ? */;
                t.local = local;
                t.voice = TestVoice >= 0f ? TestVoice : ape.VoiceLevel;
                targets.Add(t);
            }
        }
        else if (!IgnoreLocal && KillCam.Local != null && !KillCam.Local.Busy && !MonsterPlayer.IsMonster
                 && GillaBackroomsSwitch.SameSide(KillCam.Local.head.position, transform.position))
        {
            if (localMic == null)
            {
                localMic = KillCam.Local.GetComponentInChildren<MicLevel>();
                if (localMic == null)
                    localMic = UnityEngine.Object.FindAnyObjectByType<MicLevel>();
            }
            Target t = new Target();
            t.head = KillCam.Local.head;
            t.local = true;
            t.voice = localMic != null ? localMic.Level : 0f;
            targets.Add(t);
        }

        foreach (GillaDummy d in GillaDummy.All)
        {
            if (d == null || !d.Standing || d.head == null)
                continue;
            if (!GillaBackroomsSwitch.SameSide(d.head.position, transform.position))
                continue;
            Target t = new Target();
            t.head = d.head;
            t.dummy = d;
            targets.Add(t);
        }
    }

    private bool Hear(out Target heard)
    {
        heard = default(Target);
        if (ignoreT > 0f)
            return false;
        if (Time.time < hearAt)
            return false;
        hearAt = Time.time + 0.15f;
        Vector3 ear = face != null ? face.position : transform.position + Vector3.up;
        float best = 1f;
        foreach (Target t in targets)
        {
            if (t.voice <= hearThreshold)
                continue;
            float k;
            if (t.dummy == null && killedAt.TryGetValue(t.player, out k)
                && Time.time - k < recoverSeconds + 6f && !t.local)
                continue;
            float loud = 0f;
            if (hearThreshold != 1f)
                loud = Mathf.Clamp01((t.voice - hearThreshold) / (1f - hearThreshold));
            float range = hearRange * (loud * 0.7f + 0.3f);
            float dist = Vector3.Distance(t.head.position, ear);
            if (dist <= range)
            {
                if (!Clear(ear, t.head.position))
                    range *= hearThroughWalls;
                float r = dist / range;
                if (r < best)
                {
                    heard = t;
                    best = r;
                }
            }
        }
        return best < 1f;
    }

    private void Investigate(Target t)
    {
        prey = t;
        hasPrey = true;
        Vector3 hp = t.head.position;
        Vector3 mp = transform.position;
        float dx = hp.x - mp.x;
        float dz = hp.z - mp.z;
        if (Mathf.Sqrt(dx * dx + dz * dz) < senseRange + senseRange)
        {
            float len;
            if (Way(t.head, out len))
            {
                Chasing = false;
                state = State.Notice;
                stateT = 0f;
                noticeFor = 0.35f;
                listening = false;
                return;
            }
        }
        lastSeen = t.head.position;
        if (!listening)
        {
            Chasing = false;
            state = State.Search;
            searchFor = 9f;
            stateT = 0f;
            listening = true;
            listeningSince = Time.time;
        }
        lookWant = lastSeen;
        lookWeightWant = 1f;
    }

    private bool Find(Target was, out Target t)
    {
        foreach (Target x in targets)
        {
            bool match;
            if (was.dummy != null)
                match = x.dummy == was.dummy;
            else
                match = x.dummy == null && x.local == was.local && (was.local || x.player == was.player);
            if (match)
            {
                t = x;
                return true;
            }
        }
        t = default(Target);
        return false;
    }

    private bool Spot(out Target seen)
    {
        seen = default(Target);
        if (ignoreT > 0f)
            return false;
        float best = float.MaxValue;
        foreach (Target t in targets)
        {
            float k;
            if (t.dummy == null && killedAt.TryGetValue(t.player, out k)
                && Time.time - k < recoverSeconds + 6f && !t.local)
                continue;
            Vector3 hp = t.head.position;
            Vector3 mp = transform.position;
            float dx = hp.x - mp.x;
            float dz = hp.z - mp.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < best && CanSee(t.head))
            {
                if (d > senseRange)
                {
                    Vector3 to = t.head.position - transform.position;
                    to.y = 0f;
                    float angle = Vector3.Angle(transform.forward, to);
                    if (angle > sightAngle * 0.5f)
                        continue;
                }
                float len;
                if (Way(t.head, out len))
                {
                    seen = t;
                    best = d;
                }
            }
        }
        return best < float.MaxValue;
    }

    private bool GoForEasiest(bool haveOne)
    {
        float cur = float.MaxValue;
        if (haveOne)
        {
            float l;
            cur = Way(prey.head, out l) ? l : float.MaxValue;
        }
        float bestLen = float.MaxValue;
        Target pick = default(Target);
        foreach (Target t in targets)
        {
            if (haveOne)
            {
                bool same;
                if (prey.dummy != null)
                    same = t.dummy == prey.dummy;
                else
                    same = t.dummy == null && t.local == prey.local && (t.local || t.player == prey.player);
                if (same)
                    continue;
            }
            float k;
            if (t.dummy == null && killedAt.TryGetValue(t.player, out k)
                && Time.time - k < recoverSeconds + 6f && !t.local)
                continue;
            Vector3 hp = t.head.position;
            Vector3 mp = transform.position;
            float dx = hp.x - mp.x;
            float dz = hp.z - mp.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d < bestLen && (d < cur - EasiestBy || cur >= float.MaxValue)
                && (d <= senseRange || CanSee(t.head)) && InReach(t.head))
            {
                float len;
                if (Way(t.head, out len) && len < bestLen)
                {
                    pick = t;
                    bestLen = len;
                }
            }
        }
        if (bestLen == float.MaxValue || (bestLen >= cur - EasiestBy && cur < float.MaxValue))
            return false;
        prey = pick;
        hasPrey = true;
        trail.Clear();
        lastSeen = pick.head.position;
        unreachT = 0f;
        repathAt = 0f;
        return true;
    }

    private void StartKill(Target t)
    {
        if (t.local)
        {
            if (KillCam.Local == null)
                return;
            if (!KillCam.Local.Begin(this, null))
                return;
        }
        if (t.dummy != null && !t.dummy.Standing)
            return;

        Chasing = false;
        state = State.Kill;
        stateT = 0f;
        listening = false;
        killLocal = t.local;
        if (t.dummy != null)
            t.dummy.Eaten(this);
        else
            killedAt[t.player] = Time.time;
        speed = 0f;

        Vector3 hp = t.head.position;
        Vector3 mp = transform.position;
        Vector3 flat = new Vector3(hp.x - mp.x, 0f, hp.z - mp.z);
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        PlayKill();
        if (net != null)
            net.KillStarted(t.player);
    }
}
