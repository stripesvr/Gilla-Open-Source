// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.XR;

public class SoulScythe : MonoBehaviour, IToyMove
{
    [Tooltip("The blade, from where it meets the handle to its point.")]
    public Transform bladeBase;
    [Tooltip("The blade, from where it meets the handle to its point.")]
    public Transform tip;
    [Tooltip("The arc it leaves when swung.")]
    public TrailRenderer arc;
    [Tooltip("How fast (m/s) the point must move for the arc to show.")]
    public float arcSpeed = 2.2f;
    [Tooltip("How near (m) the blade must pass a player's head or body to yank their soul.")]
    public float hitRadius = 0.25f;
    [Tooltip("Seconds between yanks, and before the same player can be yanked again.")]
    public float cooldown = 0.7f;
    [Tooltip("Seconds between yanks, and before the same player can be yanked again.")]
    public float perPlayer = 2.5f;
    [Tooltip("The soul's look (Gilla/SoulGhost).")]
    public Material ghost;
    public AudioClip[] swishes;
    public AudioClip[] yanks;
    public AudioClip[] snaps;
    private bool live;
    private int hand = -1;
    private Vector3 lastTip;
    private float speed;
    private float lastSwish;
    private float lastYank;
    private bool swinging;
    private readonly Dictionary<int, float> yanked = new Dictionary<int, float>();
    private static SoulScythe prefab;

    public void Live()
    {
        live = true;
    }

    public void Taken(int h)
    {
        hand = h;
    }

    public bool LetGo(Vector3 velocity)
    {
        hand = -1;
        return false;
    }

    private void OnEnable()
    {

        if (tip != null)
        {
            lastTip = tip.position;
        }
        if (arc != null)
        {
            arc.Clear();
            arc.emitting = false;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (tip == null || !(0f < dt)) return;

        Vector3 pos = tip.position;
        Vector3 delta = pos - lastTip;
        lastTip = pos;
        float raw = delta.magnitude / dt;
        float k = Mathf.Clamp01(1f - Mathf.Exp(dt * -25f));
        speed = speed + (raw - speed) * k;
        bool moving = raw < 60f && speed > arcSpeed;

        if (arc != null)
        {
            arc.emitting = moving;
        }
        if (moving && !swinging && 0.25f < Time.time - lastSwish && swishes != null && swishes.Length != 0)
        {
            lastSwish = Time.time;
            float volume = Mathf.Clamp(speed * 0.125f, 0.25f, 0.8f);
            Sfx.Play(swishes, pos, volume, 0.9f, 1.15f);
        }
        swinging = moving;
        if (live && moving && hand > -1)
        {
            Reap();
        }
    }

    private void Reap()
    {
        NetworkApe local = NetworkApe.Local;
        if (local == null || !(cooldown <= Time.time - lastYank)) return;
        if (bladeBase == null) return;

        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null) continue;
            if (ape == local) continue;
            if (ape.head == null /* ? */ || ape.IsMonster || ape.Busy || ape.IsTorn(0)) continue;
            float when;
            if (yanked.TryGetValue(ape.Object.InputAuthority.PlayerId /* ? */, out when) && Time.time - when < perPlayer) continue;
            Vector3 p = ape.head.position; /* ? */
            if (hitRadius < Near(p) && hitRadius < Near(p - Vector3.up * 0.3f)) continue;

            int key = ape.Object.InputAuthority.PlayerId; /* ? */
            lastYank = Time.time;
            yanked[key] = lastYank;
            local.RPC_SoulYank(ape.Object.InputAuthority /* ? */);
            if (hand > -1)
            {
                InputDevice device = InputDevices.GetDeviceAtXRNode(hand != 0 ? XRNode.RightHand : XRNode.LeftHand);
                device.SendHapticImpulse(0, 0.8f, 0.12f);
            }
            break;
        }
    }

    private float Near(Vector3 p)
    {
        Vector3 a = bladeBase.position;
        Vector3 ab = tip.position - a;
        float len = Mathf.Max(ab.sqrMagnitude, 1e-06f);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len);
        return (a + ab * t - p).magnitude;
    }

    public static void Yank(NetworkApe by, PlayerRef target)
    {
        SoulScythe scythe = Prefab();
        if (scythe == null) return;
        if (by == null) return;
        if (by.head == null) return;

        bool isLocal = NetworkApe.Local != null && NetworkApe.Local.Object.InputAuthority == target; /* ? */
        List<SkinnedMeshRenderer> body = new List<SkinnedMeshRenderer>();
        if (isLocal)
        {
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            if (boot != null && boot.tearing != null)
            {
                Dismemberment[] copies = boot.tearing.copies;
                if (copies != null)
                {
                    for (int i = 0; i < copies.Length; i++)
                    {
                        Dismemberment copy = copies[i];
                        if (copy != null)
                        {
                            if (Array.Exists(copy.parts, p => p.id == 0 && p.renderer != null))
                            {
                                body.AddRange(copy.GetComponentsInChildren<SkinnedMeshRenderer>());
                            }
                        }
                    }
                }
            }
            XRNode[] nodes = new XRNode[] { XRNode.LeftHand, XRNode.RightHand };
            for (int i = 0; i < nodes.Length; i++)
            {
                InputDevice device = InputDevices.GetDeviceAtXRNode(nodes[i]);
                device.SendHapticImpulse(0, 0.9f, 0.35f);
            }
        }
        else
        {
            NetworkApe ape = NetworkApe.Find(target);
            if (ape == null) return;
            if (ape.ape == null) return;
            body.AddRange(ape.ape.GetComponentsInChildren<SkinnedMeshRenderer>());
        }
        body.RemoveAll(r => r == null || !r.enabled || r.sharedMesh == null);
        if (body.Count != 0)
        {
            SoulGhost.Spawn(body, by.head.position, scythe);
        }
    }

    private static SoulScythe Prefab()
    {
        if (prefab != null)
        {
            return prefab;
        }
        for (int i = 1; i <= CosmeticCatalog.Count; i++)
        {
            CosmeticCatalog.Entry entry = CosmeticCatalog.Of(i);
            if (entry.prefab != null)
            {
                SoulScythe found;
                if (entry.prefab.TryGetComponent<SoulScythe>(out found))
                {
                    prefab = found;
                    return found;
                }
            }
        }
        return null;
    }
}
