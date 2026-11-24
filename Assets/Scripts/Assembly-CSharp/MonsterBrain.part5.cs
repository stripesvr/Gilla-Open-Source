using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Fusion;

public partial class MonsterBrain
{
    public void OnBite()
    {
        if (KillCam.Local != null && KillCam.Local.Killer == this)
        {
            KillCam.Local.Bite();
        }
        GillaDummy.BittenBy(this, false);
    }

    public void OnTear()
    {
        if (KillCam.Local != null && KillCam.Local.Killer == this)
        {
            KillCam.Local.TearArm((ApePart)2);
        }
        GillaDummy.BittenBy(this, true);
    }

    private void Look(Vector3 at, float weight)
    {
        lookWant = at;
        lookWeightWant = weight;
    }

    private void Glance(float dt, float weight)
    {
        if (nextGlance <= Time.time)
        {
            nextGlance = Time.time + UnityEngine.Random.Range(1.2f, 3f);
            float pitch = UnityEngine.Random.Range(-10f, 15f);
            float yaw = UnityEngine.Random.Range(-75f, 75f);
            Vector3 dir = Quaternion.Euler(pitch, yaw, 0f) * transform.forward;
            Vector3 from = face != null ? face.position : transform.position + Vector3.up;
            lookWant = from + dir * 4f;
        }
        lookWeightWant = weight;
    }

    private Vector3 Hunt(Transform head, bool sees)
    {
        Vector3 at = head.position;
        if (sees)
        {
            trail.Clear();
            trail.Add(at);
            return at;
        }
        if (trail.Count == 0)
        {
            trail.Add(at);
        }
        else
        {
            Vector3 last = trail[trail.Count - 1];
            float dx = at.x - last.x;
            float dz = at.z - last.z;
            if (trailSpacing < Mathf.Sqrt(dx * dx + dz * dz))
            {
                trail.Add(at);
            }
        }
        if (trail.Count > 400)
        {
            trail.RemoveAt(0);
        }
        Vector3 pos = transform.position;
        float eye = 1f;
        if (cc != null)
        {
            eye = cc.height * 0.6f;
        }
        int index = trail.Count - 1;
        if (0 < index)
        {
            int tries = Mathf.Min(index - 1, 7) + 1;
            for (; tries != 0; tries--, index--)
            {
                if (Clear(pos + Vector3.up * eye, trail[index]))
                {
                    trail.RemoveRange(0, index);
                    break;
                }
            }
        }
        while (trail.Count >= 2)
        {
            Vector3 first = trail[0];
            Vector3 me = transform.position;
            float dx = first.x - me.x;
            float dz = first.z - me.z;
            if (0.7f <= Mathf.Sqrt(dx * dx + dz * dz))
            {
                break;
            }
            trail.RemoveAt(0);
        }
        return trail[0];
    }

    private Vector3 Steer(Vector3 goal)
    {
        NavMeshHit from;
        NavMeshHit to;
        if (!MonsterNavMesh.Ready)
        {
            return goal;
        }
        if (repathAt <= Time.time || (goal - pathGoal).sqrMagnitude > 4f)
        {
            pathGoal = goal;
            repathAt = Time.time + 0.25f;
            bool ends;
            if (NavMesh.SamplePosition(transform.position, out from, 1.5f, -1)
                && NavMesh.SamplePosition(goal, out to, 3f, -1)
                && NavMesh.CalculatePath(from.position, to.position, -1, path))
            {
                int n = path.corners.Length;
                ends = n < 2;
                pathOk = 1 < n;
            }
            else
            {
                ends = false;
                pathOk = false;
            }
            pathEnds = ends;
            corner = 1;
        }
        if (pathEnds)
        {
            return transform.position;
        }
        if (!pathOk)
        {
            return goal;
        }
        Vector3[] corners = path.corners;
        while (corner < corners.Length - 1)
        {
            Vector3 c = corners[corner];
            Vector3 me = transform.position;
            float dx = c.x - me.x;
            float dz = c.z - me.z;
            if (0.5f <= Mathf.Sqrt(dx * dx + dz * dz))
            {
                break;
            }
            corner++;
        }
        Vector3 cur = corners[corner];
        Vector3 pos = transform.position;
        float ddx = cur.x - pos.x;
        float ddz = cur.z - pos.z;
        float dist = Mathf.Sqrt(ddx * ddx + ddz * ddz);
        if (1.4f <= dist || corners.Length - 1 <= corner)
        {
            return corners[corner];
        }
        float t = Mathf.Clamp01(dist / -1.4f + 1f);
        return Vector3.LerpUnclamped(corners[corner], corners[corner + 1], t);
    }

    private bool StoodTooLong(float dt)
    {
        if (((int)state & ~1) != 4 && !lurking)
        {
            if ((transform.position - stoodAt).sqrMagnitude <= 1f)
            {
                stoodT += dt;
                if (stoodT < 30f)
                {
                    return false;
                }
                stoodT = 0f;
                Debug.LogWarning(string.Format("[MonsterBrain] {0} stood at {1:F1} for {2:0} s ({3}{4}): sending it off",
                    name, transform.position, 30f, state, climb != null ? ", climbing" : ""));
                if (((int)state & ~1) == 6)
                {
                    EndPry();
                }
                climb = null;
                ClimbPitch = 0f;
                state = State.Wander;
                Chasing = false;
                trail.Clear();
                stateT = 0f;
                unreachT = 0f;
                listening = false;
                hasPrey = false;
                wait = 0f;
                ignoreT = walkAwaySeconds;
                PickWander(null);
                return true;
            }
        }
        stoodAt = transform.position;
        stoodT = 0f;
        return false;
    }

    private bool Reachable(Transform head)
    {
        float length;
        return Way(head, out length);
    }

    private bool Way(Transform head, out float length)
    {
        ValueTuple<float, bool, float> cached;
        if (ways.TryGetValue(head, out cached) && Time.time < cached.Item1)
        {
            length = cached.Item3;
            return cached.Item2;
        }
        Vector3 headPos = head.position;
        RaycastHit hit;
        Vector3 floor;
        if (Physics.Raycast(head.position, Vector3.down, out hit, 8f, Solid, QueryTriggerInteraction.Ignore))
        {
            floor = hit.point;
        }
        else
        {
            floor = headPos + Vector3.down * 1.5f;
        }
        Vector3 hp = head.position;
        Vector3 me = transform.position;
        float dx = hp.x - me.x;
        float dz = hp.z - me.z;
        length = Mathf.Sqrt(dx * dx + dz * dz);
        bool ok;
        if (MonsterNavMesh.Ready)
        {
            NavMeshHit end;
            NavMeshHit start;
            ok = false;
            if (NavMesh.SamplePosition(floor, out end, 1f, -1) && Mathf.Abs(end.position.y - floor.y) < 0.6f
                && NavMesh.SamplePosition(transform.position, out start, 1.5f, -1)
                && NavMesh.CalculatePath(start.position, end.position, -1, scratch)
                && scratch.status == NavMeshPathStatus.PathComplete)
            {
                if (pries || breaksDoors || !ShutDoorOn(scratch.corners))
                {
                    length = 0f;
                    Vector3[] corners = scratch.corners;
                    ok = true;
                    for (int i = 1; i < corners.Length; i++)
                    {
                        length += Vector3.Distance(corners[i], corners[i - 1]);
                    }
                }
            }
        }
        else
        {
            ok = floor.y - transform.position.y < 1.2f;
        }
        if (ways.Count > 32)
        {
            ways.Clear();
        }
        ways[head] = new ValueTuple<float, bool, float>(Time.time + 0.3f, ok, length);
        return ok;
    }

    private void WalkAway(Vector3 you)
    {
        state = State.Wander;
        Chasing = false;
        stateT = 0f;
        listening = false;
        hasPrey = false;
        unreachT = 0f;
        ignoreT = walkAwaySeconds;
        trail.Clear();
        wait = 0f;
        PickWander(new Vector3?(you));
    }

    private bool CanSee(Transform head)
    {
        Vector3 eye = face != null ? face.position : transform.position + Vector3.up;
        if (sightRange < Vector3.Distance(head.position, eye))
        {
            return false;
        }
        return Clear(eye, head.position);
    }

    private static bool Clear(Vector3 a, Vector3 b)
    {
        if (Physics.Linecast(a, b, Solid, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        return !Physics.Linecast(b, a, Solid, QueryTriggerInteraction.Ignore);
    }

    private bool InReach(Transform head)
    {
        float dy = head.position.y - transform.position.y;
        if (dy <= -reachBelow)
        {
            return false;
        }
        return dy < reachAbove;
    }

    private void PickWander()
    {
        PickWander(null);
    }

    private void PickWander(Vector3? from)
    {
        stuckAt = transform.position;
        stuckT = 0f;
        if (MonsterNavMesh.Ready)
        {
            NavMeshHit start;
            if (NavMesh.SamplePosition(transform.position, out start, 1.5f, -1))
            {
                for (int i = 12; i != 0; i--)
                {
                    Vector2 r = UnityEngine.Random.insideUnitCircle;
                    float radius = wanderRadius;
                    Vector3 p = transform.position;
                    Vector3 probe = new Vector3(r.x * radius + p.x, p.y + 0f, r.y * radius + p.z);
                    NavMeshHit hit;
                    if (!NavMesh.SamplePosition(probe, out hit, 3f, -1))
                    {
                        continue;
                    }
                    if (from.HasValue)
                    {
                        Vector3 f = from.Value;
                        Vector3 me = transform.position;
                        Vector3 f2 = from.Value;
                        float mx = me.x - f2.x;
                        float mz = me.z - f2.z;
                        float hx = hit.position.x - f.x;
                        float hz = hit.position.z - f.z;
                        if (Mathf.Sqrt(hx * hx + hz * hz) < Mathf.Sqrt(mx * mx + mz * mz) + 6f)
                        {
                            continue;
                        }
                    }
                    if (NavMesh.CalculatePath(start.position, hit.position, -1, scratch)
                        && scratch.status == NavMeshPathStatus.PathComplete)
                    {
                        if (!pries && !breaksDoors && ShutDoorOn(scratch.corners))
                        {
                            continue;
                        }
                        wanderTarget = hit.position;
                        return;
                    }
                }
            }
        }
        Vector2 rr = UnityEngine.Random.insideUnitCircle;
        wanderTarget = new Vector3(home.x + rr.x * wanderRadius, home.y + 0f, rr.y * wanderRadius + home.z);
    }

    private bool ThroughDoors
    {
        get
        {
            if (pries)
            {
                return true;
            }
            return breaksDoors;
        }
    }

    private float NeedGap
    {
        get
        {
            if (cc != null)
            {
                return cc.height + 0.1f;
            }
            return 2.1f;
        }
    }

    private GillaDoor DoorInWay(Vector3 goal, float ahead)
    {
        GillaDoor[] doors = UnityEngine.Object.FindObjectsByType<GillaDoor>(FindObjectsSortMode.None); /* ? */
        if (((IReadOnlyCollection<GillaDoor>)doors).Count == 0)
        {
            return null;
        }
        float need = NeedGap;
        Vector3 from = transform.position;
        Vector3[] corners;
        int i;
        if (!pathOk)
        {
            corners = null;
            i = 0;
        }
        else
        {
            corners = path.corners;
            i = pathOk ? corner : 0;
        }
        if (0f < ahead)
        {
            float gone = 0f;
            do
            {
                Vector3 to;
                if (corners == null)
                {
                    to = goal;
                    if (0 < i)
                    {
                        return null;
                    }
                }
                else
                {
                    if (corners.Length <= i)
                    {
                        return null;
                    }
                    to = corners[i];
                }
                float leg = Mathf.Sqrt((to.z - from.z) * (to.z - from.z) + (to.x - from.x) * (to.x - from.x));
                foreach (GillaDoor door in (IEnumerable<GillaDoor>)doors)
                {
                    if (door == null || door.keycardOnly || door.modeMap || need <= door.Gap)
                    {
                        continue;
                    }
                    float t;
                    if (Crosses(door, from, to, out t) && gone + leg * t < ahead)
                    {
                        return door;
                    }
                }
                gone += leg;
                i++;
                from = to;
            } while (gone < ahead);
        }
        return null;
    }

    private bool ShutDoorOn(Vector3[] corners)
    {
        float need = NeedGap;
        if (corners.Length < 2)
        {
            return false;
        }
        GillaDoor[] doors = UnityEngine.Object.FindObjectsByType<GillaDoor>(FindObjectsSortMode.None); /* ? */
        for (int i = 1; i < corners.Length; i++)
        {
            foreach (GillaDoor door in (IEnumerable<GillaDoor>)doors)
            {
                if (door != null && door.Gap < need)
                {
                    float t;
                    if (Crosses(door, corners[i - 1], corners[i], out t))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool Crosses(GillaDoor d, Vector3 a, Vector3 b, out float t)
    {
        t = 0f;
        Vector3 foot = d.Foot;
        Vector3 n = d.Through;
        float sa = Vector3.Dot(Flat(a - foot), n);
        float sb = Vector3.Dot(Flat(b - foot), n);
        if (sa == sb || sa * sb > 0f)
        {
            return false;
        }
        t = sa / (sa - sb);
        float k = Mathf.Clamp01(t);
        Vector3 side = Vector3.Cross(n, Vector3.up);
        Vector3 p = Vector3.LerpUnclamped(a, b, k);
        if (Mathf.Abs(Vector3.Dot(Flat(p - foot), side)) <= d.Width * 0.5f)
        {
            return Mathf.Abs(p.y - foot.y) < 1.2f;
        }
        return false;
    }

    private void BeginPry(GillaDoor door)
    {
        State from = state;
        state = State.Pry;
        stateT = 0f;
        listening = false;
        pryFrom = from;
        Chasing = from == State.Chase;
        PryDoor = door;
        Prying = false;
        pryLifted = false;
        pryT = 0f;
    }

    private Vector3 PryStep(float dt, ref float want)
    {
        GillaDoor door = PryDoor;
        if (door == null)
        {
            EndPry();
            return transform.position;
        }
        float need = NeedGap;
        if (!Prying)
        {
            Vector3 foot = door.Foot;
            Vector3 through = door.Through;
            Vector3 me = transform.position;
            if (Vector3.Dot(Flat(me - foot), through) >= 0f)
            {
                through = -through;
            }
            float thickness = door.Thickness;
            float stand = pryStand;
            float y = transform.position.y;
            if (need <= door.Gap && !door.IsMoving)
            {
                EndPry();
                return transform.position;
            }
            stand = thickness * 0.5f + stand;
            Vector3 pos = transform.position;
            float sx = foot.x - through.x * stand;
            float sz = foot.z - through.z * stand;
            Vector3 to = new Vector3(sx - pos.x, 0f, sz - pos.z);
            float dist = to.magnitude;
            if (0.35f < dist)
            {
                float k = 0f;
                if (pryFrom == State.Chase)
                {
                    k = Mathf.Clamp01((dist - 0.35f) / 1.5f);
                }
                float slow = walkSpeed * 1.3f;
                want = slow + (runSpeed - slow) * k;
                return new Vector3(sx, y, sz);
            }
            cc.Move(Vector3.ClampMagnitude(to, walkSpeed * dt));
            Vector3 fwd = Flat(transform.forward);
            if (Vector3.Angle(through, fwd) < 15f)
            {
                if (!door.IsMoving && !door.IsOpen)
                {
                    pryT = 0f;
                    Prying = true;
                    prySpot = new Vector3(sx, y, sz);
                    pryToward = through;
                }
            }
            return new Vector3(sx, y, sz) + through * 3f;
        }
        pryT += dt;
        Vector3 aim = prySpot + pryToward * 3f;
        if (!pryLifted)
        {
            if (pryLiftSeconds <= pryT)
            {
                door.Pry();
                pryLifted = true;
            }
            else if (door.IsOpen)
            {
                EndPry();
                return aim;
            }
        }
        if (pryT < pryDoneSeconds || door.Gap < need)
        {
            if (pryDoneSeconds + 4f < pryT)
            {
                EndPry();
                PickWander(null);
            }
            return aim;
        }
        if (pryFrom == State.Chase)
        {
            bool had = hasPrey;
            EndPry();
            speed = had ? runSpeed : runSpeed * 0.6f;
        }
        else
        {
            EndPry();
            speed = runSpeed * 0.6f;
        }
        return aim;
    }

    private void EndPry()
    {
        Prying = false;
        PryDoor = null;
        stuckAt = transform.position;
        stuckT = 0f;
        if (pryFrom == State.Search)
        {
            state = State.Search;
            Chasing = false;
            stateT = 0f;
            searchFor = 9f;
            listening = false;
        }
        else if (pryFrom == State.Chase && hasPrey)
        {
            listening = false;
            state = State.Chase;
            Chasing = true;
            stateT = 0f;
            unreachT = 0f;
        }
        else
        {
            state = State.Wander;
            Chasing = false;
            stateT = 0f;
            listening = false;
            hasPrey = false;
        }
    }

    private void BeginBreak(GillaDoor door)
    {
        State from = state;
        state = State.Break;
        stateT = 0f;
        listening = false;
        pryFrom = from;
        Chasing = from == State.Chase;
        PryDoor = door;
        breakT = 0f;
    }

    private Vector3 BreakStep(float dt, ref float want)
    {
        GillaDoor door = PryDoor;
        if (door == null)
        {
            EndPry();
            return transform.position;
        }
        if (NeedGap <= door.Gap && !door.IsMoving)
        {
            EndPry();
            return transform.position;
        }
        Vector3 foot = door.Foot;
        Vector3 through = door.Through;
        Vector3 me = transform.position;
        float stand = breakStand;
        if (Vector3.Dot(Flat(me - foot), through) >= 0f)
        {
            through = -through;
        }
        float thickness = door.Thickness;
        float y = transform.position.y;
        stand = thickness * 0.5f + stand;
        Vector3 pos = transform.position;
        float sx = foot.x - through.x * stand;
        float sz = foot.z - through.z * stand;
        float dx = sx - pos.x;
        float dz = sz - pos.z;
        if (0.3f < Mathf.Sqrt(dx * dx + dz * dz) && breakT <= 0f)
        {
            want = pryFrom == State.Chase ? runSpeed : walkSpeed * 1.3f;
            return new Vector3(sx, y, sz);
        }
        breakT += dt;
        Vector3 aim = new Vector3(sx, y, sz) + through + through;
        if (breakSeconds <= breakT)
        {
            if (door.Blow(through))
            {
                if (pryFrom == State.Chase)
                {
                    bool had = hasPrey;
                    EndPry();
                    speed = had ? runSpeed : runSpeed * 0.6f;
                }
                else
                {
                    EndPry();
                    speed = runSpeed * 0.6f;
                }
                return aim;
            }
        }
        if (breakSeconds + 3f < breakT)
        {
            EndPry();
            PickWander(null);
        }
        return aim;
    }

    private bool Stuck(float dt)
    {
        if ((transform.position - stuckAt).sqrMagnitude <= 0.25f)
        {
            float t = stuckT + dt;
            bool stuck = 3f <= t;
            stuckT = stuck ? 0f : t;
            return stuck;
        }
        stuckAt = transform.position;
        stuckT = 0f;
        return false;
    }

    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}
