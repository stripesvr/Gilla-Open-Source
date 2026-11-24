// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public class GillaPitKill : MonoBehaviour
{
    [Tooltip("Where your eyes mean you've gone in: in this object's parent's space. Its width and depth are the whole room's.")]
    public Bounds pits;
    private const float RoomOver = 12f;
    private const float LookEvery = 0.25f;
    private static readonly List<GillaPitKill> live = new List<GillaPitKill>();
    private Vector3 outside;
    private bool wasOutside;
    private float lookAt;

    private void OnEnable()
    {

        live.Add(this);
    }

    private void OnDisable()
    {
        live.Remove(this);
    }

    public bool Covers(Vector3 at)
    {
        if (!(transform.parent == null))
        {
            Vector3 local = transform.parent.InverseTransformPoint(at);
            float c = pits.center.x;
            float e = pits.extents.x;
            if (c - e <= local.x)
            {
                if (c + e < local.x)
                {
                    return false;
                }
                c = pits.center.z;
                e = pits.extents.z;
                if (local.z < c - e)
                {
                    return false;
                }
                if (c + e < local.z)
                {
                    return false;
                }
                c = pits.center.y;
                e = pits.extents.y;
                if (local.y < c - e)
                {
                    return false;
                }
                return local.y <= c + e + RoomOver;
            }
        }
        return false;
    }

    private bool Under(Vector3 at)
    {
        if (transform.parent != null)
        {
            Vector3 local = transform.parent.InverseTransformPoint(at);
            return pits.Contains(local);
        }
        return false;
    }

    public static bool InRoom(Vector3 at)
    {
        foreach (GillaPitKill pit in live)
        {
            if (pit.Covers(at))
            {
                return true;
            }
        }
        return false;
    }

    public static bool Fallen(Vector3 at)
    {
        foreach (GillaPitKill pit in live)
        {
            if (pit.Under(at))
            {
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        KillCam cam = KillCam.Local;
        if (cam == null)
        {
            return;
        }
        if (cam.head == null)
        {
            return;
        }
        if (transform.parent == null)
        {
            return;
        }
        Vector3 headPos = cam.head.position;
        if (Covers(headPos))
        {
            if (MonsterPlayer.IsMonster)
            {
                if (wasOutside)
                {
                    Vector3 target = outside;
                    Vector3 camForward = cam.head.forward;
                    RaycastHit hit = default(RaycastHit);
                    Rigidbody body = null;
                    GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance /* ? */;
                    if (player == null)
                    {
                        return;
                    }
                    if (player.headCollider == null)
                    {
                        return;
                    }
                    GillaPlaces.Prepare(target);
                    GillaBackroomsSwitch.Prepare(target);
                    Transform root = player.transform;
                    Transform headT = player.headCollider.transform;
                    float height = 1.5f;
                    if (Physics.Raycast(headT.position, Vector3.down, out hit, 3f, -7941, QueryTriggerInteraction.Ignore))
                    {
                        height = headT.position.y - hit.point.y;
                    }
                    Vector3 from = Vector3.ProjectOnPlane(headT.forward, Vector3.up);
                    Vector3 to = Vector3.ProjectOnPlane(camForward, Vector3.up);
                    if (from.sqrMagnitude > 0.0001f && to.sqrMagnitude > 0.0001f)
                    {
                        float angle = Vector3.SignedAngle(from, to, Vector3.up) /* ? */;
                        player.Turn(angle);
                    }
                    root.position = root.position + ((target + height * Vector3.up) - headT.position);
                    if (root.TryGetComponent<Rigidbody>(out body))
                    {
                        body.position = root.position;
                        body.linearVelocity = Vector3.zero;
                    }
                    player.InitializeValues();
                    return;
                }
            }
            else if (!cam.Busy && Under(headPos))
            {
                /* ? */ // cam.FellInPit = true; // setter is not accessible from here; KillCam must set this itself
                cam.Die(false);
                return;
            }
        }
        else
        {
            if (lookAt <= Time.time)
            {
                lookAt = Time.time + LookEvery;
                outside = GillaTeleport.FloorUnder(headPos, 4f);
                wasOutside = true;
            }
        }
    }
}
