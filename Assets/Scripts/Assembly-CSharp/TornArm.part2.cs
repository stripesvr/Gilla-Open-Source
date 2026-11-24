// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class TornArm : MonoBehaviour
{
    static TornArm()
    {
        skinnedOn = new Dictionary<Mesh, bool[]>();
    }

    public TornArm()
    {
        links = new Transform[3];
        bodies = new Rigidbody[3];
        shapes = new CapsuleCollider[3];
        middles = new float[3];
        copied = new Dictionary<Transform, Transform>();
        held = -1;
    }

    private void Update()
    {
        if (held < 0)
        {
            return;
        }
        Transform hand = (Transform)typeof(GillaCorpse).GetMethod("Hand", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { heldBy, heldRight }) /* ? */;
        if (hand == null)
        {
            return;
        }
        Vector3 handPos = hand.position;
        Vector3 offset = hand.rotation * heldAt;
        Quaternion rot = Rolled(hand.rotation * heldTurned, Time.deltaTime);
        Vector3 target = handPos + offset;
        if ((target - links[held].position).sqrMagnitude > 1f)
        {
            Put(held, target, rot);
        }
        bodies[held].MovePosition(target);
        bodies[held].MoveRotation(rot);
        for (int i = 0; i < 3; i++)
        {
            if (i != held)
            {
                bodies[i].WakeUp();
            }
        }
    }

    private Quaternion Rolled(Quaternion turned, float dt)
    {
        if (held == 2)
        {
            return turned;
        }
        Vector3 up = turned * Vector3.up;
        Vector3 down = Vector3.ProjectOnPlane(Vector3.down, up);
        Quaternion result = turned * Quaternion.AngleAxis(heldRoll, Vector3.up);
        if (down.sqrMagnitude >= 0.04f)
        {
            Vector3 hinge = held != 0 ? hingeOnFore : hingeOnUpper;
            Vector3 v = result * hinge;
            float angle = Vector3.SignedAngle(Vector3.Cross(v, up), down, up) /* ? */;
            float step = Mathf.Clamp(angle, dt * -Rolls, dt * Rolls);
            heldRoll += step;
            result = turned * Quaternion.AngleAxis(heldRoll, Vector3.up);
        }
        return result;
    }

    private void LateUpdate()
    {
        if (held < 0)
        {
            return;
        }
        Transform hand = (Transform)typeof(GillaCorpse).GetMethod("Hand", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { heldBy, heldRight }) /* ? */;
        if (hand == null)
        {
            return;
        }
        Vector3 target = hand.position + hand.rotation * heldAt;
        Vector3 delta = target - links[held].position;
        if (delta.sqrMagnitude < 0.0001f)
        {
            return;
        }
        for (int i = 0; i < 3; i++)
        {
            Transform link = links[i];
            link.position = delta + link.position;
        }
    }

    private void OnDestroy()
    {
        for (int i = 1; i < 3; i++)
        {
            if (links[i] != null)
            {
                Object.Destroy(links[i].gameObject);
            }
        }
    }
}
