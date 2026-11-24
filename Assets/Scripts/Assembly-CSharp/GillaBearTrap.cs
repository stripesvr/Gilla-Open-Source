// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.XR;

public class GillaBearTrap : NetworkBehaviour
{
    public GillaItem item;
    [Header("What moves (GillaSetup.BearTrap: as Tools/gilla_bear_trap_model.py has them)")]
    public Transform jawL;
    public Transform jawR;
    public Transform springL;
    public Transform springR;
    public Transform pan;
    public Transform dog;
    [Tooltip("Shutting: each jaw about this axis of its own (local) by this many degrees.")]
    public Vector3 jawAxisL = Vector3.right;
    public Vector3 jawAxisR = Vector3.right;
    public float jawAngleL = 90f;
    public float jawAngleR = 90f;
    [Tooltip("Sprung: each spring's top leaf rises about this axis of its own by this many degrees.")]
    public Vector3 springAxisL = Vector3.right;
    public Vector3 springAxisR = Vector3.right;
    public float springAngleL;
    public float springAngleR;
    [Tooltip("Sprung: how far its pan dips (m); how far its latch is thrown about its own axis, and where it comes to rest on the shut jaw (degrees).")]
    public float panDip = 0.01f;
    public Vector3 dogAxis = Vector3.forward;
    public float dogThrown = 62f;
    public float dogRests = 6f;
    [Header("What it does")]
    [Tooltip("A body with its eyes this near over its middle is in it (m, across, at a player's size).")]
    public float jawReach = 0.27f;
    [Tooltip("A hand, or a thing, this near its pan springs it (m, across).")]
    public float panReach = 0.11f;
    [Tooltip("How long a monster's held (s).")]
    public float holds = 6f;
    [Tooltip("How long it takes to set itself once it's lying still and flat (s).")]
    public float setAfter = 0.6f;
    [Tooltip("How long it lies shut after it's sprung (s), then it's gone.")]
    public float lasts = 25f;
    public AudioClip[] setSounds;
    public AudioClip[] snapSounds;
    public AudioClip[] biteSounds;
    public AudioClip[] rattleSounds;
    private const byte Lying = 0;
    private const byte IsSet = 1;
    private const byte IsSprung = 2;
    private const float ShutIn = 0.07f;
    private const float EyesUpTo = 1.75f;
    private byte stateHere;
    private byte shown;
    private bool sprung;
    private bool youAreOn;
    private float stillFor;
    private float shutT = -1f;
    private float sprungAtHere;
    private float heldTill;
    private float nextRattle;
    private Quaternion openL;
    private Quaternion openR;
    private Quaternion openSL;
    private Quaternion openSR;
    private Quaternion openDog;
    private Vector3 panAt;
    private Vector3 holdsAt;
    public static readonly List<GillaBearTrap> All = new List<GillaBearTrap>();

    [Networked]
    private byte State { get; set; }

    private bool Live
    {
        get
        {
            if (Object == null) return false;
            if (!Object.Runner) return false;
            return Object.Runner.Exists(Object);
        }
    }

    private byte Now
    {
        get
        {
            if (Live) return State;
            return stateHere;
        }
    }

    private void OnEnable()
    {

        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Awake()
    {

        if (item == null) item = GetComponent<GillaItem>();
        if (jawL != null) openL = jawL.localRotation;
        if (jawR != null) openR = jawR.localRotation;
        if (springL != null) openSL = springL.localRotation;
        if (springR != null) openSR = springR.localRotation;
        if (dog != null) openDog = dog.localRotation;
        if (pan != null) panAt = pan.localPosition;
    }

    public override void Spawned()
    {
        shown = State;
        if (State == IsSprung)
        {
            sprung = true;
            sprungAtHere = Time.time;
            Pose(1f);
            PoseDog(1f);
        }
    }

    private void Update()
    {
        Sets();
        Shows();
        if (Now == IsSet && !sprung)
        {
            Looks();
        }
        Shuts();
        Rattles();
        Goes();
    }

    private void Sets()
    {
        if (item == null || sprung) return;
        if (!item.Mine) return;
        byte value = Now;
        if (item.Hand < 0 && !item.Swallowed && !item.Gone)
        {
            if (Now == Lying)
            {
                Vector3 up = transform.up;
                float t = 0f;
                if (Vector3.Dot(up, Vector3.up) > 0.85f)
                {
                    if (item.Still)
                    {
                        t = stillFor + Time.deltaTime;
                    }
                }
                stillFor = t;
                if (setAfter <= t)
                {
                    value = IsSet;
                }
            }
        }
        else
        {
            value = Lying;
            stillFor = 0f;
        }
        if (value != Now)
        {
            if (Live)
            {
                State = value;
                return;
            }
            stateHere = value;
        }
    }

    private void Shows()
    {
        byte now = Now;
        if (shown != now)
        {
            if (now == IsSet)
            {
                Sfx.Play(setSounds, transform.position, 0.8f, 0.97f, 1.03f);
                Transform t = pan != null ? pan : transform;
                youAreOn = YouAreOn(t.position);
            }
            shown = now;
        }
    }

    private bool YouAreOn(Vector3 mouth)
    {
        KillCam cam = KillCam.Local;
        if (cam == null) return false;
        if (cam.head == null) return false;
        float size = MonsterPlayer.IsMonster ? 1f : MonsterPlayer.ApeSize;
        float reach = jawReach;
        float across = MonsterPlayer.IsMonster ? 1.3f : size;
        Vector3 eyes = cam.head.position;
        float tall = MonsterPlayer.IsMonster ? 2f : 1f;
        float dy = eyes.y - mouth.y;
        if (-0.15f <= dy)
        {
            if (dy <= size * EyesUpTo * tall)
            {
                float dx = eyes.x - mouth.x;
                float dz = eyes.z - mouth.z;
                float r = reach * across;
                return dz * dz + dx * dx < r * r;
            }
            return false;
        }
        return false;
    }

    private void Looks()
    {
        Transform t = pan != null ? pan : transform;
        Vector3 mouth = t.position;
        KillCam cam = KillCam.Local;
        if (cam != null && cam.head != null && !cam.Busy)
        {
            bool on = YouAreOn(mouth);
            bool was = youAreOn;
            youAreOn = on;
            bool caught = (on && !was) || HandOn(mouth);
            if (caught)
            {
                Vector3 at = mouth + Vector3.up * 0.15f;
                if (Live) RPC_Sprung(at, true);
                else Sprung(at, true);
                if (!MonsterPlayer.IsMonster)
                {
                    GillaBlast.TrapYou(mouth);
                    return;
                }
                if (MonsterPlayer.Local != null)
                {
                    MonsterPlayer.Local.Snared(holds);
                }
                return;
            }
        }

        GillaDummy[] dummies = GillaDummy.All.ToArray();
        for (int i = 0; i < dummies.Length; i++)
        {
            GillaDummy dummy = dummies[i];
            if (dummy == null) continue;
            if (!dummy.Alive || !dummy.Standing || !dummy.Mine) continue;
            if (dummy.head == null || dummy.tearing == null) continue;
            if (Over(dummy.head.position, mouth, jawReach, EyesUpTo))
            {
                Vector3 at = mouth + Vector3.up * 0.15f;
                if (Live) RPC_Sprung(at, true);
                else Sprung(at, true);
                GillaBlast.TrapDummy(dummy, mouth);
                return;
            }
        }

        foreach (MonsterBrain brain in MonsterBrain.All)
        {
            if (brain == null) continue;
            if (brain.Dead /* ? */ /* ? || brain.Snaring (no such member) */) continue;
            if (!Over(brain.transform.position, mouth, jawReach * 1.3f, 1f)) continue;
            Vector3 at = mouth + Vector3.up * 0.15f;
            if (Live) RPC_Sprung(at, true);
            else Sprung(at, true);
            brain.Snared(holds, mouth);
            return;
        }

        foreach (GillaItem other in GillaItem.All)
        {
            if (other == null) continue;
            if (other == item) continue;
            if (!other.Mine || other.Swallowed || other.Gone) continue;
            if (!Over(other.transform.position, mouth, panReach, 0.14f)) continue;
            if (Live) RPC_Sprung(mouth, false);
            else Sprung(mouth, false);
            if (other.TryGetComponent<GillaEgg>(out GillaEgg egg))
            {
                egg.Smash();
            }
            break;
        }
    }

    private static bool Over(Vector3 p, Vector3 mouth, float reach, float upTo)
    {
        float dy = p.y - mouth.y;
        if (!(-0.15f <= dy && dy <= upTo)) return false;
        float dx = p.x - mouth.x;
        float dz = p.z - mouth.z;
        return dz * dz + dx * dx < reach * reach;
    }

    private bool HandOn(Vector3 mouth)
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot == null) return false;
        if (boot.leftHand != null)
        {
            if (Over(boot.leftHand.position, mouth, panReach, 0.13f)) return true;
        }
        if (boot.rightHand == null) return false;
        return Over(boot.rightHand.position, mouth, panReach, 0.13f);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Sprung(Vector3 at, NetworkBool flesh)
    {
        Sprung(at, flesh);
    }

    private void Sprung(Vector3 at, bool flesh)
    {
        Vector3 mouth;
        if (sprung) return;
        shutT = 0f;
        sprung = true;
        sprungAtHere = Time.time;
        if (Live)
        {
            if (Object != null && Object.HasStateAuthority)
            {
                State = IsSprung;
            }
        }
        else
        {
            stateHere = IsSprung;
        }
        shown = IsSprung;
        Transform t = pan != null ? pan : transform;
        mouth = t.position;
        Sfx.Play(snapSounds, mouth, 1f, 0.95f, 1.05f);
        if (flesh)
        {
            Sfx.Play(biteSounds, mouth, 1f, 0.92f, 1.05f);
            Blood.Burst(at, Vector3.up, null, 1f);
            Action later = () => GillaSplatter.Around(mouth, 1.2f, null) /* ? original passed (null, GillaSplatter.Stuff.Blood) */;
            later(); /* ? GillaWounds.Later(later) is inaccessible */
            heldTill = Time.time + holds;
            holdsAt = mouth;
            nextRattle = Time.time + 0.35f;
        }
        KillCam cam = KillCam.Local;
        if (cam != null && cam.head != null)
        {
            if ((cam.head.position - mouth).sqrMagnitude < 36f)
            {
                XRNode[] nodes = new XRNode[] { XRNode.LeftHand, XRNode.RightHand };
                for (int i = 0; i < nodes.Length; i++)
                {
                    InputDevice device = InputDevices.GetDeviceAtXRNode(nodes[i]);
                    device.SendHapticImpulse(0u, 0.5f, 0.1f);
                }
            }
        }
    }

    private void Rattles()
    {
        if (Time.time < heldTill && nextRattle <= Time.time)
        {
            nextRattle = Time.time + UnityEngine.Random.Range(0.9f, 1.5f);
            Sfx.Play(rattleSounds, holdsAt, 0.7f, 0.92f, 1.1f);
        }
    }

    private void Shuts()
    {
        if (sprung && 0f <= shutT)
        {
            shutT += Time.deltaTime;
            float u = shutT / ShutIn;
            float k;
            if (1f <= u)
            {
                k = Mathf.Exp((u - 1f) * -0.9f) * -0.08f * Mathf.Abs(Mathf.Sin((u - 1f) * 2.2f)) + 1f;
            }
            else
            {
                k = u * u;
            }
            Pose(k);
            PoseDog(shutT);
            if (8f < u)
            {
                Pose(1f);
                PoseDog(1f);
                shutT = -1f;
            }
        }
    }

    private void Pose(float k)
    {
        if (jawL != null)
        {
            jawL.localRotation = openL * Quaternion.AngleAxis(jawAngleL * k, jawAxisL);
        }
        if (jawR != null)
        {
            jawR.localRotation = openR * Quaternion.AngleAxis(jawAngleR * k, jawAxisR);
        }
        float c = Mathf.Clamp01(k);
        float c3 = c * c * c;
        if (springL != null)
        {
            springL.localRotation = openSL * Quaternion.AngleAxis(c3 * springAngleL, springAxisL);
        }
        if (springR != null)
        {
            springR.localRotation = openSR * Quaternion.AngleAxis(c3 * springAngleR, springAxisR);
        }
        if (pan != null)
        {
            pan.localPosition = panAt + Vector3.down * (c * panDip);
        }
    }

    private void PoseDog(float t)
    {
        if (dog == null) return;
        float angle;
        if (t <= ShutIn)
        {
            float a = 1f - Mathf.Clamp01(t / 0.0196f);
            angle = (1f - a * a * a) * dogThrown;
        }
        else
        {
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - ShutIn) / 0.4f));
            angle = Mathf.Lerp(dogThrown, dogRests, s);
        }
        dog.localRotation = openDog * Quaternion.AngleAxis(angle, dogAxis);
    }

    private void Goes()
    {
        if (!sprung) return;
        if (item == null) return;
        if (item.Gone) return;
        if (!item.Mine) return;
        if (item.Hand > -1) return;
        if (Time.time - sprungAtHere < lasts) return;
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
}
