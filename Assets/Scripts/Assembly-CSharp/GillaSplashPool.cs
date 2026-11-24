// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GillaSplashPool : MonoBehaviour
{
    [Serializable]
    public struct Footprint
    {
        public Vector2 at;
        public Vector2 half;
        public float yaw;
        [Tooltip("0: it never goes away (a pillar, an outlined platform); otherwise its platform's number in the layout, plus 1.")]
        public int which;
    }

    [Tooltip("Its number among the place's pools (its monster carries it on the network).")]
    public int index;
    [Tooltip("The water, in this object's own x (x, width) and z (y, height), metres.")]
    public Rect area;
    [Tooltip("How deep it is (m), its surface at this object's height.")]
    public float depth = 0.65f;
    public Transform monsterStart;
    [Header("Its monster's way round")]
    public float cell = 1f;
    public int cols;
    public int rows;
    private byte[] cells;
    [Header("What stands in it")]
    [Tooltip("Its water: Gilla/DotWater, its foam map set on it.")]
    public Renderer surface;
    [Tooltip("What's always there: its pillars, in this object's own x and z.")]
    public Footprint[] standing = new Footprint[0];
    [Tooltip("What your view goes when your head's under it.")]
    public Color murk = new Color(0.02f, 0.2f, 0.24f, 0.88f);
    public static readonly List<GillaSplashPool> All = new List<GillaSplashPool>();
    public const float BodyRoom = 0.5f;
    private readonly List<Footprint> feet = new List<Footprint>();
    private static readonly int[] StepC = new int[] { 1, -1, 0, 0, 1, 1, -1, -1 } /* ? */;
    private static readonly int[] StepR = new int[] { 0, 0, 1, -1, 1, -1, 1, -1 } /* ? */;
    public const float FoamCell = 0.125f;
    public const float FoamRange = 2.4f;
    private static readonly int FoamTexId = Shader.PropertyToID("_FoamTex");
    private static readonly int FoamRectId = Shader.PropertyToID("_FoamRect");
    private Texture2D foamMap;
    private byte[] foamBytes;

    private void OnEnable()
    {

        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public float Surface
    {
        get { return transform.position.y; }
    }

    public Vector2 Local(Vector3 world)
    {
        Vector3 p = transform.InverseTransformPoint(world);
        return new Vector2(p.x, p.z);
    }

    public Vector3 World(Vector2 local)
    {
        return transform.TransformPoint(new Vector3(local.x, 0f, local.y));
    }

    public bool Over(Vector3 world)
    {
        Vector2 p = Local(world);
        if (p.x < area.xMin || area.xMin + area.width <= p.x || p.y < area.yMin)
            return false;
        return p.y < area.yMin + area.height;
    }

    public bool In(Vector3 world, float margin = 0.02f)
    {
        if (!Over(world) || Surface - margin <= world.y)
            return false;
        return (Surface - depth) - 0.5f < world.y;
    }

    public static GillaSplashPool At(Vector3 world)
    {
        foreach (GillaSplashPool pool in All)
        {
            if (pool.Over(world))
                return pool;
        }
        return null;
    }

    public static GillaSplashPool Near(Vector3 world, float round)
    {
        foreach (GillaSplashPool pool in All)
        {
            Vector2 p = pool.Local(world);
            if (p.x > pool.area.xMin - round
                && p.x < pool.area.xMin + pool.area.width + round
                && p.y > pool.area.yMin - round
                && p.y < pool.area.yMin + pool.area.height + round)
                return pool;
        }
        return null;
    }

    private byte[] Cells
    {
        get { return cells; }
    }

    public void Lay(List<Footprint> platforms)
    {
        feet.Clear();
        feet.AddRange(standing);
        feet.AddRange(platforms);
        if (cells == null || rows * cols != cells.Length)
            cells = new byte[rows * cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 m = CellMiddle(c, r);
                bool blocked = m.x < area.xMin + 0.5f
                    || area.xMin + area.width - 0.5f < m.x
                    || m.y < area.yMin + 0.5f
                    || area.yMin + area.height - 0.5f < m.y;
                cells[c + r * cols] = (byte)(blocked ? 1 : 0);
            }
        }

        foreach (Footprint f in feet)
        {
            float reach = f.half.x;
            if (0f < f.half.y)
                reach = Mathf.Sqrt(f.half.x * f.half.x + f.half.y * f.half.y);
            reach += 0.5f;
            float sin = Mathf.Sin(f.yaw * 0.017453292f);
            float cos = Mathf.Cos(f.yaw * 0.017453292f);
            var lo = CellOf(new Vector2(f.at.x - reach, f.at.y - reach));
            var hi = CellOf(new Vector2(f.at.x + reach, f.at.y + reach));
            for (int r = Mathf.Max(lo.Item2, 0); r <= Mathf.Min(hi.Item2, rows - 1); r++)
            {
                for (int c = Mathf.Max(lo.Item1, 0); c <= Mathf.Min(hi.Item1, cols - 1); c++)
                {
                    Vector2 m = CellMiddle(c, r);
                    float dx = m.x - f.at.x;
                    float dy = m.y - f.at.y;
                    bool hit;
                    if (0f < f.half.y)
                    {
                        hit = Mathf.Abs(cos * dx - sin * dy) < f.half.x + 0.5f
                            && Mathf.Abs(sin * dx + cos * dy) < f.half.y + 0.5f;
                    }
                    else
                    {
                        hit = Mathf.Sqrt(dx * dx + dy * dy) < f.half.x + 0.5f;
                    }
                    if (hit)
                        cells[c + r * cols] = 1;
                }
            }
        }
        DrawFoam();
    }

    private bool Free(int c, int r)
    {
        if ((c | r) >= 0 && c < cols && r < rows)
        {
            if (cells != null && rows * cols == cells.Length)
                return cells[c + cols * r] == 0;
            return true;
        }
        return false;
    }

    private ValueTuple<int, int> CellOf(Vector2 local)
    {
        return new ValueTuple<int, int>(
            Mathf.FloorToInt((local.x - area.xMin) / cell),
            Mathf.FloorToInt((local.y - area.yMin) / cell));
    }

    private Vector2 CellMiddle(int c, int r)
    {
        return new Vector2(area.xMin + (c + 0.5f) * cell, area.yMin + (r + 0.5f) * cell);
    }

    private bool NearestFree(Vector2 local, out ValueTuple<int, int> at)
    {
        var start = CellOf(local);
        for (int ring = 0; ring < Mathf.Max(cols, rows); ring++)
        {
            at = new ValueTuple<int, int>(-1, -1);
            float best = float.MaxValue;
            for (int dc = -ring; dc <= ring; dc++)
            {
                for (int dr = -ring; dr <= ring; dr++)
                {
                    if (Mathf.Max(Math.Abs(dc), Math.Abs(dr)) != ring)
                        continue;
                    int c = dc + start.Item1;
                    int r = dr + start.Item2;
                    if (Free(c, r))
                    {
                        Vector2 m = CellMiddle(c, r);
                        float d = (m - local).sqrMagnitude;
                        if (d < best)
                        {
                            at = new ValueTuple<int, int>(c, r);
                            best = d;
                        }
                    }
                }
            }
            if (at.Item1 > -1)
                return true;
        }
        at = new ValueTuple<int, int>(-1, -1);
        return false;
    }

    public Vector3 FreeNear(Vector3 world)
    {
        ValueTuple<int, int> at;
        if (NearestFree(Local(world), out at))
            return World(CellMiddle(at.Item1, at.Item2));
        return world;
    }

    public Vector3 RandomSpot(Vector3 from, float awayFrom)
    {
        Vector2 f = Local(from);
        for (int i = 0; i < 40; i++)
        {
            int c = UnityEngine.Random.Range(0, cols);
            int r = UnityEngine.Random.Range(0, rows);
            if (!Free(c, r))
                continue;
            Vector2 m = CellMiddle(c, r);
            if (i < 30 && Vector2.Distance(m, f) < awayFrom)
                continue;
            return World(m);
        }
        return from;
    }

    private bool Clear(Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        int steps = Mathf.FloorToInt(d.magnitude / (cell * 0.5f));
        if (steps < 0)
            return true;
        for (int i = 0; i <= steps; i++)
        {
            float t = steps == 0 ? 0f : Mathf.Clamp01((float)i / steps);
            var c = CellOf(a + d * t);
            if (!Free(c.Item1, c.Item2))
                return false;
        }
        return true;
    }

    public List<Vector3> Path(Vector3 from, Vector3 to)
    {
        List<Vector3> result = new List<Vector3>();
        Vector2 a = Local(from);
        Vector2 b = Local(to);
        ValueTuple<int, int> s, g;
        if (!NearestFree(a, out s) || !NearestFree(b, out g))
            return result;
        if (Clear(a, b))
        {
            result.Add(World(b));
            return result;
        }

        int n = rows * cols;
        float[] cost = new float[n];
        int[] prev = new int[n];
        for (int i = 0; i < n; i++)
        {
            cost[i] = float.MaxValue;
            prev[i] = -1;
        }
        SortedSet<ValueTuple<float, int>> open = new SortedSet<ValueTuple<float, int>>();
        int startIdx = s.Item1 + s.Item2 * cols;
        cost[startIdx] = 0f;
        Vector2 goalMid = CellMiddle(g.Item1, g.Item2);
        open.Add(new ValueTuple<float, int>(Vector2.Distance(CellMiddle(s.Item1, s.Item2), goalMid), startIdx));
        int goalIdx = g.Item1 + g.Item2 * cols;

        while (open.Count > 0)
        {
            var item = open.Min;
            open.Remove(item);
            int cur = item.Item2;
            if (cur == goalIdx)
                break;
            int r0 = cur / cols;
            int c0 = cur - r0 * cols;
            for (int k = 0; k < 8; k++)
            {
                int c = StepC[k] + c0;
                int r = StepR[k] + r0;
                if (!Free(c, r))
                    continue;
                if (k >= 4 && !(Free(StepC[k] + c0, r0) && Free(c0, StepR[k] + r0)))
                    continue;
                int ni = c + cols * r;
                float step = k < 4 ? 1f : 1.4142f;
                float old = cost[ni];
                float nc = cost[cur] + step * cell;
                if (nc < old)
                {
                    if (old < float.MaxValue)
                        open.Remove(new ValueTuple<float, int>(old + Vector2.Distance(CellMiddle(c, r), goalMid), ni));
                    cost[ni] = nc;
                    prev[ni] = cur;
                    open.Add(new ValueTuple<float, int>(nc + Vector2.Distance(CellMiddle(c, r), goalMid), ni));
                }
            }
        }

        if (prev[goalIdx] < 0 && goalIdx != startIdx)
            return result;

        List<Vector2> pts = new List<Vector2>();
        for (int at = goalIdx; at >= 0; at = prev[at])
        {
            int r = at / cols;
            pts.Add(CellMiddle(at - r * cols, r));
        }
        pts.Reverse();
        if (g.Item1 == CellOf(b).Item1 && g.Item2 == CellOf(b).Item2)
            pts[pts.Count - 1] = b;

        Vector2 here = a;
        int idx = 0;
        while (idx < pts.Count)
        {
            int j = pts.Count - 1;
            while (j > idx && !Clear(here, pts[j]))
                j--;
            result.Add(World(pts[j]));
            here = pts[j];
            idx = j + 1;
        }
        return result;
    }

    public static void Ripple(Vector3 at, float strength)
    {
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        FieldInfo instanceField = typeof(GillaWaterSim).GetField("instance", Flags); /* ? */
        FieldInfo pendingField = typeof(GillaWaterSim).GetField("pending", Flags); /* ? */
        GillaWaterSim instance = instanceField != null ? instanceField.GetValue(null) as GillaWaterSim : null; /* ? */
        if (instance == null)
            return;
        List<Vector4> pending = pendingField != null ? pendingField.GetValue(null) as List<Vector4> : null; /* ? */
        if (pending == null)
            return;
        if (pending.Count > 15)
            pending.RemoveAt(0);
        float s = Mathf.Clamp01(strength);
        pending.Add(new Vector4(at.x, at.z, s * 0.55f + 0.25f, s * 0.57f + 0.18f));
    }

    public Texture2D FoamMap
    {
        get { return foamMap; }
    }

    public Vector4 FoamRect
    {
        get
        {
            Vector3 p = transform.TransformPoint(new Vector3(area.xMin, 0f, area.yMin));
            return new Vector4(p.x, p.z, 1f / area.width, 1f / area.height);
        }
    }

    public void DrawFoam()
    {
        int width = Mathf.FloorToInt(area.width * 8f);
        int height = Mathf.FloorToInt(area.height * 8f);
        if (foamMap == null)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RG16, false, true);
            tex.name = "GillaFoam " + name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            foamMap = tex;
            foamBytes = new byte[width * height * 2];
        }
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                float v = Mathf.Min((d / 8f) * 106.24999f, 255f);
                int i = (y * width + x) * 2;
                foamBytes[i] = (byte)(int)v;
                foamBytes[i + 1] = 0;
            }
        }
        foreach (Footprint f in feet)
            Stamp(in f, width, height, 106.24999f);
        foamMap.LoadRawTextureData(foamBytes);
        foamMap.Apply(false, false);
        if (surface != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            surface.GetPropertyBlock(block);
            block.SetTexture(FoamTexId, foamMap);
            block.SetVector(FoamRectId, FoamRect);
            surface.SetPropertyBlock(block);
        }
    }

    private void Stamp(in Footprint f, int w, int h, float scale)
    {
        float hy = f.half.y;
        float sin = Mathf.Sin(f.yaw * 0.017453292f);
        float cos = Mathf.Cos(f.yaw * 0.017453292f);
        float reach = 0f < hy ? Mathf.Sqrt(f.half.x * f.half.x + f.half.y * f.half.y) : f.half.x;
        reach += 2.4f;
        int x0 = Mathf.Max(Mathf.FloorToInt((f.at.x - reach - area.xMin) * 8f), 0);
        int x1 = Mathf.Min(Mathf.FloorToInt((reach + f.at.x - area.xMin) * 8f), w - 1);
        int y0 = Mathf.Max(Mathf.FloorToInt((f.at.y - reach - area.yMin) * 8f), 0);
        int y1 = Mathf.Min(Mathf.FloorToInt((reach + f.at.y - area.yMin) * 8f), h - 1);
        int which = Mathf.Clamp(f.which, 0, 255);
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                float dx = ((x + 0.5f) * 0.125f + area.xMin) - f.at.x;
                float dy = ((y + 0.5f) * 0.125f + area.yMin) - f.at.y;
                float dist;
                if (0f < hy)
                {
                    float qx = Mathf.Max(Mathf.Abs(dx * cos - dy * sin) - f.half.x, 0f);
                    float qy = Mathf.Max(Mathf.Abs(dy * cos + dx * sin) - f.half.y, 0f);
                    dist = Mathf.Sqrt(qx * qx + qy * qy);
                }
                else
                {
                    dist = Mathf.Sqrt(dx * dx + dy * dy) - f.half.x;
                }
                dist = Mathf.Max(dist, 0f);
                int i = (x + w * y) * 2;
                if (dist * scale < foamBytes[i])
                {
                    foamBytes[i] = (byte)(int)(dist * scale);
                    foamBytes[i + 1] = (byte)which;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (foamMap == null)
            return;
        if (Application.isPlaying)
            Destroy(foamMap);
        else
            DestroyImmediate(foamMap);
    }
}
