// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(100)]
public partial class MonsterWounds : MonoBehaviour
{
    private class Chain
    {
        public Transform upper;
        public Transform lower;
        public Transform end;
        public Vector3 at;
        public Vector3 hip;
        public Quaternion turn;
        public float low = float.MaxValue;
    }

    private class Thrown : MonoBehaviour
    {
        private Mesh mesh;
        private float wait;
        private float over;
        private float t;
        private Vector3 size;

        public void Begin(Mesh baked, float after, float seconds)
        {
            mesh = baked;
            wait = after;
            over = Mathf.Max(0.01f, seconds);
            size = transform.localScale;
        }

        private void Update()
        {
            t += Time.deltaTime;
            if (t <= wait)
            {
                return;
            }
            float k = Mathf.Clamp01(1f - (t - wait) / over);
            transform.localScale = size * k;
        }

        private void OnDestroy()
        {
            if (mesh != null)
            {
                Destroy(mesh);
            }
        }
    }

    private const int SaysWound = 0;
    private const int SaysArmOff = 1;
    private const int SaysTrapped = 2;
    [Tooltip("An arm this near a blast comes off (m)...")]
    public float tearReach = 1.2f;
    [Tooltip("...one that's broken already, this near.")]
    public float tearAgainReach = 2.4f;
    [Tooltip("How fast it goes at the very worst of it, of how fast it would.")]
    public float slowest = 0.32f;
    [Tooltip("How long a torn-off arm lies about (s), and how long it takes to shrink away at the end of that.")]
    public float pieceLife = 9f;
    [Tooltip("How long a torn-off arm lies about (s), and how long it takes to shrink away at the end of that.")]
    public float pieceShrink = 0.5f;
    [Tooltip("How fast it's thrown (m/s).")]
    public float throwSpeed = 6f;
    [Tooltip("How long the stump keeps spurting (s), its beats and how much is in the first of them (Dismemberment's).")]
    public float bleedSeconds = 4f;
    public int bleedPulses = 6;
    public int gushDroplets = 110;
    public bool Quiet;
    private MonsterBrain brain;
    private SkinnedMeshRenderer skin;
    private SkinnedMeshRenderer poser;
    private MonsterGore gore;
    private GillaWounds wounds;
    private ResourceRequest loading;
    private bool ready;
    private bool started;
    private float perUnit = 1f;
    private float height = 1.8f;
    private float seed;
    private readonly Dictionary<string, Transform> named = new Dictionary<string, Transform>();
    private readonly List<(Vector4 said, bool loud)> waiting = new List<ValueTuple<Vector4, bool>>();
    private readonly List<Action> waitingCalls = new List<Action>();
    private readonly List<MonsterGore.Limb> pickFrom = new List<MonsterGore.Limb>();
    private readonly List<float> pickWeights = new List<float>();
    private readonly int[] armState = new int[2];
    private readonly bool[] hangs = new bool[2];
    private readonly int[] legHits = new int[2];
    private int headHits;
    private int torsoHits;
    private int brokeFirst = -1;
    private int trappedSide = -1;
    private float torsoSide;
    private float limp;
    private float armWeight;
    private Transform pelvis;
    private Transform spine;
    private Transform chest;
    private Transform head;
    private readonly Chain[] arms = new MonsterWounds.Chain[2];
    private readonly Chain[] legs = new MonsterWounds.Chain[2];
    private readonly Transform[] spiderLegs = new Transform[8];
    private int spiderCount;
    private readonly float[] hanging = new float[2];
    private readonly Vector3[] elbow = new Vector3[2];
    private readonly Vector3[] elbowWas = new Vector3[2];
    private readonly Vector3[] wrist = new Vector3[2];
    private readonly Vector3[] wristWas = new Vector3[2];
    private readonly bool[] swinging = new bool[2];
    private readonly float[] armClear = new float[2];
    private float clock;
    private float thrash;
    private float lurch = 1f;
    private Vector3 trapAt;
    private bool trapHeld;
    private static Mesh warmMesh;

    public float Pace { get; private set; } = 1f;
    public float Wreck { get; private set; }
    public float Side { get; private set; }
    public bool Ready { get { return ready; } }
    public GillaWounds Wounds { get { return wounds; } }
    public MonsterGore Gore { get { return gore; } }

    // GillaWounds members below are non-public in the reconstructed GillaWounds; reach them by reflection.
    private static object CallWounds(object target, string name, Type[] types, object[] args) /* ? */
    {
        MethodInfo m = typeof(GillaWounds).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, types, null);
        return m.Invoke(target, args);
    }

    private static Vector3 WoundsWorld(GillaWounds w, string bone, Vector3 local) /* ? */
    {
        return (Vector3)CallWounds(w, "World", new Type[] { typeof(string), typeof(Vector3) }, new object[] { bone, local });
    }

    private static float Saying(int says, GoreSection section)
    {
        return (float)(says * 8 + (int)section);
    }

    public bool ArmIsOff(int side)
    {
        return armState[side] == 2;
    }

    public bool ArmHangs(int side)
    {
        return hangs[side];
    }

    private void Awake()
    {

        brain = GetComponent<MonsterBrain>();
    }

    private void Start()
    {
        if (GetComponent<GillaSplashMonster>() != null)
        {
            enabled = false;
            return;
        }
        Begin();
        if (skin != null && skin.sharedMesh != null)
        {
            StartCoroutine(Load());
        }
    }

    private IEnumerator Load()
    {
        loading = Resources.LoadAsync<MonsterGore>("MonsterGore/" + skin.sharedMesh.name);
        yield return loading;
        yield return Prepare(loading.asset as MonsterGore);
    }

    private void Begin()
    {
        if (started)
        {
            return;
        }
        started = true;
        if (brain == null)
        {
            brain = GetComponent<MonsterBrain>();
        }
        skin = GetComponentInChildren<SkinnedMeshRenderer>(true);
        CharacterController cc = GetComponent<CharacterController>();
        float h = cc != null ? cc.height : 1.8f;
        height = h * transform.lossyScale.y;
        seed = UnityEngine.Random.value * 300f;
        if (skin == null)
        {
            return;
        }
        Transform[] bones = skin.bones;
        for (int i = 0; i < bones.Length; i++)
        {
            Transform b = bones[i];
            if (b != null)
            {
                named[b.name] = b;
            }
        }
        Transform sp = Bone("spine");
        if (sp == null)
        {
            sp = Bone("seg5");
        }
        spine = sp;
        Transform ch = Bone("chest");
        if (ch == null)
        {
            ch = Bone("seg6");
        }
        chest = ch;
        if (spine != null)
        {
            pelvis = spine.parent;
        }
        else
        {
            pelvis = Bone("root");
        }
        head = Bone("head");
        arms[0] = ChainOf("upperarm_L", "lowerarm_L", "hand_L");
        arms[1] = ChainOf("upperarm_R", "lowerarm_R", "hand_R");
        legs[0] = ChainOf("thigh_L", "shin_L", "foot_L");
        legs[1] = ChainOf("thigh_R", "shin_R", "foot_R");
        for (int i = 0; i <= 3; i++)
        {
            string[] sides = new string[] { "_L", "_R" };
            for (int j = 0; j < sides.Length; j++)
            {
                Transform leg = Bone("leg" + i.ToString() + "a" + sides[j]);
                if (leg != null)
                {
                    spiderLegs[spiderCount++] = leg;
                }
            }
        }
    }

    private Transform Bone(string name)
    {
        Transform t = null;
        if (name == null)
        {
            return null;
        }
        if (!named.TryGetValue(name, out t))
        {
            t = null;
        }
        return t;
    }

    private Chain ChainOf(string upper, string lower, string end)
    {
        Transform u = Bone(upper);
        Transform l = Bone(lower);
        Transform e = Bone(end);
        if (u != null && l != null && e != null)
        {
            Chain c = new Chain();
            c.upper = u;
            c.lower = l;
            c.end = e;
            return c;
        }
        return null;
    }

    private IEnumerator Prepare(MonsterGore baked)
    {
        gore = baked;
        if (gore == null)
        {
            yield break;
        }
        if (gore.mesh == null)
        {
            yield break;
        }
        if (skin == null)
        {
            yield break;
        }
        Mesh imported = skin.sharedMesh;
        if (!GillaWounds.Knows(imported))
        {
            Vector3[] at;
            float units;
            int[][] lists;
            int[] tris;
            int[] points;
            Mesh m = gore.mesh;
            at = m.vertices;
            yield return null;
            Vector3[] faces = m.normals;
            yield return null;
            BoneWeight[] weights = m.boneWeights;
            yield return null;
            Material[] mats = skin.sharedMaterials;
            lists = new int[m.subMeshCount][];
            for (int s = 0; s < lists.Length; s++)
            {
                string matName = "";
                if (s < mats.Length && mats[s] != null)
                {
                    matName = mats[s].name;
                }
                if (matName.Contains("Eye"))
                {
                    continue;
                }
                lists[s] = m.GetTriangles(s);
                yield return null;
            }
            points = null;
            tris = null;
            units = gore.units;
            Task sums = Task.Run(() =>
            {
                for (int i = 0; i < at.Length; i++)
                {
                    at[i] = at[i] * units;
                }
                List<int> all = new List<int>();
                bool[] used = new bool[at.Length];
                for (int i = 0; i < lists.Length; i++)
                {
                    int[] list = lists[i];
                    if (list == null)
                    {
                        continue;
                    }
                    all.AddRange(list);
                    for (int k = 0; k < list.Length; k++)
                    {
                        used[list[k]] = true;
                    }
                }
                tris = all.ToArray();
                List<int> pts = new List<int>();
                for (int i = 0; i < used.Length; i++)
                {
                    if (used[i])
                    {
                        pts.Add(i);
                    }
                }
                points = pts.ToArray();
            });
            while (!sums.IsCompleted)
            {
                yield return null;
            }
            if (sums.IsFaulted)
            {
                Debug.LogException(sums.Exception);
                yield break;
            }
            Bounds bounds = m.bounds;
            GillaWounds.Know(imported, m, at, faces, weights, points, tris, new Bounds(bounds.center * units, bounds.size * units));
            GillaGoreAssets.Piece[] pieces = gore.pieces;
            for (int s = 0; s < pieces.Length; s++)
            {
                GillaGoreAssets.Piece piece = pieces[s];
                if (piece.mesh != null)
                {
                    Vector3[] pa;
                    Vector3[] pf;
                    int[] pc;
                    Bounds pb;
                    object[] shapeArgs = new object[] { piece.mesh, null, null, null, null };
                    bool will = (bool)CallWounds(null, "WillShape", new Type[] { typeof(Mesh), typeof(Vector3[]).MakeByRefType(), typeof(Vector3[]).MakeByRefType(), typeof(int[]).MakeByRefType(), typeof(Bounds).MakeByRefType() }, shapeArgs); /* ? */
                    pa = (Vector3[])shapeArgs[1];
                    pf = (Vector3[])shapeArgs[2];
                    pc = (int[])shapeArgs[3];
                    pb = shapeArgs[4] != null ? (Bounds)shapeArgs[4] : default(Bounds);
                    if (will)
                    {
                        Mesh mesh = piece.mesh;
                        Task.Run<GillaWounds.BoneShape>(() => (GillaWounds.BoneShape)CallWounds(null, "Shaped", new Type[] { typeof(Mesh), typeof(Vector3[]), typeof(Vector3[]), typeof(int[]), typeof(Bounds) }, new object[] { mesh, pa, pf, pc, pb })); /* ? */
                    }
                }
                yield return null;
            }
        }
        wounds = GillaWounds.On(gameObject, gore, skin, imported);
        if (gore.limbs.Length != 0)
        {
            Vector3 a = WoundsWorld(wounds, gore.limbs[0].bone, Vector3.right); /* ? */
            Vector3 b = WoundsWorld(wounds, gore.limbs[0].bone, Vector3.zero); /* ? */
            perUnit = Vector3.Distance(a, b);
        }
        MonsterGore.Arm[] goreArms = gore.arms;
        for (int i = 0; i < goreArms.Length; i++)
        {
            MonsterGore.Arm arm = goreArms[i];
            armClear[arm.section != GoreSection.ArmL ? 1 : 0] = arm.clear * perUnit;
        }
        ready = true;
        foreach ((Vector4 said, bool loud) w in waiting)
        {
            Take(w.said, w.loud);
        }
        waiting.Clear();
        foreach (Action call in waitingCalls)
        {
            call();
        }
        waitingCalls.Clear();
    }

    public void PrepareNow(MonsterGore baked)
    {
        Begin();
        IEnumerator e = Prepare(baked);
        while (e.MoveNext())
        {
        }
    }

    public void Blast(Vector3 at, float near)
    {
        Vector3 nearest;
        MonsterGore.Limb limb;
        Vector3 point;
        if (!isActiveAndEnabled && Application.isPlaying)
        {
            return;
        }
        if (near <= 0f)
        {
            return;
        }
        if (!ready)
        {
            waitingCalls.Add(() => Blast(at, near));
            return;
        }
        MonsterGore.Arm[] goreArms = gore.arms;
        if (goreArms.Length > 0)
        {
            float best = float.MaxValue;
            MonsterGore.Arm pick = null;
            for (int i = 0; i < goreArms.Length; i++)
            {
                MonsterGore.Arm a = goreArms[i];
                int side = a.section != GoreSection.ArmL ? 1 : 0;
                Chain c = arms[side];
                if (c == null || armState[side] == 2)
                {
                    continue;
                }
                if (!gore.legs && armState[a.section == GoreSection.ArmL ? 1 : 0] == 2)
                {
                    continue;
                }
                float d = ToSegment(at, c.upper.position, c.lower.position, out nearest);
                float d2 = ToSegment(at, c.lower.position, c.end.position, out nearest);
                if (d2 <= d)
                {
                    d = d2;
                }
                float reach = armState[side] == 1 ? tearAgainReach : tearReach;
                if (!(reach <= d || best <= d))
                {
                    pick = a;
                    best = d;
                }
            }
            if (pick != null)
            {
                Say(SaysArmOff, pick.section, pick.cut);
            }
        }
        MonsterGore.Limb[] limbs = gore.limbs;
        float closest = float.MaxValue;
        for (int i = 0; i < limbs.Length; i++)
        {
            MonsterGore.Limb l = limbs[i];
            Vector3 from = WoundsWorld(wounds, l.bone, l.from); /* ? */
            Vector3 to = WoundsWorld(wounds, l.bone, l.to); /* ? */
            float d = ToSegment(at, from, to, out nearest);
            if (d <= closest)
            {
                closest = d;
            }
        }
        int count;
        if (closest < 1f)
        {
            count = 3;
        }
        else if (closest >= 2.2f || near <= 0.4f)
        {
            count = 1;
        }
        else
        {
            count = 2;
        }
        do
        {
            if (Pick(at, false, out limb, out point))
            {
                Say(SaysWound, limb.section, point);
            }
            count--;
        } while (count != 0);
    }

    public void Trapped(Vector3 at)
    {
        MonsterGore.Limb limb;
        Vector3 point;
        if (!isActiveAndEnabled && Application.isPlaying)
        {
            return;
        }
        if (ready)
        {
            if (Pick(at, true, out limb, out point))
            {
                Say(SaysTrapped, limb.section, point);
            }
            return;
        }
        waitingCalls.Add(() => Trapped(at));
    }

    public void Stabbed(GoreSection section, Vector3 point)
    {
        if (!isActiveAndEnabled && Application.isPlaying)
        {
            return;
        }
        if (!ready)
        {
            return;
        }
        MonsterNet net = null;
        if (brain != null)
        {
            net = brain.net;
        }
        Vector4 said = new Vector4(point.x, point.y, point.z, (float)section);
        if (net != null && net.Live)
        {
            if (!net.CanSay)
            {
                return;
            }
            net.Say(said);
        }
        Take(said, !Quiet);
    }

    public bool Hit(Vector3 at, float reach, out GoreSection section, out Vector3 point)
    {
        Vector3 nearest;
        section = GoreSection.Torso;
        point = Vector3.zero;
        if (ready)
        {
            MonsterGore.Limb[] limbs = gore.limbs;
            if (limbs.Length > 0)
            {
                MonsterGore.Limb best = null;
                Vector3 p = at;
                for (int i = 0; i < limbs.Length; i++)
                {
                    MonsterGore.Limb limb = limbs[i];
                    if (limb.section == GoreSection.ArmR)
                    {
                        if (armState[1] == 2)
                        {
                            continue;
                        }
                    }
                    else if (limb.section == GoreSection.ArmL)
                    {
                        if (armState[0] == 2)
                        {
                            continue;
                        }
                    }
                    Vector3 a = WoundsWorld(wounds, limb.bone, limb.from); /* ? */
                    Vector3 b = WoundsWorld(wounds, limb.bone, limb.to); /* ? */
                    float d = ToSegment(at, a, b, out nearest) - limb.radius * perUnit;
                    if (d < reach)
                    {
                        Vector3 dir = (at - nearest).normalized;
                        p = nearest + dir * (limb.radius * perUnit);
                        best = limb;
                        reach = d;
                    }
                }
                if (best != null)
                {
                    section = best.section;
                    Vector3 onBody = (Vector3)CallWounds(wounds, "OnBody", new Type[] { typeof(string), typeof(Vector3) }, new object[] { best.bone, p }); /* ? */
                    point = (Vector3)CallWounds(wounds, "NearestSkin", new Type[] { typeof(GoreSection), typeof(Vector3) }, new object[] { section, onBody }); /* ? */
                    return true;
                }
            }
        }
        return false;
    }
}
