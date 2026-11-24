// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class MonsterBrain
{
    private void LateUpdate()
    {
        bool dead = Dead;
        float dt = Time.deltaTime;
        if (!dead)
        {
            ChaseVoice(dt);
            if (headBone == null) return;
            if (face == null) return;
            if (LookWeight < 0.01f) return;
            Vector3 faceDir = face.position - headBone.position;
            Vector3 want = LookPoint - headBone.position;
            if (faceDir.sqrMagnitude < 1e-06f) return;
            if (want.sqrMagnitude < 0.04f) return;
            Quaternion q = Quaternion.FromToRotation(faceDir, want);
            float angle;
            Vector3 axis;
            q.ToAngleAxis(out angle, out axis);
            if (angle > 180f) angle -= 360f;
            angle = Mathf.Clamp(angle, -60f, 60f);
            headBone.rotation = Quaternion.AngleAxis(angle * LookWeight, axis) * headBone.rotation;
            return;
        }
        Lie(dt);
    }

    public void Blasted(float damage, Vector3 push, Vector3 at, float near)
    {
        if (Dead) return;
        if (Puppet) return;
        Health = Health - damage;
        if (Hurt != null)
        {
            Hurt.Blast(at, near);
        }
        bool held = state == MonsterBrain.State.Kill || climb != null || lurking;
        if (!held || 0f < Health)
        {
            if (Health <= 0f)
            {
                Die();
                return;
            }
        }
        else
        {
            Health = 1f;
        }
        if (!held)
        {
            shove = push;
            staggerT = staggerSeconds;
            speed = 0f;
        }
    }

    public void Stabbed(float damage, GoreSection section, Vector3 point)
    {
        if (Dead) return;
        if (Puppet) return;
        Health = Health - damage;
        if (Hurt != null)
        {
            Hurt.Stabbed(section, point);
        }
        bool held = state == MonsterBrain.State.Kill || climb != null || lurking;
        if (!held || 0f < Health)
        {
            if (Health <= 0f)
            {
                Die();
                return;
            }
        }
        else
        {
            Health = 1f;
        }
        if (!held)
        {
            speed = 0f;
            staggerT = Mathf.Max(staggerT, staggerSeconds * 0.4f);
        }
    }

    private void Stagger(float dt)
    {
        staggerT -= dt;
        shove = Vector3.MoveTowards(shove, Vector3.zero, dt * 9f);
        speed = 0f;
        AnimSpeed = 0f;
        if (animator != null)
        {
            animator.SetFloat(SpeedId, 0f, 0.05f, dt);
        }
        vy = cc.isGrounded ? -1f : vy - gravity * dt;
        cc.Move((shove + Vector3.up * vy) * dt) /* ? */;
    }

    public void Snared(float seconds, Vector3 at)
    {
        if (!Dead && !Puppet)
        {
            if (Hurt != null)
            {
                Hurt.Trapped(at);
            }
            if (state != MonsterBrain.State.Kill)
            {
                if (climb == null && !lurking)
                {
                    if (state == MonsterBrain.State.Pry || state == MonsterBrain.State.Break)
                    {
                        EndPry();
                    }
                    staggerT = 0f;
                    snareT = Mathf.Max(snareT, seconds);
                    SnareLeft = snareT;
                    speed = 0f;
                    shove = Vector3.zero;
                }
            }
        }
    }

    private void Snare(float dt)
    {
        AnimSpeed = 0f;
        AnimPace = 1f;
        speed = 0f;
        snareT = snareT - dt;
        SnareLeft = snareT;
        if (animator != null)
        {
            animator.SetFloat(SpeedId, 0f, 0.05f, dt);
            animator.SetFloat(PaceId, 1f);
        }
        vy = cc.isGrounded ? -1f : vy - gravity * dt;
        cc.Move(Vector3.up * (vy * dt));
        stuckAt = transform.position;
        stuckT = 0f;
        if (snareT <= 0f)
        {
            repathAt = 0f;
        }
    }

    public void ShowSnare(float left)
    {
        snareT = left;
        SnareLeft = left;
    }

    public void HeardHealth(float left)
    {
        Health = left;
    }

    public void Die()
    {
        if (Prying)
        {
            EndPry();
        }
        hasPrey = false;
        Chasing = false;
        if (net != null && net.Live)
        {
            MonsterDirector.Killed(name);
        }
        ShowDeath();
    }

    public void ShowDeath()
    {
        if (Dead) return;
        Dead = true;
        deadT = 0f;
        All.Remove(this);
        if (animator != null)
        {
            animator.speed = 0f;
        }
        if (voice != null)
        {
            voice.Stop();
        }
        SnareLeft = 0f;
        snareT = 0f;
        if (Hurt != null)
        {
            Hurt.Died();
        }
        if (model != null)
        {
            deadFrom = model.rotation;
            deadAt = model.position;
        }
        Vector3 dir = shove;
        if (dir.sqrMagnitude <= 0.01f)
        {
            dir = -transform.forward;
        }
        dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
        fallAxis = Vector3.Cross(Vector3.up, dir);
        if (fallAxis.sqrMagnitude < 0.5f)
        {
            fallAxis = transform.right;
        }
    }

    private void Lie(float dt)
    {
        deadT += dt;
        if (model != null)
        {
            float t = deadT;
            float k = Mathf.Clamp01(t / 0.55f);
            float bounce = 0f;
            float fall;
            if (t / 0.55f < 0f || k < 1f)
            {
                fall = k * k * 84f;
            }
            else
            {
                fall = k * k * 84f;
                bounce = Mathf.Sin((t - 0.55f) * 18f) * Mathf.Exp((t - 0.55f) * -6f) * 7f;
            }
            float sink = Mathf.Clamp01((t - (deadSeconds - 1.5f)) / 1.5f);
            model.SetPositionAndRotation(deadAt + Vector3.down * sink * 1.2f,
                Quaternion.AngleAxis(fall - bounce, fallAxis) * deadFrom);
        }
        if (deadT < deadSeconds) return;
        if (Puppet) return;
        if (net == null || !net.Live)
        {
            Destroy(gameObject);
            return;
        }
        if (!net.Object.HasStateAuthority) return;
        net.Runner.Despawn(net.Object);
    }

    public void StartClimb(MonsterClimb wall)
    {
        climb = wall;
        climbPhase = 0;
        climbT = 0f;
        climbFrom = transform.position;
        Vector3 into = wall.Into;
        climbFoot = new Vector3(wall.rim.x, wall.bottom.y, wall.rim.z) - into * 0.06f;
        cc.enabled = false;
        vy = 0f;
    }

    private void Climb(float dt)
    {
        climbT += dt;
        Vector3 into = climb.Into;
        float yaw = transform.eulerAngles.y;
        float wantYaw = Quaternion.LookRotation(into, Vector3.up).eulerAngles.y;
        float delta = Mathf.DeltaAngle(yaw, wantYaw);
        yaw = Mathf.SmoothDamp(yaw, yaw + delta, ref yawVel, 0.12f, turnSpeed + turnSpeed, dt);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        float anim;
        float pace;
        int phase = climbPhase;
        if (phase == 2)
        {
            float top = climb.rim.y - 0.25f;
            ClimbPitch = climbPitch;
            Vector3 pos = transform.position;
            float wobble = Mathf.Sin(climbT * 9f) * 0.035f;
            float y = Mathf.Min(pos.y + climbSpeed * dt, top);
            Vector3 side = Vector3.Cross(Vector3.up, into) * wobble;
            transform.position = new Vector3(climbFoot.x + side.x, y + side.y, climbFoot.z + side.z);
            anim = 2f;
            pace = climbSpeed / clipRunSpeed * 1.7f;
            if (top <= y)
            {
                climbPhase = 3;
                climbT = 0f;
                climbFrom = transform.position;
            }
        }
        else
        {
            pace = walkSpeed / clipWalkSpeed;
            if (phase == 1)
            {
                anim = 1f;
                ClimbPitch = climbPitch * Mathf.SmoothStep(0f, 1f, climbT / 0.35f);
                if (0.35f <= climbT)
                {
                    climbPhase = 2;
                    climbT = 0f;
                }
            }
            else if (phase == 0)
            {
                Vector3 pos = transform.position;
                float dx = climbFoot.x - pos.x;
                float dz = climbFoot.z - pos.z;
                float step = walkSpeed * 1.4f * dt;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist <= step)
                {
                    transform.position = climbFoot;
                    anim = 1f;
                    climbPhase = 1;
                    climbT = 0f;
                }
                else
                {
                    Vector3 dir = new Vector3(dx, 0f, dz).normalized;
                    transform.position = transform.position + dir * step;
                    anim = 1f;
                }
            }
            else
            {
                float t = Mathf.Clamp01(climbT / 0.55f);
                float up = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI * 0.5f));
                float across = Mathf.Min(t * t, 1f);
                Vector3 top = climb.top;
                transform.position = new Vector3(
                    climbFrom.x + across * (top.x - climbFrom.x),
                    Mathf.Sin(t * Mathf.PI) * 0.15f + climbFrom.y + (top.y - climbFrom.y) * up,
                    climbFrom.z + across * (top.z - climbFrom.z));
                pace = climbSpeed / clipRunSpeed * 1.5f;
                ClimbPitch = climbPitch * (1f - Mathf.SmoothStep(0f, 1f, t));
                anim = 2f;
                if (1f <= t)
                {
                    climb = null;
                    ClimbPitch = 0f;
                    cc.enabled = true;
                    repathAt = 0f;
                    speed = runSpeed * 0.5f;
                    anim = 2f;
                }
            }
        }
        PoseClimb();
        AnimSpeed = anim;
        AnimPace = Mathf.Max(0.5f, pace);
        if (animator == null) return;
        animator.SetFloat(SpeedId, anim, 0.1f, dt);
        animator.SetFloat(PaceId, AnimPace);
    }

    private void PoseClimb()
    {
        if (model != null)
        {
            model.localRotation = Quaternion.AngleAxis(-ClimbPitch, Vector3.right) * modelRest;
        }
    }

    public void ShowClimb(float pitch)
    {
        ClimbPitch = pitch;
        PoseClimb();
    }

    private bool StartLurk()
    {
        RaycastHit hit;
        nextLurk = Time.time + UnityEngine.Random.Range(lurkEvery * 0.6f, lurkEvery * 1.4f);
        Vector3 pos = transform.position;
        Vector3 up = Vector3.up;
        float bestNx = 0f;
        float bestNz = 0f;
        float bestX = 0f;
        float bestZ = 0f;
        float best = float.MaxValue;
        for (int i = 0; i < 12; i++)
        {
            float jitter = UnityEngine.Random.Range(-12f, 12f);
            Vector3 dir = Quaternion.Euler(0f, i * 30f + jitter, 0f) * Vector3.forward;
            if (Physics.Raycast(pos + up * 0.4f, dir, out hit, 7f, Solid, QueryTriggerInteraction.Ignore)
                && 0.8f <= hit.distance && Mathf.Abs(hit.normal.y) <= 0.2f)
            {
                if (hit.collider.GetComponentInParent<GillaDoor>() != null) continue;
                Vector3 off = hit.point + hit.normal * 0.35f;
                if (Physics.Raycast(off, Vector3.up, lurkHeight + 0.6f, Solid, QueryTriggerInteraction.Ignore)) continue;
                Vector3 n = hit.normal;
                if (Physics.Raycast(off + Vector3.up * (lurkHeight + 0.4f), -n, out hit, 0.7f, Solid, QueryTriggerInteraction.Ignore)
                    && hit.distance < best)
                {
                    bestX = hit.point.x;
                    best = hit.distance;
                    bestZ = hit.point.z;
                    bestNx = hit.normal.x;
                    bestNz = hit.normal.z;
                }
            }
        }
        if (best != float.MaxValue)
        {
            lurkInto = new Vector3(-bestNx, 0f, -bestNz).normalized;
            Vector3 here = transform.position;
            lurking = true;
            lurkPhase = 0;
            lurkT = 0f;
            lurkFoot = new Vector3(bestX, here.y, bestZ) + lurkInto * -0.05f;
            lurkHold = UnityEngine.Random.Range(9f, 20f);
        }
        return best != float.MaxValue;
    }

    private void Lurk(float dt)
    {
        int phase = lurkPhase;
        Vector3 look = lurkInto;
        float pace = walkSpeed / Mathf.Max(clipWalkSpeed, 0.01f);
        float anim;
        lurkT += dt;
        float t0 = lurkT;
        if (phase == 0)
        {
            Target seen;
            if (Spot(out seen))
            {
                prey = seen;
                hasPrey = true;
                EndLurk(true);
                return;
            }
            Vector3 pos = transform.position;
            float dx = lurkFoot.x - pos.x;
            float dz = lurkFoot.z - pos.z;
            float sqr = dx * dx + dz * dz;
            float dist = Mathf.Sqrt(sqr);
            if (dist <= cc.radius + 0.12f)
            {
                cc.enabled = false;
                vy = 0f;
                lurkPhase = 1;
                lurkT = 0f;
                lurkFrom = transform.position;
                anim = 0f;
            }
            else
            {
                if (4f < lurkT)
                {
                    EndLurk(false);
                    return;
                }
                if (0.0001f < sqr)
                {
                    look = new Vector3(dx, 0f, dz);
                }
                vy = cc.isGrounded ? -1f : vy - gravity * dt;
                Vector3 dir = new Vector3(dx, 0f, dz).normalized;
                cc.Move((dir * walkSpeed * 1.3f + Vector3.up * vy) * dt);
                anim = 1f;
            }
        }
        else if (phase == 1)
        {
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t0 / 0.4f));
            transform.position = Vector3.Lerp(lurkFrom, lurkFoot, s);
            anim = 0f;
            ClimbPitch = s * 90f;
            if (0.4f <= lurkT)
            {
                lurkPhase = 2;
                lurkT = 0f;
            }
        }
        else if (phase == 2)
        {
            ClimbPitch = 90f;
            Vector3 pos = transform.position;
            float y = Mathf.Min(pos.y + climbSpeed * dt, lurkFoot.y + lurkHeight);
            transform.position = new Vector3(lurkFoot.x, y, lurkFoot.z);
            pace = climbSpeed / Mathf.Max(clipWalkSpeed, 0.01f);
            anim = 1f;
            if (lurkFoot.y + lurkHeight <= y)
            {
                lurkPhase = 3;
                lurkT = 0f;
            }
        }
        else if (phase == 3)
        {
            ClimbPitch = 90f;
            Glance(t0, 1f);
            Target seen;
            if (Spot(out seen))
            {
                prey = seen;
                hasPrey = true;
                Drop();
                anim = 0f;
            }
            else
            {
                anim = 0f;
                if (lurkHold < lurkT)
                {
                    hasPrey = false;
                    Drop();
                }
            }
        }
        else
        {
            float t = Mathf.Clamp01(t0 / 0.45f);
            float hop = (1f - t) * Mathf.Sin(t * Mathf.PI) * 0.25f;
            transform.position = Vector3.Lerp(lurkFrom, lurkLand, t) + Vector3.up * hop;
            ClimbPitch = (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lurkT / 0.3f))) * 90f;
            if (hasPrey)
            {
                Vector3 d = prey.head.position - transform.position;
                if (0.0001f <= d.x * d.x + d.z * d.z)
                {
                    look = new Vector3(d.x, 0f, d.z);
                }
                else
                {
                    look = -lurkInto;
                }
            }
            anim = 0f;
            if (1f <= t)
            {
                EndLurk(hasPrey);
                return;
            }
        }

        float yaw = transform.eulerAngles.y;
        float wantYaw = Quaternion.LookRotation(look, Vector3.up).eulerAngles.y;
        float delta = Mathf.DeltaAngle(yaw, wantYaw);
        yaw = Mathf.SmoothDamp(yaw, yaw + delta, ref yawVel, 0.15f, turnSpeed + turnSpeed, dt);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        PoseClimb();
        AnimSpeed = anim;
        AnimPace = Mathf.Max(0.5f, pace);
        if (animator == null) return;
        animator.SetFloat(SpeedId, anim, 0.1f, dt);
        animator.SetFloat(PaceId, AnimPace);
    }

    private void Drop()
    {
        lurkPhase = 4;
        lurkT = 0f;
        RaycastHit hit;
        lurkFrom = transform.position;
        Vector3 pos = transform.position;
        Vector3 pos2 = transform.position;
        Vector3 from = new Vector3(pos.x - lurkInto.x * 0.7f, (lurkFoot.y + 0.3f) - lurkInto.y * 0.7f, pos2.z - lurkInto.z * 0.7f);
        float x = from.x;
        float z = from.z;
        float y;
        if (Physics.Raycast(from, Vector3.down, out hit, 2f, Solid, QueryTriggerInteraction.Ignore))
        {
            x = hit.point.x;
            y = hit.point.y;
            z = hit.point.z;
        }
        else
        {
            y = lurkFoot.y;
        }
        lurkLand = new Vector3(x, y, z);
    }

    private void EndLurk(bool chase)
    {
        lurking = false;
        ClimbPitch = 0f;
        PoseClimb();
        cc.enabled = true;
        vy = 0f;
        repathAt = 0f;
        if (chase && hasPrey)
        {
            state = MonsterBrain.State.Chase;
            Chasing = true;
            listening = false;
            stateT = 0f;
            unreachT = 0f;
            speed = runSpeed * 0.6f;
            return;
        }
        state = MonsterBrain.State.Wander;
        Chasing = false;
        stateT = 0f;
        listening = false;
        hasPrey = false;
        PickWander(null);
    }

    public void ShowPuppet(float animSpeed, float pace, Vector3 look, float lookWeight)
    {
        LookPoint = look;
        AnimSpeed = animSpeed;
        AnimPace = pace;
        LookWeight = lookWeight;
        if (animator == null) return;
        animator.SetFloat(SpeedId, animSpeed, 0.1f, Time.deltaTime);
        animator.SetFloat(PaceId, pace);
    }

    public void ShowPry(bool on)
    {
        Prying = on;
        if (animator != null && hasPry)
        {
            animator.SetBool(PryId, on);
        }
    }

    public void PlayKill()
    {
        if (animator == null) return;
        animator.SetFloat(PaceId, 1f);
        animator.SetTrigger(KillId);
    }

    private void Begin(MonsterBrain.State s, float notice)
    {
        state = s;
        if (s == MonsterBrain.State.Pry || s == MonsterBrain.State.Break)
        {
            Chasing = pryFrom == MonsterBrain.State.Chase;
        }
        else if (s == MonsterBrain.State.Chase)
        {
            stateT = 0f;
            Chasing = true;
            listening = false;
            return;
        }
        else
        {
            Chasing = false;
        }
        stateT = 0f;
        if (s == MonsterBrain.State.Search)
        {
            searchFor = 9f;
        }
        else if (s == MonsterBrain.State.Notice)
        {
            if (notice <= 0f)
            {
                notice = UnityEngine.Random.Range(0.45f, 0.8f);
            }
            noticeFor = notice;
        }
        else
        {
            listening = false;
            if (s != MonsterBrain.State.Recover && s != MonsterBrain.State.Wander) return;
            hasPrey = false;
            return;
        }
        listening = false;
    }

    private static ValueTuple<int, int> Key(Target t)
    {
        int kind;
        int id;
        if (t.dummy != null)
        {
            id = t.dummy.GetHashCode();
            kind = 2;
        }
        else if (!t.local)
        {
            id = t.player.PlayerId;
            kind = 0;
        }
        else
        {
            kind = 1;
            id = 0;
        }
        return new ValueTuple<int, int>(kind, id);
    }

    public bool MarkedYou
    {
        get { return marked.ContainsKey(new ValueTuple<int, int>(1, 0)); }
    }
}
