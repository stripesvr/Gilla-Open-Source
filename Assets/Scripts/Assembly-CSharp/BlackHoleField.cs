// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public class BlackHoleField : MonoBehaviour
{
    private const float Grow = 0.45f;
    private const float Pull = 2.6f;
    private const float Shut = 0.35f;
    private const float Radius = 5f;
    private float t;
    private bool mine;
    private bool burst;
    private PocketBlackHole look;
    private Transform horizon;
    private Transform disc;
    private ParticleSystem swirl;
    private Light glow;
    private AudioSource hum;
    private float glowMax;
    private readonly List<Rigidbody> caught = new List<Rigidbody>();
    private readonly HashSet<Rigidbody> gravityOff = new HashSet<Rigidbody>();

    public void Begin(PocketBlackHole p, bool yours)
    {
        look = p;
        mine = yours;
        horizon = transform.Find("Horizon");
        disc = transform.Find("Disc");
        Transform swirlT = transform.Find("Swirl");
        if (swirlT != null)
        {
            swirl = swirlT.GetComponent<ParticleSystem>();
        }
        glow = GetComponentInChildren<Light>();
        if (glow != null)
        {
            glowMax = glow.intensity;
        }
        transform.localScale = Vector3.zero;
        if (p.opens != null)
        {
            Sfx.Play(p.opens, transform.position, 0.9f, 0.95f, 1.05f);
        }
        if (!(p.hum != null))
        {
            return;
        }
        hum = gameObject.AddComponent<AudioSource>();
        hum.clip = p.hum;
        hum.loop = true;
        hum.spatialBlend = 1f;
        hum.minDistance = 1.5f;
        hum.maxDistance = 25f;
        hum.rolloffMode = AudioRolloffMode.Linear;
        hum.dopplerLevel = 0f;
        hum.volume = 0f;
        hum.Play();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        float s;
        if (t < Grow)
        {
            s = CosmeticToy.Elastic(t / Grow);
        }
        else if (t < Pull)
        {
            s = Mathf.Sin(t * 9f) * 0.04f + 1f;
        }
        else
        {
            s = 1f - Mathf.Clamp01((t - Pull) / Shut);
        }
        s = Mathf.Max(s, 0f);
        transform.localScale = s * Vector3.one;
        if (disc != null)
        {
            disc.Rotate(0f, dt * 420f, 0f, Space.Self);
        }
        if (glow != null)
        {
            glow.intensity = s * glowMax * (t >= Pull ? 2f : 1f);
        }
        if (hum != null)
        {
            float fade = Mathf.Clamp01(t / Grow);
            float vol = t >= Pull ? Mathf.Max(0.7f - (t - Pull) * 3f, 0f) : 0.7f;
            hum.volume = fade * vol;
            hum.pitch = Mathf.Clamp01(t / Pull) * 0.5f + 0.8f;
        }
        if (swirl != null)
        {
            ParticleSystem.EmissionModule emission = swirl.emission;
            emission.enabled = t < Pull;
        }
        if (!burst && t >= Pull)
        {
            Burst();
        }
        if (t >= Pull + Shut + 0.6f)
        {
            if (mine)
            {
                GillaToys.Back();
            }
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (t < 0.15f || burst)
        {
            return;
        }
        Vector3 center = transform.position;
        caught.Clear();
        foreach (GillaItem item in GillaItem.All)
        {
            if (item == null) continue;
            if (item.Mine && item.isActiveAndEnabled /* ? */)
            {
                if ((item.transform.position - center).sqrMagnitude < Radius * Radius)
                {
                    Rigidbody body = item.GetComponent<Rigidbody>() /* ? */;
                    if (body != null)
                    {
                        caught.Add(body);
                    }
                }
            }
        }
        TornPiece[] pieces = UnityEngine.Object.FindObjectsByType<TornPiece>(FindObjectsSortMode.None);
        for (int i = 0; i < pieces.Length; i++)
        {
            TornPiece piece = pieces[i];
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb == null) continue;
            if ((rb.position - center).sqrMagnitude < Radius * Radius)
            {
                if (rb.isKinematic && piece.TryGetComponent<GillaCorpse>(out GillaCorpse corpse))
                {
                    corpse.SendMessage("Loosen", SendMessageOptions.DontRequireReceiver) /* ? */;
                }
                caught.Add(rb);
            }
        }
        float k = Mathf.Clamp01(1f - Mathf.Exp(Time.fixedDeltaTime * -5f));
        foreach (Rigidbody rb in caught)
        {
            if (rb == null) continue;
            if (rb.isKinematic) continue;
            if (rb.useGravity)
            {
                rb.useGravity = false;
                gravityOff.Add(rb);
            }
            Vector3 toCenter = center - rb.position;
            float dist = toCenter.magnitude;
            Vector3 dir = toCenter / dist;
            Vector3 tangent = Vector3.Cross(Vector3.up, dir).normalized;
            float inward;
            float closeness;
            if (dist <= 0.6f)
            {
                inward = (dist - 0.45f) * 6f;
                closeness = 1f - dist / Radius;
            }
            else
            {
                closeness = 1f - dist / Radius;
                inward = Mathf.Clamp01(closeness) * 6f + 3f;
            }
            float spin = Mathf.Clamp01(closeness) * 4f + 2f;
            float dy = center.y - rb.position.y;
            Vector3 target = dir * inward + tangent * spin + Vector3.up * dy + Vector3.up * dy;
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = v + (target - v) * k;
            Vector3 av = rb.angularVelocity;
            rb.angularVelocity = av + (UnityEngine.Random.insideUnitSphere * 8f - av) * 0.05f;
            rb.WakeUp();
        }
    }

    private void Burst()
    {
        burst = true;
        Vector3 at = transform.position;
        foreach (Rigidbody rb in gravityOff)
        {
            if (rb == null) continue;
            rb.useGravity = true;
            if (rb.isKinematic) continue;
            Vector3 away = (rb.position - at).normalized;
            Vector3 dir = (away + Vector3.up * 0.6f + UnityEngine.Random.insideUnitSphere * 0.4f).normalized;
            rb.linearVelocity = dir * UnityEngine.Random.Range(5f, 8.5f);
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
        }
        gravityOff.Clear();
        if (look != null)
        {
            if (look.bursts != null)
            {
                Sfx.Play(look.bursts, at, 1f, 0.95f, 1.05f);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (Rigidbody rb in gravityOff)
        {
            if (rb != null)
            {
                rb.useGravity = true;
            }
        }
    }
}
