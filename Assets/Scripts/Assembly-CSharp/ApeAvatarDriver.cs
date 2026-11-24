// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[DefaultExecutionOrder(100)]
public class ApeAvatarDriver : MonoBehaviour
{
    [Header("Targets (tracked transforms)")]
    public Transform headTarget;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    [Header("Bones")]
    public Transform headBone;
    public Transform eyeL;
    public Transform eyeR;
    public Transform upperArmL;
    public Transform forearmL;
    public Transform handL;
    public Transform upperArmR;
    public Transform forearmR;
    public Transform handR;
    [Tooltip("Knuckle bones (index1 / pinkie1) - they tell the driver which way each hand points.")]
    public Transform indexL;
    [Tooltip("Knuckle bones (index1 / pinkie1) - they tell the driver which way each hand points.")]
    public Transform pinkieL;
    [Tooltip("Knuckle bones (index1 / pinkie1) - they tell the driver which way each hand points.")]
    public Transform indexR;
    [Tooltip("Knuckle bones (index1 / pinkie1) - they tell the driver which way each hand points.")]
    public Transform pinkieR;
    [Header("Fit")]
    [Tooltip("The camera sits this far behind the eyes, inside the head.")]
    public float eyeBack = 0.03f;
    [Tooltip("How quickly the body turns to face where you look.")]
    public float turnSmoothing = 9f;
    [Tooltip("How much the body tips and tilts with your head. 1 = attached to it (Big Scary): look down and the body swings with you. 0 = it only turns (Gorilla Tag): look down and you see your chest.")]
    [Range(0, 1)]
    public float bodyTiltFollow = 1f;
    public bool turnHeld;
    public const float HeldNeckTwist = 70;
    [Tooltip("Longest the upper arm may stretch to reach a controller (x its own length).")]
    public float maxStretch = 2.2f;
    [Tooltip("How far the elbow swings toward the hand's little-finger side as the hand turns, 0..1. A real elbow goes out as the palm turns down and in as it turns up; following it keeps the wrist from bending back into the arm.")]
    [Range(0, 1)]
    public float elbowFollowsHand = 0.5f;
    [Tooltip("Share of the hand's roll the forearm turns with, 0..1; the wrist takes the rest. All of it at the wrist wrung the skin there thin, like a sweet wrapper, and flipped it the other way past half a turn.")]
    [Range(0, 1)]
    public float forearmTwist = 0.6f;
    [Header("Hands")]
    [Tooltip("Size of the hands against the rest of the ape, scaled at the wrist. The setup pass sets it so the hand is a real one's size (GillaSetup.HandLength): at the model's Gorilla Tag size they were far too big.")]
    public float handScale = 1f;
    [Tooltip("How far a straightened finger points from the controller's grip axis, degrees. The grip pose the controllers report runs through your closed fist, along the handle; on every Quest controller a straightened finger (OpenXR's palm and aim poses) points 60 degrees round from it, toward the front of the controller.")]
    public float handPitch = 60f;
    [Tooltip("How far the palm's skin sits from the controller's grip centre: half the handle's thickness (1.8 cm on Quest).")]
    public float gripDepth = 0.018f;
    [Tooltip("How far the hand's middle sits behind the palm's skin: half the hand's thickness (the setup pass measures it). The locomotion's hand sphere is centred there (gripDepth + this in from the grip), so the hand targets are the hand's middle and the palm's skin goes this far out from them: a hand stops on a surface with its palm or its back against it, not floating.")]
    public float handDepth = 0.02f;
    [Tooltip("Extra rotation between controller and hand, degrees (fine-tuning in the headset).")]
    public Vector3 handRotTrimL;
    [Tooltip("Extra rotation between controller and hand, degrees (fine-tuning in the headset).")]
    public Vector3 handRotTrimR;
    [Header("Rig facts (set by the setup pass)")]
    [Tooltip("The middle of each palm's skin in its hand bone's space, measured from the mesh: it goes against the controller's handle. Zero = estimated from the bones.")]
    public Vector3 palmSurfaceL;
    [Header("Rig facts (set by the setup pass)")]
    [Tooltip("The middle of each palm's skin in its hand bone's space, measured from the mesh: it goes against the controller's handle. Zero = estimated from the bones.")]
    public Vector3 palmSurfaceR;
    private Vector3 headOffset;
    private Quaternion headRotOffset;
    private Quaternion handOffsetL = Quaternion.identity;
    private Quaternion handOffsetR = Quaternion.identity;
    private Vector3 palmLocalL;
    private Vector3 palmLocalR;
    private Vector3 palmDirL;
    private Vector3 palmDirR;
    private Quaternion restUpperL;
    private Quaternion restLowerL;
    private Quaternion restHandL;
    private Quaternion restUpperR;
    private Quaternion restLowerR;
    private Quaternion restHandR;
    private Vector3 restLowerPosL;
    private Vector3 restLowerPosR;
    private Vector3 restHandPosL;
    private Vector3 restHandPosR;
    private Vector3 restUpperScaleL;
    private Vector3 restUpperScaleR;
    private Vector3 restLowerScaleL;
    private Vector3 restLowerScaleR;
    private int axisL = 1;
    private int axisR = 1;
    private Vector3 poleL;
    private Vector3 poleR;
    private float twistL;
    private float twistR;
    private float yaw;
    private float headYaw;
    private Quaternion tilt = Quaternion.identity;
    private bool ready;

    public float BodyYaw { get { return yaw; } }

    private void Awake()
    {

        if (handL != null)
            handL.localScale = handL.localScale * handScale;
        if (handR != null)
            handR.localScale = handR.localScale * handScale;
    }

    private void Start()
    {
        if (headBone == null || headTarget == null || eyeL == null || eyeR == null)
        {
            enabled = false;
            return;
        }
        float y = transform.eulerAngles.y;
        headYaw = y;
        yaw = y;
        Quaternion body = Quaternion.Euler(0f, y, 0f);
        Vector3 eyes = (eyeL.position + eyeR.position) * 0.5f + body * new Vector3(0f, 0f, -eyeBack);
        headOffset = Quaternion.Inverse(body) * (headBone.position - eyes);
        headRotOffset = Quaternion.Inverse(body) * headBone.rotation;
        if (handL != null)
            CalibrateHand(handL, indexL, pinkieL, 1f, palmSurfaceL, out handOffsetL, out palmLocalL, out palmDirL);
        if (handR != null)
            CalibrateHand(handR, indexR, pinkieR, -1f, palmSurfaceR, out handOffsetR, out palmLocalR, out palmDirR);
        if (upperArmL != null)
        {
            restUpperL = upperArmL.localRotation;
            restUpperScaleL = upperArmL.localScale;
        }
        if (forearmL != null)
        {
            restLowerL = forearmL.localRotation;
            restLowerPosL = forearmL.localPosition;
            restLowerScaleL = forearmL.localScale;
        }
        if (handL != null)
        {
            restHandL = handL.localRotation;
            restHandPosL = handL.localPosition;
        }
        if (upperArmR != null)
        {
            restUpperR = upperArmR.localRotation;
            restUpperScaleR = upperArmR.localScale;
        }
        if (forearmR != null)
        {
            restLowerR = forearmR.localRotation;
            restLowerPosR = forearmR.localPosition;
            restLowerScaleR = forearmR.localScale;
        }
        if (handR != null)
        {
            restHandR = handR.localRotation;
            restHandPosR = handR.localPosition;
        }
        axisL = DominantAxis(restLowerPosL);
        axisR = DominantAxis(restLowerPosR);
        ready = true;
    }

    private void CalibrateHand(Transform hand, Transform index1, Transform pinkie1, float palmSide, Vector3 surface, out Quaternion offset, out Vector3 palmLocal, out Vector3 palmDir)
    {
        offset = Quaternion.identity;
        palmDir = Vector3.zero;
        palmLocal = Vector3.zero;
        if (index1 == null || pinkie1 == null)
            return;
        Vector3 finger, palm, knuckles;
        HandFrame(hand, index1, pinkie1, palmSide > 0f, out finger, out palm, out knuckles);
        Quaternion inv = Quaternion.Inverse(hand.rotation);
        Quaternion handLook = Quaternion.LookRotation(inv * finger, inv * palm);
        Quaternion ctrl = Quaternion.LookRotation(Quaternion.Euler(handPitch, 0f, 0f) * Vector3.forward, new Vector3(palmSide, 0f, 0f));
        offset = ctrl * Quaternion.Inverse(handLook);
        palmDir = inv * palm;
        Vector3 handPos = hand.position;
        if (surface == Vector3.zero)
        {
            float len = Vector3.Distance(handPos, knuckles);
            Vector3 hp = hand.position;
            Vector3 pos = hp + (knuckles - hp) * 0.5f + palm * len * 0.15f;
            surface = hand.InverseTransformPoint(pos);
        }
        palmLocal = surface;
    }

    public static void HandFrame(Transform hand, Transform index1, Transform pinkie1, bool left, out Vector3 finger, out Vector3 palm, out Vector3 knuckles)
    {
        knuckles = (index1.position + pinkie1.position) * 0.5f;
        finger = (knuckles - hand.position).normalized;
        Vector3 across = Vector3.ProjectOnPlane(index1.position - pinkie1.position, finger).normalized;
        palm = Vector3.Cross(finger, across).normalized;
        if (left)
            palm = -palm;
    }

    private void LateUpdate()
    {
        if (!ready)
            return;
        Vector3 f = headTarget.forward;
        Vector3 flat = new Vector3(f.x, 0f, f.z);
        if (flat.sqrMagnitude < 0.08f)
        {
            Vector3 up = headTarget.up;
            float sign = headTarget.forward.y >= 0f ? -1f : 1f;
            flat = new Vector3(up.x * sign, 0f, up.z * sign);
        }
        float t = 1f - Mathf.Exp(-(turnSmoothing * Time.deltaTime));
        if (flat.sqrMagnitude > 1e-05f)
        {
            headYaw = Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
            float delta = Mathf.DeltaAngle(yaw, headYaw);
            if (!turnHeld)
            {
                yaw = yaw + delta * Mathf.Clamp01(t);
            }
            else if (Mathf.Abs(delta) > HeldNeckTwist)
            {
                yaw = yaw + (delta - (delta >= 0f ? HeldNeckTwist : -HeldNeckTwist));
            }
        }
        Quaternion body = Quaternion.Euler(0f, yaw, 0f);
        Quaternion lean = Quaternion.Inverse(Quaternion.Euler(0f, headYaw, 0f)) * headTarget.rotation;
        tilt = Quaternion.Slerp(tilt, Quaternion.Slerp(Quaternion.identity, lean, bodyTiltFollow), t);
        Quaternion rot = body * tilt;
        transform.rotation = rot;
        Transform tr = transform;
        tr.position = tr.position + (headTarget.position + rot * headOffset - headBone.position);
        headBone.rotation = headTarget.rotation * headRotOffset;
        if (handL != null && leftHandTarget != null)
            DriveArm(upperArmL, forearmL, handL, leftHandTarget, restUpperL, restLowerL, restHandL, restLowerPosL, restHandPosL, restUpperScaleL, restLowerScaleL, handOffsetL, handRotTrimL, palmLocalL, palmDirL, -1f, axisL, body, ref poleL, ref twistL);
        if (handR != null && rightHandTarget != null)
            DriveArm(upperArmR, forearmR, handR, rightHandTarget, restUpperR, restLowerR, restHandR, restLowerPosR, restHandPosR, restUpperScaleR, restLowerScaleR, handOffsetR, handRotTrimR, palmLocalR, palmDirR, 1f, axisR, body, ref poleR, ref twistR);
    }

    private void DriveArm(Transform upper, Transform lower, Transform end, Transform target, Quaternion restUpper, Quaternion restLower, Quaternion restHand, Vector3 restLowerPos, Vector3 restHandPos, Vector3 restUpperScale, Vector3 restLowerScale, Quaternion handOffset, Vector3 trim, Vector3 palmLocal, Vector3 palmDir, float side, int axis, Quaternion body, ref Vector3 pole, ref float twist)
    {
        upper.localRotation = restUpper;
        upper.localScale = restUpperScale;
        lower.localRotation = restLower;
        lower.localScale = restLowerScale;
        lower.localPosition = restLowerPos;
        end.localRotation = restHand;
        end.localPosition = restHandPos;

        Quaternion handRot = target.rotation * Quaternion.Euler(trim) * handOffset;
        end.rotation = handRot;
        Vector3 want = target.position + end.TransformDirection(palmDir) * handDepth - (end.TransformPoint(palmLocal) - end.position);
        float upperLen = Vector3.Distance(upper.position, lower.position);
        float lowerLen = Vector3.Distance(lower.position, end.position);
        float dist = Vector3.Distance(want, upper.position);
        if ((upperLen + lowerLen) * 0.97f < dist)
        {
            float s = (dist - lowerLen) / Mathf.Max(upperLen * 0.97f, 0.0001f);
            s = Mathf.Clamp(s, 1f, maxStretch);
            upper.localScale = ScaleAlong(restUpperScale, axis, s);
            lower.localScale = ScaleAlong(restLowerScale, axis, 1f / s);
        }

        Vector3 p = (body * new Vector3(side * 0.35f, -0.6f, -0.15f)).normalized;
        Vector3 bent = p * (1f - elbowFollowsHand) - target.forward * elbowFollowsHand;
        if (bent.sqrMagnitude > 0.0001f)
            p = bent.normalized;
        if (pole != Vector3.zero)
            p = Vector3.Slerp(pole, p, 1f - Mathf.Exp(Time.deltaTime * -20f));
        pole = p;
        SolveTwoBone(upper, lower, end, want, upper.position + pole);

        Vector3 twistAxis = restHandPos.normalized;
        Quaternion local = Quaternion.Inverse(lower.rotation) * handRot * Quaternion.Inverse(restHand);
        twist = Unwrap(TwistAngle(local, twistAxis), twist);
        lower.rotation = lower.rotation * Quaternion.AngleAxis(twist * forearmTwist, twistAxis);

        if (upper.localScale != restUpperScale)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 cur = end.position - lower.position;
                Vector3 lowerPos = lower.position;
                if (cur.sqrMagnitude < 1e-08f) break;
                Vector3 toWant = want - lowerPos;
                float wantSqr = toWant.sqrMagnitude;
                if (wantSqr < 1e-08f) break;
                lower.rotation = Quaternion.FromToRotation(cur, toWant) * lower.rotation;
                float len = Vector3.Distance(lower.position, end.position);
                if (len > 0.0001f)
                    lower.localScale = ScaleAlong(lower.localScale, axis, Mathf.Sqrt(wantSqr) / len);
            }
        }
        end.rotation = handRot;
        end.position = want;
    }

    public static float TwistAngle(Quaternion q, Vector3 axis)
    {
        float a = Mathf.Atan2(q.x * axis.x + q.y * axis.y + q.z * axis.z, q.w);
        return Mathf.DeltaAngle(0f, a * 2f * Mathf.Rad2Deg);
    }

    private static float Unwrap(float angle, float last)
    {
        float v = Mathf.DeltaAngle(last, angle) + last;
        if (v > 250f)
            return v - 360f;
        if (v < -250f)
            return v + 360f;
        return v;
    }

    private static int DominantAxis(Vector3 v)
    {
        float ax = Mathf.Abs(v.x);
        float ay = Mathf.Abs(v.y);
        float az = Mathf.Abs(v.z);
        if (ay > ax && ay >= az)
            return 1;
        return ax < az ? 2 : 0;
    }

    private static Vector3 ScaleAlong(Vector3 scale, int axis, float s)
    {
        if (axis == 1)
        {
            scale.y *= s;
            return scale;
        }
        if (axis == 0)
            scale.x *= s;
        else
            scale.z *= s;
        return scale;
    }

    public static void SolveTwoBone(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 pole)
    {
        float upperLen = Vector3.Distance(upper.position, lower.position);
        float lowerLen = Vector3.Distance(lower.position, end.position);
        Vector3 a = upper.position;
        Vector3 toTarget = target - a;
        if (toTarget.sqrMagnitude < 1e-08f)
            return;
        float mag = toTarget.magnitude;
        float d = Mathf.Clamp(mag, 0.02f, (upperLen + lowerLen) * 0.999f);
        Vector3 dir = toTarget.normalized;
        Vector3 toEnd = end.position - a;
        if (toEnd.sqrMagnitude < 1e-08f)
            return;
        upper.rotation = Quaternion.FromToRotation(toEnd.normalized, dir) * upper.rotation;
        Vector3 poleP = Vector3.ProjectOnPlane(pole - a, dir);
        Vector3 bendP = Vector3.ProjectOnPlane(lower.position - a, dir);
        if (poleP.sqrMagnitude > 1e-06f)
        {
            if (bendP.sqrMagnitude > 1e-06f)
            {
                float ang = Vector3.SignedAngle(bendP, poleP, dir) /* ? */;
                upper.rotation = Quaternion.AngleAxis(ang, dir) * upper.rotation;
            }
            float cos = (upperLen * upperLen + d * d - lowerLen * lowerLen) / (upperLen * 2f * d);
            float bend = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f));
            Vector3 bendAxis = Vector3.Cross(dir, poleP.normalized);
            if (bendAxis.sqrMagnitude > 1e-08f)
                upper.rotation = Quaternion.AngleAxis(bend * Mathf.Rad2Deg, bendAxis.normalized) * upper.rotation;
        }
        Vector3 cur = end.position - lower.position;
        Vector3 lowerPos = lower.position;
        if (cur.sqrMagnitude > 1e-08f)
        {
            Vector3 want = target - lowerPos;
            if (want.sqrMagnitude > 1e-08f)
                lower.rotation = Quaternion.FromToRotation(cur.normalized, want.normalized) * lower.rotation;
        }
    }
}
