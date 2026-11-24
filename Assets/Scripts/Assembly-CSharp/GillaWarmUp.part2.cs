using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Fusion;

public partial class GillaWarmUp : MonoBehaviour
{
    // Reads a member that is not visible from here (name kept from the reconstruction).
    private static T Member<T>(object owner, string name) where T : class
    {
        if (owner == null)
        {
            return null;
        }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = owner.GetType();
        FieldInfo field = type.GetField(name, flags);
        if (field != null)
        {
            return field.GetValue(owner) as T;
        }
        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null)
        {
            return property.GetValue(owner) as T;
        }
        return null;
    }

    private static Dismemberment DismembermentOf(NetworkApe ape)
    {
        if (ape == null)
        {
            return null;
        }
        return ape.GetComponentInChildren<Dismemberment>(true) /* ? */;
    }

    private static void CallChunks(Vector3 pos, Vector3 dir, Mesh mesh, Material mat, int count, float a, float b, float c, float d)
    {
        MethodInfo chunks = typeof(GillaWounds).GetMethod("Chunks", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(Vector3), typeof(Vector3), typeof(Mesh), typeof(Material), typeof(int), typeof(float), typeof(float), typeof(float), typeof(float) }, null);
        if (chunks != null)
        {
            chunks.Invoke(null, new object[] { pos, dir, mesh, mat, count, a, b, c, d }) /* ? */;
        }
    }

    private void Practice()
    {
        NetworkObject prefab = UnityEngine.Object.FindObjectsByType<NetworkBootstrap>(FindObjectsSortMode.None)
            .Select(b => b.apePrefab /* ? */)
            .FirstOrDefault(n => n != null);
        NetworkApe ape = null;
        if (prefab != null)
        {
            ape = prefab.GetComponent<NetworkApe>();
        }
        if (ape == null)
        {
            return;
        }
        Dismemberment apeDismemberment = DismembermentOf(ape) /* ? */;
        if (apeDismemberment == null)
        {
            return;
        }
        GameObject root = new GameObject("Practice");
        root.SetActive(false);
        root.transform.SetParent(transform, false);
        Dismemberment copy = UnityEngine.Object.Instantiate<Dismemberment>(apeDismemberment /* ? */, root.transform);
        MonoBehaviour[] behaviours = copy.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];
            if (b != copy)
            {
                UnityEngine.Object.DestroyImmediate(b);
            }
        }
        copy.tearSounds = null;
        copy.dropSounds = null;
        Transform[] all = copy.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            all[i].gameObject.layer = 0;
        }
        root.SetActive(true);
        SkinnedMeshRenderer[] skins = copy.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(s => s.sharedMesh != null /* ? */)
            .ToArray();
        for (int i = 0; i < skins.Length; i++)
        {
            SkinnedMeshRenderer skin = skins[i];
            Piece(skin, Dismemberment.WithCaps(skin));
        }
        GillaWounds wounds = GillaWounds.On(copy);
        wounds.Practice();
        Material source = null;
        for (int i = 0; i < skins.Length; i++)
        {
            SkinnedMeshRenderer r = skins[i];
            Piece(r, r.sharedMaterials);
            if (source == null)
            {
                source = r.sharedMaterial;
            }
        }
        if (source != null)
        {
            ripSkin = new Mesh();
            ripSkin.name = "GillaWarmUpRip";
            Vector3[] verts = new Vector3[3];
            verts[0] = Vector3.zero;
            verts[1] = Vector3.up;
            verts[2] = Vector3.right;
            ripSkin.SetVertices(verts);
            Mesh m = ripSkin;
            Vector3[] normals = new Vector3[3];
            normals[0] = Vector3.back;
            normals[1] = Vector3.back;
            normals[2] = Vector3.back;
            m.SetNormals(normals);
            Vector2[] uvs = new Vector2[3];
            uvs[0] = Vector2.zero;
            uvs[1] = Vector2.up;
            uvs[2] = Vector2.right;
            ripSkin.SetUVs(0, uvs);
            ripSkin.SetUVs(1, uvs);
            ripSkin.SetUVs(2, new List<Vector3>(verts));
            ripSkin.SetUVs(3, new List<Vector3>(verts));
            BoneWeight[] weights = new BoneWeight[3];
            weights[0].weight0 = 1f;
            weights[1].weight0 = 1f;
            weights[2].weight0 = 1f;
            ripSkin.boneWeights = weights;
            Matrix4x4[] binds = new Matrix4x4[1];
            binds[0] = Matrix4x4.identity;
            ripSkin.bindposes = binds;
            int[] tris = new int[3];
            tris[1] = 1;
            tris[2] = 2;
            ripSkin.SetTriangles(tris, 0);
            ripSkin.RecalculateBounds();
            GameObject half = new GameObject("PracticeHalf");
            half.transform.SetParent(root.transform, false);
            SkinnedMeshRenderer smr = half.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = ripSkin;
            Transform[] bones = new Transform[1];
            bones[0] = half.transform;
            smr.bones = bones;
            smr.rootBone = half.transform;
            ripMat = new Material(source);
            ripMat.SetFloat("_RipOn", 1f);
            smr.sharedMaterial = ripMat;
        }
        MonsterWounds.WarmUp(root.transform);
    }

    private static void Sounds()
    {
        List<AudioClip> clips = new List<AudioClip>();

        void Add(IEnumerable<AudioClip> more)
        {
            if (more != null)
            {
                clips.AddRange(more);
            }
        }

        GillaGoreAssets gore = Resources.Load<GillaGoreAssets>("GillaGore");
        if (gore != null)
        {
            Add(Member<IEnumerable<AudioClip>>(gore, "sounds") /* ? */);
        }
        NetworkBootstrap[] boots = UnityEngine.Object.FindObjectsByType<NetworkBootstrap>(FindObjectsSortMode.None);
        for (int i = 0; i < boots.Length; i++)
        {
            NetworkBootstrap boot = boots[i];
            NetworkApe ape = null;
            if (boot.apePrefab /* ? */ != null)
            {
                ape = boot.apePrefab /* ? */.GetComponent<NetworkApe>();
            }
            if (ape != null)
            {
                Dismemberment dis = DismembermentOf(ape) /* ? */;
                if (dis != null)
                {
                    Add(dis.tearSounds);
                    Add(dis.dropSounds);
                }
            }
            object monsters = Member<object>(boot, "monsters") /* ? */;
            if (monsters != null)
            {
                Array entries = Member<Array>(monsters, "entries") /* ? */;
                if (entries != null)
                {
                    for (int j = 0; j < entries.Length; j++)
                    {
                        object e = entries.GetValue(j);
                        if (e != null)
                        {
                            Add(Member<IEnumerable<AudioClip>>(e, "sounds1") /* ? */);
                            Add(Member<IEnumerable<AudioClip>>(e, "sounds2") /* ? */);
                        }
                    }
                }
            }
        }
        GillaSpawnables spawnables = GillaSpawnables.Get();
        GillaDynamite dynamite = null;
        if (spawnables != null)
        {
            dynamite = null;
            if (spawnables.dynamite != null)
            {
                dynamite = spawnables.dynamite.GetComponent<GillaDynamite>();
            }
        }
        if (dynamite != null)
        {
            clips.Add(Member<AudioClip>(dynamite, "boomClip") /* ? */);
            AudioSource fuseSource = Member<AudioSource>(dynamite, "fuseSource") /* ? */;
            if (fuseSource != null)
            {
                clips.Add(fuseSource.clip);
            }
            object extra = Member<object>(dynamite, "extra") /* ? */;
            if (extra != null)
            {
                Add(Member<IEnumerable<AudioClip>>(extra, "clips") /* ? */);
            }
        }
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                if (clip.loadState == AudioDataLoadState.Unloaded)
                {
                    clip.LoadAudioData();
                }
            }
        }
    }

    private void Piece(SkinnedMeshRenderer skin, Material[] mats)
    {
        Mesh mesh = new Mesh();
        mesh.name = "GillaWarmUpPiece";
        skin.BakeMesh(mesh, false);
        baked.Add(mesh);
        GameObject go = new GameObject("PracticePiece");
        go.transform.SetParent(transform, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = mats;
    }

    private void PracticeParticles()
    {
        GillaGoreAssets gore = Resources.Load<GillaGoreAssets>("GillaGore");
        if (gore == null)
        {
            return;
        }
        Material bloodMaterial = Member<Material>(gore, "bloodMaterial") /* ? */;
        if (bloodMaterial != null)
        {
            Blood.Spray(transform, Vector3.up, bloodMaterial /* ? */, 0.3f, 2, 1, 0.001f);
        }
        Mesh chunkMesh = Member<Mesh>(gore, "chunkMesh") /* ? */;
        if (chunkMesh == null)
        {
            return;
        }
        Material chunkMaterial = Member<Material>(gore, "chunkMaterial") /* ? */;
        if (chunkMaterial != null)
        {
            CallChunks(transform.position, Vector3.up, chunkMesh /* ? */, chunkMaterial /* ? */, 1, 0.0001f, 0.0001f, 0.01f, 38f);
        }
        Material boneMaterial = Member<Material>(gore, "boneMaterial") /* ? */;
        if (boneMaterial != null)
        {
            CallChunks(transform.position, Vector3.up, chunkMesh /* ? */, boneMaterial /* ? */, 1, 0.0001f, 0.0001f, 0.01f, 38f);
        }
    }

    private static Mesh MeshOf(Renderer r)
    {
        MeshFilter filter = null;
        if (r != null)
        {
            if (r is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }
            if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out filter))
            {
                return filter.sharedMesh;
            }
        }
        return null;
    }

    private void Speck(Mesh mesh, Material[] mats, Renderer like, bool lightmapped)
    {
        GameObject go = new GameObject("Speck");
        go.transform.SetParent(transform, false);
        int layer;
        if (like != null)
        {
            layer = like.gameObject.layer;
        }
        else
        {
            layer = 0;
        }
        go.layer = layer;
        MeshFilter filter = go.AddComponent<MeshFilter>();
        if (!(mesh != null))
        {
            mesh = Little();
        }
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = mats;
        if (like != null)
        {
            renderer.shadowCastingMode = like.shadowCastingMode;
        }
        if (lightmapped)
        {
            renderer.lightmapIndex = like.lightmapIndex;
            renderer.lightmapScaleOffset = like.lightmapScaleOffset;
        }
        Specks = Specks + 1;
    }

    private Mesh Little()
    {
        if (little != null)
        {
            return little;
        }
        little = new Mesh();
        little.name = "GillaWarmUp";
        Mesh m = little;
        Vector3[] verts = new Vector3[3];
        verts[0] = Vector3.zero;
        verts[1] = Vector3.up;
        verts[2] = Vector3.right;
        m.SetVertices(verts);
        m = little;
        Vector3[] normals = new Vector3[3];
        normals[0] = Vector3.back;
        normals[1] = Vector3.back;
        normals[2] = Vector3.back;
        m.SetNormals(normals);
        m = little;
        Vector4[] tangents = new Vector4[3];
        tangents[0] = new Vector4(1f, 0f, 0f, 1f) /* ? */;
        tangents[1] = new Vector4(1f, 0f, 0f, 1f) /* ? */;
        tangents[2] = new Vector4(1f, 0f, 0f, 1f) /* ? */;
        m.SetTangents(tangents);
        m = little;
        Color[] colors = new Color[3];
        colors[0] = new Color(1f, 1f, 1f, 1f);
        colors[1] = new Color(1f, 1f, 1f, 1f);
        colors[2] = new Color(1f, 1f, 1f, 1f);
        m.SetColors(colors);
        m = little;
        Vector2[] uvs = new Vector2[3];
        uvs[0] = Vector2.zero;
        uvs[1] = Vector2.up;
        uvs[2] = Vector2.right;
        m.SetUVs(0, uvs);
        m = little;
        int[] tris = new int[3];
        tris[1] = 1;
        tris[2] = 2;
        m.SetTriangles(tris, 0);
        return little;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }
        Transform camT = cam.transform;
        Transform me = transform;
        me.SetPositionAndRotation(camT.position + camT.forward * Ahead, camT.rotation);
        if (frame == 0)
        {
            foreach (Transform child in transform)
            {
                if (((cam.cullingMask >> child.gameObject.layer) & 1) == 0)
                {
                    child.gameObject.layer = 0;
                }
            }
            PracticeParticles();
        }
        if (frame == 2)
        {
            lamp.enabled = true;
            if (dread != null)
            {
                dread.enabled = true;
            }
        }
        if (frame == 4)
        {
            UnityEngine.Object.Destroy(gameObject);
        }
        frame = frame + 1;
    }

    private void OnDestroy()
    {
        if (little != null)
        {
            UnityEngine.Object.Destroy(little);
        }
        if (ripSkin != null)
        {
            UnityEngine.Object.Destroy(ripSkin);
        }
        if (ripMat != null)
        {
            UnityEngine.Object.Destroy(ripMat);
        }
        foreach (Mesh mesh in baked)
        {
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }
        }
    }
}
