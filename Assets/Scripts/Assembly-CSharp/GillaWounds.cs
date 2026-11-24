// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

public partial class GillaWounds : MonoBehaviour
{
    internal class BoneShape
    {
        public Vector3[] at;
        public Vector3[] faces;
        public Vector3[] middles;
        public int[] corners;
        public int[] across;
        public int[] point;
        public int points;
        public Bounds bounds;
    }

    private class Broken
    {
        public MeshFilter filter;
        public Mesh whole;
        public Mesh left;
        public float gone;
        public float thick;
        public int version;
        public GoreSection section;
        public Vector4 cut;
    }

    private class Soon
    {
        public int at;
        public int serial;
        public GoreSection section;
        public Vector4 wound;
        public Vector4 axis;
        public Vector4 side;
        public CraterIn crater;
        public Task<CraterOut> made;
        public bool gone;
        public bool burst;
        public bool stump;
        public Vector3 point;
    }

    private struct Skin
    {
        public Vector3[] vs;
        public int[] tris;
        public Bounds box;
    }

    private class CraterIn
    {
        public int w;
        public GoreSection section;
        public Vector3 middle;
        public Vector3 axis;
        public Vector3 side;
        public Vector3 side2;
        public Vector3 openingAt;
        public float radius;
        public float length;
        public float shape;
        public float pocket;
        public Vector4 cut;
        public Skin[] skin;
        public Vector3[] lump;
        public int[] lumpTris;
        public Transform bone;
        public Transform other;
        public Matrix4x4 bind;
        public Matrix4x4 otherBind;
        public List<(Vector3 at, float share)> places;
    }

    private class CraterOut
    {
        public List<Vector3> verts;
        public List<Vector3> wayOut;
        public List<Vector2> uv;
        public List<Vector2> skinDepth;
        public List<Vector2> bends;
        public List<Vector4> opening;
        public List<int> faces;
    }

    private class BoneOut
    {
        public List<Vector3> verts;
        public List<Vector3> faces;
        public List<Vector3> wayOut;
        public List<Vector2> uv;
        public List<Vector2> depth;
        public List<Vector4> opening;
        public List<int> index;
    }

    public const int Max = 6;
    private const int Spots = 3;
    public const bool ShowsBones = true;
    public const bool ShowsSpray = true;
    public const bool ShowsBits = true;
    public const bool ShowsLumps = true;
    public const bool ShowsOrgans = true;
    private static GillaGoreAssets assets;
    private static readonly ProfilerMarker AddMark = new ProfilerMarker("Gilla.Wound");
    private static readonly ProfilerMarker BonesMark = new ProfilerMarker("Gilla.Wound.Bones");
    private static readonly ProfilerMarker CraterMark = new ProfilerMarker("Gilla.Wound.Crater");
    private static readonly ProfilerMarker BurstMark = new ProfilerMarker("Gilla.Wound.Burst");
    private static readonly ProfilerMarker PutMark = new ProfilerMarker("Gilla.Wound.Put");
    private static readonly int[] WoundIds = new[] { Shader.PropertyToID("_Wound0"), Shader.PropertyToID("_Wound1"), Shader.PropertyToID("_Wound2"), Shader.PropertyToID("_Wound3"), Shader.PropertyToID("_Wound4"), Shader.PropertyToID("_Wound5") };
    private static readonly int[] AxisIds = new[] { Shader.PropertyToID("_Axis0"), Shader.PropertyToID("_Axis1"), Shader.PropertyToID("_Axis2"), Shader.PropertyToID("_Axis3"), Shader.PropertyToID("_Axis4"), Shader.PropertyToID("_Axis5") };
    private static readonly int[] SideIds = new[] { Shader.PropertyToID("_Side0"), Shader.PropertyToID("_Side1"), Shader.PropertyToID("_Side2"), Shader.PropertyToID("_Side3"), Shader.PropertyToID("_Side4"), Shader.PropertyToID("_Side5") };
    private static readonly int[] SpotIds;
    private static readonly Dictionary<Mesh, Mesh> carrying = new Dictionary<Mesh, Mesh>();
    private static readonly Dictionary<Mesh, Vector3[]> vertices = new Dictionary<Mesh, Vector3[]>();
    private static readonly Dictionary<Mesh, Vector3[]> normals = new Dictionary<Mesh, Vector3[]>();
    private static readonly Dictionary<Mesh, BoneShape> boneShapes = new Dictionary<Mesh, GillaWounds.BoneShape>();
    private static readonly Dictionary<Mesh, int[]> skinPoints = new Dictionary<Mesh, int[]>();
    private static readonly Dictionary<Mesh, int[]> skinTriangles = new Dictionary<Mesh, int[]>();
    private static readonly Dictionary<Mesh, BoneWeight[]> boneWeights = new Dictionary<Mesh, BoneWeight[]>();
    private static readonly Dictionary<Mesh, Bounds> boxes = new Dictionary<Mesh, Bounds>();
    private static readonly int OffLId = Shader.PropertyToID("_OffL");
    private static readonly int OffRId = Shader.PropertyToID("_OffR");
    private const float FarOff = 1.5f;
    private Dismemberment copy;
    private GillaCorpse corpse;
    private SkinnedMeshRenderer[] skins;
    private readonly Dictionary<string, (Transform bone, Matrix4x4 bind)> bones = new Dictionary<string, ValueTuple<Transform, Matrix4x4>>();
    private readonly Vector4[] wounds = new Vector4[6];
    private readonly Vector4[] axes = new Vector4[6];
    private readonly Vector4[] sides = new Vector4[6];
    private readonly GameObject[] walls = new GameObject[6];
    private int count;
    private int next;
    private readonly bool[] showing = new bool[6];
    private MonsterGore fitted;
    private Mesh fittedMesh;
    private readonly bool[] armOff = new bool[6];
    private readonly Vector4[] cuts = new Vector4[6];
    private readonly GoreSection[] landedOn = new GoreSection[6];
    private readonly bool[] stumps = new bool[6];
    private readonly List<(GameObject go, GoreSection section)> pieces = new List<ValueTuple<GameObject, GoreSection>>();
    private readonly Dictionary<Material, Material> wounded = new Dictionary<Material, Material>();
    private readonly List<(SkinnedMeshRenderer skin, Mesh mesh, Material[] mats)> whole = new List<ValueTuple<SkinnedMeshRenderer, Mesh, Material[]>>();
    private readonly List<(Transform eye, Vector3 was)> eyesOut = new List<ValueTuple<Transform, Vector3>>();
    private readonly List<Material> skinMats = new List<Material>();
    private readonly List<Mesh> wallMeshes = new List<Mesh>();
    private readonly List<Broken> breakable = new List<GillaWounds.Broken>();
    private Material boneMat;
    private Material gutMat;
    private Material wallMat;
    private bool hurt;
    private readonly bool[] drawnOn = new bool[6];
    private readonly Vector4[] drawn = new Vector4[6];
    private readonly Vector4[] drawnAxes = new Vector4[6];
    private readonly Vector4[] drawnSides = new Vector4[6];
    private readonly List<Soon> soons = new List<GillaWounds.Soon>();
    private readonly HashSet<Broken> toBreak = new HashSet<GillaWounds.Broken>();
    private List<(Broken b, int version, Task<BoneOut> made)> breaking;
    private int serials;
    private int breakingUpTo;
    private int brokenUpTo;
    private static readonly List<GillaWounds> busy = new List<GillaWounds>();
    private static readonly Queue<Action> after = new Queue<Action>();
    private static readonly Queue<(GillaWounds on, GoreSection section, Vector3 point, bool burst)> waiting = new Queue<ValueTuple<GillaWounds, GoreSection, Vector3, bool>>();
    private static readonly Stopwatch addFor = new Stopwatch();
    private static int addFrame = -1;
    private static bool adding;
    private const double AddMsAFrame = 3;
    private const float Under = 0.12f;
    private static readonly Stopwatch putFor = new Stopwatch();
    private const double PutMs = 1;
    private const int Rings = 26;
    private const int Round = 56;
    private static Mesh lumpFrom;
    private static Vector3[] lumpAt;
    private static int[] lumpTris;

    private static bool AddedEnough
    {
        get
        {
            if (addFrame == Time.frameCount)
            {
                return addFor.Elapsed.TotalMilliseconds > 3.0;
            }
            return false;
        }
    }

    private static GillaGoreAssets Assets
    {
        get
        {
            if (assets != null)
            {
                return assets;
            }
            assets = Resources.Load<GillaGoreAssets>("GillaGore");
            return assets;
        }
    }

    public int Count
    {
        get { return count; }
    }

    public int EyesOut
    {
        get { return eyesOut.Count; }
    }

    private bool Headless
    {
        get
        {
            if (copy != null)
            {
                return Array.Exists(copy.parts, p => p.id == 0 && p.renderer == null);
            }
            return false;
        }
    }

    private static bool Step_b__150_0(ValueTuple<Broken, int, Task<BoneOut>> x)
    {
        return x.Item3.IsCompleted;
    }

    private static int cctor_b__203_0(int i)
    {
        return Shader.PropertyToID(string.Format("_Spot{0}{1}", i / 3, i % 3));
    }

    private static CraterOut OnItsWay_b__0(CraterIn c)
    {
        return Crater(c);
    }

    private static bool ArmTorn_b__0(ValueTuple<GameObject, GoreSection> x, GameObject wall)
    {
        return x.Item1 == wall;
    }

    private static bool ArmTorn_b__1(ValueTuple<GameObject, GoreSection> x, GameObject go)
    {
        return x.Item1 == go;
    }

    private static BoneOut Step_b__1(BoneShape shape, Vector4[] w, Vector4[] ax, Vector4[] sd, int n, float gone, float thick, Vector4 cut)
    {
        return BreakMath(shape, w, ax, sd, n, gone, thick, cut);
    }

    private static float UnderSkinAlong_b__0(Vector3 q, Vector3 from, Vector3 side, Vector3 side2, Vector3 axis, float half, float size, List<Vector2>[] grid, float pocket, float radius)
    {
        Vector3 d = q - from;
        int ix = (int)((half + Vector3.Dot(d, side2)) / size);
        if (ix > 63)
        {
            return -1f;
        }
        int iy = (int)((half + Vector3.Dot(d, side)) / size);
        if (iy > 63)
        {
            return -1f;
        }
        if ((iy | ix) < 0)
        {
            return -1f;
        }
        List<Vector2> cell = grid[iy + ix * 64];
        if (cell == null)
        {
            return -1f;
        }
        float a = Vector3.Dot(d, axis);
        float above = float.MaxValue;
        float below = float.MaxValue;
        float nearest = float.MaxValue;
        float sumBefore = 0f;
        float sumAfter = 0f;
        bool anyBefore = false;
        foreach (Vector2 v in cell)
        {
            float newAbove = above;
            if (v.y > 0f)
            {
                newAbove = Mathf.Min(above, v.x - a);
            }
            float newBelow = below;
            if (v.y < 0f)
            {
                newBelow = Mathf.Min(below, a - v.x);
            }
            if (v.x <= a)
            {
                sumBefore += v.y;
                below = newBelow;
                anyBefore = true;
            }
            else
            {
                above = newAbove;
                sumAfter -= v.y;
            }
            float gap = Mathf.Abs(v.x - a);
            if (gap <= nearest)
            {
                nearest = gap;
            }
        }
        float t;
        if (sumBefore <= 0f && (!anyBefore || sumAfter <= 0f))
        {
            if (above + below < pocket)
            {
                t = Mathf.Min(above, below) / radius;
                if (t < 0f)
                {
                    return 0f;
                }
                if (t > 1f)
                {
                    t = 1f;
                }
                return t;
            }
            nearest = -nearest;
        }
        t = nearest / radius;
        if (t < -1f)
        {
            return -1f;
        }
        if (t > 1f)
        {
            t = 1f;
        }
        return t;
    }

    private static float Crater_g__R_0(System.Random rng)
    {
        return (float)rng.NextDouble();
    }

    private static float Crater_b__1(Vector3 p, Vector4 cut, Func<Vector3, float> inSkin)
    {
        if (p.z * cut.z + p.x * cut.x + p.y * cut.y > cut.w)
        {
            return -1f;
        }
        return inSkin(p);
    }

    private static float Crater_g__Wobble_2(float round, float shape)
    {
        return WobbleOf(round, shape);
    }

    private static Vector3 Crater_g__Way_3(float down, float round, Vector3 side, Vector3 side2, Vector3 axis, float shape)
    {
        float s = Mathf.Sqrt(Mathf.Max(0f, 1f - down * down)) * Crater_g__Wobble_2(round, shape);
        float sin = Mathf.Sin(round);
        float cos = Mathf.Cos(round);
        return (side * cos + side2 * sin) * s + axis * down;
    }

    private static Vector2 Crater_g__Photo_4(Vector3 p, float turnPhoto, Vector2 shiftPhoto)
    {
        float a = (p.x + p.y * 0.7f) * 5f;
        float b = (p.z + p.y * -0.5f) * 5f;
        float sin = Mathf.Sin(turnPhoto);
        float cos = Mathf.Cos(turnPhoto);
        return new Vector2(shiftPhoto.x + (a * cos - b * sin), shiftPhoto.y + a * sin + b * cos);
    }

    private static float Crater_g__Down_5(Vector3 p, Vector3 middle, Vector3 axis, float radius, float length)
    {
        float t = (Vector3.Dot(p - middle, axis) + radius * 0.12f * length) / (radius * 1.12f * length);
        float r = 0f;
        if (t >= 0f)
        {
            r = t;
            if (t > 1f)
            {
                r = 1f;
            }
        }
        return r;
    }

    public static GillaWounds On(Dismemberment copy)
    {
        if (copy == null)
        {
            return null;
        }
        GillaWounds w = copy.GetComponent<GillaWounds>();
        if (!(w == null))
        {
            return w;
        }
        return copy.gameObject.AddComponent<GillaWounds>();
    }

    public static GillaWounds On(GameObject monster, MonsterGore gore, SkinnedMeshRenderer skin, Mesh imported)
    {
        GillaWounds x = monster.GetComponent<GillaWounds>();
        if (x == null)
        {
            x = monster.AddComponent<GillaWounds>();
        }
        x.fitted = gore;
        x.fittedMesh = imported;
        x.skins = new SkinnedMeshRenderer[] { skin };
        if (gore != null && gore.mesh != null)
        {
            Matrix4x4[] binds = gore.mesh.bindposes;
            Transform[] bs = skin.bones;
            for (int i = 0; i < bs.Length; i++)
            {
                if (i >= binds.Length)
                {
                    return x;
                }
                if (bs[i] != null)
                {
                    string key = bs[i].name;
                    Matrix4x4 bind = binds[i] * Matrix4x4.Scale(Vector3.one / gore.units);
                    x.bones[key] = new ValueTuple<Transform, Matrix4x4>(bs[i], bind);
                }
            }
            return x;
        }
        return x;
    }

    public static GillaWounds OnBody(GillaCorpse body, Vector4[] holes, Vector4[] ways, Vector4[] holeSides, int many)
    {
        GillaWounds w = body.gameObject.AddComponent<GillaWounds>();
        w.corpse = body;
        w.Find();
        if (many > 5)
        {
            many = 6;
        }
        int n = 0;
        if (holes != null)
        {
            n = many;
        }
        for (int i = 0; i < n; i++)
        {
            w.wounds[i] = holes[i];
            w.axes[i] = ways[i];
            w.sides[i] = holeSides[i];
            w.Draw(i, holes[i], ways[i], holeSides[i]);
        }
        w.count = n;
        w.next = n % 6;
        GillaGoreAssets a = Assets;
        if (a != null && a.wound != null)
        {
            SkinnedMeshRenderer[] ss = w.skins;
            for (int i = 0; i < ss.Length; i++)
            {
                Material[] mats = ss[i].sharedMaterials;
                for (int j = 0; j < mats.Length; j++)
                {
                    Material m = mats[j];
                    if (m != null && m.shader == a.wound.shader)
                    {
                        if (!w.skinMats.Contains(m))
                        {
                            w.skinMats.Add(m);
                            w.hurt = true;
                        }
                    }
                }
            }
        }
        if (w.hurt)
        {
            SkinnedMeshRenderer[] ss = w.skins;
            for (int i = 0; i < ss.Length; i++)
            {
                SkinnedMeshRenderer s = ss[i];
                w.whole.Add(new ValueTuple<SkinnedMeshRenderer, Mesh, Material[]>(s, s.sharedMesh, s.sharedMaterials));
            }
        }
        for (int i = 0; i < w.showing.Length; i++)
        {
            w.showing[i] = true;
        }
        return w;
    }

    internal static bool Knows(Mesh imported)
    {
        Mesh carried;
        if (carrying.TryGetValue(imported, out carried))
        {
            return carried != null;
        }
        return false;
    }

    internal static void Know(Mesh imported, Mesh carried, Vector3[] at, Vector3[] faces, BoneWeight[] weights, int[] points, int[] tris, Bounds box)
    {
        vertices[imported] = at;
        normals[imported] = faces;
        boneWeights[imported] = weights;
        skinPoints[imported] = points;
        skinTriangles[imported] = tris;
        boxes[imported] = box;
        carrying[imported] = carried;
    }

    private static Bounds Box(Mesh m)
    {
        Bounds b;
        if (!boxes.TryGetValue(m, out b))
        {
            b = m.bounds;
        }
        return b;
    }

    private static bool IsLimb(GoreSection s)
    {
        return (int)s > 1;
    }
}
