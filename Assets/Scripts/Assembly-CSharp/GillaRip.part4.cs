// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class GillaRip
{
    private static void Bowl(Shape s, List<ValueTuple<int, int>> rim, Source source, Half half, int sign, int seed, float handed)
    {
        List<List<int>> loops = Loops(s, rim, half);
        half.loops = loops.Count;
        if (loops.Count == 0)
        {
            return;
        }
        loops.Sort((a, b) => Bowl_b__0(s, a, b));

        Shape bowl = new Shape();
        bowl.tris = new List<int>[] { new List<int>() };
        List<ValueTuple<Vector2, Vector2>> edges = new List<ValueTuple<Vector2, Vector2>>();
        List<List<int>> kept = new List<List<int>>();
        float back = (float)(-sign);

        foreach (List<int> loop in loops)
        {
            Vector3 first = s.at[loop[0]];
            bool inside = false;
            foreach (List<int> other in kept)
            {
                inside |= Inside(s, other, YZ(first));
            }
            if (inside)
            {
                half.covered = half.covered + 1;
                continue;
            }
            float area = Area(s, loop);
            if (Mathf.Abs(area) < 1e-05f)
            {
                continue;
            }
            kept.Add(loop);
            int start = bowl.at.Count;
            foreach (int index in loop)
            {
                bowl.at.Add(s.at[index]);
                bowl.faces.Add(Vector3.right * back);
                bowl.uv.Add(Vector2.zero);
                bowl.weights.Add(s.weights[index]);
            }
            for (int i = 0; i < loop.Count; )
            {
                Vector3 a = bowl.at[start + i];
                i++;
                Vector3 b = bowl.at[start + i % loop.Count];
                edges.Add(new ValueTuple<Vector2, Vector2>(YZ(a), YZ(b)));
            }
            EarClip(bowl, start, loop.Count);
        }

        Flip(bowl);
        bowl.Refine((a, b) => Bowl_b__33_1(a, b), 6);
        Flip(bowl);

        Dictionary<Vector2Int, float> limits = new Dictionary<Vector2Int, float>();
        foreach (List<int> tri in s.tris)
        {
            for (int i = 0; i < tri.Count; i += 3)
            {
                Vector3 a = s.at[tri[i]];
                Vector3 b = s.at[tri[i + 1]];
                Vector3 c = s.at[tri[i + 2]];
                float depth = Mathf.Max(0f, Mathf.Min(Into(a, sign, seed), Mathf.Min(Into(b, sign, seed), Into(c, sign, seed))));
                if (depth <= 0.12f)
                {
                    Vector2 lo = new Vector2(Mathf.Min(a.y, Mathf.Min(b.y, c.y)), Mathf.Min(a.z, Mathf.Min(b.z, c.z)));
                    Vector2 hi = new Vector2(Mathf.Max(a.y, Mathf.Max(b.y, c.y)), Mathf.Max(a.z, Mathf.Max(b.z, c.z)));
                    Vector2Int from = SquareOf(lo);
                    Vector2Int to = SquareOf(hi);
                    for (int x = from.x; x <= to.x; x++)
                    {
                        for (int y = from.y; y <= to.y; y++)
                        {
                            Vector2Int key = new Vector2Int(x, y);
                            float existing;
                            if (!limits.TryGetValue(key, out existing) || depth < existing)
                            {
                                limits[key] = depth;
                            }
                        }
                    }
                }
            }
        }

        bool isHead = source.name.EndsWith("Head");
        float[] depths = new float[bowl.at.Count];
        for (int i = 0; i < bowl.at.Count; i++)
        {
            Vector3 p = bowl.at[i];
            float nearest = float.MaxValue;
            foreach (ValueTuple<Vector2, Vector2> edge in edges)
            {
                float d = ToSegment(YZ(p), edge.Item1, edge.Item2);
                if (d <= nearest)
                {
                    nearest = d;
                }
            }
            if (1e-05f <= nearest)
            {
                float fade = Mathf.Exp(nearest / -0.012f);
                Vector3 lumpAt = new Vector3(p.y / 0.028f, p.z / 0.028f, (seed * 1.7f) / 0.028f);
                float lumps = Mathf.PerlinNoise(lumpAt.x + lumpAt.z, lumpAt.y - lumpAt.z) - 0.5f /* ? GillaWounds.Lumps is inaccessible */;
                float sink = (1f - fade) * Deep * (lumps + 0.5f);
                float limit;
                bool has = limits.TryGetValue(SquareOf(YZ(p)), out limit);
                float d = Mathf.Min(sink, limit * 0.75f);
                if (!has)
                {
                    d = sink;
                }
                d = Mathf.Min(Mathf.Max(d, 0f), Deep);
                depths[i] = d / Deep;
                bowl.at[i] = new Vector3(d * sign + Line(p.y, p.z, seed), p.y, p.z);
            }
        }

        Vector3 origin = bowl.at[0];
        Vector2 centre = YZ(origin);
        Vector2 extent = YZ(Vector3.zero) * 0.5f;
        foreach (Vector3 p in bowl.at)
        {
            Vector2 lo = Vector2.Min(centre - extent, YZ(p));
            Vector2 hi = Vector2.Max(centre + extent, YZ(p));
            extent = (hi - lo) * 0.5f;
            centre = lo + extent;
        }
        half.middle = new Vector3(0f, centre.x, centre.y);
        half.across = Mathf.Max(extent.x, extent.y);

        Vector3 facing = Vector3.right * back;
        List<int> tris = bowl.tris[0];
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = bowl.at[tris[i]];
            Vector3 b = bowl.at[tris[i + 1]];
            Vector3 c = bowl.at[tris[i + 2]];
            if (Vector3.Dot(facing, Vector3.Cross(b - a, c - a)) * handed < 0f)
            {
                int t1 = tris[i + 1];
                int t2 = tris[i + 2];
                tris[i + 1] = t2;
                tris[i + 2] = t1;
            }
        }

        float headShift = isHead ? 0.3f : 0f;
        List<Vector2> uv = new List<Vector2>(bowl.at.Count);
        List<Vector4> second = new List<Vector4>(bowl.at.Count);
        List<Vector2> thirdShort = new List<Vector2>(bowl.at.Count);
        List<Vector3> fourth = new List<Vector3>(bowl.at.Count);
        Vector2 tag = new Vector2((sign > 0 ? 0.11f : 0.61f) + seed * 0.37f, seed * 0.73f + headShift);
        for (int i = 0; i < bowl.at.Count; i++)
        {
            uv.Add(tag);
            second.Add(new Vector4(half.middle.x, half.middle.y, half.middle.z, half.across));
            thirdShort.Add(new Vector2(depths[i] * 0.34f, 1f));
            fourth.Add(facing);
        }

        Built built = new Built();
        built.name = source.name + "_Inside";
        built.at = bowl.at;
        built.uv = uv;
        built.second = second;
        built.thirdShort = thirdShort;
        built.fourth = fourth;
        built.weights = bowl.weights.ToArray();
        built.binds = source.binds;
        built.tris = bowl.tris;
        built.workOutNormals = true;
        built.flipNormals = handed;
        half.builtInside = built;

        static Vector2Int SquareOf(Vector2 p)
        {
            return new Vector2Int(Mathf.FloorToInt(p.x / Band), Mathf.FloorToInt(p.y / Band)) /* ? */;
        }
    }

    private static Vector2 YZ(Vector3 p)
    {
        return new Vector2(p.y, p.z);
    }

    private static float ToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len = ab.x * ab.x + ab.y * ab.y;
        float t = 0f;
        if (1e-12f < len)
        {
            t = Math.Clamp(((p.x - a.x) * ab.x + (p.y - a.y) * ab.y) / len, 0f, 1f);
        }
        float dy = p.y - (a.y + ab.y * t);
        float dx = p.x - (a.x + ab.x * t);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static List<List<int>> Loops(Shape s, List<ValueTuple<int, int>> rim, Half half)
    {
        Dictionary<Vector3Int, List<int>> cells = new Dictionary<Vector3Int, List<int>>();
        Dictionary<int, List<int>> onward = new Dictionary<int, List<int>>();
        HashSet<long> seen = new HashSet<long>();

        int Point(int index)
        {
            Vector3 p = s.at[index];
            Vector3Int cell = new Vector3Int(Mathf.FloorToInt(p.x / 1e-4f), Mathf.FloorToInt(p.y / 1e-4f), Mathf.FloorToInt(p.z / 1e-4f)) /* ? */;
            List<int> list;
            if (!cells.TryGetValue(cell, out list))
            {
                list = new List<int>();
                cells[cell] = list;
            }
            foreach (int other in list)
            {
                if ((s.at[other] - p).sqrMagnitude < 1e-10f)
                {
                    return other;
                }
            }
            list.Add(index);
            return index; /* ? */
        }

        void TakeEdge(int a, int b)
        {
            onward[a].Remove(b);
            onward[b].Remove(a); /* ? */
        }

        foreach (ValueTuple<int, int> edge in rim)
        {
            int a = Point(edge.Item1);
            int b = Point(edge.Item2);
            if (a == b)
            {
                continue;
            }
            long key = a <= b ? (((long)b << 32) | (uint)a) : (((long)a << 32) | (uint)b);
            if (!seen.Add(key))
            {
                continue;
            }
            List<int> fromA;
            if (!onward.TryGetValue(a, out fromA))
            {
                fromA = new List<int>(2);
                onward[a] = fromA;
            }
            fromA.Add(b);
            List<int> fromB;
            if (!onward.TryGetValue(b, out fromB))
            {
                fromB = new List<int>(2);
                onward[b] = fromB;
            }
            fromB.Add(a);
        }

        List<List<int>> closed = new List<List<int>>();
        List<List<int>> open = new List<List<int>>();
        List<int> keys = new List<int>(onward.Keys);
        keys.Sort((x, y) => Loops_b__2(onward, x, y));
        foreach (int start in keys)
        {
            while (onward[start].Count > 0)
            {
                List<int> path = new List<int>();
                path.Add(start);
                Dictionary<int, int> where = new Dictionary<int, int>();
                where.Add(start, 0);
                int at = start;
                while (onward[at].Count > 0)
                {
                    List<int> nexts = onward[at];
                    int next = nexts[onward[at].Count - 1];
                    TakeEdge(at, next);
                    int found;
                    if (where.TryGetValue(next, out found))
                    {
                        List<int> cycle = path.GetRange(found, path.Count - found);
                        if (cycle.Count > 2)
                        {
                            closed.Add(cycle);
                        }
                        for (int k = found + 1; k < path.Count; k++)
                        {
                            where.Remove(path[k]);
                        }
                        path.RemoveRange(found + 1, path.Count - found - 1);
                    }
                    else
                    {
                        where[next] = path.Count;
                        path.Add(next);
                    }
                    at = next;
                }
                if (path.Count > 1)
                {
                    open.Add(path);
                }
            }
        }

        half.open = open.Count;
        while (open.Count > 0)
        {
            List<int> piece = open[open.Count - 1];
            open.RemoveAt(open.Count - 1);
            Vector3 end = s.at[piece[piece.Count - 1]];
            Vector3 begin = s.at[piece[0]];
            int best = -1;
            if (open.Count >= 1)
            {
                float bestDist = (end - begin).sqrMagnitude;
                for (int i = 0; i < open.Count; i++)
                {
                    Vector3 other = s.at[open[i][0]];
                    float d = (end - other).sqrMagnitude;
                    if (d < bestDist)
                    {
                        best = i;
                        bestDist = d;
                    }
                }
            }
            if (best >= 0)
            {
                piece.AddRange(open[best]);
                open[best] = piece;
            }
            else if (piece.Count > 2)
            {
                closed.Add(piece);
            }
        }
        return closed;
    }
}
