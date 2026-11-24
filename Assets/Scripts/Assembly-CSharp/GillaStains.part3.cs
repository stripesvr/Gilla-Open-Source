// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

public static partial class GillaStains
{
    static GillaStains()
    {
        sheets = new List<Sheet>();
        spare = new Stack<Sheet>();
        ofMap = new Dictionary<Texture, Sheet>();
        waiting = new Queue<Job>();
        shapes = new Dictionary<string, Shape>();
        shapeNames = new Dictionary<Mesh, string>();
        queue = Task.CompletedTask;
        NoiseId = Shader.PropertyToID("_StainNoise");
        SendMark = new ProfilerMarker("Gilla.Stains.Send");
        ReadMark = new ProfilerMarker("Gilla.Stains.Read");
        PaintMark = new ProfilerMarker("Gilla.Stains.Paint");
        blots = new Blot[256];
        blotBalls = new Vector4[256];
        nearBlots = new List<int>();
        squares = new List<int>[2744];
        usedSquares = new List<int>();
        posedAt = new Vector3[8][];
        posedFaces = new Vector3[8][];
        upTo = new float[1];
        upToPiece = new int[1];
        upToTri = new int[1];
        skinning = new Matrix4x4[64];
    }

    private static void Rasterise(Job j, byte[] px, Vector2 u0, Vector2 u1, Vector2 u2, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc)
    {
        float e01x = u1.x - u0.x;
        float e01y = u1.y - u0.y;
        float cross1 = e01x * (u2.y - u0.y);
        float cross2 = e01y * (u2.x - u0.x);
        if (Math.Abs(cross1 - cross2) < 0.001f)
        {
            return;
        }
        float len20 = (float)Math.Sqrt((u0.x - u2.x) * (u0.x - u2.x) + (u0.y - u2.y) * (u0.y - u2.y));
        float minX = Math.Min(u0.x, Math.Min(u1.x, u2.x));
        float maxX = Math.Max(u0.x, Math.Max(u1.x, u2.x));
        int x1 = Math.Min((int)(maxX + 0.5f), 255);
        float minY = Math.Min(u0.y, Math.Min(u1.y, u2.y));
        float maxY = Math.Max(u0.y, Math.Max(u1.y, u2.y));
        int y0 = Mathf.Max((int)(minY - 1.5f), 0);
        int y1 = Math.Min((int)(maxY + 0.5f), 255);
        if (y0 > y1)
        {
            return;
        }
        float inv = 1f / (cross1 - cross2);
        float absInv = Math.Abs(inv);
        int x0 = Mathf.Max((int)(minX - 1.5f), 0);
        float lim01 = -((float)Math.Sqrt(e01x * e01x + e01y * e01y) * absInv);
        float len12 = (float)Math.Sqrt((u2.x - u1.x) * (u2.x - u1.x) + (u2.y - u1.y) * (u2.y - u1.y));

        for (int y = y0; y <= y1; y++)
        {
            float py = y + 0.5f;
            for (int x = x0; x <= x1; x++)
            {
                float pxf = x + 0.5f;
                float w0 = inv * ((u2.y - py) * (u1.x - pxf) - (u1.y - py) * (u2.x - pxf));
                float w1 = inv * ((u0.y - py) * (u2.x - pxf) - (u2.y - py) * (u0.x - pxf));
                float w2 = (1f - w0) - w1;
                if (w0 < -(len12 * absInv) || w1 < -(len20 * absInv) || w2 < lim01)
                {
                    continue;
                }
                bool haveNormal = false;
                Vector3 normal = Vector3.zero;
                Vector3 n = na * w0 + nb * w1 + nc * w2;
                float nLen = n.magnitude;
                Vector3 pos = a * w0 + b * w1 + c * w2;
                for (int i = 0; i < nearBlots.Count; i++)
                {
                    Blot blot = blots[nearBlots[i]];
                    Vector3 d = pos - blot.a;
                    float t = 0f;
                    if (blot.ab2 > 1e-10f)
                    {
                        float tt = (d.x * blot.abX + d.y * blot.abY + d.z * blot.abZ) / blot.ab2;
                        if (tt >= 0f)
                        {
                            t = tt;
                            if (tt > 1f) t = 1f;
                        }
                    }
                    d.x -= blot.abX * t;
                    d.y -= blot.abY * t;
                    d.z -= t * blot.abZ;
                    float r = blot.ra + t * (blot.rb - blot.ra);
                    float dist2 = d.x * d.x + d.y * d.y + d.z * d.z;
                    if (dist2 < r * r)
                    {
                        if (!haveNormal)
                        {
                            normal = nLen > 1e-05f ? n / nLen : Vector3.zero;
                        }
                        float facing = (Vector3.Dot(normal, blot.faces) + 0.15f) * 3f;
                        if (facing > 0f)
                        {
                            int idx = y * 1024 + x * 4 + blot.channel;
                            facing = Math.Min(facing, 1f);
                            float v = Math.Min((1f - (float)Math.Sqrt(dist2) / r) * 3f * facing, 1f);
                            if (px[idx] < (int)(v * 255f))
                            {
                                px[idx] = (byte)(int)(v * 255f);
                            }
                        }
                        haveNormal = true;
                    }
                }
            }
        }
    }
}
