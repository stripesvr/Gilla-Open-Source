// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class GillaStains
{
    private static void Scratched(Job j, System.Random rng)
    {
        Vector3 mid = (j.point + j.to) * 0.5f;
        Piece p = null;
        float best = float.MaxValue;
        foreach (Piece piece in j.pieces)
        {
            int[] fur = piece.shape.fur;
            Vector3[] at = piece.shape.at;
            for (int i = 0; i < fur.Length; i++)
            {
                float d2 = (at[fur[i]] - mid).sqrMagnitude;
                if (d2 < best)
                {
                    best = d2;
                    p = piece;
                }
            }
        }

        float allowed = (j.to - j.point).magnitude * 0.5f + j.big * 0.12f;
        if (p != null && best <= allowed * allowed)
        {
            Vector3 prev = j.point;
            Vector3 end = j.to;
            Vector3 prevFaces;
            Vector3 endFaces;
            Snap(p, ref prev, out prevFaces);
            Snap(p, ref end, out endFaces);
            int steps = (int)((end - prev).magnitude / (j.radius * 3f));
            if (steps > 15) steps = 16;
            if (steps < 2) steps = 1;
            float r = j.radius * ((float)rng.NextDouble() * 0.3f + 0.8f);
            for (int i = 0; i < steps; i++)
            {
                float t = Mathf.Clamp01((float)(i + 1) / (float)steps);
                Vector3 here = Vector3.Lerp(j.point, j.to, t);
                Vector3 hereFaces;
                Snap(p, ref here, out hereFaces);
                float r2 = Mathf.Clamp(r * ((float)rng.NextDouble() * 0.40000004f + 0.8f), j.radius * 0.6f, j.radius * 1.3f);
                Add(prev, here, r, r2, (prevFaces + hereFaces).normalized, 0);
                if (rng.NextDouble() < 0.22)
                {
                    Add(here, here, r2 * 1.5f, r2 * 1.5f, hereFaces, 0);
                }
                prev = here;
                prevFaces = hereFaces;
                r = r2;
            }
        }
    }

    private static void Landed(Job j, System.Random rng)
    {
        Vector3 pos = j.point;
        Vector3 normal = Vector3.up;
        Piece p = null;
        float best = float.MaxValue;
        foreach (Piece piece in j.pieces)
        {
            int[] fur = piece.shape.fur;
            Vector3[] at = piece.shape.at;
            for (int i = 0; i < fur.Length; i++)
            {
                int v = fur[i];
                float d2 = (at[v] - j.point).sqrMagnitude;
                if (d2 < best)
                {
                    normal = piece.shape.faces[v];
                    p = piece;
                    pos = at[v];
                    best = d2;
                }
            }
        }

        if (p == null) return;
        float reach = j.radius + j.big * 0.35f;
        if (reach * reach < best) return;

        Vector3 n = normal.normalized;
        float R = j.radius * 1.2f;
        if (j.stuff == GillaSplatter.Stuff.Blood)
        {
            Lobed(p, pos, n, R, 0, rng);
            Thrown(p, pos, n, R, rng.Next(6, 13), 0, j.big, rng);
            int count = rng.Next(1, 4);
            if (count < 1) return;
            do
            {
                Vector3 side = Sideways(n, rng);
                float d = R * ((float)rng.NextDouble() * 0.40000004f + 0.2f);
                Vector3 s = pos + side * d;
                Vector3 sn;
                Snap(p, ref s, out sn);
                float r = Mathf.Max(R * ((float)rng.NextDouble() * 0.099999994f + 0.14f), j.big * 0.011f);
                float len = Mathf.Max(R * 0.4f, j.big * 0.02f);
                Runs(p, s, sn, r, len, rng.Next(3, 8), 0, rng);
                count--;
            } while (count != 0);
            return;
        }
        else
        {
            Lobed(p, pos, n, R * 1.12f, 2, rng);
            Vector3 side = Sideways(n, rng);
            Vector3 yolk = pos + side * (R * ((float)rng.NextDouble() * 0.14f + 0f));
            Vector3 yolkFaces;
            Snap(p, ref yolk, out yolkFaces);
            Lobed(p, yolk, yolkFaces, R * 0.62f, 1, rng);
            Thrown(p, pos, n, R, rng.Next(3, 7), 2, j.big, rng);
            Thrown(p, yolk, yolkFaces, R * 0.8f, rng.Next(2, 5), 1, j.big, rng);
            int count = rng.Next(2, 4);
            while (count > 0)
            {
                Vector3 sd = Sideways(n, rng);
                float d = R * ((float)rng.NextDouble() * 0.5f + 0.3f);
                Vector3 s = pos + sd * d;
                Vector3 sn;
                Snap(p, ref s, out sn);
                float r = Mathf.Max(R * 0.14f, j.big * 0.011f);
                float len = Mathf.Max(R * 0.4f, j.big * 0.022f);
                Runs(p, s, sn, r, len, rng.Next(3, 7), 2, rng);
                count--;
            }
            count = rng.Next(1, 3);
            if (count < 1) return;
            do
            {
                Vector3 sd = Sideways(yolkFaces, rng);
                float d = R * ((float)rng.NextDouble() * 0.25f + 0.15f);
                Vector3 s = yolk + sd * d;
                Vector3 sn;
                Snap(p, ref s, out sn);
                float r = Mathf.Max(R * 0.09f, j.big * 0.01f);
                float len = Mathf.Max(R * 0.35f, j.big * 0.02f);
                Runs(p, s, sn, r, len, rng.Next(2, 6), 1, rng);
                count--;
            } while (count != 0);
        }
    }

    private static void Thrown(Job j, System.Random rng)
    {
        int total = 0;
        foreach (Piece piece in j.pieces)
        {
            total += piece.shape.tris.Length / 3;
        }
        if (upTo.Length < total)
        {
            upTo = new float[total];
            upToPiece = new int[total];
            upToTri = new int[total];
        }

        float reach = j.reach;
        float best = float.MaxValue;
        float all = 0f;
        int many = 0;
        for (int pi = 0; pi < j.pieces.Count; pi++)
        {
            Piece piece = j.pieces[pi];
            int[] tris = piece.shape.tris;
            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 a = piece.at[tris[t]];
                Vector3 b = piece.at[tris[t + 1]];
                Vector3 c = piece.at[tris[t + 2]];
                Vector3 d = j.point - (a + b + c) * 0.33333334f;
                float d2 = d.sqrMagnitude;
                if (reach * reach < d2) continue;
                Vector3 nsum = piece.faces[tris[t]] + piece.faces[tris[t + 1]] + piece.faces[tris[t + 2]];
                float facing = Vector3.Dot(d, nsum);
                if (facing <= 0f) continue;
                facing = facing / Mathf.Sqrt(d2 * nsum.sqrMagnitude);
                if (facing < 0.08f) continue;
                float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                all += area * facing / (d2 + 0.03f);
                upTo[many] = all;
                upToPiece[many] = pi;
                upToTri[many] = t;
                many++;
                if (d2 < best) best = d2;
            }
        }

        if (many == 0) return;

        float close = Mathf.Clamp01(1f - Mathf.Sqrt(best) / j.reach);
        float strength = Mathf.Min(j.strength, 1.5f);
        if (rng.NextDouble() < (double)(close + 0.25f))
        {
            int blobs = Mathf.RoundToInt(close * close * 3.2f * strength);
            for (int k = 0; k <= blobs; k++)
            {
                Piece p;
                Vector3 at;
                Vector3 faces;
                Landing(j, rng, many, all, out p, out at, out faces);
                float size = j.big * (Mathf.Clamp01(close * ((float)rng.NextDouble() * 0.8f + 0.2f)) * 0.038f + 0.024f);
                Lobed(p, at, faces, size, 0, rng);
                Thrown(p, at, faces, size, rng.Next(4, 11), 0, j.big, rng);
                if (j.big * 0.032f < size && rng.NextDouble() < 0.75)
                {
                    float r = Mathf.Max(size * ((float)rng.NextDouble() * 0.11999999f + 0.14f), j.big * 0.011f);
                    float len = Mathf.Max(size * 0.55f, j.big * 0.022f);
                    Runs(p, at, faces, r, len, rng.Next(2, 8), 0, rng);
                }
            }
        }

        int drops = Mathf.RoundToInt(strength * (Mathf.Clamp01(close) * 92f + 18f));
        for (int k = 0; k < drops; k++)
        {
            Piece p;
            Vector3 at;
            Vector3 faces;
            Landing(j, rng, many, all, out p, out at, out faces);
            double roll = rng.NextDouble();
            float spread;
            float baseSize;
            if (roll < 0.1)
            {
                spread = 0.012f;
                baseSize = 0.016f;
            }
            else
            {
                spread = roll < 0.42 ? 0.0055f : 0.0033f;
                baseSize = roll < 0.42 ? 0.0095f : 0.0062f;
            }
            float size = (spread * (float)rng.NextDouble() + baseSize) * j.big;
            Vector3 away = (at - j.point).normalized;
            float dot = Vector3.Dot(away, faces);
            float lift = 0.12f;
            if (dot <= -0.12f) lift = -dot;
            Vector3 dir = away + faces * lift + Vector3.down * 0.25f + Sideways(faces, rng) * 0.35f;
            dir = dir - faces * Vector3.Dot(faces, dir);
            float stretch = 1f / lift - 1f + (float)rng.NextDouble() * 1.5f + 0.3f;
            stretch = Mathf.Min(stretch, 4.5f);
            Vector3 end = at;
            if (dir.sqrMagnitude > 1e-6f)
            {
                end = at + dir.normalized * (size * stretch);
            }
            Add(at, end, size, size * 0.42f, faces, 0);
        }
    }

    private static void Landing(Job j, System.Random rng, int many, float all, out Piece p, out Vector3 at, out Vector3 faces)
    {
        int k = Array.BinarySearch(upTo, 0, many, (float)rng.NextDouble() * all);
        if (k < 0) k = ~k;
        if (many - 1 <= k) k = many - 1;
        p = j.pieces[upToPiece[k]];
        int tri = upToTri[k];
        int[] tris = p.shape.tris;
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }
        float w = 1f - u - v;
        at = p.at[tris[tri]] * u + p.at[tris[tri + 1]] * v + p.at[tris[tri + 2]] * w;
        faces = (p.faces[tris[tri]] * u + p.faces[tris[tri + 1]] * v + p.faces[tris[tri + 2]] * w).normalized;
    }

    private static void Draw(Job j)
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        for (int i = 0; i < blotCount; i++)
        {
            Vector4 ball = blotBalls[i];
            Vector3 c = new Vector3(ball.x, ball.y, ball.z);
            min = Vector3.Min(min, c - Vector3.one * ball.w);
            max = Vector3.Max(max, c + Vector3.one * ball.w);
        }

        float most = 0f;
        foreach (Piece piece in j.pieces)
        {
            int[] tris = piece.shape.tris;
            for (int t = 0; t + 2 < tris.Length + 2 && t < tris.Length; t += 3)
            {
                Vector3 a = piece.at[tris[t]];
                Vector3 b = piece.at[tris[t + 1]];
                Vector3 c = piece.at[tris[t + 2]];
                Vector3 mid = (a + b + c) * 0.33333334f;
                float r = Mathf.Max((a - mid).sqrMagnitude, Mathf.Max((b - mid).sqrMagnitude, (c - mid).sqrMagnitude));
                most = Mathf.Max(r, most);
            }
        }
        float triReach = Mathf.Sqrt(most);
        Vector3 extent = (max - min) + Vector3.one * (triReach + triReach);
        float largest = Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z));
        float square = Mathf.Max(0.05f, largest / 13.999f);
        Vector3 origin = min - Vector3.one * triReach;

        foreach (int used in usedSquares)
        {
            squares[used].Clear();
        }
        usedSquares.Clear();

        for (int i = 0; i < blotCount; i++)
        {
            Vector4 ball = blotBalls[i];
            float r = triReach + ball.w;
            int z0 = Square(ball.z - r, origin.z, square);
            int z1 = Square(ball.z + r, origin.z, square);
            int y0 = Square(ball.y - r, origin.y, square);
            int y1 = Square(ball.y + r, origin.y, square);
            int x0 = Square(ball.x - r, origin.x, square);
            int x1 = Square(ball.x + r, origin.x, square);
            for (int z = z0; z <= z1; z++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int item = x + y * Across + z * Across * Across;
                        List<int> list = squares[item];
                        if (list == null)
                        {
                            list = new List<int>(8);
                            squares[item] = list;
                        }
                        if (list.Count == 0)
                        {
                            usedSquares.Add(item);
                        }
                        list.Add(i);
                    }
                }
            }
        }

        byte[] px = j.sheet.pixels;
        foreach (Piece piece in j.pieces)
        {
            int[] tris = piece.shape.tris;
            Vector2[] uv = piece.shape.uv;
            Vector3[] at = piece.at;
            Vector3[] nrm = piece.faces;
            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t];
                int i1 = tris[t + 1];
                int i2 = tris[t + 2];
                Vector3 a = at[i0];
                Vector3 b = at[i1];
                Vector3 c = at[i2];
                if (Mathf.Max(a.x, Mathf.Max(b.x, c.x)) < min.x) continue;
                if (Mathf.Min(a.x, Mathf.Min(b.x, c.x)) > max.x) continue;
                if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < min.y) continue;
                if (Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > max.y) continue;
                if (Mathf.Max(a.z, Mathf.Max(b.z, c.z)) < min.z) continue;
                if (Mathf.Min(a.z, Mathf.Min(b.z, c.z)) > max.z) continue;

                Vector3 mid = (a + b + c) * 0.33333334f;
                float r2 = Mathf.Max(Mathf.Max((b - mid).sqrMagnitude, (c - mid).sqrMagnitude), (a - mid).sqrMagnitude);
                int cell = Square(mid.x, origin.x, square) + (Square(mid.y, origin.y, square) + Square(mid.z, origin.z, square) * Across) * Across;
                List<int> list = squares[cell];
                if (list != null && list.Count != 0)
                {
                    nearBlots.Clear();
                    for (int k = 0; k < list.Count; k++)
                    {
                        int idx = list[k];
                        Vector4 ball = blotBalls[idx];
                        float reachBall = Mathf.Sqrt(r2) + ball.w;
                        if ((new Vector3(ball.x, ball.y, ball.z) - mid).sqrMagnitude < reachBall * reachBall)
                        {
                            nearBlots.Add(idx);
                        }
                    }
                    if (nearBlots.Count != 0)
                    {
                        Rasterise(j, px, uv[i0], uv[i1], uv[i2], a, b, c, nrm[i0], nrm[i1], nrm[i2]);
                    }
                }
            }
        }
    }

    private static int Square(float at, float from, float square)
    {
        int i = (int)((at - from) / square);
        if (i > 12) i = 13;
        return Mathf.Max(i, 0);
    }
}
