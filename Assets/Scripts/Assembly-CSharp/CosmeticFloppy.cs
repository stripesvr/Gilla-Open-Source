// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

[DefaultExecutionOrder(400)]
public class CosmeticFloppy : MonoBehaviour
{
    [Serializable]
    public class Limb
    {
        public Transform bone;
        [Tooltip("The limb's far end: what the spring moves.")]
        public Transform tip;
        [Tooltip("How hard it's pulled back to its rest, and how quickly it settles (low: wobbles for longer).")]
        public float stiffness = 90f;
        [Tooltip("How hard it's pulled back to its rest, and how quickly it settles (low: wobbles for longer).")]
        public float damping = 5f;
        [Tooltip("How far gravity drags its far end, in lengths of the limb.")]
        public float sag = 0.5f;
        [Tooltip("The furthest it turns from its rest, degrees.")]
        public float maxAngle = 70f;
        public Quaternion rest;
        public Vector3 at;
        public Vector3 vel;
        public Vector3 lastTip;
    }

    [Tooltip("Parents before their children.")]
    public Limb[] limbs = new Limb[0];
    private const int Steps = 2;
    private const float LongestFrame = 0.033333335f;
    private const float Teleported = 1.5f;
    private bool started;

    private void Awake()
    {

        for (int i = 0; i < limbs.Length; i++)
        {
            Limb limb = limbs[i];
            if (limb.bone != null)
            {
                limb.rest = limb.bone.localRotation;
            }
        }
    }

    private void OnEnable()
    {

        started = false;
    }

    private void LateUpdate()
    {
        Step(Time.deltaTime);
    }

    public void Step(float dt)
    {
        if (0f < dt)
        {
            float h = Mathf.Min(dt, LongestFrame) / Steps;
            for (int i = 0; i < limbs.Length; i++)
            {
                Limb limb = limbs[i];
                if (limb.bone == null || limb.tip == null)
                    continue;

                limb.bone.localRotation = limb.rest;
                Vector3 root = limb.bone.position;
                Vector3 tipPos = limb.tip.position;
                Vector3 dir = tipPos - root;
                float len = dir.magnitude;
                if (len < 1e-05f)
                    continue;

                if (started)
                {
                    Vector3 moved = tipPos - limb.lastTip;
                    if (moved.sqrMagnitude <= Teleported * Teleported)
                    {
                        limb.lastTip = tipPos;
                        Vector3 tipVel = moved / dt;
                        float k = limb.stiffness;
                        float d = limb.damping;
                        Vector3 gravity = Vector3.down * (len * limb.sag * k);
                        Vector3 at = limb.at;
                        Vector3 vel = limb.vel;
                        for (int s = 0; s < Steps; s++)
                        {
                            vel += (gravity + ((tipPos - at) * k - (vel - tipVel) * d)) * h;
                            at += vel * h;
                        }
                        limb.vel = vel;
                        limb.at = at;

                        Vector3 offset = at - root;
                        if (offset.sqrMagnitude < 1e-10f)
                            offset = dir;

                        Vector3 want = Vector3.RotateTowards(dir.normalized, offset.normalized, limb.maxAngle * Mathf.Deg2Rad, 0f);
                        float angle = Vector3.Angle(offset, want);
                        limb.at = root + want * len;

                        Vector3 newVel = tipVel;
                        if (angle <= 0.5f)
                        {
                            newVel = tipVel + Vector3.ProjectOnPlane(limb.vel - tipVel, want);
                        }
                        limb.vel = newVel;

                        limb.bone.rotation = Quaternion.FromToRotation(dir, want) * limb.bone.rotation;
                        continue;
                    }
                }

                limb.lastTip = tipPos;
                limb.at = tipPos;
                limb.vel = Vector3.zero;
            }
            started = true;
        }
    }
}
