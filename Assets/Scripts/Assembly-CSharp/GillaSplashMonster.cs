// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public partial class GillaSplashMonster : NetworkBehaviour
{
    public const byte Prowl = 0;
    public const byte Hear = 1;
    public const byte Rush = 2;
    public const byte Search = 3;
    public const byte Kill = 4;

    public MonsterBrain brain;
    [Header("Moving (m/s)")]
    public float prowlSpeed = 1.3f;
    [Tooltip("Its top speed coming for a splash: your fastest swing is 6.5 (the user, 2026-11-02, of its run: \"make it faster\"; it was 18).")]
    public float rushSpeed = 24f;
    public float accel = 40f;
    [Tooltip("How much faster it is each round of a game after the first, as a share of its speed (the user, 2026-11-04: \"as each round happens, make the mako faster\"): 24 m/s coming for you in round 1, 41 in round 7.")]
    public float fasterEachRound = 0.12f;
    [Tooltip("Turning on the spot after a splash before it comes (s).")]
    public float hearSeconds = 0.45f;
    public float searchSeconds = 3f;
    [Tooltip("How close its mouth gets (m, across the water) to something of yours in it to have you.")]
    public float reach = 1.5f;
    [Header("How it sits in the water (m down from standing on the pool's floor; less than 0, up out of it)")]
    [Tooltip("Prowling: how high the top of its back is over its feet as it walks (m): it's sunk till that's just under the water, only its dorsal fin out of it, however deep its pool is.")]
    public float prowlBack = 2.48f;
    [Tooltip("The top of its back at the fin's foot (its `fin` bone): what's kept at the water as it prowls, where its wake comes off.")]
    public Transform prowlMark;
    [Tooltip("Coming for a splash, looking for it, and having you: its feet on the pool's floor, all of it over the water.")]
    public float rushSink;
    [Header("Sounds")]
    [Tooltip("As it hears a splash (one of them, pitched down).")]
    public AudioClip[] hearSounds;
    [Tooltip("Its body ploughing through the water as it rushes.")]
    public AudioClip[] surgeSounds;

    public static readonly List<GillaSplashMonster> All = new List<GillaSplashMonster>();
    private byte state;
    private int poolIndex;
    private int kills;
    private int seenKills;
    private PlayerRef victim;
    private float stateT;
    private float speed;
    private float animSpeed;
    private float pace = 1f;
    private float sink;
    private float lookWeight;
    private float wait;
    private float yawVel;
    private float repathAt;
    private float nextWake;
    private float predictedAt = -10f;
    private float surgeAt;
    private Vector3 target;
    private Vector3 look;
    private readonly List<Vector3> path = new List<Vector3>();
    private GillaSplashPool pool;
    private Transform model;
    private Vector3 modelRest;
    private Vector3 lastPos;
    private float killSeconds;

    [Networked] private byte NetState { get; set; }
    [Networked] private float NetSpeed { get; set; }
    [Networked] private float NetPace { get; set; }
    [Networked] private float NetSink { get; set; }
    [Networked] private Vector3 NetLook { get; set; }
    [Networked] private float NetLookWeight { get; set; }
    [Networked] private int NetKills { get; set; }
    [Networked] private PlayerRef NetVictim { get; set; }
    [Networked] public byte NetPool { get; set; }

    public bool Live
    {
        get
        {
            if (Object == null) return false;
            if (!Object.Runner) return false;
            return Object.Runner.Exists(Object) /* ? */;
        }
    }

    private bool Authority
    {
        get
        {
            if (!Live) return true;
            return Object.HasStateAuthority;
        }
    }

    public byte State { get { return state; } }

    public int PoolIndex
    {
        get { return Live ? NetPool : poolIndex; }
        set
        {
            poolIndex = value;
            if (Live && Object.HasStateAuthority)
            {
                NetPool = (byte)value;
            }
        }
    }

    public bool Hunting { get { return (byte)(state - 1) < 3; } }

    public Vector3 Mouth
    {
        get
        {
            if (brain != null && brain.face != null)
            {
                return brain.face.position;
            }
            return transform.position + transform.forward;
        }
    }

    public Vector3 Mark
    {
        get
        {
            if (prowlMark != null) return prowlMark.position;
            return Mouth;
        }
    }

    public float RoundPace
    {
        get
        {
            GillaSplashGame game = GillaSplashGame.Current;
            float p = 1f;
            if (game != null)
            {
                p = 1f;
                if (game.Live)
                {
                    p = fasterEachRound * Mathf.Max(game.Round - 1, 0) + 1f;
                }
            }
            return p;
        }
    }

    private GillaSplashPool Pool
    {
        get
        {
            if (pool != null && pool.index == PoolIndex) return pool;
            pool = GillaSplashPool.All.Find(p => p.index == PoolIndex);
            return pool;
        }
    }

    private void Awake()
    {

        if (brain == null) brain = GetComponent<MonsterBrain>();
        brain.Puppet = true;
        model = brain.animator != null ? brain.animator.transform : null;
        if (model != null) modelRest = model.localPosition;
        float ks = brain.killSeconds;
        if (ks <= 0f) ks = 3.4f;
        killSeconds = ks;
        sink = prowlBack - 0.65f;
        lastPos = transform.position;
    }

    private void OnEnable()
    {

        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public override void Spawned()
    {
        seenKills = NetKills;
        if (Object.HasStateAuthority)
        {
            poolIndex = NetPool;
        }
    }

    public void Splash(Vector3 at, float strength, bool loud)
    {
        if (Live)
        {
            RPC_Splash(at, strength, loud);
            return;
        }
        Splashed(at, strength, loud);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Splash(Vector3 at, float strength, NetworkBool loud)
    {
        Splashed(at, strength, loud);
    }

    private void Splashed(Vector3 at, float strength, bool loud)
    {
        GillaSplashDirector.Show(at, strength, loud);
        if (Authority)
        {
            Heard(at);
        }
    }

    private void Heard(Vector3 at)
    {
        if (state != Kill && !MonsterBrain.Frozen)
        {
            byte s = state;
            target = at;
            if (s == Search)
            {
                stateT = 0f;
                state = Rush;
                repathAt = 0f;
            }
            else
            {
                if (s == Prowl)
                {
                    stateT = 0f;
                    state = Hear;
                    Screech();
                    return;
                }
                repathAt = Mathf.Min(repathAt, Time.time + 0.12f);
            }
        }
    }

    public void Catch()
    {
        if (state != Kill)
        {
            KillCam cam = KillCam.Local;
            if (cam != null && !cam.Busy)
            {
                Transform backTo = Sent();
                if (cam.Begin(brain, backTo))
                {
                    predictedAt = Time.time;
                    brain.PlayKill();
                    if (!Live || Object.HasStateAuthority)
                    {
                        PlayerRef who = Live ? Runner.LocalPlayer : PlayerRef.None;
                        StartKill(who);
                        return;
                    }
                    RPC_Caught(default);
                }
            }
        }
    }

    private static Transform Sent()
    {
        GillaSplashGame game = GillaSplashGame.Current;
        GillaSplashLobby lobby = GillaSplashLobby.Current;
        if (game != null && game.Live && lobby != null && lobby.lobby != null)
        {
            if (game.Phase == 2)
            {
                game.RPC_Died(default);
            }
            return lobby.lobby;
        }
        GillaSplashDirector director = GillaSplashDirector.Instance;
        if (director == null) return null;
        return director.spawn;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_Caught(RpcInfo info = default)
    {
        if (state != Kill)
        {
            StartKill(info.Source);
        }
    }

    private void StartKill(PlayerRef who)
    {
        state = Kill;
        stateT = 0f;
        speed = 0f;
        path.Clear();
        victim = who;
        kills++;
        if (Live && Runner.LocalPlayer != who)
        {
            brain.PlayKill();
            foreach (NetworkApe ape in NetworkApe.All)
            {
                if (ape != null && ape.Owner /* ? */ == who && ape.head /* ? */ != null)
                {
                    Face(ape.head.position, 1f);
                    break;
                }
            }
            return;
        }
        if (KillCam.Local != null)
        {
            Face(KillCam.Local.head.position, 1f);
        }
    }

    private void Begin(byte s)
    {
        state = s;
        stateT = 0f;
        if (s != 0) return;
        wait = Random.Range(0.5f, 2.5f);
    }

    private void Update()
    {
        brain.Puppet = true;
        float dt = Time.deltaTime;
        if (Authority && !MonsterBrain.Frozen)
        {
            Think(dt);
        }
        Show(dt);
    }

    private void Think(float dt)
    {
        GillaSplashPool p = Pool;
        stateT += dt;
        float roundPace = RoundPace;
        float wantSink = prowlBack - (p != null ? p.depth : 0.65f);
        float want = 0f;
        float lookTo;

        if (state < 2)
        {
            if (state == Prowl)
            {
                if (path.Count == 0)
                {
                    wait -= dt;
                    if (wait <= 0f && p != null)
                    {
                        target = p.RandomSpot(transform.position, 6f);
                        path.AddRange(p.Path(transform.position, target));
                        wait = Random.Range(2f, 5f);
                    }
                }
                else
                {
                    want = roundPace * prowlSpeed;
                }
                lookTo = 0f;
            }
            else
            {
                wantSink = rushSink;
                Face(target, dt);
                lookTo = 1f;
                if (hearSeconds <= stateT)
                {
                    stateT = 0f;
                    state = Rush;
                    repathAt = 0f;
                }
            }
        }
        else if (state == Rush)
        {
            wantSink = rushSink;
            if (repathAt <= Time.time && p != null)
            {
                path.Clear();
                path.AddRange(p.Path(transform.position, target));
                repathAt = Time.time + 0.4f;
            }
            Vector3 d = Flat(target - transform.position);
            if (d.magnitude >= 0.6f && path.Count != 0)
            {
                lookTo = 1f;
                want = roundPace * rushSpeed;
            }
            else
            {
                stateT = 0f;
                state = Search;
                lookTo = 1f;
                path.Clear();
            }
        }
        else if (state == Search)
        {
            wantSink = rushSink;
            lookTo = 0.8f;
            if (stateT >= searchSeconds)
            {
                Begin(Prowl);
                wait = Random.Range(1f, 3f);
            }
        }
        else if (state == Kill)
        {
            wantSink = rushSink;
            lookTo = 0f;
            if (stateT >= killSeconds)
            {
                Begin(Prowl);
                wait = Random.Range(2f, 4f);
            }
        }
        else
        {
            lookTo = 0f;
        }

        float rate = want <= speed ? accel * 1.5f : accel;
        speed = Mathf.MoveTowards(speed, want, roundPace * rate * dt);

        if (path.Count < 1)
        {
            if (path.Count == 0 && state == Prowl)
            {
                speed = Mathf.MoveTowards(speed, 0f, accel * dt);
            }
        }
        else if (speed > 0.01f)
        {
            Vector3 next = path[0];
            Vector3 pos = transform.position;
            Vector3 delta = Flat(next - pos);
            float s = speed;
            if (path.Count == 1)
            {
                speed = Mathf.Min(s, Mathf.Max(prowlSpeed, delta.magnitude * 4f));
            }
            float step = s * dt;
            float dist = delta.magnitude;
            Transform t = transform;
            if (dist <= step)
            {
                t.position = Floor(path[0]);
                path.RemoveAt(0);
            }
            else
            {
                t.position = Floor(transform.position + delta.normalized * step);
            }
            if (delta.sqrMagnitude > 0.0001f)
            {
                Face(transform.position + delta, dt);
            }
        }

        float sinkRate = sink <= wantSink ? 0.5f : 4f;
        sink = Mathf.MoveTowards(sink, wantSink, sinkRate * dt);

        if (state == Search)
        {
            Vector3 mouth = Mouth;
            Quaternion sweep = Quaternion.Euler(0f, Mathf.Sin(stateT * 1.7f) * 70f, 0f);
            look = mouth + sweep * transform.forward * 4f;
        }
        else
        {
            look = target + Vector3.up * 0.3f;
        }

        lookWeight = Mathf.MoveTowards(lookWeight, lookTo, dt * 3f);

        float walk = brain.walkSpeed;
        float run = Mathf.Max(brain.runSpeed, walk + 0.1f);
        float pc;
        if (speed <= walk)
        {
            pc = walk / Mathf.Max(brain.clipWalkSpeed, 0.01f);
            animSpeed = speed / Mathf.Max(walk, 0.01f);
        }
        else
        {
            float k = 0f;
            if (walk != run) k = Mathf.Clamp01((speed - walk) / (run - walk));
            animSpeed = k + 1f;
            pc = Mathf.Min(speed / Mathf.Max(brain.clipRunSpeed, 0.01f), 3.2f);
        }
        pace = Mathf.Max(pc, 0.5f);
    }

    private void Face(Vector3 at, float dt)
    {
        Vector3 pos = transform.position;
        float dx = at.x - pos.x;
        float dz = at.z - pos.z;
        if (dx * dx + dz * dz < 0.0001f) return;
        if (1f <= dt)
        {
            Vector3 euler = Quaternion.LookRotation(new Vector3(dx, 0f, dz), Vector3.up).eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }
        else
        {
            Vector3 e = transform.eulerAngles;
            float yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
            float y = Mathf.SmoothDampAngle(e.y, yaw, ref yawVel, 0.12f, Mathf.Infinity, dt) /* ? */;
            transform.rotation = Quaternion.Euler(0f, y, 0f) /* ? */;
        }
    }

    private Vector3 Floor(Vector3 p)
    {
        GillaSplashPool pl = Pool;
        float y = p.y;
        if (pl != null)
        {
            y = pl.Surface - pl.depth;
        }
        return new Vector3(p.x, y, p.z);
    }

    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    private void Screech()
    {
        Sfx.Play(hearSounds, Mouth, 1f, 0.72f, 0.82f);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            NetState = state;
            NetSpeed = animSpeed;
            NetPace = pace;
            NetSink = sink;
            NetLook = look;
            NetLookWeight = lookWeight;
            NetKills = kills;
            NetVictim = victim;
        }
    }

    public override void Render()
    {
        if (Object.HasStateAuthority) return;
        if (NetState == Hear && state != Hear)
        {
            Screech();
        }
        state = NetState;
        animSpeed = NetSpeed;
        pace = NetPace;
        sink = NetSink;
        look = NetLook;
        lookWeight = NetLookWeight;
        poolIndex = NetPool;
        if (NetKills == seenKills) return;
        seenKills = NetKills;
        PlayerRef who = NetVictim;
        if (who == Runner.LocalPlayer)
        {
            if (2f <= Time.time - predictedAt)
            {
                brain.PlayKill();
                KillCam cam = KillCam.Local;
                if (cam != null && !cam.Busy)
                {
                    cam.Begin(brain, Sent());
                }
            }
            return;
        }
        brain.PlayKill();
    }

    private void Show(float dt)
    {
        float anim = animSpeed;
        if (MonsterBrain.Frozen && Authority)
        {
            anim = 0f;
        }
        brain.ShowPuppet(anim, pace, look, lookWeight);
        typeof(MonsterBrain).GetProperty("Chasing", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.SetValue(brain, state == Rush) /* ? */;
        if (model != null)
        {
            model.localPosition = modelRest + Vector3.down * sink;
        }
        Vector3 pos = transform.position;
        Vector3 last = lastPos;
        lastPos = transform.position;
        float moved = 0f;
        if (0f < dt)
        {
            float dx = pos.x - last.x;
            float dz = pos.z - last.z;
            moved = Mathf.Sqrt(dx * dx + dz * dz) / dt;
        }
        if (0.3f < moved && nextWake <= Time.time)
        {
            GillaSplashPool.Ripple(Mark, Mathf.Clamp01(moved / 12f + 0.2f));
            nextWake = (6f < moved ? 0.09f : 0.6f) + Time.time;
        }
        if (6f < moved && surgeAt <= Time.time)
        {
            Sfx.Play(surgeSounds, Mouth, 1f, 0.8f, 0.95f);
            surgeAt = Time.time + 0.16f;
        }
    }
}
