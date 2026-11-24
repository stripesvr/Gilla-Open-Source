// Editor-only health check for the reconstructed project (not part of the game).
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class GillaCheck
{
    public static void Run()
    {
        foreach (string scenePath in new[] { "Assets/Scenes/Loading.unity", "Assets/Scenes/Main.unity" })
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            int missing = all.Sum(g => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g));
            var renderers = all.SelectMany(g => g.GetComponents<Renderer>()).ToArray();
            var shaders = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.shader ? m.shader.name : "<null>")
                .GroupBy(s => s).OrderByDescending(g => g.Count()).Select(g => g.Key + " x" + g.Count());
            int behaviours = all.Sum(g => g.GetComponents<MonoBehaviour>().Count(b => b != null));
            Debug.Log("GILLACHECK scene=" + scenePath + " objects=" + all.Length + " renderers=" + renderers.Length +
                " behaviours=" + behaviours + " missingScripts=" + missing + " pipeline=" + (GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.name : "none"));
            Debug.Log("GILLACHECK shaders: " + string.Join("; ", shaders.Take(40)));

            Bounds b = new Bounds(); bool any = false;
            foreach (var r in renderers) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            Debug.Log("GILLACHECK bounds=" + b);
            if (!any) continue;
            var go = new GameObject("GillaCheckCam");
            var cam = go.AddComponent<Camera>();
            cam.farClipPlane = 5000f;
            for (int view = 0; view < 2; view++)
            {
                Vector3 dir = view == 0 ? new Vector3(0.6f, 0.7f, -0.6f).normalized : new Vector3(-0.2f, 0.25f, 1f).normalized;
                float dist = view == 0 ? b.extents.magnitude * 1.3f : b.extents.magnitude * 0.35f;
                cam.transform.position = b.center + dir * dist;
                cam.transform.LookAt(b.center);
                var rt = new RenderTexture(1600, 900, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                File.WriteAllBytes("D:/GillaUnity/shot_" + Path.GetFileNameWithoutExtension(scenePath) + "_" + view + ".png", tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null;
            }
            Object.DestroyImmediate(go);
        }
    }
}
