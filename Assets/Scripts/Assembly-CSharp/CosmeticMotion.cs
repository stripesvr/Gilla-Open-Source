// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class CosmeticMotion : MonoBehaviour
{
    private class Part
    {
        public Transform t;
        public Quaternion rest;
        public Vector3 tip;
        public Vector3 lastTip;
        public Vector3 vel;
        public Vector3 lastVel;
        public Vector3 acc;
        public Vector3 angle;
        public Vector3 speed;
    }

    [Tooltip("Degrees a propeller turns per metre the cosmetic travels.")]
    public float spinPerMetre = 900f;
    [Tooltip("The fastest it turns, degrees a second.")]
    public float maxSpin = 1440f;
    [Tooltip("How it turns standing still, degrees a second (the shop's stands give theirs some).")]
    public float idleSpin;
    [Tooltip("How quickly it winds down once you stop (per second).")]
    public float spinDown = 1.2f;
    [Tooltip("The swinging parts' spring: how stiff, and how quickly it settles.")]
    public float swayStiffness = 140f;
    [Tooltip("The swinging parts' spring: how stiff, and how quickly it settles.")]
    public float swayDamping = 6f;
    [Tooltip("Degrees a swinging part is flung per m/s² the head speeds up, slows down or turns; and the most it swings.")]
    public float swayPerAccel = 1.6f;
    [Tooltip("Degrees a swinging part is flung per m/s² the head speeds up, slows down or turns; and the most it swings.")]
    public float maxSway = 30f;
    private readonly List<Part> spins = new List<Part>();
    private readonly List<Part> sways = new List<Part>();
    private Vector3 lastPos;
    private float spinAngle;
    private float spinSpeed;
    private bool started;

    private void Awake()
    {

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform tr = all[i];
            bool isSpin = tr.name.StartsWith("Spin_");
            bool isSway = tr.name.StartsWith("Sway_");
            if (isSpin || isSway)
            {
                Part part = new Part();
                part.t = tr;
                part.rest = tr.localRotation;
                MeshFilter mf = tr.GetComponent<MeshFilter>();
                float height = 0.1f;
                if (mf != null)
                {
                    height = 0.1f;
                    if (mf.sharedMesh != null)
                    {
                        Bounds b = mf.sharedMesh.bounds;
                        height = b.center.y + b.extents.y;
                    }
                }
                Vector3 pos = tr.localPosition;
                Quaternion rot = tr.localRotation;
                Vector3 scale = tr.localScale;
                Vector3 point = Vector3.Scale(scale * height, Vector3.up);
                part.tip = pos + rot * point;
                (isSpin ? spins : sways).Add(part);
            }
        }
        if (spins.Count == 0 && sways.Count == 0)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {

        started = false;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        Vector3 pos = transform.position;
        float sqr = (pos - lastPos).sqrMagnitude;
        if (!started || 1f < sqr)
        {
            lastPos = pos;
            foreach (Part p in sways)
            {
                p.lastTip = Tip(p);
                p.vel = Vector3.zero;
                p.lastVel = Vector3.zero;
                p.acc = Vector3.zero;
            }
            started = true;
            return;
        }

        float target = Mathf.Clamp(Mathf.Sqrt(sqr) / dt * spinPerMetre, idleSpin, maxSpin);
        float rate = target <= spinSpeed ? spinDown : 8f;
        spinSpeed = Mathf.Lerp(spinSpeed, target, 1f - Mathf.Exp(-(rate * dt)));
        float newAngle = Mathf.Repeat(spinAngle + dt * spinSpeed, 360f);
        lastPos = pos;
        spinAngle = newAngle;
        foreach (Part p in spins)
        {
            p.t.localRotation = p.rest * Quaternion.AngleAxis(spinAngle, Vector3.up);
        }

        int steps = dt > 1f / 60f ? 2 : 1;
        float h = Mathf.Min(dt, 0.05f) / steps;
        float kv = Mathf.Clamp01(1f - Mathf.Exp(dt * -30f));
        float ka = Mathf.Clamp01(1f - Mathf.Exp(dt * -20f));
        foreach (Part p in sways)
        {
            Transform parent = p.t.parent;
            Vector3 tip = Tip(p);
            Vector3 newVel = p.vel + ((tip - p.lastTip) / dt - p.vel) * kv;
            p.lastTip = tip;
            p.vel = newVel;
            p.acc = p.acc + ((p.vel - p.lastVel) / dt - p.acc) * ka;
            p.lastVel = p.vel;

            Vector3 up = p.rest * Vector3.up;
            Vector3 local = parent.InverseTransformDirection(p.acc);
            Vector3 targetAngle = Vector3.ClampMagnitude(Vector3.Cross(local, up) * swayPerAccel, maxSway);
            for (int i = steps; i != 0; i--)
            {
                p.speed = p.speed + ((targetAngle - p.angle) * swayStiffness - p.speed * swayDamping) * h;
                p.angle = Vector3.ClampMagnitude(p.angle + p.speed * h, maxSway);
            }

            float mag = p.angle.magnitude;
            Quaternion value;
            if (mag <= 0.01f)
            {
                value = p.rest;
            }
            else
            {
                value = Quaternion.AngleAxis(mag, p.angle / mag) * p.rest;
            }
            p.t.localRotation = value;
        }
    }

    private static Vector3 Tip(Part p)
    {
        return p.t.parent.TransformPoint(p.tip);
    }
}
