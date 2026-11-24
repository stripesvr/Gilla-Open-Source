// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(600)]
public partial class GillaStrands : MonoBehaviour
{
    private enum Kind
    {
        Thread = 0,
        Rope = 1,
        Sheet = 2
    }

    private struct Vertex
    {
        public Vector3 at;
        public Vector3 normal;
        public Color32 colour;
        public Vector2 uv;
    }

    private struct End
    {
        public int b0;
        public int b1;
        public int b2;
        public float w0;
        public float w1;
        public float w2;
        public Vector3 p0;
        public Vector3 p1;
        public Vector3 p2;
    }

    private class Strand
    {
        public End onA;
        public End onB;
        public readonly Vector3[] at = new Vector3[9];
        public readonly Vector3[] was = new Vector3[9];
        public Kind kind;
        public float wide;
        public float deep;
        public float goesAt;
        public float goesAfter;
        public float hangsFor;
        public float gone;
        public float slack;
        public float heavy;
        public float stub;
        public Vector3 drift;
        public Vector3 flatWay;
        public bool snapped;
        public bool hidden;
        public int cut;
        public int sides;
        public int firstVertex;
        public float beadAt;
        public float bead;
        public float beadBig;
        public float beadGrows;
        public float waves;
        public float phase;
        public float tone;
    }

    private struct Spot
    {
        public Vector3 at;
        public Vector3 into;
        public Transform on;
        public float room;
        public int vertex;
    }

    private const int Points = 9;
    private const int Rings = 17;
    private const int MostAtOnce = 6;
    private const int MostDrops = 14;
    private const float MostHeld = 14f;
    private const float Sink = 0.012f;
    public static bool On;
    private static readonly Color32 Dark = new Color32(78, 5, 7, 0);
    private static readonly Color32 Bright = new Color32(156, 24, 19, 0);
    private static readonly List<GillaStrands> live = new List<GillaStrands>();
    private static GillaGoreAssets gore;
    private static readonly Dictionary<int, Vector2[]> rounds = new Dictionary<int, Vector2[]>();
    private static readonly Dictionary<Mesh, BoneWeight[]> weightsOf = new Dictionary<Mesh, BoneWeight[]>();
    private static readonly List<Vector3> readAt = new List<Vector3>();
    private static readonly List<Vector3> readFaces = new List<Vector3>();
    private static readonly List<Vector2> readDown = new List<Vector2>();
    private static readonly List<int> readTris = new List<int>();
    private static readonly Vector3[] ringAt = new Vector3[17];
    private static Vector3[] readWorld = new Vector3[4096];
    private Transform a;
    private Transform b;
    private readonly List<Vector3> rimA = new List<Vector3>();
    private Rigidbody pulled;
    private Strand[] strands;
    private Mesh mesh;
    private Vertex[] verts;
    private readonly List<Transform> anchors = new List<Transform>();
    private Matrix4x4[] anchorNow;
    private float age;
    private float held;
    private float size;
    private int left;
    private int dropFirst;
    private readonly Vector3[] dropAt = new Vector3[14];
    private readonly Vector3[] dropGoing = new Vector3[14];
    private readonly float[] dropSize = new float[14];
    private readonly float[] dropLife = new float[14];
    private readonly float[] dropFloor = new float[14];
    private readonly bool[] dropShown = new bool[14];
    private int dropsOut;
    private static readonly Vector3[] DropRound = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
    private static readonly int[] DropTris;

    public int Many
    {
        get
        {
            if (strands != null)
            {
                return strands.Length;
            }
            return 0;
        }
    }

    public void EndsOf(int i, out Vector3 onA, out Vector3 onB, out int kind, out bool whole)
    {
        Fetch();
        onA = Where(ref strands[i].onA);
        onB = Where(ref strands[i].onB);
        kind = (int)strands[i].kind;
        whole = !strands[i].snapped;
    }

    public static GillaStrands Between(Transform a, Transform b, Vector3 at, Vector3 outWay, Vector3 along, float across, float longWay, int many, float size)
    {
        if (On && !(a == null) && !(b == null) && Application.isPlaying)
        {
            if (gore == null)
            {
                gore = Resources.Load<GillaGoreAssets>("GillaGore");
            }
            if (!(gore == null))
            {
                if (gore.strand == null)
                {
                    return null;
                }
                live.RemoveAll(s => s == null);
                if (live.Count > 5)
                {
                    Destroy(live[0].gameObject);
                    live.RemoveAt(0);
                }
                GameObject go = new GameObject("Strands");
                GillaStrands strands = go.AddComponent<GillaStrands>();
                strands.Begin(a, b, at, outWay, along, across, longWay, many, size);
                live.Add(strands);
                return strands;
            }
        }
        return null;
    }

    private void Begin(Transform sideA, Transform sideB, Vector3 where, Vector3 outWay, Vector3 along, float across, float longWay, int many, float scale)
    {
        if (gore == null)
        {
            gore = Resources.Load<GillaGoreAssets>("GillaGore");
        }
        GillaStains.LayNoise();
        a = sideA;
        b = sideB;
        size = Mathf.Max(0.05f, scale);
        pulled = sideB.GetComponent<Rigidbody>();
        if (outWay.sqrMagnitude <= 1e-06f)
        {
            outWay = Vector3.up;
        }
        else
        {
            outWay = outWay.normalized;
        }
        int count = Mathf.RoundToInt(many * 0.66f);
        if (count > 19) count = 20;
        if (count < 6) count = 5;
        strands = new Strand[count];
        List<Spot> inA = new List<Spot>();
        List<Spot> inB = new List<Spot>();
        InsideOf(sideA, inA, rimA);
        InsideOf(sideB, inB, null);

        bool fromCap;
        bool plain;
        Transform[] bones = null;
        BoneWeight[] weights = null;
        if (inA.Count < 1 || inB.Count < 1)
        {
            inB.Clear();
            float near = Mathf.Max(longWay * 2.5f, size * 0.2f);
            Vector3 middle;
            float round;
            Vector3 into;
            if (CapOf(sideB, where, near, inB, count * 8 + 16, out middle, out round, out into))
            {
                SkinnedMeshRenderer torso = TorsoOf(sideA);
                if (torso != null)
                {
                    bones = torso.bones;
                    weights = WeightsOf(torso.sharedMesh);
                    TorsoCap(torso, bones, weights, middle, round * 1.4f, -into, count * 8 + 16, inA);
                }
                plain = false;
            }
            else
            {
                plain = true;
            }
            fromCap = true;
        }
        else
        {
            fromCap = false;
            plain = true;
        }

        float sink = size * 0.012f;
        across = Mathf.Max(across, longWay);
        for (int i = 0; i < count; i++)
        {
            Kind kind = Kind.Sheet;
            if (count <= i * 6)
            {
                kind = (i * 2 < count) ? Kind.Rope : Kind.Thread;
            }
            Strand st = new Strand();
            st.kind = kind;
            strands[i] = st;
            Vector3 p;
            Vector3 q;
            Vector3 flat;
            if (fromCap)
            {
                if (plain)
                {
                    if (along.sqrMagnitude <= 1e-06f)
                    {
                        along = Vector3.Cross(outWay, Vector3.right).normalized;
                    }
                    else
                    {
                        along = Vector3.ProjectOnPlane(along, outWay).normalized;
                    }
                    Vector3 up2 = Vector3.Cross(outWay, along).normalized;
                    Vector2 c = Random.insideUnitCircle;
                    p = where + along * (across * c.x) + up2 * (c.y * longWay);
                    float r1 = Random.Range(-0.3f, 0.3f);
                    float r2 = Random.Range(-0.3f, 0.3f);
                    q = p + (along * r1 + up2 * r2) * longWay;
                    st.onA = Rigid(sideA, p);
                    st.onB = Rigid(sideB, q);
                    flat = up2;
                }
                else
                {
                    Spot sb = Pick(inB, kind, null);
                    q = sb.at + sb.into * sink;
                    st.onB = Rigid(sb.on, q);
                    if (inA.Count < 1)
                    {
                        p = sb.at - sb.into * sink;
                        st.onA = Rigid(Steady(sideA), p);
                    }
                    else
                    {
                        Spot sa = Nearest(inA, sb.at + Random.insideUnitSphere * size * 0.022f, kind);
                        BoneWeight by = weights[sa.vertex];
                        p = sa.at + sa.into * sink;
                        st.onA = OnSkin(bones, by, p, Steady(sideA));
                    }
                    flat = Vector3.Cross(sb.into, Random.onUnitSphere);
                }
            }
            else
            {
                Spot sa = Pick(inA, kind, rimA);
                Spot sb = Nearest(inB, sa.at + Random.insideUnitSphere * size * 0.02f, kind);
                p = sa.at + sa.into * sink;
                st.onA = Rigid(sa.on, p);
                q = sb.at + sb.into * sink;
                st.onB = Rigid(sb.on, q);
                flat = Vector3.Cross(sa.into, Random.onUnitSphere);
            }
            Shape(st, p, q, flat);
        }
        anchorNow = new Matrix4x4[anchors.Count];
        left = count;
        Build();
    }

    private void Shape(Strand st, Vector3 p, Vector3 q, Vector3 flat)
    {
        float v = Random.value;
        Kind kind = st.kind;
        st.sides = kind == Kind.Sheet ? 8 : (kind == Kind.Rope ? 6 : 5);
        float slack;
        float bead;
        if (kind == Kind.Rope)
        {
            float w = size * (Mathf.Clamp01(v) * 0.0085f + 0.0095f);
            st.wide = w;
            st.deep = w;
            st.goesAt = size * (Mathf.Clamp01(v * 0.4f + Random.value * 0.6f) * 0.73f + 0.42f);
            st.goesAfter = Mathf.Clamp01(v * 0.4f + Random.value * 0.6f) * 2.2f + 1.4f;
            st.heavy = Random.Range(0.4f, 0.9f);
            slack = Random.Range(1.04f, 1.22f);
            bead = 0.011f;
        }
        else if (kind == Kind.Sheet)
        {
            st.wide = Random.Range(0.024f, 0.05f) * size;
            st.deep = Random.Range(0.0055f, 0.0085f) * size;
            float c = Mathf.Clamp01(v);
            st.goesAt = (c * 0.3f + 0.2f) * size;
            st.goesAfter = c * 1.3f + 0.9f;
            st.heavy = Random.Range(0.5f, 0.9f);
            slack = Random.Range(1.03f, 1.12f);
            bead = 0.012f;
        }
        else
        {
            float w = size * (Mathf.Clamp01(v) * 0.0036f + 0.0042f);
            st.wide = w;
            st.deep = w;
            st.goesAt = size * (Mathf.Clamp01(Random.value) * 0.59f + 0.26f);
            st.goesAfter = Mathf.Clamp01(Random.value) * 1.9f + 0.9f;
            st.heavy = Random.Range(0.1f, 0.5f);
            slack = Random.Range(1.01f, 1.14f);
            bead = 0.0065f;
        }
        Vector3 dir = q - p;
        st.slack = slack;
        st.beadBig = size * bead;
        for (int i = 0; i < 9; i++)
        {
            float t = Mathf.Clamp01(i / 8f);
            Vector3 pos = p + dir * t;
            st.was[i] = pos;
            st.at[i] = pos;
        }
        Vector3 r = Random.onUnitSphere;
        if (dir.sqrMagnitude <= 1e-08f)
        {
            dir = Vector3.up;
        }
        r = Vector3.ProjectOnPlane(r, dir);
        st.drift = r * Random.Range(0.3f, 3f);
        st.hangsFor = Random.Range(2.5f, 6f);
        st.stub = Random.Range(0.03f, 0.075f) * size * (st.kind == Kind.Thread ? 0.7f : 1.2f);
        st.cut = Random.Range(2, 6);
        st.beadAt = Random.Range(5f, 11f);
        st.beadGrows = Random.Range(0.18f, 0.55f) * st.heavy;
        st.waves = Random.Range(2f, 4.2f);
        st.phase = Random.value * 6.283f;
        st.tone = Random.value;
        Transform t0 = anchors[st.onA.b0];
        if (t0 != null)
        {
            flat = t0.InverseTransformDirection(flat);
        }
        st.flatWay = flat;
    }

    private int Anchor(Transform t)
    {
        int i = anchors.IndexOf(t);
        if (i > -1)
        {
            return i;
        }
        anchors.Add(t);
        return anchors.Count - 1;
    }

    private End Rigid(Transform t, Vector3 world)
    {
        int i = Anchor(t);
        End e = new End();
        e.b0 = i;
        e.b1 = -1;
        e.b2 = -1;
        e.w0 = 1f;
        e.w1 = 0f;
        e.w2 = 0f;
        e.p0 = t.InverseTransformPoint(world);
        e.p1 = Vector3.zero;
        e.p2 = Vector3.zero;
        return e;
    }

    private static void InsideOf(Transform half, List<Spot> found, List<Vector3> rim)
    {
        MeshFilter[] filters = half.GetComponentsInChildren<MeshFilter>(true);
        for (int f = 0; f < filters.Length; f++)
        {
            MeshFilter mf = filters[f];
            if (mf.name != "GoreBone_Inside") continue;
            if (mf.sharedMesh == null) continue;
            if (!mf.sharedMesh.isReadable) continue;
            Mesh m = mf.sharedMesh;
            m.GetVertices(readAt);
            m.GetNormals(readFaces);
            m.GetUVs(2, readDown);
            if (readFaces.Count != readAt.Count) continue;
            if (readDown.Count != readAt.Count) continue;
            Matrix4x4 mat = mf.transform.localToWorldMatrix;
            for (int v = 0; v < readAt.Count; v++)
            {
                float d = readDown[v].x / 0.34f;
                if (d >= 0.02f)
                {
                    if (v % 3 == 0 && d >= 0.4f)
                    {
                        Spot s = new Spot();
                        s.at = mat.MultiplyPoint3x4(readAt[v]);
                        s.into = -mat.MultiplyVector(readFaces[v]).normalized;
                        s.on = mf.transform;
                        s.room = -1f;
                        s.vertex = 0;
                        found.Add(s);
                    }
                }
                else if (rim != null)
                {
                    rim.Add(mat.MultiplyPoint3x4(readAt[v]));
                }
            }
        }
    }

    private static bool CapOf(Transform piece, Vector3 cut, float near, List<Spot> found, int many, out Vector3 middle, out float round, out Vector3 into)
    {
        MeshFilter mf = null;
        SkinnedMeshRenderer skin = null;
        Renderer rend = null;
        into = Vector3.zero;
        middle = Vector3.zero;
        round = 0f;
        Mesh m;
        if (piece.TryGetComponent<MeshFilter>(out mf))
        {
            m = mf.sharedMesh;
        }
        else if (piece.TryGetComponent<SkinnedMeshRenderer>(out skin))
        {
            m = skin.sharedMesh;
        }
        else
        {
            m = null;
        }
        if (m == null)
        {
            return false;
        }
        if (!m.isReadable)
        {
            return false;
        }
        if (m.subMeshCount < 2)
        {
            return false;
        }
        m.GetTriangles(readTris, m.subMeshCount - 1);
        if (readTris.Count < 9)
        {
            return false;
        }
        m.GetVertices(readAt);
        if (readWorld.Length < readAt.Count)
        {
            readWorld = new Vector3[readAt.Count];
        }
        if (skin != null)
        {
            Transform[] bones = skin.bones;
            Matrix4x4[] binds = m.bindposes;
            BoneWeight[] weights = WeightsOf(m);
            foreach (int idx in readTris)
            {
                int bi = weights[idx].boneIndex0;
                Matrix4x4 bm = bones[bi].localToWorldMatrix;
                readWorld[idx] = bm.MultiplyPoint3x4(binds[bi].MultiplyPoint3x4(readAt[idx]));
            }
        }
        else
        {
            Matrix4x4 mat = piece.localToWorldMatrix;
            foreach (int idx in readTris)
            {
                readWorld[idx] = mat.MultiplyPoint3x4(readAt[idx]);
            }
        }
        middle = MiddleOf(readTris, out round);
        if ((middle - cut).sqrMagnitude > near * near)
        {
            return false;
        }
        Vector3 centre;
        if (skin != null)
        {
            centre = piece.position + piece.up * 0.3f;
        }
        else if (piece.TryGetComponent<Renderer>(out rend))
        {
            centre = rend.bounds.center;
        }
        else
        {
            centre = piece.position;
        }
        Vector3 dir = centre - middle;
        if (dir.sqrMagnitude <= 1e-06f)
        {
            into = piece.forward;
        }
        else
        {
            into = dir.normalized;
        }
        Places(readTris, into, piece, many, found);
        return true;
    }
}
