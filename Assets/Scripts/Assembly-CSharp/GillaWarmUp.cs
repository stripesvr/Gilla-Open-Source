// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaWarmUp : MonoBehaviour
{
    private const float Size = 0.0001f;
    private const float Ahead = 0.4f;
    public const int FramesEach = 2;
    private Mesh little;
    private Mesh ripSkin;
    private Material ripMat;
    private readonly List<Mesh> baked = new List<Mesh>();
    private Light lamp;
    private Volume dread;
    private int frame;

    public int Specks { get; private set; }
    public long PracticeMs { get; private set; }

    public static GillaWarmUp Run()
    {
        GameObject go = new GameObject("GillaWarmUp");
        GillaWarmUp warm = go.AddComponent<GillaWarmUp>();
        warm.Make();
        return warm;
    }

    private void Make()
    {
        List<Renderer> renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).ToList();
        foreach (GameObject prefab in Prefabs())
        {
            if (prefab != null)
            {
                renderers.AddRange(prefab.GetComponentsInChildren<Renderer>(true));
            }
        }

        HashSet<(Material, bool)> seen = new HashSet<(Material, bool)>();
        foreach ((Renderer r, Mesh mesh) x in renderers.Select(r => (r, MeshOf(r))).OrderBy(x => x.Item2 != null ? x.Item2.vertexCount : 0))
        {
            Renderer r = x.r;
            bool lightmapped;
            if (r.lightmapIndex < 0)
            {
                lightmapped = false;
            }
            else
            {
                lightmapped = r.lightmapIndex < LightmapSettings.lightmaps.Length;
            }
            Material[] mats = r.sharedMaterials.Where(m => m != null).ToArray();
            if (mats.Length > 0)
            {
                bool any = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    any |= seen.Add((mats[i], lightmapped));
                }
                if (any)
                {
                    Speck(x.mesh, mats, r, lightmapped);
                }
            }
        }

        foreach (Material m in Others())
        {
            if (m != null && seen.Add((m, false)))
            {
                Speck(null, new Material[] { m }, null, false);
            }
        }

        Stopwatch sw = Stopwatch.StartNew();
        Practice();
        PracticeMs = sw.ElapsedMilliseconds;
        Sounds();
        transform.localScale = Vector3.one * Size;

        GameObject lampGo = new GameObject("Lamp");
        lamp = lampGo.AddComponent<Light>();
        lamp.transform.SetParent(transform, false);
        lamp.type = LightType.Point;
        lamp.range = 1f;
        lamp.intensity = 0.01f;
        lamp.shadows = LightShadows.None;
        lamp.renderMode = LightRenderMode.ForcePixel;
        lamp.enabled = false;

        VolumeProfile profile = Resources.Load<VolumeProfile>("DreadPost");
        if (!(profile != null))
        {
            return;
        }
        GameObject dreadGo = new GameObject("Dread");
        dread = dreadGo.AddComponent<Volume>();
        dread.transform.SetParent(transform, false);
        dread.isGlobal = true;
        dread.sharedProfile = profile;
        dread.priority = 100f;
        dread.enabled = false;
    }

    private static IEnumerable<GameObject> Prefabs()
    {
        MonsterCatalog monsters = Resources.Load<MonsterCatalog>("MonsterCatalog");
        if (monsters != null)
        {
            foreach (MonsterCatalog.Entry e in monsters.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(MonsterCatalog.Entry[])).SelectMany(f => (MonsterCatalog.Entry[])f.GetValue(monsters) ?? new MonsterCatalog.Entry[0]) /* ? */)
            {
                yield return e.prefab;
            }
        }
        CosmeticCatalog cosmetics = Resources.Load<CosmeticCatalog>("CosmeticCatalog");
        if (cosmetics != null)
        {
            foreach (CosmeticCatalog.Entry e in cosmetics.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(CosmeticCatalog.Entry[])).SelectMany(f => (CosmeticCatalog.Entry[])f.GetValue(cosmetics) ?? new CosmeticCatalog.Entry[0]) /* ? */)
            {
                yield return e.prefab;
            }
        }
        foreach (MonsterDirector director in UnityEngine.Object.FindObjectsByType<MonsterDirector>(FindObjectsSortMode.None))
        {
            foreach (MonsterDirector.Spawn s in director.spawns)
            {
                yield return s.prefab;
            }
        }
        foreach (GillaItemDirector director in UnityEngine.Object.FindObjectsByType<GillaItemDirector>(FindObjectsSortMode.None))
        {
            foreach (GillaItemDirector.Kind k in director.kinds)
            {
                yield return k.prefab;
            }
        }
        foreach (GillaSplashDirector splash in UnityEngine.Object.FindObjectsByType<GillaSplashDirector>(FindObjectsSortMode.None))
        {
            yield return splash.monsterPrefab;
        }
        foreach (NetworkBootstrap n in UnityEngine.Object.FindObjectsByType<NetworkBootstrap>(FindObjectsSortMode.None))
        {
            if (n.apePrefab != null)
            {
                yield return n.apePrefab.gameObject;
            }
        }
        GillaSpawnables spawnables = GillaSpawnables.Get();
        if (!(spawnables != null))
        {
            yield break;
        }
        yield return spawnables.dynamite;
        yield return spawnables.dummy;
        if (!(spawnables.dynamite != null))
        {
            yield break;
        }
        if (!spawnables.dynamite.TryGetComponent<GillaDynamite>(out GillaDynamite dyn))
        {
            yield break;
        }
        if (!(dyn.blast /* ? */ != null))
        {
            yield break;
        }
        yield return dyn.blast /* ? */.gameObject;
    }

    private static IEnumerable<Material> Others()
    {
        foreach (Material m in Resources.LoadAll<Material>(""))
        {
            yield return m;
        }
        GillaGoreAssets gore = Resources.Load<GillaGoreAssets>("GillaGore");
        if (gore == null)
        {
            yield break;
        }
        yield return gore.wound;
        yield return gore.bone;
        yield return gore.strand;
        yield return gore.splat;
        yield return gore.stain;
        yield return gore.chunkMaterial;
        yield return gore.chipMaterial;
        yield return gore.lump;
        yield return gore.spray;
        foreach (GillaGoreAssets.Piece p in gore.pieces)
        {
            yield return p.material;
        }
    }
}
