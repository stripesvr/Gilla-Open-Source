// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PocketBlackHole : MonoBehaviour, IToyMove
{
    [Tooltip("The floating ball: bobs where it's worn.")]
    public Transform orb;
    [Tooltip("Its ring: spins.")]
    public Transform ring;
    [Tooltip("What opens where it lands (BlackHoleField).")]
    public GameObject field;
    [Tooltip("Let go faster than this (m/s) and it's thrown.")]
    public float throwSpeed = 1.3f;
    public AudioClip[] throws;
    public AudioClip[] opens;
    public AudioClip[] bursts;
    [Tooltip("The hum while it's open.")]
    public AudioClip hum;
    private bool live;
    private Vector3 orbRest;
    private float phase;
    private static PocketBlackHole prefab;
    private static readonly List<BlackHoleFlight> flying = new List<BlackHoleFlight>();

    private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static object GetMember(System.Type type, object target, string name)
    {
        if (type == null) return null;
        FieldInfo f = type.GetField(name, AnyMember);
        if (f != null) return f.GetValue(target);
        PropertyInfo p = type.GetProperty(name, AnyMember);
        if (p != null) return p.GetValue(target);
        return null;
    }

    private static void SetMember(System.Type type, object target, string name, object value)
    {
        if (type == null) return;
        FieldInfo f = type.GetField(name, AnyMember);
        if (f != null) { f.SetValue(target, value); return; }
        PropertyInfo p = type.GetProperty(name, AnyMember);
        if (p != null && p.CanWrite) p.SetValue(target, value);
    }

    private static object Get(object target, string name)
    {
        return target == null ? null : GetMember(target.GetType(), target, name);
    }

    private static void Set(object target, string name, object value)
    {
        if (target != null) SetMember(target.GetType(), target, name, value);
    }

    private static object Call(object target, string name, params object[] args)
    {
        if (target == null) return null;
        MethodInfo m = target.GetType().GetMethod(name, AnyMember);
        return m != null ? m.Invoke(target, args) : null;
    }

    public void Live()
    {
        live = true;
    }

    public void Taken(int h)
    {
    }

    public bool LetGo(Vector3 velocity)
    {
        if (live)
        {
            if (throwSpeed <= velocity.magnitude)
            {
                Transform t = orb != null ? orb : transform;
                Vector3 from = t.position;
                float size = transform.lossyScale.x;
                Launch(this, from, velocity, null, size);
                NetworkApe local = NetworkApe.Local;
                if (local != null)
                {
                    var obj = local.Object;
                    if (obj != null)
                    {
                        if (obj.Runner && obj.HasStateAuthority /* ? */)
                        {
                            local.RPC_BlackHoleThrow(from, velocity, size);
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }

    private void Awake()
    {

        if (orb != null)
        {
            orbRest = orb.localPosition;
        }
        phase = Random.value * 10f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        phase += dt;
        if (ring != null)
        {
            ring.Rotate(0f, dt * 260f, 0f, Space.Self);
        }
        if (orb != null)
        {
            orb.localPosition = orbRest + Vector3.up * (Mathf.Sin(phase * 2.2f) * 0.01f) /* ? */;
        }
    }

    private static PocketBlackHole Prefab()
    {
        if (prefab != null)
        {
            return prefab;
        }
        for (int i = 1; i <= CosmeticCatalog.Count; i++)
        {
            CosmeticCatalog.Entry e = CosmeticCatalog.Of(i);
            if (e.prefab != null)
            {
                if (e.prefab.TryGetComponent<PocketBlackHole>(out PocketBlackHole p))
                {
                    prefab = p;
                    return p;
                }
            }
        }
        return null;
    }

    public static void ShowThrow(NetworkApe by, Vector3 from, Vector3 velocity, float size)
    {
        PocketBlackHole look = Prefab();
        if (look != null)
        {
            Launch(look, from, velocity, by, size);
        }
    }

    public static void Open(NetworkApe by, Vector3 at)
    {
        object ownerObj = by != null ? Get(by, "owner") /* ? */ : null;
        int owner = ownerObj is int o ? o : -1;
        for (int index = flying.Count - 1; index >= 0; index--)
        {
            BlackHoleFlight f = flying[index];
            if (f == null)
            {
                flying.RemoveAt(index);
            }
            else if (Get(f, "owner") is int fo && fo == owner /* ? */)
            {
                flying.RemoveAt(index);
                Destroy(f.gameObject);
            }
        }

        PocketBlackHole look = Prefab();
        if (look == null || look.field == null)
        {
            if (by == null || by == NetworkApe.Local)
            {
                object you = GetMember(typeof(GillaToys), null, "you") /* ? */;
                if (you != null)
                {
                    Component toy = Get(you, "toy") as Component /* ? */;
                    if (toy == null) return;
                    if (!(Get(you, "away") is bool away) || !away) return;
                    Set(you, "away", false) /* ? */;
                    toy.gameObject.SetActive(true);
                    Call(you, "GoHome", true) /* ? */;
                }
            }
            return;
        }

        GameObject go = Instantiate<GameObject>(look.field, at, Quaternion.identity);
        go.name = "BlackHole";
        BlackHoleField bh = go.GetComponent<BlackHoleField>();
        if (bh == null)
        {
            bh = go.AddComponent<BlackHoleField>();
        }
        bool mine = by == null || by == NetworkApe.Local;
        Set(bh, "look", look) /* ? */;
        Set(bh, "mine", mine) /* ? */;
        Set(bh, "horizon", bh.transform.Find("Horizon")) /* ? */;
        Set(bh, "disc", bh.transform.Find("Disc")) /* ? */;
        Transform swirl = bh.transform.Find("Swirl");
        if (swirl != null)
        {
            Set(bh, "swirl", swirl.GetComponent<ParticleSystem>()) /* ? */;
        }
        Light glow = bh.GetComponentInChildren<Light>();
        Set(bh, "glow", glow) /* ? */;
        if (glow != null)
        {
            Set(bh, "glowIntensity", glow.intensity) /* ? */;
        }
        bh.transform.localScale = Vector3.zero;
        if (look.opens != null)
        {
            Sfx.Play(look.opens, bh.transform.position, 0.9f, 0.95f, 1.05f);
        }
        if (look.hum == null)
        {
            return;
        }
        AudioSource src = bh.gameObject.AddComponent<AudioSource>();
        Set(bh, "humSource", src) /* ? */;
        src.clip = look.hum;
        src.loop = true;
        src.spatialBlend = 1f;
        src.minDistance = 1.5f;
        src.maxDistance = 25f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.dopplerLevel = 0f;
        src.volume = 0f;
        src.Play();
    }

    private static void Launch(PocketBlackHole look, Vector3 from, Vector3 velocity, NetworkApe by, float size)
    {
        CosmeticCatalog.Entry e = Entry(look);
        GameObject display = e != null ? Call(e, "Display") as GameObject /* ? */ : null;
        GameObject go = new GameObject("BlackHole_Flying");
        go.transform.position = from;
        go.transform.localScale = Vector3.one * Mathf.Clamp(size, 0.01f, 100f);
        BlackHoleFlight flight = go.AddComponent<BlackHoleFlight>();
        flight.Begin(look, display, velocity, by);
        flying.Add(flight);
        if (look.throws == null)
        {
            return;
        }
        Sfx.Play(look.throws, from, 0.6f, 0.95f, 1.1f) /* ? */;
    }

    private static CosmeticCatalog.Entry Entry(PocketBlackHole look)
    {
        for (int i = 1; i <= CosmeticCatalog.Count; i++)
        {
            CosmeticCatalog.Entry e = CosmeticCatalog.Of(i);
            if (e.prefab != null)
            {
                if (e.prefab.TryGetComponent<PocketBlackHole>(out PocketBlackHole p))
                {
                    return e;
                }
            }
        }
        return null;
    }
}
