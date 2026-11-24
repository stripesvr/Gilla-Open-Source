// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Reflection;
using UnityEngine;

public partial class MonsterAvatar : MonoBehaviour
{
    private void Bases()
    {
        baseLater = true;
        if (anim == null) return;
        if (anim.runtimeAnimatorController == null) return;
        if (!anim.isActiveAndEnabled) return;
        if (skull == null) return;
        if (neck == null) return;
        if (chest == null) return;

        bool fire = anim.fireEvents;
        anim.fireEvents = false;
        anim.Rebind();
        anim.Update(0f);
        neckBase = new Quaternion[3];
        skullBase = new Quaternion[3];
        for (int s = 0; s < 3; s++)
        {
            anim.SetFloat(SpeedId, (float)s);
            anim.SetFloat(PaceId, 1f);
            anim.Play(0, 0, 0f);
            anim.Update(0f);
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            float length = info.length;
            int samples = s == 0 ? 1 : 8;
            if (length <= 0.01f) length = 0.01f;
            Vector4 neckSum = Vector4.zero;
            Vector4 skullSum = Vector4.zero;
            for (int i = 0; i < samples; i++)
            {
                if (i != 0)
                {
                    anim.Update(length / (float)samples);
                }
                Add(ref neckSum, neck.localRotation);
                Add(ref skullSum, skull.localRotation);
            }
            neckBase[s] = Mean(neckSum);
            skullBase[s] = Mean(skullSum);
        }
        anim.SetFloat(SpeedId, 0f);
        anim.SetFloat(PaceId, 1f);
        anim.Play(0, 0, 0f);
        anim.Update(0f);
        anim.fireEvents = fire;
        HeadStands = "its moves";
        baseLater = false;

        static void Add(ref Vector4 sum, Quaternion q)
        {
            if ((sum - Vector4.zero).sqrMagnitude >= 9.9999994e-11f &&
                q.w * sum.w + q.z * sum.z + q.x * sum.x + q.y * sum.y < 0f)
            {
                q.x = -q.x;
                q.y = -q.y;
                q.z = -q.z;
                q.w = -q.w;
            }
            sum.x = q.x + sum.x;
            sum.y = q.y + sum.y;
            sum.z = q.z + sum.z;
            sum.w = q.w + sum.w;
        }

        static Quaternion Mean(Vector4 sum)
        {
            float sq = sum.w * sum.w + sum.z * sum.z + sum.x * sum.x + sum.y * sum.y;
            if (sq >= 1e-12f)
            {
                float len = Mathf.Sqrt(sq);
                if (len > 1e-05f)
                {
                    return new Quaternion(sum.x / len, sum.y / len, sum.z / len, sum.w / len);
                }
                return new Quaternion(0f, 0f, 0f, 0f);
            }
            return Quaternion.identity;
        }
    }

    private void Stomps(float dt)
    {
        for (int i = 0; i < 2; i++)
        {
            Transform hand = i == 0 ? leftHand : rightHand;
            if (hand == null) continue;
            Vector3 pos = hand.position;
            float v;
            if (!handsKnown)
            {
                v = 0f;
            }
            else
            {
                v = Vector3.Distance(pos, handWas[i]) / dt;
            }
            float peak = v;
            if (v <= handSpeed[i])
            {
                peak = handSpeed[i];
            }
            handWas[i] = pos;
            handSpeed[i] = v;
            stompIn[i] = stompIn[i] - dt;
            bool hit;
            if (v <= 30f)
            {
                hit = Physics.Raycast(pos, Vector3.down, 0.18f, Floor, QueryTriggerInteraction.Ignore);
                if (hit)
                {
                    if (!onFloor[i] && stompIn[i] <= 0f && peak >= 0.6f && offFloor[i] >= 0.1f)
                    {
                        float k = peak / 5f;
                        float mul = 0.6f;
                        if (k >= 0f)
                        {
                            mul = 1f;
                            if (k <= 1f) mul = k * 0.39999998f + 0.6f;
                        }
                        typeof(MonsterAnimEvents).GetMethod("Step", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Vector3), typeof(float) }, null)?.Invoke(events, new object[] { pos, events.footstepVolume * mul }); /* ? */
                        stompIn[i] = 0.12f;
                        if (Stomped != null)
                        {
                            Stomped();
                        }
                    }
                    offFloor[i] = 0f;
                }
                else
                {
                    offFloor[i] = offFloor[i] + dt;
                }
            }
            else
            {
                hit = false;
            }
            onFloor[i] = hit;
        }
        handsKnown = true;
    }

    private void Steps(float walk, float dt)
    {
        anim.SetFloat(SpeedId, 1f);
        float r = speed / walk;
        float target;
        if (r < 0f || (r <= 2.5f && r < 0.3f))
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            float t = info.normalizedTime;
            t = t + (float)(int)(t + t) * -0.5f;
            float c = Mathf.Clamp(t, 0f, 0.5f);
            target = (c >= 0.47f || c <= 0.03f) ? 0f : 0.6f;
        }
        else
        {
            target = r > 2.5f ? 2.5f : r;
        }
        pace = Mathf.MoveTowards(pace, target, dt * 4f);
        anim.SetFloat(PaceId, pace);
    }

    private static void Copy(HandPoser from, HandPoser to)
    {
        if (from == null) return;
        if (to == null) return;
        to.trigger = from.trigger;
        to.grip = from.grip;
        to.thumbPress = from.thumbPress;
    }

    private static Quaternion Turned(Transform hand, Transform index1, Transform pinkie1, Vector3 palmLocal, Quaternion aim, float palmSide)
    {
        if (index1 != null && pinkie1 != null && palmLocal.sqrMagnitude >= 1e-06f)
        {
            Vector3 mid = (index1.position + pinkie1.position) * 0.5f - hand.position;
            Vector3 palm = hand.TransformDirection(palmLocal);
            if (mid.sqrMagnitude >= 1e-08f)
            {
                Vector3 forward = Quaternion.AngleAxis(60f, aim * Vector3.right) * (aim * Vector3.forward);
                Vector3 sideDir = Vector3.ProjectOnPlane(aim * Vector3.right * palmSide, forward);
                palm = Vector3.ProjectOnPlane(palm, mid);
                if (sideDir.sqrMagnitude < 1e-06f || palm.sqrMagnitude < 1e-06f)
                {
                    return hand.rotation;
                }
                Quaternion want = Quaternion.LookRotation(forward, sideDir);
                Quaternion have = Quaternion.LookRotation(mid, palm);
                return want * Quaternion.Inverse(have) * hand.rotation; /* ? */
            }
        }
        return hand.rotation;
    }

    private static Vector3 WristFor(Vector3 at, Transform hand, Transform index1, Transform pinkie1, Quaternion turned)
    {
        if (index1 == null || pinkie1 == null) return at;
        Vector3 mid = (index1.position + pinkie1.position) * 0.5f - hand.position;
        Vector3 local = Quaternion.Inverse(hand.rotation) * mid;
        Vector3 off = turned * local;
        return at - off * 0.5f;
    }

    private static int Along(Transform bone, Transform child)
    {
        Vector3 d = bone.InverseTransformDirection(child.position - bone.position);
        float ax = Mathf.Abs(d.x);
        float ay = Mathf.Abs(d.y);
        float az = Mathf.Abs(d.z);
        int axis = az < ay ? 1 : 2;
        if (az < ax && ax > ay)
        {
            axis = 0;
        }
        return axis;
    }

    private static Vector3 ScaleAlong(Vector3 scale, int axis, float s)
    {
        scale[axis] = scale[axis] * s;
        return scale;
    }

    private static void Stretch(Transform up, Transform lo, Transform end, Vector3 target, Vector3 restUp, Vector3 restLo, int axisUp, int axisLo, float w)
    {
        if (up == null) return;
        if (lo == null) return;
        if (end == null) return;
        up.localScale = restUp;
        lo.localScale = restLo;
        float a = Vector3.Distance(lo.position, up.position);
        float b = Vector3.Distance(end.position, lo.position);
        float d = Vector3.Distance(target, up.position);
        if (a < 0.0001f) return;
        if (d <= (a + b) * 0.98f) return;
        float f = Mathf.Clamp((d - b) / (a * 0.98f), 1f, 1.33f);
        f = (f - 1f) * w + 1f;
        up.localScale = ScaleAlong(restUp, axisUp, f);
        lo.localScale = ScaleAlong(restLo, axisLo, 1f / f);
    }

    private static void Reach(Transform up, Transform lo, Transform end, Vector3 target, Vector3 side, Vector3 back, float w)
    {
        if (up == null) return;
        if (lo == null) return;
        if (end == null) return;
        Vector3 upPos = up.position;
        float a = Vector3.Distance(lo.position, upPos);
        float b = Vector3.Distance(end.position, lo.position);
        Vector3 toTarget = target - upPos;
        float dist = toTarget.magnitude;
        float reach = 0.01f;
        if (dist >= 0.01f)
        {
            reach = dist;
            float max = (a + b) * 0.999f;
            if (max < dist) reach = max;
        }
        Vector3 dir = toTarget.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(side * 0.5f + back * 0.6f + Vector3.down * 0.6f, dir);
        if (bend.sqrMagnitude <= 1e-06f)
        {
            bend = Vector3.down;
        }
        else
        {
            bend = bend.normalized;
        }
        float along = (a * a - b * b + reach * reach) / (reach + reach);
        float h = Mathf.Sqrt(Mathf.Max(a * a - along * along, 0f));
        Quaternion upRot = up.rotation;
        Vector3 elbow = upPos + dir * along + bend * h;
        Quaternion turnUp = Quaternion.FromToRotation(lo.position - upPos, elbow - upPos);
        up.rotation = Quaternion.Slerp(upRot, turnUp * upRot, w);
        Quaternion loRot = lo.rotation;
        Quaternion turnLo = Quaternion.FromToRotation(end.position - lo.position, upPos + dir * reach - lo.position);
        lo.rotation = Quaternion.Slerp(loRot, turnLo * loRot, w);
    }

    public MonsterAvatar()
    {
        sizeBy = 1f;
        HeadStands = "";
        onFloor = new bool[2];
        handWas = new Vector3[2];
        handSpeed = new float[2];
        offFloor = new float[2];
        stompIn = new float[2];
        neckShare = Quaternion.identity;
        skullOnFace = Quaternion.identity;
    }

    static MonsterAvatar()
    {
        SpeedId = Animator.StringToHash("Speed");
        PaceId = Animator.StringToHash("Pace");
    }

    private HandPoser.Finger Poser_g__Chain_82_0(string name, string side)
    {
        HandPoser.Finger finger = new HandPoser.Finger();
        Transform[] bones = new Transform[3];
        bones[0] = Bone(name + "1" + side);
        bones[1] = Bone(name + "2" + side);
        bones[2] = Bone(name + "3" + side);
        finger.bones = bones;
        return finger;
    }
}
