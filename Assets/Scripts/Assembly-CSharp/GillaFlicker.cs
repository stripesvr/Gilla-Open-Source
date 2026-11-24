// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaFlicker : MonoBehaviour
{
    public Light lamp;
    public Renderer tube;
    public int tubeSlot = 1;
    [ColorUsage(false, true)]
    public Color glow = new Color(1.8f, 2.1f, 2.2f, 1f);
    [Range(0, 1)]
    [Tooltip("How much of the time it holds steady.")]
    public float steadiness = 0.8f;
    public int seed = 1;
    private System.Random rng;
    private MaterialPropertyBlock block;
    private float level = 1f;
    private float baseIntensity;
    private float nextChange;

    private void Start()
    {
        rng = new System.Random(seed);
        block = new MaterialPropertyBlock();
        baseIntensity = lamp != null ? lamp.intensity : 1f;
    }

    private void Update()
    {
        if (nextChange <= Time.time)
        {
            double roll = rng.NextDouble();
            float to;
            float min;
            float max;
            if (roll < steadiness)
            {
                to = 1f;
                min = 0.5f;
                max = 3.5f;
            }
            else if (roll < steadiness + (1f - steadiness) * 0.75f)
            {
                to = (float)rng.NextDouble() * 0.35f;
                min = 0.03f;
                max = 0.1f;
            }
            else
            {
                to = 0f;
                min = 0.15f;
                max = 0.7f;
            }
            Hold(to, min, max);
        }
        if (lamp != null)
        {
            lamp.intensity = level * baseIntensity;
        }
        if (tube != null)
        {
            tube.GetPropertyBlock(block, tubeSlot);
            float f = level;
            if (level <= 0.04f)
            {
                f = 0.04f;
            }
            block.SetColor("_BaseColor", glow * f);
            tube.SetPropertyBlock(block, tubeSlot);
        }
    }

    private void Hold(float to, float min, float max)
    {
        level = to;
        float now = Time.time;
        nextChange = now + min + (max - min) * (float)rng.NextDouble();
    }
}
