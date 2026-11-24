// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(200)]
public partial class GillaPickaxe : NetworkBehaviour
{
    private enum Into : byte
    {
        Nothing = 0,
        Player = 1,
        Dummy = 2,
        Body = 3,
        Monster = 4
    }

    private struct Landed
    {
        public Into into;
        public int id;
        public NetworkId thing;
        public GoreSection section;
        public Vector3 point;
    }

    private struct Skin
    {
        public Into into;
        public int id;
        public NetworkId thing;
        public GoreSection section;
        public Vector3 world;
        public Vector3 body;
        public Vector3 normal;
        public float inside;
        public GillaWounds wounds;
        public GillaCorpse corpse;
    }

    private const int Spike = 0;
    private const int Chisel = 1;
    private const byte JustHurts = 0;
    private const byte Counts = 1;
    private const byte Kills = 2;
    private const byte Soft = 0;
    private const byte Stone = 1;
    private const byte Metal = 2;
    public GillaItem item;
    [Header("The model (GillaSetup: from its Tip, TipBase and Edge)")]
    public Vector3 spikeTip;
    public Vector3 spikeBase;
    public Vector3 edgeTip;
    public Vector3 edgeBase;
    [Tooltip("Where it balances, near the head (its own coordinates).")]
    public Vector3 balance = new Vector3(0f, 0.5f, 0f);
    [Tooltip("The same mesh in Gilla/PickWet, drawn over it once it's been in anything.")]
    public Renderer wet;
    [Header("Going in")]
    [Tooltip("How nearly an end has to be going the way it points to go in (the cosine: 1 dead straight).")]
    public float inLine = 0.6f;
    [Tooltip("Where how hard it lands starts from (m/s); going this fast or faster it goes as far in as it can in one go; any other way, this fast or faster and it's a knock on them.")]
    public float pushSpeed = 0.7f;
    [Tooltip("Where how hard it lands starts from (m/s); going this fast or faster it goes as far in as it can in one go; any other way, this fast or faster and it's a knock on them.")]
    public float hardSpeed = 6f;
    [Tooltip("Where how hard it lands starts from (m/s); going this fast or faster it goes as far in as it can in one go; any other way, this fast or faster and it's a knock on them.")]
    public float knockSpeed = 2f;
    [Tooltip("The slowest an end has to be going, point first, to go in at all (m/s): slower, it rests on them, and drawn along them scratches them.")]
    public float stabSpeed = 2.2f;
    [Tooltip("How hard it has to hit a pane of glass to break it (m/s).")]
    public float glassSpeed = 2.5f;
    [Tooltip("How close the tip has to be to them (m).")]
    public float reach = 0.025f;
    [Tooltip("How far it goes in as it lands (m): pushed, and at its hardest. The chisel's a wide edge: chiselIn of that.")]
    public Vector2 firstIn = new Vector2(0.012f, 0.13f);
    public float chiselIn = 0.45f;
    [Tooltip("How far in it has to be to do its harm (m). Short of this it's a scratch.")]
    public float countsAt = 0.03f;
    [Tooltip("How far in a skull lets it, at a player's size (m).")]
    public float skullStops = 0.09f;
    [Tooltip("The holes it leaves, in the body's coordinates: the spike's and the chisel's (a fist's is about 0.035).")]
    public float holeRadius = 0.016f;
    [Tooltip("The holes it leaves, in the body's coordinates: the spike's and the chisel's (a fist's is about 0.035).")]
    public float chiselRadius = 0.022f;
    [Header("In")]
    [Tooltip("How fast it slides in and out, however hard your hand's going (m/s).")]
    public float pushRate = 0.3f;
    [Header("In")]
    [Tooltip("How fast it slides in and out, however hard your hand's going (m/s).")]
    public float pullRate = 0.75f;
    [Tooltip("How far it can be levered off the way it went in (degrees): in someone alive, and in a body.")]
    public float leverMost = 28f;
    [Tooltip("How far it can be levered off the way it went in (degrees): in someone alive, and in a body.")]
    public float bodyLeverMost = 45f;
    [Tooltip("How far your hand can get from it before it tears out (m): out of someone alive, and out of a body.")]
    public float yanksAt = 0.24f;
    [Tooltip("How far your hand can get from it before it tears out (m): out of someone alive, and out of a body.")]
    public float bodyYanksAt = 0.55f;
    [Header("A monster")]
    [Tooltip("Its harm to a monster (their health's about 400; a stick of dynamite at its worst, 120): anywhere, and in the head.")]
    public float monsterHarm = 45f;
    [Header("A monster")]
    [Tooltip("Its harm to a monster (their health's about 400; a stick of dynamite at its worst, 120): anywhere, and in the head.")]
    public float monsterHeadHarm = 160f;
    [Header("Sounds")]
    public AudioClip[] swingSounds;
    public AudioClip[] stabSounds;
    public AudioClip[] skullSounds;
    public AudioClip[] slideSounds;
    public AudioClip[] pullSounds;
    public AudioClip[] stoneSounds;
    public AudioClip[] metalSounds;
    private const float Stick = 0.012f;
    private const float HandSlides = 0.26f;
    private const float FollowsThrough = 0.35f;
    private const float Gives = 4f;
    private const float SwingLasts = 0.09f;
    private const float MostSpeed = 40f;
    private const float ComesBackIn = 0.12f;
    private const float SlidesFrom = 0.05f;
    private const float Slips = 0.05f;
    private const float PullStiff = 260f;
    private const float PullDamp = 22f;
    private const float PullHardest = 70f;
    private const float ScratchReach = 0.012f;
    private const float ScratchStep = 0.025f;
    private const float ScratchJump = 0.25f;
    private const float ScratchWidth = 0.0055f;
    private static readonly int TipId = Shader.PropertyToID("_Tip");
    private static readonly int EdgeId = Shader.PropertyToID("_Edge");
    private static readonly int DryId = Shader.PropertyToID("_Dry");
    private Into into;
    private int end;
    private int targetId;
    private int bodyId;
    private GoreSection section;
    private Vector3 point;
    private Transform on;
    private Vector3 onLocal;
    private Vector3 axisOn;
    private Quaternion turnOn;
    private float depth;
    private float mostIn;
    private float stuckAt;
    private float slidWas;
    private float slideAt;
    private float hiddenSince = -1f;
    private bool harmed;
    private bool through;
    private NetworkApe ape;
    private Dismemberment copy;
    private GillaDummy dummy;
    private MonsterBrain monster;
    private Rigidbody pulls;
    private Vector3 pullTo;
    private readonly Vector3[] was = new Vector3[2];
    private readonly Vector3[] swung = new Vector3[2];
    private readonly float[] swungAt = new float[2];
    private readonly float[] wetTo = new float[2];
    private float againAt;
    private float clangAt;
    private float swishAt;
    private float comeFor;
    private Vector3 comeFrom;
    private Quaternion comeTurn;
    private int heldWas = -1;
    private float wetAt = -1f;
    private float wetSaid = -100f;
    private bool wetChanged;
    private Vector3 tipOnMesh;
    private Vector3 edgeOnMesh;
    private float perMesh = 1f;
    private MaterialPropertyBlock props;
    private static GillaGoreAssets gore;
    private static ParticleSystem sparks;
    private static ParticleSystem flash;
    private static readonly List<Collider> found = new List<Collider>();
    private readonly bool[] scraping = new bool[2];
    private readonly Skin[] scrapedFrom = new GillaPickaxe.Skin[2];

    [Networked]
    private float Depth { get; set; }

    [Networked]
    private Vector3 PullTo { get; set; }

    [Networked]
    private NetworkBool Pulling { get; set; }

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
            return Object.Runner.Exists(Object) /* ? */;
        }
    }

    private static Material Spray
    {
        get
        {
            GillaGoreAssets g;
            if (gore != null)
            {
                g = gore;
            }
            else
            {
                g = Resources.Load<GillaGoreAssets>("GillaGore");
                gore = g;
            }
            Material m = null;
            if (g != null)
            {
                m = gore.spray;
            }
            return m;
        }
    }

    private Vector3 HoleOn
    {
        get
        {
            return Tip(end) - Along(end) * depth;
        }
    }

    private static bool Scratch_b__155_0(NetworkApe x, int id)
    {
        return x != null && x.Object != null && x.Object.InputAuthority.PlayerId == id /* ? */;
    }

    private static bool Hold_b__160_0(NetworkApe a, int id)
    {
        return a != null && a.Object != null && a.Object.InputAuthority.PlayerId == id /* ? */;
    }

    private static void Bled_b__174_0(Vector3 hole, Dismemberment keepOff)
    {
        GillaSplatter.Around(hole, 0.3f, keepOff);
    }

    private Vector3 Tip(int e)
    {
        return e != 0 ? edgeTip : spikeTip;
    }

    private Vector3 Along(int e)
    {
        if (e == 0)
        {
            return (spikeTip - spikeBase).normalized;
        }
        return (edgeTip - edgeBase).normalized;
    }

    private float Blade(int e)
    {
        if (e == 0)
        {
            return (spikeTip - spikeBase).magnitude;
        }
        return (edgeTip - edgeBase).magnitude;
    }

    private void Awake()
    {

        if (item == null)
        {
            item = GetComponent<GillaItem>();
        }
        item.ForMonsters = true;
        item.Knocked = new Action<Vector3, Vector3, float, Collider>(HeldKnock);
        Rigidbody body = GetComponent<Rigidbody>();
        body.centerOfMass = balance;
        if (wet != null)
        {
            wet.enabled = false;
            tipOnMesh = wet.transform.InverseTransformPoint(transform.TransformPoint(spikeTip));
            edgeOnMesh = wet.transform.InverseTransformPoint(transform.TransformPoint(edgeTip));
            perMesh = wet.transform.InverseTransformVector(transform.up).magnitude;
        }
        Remember();
    }

    public override void Spawned()
    {
        Remember();
    }

    private void Remember()
    {
        for (int e = 0; e < 2; e++)
        {
            was[e] = transform.TransformPoint(Tip(e));
            swung[e] = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (!(0f < dt))
        {
            return;
        }
        if (item == null)
        {
            return;
        }
        int hand = item.Hand;
        if (hand != heldWas)
        {
            heldWas = hand;
            againAt = Mathf.Max(againAt, Time.time + 0.2f);
        }
        if (into == Into.Nothing)
        {
            if (0f < comeFor && item.Hand > -1)
            {
                CameFrom(dt);
            }
            Swings(dt);
        }
        else
        {
            InIt(dt);
        }
        if (wet == null || wetAt < 0f || (!wetChanged && Time.time - wetSaid < 0.5f))
        {
            return;
        }
        wetChanged = false;
        wetSaid = Time.time;
        if (props == null)
        {
            props = new MaterialPropertyBlock();
            GillaStains.LayNoise();
        }
        wet.enabled = true;
        props.SetVector(TipId, new Vector4(tipOnMesh.x, tipOnMesh.y, tipOnMesh.z, wetTo[0] * perMesh));
        props.SetVector(EdgeId, new Vector4(edgeOnMesh.x, edgeOnMesh.y, edgeOnMesh.z, wetTo[1] * perMesh));
        props.SetFloat(DryId, Mathf.Clamp01((Time.time - wetAt) / 45f));
        wet.SetPropertyBlock(props);
    }

    private void Swings(float dt)
    {
        bool notMine = true;
        if (item.Mine)
        {
            if (!item.Gone)
            {
                notMine = item.Swallowed;
            }
        }
        for (int e = 0; e < 2; e++)
        {
            Vector3 from = was[e];
            Vector3 to = transform.TransformPoint(Tip(e));
            was[e] = to;
            Vector3 v = (to - from) / dt;
            float sq = v.sqrMagnitude;
            if (1600f < sq)
            {
                swung[e] = Vector3.zero;
                continue;
            }
            if (Time.time - swungAt[e] > 0.09f || swung[e].sqrMagnitude <= sq)
            {
                swung[e] = v;
                swungAt[e] = Time.time;
            }
            float speed = swung[e].magnitude;
            if (e == 0 && hardSpeed < speed && swishAt <= Time.time)
            {
                if (item.Hand > -1 || item.HeldByOther)
                {
                    swishAt = Time.time + 0.45f;
                    Sfx.Play(swingSounds, to, Mathf.Min(speed / 12f, 1f), 0.92f, 1.1f);
                }
            }
            if (notMine || Time.time < againAt)
            {
                scraping[e] = false;
            }
            else
            {
                if (item.Hand > -1 || !Clangs(from, to, speed))
                {
                    if (Mathf.Min(stabSpeed, knockSpeed) <= speed)
                    {
                        if (Looks(e, from, to, swung[e], speed))
                        {
                            return;
                        }
                    }
                    if (item.Hand < 0)
                    {
                        scraping[e] = false;
                    }
                    else
                    {
                        Scrapes(e, to);
                    }
                }
            }
        }
    }

    private void HeldKnock(Vector3 at, Vector3 faces, float speed, Collider on)
    {
        GillaGlass glass = null;
        if (!item.Mine || into != Into.Nothing)
        {
            return;
        }
        for (int e = 0; e < 2; e++)
        {
            Vector3 tip = transform.TransformPoint(Tip(e));
            if ((tip - at).sqrMagnitude < 0.04f)
            {
                float s = swung[e].magnitude;
                if (speed <= s)
                {
                    speed = s;
                }
            }
        }
        if (on != null && glassSpeed <= speed)
        {
            if (on.TryGetComponent<GillaGlass>(out glass))
            {
                FieldInfo brokenField = typeof(GillaGlass).GetField("broken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) /* ? */;
                bool isBroken = brokenField != null && (bool)brokenField.GetValue(glass);
                if (!isBroken)
                {
                    glass.Struck(at, -(faces * speed) * 0.6f);
                    Buzz(0.8f, 0.12f);
                    return;
                }
            }
        }
        if (knockSpeed <= speed)
        {
            if (clangAt <= Time.time)
            {
                clangAt = Time.time + 0.2f;
                byte stuff;
                if (on != null)
                {
                    stuff = StuffOf(on);
                }
                else
                {
                    stuff = Stone;
                }
                float hard = 0f;
                if (knockSpeed != hardSpeed)
                {
                    hard = Mathf.Clamp01((speed - knockSpeed) / (hardSpeed - knockSpeed));
                }
                if (Live)
                {
                    RPC_Clang(at, faces, stuff, hard);
                }
                else
                {
                    Clang(at, faces, stuff, hard);
                }
            }
        }
        else
        {
            item.Knock(at, speed);
        }
    }

    private bool Clangs(Vector3 from, Vector3 to, float speed)
    {
        RaycastHit hit = default(RaycastHit);
        if (knockSpeed <= speed && clangAt <= Time.time)
        {
            if (Physics.Linecast(from, to, out hit, -5893, QueryTriggerInteraction.Ignore))
            {
                clangAt = Time.time + 0.2f;
                byte stuff = StuffOf(hit.collider);
                float hard = 0f;
                if (knockSpeed != hardSpeed)
                {
                    hard = Mathf.Clamp01((speed - knockSpeed) / (hardSpeed - knockSpeed));
                }
                if (Live)
                {
                    RPC_Clang(hit.point, hit.normal, stuff, hard);
                }
                else
                {
                    Clang(hit.point, hit.normal, stuff, hard);
                }
                return true;
            }
        }
        return false;
    }

    private static byte StuffOf(Collider c)
    {
        Renderer renderer = c.GetComponentInParent<Renderer>();
        string name = "";
        if (renderer != null)
        {
            name = "";
            if (renderer.sharedMaterial != null)
            {
                name = renderer.sharedMaterial.name.ToLowerInvariant();
            }
        }
        string[] metals = new string[] { "metal", "steel", "iron", "rust", "pipe", "door", "locker", "grate", "vent" };
        for (int i = 0; i < metals.Length; i++)
        {
            if (name.Contains(metals[i]))
            {
                return Metal;
            }
        }
        string[] softs = new string[] { "wood", "plank", "carpet", "fabric", "cloth", "dirt", "grass", "mud", "foam" };
        for (int i = 0; i < softs.Length; i++)
        {
            if (name.Contains(softs[i]))
            {
                return Soft;
            }
        }
        return Stone;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Clang(Vector3 at, Vector3 normal, byte stuff, float hard)
    {
        Clang(at, normal, stuff, hard);
    }

    private void Clang(Vector3 at, Vector3 normal, byte stuff, float hard)
    {
        AudioClip[] clips;
        if (stuff == Metal)
        {
            clips = metalSounds;
        }
        else if (stuff == Stone)
        {
            clips = stoneSounds;
        }
        else
        {
            clips = item.knocks;
        }
        Sfx.Play(clips, at, Mathf.Clamp01(hard) * 0.6f + 0.4f, 0.93f, 1.08f);
        Buzz(Mathf.Clamp01(hard) * 0.59999996f + 0.3f, 0.08f);
        if (stuff == Soft || hard <= 0.15f)
        {
            return;
        }
        int many = stuff != Metal ? 14 : 30;
        Sparks(at, normal, many, hard);
    }

    private static void Sparks(Vector3 at, Vector3 normal, int many, float hard)
    {
        ParticleSystem.EmitParams p = default(ParticleSystem.EmitParams);
        if (sparks == null)
        {
            MakeSparks();
        }
        if (sparks == null)
        {
            return;
        }
        p = default(ParticleSystem.EmitParams);
        p.position = at + normal * 0.012f;
        p.velocity = Vector3.zero;
        p.startLifetime = 0.08f;
        p.startSize = Mathf.Clamp01(hard) * 0.045f + 0.045f;
        flash.Emit(p, 1);
        if (0 < many)
        {
            float pace = 0.75f;
            if (0f <= hard)
            {
                pace = Mathf.Min(hard, 1f) * 0.5f + 0.75f;
            }
            do
            {
                Vector3 d = normal * 0.8f + UnityEngine.Random.insideUnitSphere;
                float dot = Vector3.Dot(normal, d);
                if (dot < 0.1f)
                {
                    float more = 0.1f - dot;
                    d = d + normal * more + normal * more;
                }
                p.position = at + normal * 0.006f;
                p.velocity = d.normalized * (UnityEngine.Random.Range(2.2f, 6.5f) * pace);
                p.startLifetime = UnityEngine.Random.Range(0.12f, 0.42f);
                p.startSize = UnityEngine.Random.Range(0.0035f, 0.007f);
                sparks.Emit(p, 1);
                many--;
            } while (many != 0);
        }
    }
}
