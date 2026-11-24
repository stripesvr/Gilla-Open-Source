// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using Fusion;

public class GillaEgg : NetworkBehaviour
{
    public GillaItem item;
    [Tooltip("How hard it has to hit something to break (m/s: a drop of about half a metre).")]
    public float breakSpeed = 3f;
    [Tooltip("How fast it has to be going to break on someone (m/s).")]
    public float hitsAt = 1.5f;
    [Tooltip("What's left: its shell's bits; the yolk on a floor; a splat up a wall.")]
    public GillaEggMess shards;
    [Tooltip("What's left: its shell's bits; the yolk on a floor; a splat up a wall.")]
    public GillaEggMess puddle;
    [Tooltip("What's left: its shell's bits; the yolk on a floor; a splat up a wall.")]
    public GillaEggMess splat;
    public AudioClip[] crackSounds;
    public AudioClip[] splatSounds;
    [Tooltip("How big the yolk is on someone (m).")]
    public float stain = 0.15f;
    private const float BodyDown = 0.62f;
    private const float BodyRound = 0.24f;
    private const float MonsterRound = 0.65f;
    private const float NotYouFor = 0.5f;
    private bool broke;
    private Vector3 lastAt;
    private float letGoAt = -10f;
    private int wasHand = -1;

    [Networked]
    private NetworkBool Broken { get; set; }

    private bool Live
    {
        get
        {
            if (Object == null)
            {
                return false;
            }
            if (!Object.Runner)
            {
                return false;
            }
            return Object.Runner.Exists(Object); /* ? */
        }
    }

    private void Awake()
    {

        if (item == null)
        {
            item = GetComponent<GillaItem>();
        }
        lastAt = transform.position;
    }

    public override void Spawned()
    {
        lastAt = transform.position;
        if (Broken)
        {
            Hide();
        }
    }

    private void Update()
    {
        if (broke) return;
        if (item == null) return;
        if (item.Hand < 0 && wasHand >= 0)
        {
            letGoAt = Time.time;
        }
        wasHand = item.Hand;
        float dt = Time.deltaTime;
        Vector3 b = transform.position;
        Vector3 a = lastAt;
        Vector3 delta = b - a;
        lastAt = b;
        if (!item.Mine) return;
        if (item.Gone) return;
        if (item.Swallowed) return;
        if (dt <= 0f) return;
        float speed = delta.magnitude / dt;
        if (speed < hitsAt) return;
        if (40f < speed) return;
        LooksForSomeone(a, b);
    }

    private void OnCollisionEnter(Collision hit)
    {
        if (broke) return;
        if (item == null) return;
        if (!item.Mine) return;
        if (item.Hand >= 0) return;
        if (hit.contactCount == 0) return;
        if (breakSpeed <= hit.relativeVelocity.magnitude)
        {
            ContactPoint contact = hit.GetContact(0);
            Break(contact.point, contact.normal, -1, null);
        }
    }

    public void Smash()
    {
        if (broke) return;
        Break(transform.position, Vector3.up, -1, null);
    }

    private void LooksForSomeone(Vector3 a, Vector3 b)
    {
        Vector3 at;
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null) continue;
            if (ape.head /* ? */ == null) continue;
            if (ape == NetworkApe.Local)
            {
                if (item.Hand >= 0 || Time.time - letGoAt < NotYouFor) continue;
            }
            if (Near(a, b, ape.head /* ? */.position, BodyDown, BodyRound, out at))
            {
                Break(at, (at - ape.head /* ? */.position).normalized, ape.Object.InputAuthority.PlayerId /* ? */, null);
                return;
            }
        }
        foreach (GillaDummy dummy in GillaDummy.All)
        {
            if (dummy == null) continue;
            if (!dummy.isActiveAndEnabled /* ? */) continue;
            if (dummy.head /* ? */ == null) continue;
            if (Near(a, b, dummy.head /* ? */.position, BodyDown, BodyRound, out at))
            {
                Break(at, (at - dummy.head /* ? */.position).normalized, -1, dummy);
                return;
            }
        }
        foreach (MonsterBrain brain in MonsterBrain.All)
        {
            if (brain == null) continue;
            if (!brain.isActiveAndEnabled /* ? */) continue;
            Vector3 middle = brain.Middle;
            if (Near(a, b, middle + Vector3.up * 0.3f, 0.6f, MonsterRound, out at))
            {
                Break(at, (at - middle).normalized, -1, null);
                return;
            }
        }
    }

    private static bool Near(Vector3 a, Vector3 b, Vector3 top, float down, float round, out Vector3 at)
    {
        for (int i = 1; i < 4; i++)
        {
            float t = Mathf.Clamp01(i / 3f);
            Vector3 p = Vector3.Lerp(a, b, t);
            float y = Mathf.Clamp(p.y, top.y - down, top.y);
            Vector3 d = new Vector3(p.x - top.x, p.y - y, p.z - top.z);
            if (d.sqrMagnitude < round * round)
            {
                at = p;
                return true;
            }
        }
        at = b;
        return false;
    }

    private void Break(Vector3 at, Vector3 normal, int player, GillaDummy dummy)
    {
        if (broke) return;
        if (!Live)
        {
            Broke(at, normal, player, dummy);
            return;
        }
        NetworkId id = default;
        if (dummy != null && dummy.Object != null)
        {
            id = dummy.Object.Id;
        }
        RPC_Broke(at, normal, player, id);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Broke(Vector3 at, Vector3 normal, int player, NetworkId dummy)
    {
        GillaDummy found = null;
        if (dummy.Raw != 0 && Runner != null)
        {
            NetworkObject obj = Runner.FindObject(dummy);
            if (obj != null)
            {
                found = obj.GetComponent<GillaDummy>();
            }
        }
        Broke(at, normal, player, found);
    }

    private void Broke(Vector3 at, Vector3 normal, int player, GillaDummy dummy)
    {
        if (broke) return;
        broke = true;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = Vector3.up;
        }
        Vector3 n = normal.normalized;
        Sfx.Play(crackSounds, at, 0.8f, 0.95f, 1.1f);
        Sfx.Play(splatSounds, at, 0.8f, 0.92f, 1.08f);
        Blood.Yolk(at, n);
        if (shards != null)
        {
            GillaEggMess mess = Instantiate(shards, at + n * 0.04f, UnityEngine.Random.rotation);
            mess.Fling(n, 2.6f);
        }
        bool onSomeone;
        if (player < 0)
        {
            onSomeone = dummy != null;
        }
        else
        {
            onSomeone = true;
            GillaSplatter.Stain(player, at, stain, GillaSplatter.Stuff.Yolk);
        }
        if (dummy != null && dummy.tearing != null)
        {
            GillaSplatter.Stain(dummy.tearing, at, stain, GillaSplatter.Stuff.Yolk);
        }
        Lay(at, n, onSomeone);
        Hide();
        if (item == null) return;
        if (Live)
        {
            if (Object == null) return;
            if (!Object.HasStateAuthority) return; /* ? */
        }
        if (Live)
        {
            Broken = true;
        }
        if (item.Hand >= 0)
        {
            GillaHands.Drop(item);
        }
        Collider[] colliders = item.Colliders;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c != null)
            {
                c.enabled = false;
            }
        }
        item.Recycle();
    }

    private void Lay(Vector3 at, Vector3 normal, bool onSomeone)
    {
        Transform t;
        Vector3 scale;
        float k;
        if (onSomeone || 0.6f < normal.y)
        {
            Vector3 point = at;
            Vector3 n = normal;
            if (onSomeone)
            {
                RaycastHit hit;
                if (!Physics.Raycast(at, Vector3.down, out hit, 3f, ~0x1704, QueryTriggerInteraction.Ignore))
                {
                    return;
                }
                n = hit.normal;
                point = hit.point;
            }
            if (puddle == null) return;
            Quaternion rot = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), n) * Quaternion.FromToRotation(Vector3.up, n);
            GillaEggMess mess = Instantiate(puddle, point + n * 0.002f, rot);
            t = mess.transform;
            scale = t.localScale;
            k = onSomeone ? 0.6f : UnityEngine.Random.Range(0.85f, 1.15f);
        }
        else
        {
            if (splat == null) return;
            Quaternion rot = Quaternion.AngleAxis(UnityEngine.Random.Range(-35f, 35f), normal) * Quaternion.FromToRotation(Vector3.up, normal);
            GillaEggMess mess = Instantiate(splat, at + normal * 0.002f, rot);
            t = mess.transform;
            scale = t.localScale;
            k = UnityEngine.Random.Range(0.9f, 1.2f);
        }
        t.localScale = scale * k;
    }

    private void Hide()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }
}
