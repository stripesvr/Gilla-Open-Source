// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public class GillaSinkPlatform : MonoBehaviour
{
    public int index;
    [Tooltip("Its place in the map (local), floating or standing where the map put it.")]
    public Vector3 home;
    public Quaternion homeTurn = Quaternion.identity;
    public const float Depth = 0.85f;

    public static ValueTuple<float, float, float, float> Times(int round)
    {
        float n = Mathf.Max(round - 2, 0);
        float shake = n * -0.1f + 0.8f;
        float sink = n * -0.12f + 1.2f;
        if (shake < 0.3f) shake = 0.3f;
        if (sink < 0.6f) sink = 0.6f;
        float down = shake + sink;
        float rise = n * 1.2f + down + 4.5f;
        return new ValueTuple<float, float, float, float>(shake, down, rise, rise + 1.7f);
    }

    public static float Cycle(int round)
    {
        return Times(round).Item4 + 0.8f;
    }

    public static float Under(float t, int round)
    {
        float result = 0f;
        if (0f <= t)
        {
            var at = At(t, 0, round);
            result = Mathf.Clamp01((at.Item1 - 0.2f) / 0.15f);
        }
        return result;
    }

    public static ValueTuple<float, float> At(float t, int seed, int round)
    {
        var times = Times(round);
        float sink;
        float wobble;
        if (t < 0f || times.Item4 <= t)
        {
            sink = 0f;
            wobble = 0f;
        }
        else if (t < times.Item1)
        {
            float s1 = Mathf.Sin(t * 55f + seed);
            float s2 = Mathf.Sin(t * 38f + seed * 1.7f);
            sink = s1 * 0.015f + (t / times.Item1) * 0.03f;
            wobble = s2 * (t / times.Item1) * 1.6f;
        }
        else if (t < times.Item2)
        {
            float k = (t - times.Item1) / (times.Item2 - times.Item1);
            float e = Mathf.Clamp01(k * k * (3f - (k + k)));
            sink = e * 0.82000005f + 0.03f;
            wobble = Mathf.Sin(seed) * (1f - k) * 4f;
        }
        else if (times.Item3 <= t)
        {
            float k = (t - times.Item3) / (times.Item4 - times.Item3);
            float e = Mathf.Clamp01(k * k * (3f - (k + k)));
            sink = 0.85f - e * 0.85f;
            wobble = 0f;
        }
        else
        {
            sink = 0.85f;
            wobble = 0f;
        }
        return new ValueTuple<float, float>(sink, wobble);
    }

    public bool Show(float t, int round)
    {
        var at = At(t, index, round);
        Vector3 pos = home + Vector3.down * at.Item1;
        Quaternion rot = homeTurn * Quaternion.Euler(at.Item2, 0f, at.Item2 * 0.6f);
        if ((transform.localPosition - pos).sqrMagnitude < 1e-08f)
        {
            if (Quaternion.Angle(transform.localRotation, rot) < 0.01f)
            {
                return false;
            }
        }
        transform.SetLocalPositionAndRotation(pos, rot);
        return true;
    }
}
