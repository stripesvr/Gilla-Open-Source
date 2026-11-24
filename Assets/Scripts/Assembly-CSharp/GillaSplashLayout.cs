// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static partial class GillaSplashLayout
{
    public struct Kind
    {
        public string name;
        public string model;
        public float across;
        public float along;
        public bool safe;

        public Kind(string name, string model, float across, float along, bool safe = false)
        {
            this.name = name;
            this.model = model;
            this.across = across;
            this.along = along;
            this.safe = safe;
        }
    }

    public struct Spot
    {
        public int kind;
        public float x;
        public float z;
        public float yaw;
    }

    public struct Hall
    {
        public Rect water;
        public Vector3[] pillars;
    }

    private struct LayHallState
    {
        public Random rng;
        public string[] picks;
        public float safeShare;
        public float lo;
        public float hi;
        public Rect w;
        public Hall hall;
        public List<Vector2[]> shapes;
        public List<Spot> spots;
    }

    public static readonly Kind[] Kinds = new[] { new GillaSplashLayout.Kind("S20", "Gilla_TileSlab20x20", 2f, 2f), new GillaSplashLayout.Kind("S30", "Gilla_TileSlab30x30", 3f, 3f), new GillaSplashLayout.Kind("S40", "Gilla_TileSlab40x40", 4f, 4f), new GillaSplashLayout.Kind("S50", "Gilla_TileSlab50x50", 5f, 5f), new GillaSplashLayout.Kind("S60", "Gilla_TileSlab60x60", 6f, 6f), new GillaSplashLayout.Kind("S80", "Gilla_TileSlab80x80", 8f, 8f), new GillaSplashLayout.Kind("R35", "Gilla_TileSlab30x50", 3f, 5f), new GillaSplashLayout.Kind("R47", "Gilla_TileSlab40x70", 4f, 7f), new GillaSplashLayout.Kind("R38", "Gilla_TileSlab30x80", 3f, 8f), new GillaSplashLayout.Kind("R58", "Gilla_TileSlab50x80", 5f, 8f), new GillaSplashLayout.Kind("B6", "Gilla_TileSlab15x60", 1.5f, 6f), new GillaSplashLayout.Kind("B8", "Gilla_TileSlab15x80", 1.5f, 8f), new GillaSplashLayout.Kind("B10", "Gilla_TileSlab20x100", 2f, 10f), new GillaSplashLayout.Kind("B12", "Gilla_TileSlab15x120", 1.5f, 12f), new GillaSplashLayout.Kind("K30", "Gilla_TileRim30x30", 3f, 3f, true), new GillaSplashLayout.Kind("K40", "Gilla_TileRim40x40", 4f, 4f, true), new GillaSplashLayout.Kind("K50", "Gilla_TileRim50x50", 5f, 5f, true), new GillaSplashLayout.Kind("K60", "Gilla_TileRim60x60", 6f, 6f, true), new GillaSplashLayout.Kind("K38", "Gilla_TileRim30x80", 3f, 8f, true), new GillaSplashLayout.Kind("K58", "Gilla_TileRim50x80", 5f, 8f, true), new GillaSplashLayout.Kind("K80", "Gilla_TileRim80x80", 8f, 8f, true), new GillaSplashLayout.Kind("L316", "Gilla_TileSlab30x160", 3f, 16f), new GillaSplashLayout.Kind("L414", "Gilla_TileSlab40x140", 4f, 14f), new GillaSplashLayout.Kind("L516", "Gilla_TileSlab50x160", 5f, 16f), new GillaSplashLayout.Kind("L620", "Gilla_TileSlab60x200", 6f, 20f), new GillaSplashLayout.Kind("L820", "Gilla_TileSlab80x200", 8f, 20f), new GillaSplashLayout.Kind("KL414", "Gilla_TileRim40x140", 4f, 14f, true), new GillaSplashLayout.Kind("KL516", "Gilla_TileRim50x160", 5f, 16f, true) };
    public const int Styles = 3;
    public const int MostInAHall = 80;
    private static readonly string[][][] Picks = new string[][][] { new string[][] { new[] { "L620", "L516", "L414" }, new[] { "L516", "L414", "R58" }, new[] { "L414", "R58", "R47", "S60" }, new[] { "R58", "R47", "R38", "S50" }, new[] { "R47", "R38", "S50", "S40" }, new[] { "R38", "S40", "R35", "S30" }, new[] { "S40", "R35", "S30", "S20" } }, new string[][] { new[] { "L316", "L414", "B12" }, new[] { "L316", "B12", "R38" }, new[] { "B12", "R38", "B10" }, new[] { "B10", "B8", "R35" }, new[] { "B8", "R35", "B6" }, new[] { "B6", "R35", "S30" }, new[] { "B6", "S30", "S20" } }, new string[][] { new[] { "L820", "L620", "L516" }, new[] { "L620", "L516", "S80" }, new[] { "L516", "S80", "R58" }, new[] { "S80", "R58", "S60" }, new[] { "R58", "S60", "S50" }, new[] { "S60", "S50", "S40" }, new[] { "S50", "S40", "S30" } } };
    private const float Grid = 0.25f;
    private const float YawStep = 5.625f;

    public static float Hard(int round)
    {
        float t = (float)(round - 1) / 6f;
        float r = 0f;
        if (0f <= t)
        {
            r = t;
            if (1f < t) r = 1f;
        }
        return r;
    }

    public static ValueTuple<float, float> Gaps(int style, int round)
    {
        float hard = Hard(round);
        float lo;
        float hi;
        if (style == 1)
        {
            hi = 3f;
            lo = 2.2f;
        }
        else if (style == 2)
        {
            lo = 3f;
            hi = 4f;
        }
        else
        {
            lo = 2.4f;
            hi = 3.4f;
        }
        float t = Mathf.Clamp01(hard);
        float top = Mathf.Min(lo + 1.4f, 3.8f);
        return new ValueTuple<float, float>(lo + (top - lo) * t, hi + (4.6f - hi) * t);
    }

    public static int HowMany(int round)
    {
        float t = (float)(round - 1) / 6f;
        float f = 0f;
        if (0f <= t)
        {
            f = 18f;
            if (t <= 1f) f = t * 18f;
        }
        return (int)Math.Round((double)(46f - f));
    }

    public static float SafeShare(int round)
    {
        float r = 0f;
        if (1 < round)
        {
            float t = (float)(round - 1) / 6f;
            float f = 0f;
            if (0f <= t)
            {
                f = 0.13f;
                if (t <= 1f) f = t * 0.13f;
            }
            r = 0.25f - f;
        }
        return r;
    }

    public static List<Spot> Make(Hall[] halls, int style, int round, Random rng)
    {
        int r = round;
        if (round < 2) r = 1;
        int idx = (int)((uint)(r - 1) % 3);
        if (idx < halls.Length)
        {
            return MakeHall(halls[idx], style, round, rng);
        }
        return new List<Spot>();
    }

    public static List<Spot> MakeHall(Hall hall, int style, int round, Random rng)
    {
        if (1 < style) style = 2;
        int s = Mathf.Max(style, 0);
        string[][] forStyle = Picks[s];
        int i = Mathf.Min(round - 1, forStyle.Length - 1);
        if (round - 1 < 0) i = 0;
        return LayHall(hall, forStyle[i], s, round, rng);
    }

    private static int KindOf(string name)
    {
        for (int i = 0; i < Kinds.Length; i++)
        {
            if (Kinds[i].name == name) return i;
        }
        return 0;
    }

    private static int SafeLike(int kind)
    {
        int result = kind;
        float area = Kinds[kind].across * Kinds[kind].along;
        float best = float.MaxValue;
        for (int i = 0; i < Kinds.Length; i++)
        {
            if (Kinds[i].safe)
            {
                float d = Mathf.Abs(Kinds[i].across * Kinds[i].along - area);
                if (d < best)
                {
                    result = i;
                    best = d;
                }
            }
        }
        return result;
    }

    private static List<Spot> LayHall(Hall hall, string[] picks, int style, int round, Random rng)
    {
        LayHallState st = new LayHallState();
        st.rng = rng;
        st.picks = picks;
        st.w = hall.water;
        st.hall = hall;
        st.safeShare = SafeShare(round);
        int howMany = HowMany(round);
        ValueTuple<float, float> gaps = Gaps(style, round);
        st.lo = gaps.Item1;
        st.hi = gaps.Item2;
        Rect w = st.w;

        Vector2[] p = Corners(w.xMin + w.width * 0.5f, w.yMin - 5f, w.width, 10f, 0f);
        Vector2[] q = Corners(w.xMin + w.width * 0.5f, w.yMin + w.height + 5f, w.width, 10f, 0f);

        List<Spot> best = new List<Spot>();
        int attempt = 0;
        while (true)
        {
            st.spots = new List<Spot>();
            st.shapes = new List<Vector2[]>();

            int tries = howMany * 30;
            if (0 < tries)
            {
                do
                {
                    int count = st.spots.Count;
                    if (howMany <= count) break;
                    Vector2[] from;
                    Vector2 at;
                    float heading;
                    if (count == 0 || st.rng.NextDouble() < 0.1)
                    {
                        float x = Mathf.Lerp(w.xMin + 3f, w.xMin + w.width - 3f, (float)st.rng.NextDouble());
                        from = p;
                        at = new Vector2(x, w.yMin);
                        heading = 120f * (float)st.rng.NextDouble() - 60f;
                    }
                    else
                    {
                        int idx;
                        if (0.7 <= st.rng.NextDouble())
                            idx = st.rng.Next(count);
                        else
                            idx = st.rng.Next(Mathf.Max(count - 6, 0), count);
                        from = st.shapes[idx];
                        at = LayHall_Middle(idx, ref st);
                        heading = 360f * (float)st.rng.NextDouble();
                    }
                    LayHall_Place(from, at, heading, ref st);
                    tries--;
                } while (tries != 0);
            }

            for (int step = 0; step < 60; step++)
            {
                if (80 < st.spots.Count) break;
                bool[] reached = new bool[st.shapes.Count];
                Queue<int> queue = new Queue<int>();
                for (int i = 0; i < st.shapes.Count; i++)
                {
                    if (Apart(p, st.shapes[i]) <= st.hi + 0.01f)
                    {
                        reached[i] = true;
                        queue.Enqueue(i);
                    }
                }
                int far = -1;
                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue();
                    if (Apart(st.shapes[idx], q) <= st.hi + 0.01f)
                    {
                        return st.spots;
                    }
                    if (far < 0 || st.spots[far].z < st.spots[idx].z)
                    {
                        far = idx;
                    }
                    for (int j = 0; j < st.shapes.Count; j++)
                    {
                        if (!reached[j])
                        {
                            if (Apart(st.shapes[idx], st.shapes[j]) <= st.hi + 0.01f)
                            {
                                reached[j] = true;
                                queue.Enqueue(j);
                            }
                        }
                    }
                }
                if (79 < st.spots.Count) break;
                Vector2[] from2;
                Vector2 at2;
                float heading2;
                if (far < 0)
                {
                    float x = Mathf.Lerp(w.xMin + 3f, w.xMin + w.width - 3f, (float)st.rng.NextDouble());
                    from2 = p;
                    at2 = new Vector2(x, w.yMin);
                    heading2 = 80f * (float)st.rng.NextDouble() - 40f;
                }
                else
                {
                    from2 = st.shapes[far];
                    at2 = LayHall_Middle(far, ref st);
                    heading2 = 100f * (float)st.rng.NextDouble() - 50f;
                }
                LayHall_Place(from2, at2, heading2, ref st);
            }

            attempt++;
            List<Spot> keep = st.spots;
            if (st.spots.Count <= best.Count) keep = best;
            best = keep;
            if (attempt == 30)
            {
                Debug.LogWarning(string.Format("[GillaSplashLayout] no way across found in round {0}: a hall has only what it got", round));
                return best;
            }
        }
    }

    // <LayHall>g__R|17_0 (reconstructed)
    private static float LayHall_R(float a, float b, ref LayHallState st)
    {
        return a + (b - a) * (float)st.rng.NextDouble();
    }

    // <LayHall>g__Place|17_1 (reconstructed; body not recovered exactly) /* ? */
    private static bool LayHall_Place(Vector2[] from, Vector2 at, float heading, ref LayHallState st)
    {
        if (st.picks == null || st.picks.Length == 0) return false;
        int kind = KindOf(st.picks[st.rng.Next(st.picks.Length)]);
        if (st.rng.NextDouble() < st.safeShare) kind = SafeLike(kind);
        Kind k = Kinds[kind];

        float yaw = Mathf.Round(LayHall_R(0f, 360f, ref st) / YawStep) * YawStep;
        yaw = Mathf.Repeat(yaw, 360f);

        float gap = LayHall_R(st.lo, st.hi, ref st);
        float rad = heading * 0.017453292f;
        Vector2 dir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 c = at + dir * (gap + Mathf.Max(k.across, k.along) * 0.5f);
        c.x = Mathf.Round(c.x / Grid) * Grid;
        c.y = Mathf.Round(c.y / Grid) * Grid;

        if (!st.w.Contains(c)) return false;

        Vector2[] shape = Corners(c.x, c.y, k.across, k.along, yaw);
        float d = Apart(from, shape);
        if (d < st.lo || st.hi + 0.01f < d) return false;

        for (int i = 0; i < st.shapes.Count; i++)
        {
            if (Apart(st.shapes[i], shape) < st.lo) return false;
        }

        if (st.hall.pillars != null)
        {
            for (int i = 0; i < st.hall.pillars.Length; i++)
            {
                Vector3 pl = st.hall.pillars[i];
                if (PointApart(new Vector2(pl.x, pl.z), c, k.across, k.along, yaw) < 1f) return false;
            }
        }

        Spot s;
        s.kind = kind;
        s.x = c.x;
        s.z = c.y;
        s.yaw = yaw;
        st.spots.Add(s);
        st.shapes.Add(shape);
        return true;
    }

    // <LayHall>g__Middle|17_2 (reconstructed)
    private static Vector2 LayHall_Middle(int i, ref LayHallState st)
    {
        Spot s = st.spots[i];
        return new Vector2(s.x, s.z);
    }

    public static Vector2[] Corners(float x, float z, float across, float along, float yaw)
    {
        float rad = yaw * 0.017453292f;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        Vector2[] c = new Vector2[4];
        float ax = across * 0.5f * cos;
        float az = -(sin * across * 0.5f);
        float lx = along * 0.5f * sin;
        float lz = along * 0.5f * cos;
        c[0] = new Vector2(x - ax - lx, z - az - lz);
        c[1] = new Vector2(x + ax - lx, z + az - lz);
        c[2] = new Vector2(x + ax + lx, z + az + lz);
        c[3] = new Vector2(x - ax + lx, z - az + lz);
        return c;
    }

    private static float PointApart(Vector2 point, Vector2 middle, float across, float along, float yaw)
    {
        float dx = point.x - middle.x;
        float rad = yaw * 0.017453292f;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        float dy = point.y - middle.y;
        float a = Mathf.Abs(dx * cos - dy * sin) - across * 0.5f;
        float b = Mathf.Abs(dx * sin + dy * cos) - along * 0.5f;
        if (b <= 0f) b = 0f;
        if (a <= 0f) a = 0f;
        return Mathf.Sqrt(a * a + b * b);
    }

    public static float Apart(Vector2[] p, Vector2[] q)
    {
        if (!Separated(p, q))
        {
            if (!Separated(q, p))
            {
                return 0f;
            }
        }
        float a = Nearest(p, q);
        float b = Nearest(q, p);
        if (b <= a) a = b;
        return a;
    }

    private static bool Separated(Vector2[] a, Vector2[] b)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 axis = a[i + 1] - a[i];
            float minA = float.MaxValue;
            float maxA = float.MinValue;
            for (int k = 0; k < a.Length; k++)
            {
                float d = Vector2.Dot(axis, a[k]);
                if (d <= minA) minA = d;
                if (maxA <= d) maxA = d;
            }
            float minB = float.MaxValue;
            float maxB = float.MinValue;
            for (int k = 0; k < b.Length; k++)
            {
                float d = Vector2.Dot(axis, b[k]);
                if (d <= minB) minB = d;
                if (maxB <= d) maxB = d;
            }
            if (maxA < minB || maxB < minA)
            {
                return true;
            }
        }
        return false;
    }

    private static float Nearest(Vector2[] points, Vector2[] edges)
    {
        float best = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 pt = points[i];
            for (int e = 0; e < 4; e++)
            {
                Vector2 a = edges[e];
                Vector2 ab = edges[(e + 1) & 3] - a;
                float t = Mathf.Clamp01(Vector2.Dot(pt - a, ab) / ab.sqrMagnitude);
                float d = ((a + ab * t) - pt).magnitude;
                if (d <= best) best = d;
            }
        }
        return best;
    }

    public static int Pack(in Spot s)
    {
        int x = (int)Math.Round((double)((s.x + 64f) * 4f));
        int z = (int)Math.Round((double)(s.z * 4f));
        int yaw = (int)Math.Round((double)(s.yaw / 5.625f));
        return (x & 0x1ff) | ((z & 0x7ff) << 9) | ((s.kind & 0x1f) << 20) | ((yaw & 0x3f) << 25);
    }

    public static Spot Unpack(int v)
    {
        uint u = (uint)v;
        Spot s;
        s.kind = (int)((u >> 20) & 0x1f);
        s.x = (float)(v & 0x1ff) / 4f - 64f;
        s.z = (float)((u >> 9) & 0x7ff) / 4f;
        s.yaw = (float)((u >> 25) & 0x3f) * 5.625f;
        return s;
    }
}
