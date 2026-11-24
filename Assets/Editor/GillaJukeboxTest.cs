// Redux test helper: checks the jukebox can be carried whole and photographs its MUSIC panel.
//   Unity.exe -batchmode -projectPath ... -executeMethod GillaJukeboxTest.Run -logFile jukebox.log
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GillaJukeboxTest
{
    private const string On = "GillaJukeboxTest.On";
    private static float playStart = -1f;
    private static float pickedAt = -1f;
    private static GillaItem grabbing;
    private static float grabAt = -1f;
    private static int step;

    static GillaJukeboxTest()
    {
        if (SessionState.GetBool(On, false)) EditorApplication.update += Tick;
    }

    public static void Run()
    {
        SessionState.SetBool(On, true);
        EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void Log(string s) { Debug.Log("[GillaJukeboxTest] " + s); }

    private static void Shot(string file, Vector3 from, Vector3 at)
    {
        GameObject go = new GameObject("TestCam");
        Camera cam = go.AddComponent<Camera>();
        cam.transform.position = from;
        cam.transform.LookAt(at);
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.02f;
        RenderTexture rt = new RenderTexture(900, 900, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(900, 900, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
        tex.Apply();
        File.WriteAllBytes(file, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(go);
        Log("saved " + file);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (playStart < 0f) playStart = Time.realtimeSinceStartup;
        float t = Time.realtimeSinceStartup - playStart;
        GillaBoombox box = GillaBoombox.Instance;
        if (step == 0 && box != null && t > 55f)
        {
            step = 1;
            Transform b = box.transform;
            Renderer[] rs = b.GetComponentsInChildren<Renderer>();
            int batched = 0;
            foreach (Renderer r in rs) if (r.isPartOfStaticBatch) { batched++; Log("still combined: " + r.name); }
            Log(rs.Length + " renderers on the jukebox, " + batched + " still combined; lamp left behind: " + (b.Find("Gilla_ShopLampSmall") == null));
            Vector3 front = b.rotation * Vector3.back;
            Shot("D:/GillaUnity/jukebox_shot.png", b.position + front * 0.7f + Vector3.up * 1.75f, b.position + Vector3.up * 1.15f);

            // carry it 2 m sideways and see every piece follow
            Vector3[] before = new Vector3[rs.Length];
            for (int i = 0; i < rs.Length; i++) before[i] = rs[i].bounds.center;
            b.position += b.right * 2f;
            int moved = 0;
            for (int i = 0; i < rs.Length; i++) if ((rs[i].bounds.center - before[i]).magnitude > 1.9f) moved++;
            Log("moved 2 m: " + moved + " of " + rs.Length + " pieces followed");
            b.position -= b.right * 2f;

            // open the panel as if the MUSIC button were pressed
            GillaMusicPanel p = Object.FindAnyObjectByType<GillaMusicPanel>();
            if (p == null) { Log("no music panel"); }
            else
            {
                typeof(GillaMusicPanel).GetMethod("Toggle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, null);
                // the MUSIC key must remember its new spot (not MUTE's)
                Transform mk = b.Find("MusicKey");
                if (mk != null)
                {
                    PressKey pk = mk.GetComponent<PressKey>();
                    Vector3 rest = (Vector3)typeof(PressKey).GetField("rest", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pk);
                    Log("MUSIC key rest " + rest.ToString("F3") + " vs where it is " + mk.localPosition.ToString("F3") + " (MUTE at " + b.Find("Mute").localPosition.ToString("F3") + ")");
                }
                // a mouse grab must put the jukebox's middle on the hand point
                object you = typeof(GillaHands).GetField("you", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                GillaItem it = b.GetComponent<GillaItem>();
                if (you != null && it != null)
                {
                    typeof(GillaHands).GetMethod("Take", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(you, new object[] { 2, it });
                    grabbing = it;
                }
                else Log("no hands to test the grab with");
                Log("screen toggled open; item on jukebox: " + (b.GetComponent<GillaItem>() != null) + ", colliders " + b.GetComponentsInChildren<Collider>().Length);
                GillaMusic.Search("blinding lights");
            }
        }
        if (step == 1 && !GillaMusic.Searching && GillaMusic.Results.Count > 0 && pickedAt < 0f)
        {
            GillaMusic.Pick(0);
            pickedAt = t;
        }
        if (grabbing != null && t > grabAt + 3f && grabAt > 0f)
        {
            Bounds gb = grabbing.GetComponent<Collider>().bounds;
            Log("held by mouse: middle is " + (gb.center - GillaHands.HandPoint(2)).magnitude.ToString("F2") + " m from the hand point");
            object you = typeof(GillaHands).GetField("you", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            typeof(GillaHands).GetMethod("LetGo", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(you, new object[] { 2, false });
            grabbing = null;
        }
        if (grabbing != null && grabAt < 0f) grabAt = t;
        if (step == 1)
        {
            // the test player is far from the shop, which would close the screen: hold it open
            GillaMusicPanel mp = Object.FindAnyObjectByType<GillaMusicPanel>();
            if (mp != null) typeof(GillaMusicPanel).GetField("open", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mp, true);
        }
        if (step == 1 && pickedAt >= 0f && t > pickedAt + 10f)
        {
            step = 2;
            Transform b = box.transform;
            Vector3 front = b.rotation * Vector3.back;
            Vector3 top = b.position + Vector3.up * 1.35f;
            Log("now playing: " + GillaMusic.NowPlaying + ", cover " + (GillaMusic.NowArt.Length > 0));
            Shot("D:/GillaUnity/musicpanel_shot.png", top + front * 1.05f + Vector3.up * 0.45f, top + Vector3.up * 0.3f);
            Shot("D:/GillaUnity/musicpanel_side.png", top + front * 0.8f + b.right * 0.8f + Vector3.up * 0.2f, top + Vector3.up * 0.1f);
            Shot("D:/GillaUnity/musicpanel_back.png", top - front * 1.0f + Vector3.up * 0.4f, top + Vector3.up * 0.3f);
            foreach (SkinnedMeshRenderer sk in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
            {
                if (sk.sharedMaterial == null || sk.sharedMaterial.shader.name != "Gilla/ApeFur" || !sk.isVisible && false) continue;
                Bounds ab = sk.bounds;
                Log("fur on " + sk.name + " uv2 " + (sk.sharedMesh.uv3 != null && sk.sharedMesh.uv3.Length > 0));
                Shot("D:/GillaUnity/ape_shot.png", ab.center + sk.transform.forward * 0.9f + Vector3.up * 0.2f, ab.center);
                break;
            }
        }
        if (step == 2 || t > 200f)
        {
            Log("done at step " + step);
            SessionState.SetBool(On, false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
    }
}
