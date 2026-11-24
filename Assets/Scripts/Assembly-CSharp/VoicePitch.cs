// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public class VoicePitch : MonoBehaviour
{
    private sealed class Line
    {
        public int sampleRate;
        public float windowMs;
        public float glideMs;
        public float[] buf;
        public int size;
        public int mask;
        public float window;
        public double invWindow;
        public float glideK;
        public float fadeStep;
        public int write;
        public double phase;
        public float glide = 1f;
        public float mix;
        public int warmup;
    }

    private const int MaxChannels = 8;
    private const float FadeMs = 30;
    private const float NoShift = 0.01f;

    [Range(0.5f, 2f)]
    [Tooltip("Voice pitch: 1 = normal, 0.7 = deep monster, 1.5 = squeaky. Set it every frame from the main thread; changes glide in, speed never changes.")]
    public float pitch = 1f;

    [Range(30f, 80f)]
    [Tooltip("Length of each read head's fade window in milliseconds. Longer is smoother on deep voices but more echo-y; 50 suits speech.")]
    public float windowMs = 50f;

    [Range(20f, 200f)]
    [Tooltip("Roughly how long a pitch change takes to settle, in milliseconds.")]
    public float glideMs = 80f;

    private Line line;
    private bool resetPending;

    private void OnEnable()
    {

        Build();
        resetPending = true;
        AudioSettings.OnAudioConfigurationChanged += AudioConfigChanged;
    }

    private void OnDisable()
    {
        AudioSettings.OnAudioConfigurationChanged -= AudioConfigChanged;
    }

    private void AudioConfigChanged(bool deviceWasChanged)
    {
        Build();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && line != null)
        {
            Build();
        }
    }

    private void Build()
    {
        int rate = AudioSettings.outputSampleRate;
        if (rate < 1)
        {
            rate = 48000;
        }
        Line old = line;
        if (old != null && old.sampleRate == rate && old.windowMs == windowMs && old.glideMs == glideMs)
        {
            return;
        }
        float rateF = (float)rate;
        float win = Mathf.Max(64f, windowMs * 0.001f * rateF);
        int m = (int)win + 3;
        m |= m >> 16;
        m |= m >> 8;
        m |= m >> 4;
        m |= m >> 2;
        m |= m >> 1;
        int size = m + 1;

        Line l = new Line();
        l.sampleRate = rate;
        l.windowMs = windowMs;
        l.glideMs = glideMs;
        l.buf = new float[size * MaxChannels];
        l.size = size;
        l.mask = m;
        l.window = win;
        float glideSamples = Mathf.Max(1f, glideMs * 0.001f * rateF);
        l.invWindow = 1.0 / (double)win;
        float e = Mathf.Exp(-3f / glideSamples);
        l.warmup = size;
        float fadeSamples = Mathf.Max(1f, rateF * 0.03f);
        l.glideK = 1f - e;
        l.fadeStep = 1f / fadeSamples;
        line = l;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        Line l = line;
        if (channels <= 0 || l == null)
        {
            return;
        }

        float p = float.IsNaN(pitch) || float.IsInfinity(pitch) ? 1f : pitch;
        float target = Mathf.Clamp(p, 0.5f, 2f);
        bool near = target >= 0.99f && target <= 1.01f;
        if (near)
        {
            target = 1f;
        }

        if (resetPending)
        {
            resetPending = false;
            l.warmup = l.size;
            l.glide = 1f;
            l.mix = 0f;
        }
        int size = l.size;

        int chans = channels > 7 ? MaxChannels : channels;
        float mix = l.mix;
        float[] buf = l.buf;
        int mask = l.mask;
        int frames = data.Length / channels;
        int write = l.write;

        if (mix <= 0f && near)
        {
            for (int f = 0, baseIdx = 0; f < frames; f++, baseIdx += channels)
            {
                for (int c = 0; c < chans; c++)
                {
                    buf[write + c * size] = data[baseIdx + c];
                }
                write = (write + 1) & mask;
            }
            int warm = l.warmup;
            l.write = write;
            l.glide = 1f;
            if (warm > 0)
            {
                l.warmup = Mathf.Max(warm - frames, 0);
            }
            return;
        }

        double phase = l.phase;
        float glide = l.glide;
        int warmup = l.warmup;
        if (frames > 0)
        {
            float window = l.window;
            double invWindow = l.invWindow;
            float glideK = l.glideK;
            float fadeStep = l.fadeStep;
            for (int f = 0, baseIdx = 0; f < frames; f++, baseIdx += channels)
            {
                float g = glide + glideK * (target - glide);
                glide = Mathf.Abs(target - g) < 0.0001f ? target : g;

                bool fadeOut;
                if (warmup < 1 && near)
                {
                    fadeOut = !(glide > 1.01f || glide < 0.99f);
                }
                else if (warmup > 0)
                {
                    warmup--;
                    fadeOut = true;
                }
                else
                {
                    fadeOut = false;
                }
                if (fadeOut)
                {
                    mix -= fadeStep;
                    if (mix < 0f) mix = 0f;
                }
                else
                {
                    mix += fadeStep;
                    if (mix > 1f) mix = 1f;
                }

                phase += invWindow * (double)(1f - glide);
                if (phase >= 1.0)
                {
                    phase -= 1.0;
                }
                else if (phase < 0.0)
                {
                    phase += 1.0;
                }
                double off = phase >= 0.5 ? -0.5 : 0.5;
                double s = Math.Sin(phase * Math.PI);
                double d1 = phase * (double)window;
                double d2 = (phase + off) * (double)window;
                float w = (float)s * (float)s;
                int i1 = (int)d1;
                int i2 = (int)d2;
                int r1 = write - i1;
                int r2 = write - i2;
                float frac1 = (float)d1 - (float)i1;
                float frac2 = (float)d2 - (float)i2;

                for (int c = 0; c < chans; c++)
                {
                    int chOff = c * size;
                    float dry = data[baseIdx + c];
                    buf[write + chOff] = dry;
                    int a1 = (r1 & mask) + chOff;
                    int b1 = ((r1 - 1) & mask) + chOff;
                    int a2 = (r2 & mask) + chOff;
                    int b2 = ((r2 - 1) & mask) + chOff;
                    float tap1 = buf[a1] + frac1 * (buf[b1] - buf[a1]);
                    float tap2 = buf[a2] + frac2 * (buf[b2] - buf[a2]);
                    data[baseIdx + c] = dry + mix * ((w * tap1 + (1f - w) * tap2) - dry);
                }
                write = (write + 1) & mask;
            }
        }
        l.phase = phase;
        l.glide = glide;
        l.mix = mix;
        l.write = write;
        l.warmup = warmup;
    }
}
