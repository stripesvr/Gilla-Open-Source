// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class GillaDynamite : NetworkBehaviour
{
    public GillaItem item;
    [Tooltip("Seconds from lighting to the bang.")]
    public float fuseSeconds = 5f;
    [Tooltip("The wick, its pivot at its root on the stick: it sinks into the stick as it burns, root to tip.")]
    public Transform fuse;
    [Tooltip("The burning end: at the wick's tip when it's lit, sinking with it to the stick when it goes off.")]
    public Transform spark;
    [Tooltip("What's on while it burns (on Spark): the bud of fire and its thread of smoke.")]
    public ParticleSystem[] burning = new ParticleSystem[0];
    [Tooltip("Small bursts of the explosion's own cubes, on Spark: the fire, while it burns.")]
    public MeshRenderer[] buds = new MeshRenderer[0];
    [Tooltip("The fuse's hiss, looped while it burns (on Spark).")]
    public AudioSource hiss;
    [Tooltip("The fuse catching.")]
    public AudioClip lightSound;
    [Tooltip("The explosion: made where it goes off, on every headset.")]
    public GillaBlast blast;
    [Tooltip("Another stick going off this near sets this one off too, lit or not (m): nothing solid between them.")]
    public float chainRadius = 3.6f;

    public static readonly List<GillaDynamite> All = new List<GillaDynamite>();
    private const float BudEvery = 0.85f;
    private static readonly int BudAge = Shader.PropertyToID("_Age");
    private static readonly int BudTurn = Shader.PropertyToID("_Turn");
    private MaterialPropertyBlock budBlock;
    private float[] budWas;
    private bool litHere;
    private bool blownHere;
    private bool wentOff;
    private bool shownLit;
    private bool shownBlown;
    private float burnHere;
    private float lightWaits = -1f;
    private float buzzAt;
    private float chainAt = -1f;
    private readonly bool[] was = new bool[3];
    private Vector3 fuseAt;
    private Vector3 tipAt;
    private Vector3 rootAt;
    private float hissVolume;
    public static readonly Bounds NoBlast = new Bounds(new Vector3(4.5f, 2.5f, 59.75f), new Vector3(35f, 7f, 17.5f));

    [Networked]
    private NetworkBool Lit { get; set; }
    [Networked]
    private float Burn { get; set; }
    [Networked]
    private NetworkBool Blown { get; set; }

    private bool Live
    {
        get
        {
            if (Object == null) return false;
            if (!Object.Runner) return false;
            return Object.Runner.Exists(Object); /* ? */
        }
    }

    public static bool InSpawn(Vector3 at)
    {
        return NoBlast.Contains(at);
    }

    private Vector3 Where
    {
        get { return (spark != null ? spark : transform).position; }
    }

    private void SayNotHere(string line)
    {
        if (item == null) return;
        int hand = item.Hand;
        if (hand >= 0)
        {
            if (hand > 1) hand = 2;
            GillaToast.Show(hand, "NO DYNAMITE HERE", line, true);
        }
    }

    public bool IsLit
    {
        get
        {
            if (Live)
            {
                if (!Lit) return false;
            }
            else if (!litHere)
            {
                return false;
            }
            return !IsBlown;
        }
    }

    public float Burnt
    {
        get
        {
            if (Live) return Burn;
            return burnHere;
        }
    }

    public bool IsBlown
    {
        get
        {
            if (wentOff) return true;
            if (Live) return Blown;
            return blownHere;
        }
    }

    public static bool InHand(int hand)
    {
        GillaItem held = GillaHands.Holding(hand);
        if (held == null) return false;
        if (!held.TryGetComponent<GillaDynamite>(out GillaDynamite dynamite)) return false;
        return !dynamite.IsBlown;
    }

    private void Awake()
    {

        if (item == null)
        {
            item = GetComponent<GillaItem>();
        }
        if (fuse != null)
        {
            rootAt = fuse.localPosition;
            fuseAt = rootAt;
        }
        if (spark != null)
        {
            tipAt = spark.localPosition;
        }
        float volume = 0f;
        if (hiss != null)
        {
            volume = hiss.volume;
        }
        hissVolume = volume;
        Show();
    }

    private void OnEnable()
    {

        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public static void ChainFrom(Vector3 at)
    {
        foreach (GillaDynamite d in All)
        {
            if (d == null) continue;
            if (!d.IsBlown && d.chainAt < 0f && (!d.Live || (d.Object != null && d.Object.HasStateAuthority)))
            {
                Vector3 end = d.transform.position;
                float dist = (end - at).magnitude;
                if (dist >= 0.001f && dist <= d.chainRadius)
                {
                    if (!Physics.Linecast(at, end, ~0x1704, QueryTriggerInteraction.Ignore))
                    {
                        d.chainAt = Time.time + dist * 0.02f + Random.Range(0.01f, 0.04f);
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (chainAt >= 0f && chainAt <= Time.time)
        {
            chainAt = -1f;
            bool handled = false;
            if (Live && Object != null && Object.HasStateAuthority)
            {
                if (!Blown)
                {
                    Lit = true;
                    Burn = 1f;
                    handled = true;
                }
            }
            if (!handled)
            {
                if (!Live && !blownHere)
                {
                    litHere = true;
                    burnHere = 1f;
                }
            }
        }

        if (IsLit)
        {
            GillaRip.MakeOneMore();
        }

        for (int i = 0; i < was.Length; i++)
        {
            bool pressed = Button(i);
            if (pressed && !was[i])
            {
                if (item != null)
                {
                    if (i == item.Hand && !IsLit && !IsBlown)
                    {
                        Light();
                    }
                }
            }
            was[i] = pressed;
        }

        if (lightWaits >= 0f)
        {
            if (Live && Object != null && Object.HasStateAuthority)
            {
                lightWaits = -1f;
                Light();
            }
            else if (lightWaits < Time.time)
            {
                lightWaits = -1f;
            }
        }

        if (!Live && litHere && !blownHere)
        {
            if (InSpawn(Where))
            {
                litHere = false;
                burnHere = 0f;
                SayNotHere("IT WENT OUT IN THE SPAWN ROOM");
            }
        }

        if (!Live && litHere && !blownHere)
        {
            float burn = burnHere + Time.deltaTime / fuseSeconds;
            if (burn > 1f) burn = 1f;
            burnHere = burn;
            if (burn >= 1f)
            {
                blownHere = true;
                GoOff((spark != null ? spark : transform).position);
            }
        }

        if (IsLit && item != null)
        {
            if ((uint)item.Hand < 2 && buzzAt <= Time.time)
            {
                buzzAt = Time.time + 0.09f;
                XRNode node = item.Hand != 0 ? XRNode.RightHand : XRNode.LeftHand;
                UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
                device.SendHapticImpulse(0, Burnt * 0.25f + 0.08f, 0.06f);
            }
        }

        Show();
    }

    private void Light()
    {
        if (InSpawn(Where))
        {
            lightWaits = -1f;
            SayNotHere("NOT IN THE SPAWN ROOM");
            return;
        }
        if (Live && (Object == null || !Object.HasStateAuthority))
        {
            lightWaits = Time.time + 1.5f;
        }
        else
        {
            if (Live)
            {
                Lit = true;
                Burn = 0f;
            }
            else
            {
                burnHere = 0f;
                litHere = true;
            }
            if (item != null)
            {
                int hand = item.Hand;
                if ((uint)hand < 2)
                {
                    XRNode node = hand != 0 ? XRNode.RightHand : XRNode.LeftHand;
                    UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
                    device.SendHapticImpulse(0, 0.6f, 0.12f);
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.HasStateAuthority) return;
        if (!Lit) return;
        if (Blown) return;
        if (InSpawn(Where))
        {
            Lit = false;
            Burn = 0f;
            SayNotHere("IT WENT OUT IN THE SPAWN ROOM");
            return;
        }
        float burn = Burn + Runner.DeltaTime / fuseSeconds;
        if (burn > 1f) burn = 1f;
        Burn = burn;
        if (Burn < 1f) return;
        Blown = true;
        RPC_GoOff((spark != null ? spark : transform).position);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_GoOff(Vector3 at)
    {
        GoOff(at);
    }

    private void GoOff(Vector3 at)
    {
        if (wentOff) return;
        wentOff = true;
        Show();
        Vector3 pos = (spark != null ? spark : transform).position;
        if (blast != null)
        {
            Vector3 from = pos;
            if ((pos - at).sqrMagnitude >= 4f)
            {
                from = at;
            }
            GillaBlast.Go(blast, at, from);
        }
        if (item == null) return;
        if (Live)
        {
            if (Object == null) return;
            if (!Object.HasStateAuthority) return;
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

    private void Show()
    {
        bool lit = IsLit;
        bool blown;
        if (wentOff)
        {
            blown = true;
        }
        else if (Live)
        {
            if (Object != null && Object.HasStateAuthority)
            {
                blown = Blown;
            }
            else
            {
                blown = false;
            }
        }
        else
        {
            blown = blownHere;
        }

        float burnt = Burnt;
        if (lit && !shownLit)
        {
            if (lightSound != null && spark != null)
            {
                AudioClip[] clips = new AudioClip[1];
                clips[0] = lightSound;
                Sfx.Play(clips, spark.position, 0.7f, 0.95f, 1.05f);
            }
        }
        shownLit = lit;
        Vector3 sink = (tipAt - rootAt) * burnt;
        if (fuse != null)
        {
            fuse.localPosition = fuseAt - sink;
        }
        if (spark != null)
        {
            spark.localPosition = tipAt - sink;
        }
        for (int i = 0; i < burning.Length; i++)
        {
            ParticleSystem ps = burning[i];
            if (ps == null) continue;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = lit;
            if (lit && !ps.isPlaying)
            {
                ps.Play();
            }
        }
        ShowBuds(!blown && lit);
        if (hiss != null)
        {
            AudioSource src = hiss;
            if (lit)
            {
                if (!src.isPlaying)
                {
                    float length = 0f;
                    if (src.clip != null)
                    {
                        length = hiss.clip.length;
                    }
                    src.time = Random.Range(0f, length);
                    hiss.Play();
                }
            }
            else if (src.isPlaying)
            {
                hiss.Stop();
            }
            hiss.volume = hissVolume;
            hiss.pitch = Mathf.Clamp01(burnt) * 0.3f + 0.95f;
        }
        if (shownBlown == blown) return;
        shownBlown = blown;
        if (!blown) return;
        for (int i = 0; i < burning.Length; i++)
        {
            ParticleSystem ps = burning[i];
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }

    private void ShowBuds(bool on)
    {
        if (buds == null) return;
        if (budBlock == null)
        {
            budBlock = new MaterialPropertyBlock();
        }
        if (budWas == null)
        {
            budWas = new float[buds.Length];
        }
        for (int i = 0; i < buds.Length; i++)
        {
            MeshRenderer bud = buds[i];
            if (bud == null) continue;
            if (bud.enabled != on)
            {
                bud.enabled = on;
            }
            if (!on) continue;
            float t = Time.time + (i * BudEvery) / buds.Length;
            t = t - (int)(t / BudEvery) * BudEvery;
            float age = 0f;
            if (t >= 0f)
            {
                age = t;
                if (t > BudEvery) age = BudEvery;
            }
            if (age < budWas[i])
            {
                bud.transform.localEulerAngles = Vector3.up * Random.Range(0f, 360f);
            }
            budWas[i] = age;
            Vector3 euler = bud.transform.localEulerAngles;
            budBlock.SetFloat(BudAge, age);
            float rad = euler.y * 0.017453292f;
            budBlock.SetVector(BudTurn, new Vector4(Mathf.Cos(rad), Mathf.Sin(rad), 0f, 0f));
            bud.SetPropertyBlock(budBlock);
        }
    }

    public void LightForTest()
    {
        Light();
    }

    private static bool Button(int hand)
    {
        XRNode node;
        if (hand == 0)
        {
            node = XRNode.LeftHand;
        }
        else
        {
            if (hand == 2)
            {
                Keyboard keyboard = Keyboard.current;
                if (!Application.isEditor) return false;
                if (keyboard == null) return false;
                return keyboard.bKey.isPressed;
            }
            node = XRNode.RightHand;
        }
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool pressed = false;
        if (device.isValid)
        {
            device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out pressed);
        }
        return pressed; /* ? */
    }
}
