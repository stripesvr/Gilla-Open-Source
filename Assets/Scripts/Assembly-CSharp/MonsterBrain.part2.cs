// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Fusion;

public partial class MonsterBrain : MonoBehaviour
{
    private void Update()
    {
        if (livesBelow && Asleep())
        {
            speed = 0f;
            AnimSpeed = 0f;
            return;
        }
        if (Puppet)
        {
            return;
        }
        if (Frozen)
        {
            speed = 0f;
            AnimSpeed = 0f;
            return;
        }
        float dt = Time.deltaTime;
        if (Dead)
        {
            return;
        }

        if (GillaPitKill.Fallen(transform.position))
        {
            cc.enabled = false;
            transform.position = home;
            cc.enabled = true;
            vy = 0f;
            speed = 0f;
            stuckAt = transform.position;
            stuckT = 0f;
            PickWander(null);
            return;
        }

        if (snareT > 0f)
        {
            AnimSpeed = 0f;
            AnimPace = 1f;
            speed = 0f;
            snareT -= dt;
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
            return;
        }

        if (staggerT <= 0f && Hurt != null)
        {
            if (Hurt.Wreck > 0.45f && speed > walkSpeed * 0.5f && Time.time >= stumbleAt
                && state != State.Kill && state != State.Pry && state != State.Break
                && climb == null && !lurking)
            {
                stumbleAt = Time.time + UnityEngine.Random.Range(3.5f, 7f);
                staggerT = Mathf.Clamp01(Hurt.Wreck) * 0.35f + 0.25f;
                Vector3 right = transform.right;
                float side = Hurt.Side * 1.5f;
                Vector3 forward = transform.forward;
                shove = right * side + forward * (speed * 0.25f);
            }
        }

        if (staggerT > 0f)
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
            return;
        }

        bool gathered;
        if (!weeping || state == State.Kill)
        {
            gathered = false;
        }
        else
        {
            GatherTargets();
            if (Watched())
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
                {
                    return;
                }
                AnimSpeed = animator.GetFloat(SpeedId);
                animator.SetFloat(PaceId, 0f);
                return;
            }
            gathered = true;
        }

        if (ignoreT > 0f)
        {
            ignoreT -= dt;
        }
        stateT += dt;
        if (StoodTooLong(dt))
        {
            return;
        }

        if (climb != null)
        {
            climbT += dt;
            Vector3 into = climb.Into;
            float yaw = transform.eulerAngles.y;
            float wantYaw = Quaternion.LookRotation(into, Vector3.up).eulerAngles.y /* ? */;
            float newYaw = Mathf.SmoothDamp(yaw, yaw + Mathf.DeltaAngle(yaw, wantYaw), ref yawVel, 0.12f, turnSpeed + turnSpeed, dt);
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);

            float animSpeed;
            float pace;
            if (climbPhase == 2)
            {
                float top = climb.rim.y - 0.25f;
                ClimbPitch = climbPitch;
                float y = Mathf.Min(transform.position.y + climbSpeed * dt, top);
                float wobble = Mathf.Sin(climbT * 9f) * 0.035f;
                transform.position = new Vector3(climbFoot.x, y, climbFoot.z) + Vector3.Cross(Vector3.up, into) * wobble;
                animSpeed = 2f;
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
                if (climbPhase == 1)
                {
                    float t = Mathf.Clamp01(climbT / 0.35f);
                    animSpeed = 1f;
                    ClimbPitch = climbPitch * Mathf.SmoothStep(0f, 1f, t);
                    if (climbT >= 0.35f)
                    {
                        climbPhase = 2;
                        climbT = 0f;
                    }
                }
                else if (climbPhase == 0)
                {
                    Vector3 pos = transform.position;
                    float dx = climbFoot.x - pos.x;
                    float dz = climbFoot.z - pos.z;
                    float step = walkSpeed * 1.4f * dt;
                    float dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist <= step)
                    {
                        transform.position = climbFoot;
                        climbPhase = 1;
                        climbT = 0f;
                    }
                    else
                    {
                        transform.position = transform.position + new Vector3(dx, 0f, dz).normalized * step;
                    }
                    animSpeed = 1f;
                }
                else
                {
                    float t = Mathf.Clamp01(climbT / 0.55f);
                    float rise = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI * 0.5f));
                    float across = Mathf.Min(t * t, 1f);
                    Vector3 top = climb.top;
                    transform.position = new Vector3(
                        climbFrom.x + across * (top.x - climbFrom.x),
                        Mathf.Sin(t * Mathf.PI) * 0.15f + climbFrom.y + (top.y - climbFrom.y) * rise,
                        climbFrom.z + across * (top.z - climbFrom.z));
                    float s = Mathf.SmoothStep(0f, 1f, t);
                    pace = climbSpeed / clipRunSpeed * 1.5f;
                    ClimbPitch = climbPitch * (1f - s);
                    animSpeed = 2f;
                    if (t >= 1f)
                    {
                        climb = null;
                        ClimbPitch = 0f;
                        cc.enabled = true;
                        repathAt = 0f;
                        speed = runSpeed * 0.5f;
                    }
                }
            }
            PoseClimb();
            AnimSpeed = animSpeed;
            AnimPace = Mathf.Max(0.5f, pace);
            if (!(animator != null))
            {
                return;
            }
            animator.SetFloat(SpeedId, animSpeed, 0.1f, dt);
            animator.SetFloat(PaceId, AnimPace);
            return;
        }

        if (lurking)
        {
            float pace = walkSpeed / Mathf.Max(clipWalkSpeed, 0.01f);
            lurkT += dt;
            Vector3 face = lurkInto;
            float animSpeed;
            int phase = lurkPhase;
            if (phase == 0)
            {
                Target found0;
                if (Spot(out found0))
                {
                    prey = found0;
                    hasPrey = true;
                    EndLurk(true);
                    return;
                }
                Vector3 pos = transform.position;
                float dx = lurkFoot.x - pos.x;
                float dz = lurkFoot.z - pos.z;
                float sq = dx * dx + dz * dz;
                float dist = Mathf.Sqrt(sq);
                if (dist <= cc.radius + 0.12f)
                {
                    cc.enabled = false;
                    vy = 0f;
                    lurkPhase = 1;
                    lurkT = 0f;
                    lurkFrom = transform.position;
                    animSpeed = 0f;
                }
                else
                {
                    if (lurkT > 4f)
                    {
                        EndLurk(false);
                        return;
                    }
                    vy = cc.isGrounded ? -1f : vy - gravity * dt;
                    Vector3 dir = dist <= 1E-05f ? Vector3.zero : new Vector3(dx / dist, 0f, dz / dist);
                    if (sq > 0.0001f)
                    {
                        face = new Vector3(dx, 0f, dz);
                    }
                    cc.Move((dir * walkSpeed * 1.3f + Vector3.up * vy) * dt);
                    animSpeed = 1f;
                }
            }
            else if (phase == 1)
            {
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lurkT / 0.4f));
                transform.position = Vector3.Lerp(lurkFrom, lurkFoot, Mathf.Clamp01(s));
                animSpeed = 0f;
                ClimbPitch = s * 90f;
                if (lurkT >= 0.4f)
                {
                    lurkPhase = 2;
                    lurkT = 0f;
                }
            }
            else if (phase == 2)
            {
                ClimbPitch = 90f;
                float top = lurkFoot.y + lurkHeight;
                float y = Mathf.Min(transform.position.y + climbSpeed * dt, top);
                transform.position = new Vector3(lurkFoot.x, y, lurkFoot.z);
                pace = climbSpeed / Mathf.Max(clipWalkSpeed, 0.01f);
                animSpeed = 1f;
                if (lurkFoot.y + lurkHeight <= y)
                {
                    lurkPhase = 3;
                    lurkT = 0f;
                }
            }
            else if (phase == 3)
            {
                ClimbPitch = 90f;
                Glance(dt, 1f);
                Target found3;
                if (Spot(out found3))
                {
                    prey = found3;
                    hasPrey = true;
                    Drop();
                }
                else if (lurkHold < lurkT)
                {
                    hasPrey = false;
                    Drop();
                }
                animSpeed = 0f;
            }
            else
            {
                float t = Mathf.Clamp01(lurkT / 0.45f);
                transform.position = Vector3.Lerp(lurkFrom, lurkLand, Mathf.Clamp01(t)) + Vector3.up * ((1f - t) * Mathf.Sin(t * Mathf.PI) * 0.25f);
                float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lurkT / 0.3f));
                ClimbPitch = (1f - s) * 90f;
                if (hasPrey)
                {
                    Vector3 head = prey.head.position;
                    Vector3 pos = transform.position;
                    float dx = head.x - pos.x;
                    float dz = head.z - pos.z;
                    if (dx * dx + dz * dz >= 0.0001f)
                    {
                        face = new Vector3(dx, 0f, dz);
                    }
                    else
                    {
                        face = -lurkInto;
                    }
                }
                animSpeed = 0f;
                if (t >= 1f)
                {
                    bool had = hasPrey;
                    lurking = false;
                    ClimbPitch = 0f;
                    PoseClimb();
                    cc.enabled = true;
                    vy = 0f;
                    repathAt = 0f;
                    if (had && hasPrey)
                    {
                        state = State.Chase;
                        Chasing = true;
                        listening = false;
                        stateT = 0f;
                        unreachT = 0f;
                        speed = runSpeed * 0.6f;
                        return;
                    }
                    state = State.Wander;
                    Chasing = false;
                    stateT = 0f;
                    listening = false;
                    hasPrey = false;
                    PickWander(null);
                    return;
                }
            }

            float yaw = transform.eulerAngles.y;
            float wantYaw = Quaternion.LookRotation(face, Vector3.up).eulerAngles.y;
            float newYaw = Mathf.SmoothDamp(yaw, yaw + Mathf.DeltaAngle(yaw, wantYaw), ref yawVel, 0.15f, turnSpeed + turnSpeed, dt);
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            PoseClimb();
            AnimSpeed = animSpeed;
            AnimPace = Mathf.Max(0.5f, pace);
            if (!(animator != null))
            {
                return;
            }
            animator.SetFloat(SpeedId, animSpeed, 0.1f, dt);
            animator.SetFloat(PaceId, AnimPace);
            return;
        }

        if (!gathered)
        {
            GatherTargets();
        }
        bool found;
        if (!hasPrey)
        {
            found = false;
        }
        else
        {
            found = Find(prey, out prey) /* ? */;
        }

        Vector3 goal = transform.position;
        float want = 0f;
        lookWeightWant = 0f;

        if (weeping && state != State.Kill && state != State.Recover)
        {
            goal = Weep(dt, found, ref want);
        }
        else
        {
            switch (state)
            {
                case State.Wander:
                {
                    Target spotted;
                    Target heard;
                    if (Spot(out spotted))
                    {
                        Chasing = false;
                        state = State.Notice;
                        stateT = 0f;
                        noticeFor = UnityEngine.Random.Range(0.45f, 0.8f);
                        listening = false;
                        prey = spotted;
                        hasPrey = true;
                    }
                    else if (Hear(out heard))
                    {
                        Investigate(heard);
                    }
                    else if (wait <= 0f)
                    {
                        if (wallCrawler && Time.time >= nextLurk && StartLurk())
                        {
                            return;
                        }
                        goal = wanderTarget;
                        want = walkSpeed * gait;
                        Vector3 pos = transform.position;
                        float dist = new Vector2(goal.x - pos.x, goal.z - pos.z).magnitude;
                        if (dist < 0.6f || Stuck(dt))
                        {
                            PickWander(null);
                            if (UnityEngine.Random.value < 0.2f)
                            {
                                wait = UnityEngine.Random.Range(4f, 7f);
                            }
                            else
                            {
                                wait = UnityEngine.Random.Range(1.2f, 3.5f);
                            }
                        }
                    }
                    else
                    {
                        wait -= dt;
                        Glance(dt, 0.7f);
                    }
                    break;
                }
                case State.Notice:
                {
                    if (!found)
                    {
                        state = State.Wander;
                        Chasing = false;
                        stateT = 0f;
                        listening = false;
                        hasPrey = false;
                        break;
                    }
                    goal = prey.head.position;
                    lastSeen = prey.head.position;
                    lookWant = prey.head.position;
                    lookWeightWant = 1f;
                    if (noticeFor < stateT)
                    {
                        listening = false;
                        state = State.Chase;
                        Chasing = true;
                        stateT = 0f;
                        unreachT = 0f;
                    }
                    break;
                }
                case State.Chase:
                {
                    bool keep = found;
                    if (easiestAt <= Time.time)
                    {
                        easiestAt = Time.time + 0.4f;
                        if (GoForEasiest(found))
                        {
                            keep = true;
                        }
                    }
                    bool chasing = false;
                    if (keep)
                    {
                        Vector3 head = prey.head.position;
                        Vector3 pos = transform.position;
                        float dist = new Vector2(head.x - pos.x, head.z - pos.z).magnitude;
                        if (dist <= loseRange)
                        {
                            chasing = true;
                            bool seen = CanSee(prey.head);
                            if (seen)
                            {
                                lastSeen = prey.head.position;
                            }
                            goal = Hunt(prey.head, seen);
                            want = runSpeed;
                            lookWant = prey.head.position;
                            lookWeightWant = seen ? 1f : 0.5f;
                            float wayLength;
                            bool way = Way(prey.head, out wayLength);
                            unreachT = way ? 0f : dt + unreachT;
                            if (unreachT <= giveUpSeconds)
                            {
                                if (seen && InReach(prey.head))
                                {
                                    bool grab = false;
                                    if (grabSocket != null && (prey.head.position - grabSocket.position).magnitude < grabRange)
                                    {
                                        grab = true;
                                    }
                                    else
                                    {
                                        Vector3 h = prey.head.position;
                                        Vector3 p = transform.position;
                                        float flat = new Vector2(h.x - p.x, h.z - p.z).magnitude;
                                        if (flat < Mathf.Max(grabRange * 0.6f, 0.45f))
                                        {
                                            grab = true;
                                        }
                                    }
                                    if (grab)
                                    {
                                        StartKill(prey);
                                    }
                                }
                            }
                            else
                            {
                                WalkAway(prey.head.position);
                            }
                        }
                    }
                    if (!chasing)
                    {
                        Chasing = false;
                        state = State.Search;
                        stateT = 0f;
                        searchFor = 9f;
                        listening = false;
                        trail.Clear();
                    }
                    break;
                }
                case State.Search:
                {
                    goal = lastSeen;
                    Vector3 pos = transform.position;
                    float dist = new Vector2(goal.x - pos.x, goal.z - pos.z).magnitude;
                    bool moving;
                    if (dist <= 1f)
                    {
                        want = 0f;
                        Glance(dt, 1f);
                        moving = false;
                    }
                    else
                    {
                        want = walkSpeed * 1.6f;
                        if (want <= 0f)
                        {
                            Glance(dt, 1f);
                            moving = false;
                        }
                        else
                        {
                            moving = true;
                            lookWant = lastSeen;
                            lookWeightWant = 0.6f;
                        }
                    }
                    Target spotted;
                    if (Spot(out spotted))
                    {
                        Chasing = false;
                        state = State.Notice;
                        stateT = 0f;
                        noticeFor = 0.25f;
                        listening = false;
                        prey = spotted;
                        hasPrey = true;
                        break;
                    }
                    Target heard;
                    if (Hear(out heard))
                    {
                        Investigate(heard);
                        if (state != State.Search)
                        {
                            break;
                        }
                    }
                    if (!listening)
                    {
                        if (stateT <= 4f)
                        {
                            moving = true;
                        }
                        if (searchFor < stateT || !moving)
                        {
                            state = State.Wander;
                            Chasing = false;
                            stateT = 0f;
                            listening = false;
                            hasPrey = false;
                            PickWander(null);
                        }
                    }
                    else
                    {
                        if (Time.time - listeningSince <= 12f && (moving || stateT <= 4f))
                        {
                            if (!Stuck(dt))
                            {
                                break;
                            }
                        }
                        WalkAway(lastSeen);
                        hearAt = Time.time + 25f;
                    }
                    break;
                }
                case State.Kill:
                {
                    bool over;
                    if (!killLocal)
                    {
                        over = !(stateT <= killSeconds);
                    }
                    else
                    {
                        KillCam cam = KillCam.Local;
                        over = cam == null || !cam.Busy || cam.Killer != this;
                    }
                    if (!over)
                    {
                        break;
                    }
                    if (despawnAfterKill)
                    {
                        UnityEngine.Object.Destroy(gameObject, 0f);
                        return;
                    }
                    Chasing = false;
                    state = State.Recover;
                    stateT = 0f;
                    listening = false;
                    hasPrey = false;
                    break;
                }
                case State.Recover:
                {
                    if (recoverSeconds < stateT)
                    {
                        state = State.Wander;
                        Chasing = false;
                        stateT = 0f;
                        listening = false;
                        hasPrey = false;
                        PickWander(null);
                    }
                    break;
                }
                case State.Pry:
                case State.Break:
                {
                    Target spotted;
                    bool caught = false;
                    if (Spot(out spotted) && InReach(spotted.head))
                    {
                        Vector3 h = spotted.head.position;
                        Vector3 p = transform.position;
                        float flat = new Vector2(h.x - p.x, h.z - p.z).magnitude;
                        if (flat < grabRange * 1.6f)
                        {
                            caught = true;
                        }
                    }
                    if (caught)
                    {
                        EndPry();
                        state = State.Chase;
                        Chasing = true;
                        stateT = 0f;
                        listening = false;
                        prey = spotted;
                        hasPrey = true;
                    }
                    else if (state == State.Pry)
                    {
                        goal = PryStep(dt, ref want);
                    }
                    else
                    {
                        goal = BreakStep(dt, ref want);
                    }
                    break;
                }
            }
        }

        if (Hurt != null)
        {
            want *= Hurt.Pace;
        }
        Vector3 steer = goal;
        if (state != State.Kill && want > 0f)
        {
            steer = Steer(goal);
        }
        if (want > 0f)
        {
            if (pathOk && corner < path.corners.Length)
            {
                if (path.corners[corner].y - transform.position.y > 1.2f)
                {
                    Vector3 at = transform.position;
                    MonsterClimb up = MonsterClimb.Up(at, path.corners[corner], 0.9f);
                    if (up != null)
                    {
                        StartClimb(up);
                        return;
                    }
                }
            }
            if ((int)state < 4 && state != State.Notice && doorCheckAt <= Time.time)
            {
                doorCheckAt = Time.time + 0.1f;
                float reach = 3.5f;
                if (!pries)
                {
                    reach = breaksDoors ? 2.5f : 1.6f;
                }
                GillaDoor door = DoorInWay(goal, reach);
                if (door != null)
                {
                    if (pries)
                    {
                        BeginPry(door);
                    }
                    else if (breaksDoors)
                    {
                        BeginBreak(door);
                    }
                    else if (state == State.Search)
                    {
                        state = State.Wander;
                        Chasing = false;
                        stateT = 0f;
                        listening = false;
                        hasPrey = false;
                        PickWander(null);
                    }
                    else if (state == State.Wander)
                    {
                        PickWander(null);
                        wait = UnityEngine.Random.Range(0.8f, 2f);
                    }
                    want = 0f;
                }
            }
        }

        Vector3 here = transform.position;
        float tx = steer.x - here.x;
        float tz = steer.z - here.z;
        if (tx * tx + tz * tz < 0.09f)
        {
            want = 0f;
            Vector3 p = transform.position;
            tx = goal.x - p.x;
            tz = goal.z - p.z;
        }
        float sqr = tx * tx + tz * tz;
        float angle = 0f;
        if (sqr > 0.0001f)
        {
            angle = Vector3.Angle(transform.forward, new Vector3(tx, 0f, tz));
        }
        float slow = Mathf.Clamp01((angle - 30f) / 80f);
        float accel;
        if (!weeping)
        {
            accel = speed < want ? 6f : 10f;
        }
        else
        {
            accel = 60f;
        }
        want *= slow * -0.7f + 1f;
        speed = Mathf.MoveTowards(speed, want, dt * accel);

        if (sqr > 0.0001f && state != State.Kill)
        {
            float yaw = transform.eulerAngles.y;
            float targetYaw = Mathf.Atan2(tx, tz) * Mathf.Rad2Deg;
            float newYaw = Mathf.SmoothDampAngle(yaw, targetYaw, ref yawVel, 0.15f /* ? */, turnSpeed /* ? */, dt);
            transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
        }

        vy = cc.isGrounded ? -1f : vy - dt * gravity;
        Vector3 move = transform.forward * (state != State.Kill ? speed : 0f);
        Vector3 ahead = transform.position + move * dt + transform.forward * cc.radius;
        bool stop = false;
        if (speed > 0f && MonsterNavMesh.Barred(ahead) && !MonsterNavMesh.Barred(transform.position))
        {
            stop = true;
        }
        else if (speed > 0f && GillaPitKill.InRoom(ahead) && !GillaPitKill.InRoom(transform.position))
        {
            stop = true;
        }
        if (stop)
        {
            move = Vector3.zero;
            speed = 0f;
        }
        cc.Move((move + Vector3.up * vy) * dt);

        float animSpeed2;
        float pace2;
        if (speed <= walkSpeed)
        {
            animSpeed2 = speed / walkSpeed;
            pace2 = walkSpeed / clipWalkSpeed;
        }
        else
        {
            float t = 0f;
            if (walkSpeed != runSpeed)
            {
                t = Mathf.Clamp01((speed - walkSpeed) / (runSpeed - walkSpeed));
            }
            float walkPace = walkSpeed / clipWalkSpeed;
            animSpeed2 = t + 1f;
            pace2 = walkPace + (runSpeed / clipRunSpeed - walkPace) * t;
        }
        float minPace = 0.5f;
        if (Hurt != null)
        {
            if (Hurt.Pace < 1f && speed > 0.02f && speed < walkSpeed)
            {
                minPace = 0.25f;
                pace2 = speed / Mathf.Max(clipWalkSpeed, 0.01f);
                animSpeed2 = Mathf.Min(speed / (walkSpeed * 0.25f), 1f);
            }
        }
        AnimSpeed = animSpeed2;
        AnimPace = Mathf.Max(minPace, pace2);
        if (animator != null)
        {
            animator.SetFloat(SpeedId, animSpeed2, 0.1f, dt);
            animator.SetFloat(PaceId, AnimPace);
            if (hasPry)
            {
                animator.SetBool(PryId, Prying);
            }
        }

        LookWeight = Mathf.MoveTowards(LookWeight, lookWeightWant, dt * 3f);
        if (lookWeightWant > 0f)
        {
            Vector3 from = (LookPoint - Vector3.zero).sqrMagnitude >= 9.9999994E-11f ? LookPoint : lookWant;
            float k = Mathf.Clamp01(1f - Mathf.Exp(dt * -8f));
            LookPoint = new Vector3(
                from.x + (lookWant.x - from.x) * k,
                from.y + (lookWant.y - from.y) * k,
                from.z + (lookWant.z - from.z) * k);
        }
    }
}
