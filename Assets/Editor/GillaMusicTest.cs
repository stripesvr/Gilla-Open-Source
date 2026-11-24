// Redux test helper: in a batch-mode Play, searches a song, plays the first result and logs
// whether audio arrived and the boombox was set up for carrying.
//   Unity.exe -batchmode -projectPath ... -executeMethod GillaMusicTest.Run -logFile music.log
// Entering Play reloads scripts, so the test keeps its state in SessionState and re-hooks itself.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GillaMusicTest
{
    private const string On = "GillaMusicTest.On";
    private const string Step = "GillaMusicTest.Step";
    private static float playStart = -1f;

    static GillaMusicTest()
    {
        if (SessionState.GetBool(On, false))
        {
            EditorApplication.update += Tick;
        }
    }

    public static void Run()
    {
        SessionState.SetBool(On, true);
        SessionState.SetInt(Step, 0);
        EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity");
        EditorApplication.EnterPlaymode();
    }

    private static void Log(string s) { Debug.Log("[GillaMusicTest] " + s); }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (playStart < 0f) playStart = Time.realtimeSinceStartup;
        float t = Time.realtimeSinceStartup - playStart;
        int step = SessionState.GetInt(Step, 0);
        if (step == 0 && GillaBoombox.Instance != null && t > 55f)
        {
            step = 1;
            Log("boombox found, grab attached: " + (GillaBoomboxGrab.Instance != null) + "; glass shader: " + (Shader.Find("Gilla/Glass") != null));
            GillaMusic.Search("blinding lights");
        }
        else if (step == 1 && !GillaMusic.Searching && GillaMusic.Results.Count > 0)
        {
            step = 2;
            foreach (GillaMusic.Result r in GillaMusic.Results) Log("result: " + r.Label + (r.full ? " (full)" : " (30s)"));
            GillaMusic.Pick(0);
        }
        else if (step == 2 || step == 4)
        {
            GillaMusic m = Object.FindAnyObjectByType<GillaMusic>();
            AudioSource s = m != null ? m.GetComponent<AudioSource>() : null;
            if (s != null && s.isPlaying && s.clip != null && s.time > 0.5f)
            {
                Log("PLAYING " + GillaMusic.NowPlaying + " (" + s.clip.length.ToString("F0") + " s clip, at " + s.time.ToString("F1") + " s)");
                int full = GillaMusic.Results.FindIndex(r => r.full);
                if (step == 2 && full >= 0) { GillaMusic.Pick(full); step = 4; }
                else step = 5;
            }
        }
        SessionState.SetInt(Step, step);
        if (step == 5 || t > 200f)
        {
            Log("done at step " + step + " after " + t.ToString("F0") + " s, status '" + GillaMusic.Status + "'");
            SessionState.SetBool(On, false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
    }
}
