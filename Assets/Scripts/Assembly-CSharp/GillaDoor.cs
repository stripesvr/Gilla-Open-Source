// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class GillaDoor : MonoBehaviour
{
    public const int PunchesToBreak = 3;
    private const int RoundStep = 65536;
    private static int modeRound = -1;
    private static int roundSeenAt = -1;
    private static bool modeHeard;
    private static readonly float[] KnockIn = new float[] { 0f, 0.025f, 0.06f };
    private static readonly float[] KnockOver = new float[] { 0f, 1.2f, 3f };
    private const float ShakeFor = 0.35f;
    private Quaternion shutTurn = Quaternion.identity;
    private int punches;
    private Vector3 knockWay;
    private float shakeFrom = -1f;
    private Collider slabBox;
    private int heldBy = -1;
    private bool heldRight;
    private Vector3 heldPos;
    private Quaternion heldRot = Quaternion.identity;
    private bool toldAtRest = true;
    private float stillSince;
    private Vector3 stillAt;
    private Quaternion stillTurned;
    [Tooltip("This door's bit in the doors' states (NetworkApe's Doors): each door in the map its own, from 0 to 15 (the top half says which were pried).")]
    public int index;
    [Tooltip("What slides: the door itself, with its collider.")]
    public Transform panel;
    [Tooltip("The locked room's: it opens for the keycard (GillaKeycardReader). No monster forces it or tries.")]
    public bool keycardOnly;
    [Tooltip("How far it slides up to open, metres.")]
    public float rise = 2.25f;
    [Tooltip("How long it takes to open or shut, seconds.")]
    public float seconds = 1.3f;
    [Tooltip("Played from the door as it starts to move (one of them).")]
    public AudioClip[] moveSounds;
    [Tooltip("Played as it lands shut or comes to a stop open (one of them).")]
    public AudioClip[] stopSounds;
    [Tooltip("Played from its foot as a monster pries it open, timed to its pried way up, slam included (one of them).")]
    public AudioClip[] prySounds;
    [Tooltip("Played as a monster blows it off its frame (one of them).")]
    public AudioClip[] blowSounds;
    [Tooltip("Blown off its frame, it's back (a new one sliding down into the frame) after this long (s).")]
    public float blownResetSeconds = 15f;
    [Tooltip("A monster forcing it: yellow sparks a second while it strains, however slowly it's going.")]
    public float prySparksPerSecond = 45f;
    [Tooltip("A monster forcing it: more yellow sparks a second for each m/s it jerks up or slips back.")]
    public float prySparksPerSpeed = 160f;
    [Tooltip("BE THE MONSTER's map's (GillaSetup.ModeDoors): its own states, open as each hunt starts; a monster player's fists knock it off its frame (PunchesToBreak), and it stays off till the next hunt, for the monster to pick up. No monster of the map's own forces it.")]
    public bool modeMap;
    [Tooltip("BE THE MONSTER's map's: played as a monster player's fist lands on it (one of them).")]
    public AudioClip[] punchSounds;
    [Tooltip("Made standing open, its slab up by rise (BE THE MONSTER's map's, GillaSetup.ModeDoors: open's how they start). Where it comes down to is under where it's been put.")]
    public bool madeOpen;
    private static readonly List<GillaDoor> doors = new List<GillaDoor>();
    private const int PriedShift = 16;
    private const int BlownShift = 24;
    private static readonly float[] PryT;
    private static readonly float[] PryH;
    private Vector3 shut;
    private float open;
    private bool moving;
    private bool prying;
    private float tall = 2.7f;
    private float seam = 1.24f;
    private float top = 2.5f;
    private ParticleSystem prySparks;
    private bool blownShown;
    private float blownAt;
    private Vector3 blowAway;
    private GameObject flying;
    private float pryDue;
    private static Material sparkMaterial;
    private const float CrushReach = 0.08f;
    private const float CrushHead = 0.15f;
    private const float CrushThrow = 1f;
    private Renderer[] slabRenderers;
    private Collider[] slabColliders;

    public static int ModeShut { get; private set; }
    public static int ModeBroken { get; private set; }
    public static int ModeVersion { get; private set; }
    public static int ModeBy { get; private set; } = -1;

    public Collider SlabBox
    {
        get
        {
            if (slabBox != null)
                return slabBox;
            Collider c = null;
            if (panel != null)
                c = panel.GetComponentInChildren<Collider>();
            slabBox = c;
            return c;
        }
    }

    public Transform Loose
    {
        get
        {
            if (modeMap && Blown && flying != null)
                return flying.transform;
            return null;
        }
    }

    public Collider LooseBox
    {
        get
        {
            if (Loose != null)
                return flying.GetComponent<Collider>();
            return null;
        }
    }

    public bool HeldNow { get { return heldBy >= 0; } }

    public static int Mask { get; private set; }
    public static int Version { get; private set; }
    public static int By { get; private set; } = -1;

    public static IReadOnlyList<GillaDoor> All { get { return doors; } }

    public static float PrySeconds { get { return PryT[PryT.Length - 1]; } }

    public bool IsOpen
    {
        get
        {
            if (!modeMap)
                return ((Mask >> index) & 1) != 0;
            return ((ModeShut >> index) & 1) == 0;
        }
    }

    private bool Pried
    {
        get
        {
            if (!modeMap)
                return ((Mask >> (index + PriedShift)) & 1) != 0;
            return false;
        }
    }

    public bool Blown
    {
        get
        {
            if (!modeMap)
            {
                if (index > 7)
                    return false;
                return ((Mask >> (index + BlownShift)) & 1) != 0;
            }
            return ((ModeBroken >> index) & 1) != 0;
        }
    }

    public bool IsMoving { get { return open != (IsOpen ? 1f : 0f); } }

    public bool Busy
    {
        get
        {
            if (Blown)
                return true;
            if (open != (IsOpen ? 1f : 0f))
                return true;
            if (!IsOpen)
                return false;
            Vector3 foot = Foot;
            Vector3 side = Vector3.Cross(Vector3.up, Through);
            bool found = false;
            foreach (MonsterBrain m in MonsterBrain.All)
            {
                if (m == null)
                    continue;
                /* ? */ // MonsterBrain has no 'door' member; the "monster using this door" check is covered by the doorway proximity test below.
                Vector3 d = m.transform.position - foot;
                if (Mathf.Abs(Vector3.Dot(d, Through)) < Thickness * 0.5f + 0.9f
                    && Mathf.Abs(Vector3.Dot(d, side)) < Width * 0.5f + 0.3f
                    && Mathf.Abs(d.y) < 2f)
                {
                    found = true;
                    break;
                }
            }
            return found;
        }
    }

    public float Gap { get { return rise * Height(); } }

    public Vector3 Foot
    {
        get
        {
            if (panel.parent != null)
                return panel.parent.TransformPoint(shut);
            return shut;
        }
    }

    public Vector3 Through { get; private set; } = Vector3.forward;
    public float Width { get; private set; } = 2.8f;
    public float Thickness { get; private set; } = 0.3f;

    private static bool ShowSlab_b__168_0(Renderer r)
    {
        return r.enabled;
    }

    private static bool ShowSlab_b__168_1(Collider c)
    {
        return c.enabled;
    }

    public static GillaDoor ModeDoor(int index)
    {
        foreach (GillaDoor d in doors)
        {
            if (d != null && d.modeMap && d.index == index)
                return d;
        }
        return null;
    }

    private static void ModeChanged()
    {
        NetworkApe local = NetworkApe.Local;
        ModeVersion = ModeVersion + 1;
        ModeBy = (local != null && local.Object != null) ? local.Object.InputAuthority.PlayerId /* ? */ : 0;
    }

    public static void OfferMode(int shut, int broken, int version, int by)
    {
        if (ModeVersion > version)
            return;
        if (ModeVersion == version && by <= ModeBy)
            return;
        ModeShut = shut;
        ModeBroken = broken;
        ModeVersion = version;
        ModeBy = by;
        if (!modeHeard)
        {
            modeHeard = true;
            foreach (GillaDoor d in doors)
            {
                if (d.modeMap)
                    d.Snap();
            }
        }
    }

    private static void NewModeRoom()
    {
        ModeShut = 0;
        ModeBroken = 0;
        ModeVersion = 0;
        modeHeard = false;
        ModeBy = -1;
        modeRound = -1;
    }

    private static void FollowRound()
    {
        if (roundSeenAt == Time.frameCount)
            return;
        roundSeenAt = Time.frameCount;
        GillaModeRound round = GillaModeRound.Current;
        if (round == null)
            return;
        if (!round.Live)
            return;
        if (round.Round == modeRound)
            return;
        int r = round.Round;
        modeRound = r;
        if (ModeVersion < r * RoundStep)
        {
            ModeShut = 0;
            ModeBroken = 0;
            ModeVersion = r * RoundStep;
            ModeBy = -1;
        }
        foreach (GillaDoor d in doors)
        {
            if (d.modeMap)
                d.NewRound();
        }
    }

    private void NewRound()
    {
        punches = 0;
        shakeFrom = -1f;
        knockWay = Vector3.zero;
        heldBy = -1;
        Snap();
    }

    public void Punch(Vector3 at, Vector3 way)
    {
        if (!modeMap)
            return;
        if (Blown)
            return;
        int hit = punches + 1;
        NetworkApe local = NetworkApe.Local;
        bool sent = false;
        if (local != null && local.Object != null)
        {
            NetworkObject obj = local.Object;
            if (obj.Runner && obj.Runner.IsRunning /* ? */)
            {
                local.RPC_DoorPunch((byte)index, (byte)hit, at, way);
                sent = true;
            }
        }
        if (!sent)
            Punched(hit, at, way);
        if (hit < PunchesToBreak)
            return;
        ModeBroken = ModeBroken | (1 << index);
        ModeShut = ModeShut & ~(1 << index);
        ModeChanged();
    }

    public void Punched(int hit, Vector3 at, Vector3 way)
    {
        if (!modeMap || Blown)
            return;
        Vector3 flat = new Vector3(way.x, 0f, way.z);
        Vector3 dir;
        if (flat.sqrMagnitude <= 0.0001f)
            dir = Through;
        else
            dir = flat.normalized;
        punches = Mathf.Max(punches, hit);
        Sfx.Play(punchSounds, at, 1f, 0.92f, 1.08f);
        HitSparks(at, dir);
        if (hit < PunchesToBreak)
        {
            knockWay = dir;
            shakeFrom = Time.time;
            Place();
            return;
        }
        blowAway = dir;
    }

    private Vector3 Knock()
    {
        if (modeMap && knockWay != Vector3.zero)
        {
            float shake = 0f;
            if (shakeFrom >= 0f)
            {
                float t = (Time.time - shakeFrom) / ShakeFor;
                if (t < 1f)
                    shake = (1f - t) * Mathf.Sin(t * 30f) * 0.03f;
            }
            int i = Mathf.Clamp(punches, 0, KnockIn.Length - 1);
            Vector3 v = knockWay * (shake + KnockIn[i]);
            if (panel.parent != null)
                v = panel.parent.InverseTransformVector(v);
            return v;
        }
        return Vector3.zero;
    }

    private Quaternion KnockTurn()
    {
        int i = Mathf.Clamp(punches, 0, KnockOver.Length - 1);
        if (punches >= 0 && i != 0 && knockWay != Vector3.zero)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, knockWay);
            if (panel.parent != null)
                axis = panel.parent.InverseTransformDirection(axis);
            return Quaternion.AngleAxis(KnockOver[i], axis);
        }
        return Quaternion.identity;
    }

    private void Shudder()
    {
        if (Time.time - shakeFrom > ShakeFor)
            shakeFrom = -1f;
        Place();
    }

    private void HitSparks(Vector3 at, Vector3 way)
    {
        if (prySparks == null)
        {
            prySparks = MakeSparks("PrySparks", 450,
                new Color(1f, 0.85f, 0.12f, 1f),
                new Color(1f, 0.68f, 0.03f, 1f),
                new Color(0.6f, 0.26f, 0.01f, 1f));
        }
        if (prySparks == null)
            return;
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < 24; i++)
        {
            ep.position = at - way * 0.04f;
            float push = UnityEngine.Random.Range(0.6f, 2.2f);
            Vector3 jitter = UnityEngine.Random.insideUnitSphere;
            float up = UnityEngine.Random.Range(0f, 1.2f);
            ep.velocity = (jitter * 1.4f - way * push) + Vector3.up * up;
            ep.startLifetime = UnityEngine.Random.Range(0.3f, 0.8f);
            ep.startSize = UnityEngine.Random.Range(0.015f, 0.03f);
            prySparks.Emit(ep, 1);
        }
    }

    private void LooseAtRest(bool told)
    {
        toldAtRest = told;
        stillSince = 0f;
    }

    public void Hold(int by, bool right, Vector3 pos, Quaternion rot)
    {
        if (!modeMap)
            return;
        if (!Blown)
            return;
        if (flying == null)
            MakeLoose();
        foreach (GillaDoor d in doors)
        {
            if (d != this && d.heldBy == by && d.heldRight == right)
                d.heldBy = -1;
        }
        GameObject go = flying;
        heldBy = by;
        heldRight = right;
        heldPos = pos;
        heldRot = rot;
        toldAtRest = true;
        stillSince = 0f;
        Rigidbody rb;
        if (go.TryGetComponent<Rigidbody>(out rb))
            rb.isKinematic = true;
    }

    public void LetGo(Vector3 at, Quaternion rot, Vector3 velocity, Vector3 spin)
    {
        heldBy = -1;
        if (flying == null)
            return;
        flying.transform.SetPositionAndRotation(at, rot);
        toldAtRest = false;
        stillSince = 0f;
        Rigidbody rb;
        if (!flying.TryGetComponent<Rigidbody>(out rb))
            return;
        rb.position = at;
        rb.rotation = rot;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        rb.angularVelocity = spin;
        rb.WakeUp();
    }

    public void Rest(Vector3 at, Quaternion rot)
    {
        if (heldBy >= 0)
            return;
        if (flying == null)
            return;
        toldAtRest = true;
        flying.transform.SetPositionAndRotation(at, rot);
        Rigidbody rb;
        if (!flying.TryGetComponent<Rigidbody>(out rb))
            return;
        rb.position = at;
        rb.rotation = rot;
        if (rb.isKinematic)
            return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Sleep();
    }

    public bool NewlyAtRest(out Vector3 at, out Quaternion rot)
    {
        at = default(Vector3);
        rot = default(Quaternion);
        if (Loose == null)
            return false;
        if (heldBy >= 0)
            return false;
        if (toldAtRest)
            return false;
        Transform t = flying.transform;
        bool still = false;
        if (stillSince <= 0f)
        {
            stillSince = Time.time;
        }
        else
        {
            if ((t.position - stillAt).sqrMagnitude <= 0.0004f && Quaternion.Angle(t.rotation, stillTurned) <= 2f)
                still = true;
            else
                stillSince = Time.time;
        }
        if (!still)
        {
            stillAt = t.position;
            stillTurned = t.rotation;
        }
        if (Time.time - stillSince < 0.6f)
            return false;
        toldAtRest = true;
        at = t.position;
        rot = t.rotation;
        return true;
    }

    private void LateUpdate()
    {
        if (heldBy < 0)
            return;
        if (flying == null)
            return;
        Transform hand = Hand(heldBy, heldRight);
        if (hand == null)
        {
            Vector3 p = flying.transform.position;
            Quaternion q = flying.transform.rotation;
            LetGo(p, q, Vector3.zero, Vector3.zero);
            return;
        }
        Vector3 pos = hand.position + hand.rotation * heldPos;
        Quaternion rot = hand.rotation * heldRot;
        flying.transform.SetPositionAndRotation(pos, rot);
        Rigidbody rb;
        if (!flying.TryGetComponent<Rigidbody>(out rb))
            return;
        rb.position = pos;
        rb.rotation = rot;
    }

    private static Transform Hand(int by, bool right)
    {
        NetworkApe local = NetworkApe.Local;
        int me;
        if (local == null || local.Object == null)
            me = PlayerRef.None.PlayerId;
        else
            me = local.Object.InputAuthority.PlayerId /* ? */;
        if (me == by && MonsterPlayer.Local != null)
        {
            if (right)
                return MonsterPlayer.Local.rightFollower;
            return MonsterPlayer.Local.leftFollower;
        }
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape != null && ape.Object != null && ape.Object.InputAuthority.PlayerId /* ? */ == by)
                return right ? ape.rightHand /* ? */ : ape.leftHand /* ? */;
        }
        return null;
    }

    private IEnumerator Unignore(Collider box, Collider[] frame, float after)
    {
        yield return new WaitForSeconds(after);
        if (box == null)
            yield break;
        for (int i = 0; i < frame.Length; i++)
        {
            Collider c = frame[i];
            if (c != null && c != box)
                Physics.IgnoreCollision(box, c, false);
        }
    }

    private void Awake()
    {

        Vector3 local = panel.localPosition;
        Vector3 up = madeOpen ? Vector3.up * rise : Vector3.zero;
        shut = local - up;
        shutTurn = panel.localRotation;
        Vector3 fwd = panel.forward;
        Vector3 flat = new Vector3(fwd.x, 0f, fwd.z);
        if (flat.sqrMagnitude > 0.0001f)
            Through = flat.normalized;
        Collider[] cols = panel.GetComponentsInChildren<Collider>();
        if (cols.Length == 0)
            return;
        Bounds b = cols[0].bounds;
        for (int i = 0; i < cols.Length; i++)
            b.Encapsulate(cols[i].bounds);
        Vector3 size = b.size;
        Vector3 through = Through;
        Vector3 side = Vector3.Cross(Vector3.up, through);
        tall = size.y;
        Width = Mathf.Abs(size.x * side.x) + Mathf.Abs(size.z * side.z);
        Thickness = Mathf.Abs(size.x * through.x) + Mathf.Abs(size.z * through.z);
        top = size.y - 0.2f;
        Vector3 foot = Foot;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float best = float.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r.transform.IsChildOf(panel))
                continue;
            if (r.name.StartsWith("Guide"))
            {
                Bounds rb = r.bounds;
                float d = Mathf.Abs(Vector3.Dot(rb.center - foot, side))
                          - (Mathf.Abs(side.x * rb.extents.x) + Mathf.Abs(side.z * rb.extents.z));
                if (d > 0.2f && d <= best)
                    best = d;
            }
            else if (r.name == "Track")
            {
                float t = top;
                Bounds rb = r.bounds;
                float m = rb.min.y - foot.y;
                if (m <= t)
                    t = m;
                top = t;
            }
        }
        if (best >= float.MaxValue)
            best = Width * 0.5f - 0.16f;
        seam = best;
    }

    private void OnEnable()
    {

        doors.Add(this);
        if (modeMap)
            Snap();
    }

    private void OnDisable()
    {
        doors.Remove(this);
    }

    public void Press()
    {
        if (Busy)
            return;
        if (modeMap)
        {
            ModeShut = (1 << index) ^ ModeShut;
            ModeChanged();
            return;
        }
        Mask = (1 << index) ^ Mask;
        Mask = Mask & ~(1 << (index + PriedShift));
        NetworkApe local = NetworkApe.Local;
        Version = Version + 1;
        By = (local != null && local.Object != null) ? local.Object.InputAuthority.PlayerId /* ? */ : 0;
    }

    public bool Pry()
    {
        if (!keycardOnly && !modeMap && !IsOpen)
        {
            if (open == (IsOpen ? 1f : 0f))
            {
                Mask = (1 << index) | (1 << (index + PriedShift)) | Mask;
                Changed();
                return true;
            }
        }
        return false;
    }
}
