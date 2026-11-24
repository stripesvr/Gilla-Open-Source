// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[RequireComponent(typeof(MonsterBrain))]
public class MonsterClimbDebris : MonoBehaviour
{
    [Tooltip("Chips knocked off per metre it climbs.")]
    public float chipsPerMetre = 34f;
    [Tooltip("Dust puffed off per metre it climbs.")]
    public float dustPerMetre = 14f;
    private MonsterBrain brain;
    private Debris debris;
    private float lastY;
    private float chipDue;
    private float dustDue;
    private float reach = 1f;
    private bool onWall;

    private void Start()
    {
        brain = GetComponent<MonsterBrain>();
        lastY = transform.position.y;
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r is ParticleSystemRenderer) continue;
            bounds.Encapsulate(r.bounds);
        }
        float size = Mathf.Max(bounds.size.x, bounds.size.z) * 0.85f;
        reach = Mathf.Clamp(size, 0.6f, 3f);
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        float prevY = lastY;
        lastY = pos.y;
        float pitch = brain.ClimbPitch;
        float climbPitch = brain.climbPitch;
        float dy = pos.y - prevY;
        Vector3 fwd = transform.forward;
        if (pitch >= climbPitch * 0.9f)
        {
            onWall = true;
            if (dy > 0f && dy <= 1f)
            {
                if (debris == null)
                {
                    debris = Debris.On(this);
                }
                float chips = chipDue + chipsPerMetre * dy;
                float dust = dustDue + dustPerMetre * dy;
                int chipCount = (int)chips;
                int dustCount = (int)dust;
                chipDue = chips - chipCount;
                dustDue = dust - dustCount;
                Vector3 basePos = transform.position + fwd * 0.06f;
                for (int i = chipCount; i > 0; i--)
                {
                    float t = Random.value < 0.75f ? Random.Range(0.7f, 1f) : Random.Range(0f, 0.25f);
                    Vector3 at = basePos + Vector3.up * (t * reach) + transform.right * Random.Range(-0.3f, 0.3f);
                    debris.Chips(at, -fwd, 1, 1.1f, 0.03f);
                }
                if (dustCount > 0)
                {
                    Debris d = debris;
                    float t = Random.Range(0.6f, 1f) * reach;
                    Vector3 right = transform.right;
                    d.Puff(basePos + Vector3.up * t, -fwd, right, dustCount, 0 /* ? decompiled as 0.5f; parameter is int */);
                }
            }
        }
        else if (onWall && pitch < climbPitch * 0.9f)
        {
            onWall = false;
            if (pitch > climbPitch * 0.2f && dy >= 0f)
            {
                if (debris == null)
                {
                    debris = Debris.On(this);
                }
                Vector3 p = transform.position;
                Vector3 at = p + fwd * 0.1f + Random.insideUnitSphere * 0.35f;
                Debris d = debris;
                Vector3 outward = Random.insideUnitSphere * 0.3f - fwd;
                d.Puff(at, outward, transform.right, 14, 0 /* ? decompiled as 0.8f; parameter is int */);
                Debris d2 = debris;
                // Inlined private Debris chip burst (10 chips, speed 1.3, size 0.014-0.034) replaced with public API.
                d2.Chips(at, -fwd, 10, 1.3f, 0.024f); /* ? */
            }
        }
    }
}
