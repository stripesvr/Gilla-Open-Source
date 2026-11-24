// Redux addition: the ripped ape meshes have badly distorted UVs, which stretched the fur.
// Gilla/ApeFur now maps the fur from the mesh's rest-pose shape instead. This copies each fur
// mesh's rest-pose positions and normals into UV channels 2 and 3 (once per mesh), so the fur
// stays put on the body while it animates.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GillaFurMap : MonoBehaviour
{
    private static readonly Dictionary<Mesh, Mesh> done = new Dictionary<Mesh, Mesh>();
    private static readonly HashSet<int> seen = new HashSet<int>();
    private static Shader fur;
    private float next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        done.Clear();
        seen.Clear();
        GameObject go = new GameObject("GillaFurMap");
        DontDestroyOnLoad(go);
        go.AddComponent<GillaFurMap>();
    }

    private void OnEnable() { SceneManager.sceneLoaded += Loaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= Loaded; }
    private void Loaded(Scene s, LoadSceneMode m) { next = 0f; }

    private void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 1f;   // players and cosmetics appear later, so keep looking
        if (fur == null) fur = Shader.Find("Gilla/ApeFur");
        if (fur == null) return;
        foreach (SkinnedMeshRenderer r in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!seen.Add(r.GetInstanceID()) || !UsesFur(r)) continue;
            Mesh m = Baked(r.sharedMesh);
            if (m != null) r.sharedMesh = m;
        }
        foreach (MeshFilter f in FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!seen.Add(f.GetInstanceID())) continue;
            Renderer r = f.GetComponent<Renderer>();
            if (r == null || !UsesFur(r)) continue;
            Mesh m = Baked(f.sharedMesh);
            if (m != null) f.sharedMesh = m;
        }
    }

    private static bool UsesFur(Renderer r)
    {
        foreach (Material m in r.sharedMaterials) if (m != null && m.shader == fur) return true;
        return false;
    }

    private static Mesh Baked(Mesh src)
    {
        if (src == null) return null;
        if (done.ContainsValue(src)) return null;           // already ours
        Mesh m;
        if (done.TryGetValue(src, out m)) return m;
        if (!src.isReadable) { done[src] = null; return null; }
        m = Instantiate(src);
        m.name = src.name;
        m.SetUVs(2, new List<Vector3>(src.vertices));
        m.SetUVs(3, new List<Vector3>(src.normals));
        done[src] = m;
        return m;
    }
}
