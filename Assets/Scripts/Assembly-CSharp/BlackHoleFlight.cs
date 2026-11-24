// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class BlackHoleFlight : MonoBehaviour
{
    private const float Flies = 0.65f;
    private const float Waits = 3f;

    private Vector3 velocity;
    private float t;
    private bool mine;
    private bool stopped;
    private Transform model;
    private Transform ring;
    private int mask;

    public int Owner { get; private set; } = -1;

    public void Begin(PocketBlackHole look, GameObject display, Vector3 v, NetworkApe by)
    {
        velocity = v;
        mine = by == null;
        Owner = by != null && by.Object != null ? by.Object.InputAuthority.PlayerId : -1; /* ? */
        mask = ~LayerMask.GetMask(new string[] { "PlayerRig", "FirstPerson", "MirrorOnly", "Buttons", "Apes", "Ignore Raycast" });
        if (display == null)
        {
            return;
        }
        model = display.transform;

        Transform orb = null;
        if (look.orb != null)
        {
            orb = display.transform.Find(Path(look.transform, look.orb));
        }
        Transform r = null;
        if (look.ring != null)
        {
            r = display.transform.Find(Path(look.transform, look.ring));
        }
        ring = r;
        if (orb == null)
        {
            orb = display.transform;
        }
        orb.SetParent(transform, false);
        orb.localPosition = Vector3.zero;
        Destroy(display);
        model = orb;
    }

    private static string Path(Transform root, Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (ring != null)
        {
            ring.Rotate(0f, dt * 500f, 0f, Space.Self);
        }
        if (!stopped)
        {
            velocity = (velocity + Physics.gravity * (dt * 0.25f)) * Mathf.Exp(dt * -0.9f);
            Vector3 origin = transform.position;
            Vector3 step = velocity * dt;
            float dist = step.magnitude;
            RaycastHit hit;
            if (Physics.Raycast(origin, step.normalized, out hit, dist + 0.3f, mask, QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point + hit.normal * 0.45f;
            }
            else
            {
                transform.position = transform.position + step;
                if (t < Flies)
                {
                    return;
                }
            }
            Stop();
        }
        else if (!mine && t > Waits)
        {
            Destroy(gameObject, 0f);
        }
    }

    private void Stop()
    {
        t = 0f;
        stopped = true;
        if (!mine)
        {
            return;
        }
        NetworkApe local = NetworkApe.Local;
        if (local != null && local.Object != null && local.Object.Runner && local.Object.HasStateAuthority /* ? */)
        {
            local.RPC_BlackHoleOpen(transform.position);
            return;
        }
        PocketBlackHole.Open(null, transform.position);
    }
}
