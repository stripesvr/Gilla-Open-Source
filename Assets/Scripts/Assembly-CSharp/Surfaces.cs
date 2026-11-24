// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Surfaces
{
    private static readonly Dictionary<Collider, SurfaceKind> known = new Dictionary<Collider, SurfaceKind>();
    private static readonly List<string> names = new List<string>();
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Collider[] near = new Collider[8];
    private static readonly (SurfaceKind kind, string[] words)[] Words = new (SurfaceKind kind, string[] words)[]
    {
        (SurfaceKind.Glass, new string[] { "glass", "mirror" }),
        (SurfaceKind.MetalGrate, new string[] { "grate", "wiremesh", "blacksteel", "cage" }),
        (SurfaceKind.Metal, new string[] { "metal", "steel", "rust", "paint", "plate", "drum", "barrel", "pipe", "shelves", "pedestal" }),
        (SurfaceKind.Wood, new string[] { "wood", "plank", "parquet", "bark", "crate", "pallet" }),
        (SurfaceKind.Dirt, new string[] { "dirt", "lawn", "grass", "gravel", "pebble", "sand", "mud" }),
        (SurfaceKind.Plastic, new string[] { "plastic", "console" }),
        (SurfaceKind.Soft, new string[] { "cloth", "cushion", "rug", "carpet", "fabric" }),
    };

    public static SurfaceKind KindOf(Collider c)
    {
        SurfaceKind kind = SurfaceKind.Concrete;
        if (c == null)
        {
            return SurfaceKind.Concrete;
        }
        if (!known.TryGetValue(c, out kind))
        {
            kind = Find(c);
            known[c] = kind;
        }
        return kind;
    }

    public static Collider Under(Vector3 at, float radius, int layers)
    {
        int count = Physics.OverlapSphereNonAlloc(at, radius, near, layers, QueryTriggerInteraction.Ignore);
        if (count < 1)
        {
            return null;
        }
        float best = float.MaxValue;
        Collider bestCollider = null;
        for (int i = 0; i < count; i++)
        {
            Collider col = near[i];
            Vector3 p;
            MeshCollider mc = col as MeshCollider;
            if (mc == null || mc.convex)
            {
                p = col.ClosestPoint(at);
            }
            else
            {
                Bounds b = col.bounds;
                p = b.ClosestPoint(at);
            }
            float d = (p - at).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestCollider = col;
            }
        }
        return bestCollider;
    }

    public static bool Play(AudioSource src, Collider surface, float loud, float pitchMin = 0.92f, float pitchMax = 1.08f)
    {
        GillaSounds sounds = GillaSounds.Get();
        if (sounds != null)
        {
            SurfaceKind kind = KindOf(surface);
            GillaSounds.SurfaceSet set = sounds.For(kind);
            if (set == null)
            {
                return false;
            }
            AudioClip[] clips = set.clips;
            if (clips != null && clips.Length != 0)
            {
                AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
                if (!(clip == null))
                {
                    src.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
                    src.PlayOneShot(clip, set.volume * loud);
                    return true;
                }
            }
        }
        return false;
    }

    private static SurfaceKind Find(Collider c)
    {
        GillaSurface tag = c.GetComponentInParent<GillaSurface>();
        if (tag != null)
        {
            return tag.kind /* ? */;
        }
        names.Clear();
        if (c.sharedMaterial != null)
        {
            names.Add(c.sharedMaterial.name);
        }
        Renderer renderer = c.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = c.GetComponentInParent<Renderer>();
        }
        if (renderer == null)
        {
            renderer = c.GetComponentInChildren<Renderer>();
        }
        if (renderer != null)
        {
            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null)
                {
                    continue;
                }
                names.Add(mat.name);
                Texture tex = null;
                if (mat.HasProperty(BaseMap))
                {
                    tex = mat.GetTexture(BaseMap);
                }
                if (tex != null)
                {
                    names.Add(tex.name);
                }
            }
        }
        for (int i = 0; i < Words.Length; i++)
        {
            SurfaceKind kind = Words[i].kind;
            string[] words = Words[i].words;
            foreach (string n in names)
            {
                string lower = n.ToLowerInvariant();
                for (int j = 0; j < words.Length; j++)
                {
                    if (lower.Contains(words[j]))
                    {
                        return kind;
                    }
                }
            }
        }
        return SurfaceKind.Concrete;
    }
}
