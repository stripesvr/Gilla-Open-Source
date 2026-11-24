// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class CosmeticToy : MonoBehaviour
{
    public ToyKind kind;
    [Tooltip("Where a hand holds it: the middle of the hand goes here, the hand's forward along its forward, its up along its up.")]
    public Transform grip;
    [Tooltip("The bit you reach for while it's on you (none: the grip).")]
    public Transform grab;
    [Tooltip("How near (m) a hand must come to that bit to take it.")]
    public float reach = 0.2f;
    private const float PalmSkin = 0.02f;
    public static readonly Vector3 FingersInHand = new Vector3(0f, -0.866f, 0.5f);
    public static readonly Vector3 HeadInHand = new Vector3(0f, 0.5f, 0.866f);
    private const float IntoPalm = 0.025f;

    public Vector3 GrabPoint
    {
        get
        {
            Transform t;
            if (grab != null)
            {
                t = grab;
            }
            else if (grip != null)
            {
                t = grip;
            }
            else
            {
                t = transform;
            }
            return t.position;
        }
    }

    public void PoseIn(Transform hand, float palmSide = 1)
    {
        if (kind == ToyKind.Monkey && !(grip == null) && !(grab == null))
        {
            Vector3 right = hand.right;
            float side = 0f <= palmSide ? 1f : -1f;
            Vector3 scale = hand.lossyScale;
            Vector3 up = hand.rotation * HeadInHand;
            Quaternion turn = Quaternion.LookRotation(-(side * right), up);
            Quaternion invGrip = Quaternion.Inverse(grip.rotation);
            Vector3 offset = invGrip * (grab.position - grip.position);
            Vector3 handPos = hand.position;
            float outward = scale.x * PalmSkin + offset.magnitude * 0.22f;
            Vector3 fingers = hand.rotation * FingersInHand;
            float into = scale.x * IntoPalm;
            Vector3 turned = turn * offset;
            Vector3 at = side * right * outward + handPos + into * fingers - turned;
            PoseAt(at, turn);
            return;
        }
        PoseAt(hand.position, hand.rotation);
    }

    public void Sit(Vector3 at, Vector3 behind)
    {
        Quaternion turn = Quaternion.LookRotation(behind, Vector3.up);
        PoseAt(at, turn);
    }

    public void PoseAt(Vector3 at, Quaternion turn)
    {
        bool noGrip = grip == null;
        Transform t = transform;
        if (noGrip)
        {
            t.SetPositionAndRotation(at, turn);
            return;
        }
        Quaternion inv = Quaternion.Inverse(grip.localRotation);
        t.rotation = turn * inv;
        Transform self = transform;
        self.position = self.position + (at - grip.position);
    }

    public static float Elastic(float t)
    {
        float result = 0f;
        if (0f < t)
        {
            result = 1f;
            if (t < 1f)
            {
                result = Mathf.Pow(2f, t * -10f) * Mathf.Sin(((t - 0.075f) * 6.2831855f) / 0.3f) + 1f;
            }
        }
        return result;
    }
}
