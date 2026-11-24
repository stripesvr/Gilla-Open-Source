// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

public partial class MonsterPlayer : MonoBehaviour
{
    private float TouchFrom(Component body, bool right)
    {
        HandPoints(right, touching);
        if (touching == null || body == null || touching.Count == 0)
        {
            return float.PositiveInfinity;
        }
        return GillaSkin.Nearest(body, touching);
    }

    private bool WouldTakeHold(Dismemberment body, Transform left, Transform right, bool rightHand)
    {
        HandPoints(rightHand, touching);
        if (Local != null)
        {
            if (Walled(Local.head.position, touching[0]))
            {
                return false;
            }
        }
        List<Vector3> points = touching;
        float near = skinTouch * Mathf.Max(transform.localScale.x, 0.4f);
        foreach (Vector3 p in points)
        {
            if (left != null && Vector3.Distance(left.position, p) < near)
            {
                return true;
            }
            if (right != null && Vector3.Distance(right.position, p) < near)
            {
                return true;
            }
        }
        if (body == null)
        {
            return false;
        }
        float d = GillaSkin.Nearest(body, points);
        if (d != float.PositiveInfinity)
        {
            return d < near;
        }
        GillaWounds wounds = GillaWounds.On(body);
        if (wounds == null)
        {
            return false;
        }
        foreach (Vector3 p in points)
        {
            if (wounds.Hit(p, near, Vector3.zero, out _, out _))
            {
                return true;
            }
        }
        return false;
    }

    // NetworkApe.limp / NetworkApe.monsterBody are not accessible from here; read them by name.
    private static T NonPublic<T>(object o, string name)
    {
        if (o == null)
        {
            return default(T);
        }
        Type type = o.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        while (type != null)
        {
            FieldInfo f = type.GetField(name, flags);
            if (f != null)
            {
                object v = f.GetValue(o);
                return v is T t ? t : default(T);
            }
            PropertyInfo pi = type.GetProperty(name, flags);
            if (pi != null)
            {
                object v = pi.GetValue(o);
                return v is T t ? t : default(T);
            }
            type = type.BaseType;
        }
        return default(T);
    }

    private static NetworkApe NearestBody(Vector3 at, float reach)
    {
        if (Local != null)
        {
            if (Walled(Local.head.position, at))
            {
                return null;
            }
        }
        NetworkApe best = null;
        float bestD = reach;
        foreach (NetworkApe a in NetworkApe.All)
        {
            if (!CanTouch(a))
            {
                continue;
            }
            if (a.Busy && !a.LetGoHere && !a.DownHere)
            {
                continue;
            }
            Vector3 headPos = a.head.position;
            Vector3 floor = FloorUnder(headPos);
            float y = floor.y + 0.05f;
            if (y <= at.y)
            {
                y = at.y;
                if (headPos.y < at.y)
                {
                    y = headPos.y;
                }
            }
            Vector3 point = new Vector3(headPos.x, y, headPos.z);
            float d = Mathf.Max(Vector3.Distance(at, point) - 0.12f, 0f);
            object limp = NonPublic<object>(a, "limp"); /* ? */
            if (limp is UnityEngine.Object ul ? ul != null : limp != null)
            {
                if (NonPublic<bool>(limp, "IsDown")) /* ? */
                {
                    Vector3 middle = NonPublic<Vector3>(limp, "Middle"); /* ? */
                    float dm = Mathf.Max(Vector3.Distance(middle, at) - 0.12f, 0f);
                    if (dm < d)
                    {
                        point = middle;
                        d = Mathf.Max(Vector3.Distance(point, at) - 0.12f, 0f);
                    }
                }
            }
            Transform[] hands = new Transform[] { a.leftHand, a.rightHand };
            for (int i = 0; i < hands.Length; i++)
            {
                Transform t = hands[i];
                if (t == null)
                {
                    continue;
                }
                if (Vector3.Distance(t.position, at) < d)
                {
                    Vector3 tp = t.position;
                    point = t.position;
                    d = Vector3.Distance(tp, at);
                }
            }
            if (d < bestD)
            {
                if (!Walled(at, point))
                {
                    best = a;
                    bestD = d;
                }
            }
        }
        return best;
    }

    private static bool Walled(Vector3 from, Vector3 to)
    {
        return Physics.Linecast(from, to, Floor, QueryTriggerInteraction.Ignore);
    }

    public static float RigOf(NetworkApe who)
    {
        float size = 1f;
        object monsterBody = NonPublic<object>(who, "monsterBody"); /* ? */
        if (monsterBody is UnityEngine.Object um ? um != null : monsterBody != null)
        {
            MonsterCatalog.Entry e = NonPublic<MonsterCatalog.Entry>(monsterBody, "entry"); /* ? */
            if (e != null)
            {
                if (!e.legs)
                {
                    float eyes = 0.5f;
                    if (Local != null)
                    {
                        eyes = Local.apeEyeHeight;
                    }
                    float armReach;
                    if (Local != null)
                    {
                        armReach = Local.apeReach;
                    }
                    else
                    {
                        armReach = 0.6f;
                    }
                    float r;
                    if (!e.hangs || e.reach <= 0f)
                    {
                        r = e.eyeHeight / eyes;
                    }
                    else
                    {
                        r = e.reach / armReach;
                    }
                    size = Mathf.Clamp(r, 0.2f, 3.5f);
                }
                else
                {
                    size = Mathf.Clamp(e.eyeHeight / 1.5f, 0.5f, 2f);
                }
                size *= NonPublic<float>(monsterBody, "sizeBy"); /* ? */
            }
        }
        return size;
    }

    private static bool Part(NetworkApe who, Vector3 hand, float reach, out ApePart part, out float away)
    {
        part = ApePart.Head;
        away = reach;
        (ApePart, Transform)[] parts = new (ApePart, Transform)[]
        {
            (ApePart.Head, who.head),
            (ApePart.ArmL, who.leftHand),
            (ApePart.ArmR, who.rightHand)
        };
        bool found = false;
        for (int i = 0; i < parts.Length; i++)
        {
            ApePart p = parts[i].Item1;
            Transform t = parts[i].Item2;
            if (t == null || who.IsTorn((int)p))
            {
                continue;
            }
            float d = Vector3.Distance(t.position, hand);
            if (d < away)
            {
                found = true;
                away = d;
                part = p;
            }
        }
        return found;
    }

    private static Vector3 FloorUnder(Vector3 from)
    {
        RaycastHit hit;
        if (Physics.Raycast(from + Vector3.up * 0.1f, Vector3.down, out hit, 8f, Floor, QueryTriggerInteraction.Ignore))
        {
        }
        else
        {
            hit.point = from + Vector3.down * 1.5f;
        }
        return hit.point;
    }

    private static bool Grip(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool result = false;
        if (device.isValid)
        {
            float value;
            if (device.TryGetFeatureValue(CommonUsages.grip, out value))
            {
                result = 0.7f < value;
            }
            else
            {
                result = false;
            }
        }
        return result;
    }

    private static bool Tracked(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool result = false;
        if (device.isValid)
        {
            bool tracked;
            bool got = device.TryGetFeatureValue(CommonUsages.isTracked, out tracked);
            result = tracked || !got;
        }
        return result;
    }

    private static bool Button(XRNode node, InputFeatureUsage<bool> usage)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool result = false;
        if (device.isValid)
        {
            bool pressed;
            result = device.TryGetFeatureValue(usage, out pressed) && pressed;
        }
        return result;
    }

    private static Vector2 Stick(XRNode node)
    {
        return GillaSticks.Read(node, 0.15f);
    }

    private static void Buzz(XRNode node, float amp, float seconds)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            device.SendHapticImpulse(0, amp, seconds);
        }
    }

    public MonsterPlayer()
    {
        maxDoorThrow = 8f;
        squashSpeed = 2.5f;
        squashReach = 0.25f;
        squashed = new Dictionary<int, float>();
        grabRadius = 0.45f;
        skinTouch = 0.07f;
        apeEyeHeight = 0.5f;
        apeReach = 0.6f;
        graspFor = 0.25f;
        pullApart = 0.2f;
        heldReach = 0.2f;
        punchSwing = 1.5f;
        hardestThrow = 14f;
        slamSwing = 2.5f;
        punchEvery = 0.35f;
        freePunchSwing = 2f;
        punchSwingLasts = 0.2f;
        punchReach = 0.12f;
        punchShoveLimit = 5f;
        punchAfterThrow = 0.6f;
        punchShoveLeast = 2.5f;
        punchShoveMost = 4.5f;
        faster = 1.15f;
        locomotionWasOn = true;
        floatFrame = -10;
        interp = RigidbodyInterpolation.Interpolate;
        maxBodyThrow = 9f;
        backHold = 1f;
        heldDeadFor = 1.5f;
        neverHeldAfter = 3f;
        crawlSize = 1f;
        armWas = new Vector3[2];
        armSwung = new Vector3[2];
        armSwungAt = new float[2];
        armLostAt = new float[] { -10f, -10f };
        deadBody = new int[] { -1, -1 };
        deadSince = new float[2];
        bodyOf = new int[] { -1, -1 };
        bodyPart = new int[] { -1, -1 };
        ripBody = -1;
        awaitBody = -1;
        fistsFrame = -10;
        touching = new List<Vector3>(4);
        closedL = -10f;
        closedR = -10f;
    }

    static MonsterPlayer()
    {
        ApeSize = 1f;
        TestGrips = -1;
    }

    private Vector3 Squashing_g__Going_16_0(Vector3 p, bool inHand, Matrix4x4 back, float dt, Rigidbody rb)
    {
        if (!inHand)
        {
            if (rb != null && !rb.isKinematic)
            {
                return rb.GetPointVelocity(p);
            }
            return Vector3.zero;
        }
        return (p - back.MultiplyPoint3x4(p)) / dt;
    }

    private bool Squashing_g__Meets_16_1(Vector3 p, out Vector3 push, bool inHand, Matrix4x4 back, float dt, Rigidbody rb, Collider box)
    {
        push = Vector3.zero;
        Vector3 c = box.ClosestPoint(p);
        float d2 = (c - p).sqrMagnitude;
        if (d2 <= squashReach * squashReach)
        {
            Vector3 v = Squashing_g__Going_16_0(c, inHand, back, dt, rb);
            float speed = v.magnitude;
            if (squashSpeed <= speed && (d2 <= 1e-06f || 0f < Vector3.Dot(p - c, v)))
            {
                push = v.normalized;
                return true;
            }
        }
        return false;
    }
}
