// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaTvSong : MonoBehaviour
{
    public AudioSource source;
    [Tooltip("Where it's heard, in this object's own space: the room, and the ledge outside its door.")]
    public Bounds[] room = new Bounds[0];
    [Tooltip("How loud it plays at its loudest. The song's mastered loud (-10.8 LUFS, the shop's -25.1), so this is low for the same loudness as the boombox.")]
    [Range(0, 1)]
    public float volume = 0.04f;
    [Tooltip("Metres in from the edges over which it fades in.")]
    public float fade = 0.8f;
    private const float Ease = 0.6f;
    private const float OutOfStep = 0.4f;
    private float level;
    private float nextCheck;

    public float Level
    {
        get { return level; }
    }

    public float Length
    {
        get
        {
            if (source != null)
            {
                if (source.clip != null)
                {
                    return source.clip.length;
                }
                return 0f;
            }
            return 0f;
        }
    }

    public float SongTime
    {
        get
        {
            if (source == null) return 0f;
            if (source.clip == null) return 0f;
            if (source.isPlaying && source.clip.frequency > 0)
            {
                return (float)source.timeSamples / (float)source.clip.frequency;
            }
            return GillaBoombox.InStep(source.clip.length);
        }
    }

    private void OnEnable()
    {

        level = 0f;
        if (source != null)
        {
            source.volume = 0f;
        }
    }

    private void OnDisable()
    {
        if (source != null)
        {
            source.Pause();
        }
    }

    private void Update()
    {
        if (source == null) return;
        if (source.clip == null) return;
        float target = Inside(Ears());
        level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime / Ease);
        if (level <= 0.001f)
        {
            if (source.isPlaying)
            {
                source.Pause();
            }
            source.volume = 0f;
            return;
        }
        if (source.isPlaying)
        {
            if (nextCheck <= Time.unscaledTime)
            {
                nextCheck = Time.unscaledTime + 5f;
                float length = source.clip.length;
                float time = source.time;
                float delta = Mathf.DeltaAngle(time / length * 360f, GillaBoombox.InStep(length) / length * 360f);
                if (OutOfStep < length * (Mathf.Abs(delta) / 360f))
                {
                    source.time = GillaBoombox.InStep(length);
                }
            }
        }
        else
        {
            source.time = GillaBoombox.InStep(source.clip.length);
            source.Play();
            nextCheck = Time.unscaledTime + 2f;
        }
        source.volume = level * volume;
    }

    private static Vector3 Ears()
    {
        AudioListener listener = GillaEars.Listening;
        if (listener != null)
        {
            return listener.transform.position;
        }
        Camera cam = Camera.main;
        if (!(cam != null))
        {
            return new Vector3(0f, -1000000f, 0f);
        }
        return cam.transform.position;
    }

    public float Inside(Vector3 p)
    {
        Vector3 local = transform.InverseTransformPoint(p);
        float best = 0f;
        for (int i = 0; i < room.Length; i++)
        {
            Bounds b = room[i];
            Vector3 c = b.center;
            Vector3 e = b.extents;
            if (c.y - e.y <= local.y && local.y <= c.y + e.y)
            {
                float d = Mathf.Min(local.x - (c.x - e.x), (c.x + e.x) - local.x);
                float dz = Mathf.Min(local.z - (c.z - e.z), (c.z + e.z) - local.z);
                d = Mathf.Min(d, dz);
                if (0f < d)
                {
                    float w = 1f;
                    if (0f < fade)
                    {
                        w = Mathf.Clamp01(d / fade);
                    }
                    best = Mathf.Max(best, w);
                }
            }
        }
        return best;
    }
}
