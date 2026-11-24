// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Sfx
{
    private const int MostAtOnce = 24;
    private const int SameAtOnce = 2;
    private const float Moment = 0.08f;
    private const float Near = 2f;
    private static readonly List<float> ending = new List<float>();
    private static readonly List<(AudioClip[] clips, Vector3 at, float t)> lately = new List<(AudioClip[] clips, Vector3 at, float t)>();

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Fresh()
    {
        ending.Clear();
        lately.Clear();
    }

    private static bool Room(AudioClip[] clips, Vector3 at, float now)
    {
        ending.RemoveAll(e => e <= now);
        if (ending.Count > 23)
        {
            return false;
        }
        lately.RemoveAll(l => now - l.t > Moment);
        int count = 0;
        foreach (var l in lately)
        {
            if (l.clips == clips)
            {
                if ((l.at - at).sqrMagnitude < 4f)
                {
                    count++;
                }
            }
        }
        return count < SameAtOnce;
    }

    public static void Play(AudioClip[] clips, Vector3 at, float volume, float pitchMin, float pitchMax)
    {
        if (clips == null || !GillaBackroomsSwitch.OnYourSide(at) || clips.Length == 0)
        {
            return;
        }
        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clip == null)
        {
            return;
        }
        float now = Time.time;
        if (!Room(clips, at, now))
        {
            return;
        }
        lately.Add((clips, at, now));
        GameObject obj = new GameObject("Sfx_" + clip.name);
        obj.transform.position = at;
        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        source.spatialBlend = 1f;
        source.minDistance = 1f;
        source.maxDistance = 25f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.Play();
        float life = clip.length / source.pitch + 0.1f;
        ending.Add(now + life);
        UnityEngine.Object.Destroy(obj, life);
    }
}
