// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class MonsterWounds : MonoBehaviour
{
    private void Died_b__105_0()
    {
        if (this == null)
        {
            return;
        }
        Dismemberment tearing = MonsterWounds.Tearing();
        Vector3 at;
        if (brain != null)
        {
            at = brain.Middle;
        }
        else
        {
            at = transform.position;
        }
        if (tearing != null)
        {
            Blood.Burst(at, Vector3.up + transform.forward * 0.3f, tearing.bloodMaterial, 0.9f);
        }
        GillaGoreAssets assets = Resources.Load<GillaGoreAssets>("GillaGore");
        if (assets != null)
        {
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            AudioClip[] clips = null;
            FieldInfo clipsField = typeof(GillaGoreAssets).GetField("deathClips", any); /* ? */
            if (clipsField != null)
            {
                clips = clipsField.GetValue(assets) as AudioClip[];
            }
            else
            {
                PropertyInfo clipsProp = typeof(GillaGoreAssets).GetProperty("deathClips", any); /* ? */
                if (clipsProp != null)
                {
                    clips = clipsProp.GetValue(assets) as AudioClip[];
                }
            }
            bool onSide = GillaBackroomsSwitch.OnYourSide(at);
            if (clips != null && onSide && clips.Length != 0)
            {
                AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                if (clip == null)
                {
                    return;
                }
                float now = Time.time;
                MethodInfo room = typeof(Sfx).GetMethod("Room", any, null, new Type[] { typeof(AudioClip[]), typeof(Vector3), typeof(float) }, null); /* ? */
                bool hasRoom = room != null && (bool)room.Invoke(null, new object[] { clips, at, now });
                if (hasRoom)
                {
                    object lately = typeof(Sfx).GetField("lately", any)?.GetValue(null); /* ? */
                    if (lately != null)
                    {
                        lately.GetType().GetMethod("Add")?.Invoke(lately, new object[] { (clips, at, now) }); /* ? */
                    }
                    GameObject go = new GameObject("Sfx_" + clip.name);
                    go.transform.position = at;
                    AudioSource source = go.AddComponent<AudioSource>();
                    source.clip = clip;
                    source.volume = 1f;
                    source.pitch = UnityEngine.Random.Range(0.6f, 0.75f);
                    source.spatialBlend = 1f;
                    source.minDistance = 1f;
                    source.maxDistance = 25f;
                    source.rolloffMode = AudioRolloffMode.Linear;
                    source.dopplerLevel = 0f;
                    source.Play();
                    float life = clip.length / source.pitch + 0.1f;
                    object ending = typeof(Sfx).GetField("ending", any)?.GetValue(null); /* ? */
                    if (ending != null)
                    {
                        ending.GetType().GetMethod("Add")?.Invoke(ending, new object[] { now + life }); /* ? */
                    }
                    UnityEngine.Object.Destroy(go, life);
                }
            }
        }
    }
}
