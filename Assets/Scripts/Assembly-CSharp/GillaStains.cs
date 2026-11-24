// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

public static partial class GillaStains
{
    public class Sheet
    {
        public Texture2D map;
        public readonly byte[] pixels = new byte[262144];
        public int users;
        public int working;
        public bool changed;
        public bool egg;
        public float nextFade;
        public float fadeOver;
    }

    public class Shape
    {
        public Vector3[] at;
        public Vector3[] faces;
        public Vector2[] uv;
        public BoneWeight[] weights;
        public Matrix4x4[] binds;
        public int[] tris;
        public int[] fur;
    }

    public class Piece
    {
        public Mesh mesh;
        public string name;
        public Shape shape;
        public Matrix4x4[] bones;
        public Vector3[] at;
        public Vector3[] faces;
    }

    public class Job
    {
        public Sheet sheet;
        public List<Piece> pieces;
        public Vector3 point;
        public Vector3 to;
        public float radius;
        public float reach;
        public float strength;
        public float big;
        public bool line;
        public GillaSplatter.Stuff stuff;
        public int seed;
    }

    private struct Blot
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 faces;
        public float abX;
        public float abY;
        public float abZ;
        public float ab2;
        public float ra;
        public float rb;
        public int channel;
    }

    public const int Size = 256;
    private const float EggLasts = 24f;
    private const float FadeEvery = 0.25f;
    private const int MostSpare = 4;
    private static readonly List<Sheet> sheets = new List<GillaStains.Sheet>();
    private static readonly Stack<Sheet> spare = new Stack<GillaStains.Sheet>();
    private static readonly Dictionary<Texture, Sheet> ofMap = new Dictionary<Texture, GillaStains.Sheet>();
    private static readonly Queue<Job> waiting = new Queue<GillaStains.Job>();
    private static readonly Dictionary<string, Shape> shapes = new Dictionary<string, GillaStains.Shape>();
    private static readonly Dictionary<Mesh, string> shapeNames = new Dictionary<Mesh, string>();
    private static Task queue;
    private static bool noiseLaid;
    private static readonly int NoiseId = Shader.PropertyToID("_StainNoise");
    private static readonly ProfilerMarker SendMark = new ProfilerMarker("Gilla.Stains.Send");
    private static readonly ProfilerMarker ReadMark = new ProfilerMarker("Gilla.Stains.Read");
    private static readonly ProfilerMarker PaintMark = new ProfilerMarker("Gilla.Stains.Paint");
    private static Blot[] blots = new GillaStains.Blot[256];
    private static Vector4[] blotBalls = new Vector4[256];
    private static int blotCount;
    private static readonly List<int> nearBlots = new List<int>();
    private const int Across = 14;
    private static readonly List<int>[] squares = new List<int>[2744];
    private static readonly List<int> usedSquares = new List<int>();
    private static Vector3[][] posedAt = new Vector3[8][];
    private static Vector3[][] posedFaces = new Vector3[8][];
    private static float[] upTo = new float[1];
    private static int[] upToPiece = new int[1];
    private static int[] upToTri = new int[1];
    private static Matrix4x4[] skinning = new Matrix4x4[64];

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Fresh()
    {
        foreach (Sheet s in sheets)
        {
            if (s.map != null)
            {
                UnityEngine.Object.Destroy(s.map);
            }
        }
        foreach (Sheet s in spare)
        {
            if (s.map != null)
            {
                UnityEngine.Object.Destroy(s.map);
            }
        }
        sheets.Clear();
        spare.Clear();
        ofMap.Clear();
        waiting.Clear();
        noiseLaid = false;
    }

    public static void LayNoise()
    {
        if (noiseLaid) return;
        GillaGoreAssets assets = Resources.Load<GillaGoreAssets>("GillaGore");
        Texture value = null;
        if (assets != null)
        {
            // Original read a wound material field on GillaGoreAssets; field name unknown, so find a Material carrying _WoundNoise. /* ? */
            foreach (FieldInfo f in typeof(GillaGoreAssets).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) /* ? */
            {
                Material mat = f.GetValue(assets) as Material;
                if (mat != null && mat.HasProperty("_WoundNoise"))
                {
                    value = mat.GetTexture("_WoundNoise");
                    break;
                }
            }
        }
        if (!(value == null))
        {
            Shader.SetGlobalTexture(NoiseId, value);
            noiseLaid = true;
        }
    }

    public static Sheet NewSheet()
    {
        LayNoise();
        Sheet s = null;
        while (spare.Count > 0)
        {
            if (s != null && !(s.map == null)) break;
            s = spare.Pop();
        }
        if (s == null || s.map == null)
        {
            s = new Sheet();
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
            tex.name = "GillaStains";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            s.map = tex;
            s.changed = true;
        }
        else
        {
            Send(s);
        }
        ofMap[s.map] = s;
        sheets.Add(s);
        return s;
    }

    public static Sheet OfMap(Texture map)
    {
        Sheet s = null;
        if (map != null)
        {
            if (!ofMap.TryGetValue(map, out s))
            {
                s = null;
            }
        }
        return s;
    }

    public static void Add(Job j)
    {
        Interlocked.Increment(ref j.sheet.working);
        waiting.Enqueue(j);
    }

    public static void Pump()
    {
        bool readOne = false;
        while (waiting.Count > 0)
        {
            Job j = waiting.Peek();
            if (j.pieces.Exists(p => p.shape == null && p.mesh == null))
            {
                waiting.Dequeue();
                Interlocked.Decrement(ref j.sheet.working);
            }
            else
            {
                if (!Read(j, ref readOne)) break;
                waiting.Dequeue();
                queue = queue.ContinueWith(_ =>
                {
                    PaintMark.Begin();
                    Paint(j);
                    if (0f < j.radius && j.stuff == GillaSplatter.Stuff.Yolk)
                    {
                        j.sheet.egg = true;
                    }
                    j.sheet.changed = true;
                    PaintMark.End();
                    Interlocked.Decrement(ref j.sheet.working);
                }, TaskScheduler.Default);
            }
        }

        float now = Time.unscaledTime;
        bool sent = false;
        for (int i = sheets.Count - 1; i >= 0; i--)
        {
            Sheet s = sheets[i];
            if (s.users < 1)
            {
                if (s.working < 1)
                {
                    sheets.RemoveAt(i);
                    if (!(s.map == null))
                    {
                        if (spare.Count < 4)
                        {
                            Array.Clear(s.pixels, 0, s.pixels.Length);
                            s.changed = true;
                            s.egg = false;
                            spare.Push(s);
                        }
                        else
                        {
                            ofMap.Remove(s.map);
                            UnityEngine.Object.Destroy(s.map);
                        }
                    }
                }
            }
            else
            {
                if (s.egg && s.nextFade <= now)
                {
                    Sheet fading = s;
                    s.nextFade = now + 0.25f;
                    Interlocked.Increment(ref fading.working);
                    queue = queue.ContinueWith(_ => Fade(fading), TaskScheduler.Default);
                }
                if (sent)
                {
                    sent = true;
                }
                else
                {
                    if (s.changed && s.map != null)
                    {
                        Send(s);
                        sent = true;
                    }
                    else
                    {
                        sent = false;
                    }
                }
            }
        }
    }

    public static void Send(Sheet s)
    {
        SendMark.Begin();
        s.changed = false;
        s.map.SetPixelData(s.pixels, 0, 0);
        s.map.Apply(false, false);
        SendMark.End();
    }

    public static void Finish()
    {
        for (int n = 16; n != 0; n--)
        {
            if (waiting.Count < 1) break;
            Pump();
        }
        queue.Wait();
    }

    private static bool Read(Job j, ref bool readOne)
    {
        bool finished = true;
        foreach (Piece p in j.pieces)
        {
            if (p.shape != null) continue;
            string name;
            if (!shapeNames.TryGetValue(p.mesh, out name))
            {
                Mesh mesh = p.mesh;
                int count = mesh.vertexCount;
                name = p.name + count.ToString();
                shapeNames[mesh] = name;
            }
            if (!shapes.TryGetValue(name, out p.shape))
            {
                if (readOne)
                {
                    finished = false;
                    break;
                }
                readOne = true;
                ReadMark.Begin();
                int[] tris = p.mesh.GetTriangles(0);
                HashSet<int> set = new HashSet<int>(tris);
                int[] fur = new int[set.Count];
                set.CopyTo(fur);
                Vector2[] uv = p.mesh.uv;
                for (int i = 0; i < uv.Length; i++)
                {
                    uv[i] = uv[i] * 256f;
                }
                Shape shape = new Shape();
                shape.at = p.mesh.vertices;
                shape.faces = p.mesh.normals;
                shape.uv = uv;
                shape.weights = p.mesh.boneWeights;
                shape.binds = p.mesh.bindposes;
                shape.tris = tris;
                shape.fur = fur;
                p.shape = shape;
                shapes[name] = shape;
                ReadMark.End();
            }
        }
        return finished;
    }

    private static void Work(Job j)
    {
        PaintMark.Begin();
        Paint(j);
        if (0f < j.radius && j.stuff == GillaSplatter.Stuff.Yolk)
        {
            j.sheet.egg = true;
        }
        j.sheet.changed = true;
        PaintMark.End();
        Interlocked.Decrement(ref j.sheet.working);
    }

    private static void Fade(Sheet s)
    {
        byte[] px = s.pixels;
        float over = s.fadeOver + 2.65625f;
        int step = (int)over;
        s.fadeOver = over - step;
        bool any = false;
        for (int i = 1; i < px.Length; i += 4)
        {
            byte r = px[i];
            byte g = px[i + 1];
            if (g != 0 || r != 0)
            {
                px[i] = (byte)Mathf.Max(r - step, 0);
                any = true;
                px[i + 1] = (byte)Mathf.Max(g - step, 0);
            }
        }
        s.changed = any || s.egg;
        if (!any)
        {
            s.egg = false;
        }
        Interlocked.Decrement(ref s.working);
    }

    public static void Paint(Job j)
    {
        System.Random rng = new System.Random(j.seed);
        blotCount = 0;
        for (int i = 0; i < j.pieces.Count; i++)
        {
            Pose(j.pieces[i], i);
        }
        if (!j.line)
        {
            if (j.radius <= 0f)
            {
                Thrown(j, rng);
            }
            else
            {
                Landed(j, rng);
            }
        }
        else
        {
            Scratched(j, rng);
        }
        if (0 < blotCount)
        {
            Draw(j);
        }
    }

    public static void Pose(Piece p, int index)
    {
        Shape shape = p.shape;
        int count = shape.at.Length;
        if (posedAt.Length <= index)
        {
            Array.Resize(ref posedAt, index + 4);
            Array.Resize(ref posedFaces, index + 4);
        }
        if (posedAt[index] == null || posedAt[index].Length < count)
        {
            posedAt[index] = new Vector3[count];
            posedFaces[index] = new Vector3[count];
        }
        Vector3[] at = posedAt[index];
        p.at = at;
        Vector3[] faces = posedFaces[index];
        p.faces = faces;
        if (skinning.Length < p.bones.Length)
        {
            skinning = new Matrix4x4[p.bones.Length];
        }
        for (int i = 0; i < p.bones.Length && i < shape.binds.Length; i++)
        {
            skinning[i] = p.bones[i] * shape.binds[i];
        }
        for (int v = 0; v < count; v++)
        {
            BoneWeight w = shape.weights[v];
            Vector3 a = shape.at[v];
            Vector3 n = shape.faces[v];
            Matrix4x4 m0 = skinning[w.boneIndex0];
            Vector3 pos = m0.MultiplyPoint3x4(a);
            Vector3 nor = m0.MultiplyVector(n);
            if (w.weight0 < 0.995f)
            {
                Matrix4x4 m1 = skinning[w.boneIndex1];
                pos = pos * w.weight0 + m1.MultiplyPoint3x4(a) * w.weight1;
                nor = nor * w.weight0 + m1.MultiplyVector(n) * w.weight1;
                if (0.001f < w.weight2)
                {
                    Matrix4x4 m2 = skinning[w.boneIndex2];
                    pos += m2.MultiplyPoint3x4(a) * w.weight2;
                    nor += m2.MultiplyVector(n) * w.weight2;
                    if (0.001f < w.weight3)
                    {
                        Matrix4x4 m3 = skinning[w.boneIndex3];
                        pos += m3.MultiplyPoint3x4(a) * w.weight3;
                        nor += m3.MultiplyVector(n) * w.weight3;
                    }
                }
            }
            at[v] = pos;
            faces[v] = nor;
        }
    }

    private static void Add(Vector3 a, Vector3 b, float ra, float rb, Vector3 faces, int channel)
    {
        if (blotCount == blots.Length)
        {
            Array.Resize(ref blots, blotCount * 2);
            Array.Resize(ref blotBalls, blotCount * 2);
        }
        Vector3 ab = b - a;
        Blot blot = new Blot();
        blot.a = a;
        blot.b = b;
        blot.faces = faces;
        blot.ra = ra;
        blot.rb = rb;
        blot.abX = ab.x;
        blot.abY = ab.y;
        blot.abZ = ab.z;
        blot.channel = channel;
        blot.ab2 = ab.z * ab.z + ab.x * ab.x + ab.y * ab.y;
        blots[blotCount] = blot;
        int n = blotCount;
        blotCount = n + 1;
        Vector3 mid = (a + b) * 0.5f;
        blotBalls[n] = new Vector4(mid.x, mid.y, mid.z, Math.Max(ra, rb) + Vector3.Distance(a, b) * 0.5f);
    }

    private static float Rand(System.Random rng, float lo, float hi)
    {
        return (hi - lo) * (float)rng.NextDouble() + lo;
    }

    private static void Snap(Piece p, ref Vector3 at, out Vector3 faces)
    {
        int[] fur = p.shape.fur;
        Vector3[] pts = p.at;
        int best = 0;
        float bestD = float.MaxValue;
        for (int i = 0; i < fur.Length; i++)
        {
            int k = fur[i];
            float d = (pts[k] - at).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = k;
            }
        }
        faces = p.faces[best].normalized;
        float dist = Vector3.Dot(at - pts[best], faces);
        at -= faces * dist;
    }

    private static Vector3 Sideways(Vector3 faces, System.Random rng)
    {
        Vector3 axis = Mathf.Abs(faces.y) <= 0.9f ? Vector3.up : Vector3.right;
        Vector3 side = Vector3.Cross(faces, axis).normalized;
        float angle = (float)rng.NextDouble() * 3.1415927f * 2f;
        return side * Mathf.Cos(angle) + Vector3.Cross(faces, side) * Mathf.Sin(angle);
    }

    private static void Lobed(Piece p, Vector3 at, Vector3 faces, float r, int channel, System.Random rng)
    {
        Add(at, at, r, r, faces, channel);
        int n = rng.Next(4, 8);
        for (; n > 0; n--)
        {
            Vector3 dir = Sideways(faces, rng);
            Vector3 pos = at + dir * (((float)rng.NextDouble() * 0.54999995f + 0.5f) * r);
            Vector3 f;
            Snap(p, ref pos, out f);
            float rr = ((float)rng.NextDouble() * 0.42000002f + 0.2f) * r;
            Add(pos, pos, rr, rr, f, channel);
        }
    }

    private static void Thrown(Piece p, Vector3 at, Vector3 faces, float r, int many, int channel, float big, System.Random rng)
    {
        for (int n = many; n > 0; n--)
        {
            Vector3 dir = Sideways(faces, rng);
            Vector3 pos = at + dir * (((float)rng.NextDouble() * 1.7f + 1f) * r);
            Vector3 f;
            Snap(p, ref pos, out f);
            float d = (float)rng.NextDouble();
            Vector3 along = (dir - f * Vector3.Dot(dir, f)).normalized;
            float size = (d * 0.008f + 0.008f) * big;
            float len = size * ((float)rng.NextDouble() * 3.5f + 1f);
            Add(pos, pos + along * len, size, size * 0.35f, f, channel);
        }
    }

    private static void Runs(Piece p, Vector3 from, Vector3 faces, float r, float each, int steps, int channel, System.Random rng)
    {
        Vector3 pos = from;
        Vector3 f = faces;
        for (int n = steps; n > 0; n--)
        {
            Vector3 down = f * f.y + Vector3.down;
            if (down.sqrMagnitude < 0.04f) break;
            Vector3 dir = down.normalized;
            dir = (dir + Sideways(f, rng) * 0.3f).normalized;
            Vector3 next = pos + dir * (((float)rng.NextDouble() * 0.59999996f + 0.7f) * each);
            Vector3 nf;
            Snap(p, ref next, out nf);
            float rb = r * 0.86f;
            Add(pos, next, r, rb, f, channel);
            pos = next;
            r = rb;
            f = nf;
        }
        Add(pos, pos, r * 1.35f, r * 1.35f, f, channel);
    }
}
