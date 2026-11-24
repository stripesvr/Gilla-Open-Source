// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Fusion;
using UnityEngine;

public partial class GillaSplashDirector : MonoBehaviour
{
    public GillaSplashPool[] pools = new GillaSplashPool[0];
    [Tooltip("One of each kind of platform, off (GillaSplashLayout.Kinds, in order): what's put out is copies.")]
    public GameObject[] kinds = new GameObject[0];
    [Tooltip("Each level's tiles for a platform's deck and sides, and the black of an outline.")]
    public Material[] decks = new Material[0];
    [Tooltip("Each level's tiles for a platform's deck and sides, and the black of an outline.")]
    public Material[] sides = new Material[0];
    public Material outline;
    [Tooltip("Gilla_SplashMonster: one made for each pool.")]
    public GameObject monsterPrefab;
    [Tooltip("The dock: where you start (the mode's PLAY puts you here) and where you're put back when it has you.")]
    public Transform spawn;
    [Header("You in the water")]
    [Tooltip("How fast it takes your speed away while you're in it up to your body (per second).")]
    public float drag = 1.6f;
    [Tooltip("While you're in it: how often it hears you (s), moving about; every second, keeping still.")]
    public float noiseEvery = 0.25f;
    [Tooltip("Something big falling in, a hand dipping in, moving about in it.")]
    public AudioClip[] splashSounds;
    [Tooltip("Something big falling in, a hand dipping in, moving about in it.")]
    public AudioClip[] dipSounds;
    [Tooltip("Something big falling in, a hand dipping in, moving about in it.")]
    public AudioClip[] wadeSounds;
    private const float CheckEvery = 2;
    private readonly List<GameObject> local = new List<GameObject>();
    private float offlineFor;
    private float nextCheck;
    private readonly bool[] wasIn = new bool[4];
    private readonly Vector3[] lastAt = new Vector3[4];
    private readonly float[] nextWake = new float[4];
    private readonly Dictionary<Transform, (Vector3 at, float next)> others = new Dictionary<Transform, ValueTuple<Vector3, float>>();
    private Vector3 lastNoise;
    private float nextNoise;
    private Renderer murk;
    private Material murkMat;
    private AudioLowPassFilter muffle;
    private readonly float[] asked = new float[128];
    private readonly float[] sunk = new float[128];
    private static readonly int SunkId = Shader.PropertyToID("_GillaSunk");
    private readonly List<GameObject> standing = new List<GameObject>();
    private readonly List<int> standingKinds = new List<int>();
    private readonly List<GillaSinkPlatform> sinkers = new List<GillaSinkPlatform>();
    private Transform[] hallGroups = new Transform[0];
    private Stack<GameObject>[] spare;
    private bool[] hasPlatforms;
    private int hallsOut = int.MinValue;
    private int built;
    private readonly Dictionary<GillaSplashMonster, Transform[]> limbs = new Dictionary<GillaSplashMonster, Transform[]>();
    private readonly Dictionary<Transform, (Vector3 at, float next)> wading = new Dictionary<Transform, ValueTuple<Vector3, float>>();
    private const float WakeEvery = 0.14f;
    private const float WakeSpeed = 0.35f;

    public static GillaSplashDirector Instance { get; private set; }

    public int Style { get; private set; }

    public IReadOnlyList<GameObject> Standing { get { return standing; } }

    public IReadOnlyList<GillaSinkPlatform> Sinkers { get { return sinkers; } }

    public Transform HallGroup(int hall)
    {
        return hallGroups[hall];
    }

    public GillaSplashLayout.Hall[] Halls
    {
        get
        {
            return pools.Select(p => new GillaSplashLayout.Hall
            {
                water = p.area,
                pillars = p.standing.Select(s => new Vector3(s.at.x, s.at.y, s.half.x)).ToArray()
            }).ToArray();
        }
    }

    private void Awake()
    {

        Instance = this;
        Prepare();
        Shader.SetGlobalFloatArray(SunkId, sunk);
        for (int i = 0; i < pools.Length; i++)
        {
            GillaSplashPool pool = pools[i];
            if (pool != null)
            {
                pool.Lay(new List<GillaSplashPool.Footprint>());
            }
        }
    }

    private void Prepare()
    {
        if (spare != null) return;
        spare = kinds.Select(_ => new Stack<GameObject>()).ToArray();
        hasPlatforms = new bool[pools.Length];
        hallGroups = new Transform[pools.Length];
        Transform root = new GameObject("Platforms").transform;
        root.SetParent(transform, false);
        for (int i = 0; i < pools.Length; i++)
        {
            hallGroups[i] = new GameObject("Hall " + (i + 1).ToString()).transform;
            hallGroups[i].SetParent(root, false);
        }
    }

    public void Preview(int style, int round, int seed)
    {
        Prepare();
        Style = style;
        System.Random rng = new System.Random(seed);
        Put(Halls.SelectMany(hall => GillaSplashLayout.MakeHall(hall, style, round, rng)).ToList());
    }

    private void FollowLayout()
    {
        GillaSplashGame game = GillaSplashGame.Current;
        if (game == null) return;
        if (game.Live && game.Layouts != 0 && game.Layouts != built)
        {
            if (game.LayoutCheck == game.CheckNow())
            {
                built = game.Layouts;
                Style = game.Map;
                List<GillaSplashLayout.Spot> spots = new List<GillaSplashLayout.Spot>(game.PlatformCount);
                for (int i = 0; i < game.PlatformCount; i++)
                {
                    uint v = (uint)game.Platforms[i];
                    spots.Add(new GillaSplashLayout.Spot
                    {
                        kind = (int)((v >> 20) & 0x1F),
                        x = (v & 0x1FF) / 4f - 64f,
                        z = ((v >> 9) & 0x7FF) / 4f,
                        yaw = ((v >> 25) & 0x3F) * 5.625f
                    });
                }
                Put(spots);
            }
        }
    }

    private void Put(List<GillaSplashLayout.Spot> spots)
    {
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < standing.Count; i++)
        {
            standing[i].SetActive(false);
            spare[standingKinds[i]].Push(standing[i]);
        }
        standing.Clear();
        standingKinds.Clear();
        sinkers.Clear();
        Array.Clear(sunk, 0, sunk.Length);
        Array.Clear(asked, 0, asked.Length);
        Shader.SetGlobalFloatArray(SunkId, sunk);
        List<GillaSplashPool.Footprint>[] lays = pools.Select(_ => new List<GillaSplashPool.Footprint>()).ToArray();
        float y = pools.Length == 0 ? 0f : pools[0].transform.localPosition.y;

        for (int i = 0; i < spots.Count; i++)
        {
            GillaSplashLayout.Spot spot = spots[i];
            if (spot.kind < 0 || spot.kind >= kinds.Length) continue;
            if (kinds[spot.kind] == null) continue;
            int hall = Array.FindIndex(pools, p =>
                spot.x >= p.area.xMin && spot.x < p.area.xMin + p.area.width &&
                spot.z >= p.area.yMin && spot.z < p.area.yMin + p.area.height);
            if (hall < 0) continue;

            GillaSplashLayout.Kind info = GillaSplashLayout.Kinds[spot.kind];
            Stack<GameObject> stack = spare[spot.kind];
            GameObject go;
            if (stack.Count < 1)
            {
                go = Instantiate(kinds[spot.kind], hallGroups[hall]);
            }
            else
            {
                go = stack.Pop();
            }
            Transform t = go.transform;
            t.SetParent(hallGroups[hall], false);
            t.localPosition = new Vector3(spot.x, y, spot.z);
            t.localRotation = Quaternion.Euler(0f, spot.yaw, 0f);

            MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                MeshRenderer renderer = renderers[r];
                bool frame = renderer.name == "Frame";
                Material[] mats = new Material[2];
                if (frame)
                {
                    mats[0] = outline;
                    mats[1] = outline;
                }
                else
                {
                    mats[0] = decks[hall];
                    mats[1] = sides[hall];
                }
                renderer.sharedMaterials = mats;
            }

            GillaSinkPlatform sink = go.GetComponent<GillaSinkPlatform>();
            if (sink != null)
            {
                sink.index = i;
                sink.home = t.localPosition;
                sink.homeTurn = t.localRotation;
                sinkers.Add(sink);
            }
            go.SetActive(true);
            standing.Add(go);
            standingKinds.Add(spot.kind);

            int which = 0;
            if (sink != null /* ? */)
            {
                which = i + 1;
            }
            Vector2 size = Vector2.zero;
            MeshFilter filter = go.GetComponentInChildren<MeshFilter>(true);
            if (filter != null && filter.sharedMesh != null)
            {
                Bounds b = filter.sharedMesh.bounds;
                Vector3 scale = filter.transform.lossyScale;
                size = new Vector2(b.size.x * scale.x, b.size.z * scale.z); /* ? */
            }
            lays[hall].Add(new GillaSplashPool.Footprint
            {
                at = new Vector2(spot.x, spot.z),
                half = size /* ? */ * 0.5f,
                yaw = spot.yaw,
                which = which
            });
        }

        for (int p = 0; p < pools.Length; p++)
        {
            if (lays[p].Count > 0 || hasPlatforms[p])
            {
                pools[p].Lay(lays[p]);
            }
            hasPlatforms[p] = lays[p].Count > 0;
        }
        hallsOut = int.MinValue;
        Physics.SyncTransforms();
        UnityEngine.Debug.Log(string.Format("[GillaSplashDirector] {0} platforms out, {1} that sink, in {2} ms", standing.Count, sinkers.Count, watch.ElapsedMilliseconds));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        ClearLocal();
        if (murkMat != null)
        {
            Destroy(murkMat);
        }
    }

    private void Update()
    {
        FollowLayout();
        Spawning();
        You(Time.deltaTime);
        Wakes(Time.deltaTime);
        Sinking();
        Wading(Time.deltaTime);
        HallsOut();
    }

    private void HallsOut()
    {
        Camera cam = Camera.main;
        GillaSplashPool pool = null;
        if (cam != null)
        {
            pool = GillaSplashPool.Near(cam.transform.position, 24f);
        }
        GillaSplashGame game = GillaSplashGame.Current;
        int want;
        if (game != null)
        {
            want = -2;
            if (game.Live)
            {
                want = -2;
                if ((int)game.Phase == 2)
                {
                    want = -1;
                }
            }
        }
        else
        {
            want = -2;
        }
        if (pool != null)
        {
            want = pool.index;
        }
        if (want == hallsOut) return;
        hallsOut = want;
        for (int i = 0; i < hallGroups.Length; i++)
        {
            bool on = want == -1;
            if (want >= 0)
            {
                on = Math.Abs(i - want) < 2;
            }
            if (hallGroups[i].gameObject.activeSelf != on)
            {
                hallGroups[i].gameObject.SetActive(on);
            }
        }
    }

    private void Wading(float dt)
    {
        if (dt <= 0f) return;
        foreach (GillaSplashMonster monster in GillaSplashMonster.All)
        {
            if (monster == null) continue;
            Transform[] arr;
            if (!limbs.TryGetValue(monster, out arr))
            {
                Transform[] all = monster.GetComponentsInChildren<Transform>(true);
                arr = new string[] { "hand_L", "hand_R", "foot_L", "foot_R" }
                    .Select(n => all.FirstOrDefault(t => t.name == n)).ToArray();
                limbs[monster] = arr;
            }
            System.Reflection.FieldInfo stateField = typeof(GillaSplashMonster).GetField("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            int state = stateField != null ? Convert.ToInt32(stateField.GetValue(monster)) : 0; /* ? */
            if ((state & 0xFE) == 2 /* ? */)
            {
                for (int j = 0; j < arr.Length; j++)
                {
                    Transform limb = arr[j];
                    if (limb == null) continue;
                    Vector3 pos = limb.position;
                    Vector3 fwd = monster.transform.forward;
                    float reach = j < 2 ? 0.7f : 0.65f;
                    float drop = j < 2 ? 0.17f : 0.35f;
                    Vector3 p = pos + fwd * reach + Vector3.down * drop;
                    (Vector3 at, float next) prev;
                    bool had = wading.TryGetValue(limb, out prev);
                    Vector3 prevAt = had ? prev.at : p;
                    float next = had ? prev.next : 0f;
                    GillaSplashPool pool = GillaSplashPool.At(p);
                    if (pool != null)
                    {
                        float surface = pool.Surface;
                        float dx = p.x - prevAt.x;
                        float dz = p.z - prevAt.z;
                        bool moving = Mathf.Sqrt(dx * dx + dz * dz) / dt > 1.2f && p.y < surface + 0.3f;
                        if (moving && next <= Time.time)
                        {
                            float s = pool.Surface;
                            GillaSplashPool.Ripple(new Vector3(p.x, s, p.z), 0.35f);
                            GillaVoxelSplash.Emit(new Vector3(p.x, s, p.z), 0.32f);
                            next = Time.time + WakeEvery;
                        }
                    }
                    wading[limb] = (p, next);
                }
            }
        }
        if (limbs.Count < 9) return;
        limbs.Clear();
        wading.Clear();
    }

    private static void Wake(Vector3 at, Vector3 was, float dt, ref float next)
    {
        if (dt > 0f && next <= Time.time)
        {
            GillaSplashPool pool = GillaSplashPool.At(at);
            if (pool != null)
            {
                if (pool.In(at, -0.03f))
                {
                    float dx = at.x - was.x;
                    float dz = at.z - was.z;
                    float speed = Mathf.Sqrt(dx * dx + dz * dz) / dt;
                    if (speed >= WakeSpeed)
                    {
                        next = Time.time + WakeEvery;
                        float surface = pool.Surface;
                        GillaSplashPool.Ripple(new Vector3(at.x, surface, at.z), Mathf.Clamp(speed / 6f, 0.12f, 0.35f));
                    }
                }
            }
        }
    }

    private void Wakes(float dt)
    {
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null) continue;
            if (ape.IsLocal) continue;
            Transform[] parts = new Transform[] { ape.leftHand /* ? */, ape.rightHand /* ? */, ape.head /* ? */ };
            for (int i = 0; i < parts.Length; i++)
            {
                Transform t = parts[i];
                if (t == null) continue;
                (Vector3 at, float next) prev;
                Vector3 was;
                float next;
                if (others.TryGetValue(t, out prev))
                {
                    next = prev.next;
                    was = prev.at;
                }
                else
                {
                    was = t.position;
                    next = 0f;
                }
                Wake(t.position, was, dt, ref next);
                others[t] = (t.position, next);
            }
        }
        if (others.Count > 64)
        {
            others.Clear();
        }
    }

    private static void MakeMissing_b__69_0(byte index, NetworkRunner r, NetworkObject obj)
    {
        obj.GetComponent<GillaSplashMonster>().NetPool = index;
    }
}
