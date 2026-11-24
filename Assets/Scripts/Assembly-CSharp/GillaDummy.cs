// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(86)]
public partial class GillaDummy : NetworkBehaviour
{
    private enum Mode
    {
        Standing = 0,
        Held = 1,
        Eaten = 2,
        Gone = 3
    }

    [Tooltip("Where its eyes are and its hands: what its ape is laid on (ApeAvatarDriver).")]
    public Transform head;
    [Tooltip("Where its eyes are and its hands: what its ape is laid on (ApeAvatarDriver).")]
    public Transform leftHand;
    [Tooltip("Where its eyes are and its hands: what its ape is laid on (ApeAvatarDriver).")]
    public Transform rightHand;
    public Dismemberment tearing;
    public ApeColor colour;
    [Tooltip("Its eyes over the floor, stood (m).")]
    public float eyeHeight = 1.2f;
    public static readonly List<GillaDummy> All = new List<GillaDummy>();
    private const int FirstBody = 50000;
    private const float EyesOverHand = 0.36f;
    private const float ThrownDies = 4f;
    private const float GoneAfter = 1.5f;
    private static int offline;
    private static readonly Color[] Furs = new[] { new Color(0.85f, 0.32f, 0.1f, 1f), new Color(0.2f, 0.45f, 0.95f, 1f), new Color(0.75f, 0.1f, 0.12f, 1f), new Color(0.25f, 0.7f, 0.2f, 1f), new Color(0.55f, 0.2f, 0.8f, 1f), new Color(0.95f, 0.75f, 0.1f, 1f), new Color(0.1f, 0.65f, 0.7f, 1f), new Color(0.9f, 0.9f, 0.9f, 1f) };
    private Mode mode;
    private PlayerRef holder;
    private Vector3 heldEyes;
    private bool holderRight;
    private MonsterBrain eater;
    private float eatenAt;
    private float biteAt = -1f;
    private float goneAt;
    internal int hits;
    private int offlineBody;
    private Vector3 standAt;
    private Quaternion standFacing = Quaternion.identity;
    private float phase;
    private bool paintPending;
    private bool blown;
    private const int Solid = -5893;

    [Networked]
    private int Rgb { get; set; }

    private bool Live
    {
        get
        {
            if (Object == null)
            {
                return false;
            }
            NetworkObject obj = Object;
            if (!obj.Runner)
            {
                return false;
            }
            return obj.Runner.Exists(obj) /* ? */;
        }
    }

    public bool Mine
    {
        get
        {
            if (!Live)
            {
                return true;
            }
            return Object.HasStateAuthority;
        }
    }

    public bool Standing
    {
        get { return mode == Mode.Standing; }
    }

    public bool Alive
    {
        get
        {
            if (mode == Mode.Gone)
            {
                return false;
            }
            return !blown;
        }
    }

    public int Hits
    {
        get { return hits; }
    }

    public int BodyId
    {
        get
        {
            if (!Live)
            {
                return offlineBody;
            }
            if (!Object.Id.IsValid)
            {
                return 50000;
            }
            return (int)(Object.Id.Raw % 40000u) + 50000;
        }
    }

    public Vector3 Middle
    {
        get
        {
            if (head != null)
            {
                return head.position + Vector3.down * 0.36f;
            }
            return transform.position + Vector3.up;
        }
    }

    private void Blown_b__0(byte torn, Vector3 velocity)
    {
        if (this == null)
        {
            return;
        }
        if (mode == Mode.Gone || tearing == null)
        {
            return;
        }
        ApePart[] parts = new ApePart[3];
        parts[0] = ApePart.ArmL;
        parts[1] = ApePart.ArmR;
        for (int i = 0; i < parts.Length; i++)
        {
            ApePart part = parts[i];
            if ((NetworkApe.TornBit(part) & torn) != 0 && !tearing.IsTorn(part))
            {
                tearing.Tear(part, velocity * 0.5f + Random.insideUnitSphere * 2f);
            }
        }
    }

    private void Blown_b__1(Vector3 velocity)
    {
        if (this != null)
        {
            Die(velocity, false);
        }
    }

    private static void Blown_b__2(int id, byte seed)
    {
        GillaCorpse.RipSoon(id, seed);
    }

    public bool IsTorn(ApePart part)
    {
        if (tearing != null)
        {
            return tearing.IsTorn(part);
        }
        return false;
    }

    public bool HeldBy(PlayerRef who, bool right)
    {
        if (mode == Mode.Held && holder.RawEncoded == who.RawEncoded)
        {
            return holderRight == right;
        }
        return false;
    }

    private void Awake()
    {

        int n = offline;
        offline = n + 1;
        offlineBody = n % 9000 + 90000;
        standAt = transform.position;
        standFacing = transform.rotation;
        phase = Random.Range(0f, 10f);
        if (tearing != null)
        {
            GillaRip.Prepare(tearing);
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

    private void Start()
    {
        if (Live)
        {
            return;
        }
        Color fur = Furs[Random.Range(0, Furs.Length)];
        if (colour != null)
        {
            colour.furColor = fur;
            paintPending = true;
        }
    }

    public override void Spawned()
    {
        standAt = transform.position;
        standFacing = transform.rotation;
        if (Object != null && Object.HasStateAuthority && Rgb == 0)
        {
            Color fur = Furs[Random.Range(0, Furs.Length)];
            int r = Mathf.RoundToInt(fur.r * 255f);
            int g = Mathf.RoundToInt(fur.g * 255f);
            int b = Mathf.RoundToInt(fur.b * 255f);
            Rgb = r << 16 | g << 8 | b | 0x1000000;
        }
        if (Rgb == 0)
        {
            return;
        }
        int rv = Rgb;
        int gv = Rgb;
        int bv = Rgb;
        if (colour != null)
        {
            colour.furColor = new Color((float)(rv >> 16 & 0xff) / 255f, (float)(gv >> 8 & 0xff) / 255f, (float)(bv & 0xff) / 255f, 1f);
            paintPending = true;
        }
    }

    private void Paint(Color c)
    {
        if (colour != null)
        {
            colour.furColor = c;
            paintPending = true;
        }
    }

    public void Grab(PlayerRef by, bool right, Vector3 eyesFromHand)
    {
        if (!Live)
        {
            Grabbed(by, right, eyesFromHand);
            return;
        }
        RPC_Grab(by, right, eyesFromHand);
    }

    public void Release(Vector3 at, Vector3 velocity)
    {
        if (!Live)
        {
            Released(at, velocity);
            return;
        }
        RPC_Release(at, velocity);
    }

    public void Punch(GoreSection section, Vector3 point)
    {
        if (!Live)
        {
            Punched(section, point, true);
            return;
        }
        RPC_Punch((byte)section, point);
    }

    public void Wound(GoreSection section, Vector3 point)
    {
        if (!Live)
        {
            Punched(section, point, false);
            return;
        }
        RPC_Wound((byte)section, point);
    }

    public void Stab(GoreSection section, Vector3 point, float radius, byte does)
    {
        if (!Live)
        {
            Stabbed(section, point, radius, does);
            return;
        }
        RPC_Stab((byte)section, point, radius, does);
    }

    public void Tear(ApePart part)
    {
        if (!Live)
        {
            Torn(part);
            return;
        }
        RPC_Tear((byte)part);
    }

    public void Rip(PlayerRef by, byte seed, bool plusInRight, Quaternion rotRight, Quaternion rotLeft)
    {
        if (!Live)
        {
            Ripped(by, seed, plusInRight, rotRight, rotLeft);
            return;
        }
        RPC_Rip(by, seed, plusInRight, rotRight, rotLeft);
    }

    public void Blasted(Vector3 velocity, byte torn, bool apart = false)
    {
        byte seed = (byte)GillaRip.PickSeed();
        if (!Live)
        {
            Blown(velocity, torn, apart, seed);
            return;
        }
        RPC_Blasted(velocity, torn, apart, seed);
    }

    public void Eaten(MonsterBrain brain)
    {
        if (Live)
        {
            if (brain.net != null && brain.net.Live)
            {
                RPC_Eaten(brain.net.Object.Id);
                return;
            }
        }
        if (mode == Mode.Standing && brain != null)
        {
            eater = brain;
            mode = Mode.Eaten;
            eatenAt = Time.time;
            biteAt = -1f;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Grab(PlayerRef by, NetworkBool right, Vector3 eyesFromHand)
    {
        Grabbed(by, right, eyesFromHand);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Release(Vector3 at, Vector3 velocity)
    {
        Released(at, velocity);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Punch(byte section, Vector3 point)
    {
        Punched((GoreSection)section, point, true);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Wound(byte section, Vector3 point)
    {
        Punched((GoreSection)section, point, false);
    }
}
