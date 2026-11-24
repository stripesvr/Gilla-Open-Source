// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.XR;

public class DismemberGroup : MonoBehaviour
{
    public Dismemberment[] copies;
    [Tooltip("How hard a torn part is flung, m/s.")]
    public float flingSpeed = 2.5f;
    [Range(0, 1)]
    public float tearHaptic = 1f;
    public float tearHapticSeconds = 0.35f;

    public bool IsTorn(ApePart part)
    {
        if (copies != null && copies.Length != 0 && copies[0] != null)
        {
            Dismemberment.Part p = copies[0].Find(part);
            if (p == null)
            {
                return false;
            }
            return p.torn;
        }
        return false;
    }

    public void Tear(ApePart part)
    {
        if (copies == null || copies.Length == 0)
        {
            return;
        }
        if (copies[0] == null)
        {
            return;
        }
        Dismemberment.Part p = copies[0].Find(part);
        if (p != null && p.torn)
        {
            return;
        }

        Vector3 velocity = transform.up * 1.5f + Random.insideUnitSphere * 0.6f;
        if (part == ApePart.ArmL)
        {
            velocity -= transform.right * flingSpeed;
        }
        else if (part == ApePart.ArmR)
        {
            velocity += transform.right * flingSpeed;
        }
        else
        {
            velocity += transform.up * flingSpeed + transform.forward * 0.8f;
        }

        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment copy = copies[i];
            if (copy != null)
            {
                copy.Tear(part, velocity);
            }
        }

        if (part == ApePart.ArmR)
        {
            Buzz(XRNode.RightHand);
        }
        else
        {
            Buzz(XRNode.LeftHand);
            if (part != ApePart.ArmL)
            {
                Buzz(XRNode.RightHand);
            }
        }

        // No 'Player' type exists in this project; the arm-torn flags below were a
        // reconstruction guess with no backing type. Intent kept for reference.
        /* ? */ // Player player = GetComponent<Player>();
        /* ? */ // if (part == ApePart.ArmL && player != null) player.leftArmTorn = true;
        /* ? */ // if (part == ApePart.ArmR && player != null) player.rightArmTorn = true;
    }

    private void Buzz(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            device.SendHapticImpulse(0u, tearHaptic, tearHapticSeconds);
        }
    }

    public void ResetAll()
    {
        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment copy = copies[i];
            if (copy != null)
            {
                copy.ResetAll();
            }
        }
        // No 'Player' type exists in this project; reconstruction guess kept for reference.
        /* ? */ // Player player = GetComponent<Player>();
        /* ? */ // if (player != null) { player.leftArmTorn = false; player.rightArmTorn = false; }
    }
}
