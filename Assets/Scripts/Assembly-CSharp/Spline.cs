// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;

public static class Spline
{
    public static float Eval(float t, float[] T, float[] V)
    {
        int n = T.Length;
        int last = n - 1;
        if (last < 0)
        {
            return 0f;
        }
        if (t <= T[0])
        {
            return V[0];
        }
        if (T[last] <= t)
        {
            return V[last];
        }

        int maxSeg = Math.Max(n - 2, 0);
        int i = 0;
        while (i != maxSeg && T[i + 1] <= t)
        {
            i++;
        }

        float t0 = T[i];
        float h = T[i + 1] - t0;
        float m0 = 0f;
        float m1 = 0f;
        if (i != 0)
        {
            m0 = (V[i + 1] - V[i - 1]) * (h / (T[i + 1] - T[i - 1]));
        }
        if (i + 2 <= last)
        {
            m1 = (V[i + 2] - V[i]) * (h / (T[i + 2] - t0));
        }

        float s = (t - t0) / h;
        float s2 = s * s;
        float s3 = s * s2;
        return (s2 * 3f - (s3 + s3)) * V[i + 1]
             + (((s3 + s3) - s2 * 3f) + 1f) * V[i]
             + (s + (s3 - (s2 + s2))) * m0
             + (s3 - s2) * m1;
    }
}
