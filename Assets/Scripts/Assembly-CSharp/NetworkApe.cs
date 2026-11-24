// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(200)]
public partial class NetworkApe : NetworkBehaviour
{
    [Header("Synced targets (NetworkTransform)")]
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    [Header("The ape the others see")]
    public GameObject ape;
    public ApeColor color;
    public ApeFace face;
    [Tooltip("Its finger posers (remote: posed from the network), left then right.")]
    public HandPoser fingersL;
    [Tooltip("Its finger posers (remote: posed from the network), left then right.")]
    public HandPoser fingersR;
    public Dismemberment tearing;
    [Tooltip("What the name over their head is drawn with (NameTag).")]
    public Material tagMaterial;

    private const float ProofPoll = 1;
    private const float ProofDelay = 3;
    private const float ProofAgain = 30;
    private const int RefusalsToHide = 2;
    private float nextProofPoll;
    private float lastProofAt;
    private float askAgainAt;
    private int refusals;
    private bool checkingProof;
    private string lastProof;
    private string verifiedProof;
    private string sentAuthSig;
    private const float TornHereFor = 3;
    private int appliedRgb = -1;
    private int appliedTorn = -1;
    private byte tornHere;
    private float tornHereAt;
    internal GillaHeldApe drawn;
    private GillaLimp limp;
    private int appliedMonster;
    private long appliedCos = -1;
    private CosmeticWearer wearer;
    private readonly byte[] cos = new byte[8];
    private readonly byte[] said = new byte[8];
    private long weighedCos = -1;
    private int weighedOwned = -1;
    private bool weighedTrusted;
    private bool someHidden;
    private float recheckAt;
    private const float OwnedRecheck = 2;
    private string provenProof;
    private bool proofProven;
    private bool started;
    internal int owner;
    internal PlayerRef ownerRef;
    private MonsterAvatar monsterBody;
    private ApeAvatarDriver driver;
    private int punches;
    private int seenHits;
    private float hiddenTill;
    private float bodyLeftAt = -10f;
    private float rippedAt = -10f;
    private float bodyHereAt = -10f;
    private Dismemberment tornFrom;
    private VoicePitch pitch;
    private NameTag nameTag;
    private string sentName;
    private string sentId;
    private NetworkString<_32> shownNick;
    private NetworkString<_32> knownId;
    private string accountId = "";
    private AudioSource voiceOut;
    private bool mutedHere;
    public static readonly List<NetworkApe> All = new List<NetworkApe>();
    private static GillaGoreAssets gore;
    private float squashedAt = -10f;
    private static byte tearsComing;
    private static readonly HashSet<string> proofsYes = new HashSet<string>();
    private static readonly HashSet<string> proofsNo = new HashSet<string>();

    [Networked] private NetworkString<_32> Nick { get; set; }
    [Networked] private int FurRgb { get; set; }
    [Networked] private float Voice { get; set; }
    [Networked] private int FingersL { get; set; }
    [Networked] private int FingersR { get; set; }
    [Networked] private byte Torn { get; set; }
    [Networked] private int Doors { get; set; }
    [Networked] private int DoorsVersion { get; set; }
    [Networked] private int DoorsBy { get; set; }
    [Networked] private int ModeShut { get; set; }
    [Networked] private int ModeBroken { get; set; }
    [Networked] private int ModeDoorsVersion { get; set; }
    [Networked] private int ModeDoorsBy { get; set; }
    [Networked] private byte Monster { get; set; }
    [Networked] private NetworkBool Caught { get; set; }
    [Networked] private PlayerRef Holder { get; set; }
    [Networked] private NetworkBool HolderRight { get; set; }
    [Networked] private Vector3 HolderEyes { get; set; }
    [Networked] private byte Hits { get; set; }
    [Networked] private NetworkBool Down { get; set; }
    [Networked] private byte CosHead { get; set; }
    [Networked] private byte CosFace { get; set; }
    [Networked] private byte CosEyes { get; set; }
    [Networked] private byte CosBody { get; set; }
    [Networked] private byte CosHands { get; set; }
    [Networked] private byte CosBack { get; set; }
    [Networked] private NetworkString<_32> CloudId { get; set; }
    [Networked] private ushort LevelNow { get; set; }
    [Networked] private byte CosSkin { get; set; }
    [Networked] private byte CosToy { get; set; }
    [Networked] private byte Title { get; set; }
    [Networked] private float Size { get; set; }
    [Networked] private byte ToyHeld { get; set; }
    [Networked] private NetworkString<_32> AuthPlayer { get; set; }
    [Networked] private NetworkString<_64> AuthSession { get; set; }
    [Networked] private NetworkString<_128> AuthSig { get; set; }
    [Networked] private NetworkBool NoPunches { get; set; }

    private void RPC_Tear_b__272_0(byte part)
    {
        if (this != null)
        {
            TearHere((ApePart)part);
        }
    }

    private static void RPC_Tear_b__272_1(byte part, NetworkBootstrap rig)
    {
        tearsComing = (byte)(tearsComing & ~TornBit((ApePart)part));
        rig.tearing.Tear((ApePart)part);
    }

    private static void CallChunks(Vector3 at, Vector3 dir, Mesh mesh, Material mat, int count, float min, float max, float speed, float spread)
    {
        var m = typeof(GillaWounds).GetMethod("Chunks", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new Type[] { typeof(Vector3), typeof(Vector3), typeof(Mesh), typeof(Material), typeof(int), typeof(float), typeof(float), typeof(float), typeof(float) }, null);
        if (m != null) m.Invoke(null, new object[] { at, dir, mesh, mat, count, min, max, speed, spread }); /* ? */
    }

    private static void CrushGore_b__293_0(Vector3 at, Vector3 way, Dismemberment body)
    {
        Vector3 dir = Vector3.up * 0.35f - way;
        /* ? */ // was: using (Blood.BloodMark.Auto())
        {
            Material mat = body.bloodMaterial; /* ? */ // was Blood.RoomLit(body.bloodMaterial)
            if (mat == null) return;
            if (dir.sqrMagnitude < 1e-06f) dir = Vector3.up;
            float share = Blood.Share(469f);
            GameObject go = new GameObject("BloodBurst");
            Transform t = go.transform;
            t.position = at;
            UnityEngine.Object.Destroy(t.gameObject, 5f);
            float amount = share * 1.4f;

            ParticleSystem ps = Blood.Particles("Blood", t, dir, mat, 0.4f, Mathf.RoundToInt(amount * 240f));
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 7f);
            ParticleSystem.MinMaxCurve sizeXY = new ParticleSystem.MinMaxCurve(0.0035f, 0.011f);
            main.startSizeY = sizeXY;
            main.startSizeX = sizeXY;
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.008f, 0.032f);
            main.gravityModifier = 1.2f;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[2];
            bursts[0] = new ParticleSystem.Burst(0f, (short)Mathf.Max(1, Mathf.RoundToInt(amount * 190f))); /* ? */
            bursts[1] = new ParticleSystem.Burst(0.05f, (short)Mathf.Max(1, Mathf.RoundToInt(amount * 45f))); /* ? */
            emission.SetBursts(bursts);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = (ParticleSystemShapeType)4;
            shape.angle = 62f;
            shape.radius = 0.06f;
            ParticleSystem.CollisionModule collision = ps.collision;
            collision.enabled = true;
            collision.type = (ParticleSystemCollisionType)1;
            collision.mode = (ParticleSystemCollisionMode)0;
            collision.quality = (ParticleSystemCollisionQuality)2;
            collision.collidesWith = -1797;
            collision.enableDynamicColliders = false;
            collision.dampen = 0.9f;
            collision.bounce = 0.05f;
            collision.lifetimeLoss = 0f;
            collision.radiusScale = 0.5f;
            ps.Play();

            if (share < 0.35f) return;

            int fine = Mathf.RoundToInt(amount * 100f);
            ParticleSystem ps2 = Blood.Particles("Blood", t, dir, mat, 0.1f, fine);
            main = ps2.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            sizeXY = new ParticleSystem.MinMaxCurve(0.0014f, 0.0034f);
            main.startSizeY = sizeXY;
            main.startSizeX = sizeXY;
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.002f, 0.007f);
            main.gravityModifier = 0.5f;
            emission = ps2.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.Burst[] bursts2 = new ParticleSystem.Burst[1];
            bursts2[0] = new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(amount * 100f));
            emission.SetBursts(bursts2);
            shape = ps2.shape;
            shape.shapeType = (ParticleSystemShapeType)4;
            shape.angle = 75f;
            shape.radius = 0.07f;
            ps2.Play();

            GillaGoreAssets g = Gore; /* ? */ // was Blood.Gore
            if (g == null) return;
            if (g.chunk == null) return;
            CallChunks(at, dir, g.chunk, g.chunkMaterial, Mathf.RoundToInt(amount * 26f), 0.009f, 0.034f, 1.35f, 60f); /* ? */
            if (g.chipMaterial == null) return;
            CallChunks(at, dir, g.chunk, g.chipMaterial, Mathf.RoundToInt(amount * 12f), 0.004f, 0.013f, 1.5f, 60f); /* ? */
        }
    }

    private static void CrushGore_b__293_1(Vector3 at)
    {
        GillaSplatter.Around(at, 2f, null);
    }

    private static void TearYou_b__294_0(ApePart part, NetworkBootstrap rig)
    {
        tearsComing = (byte)(tearsComing & ~TornBit(part));
        rig.tearing.Tear(part);
    }

    private void RPC_Corpse_b__301_0(byte torn, Vector3 velocity)
    {
        if (this != null)
        {
            BodyNow(torn, velocity);
        }
    }

    private void RPC_BlownApart_b__303_0(byte torn, Vector3 velocity)
    {
        if (this == null) return;
        if (Object == null) return;
        if (!Object.Runner) return;
        if (!Object.IsValid /* ? */) return;
        BodyHere(torn, velocity, false);
        if (Object.HasStateAuthority /* ? */)
        {
            if (KillCam.Local != null)
            {
                var hide = typeof(KillCam).GetMethod("HideBody", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (hide != null) hide.Invoke(KillCam.Local, null); /* ? */
            }
            return;
        }
        if (ape == null) return;
        hiddenTill = Time.time + 1.5f;
        ape.SetActive(false);
    }

    private void RPC_BlownApart_b__303_1(byte seed)
    {
        GillaCorpse.RipSoon(owner, seed);
    }

    private IEnumerator VerifyProof(string player, string session, string sig, string account, string nick, string room)
    {
        bool valid = true;
        yield return ShiftAuth.VerifyPeer(player, session, sig, ok => valid = ok);
        checkingProof = false;
        string proof = string.Concat(new string[] { player, "|", session, "|", sig, "|", account });
        if (proof != lastProof) yield break;
        if (valid)
        {
            provenProof = proof;
            proofProven = true;
            refusals = 0;
            proofsYes.Add(player);
            proofsNo.Remove(player);
            yield break;
        }
        refusals++;
        askAgainAt = Time.unscaledTime + 30f;
        if (!proofsYes.Contains(player))
        {
            proofsNo.Add(player);
        }
        Debug.LogWarning(string.Format(
            "[Network] ShiftAuth's server says {0}'s proof isn't theirs ({1} in a row; it's said no to {2} and yes to {3} here{4})",
            new object[] { nick, refusals, proofsNo.Count, proofsYes.Count, ProofsLookBroken ? ": its fault, so nothing's held back" : "" }));
        if (refusals != 2) yield break;
        if (ProofsLookBroken) yield break;
        yield return ShiftAuth.ReportKick(player, nick, "peer_proof_invalid", "session " + session, room);
    }
}
