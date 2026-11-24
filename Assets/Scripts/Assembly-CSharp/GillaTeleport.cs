// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public static class GillaTeleport
{
    private const int Floors = -7941;

    public static bool To(Vector3 feet, Vector3 facing)
    {
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player == null)
        {
            return false;
        }
        if (player.headCollider == null)
        {
            return false;
        }
        GillaPlaces.Prepare(feet);
        GillaBackroomsSwitch.Prepare(feet);
        Transform root = player.transform;
        Transform head = player.headCollider.transform;
        float height = 1.5f;
        RaycastHit hit;
        if (Physics.Raycast(head.position, Vector3.down, out hit, 3f, Floors, QueryTriggerInteraction.Ignore))
        {
            height = head.position.y - hit.point.y;
        }
        Vector3 from = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        Vector3 to = Vector3.ProjectOnPlane(facing, Vector3.up);
        if (from.sqrMagnitude > 0.0001f && to.sqrMagnitude > 0.0001f)
        {
            player.Turn(Vector3.SignedAngle(from, to, Vector3.up) /* ? */);
        }
        root.position = root.position + ((feet + height * Vector3.up) - head.position);
        Rigidbody body;
        if (root.TryGetComponent<Rigidbody>(out body))
        {
            body.position = root.position;
            body.linearVelocity = Vector3.zero;
        }
        player.InitializeValues();
        return true;
    }

    public static Vector3 FloorUnder(Vector3 p, float down = 100)
    {
        RaycastHit hit;
        if (!Physics.Raycast(p, Vector3.down, out hit, down, Floors, QueryTriggerInteraction.Ignore))
        {
            hit.point = p;
        }
        return hit.point;
    }
}
