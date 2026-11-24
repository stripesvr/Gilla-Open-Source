// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReverbProbe : MonoBehaviour
{
    [Tooltip("How far a ray looks for a wall (m).")]
    public float range = 30f;
    [Tooltip("How quickly the echo changes to a new space (per second).")]
    public float blend = 2f;
    private AudioReverbZone zone;
    private float enclosed;
    private float size = 5f;
    private float nextProbe;
    private static readonly Vector3[] Dirs = MakeDirs();
    private const int Solid = -5893;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddToListener();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddToListener();
    }

    private static void AddToListener()
    {
        AudioListener listener = Object.FindAnyObjectByType<AudioListener>();
        if (listener != null)
        {
            if (listener.GetComponent<ReverbProbe>() == null)
            {
                listener.gameObject.AddComponent<ReverbProbe>();
            }
        }
    }

    private static Vector3[] MakeDirs()
    {
        List<Vector3> list = new List<Vector3>();
        for (int i = 0; i < 12; i++)
        {
            list.Add(Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward);
        }
        for (int i = 0; i < 12; i++)
        {
            list.Add(Quaternion.Euler(-35f, i * 30f + 15f, 0f) * Vector3.forward);
        }
        list.Add(Vector3.up);
        list.Add(Vector3.down);
        for (int i = 0; i < 4; i++)
        {
            list.Add(Quaternion.Euler(-65f, i * 90f, 0f) * Vector3.forward);
        }
        return list.ToArray();
    }

    private void Start()
    {
        GameObject go = new GameObject("Reverb");
        go.transform.SetParent(transform, false);
        zone = go.AddComponent<AudioReverbZone>();
        zone.minDistance = 500f;
        zone.maxDistance = 500f;
        zone.reverbPreset = AudioReverbPreset.User;
        zone.diffusion = 100f;
        zone.density = 100f;
        zone.HFReference = 5000f;
        zone.LFReference = 250f;
    }

    private void Update()
    {
        if (zone == null)
        {
            return;
        }
        if (nextProbe <= Time.time)
        {
            nextProbe = Time.time + 0.25f;
            float total = 0f;
            int hits = 0;
            for (int i = 0; i < Dirs.Length; i++)
            {
                Vector3 dir = Dirs[i];
                RaycastHit hit;
                float d;
                if (Physics.Raycast(transform.position, dir, out hit, range, Solid, QueryTriggerInteraction.Ignore))
                {
                    hits++;
                    d = hit.distance;
                }
                else
                {
                    d = range;
                }
                total += d;
            }
            float n = Dirs.Length;
            enclosed = hits / n;
            size = total / n;
        }

        float k = 1f - Mathf.Exp(-(blend * Time.deltaTime));
        float t = Mathf.Clamp01((size - 2f) * 0.0625f);
        float e = enclosed;
        float hall = t * e;
        float cave = e * (1f - t) * 0.6f;
        float generic = 1f - hall - cave;

        float Mix(float h, float c, float g)
        {
            return hall * h + cave * c + generic * g;
        }

        zone.room = Mathf.RoundToInt(Mathf.Lerp(zone.room, -1000f, k));
        zone.roomHF = Mathf.RoundToInt(Mathf.Lerp(zone.roomHF, Mix(-237f, 0f, -100f), k));
        zone.decayTime = Mathf.Lerp(zone.decayTime, Mix(2.7f, 2.91f, 1.49f), k);
        zone.decayHFRatio = Mathf.Lerp(zone.decayHFRatio, Mix(0.79f, 1.3f, 0.83f), k);
        zone.reflections = Mathf.RoundToInt(Mathf.Lerp(zone.reflections, Mix(649f, -602f, -2602f), k));
        zone.reflectionsDelay = Mathf.Lerp(zone.reflectionsDelay, Mix(0.013f, 0.015f, 0.007f), k);
        zone.reverb = Mathf.RoundToInt(Mathf.Lerp(zone.reverb, Mix(395f, -302f, 200f), k));
        zone.reverbDelay = Mathf.Lerp(zone.reverbDelay, Mix(0.02f, 0.022f, 0.011f), k);
        zone.LFReference = Mathf.Lerp(zone.LFReference, Mix(250f, 250f, 537f), k);
    }
}
