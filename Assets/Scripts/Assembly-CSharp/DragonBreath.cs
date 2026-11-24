// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class DragonBreath : MonoBehaviour
{
    public ParticleSystem flames;
    public ParticleSystem embers;
    public ParticleSystem smoke;
    [Tooltip("Lights up the walls while you breathe.")]
    public Light glow;
    [Tooltip("The fire's roar, looped (its volume follows the fire).")]
    public AudioSource roar;
    [Tooltip("How far into a scream (0-1) the fire starts.")]
    [Range(0, 1)]
    public float from = 0.12f;
    public float flameRate = 160f;
    public float emberRate = 45f;
    public float smokeRate = 22f;
    public float glowIntensity = 2.2f;
    public float roarVolume = 0.9f;
    [Tooltip("Talking's fire: how much of the scream's (0-1) a word puffs out, at its loudest.")]
    [Range(0, 1)]
    public float talkFire = 0.18f;
    private ApeFace face;
    private float fire;
    private bool mirror;
    private bool firstPerson;

    private void Start()
    {
        CosmeticWearer wearer = GetComponentInParent<CosmeticWearer>();
        if (wearer != null)
        {
            face = wearer.GetComponentInChildren<ApeFace>() /* ? */;
        }
        else
        {
            face = GetComponentInParent<ApeFace>();
        }
        mirror = gameObject.layer == LayerMask.NameToLayer("MirrorOnly");
        firstPerson = gameObject.layer == LayerMask.NameToLayer("FirstPerson");
        if (mirror)
        {
            if (glow != null)
            {
                glow.enabled = false;
            }
            if (roar != null)
            {
                roar.enabled = false;
            }
        }
        if (firstPerson)
        {
            ParticleSystem[] systems = new ParticleSystem[] { flames, embers, smoke };
            foreach (ParticleSystem ps in systems)
            {
                if (ps != null)
                {
                    ps.GetComponent<ParticleSystemRenderer>().enabled = false;
                }
            }
        }
        Set(0f);
    }

    private void Update()
    {
        float talk = 0f;
        float scream;
        if (face != null)
        {
            scream = face.Scream;
            talk = face.Talk;
        }
        else
        {
            scream = Yours(out talk);
        }
        float target = Mathf.InverseLerp(from, 0.85f, scream);
        float talking = talkFire * Mathf.Clamp01(talk * 1.4f - 0.2f);
        target = Mathf.Max(target, talking);
        float speed = target > fire ? 6f : 3f;
        fire = Mathf.MoveTowards(fire, target, Time.deltaTime * speed);
        Set(fire);
    }

    private static float Yours(out float talk)
    {
        talk = 0f;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot == null)
        {
            return 0f;
        }
        if (boot.mic == null)
        {
            return 0f;
        }
        talk = Mathf.Clamp01(boot.mic.Level / 0.7f);
        return Mathf.Clamp01((boot.mic.Level - 0.7f) / 0.3f);
    }

    private void Set(float k)
    {
        Rate(flames, flameRate * k);
        Rate(embers, emberRate * k);
        Rate(smoke, smokeRate * k);
        if (glow != null && !mirror)
        {
            glow.enabled = k > 0.01f;
            glow.intensity = glowIntensity * k * (Mathf.PerlinNoise(Time.time * 14f, 0.3f) * 0.3f + 0.85f);
        }
        if (roar == null)
        {
            return;
        }
        if (mirror)
        {
            return;
        }
        if (k > 0.01f && !roar.isPlaying)
        {
            roar.Play();
        }
        roar.volume = roarVolume * k;
        roar.pitch = k * 0.2f + 0.9f;
        if (k > 0.01f)
        {
            return;
        }
        if (roar.isPlaying)
        {
            roar.Stop();
        }
    }

    private static void Rate(ParticleSystem ps, float rate)
    {
        if (ps == null)
        {
            return;
        }
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;
        if (rate > 0.01f && !ps.isPlaying)
        {
            ps.Play();
        }
    }
}
