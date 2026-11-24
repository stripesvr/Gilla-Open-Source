// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class ApeIdle : MonoBehaviour
{
    [Tooltip("How much it moves (1: a gentle idle).")]
    public float amount = 1f;
    [Tooltip("How quickly.")]
    public float speed = 1f;
    private Transform spine;
    private Transform neck;
    private Transform head;
    private Transform armL;
    private Transform armR;
    private Transform foreL;
    private Transform foreR;
    private Quaternion spineRest;
    private Quaternion neckRest;
    private Quaternion headRest;
    private Quaternion armLRest;
    private Quaternion armRRest;
    private Quaternion foreLRest;
    private Quaternion foreRRest;
    private Vector3 rest;
    private float phase;

    private void Awake()
    {

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            switch (t.name)
            {
                case "lowerarm_L": foreL = t; break;
                case "lowerarm_R": foreR = t; break;
                case "upperarm_L": armL = t; break;
                case "upperarm_R": armR = t; break;
                case "spine": spine = t; break;
                case "head": head = t; break;
                case "neck": neck = t; break;
            }
        }
        spineRest = spine != null ? spine.localRotation : Quaternion.identity;
        neckRest = neck != null ? neck.localRotation : Quaternion.identity;
        headRest = head != null ? head.localRotation : Quaternion.identity;
        armLRest = armL != null ? armL.localRotation : Quaternion.identity;
        armRRest = armR != null ? armR.localRotation : Quaternion.identity;
        foreLRest = foreL != null ? foreL.localRotation : Quaternion.identity;
        foreRRest = foreR != null ? foreR.localRotation : Quaternion.identity;
        rest = transform.localPosition;
        phase = Mathf.Repeat(transform.position.x * 1.7f + transform.position.z * 2.3f, 20f);
    }

    private void LateUpdate()
    {
        float a = amount;
        float t = Time.time * speed + phase;
        if (spine != null)
        {
            spine.localRotation = spineRest * Quaternion.Euler(
                a * 1.6f * Mathf.Sin(t * 1.5f),
                a * 1.2f * Mathf.Sin(t * 0.37f),
                a * 1.4f * Mathf.Sin(t * 0.61f));
        }
        if (neck != null)
        {
            neck.localRotation = neckRest * Quaternion.Euler(
                a * 1.5f * Mathf.Sin(t * 0.83f + 1f),
                a * 4f * Mathf.Sin(t * 0.41f + 0.5f),
                0f);
        }
        if (head != null)
        {
            head.localRotation = headRest * Quaternion.Euler(
                a * 3f * Mathf.Sin(t * 0.9f),
                a * 9f * Mathf.Sin(t * 0.43f),
                a * 2.5f * Mathf.Sin(t * 0.57f + 2f));
        }
        if (armL != null)
        {
            armL.localRotation = armLRest * Quaternion.Euler(
                a * 3.5f * Mathf.Sin(t * 1.1f),
                0f,
                a * 2.5f * Mathf.Sin(t * 0.73f));
        }
        if (armR != null)
        {
            armR.localRotation = armRRest * Quaternion.Euler(
                a * 3.5f * Mathf.Sin(t * 1.1f + 2.2f),
                0f,
                a * 2.5f * Mathf.Sin(t * 0.73f + 1.7f));
        }
        if (foreL != null)
        {
            foreL.localRotation = foreLRest * Quaternion.Euler(a * 4f * Mathf.Sin(t * 0.95f + 0.6f), 0f, 0f);
        }
        if (foreR != null)
        {
            foreR.localRotation = foreRRest * Quaternion.Euler(a * 4f * Mathf.Sin(t * 0.95f + 2.9f), 0f, 0f);
        }
        Transform tr = transform;
        float bob = Mathf.Sin(t * 1.5f + 0.4f) * a * 0.012f * transform.localScale.y;
        tr.localPosition = rest + Vector3.up * bob;
    }
}
