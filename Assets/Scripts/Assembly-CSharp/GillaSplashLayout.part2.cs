// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class GillaSplashLayout
{
    static GillaSplashLayout()
    {
        Kinds = new Kind[]
        {
            new Kind { name = "S20", model = "Gilla_TileSlab20x20", across = 2f, along = 2f, safe = false },
            new Kind { name = "S30", model = "Gilla_TileSlab30x30", across = 3f, along = 3f, safe = false },
            new Kind { name = "S40", model = "Gilla_TileSlab40x40", across = 4f, along = 4f, safe = false },
            new Kind { name = "S50", model = "Gilla_TileSlab50x50", across = 5f, along = 5f, safe = false },
            new Kind { name = "S60", model = "Gilla_TileSlab60x60", across = 6f, along = 6f, safe = false },
            new Kind { name = "S80", model = "Gilla_TileSlab80x80", across = 8f, along = 8f, safe = false },
            new Kind { name = "R35", model = "Gilla_TileSlab30x50", across = 3f, along = 5f, safe = false },
            new Kind { name = "R47", model = "Gilla_TileSlab40x70", across = 4f, along = 7f, safe = false },
            new Kind { name = "R38", model = "Gilla_TileSlab30x80", across = 3f, along = 8f, safe = false },
            new Kind { name = "R58", model = "Gilla_TileSlab50x80", across = 5f, along = 8f, safe = false },
            new Kind { name = "B6", model = "Gilla_TileSlab15x60", across = 1.5f, along = 6f, safe = false },
            new Kind { name = "B8", model = "Gilla_TileSlab15x80", across = 1.5f, along = 8f, safe = false },
            new Kind { name = "B10", model = "Gilla_TileSlab20x100", across = 2f, along = 10f, safe = false },
            new Kind { name = "B12", model = "Gilla_TileSlab15x120", across = 1.5f, along = 12f, safe = false },
            new Kind { name = "K30", model = "Gilla_TileRim30x30", across = 3f, along = 3f, safe = true },
            new Kind { name = "K40", model = "Gilla_TileRim40x40", across = 4f, along = 4f, safe = true },
            new Kind { name = "K50", model = "Gilla_TileRim50x50", across = 5f, along = 5f, safe = true },
            new Kind { name = "K60", model = "Gilla_TileRim60x60", across = 6f, along = 6f, safe = true },
            new Kind { name = "K38", model = "Gilla_TileRim30x80", across = 3f, along = 8f, safe = true },
            new Kind { name = "K58", model = "Gilla_TileRim50x80", across = 5f, along = 8f, safe = true },
            new Kind { name = "K80", model = "Gilla_TileRim80x80", across = 8f, along = 8f, safe = true },
            new Kind { name = "L316", model = "Gilla_TileSlab30x160", across = 3f, along = 16f, safe = false },
            new Kind { name = "L414", model = "Gilla_TileSlab40x140", across = 4f, along = 14f, safe = false },
            new Kind { name = "L516", model = "Gilla_TileSlab50x160", across = 5f, along = 16f, safe = false },
            new Kind { name = "L620", model = "Gilla_TileSlab60x200", across = 6f, along = 20f, safe = false },
            new Kind { name = "L820", model = "Gilla_TileSlab80x200", across = 8f, along = 20f, safe = false },
            new Kind { name = "KL414", model = "Gilla_TileRim40x140", across = 4f, along = 14f, safe = true },
            new Kind { name = "KL516", model = "Gilla_TileRim50x160", across = 5f, along = 16f, safe = true },
        };

        Picks = new string[][][]
        {
            new string[][]
            {
                new string[] { "L620", "L516", "L414" },
                new string[] { "L516", "L414", "R58" },
                new string[] { "L414", "R58", "R47", "S60" },
                new string[] { "R58", "R47", "R38", "S50" },
                new string[] { "R47", "R38", "S50", "S40" },
                new string[] { "R38", "S40", "R35", "S30" },
                new string[] { "S40", "R35", "S30", "S20" },
            },
            new string[][]
            {
                new string[] { "L316", "L414", "B12" },
                new string[] { "L316", "B12", "R38" },
                new string[] { "B12", "R38", "B10" },
                new string[] { "B10", "B8", "R35" },
                new string[] { "B8", "R35", "B6" },
                new string[] { "B6", "R35", "S30" },
                new string[] { "B6", "S30", "S20" },
            },
            new string[][]
            {
                new string[] { "L820", "L620", "L516" },
                new string[] { "L620", "L516", "S80" },
                new string[] { "L516", "S80", "R58" },
                new string[] { "S80", "R58", "S60" },
                new string[] { "R58", "S60", "S50" },
                new string[] { "S60", "S50", "S40" },
                new string[] { "S50", "S40", "S30" },
            },
        };
    }

    private static float LayHall_g__R_17_0(float lo, float hi, System.Random rng)
    {
        return (hi - lo) * (float)rng.NextDouble() + lo;
    }

    private static bool LayHall_g__Place_17_1(float x, float z, float dirDeg, Vector2[] own,
        System.Random rng, string[] picks, float safeShare, float lo, float hi, Rect w, Hall hall,
        List<Vector2[]> shapes, List<Spot> spots)
    {
        int kind = KindOf(picks[rng.Next(picks.Length)]);
        if (rng.NextDouble() < (double)safeShare)
        {
            kind = SafeLike(kind);
        }
        float across = Kinds[kind].across;
        float along = Kinds[kind].along;
        float yaw = Mathf.Round(((float)rng.NextDouble() * 360f + 0f) / YawStep) * YawStep;
        float gap = lo + (hi - lo) * (float)rng.NextDouble();

        float rad = dirDeg * 0.017453292f;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        float a = 0f;
        float b = 34f;
        for (int i = 0; i < 14; i++)
        {
            float mid = (b + a) * 0.5f;
            Vector2[] test = Corners(x + sin * mid, z + cos * mid, across, along, yaw);
            float apart = Apart(own, test);
            if (gap <= apart)
            {
                b = mid;
            }
            else
            {
                a = mid;
            }
        }

        float px = Mathf.Round((x + sin * b) * 4f) * Grid;
        float pz = Mathf.Round((z + cos * b) * 4f) * Grid;
        Vector2[] corners = Corners(px, pz, across, along, yaw);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 c = corners[i];
            if (c.x < w.x + 1f) return false;
            if (w.xMax - 1f < c.x) return false;
            if (c.y < w.y + 1.5f) return false;
            if (w.yMax - 1.5f < c.y) return false;
        }

        Vector3[] pillars = hall.pillars;
        for (int i = 0; i < pillars.Length; i++)
        {
            Vector3 p = pillars[i];
            float d = PointApart(new Vector2(p.x, p.y), new Vector2(px, pz), across, along, yaw);
            if (d < p.z + 1.2f) return false;
        }

        if (hi < Apart(own, corners)) return false;

        foreach (Vector2[] q in shapes)
        {
            if (q == own) continue;
            if (Apart(corners, q) < lo * 0.8f) return false;
        }

        spots.Add(new Spot { kind = kind, x = px, z = pz, yaw = yaw });
        shapes.Add(corners);
        return true;
    }

    private static Vector2 LayHall_g__Middle_17_2(int i, List<Spot> spots)
    {
        return new Vector2(spots[i].x, spots[i].z);
    }
}
