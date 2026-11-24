// Editor-only play-mode smoke test for the reconstructed project (not part of the game).
// Unity.exe -projectPath ... -executeMethod GillaPlay.Run   (no -batchmode: play mode needs a window)
// Plays the first scene for GILLA_PLAY_SECONDS (default 45), writes every console message to
// D:/GillaUnity/play_log.txt and a Game-view capture to D:/GillaUnity/play_shot.png, then quits.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GillaPlay
{
    const string LogPath = "D:/GillaUnity/play_log.txt";
    const string Key = "GillaPlay.active";
    static double playStart = -1;
    static bool shot;

    static GillaPlay()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceivedThreaded += OnLog;
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        File.WriteAllText(LogPath, "");
        SessionState.SetBool(Key, true);
        string scene = Environment.GetEnvironmentVariable("GILLA_PLAY_SCENE");
        EditorSceneManager.OpenScene(string.IsNullOrEmpty(scene) ? "Assets/Scenes/Loading.unity" : scene, OpenSceneMode.Single);
        Application.logMessageReceivedThreaded += OnLog;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    static void OnLog(string message, string stack, LogType type)
    {
        try
        {
            string first = stack == null ? "" : stack.Split('\n')[0];
            lock (LogPath) File.AppendAllText(LogPath, "[" + type + "] " + message.Replace("\n", " | ") + "  @ " + first + "\n");
        }
        catch { }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (playStart < 0) playStart = EditorApplication.timeSinceStartup;
        double t = EditorApplication.timeSinceStartup - playStart;
        double limit = 45;
        double.TryParse(Environment.GetEnvironmentVariable("GILLA_PLAY_SECONDS"), out double env);
        if (env > 0) limit = env;
        if (!shot && t > limit - 6)
        {
            shot = true;
            int n = 0;
            foreach (Camera cam in Camera.allCameras)
            {
                var rt = new RenderTexture(1600, 900, 24);
                var old = cam.targetTexture;
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                File.WriteAllBytes("D:/GillaUnity/play_shot_" + (n++) + ".png", tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = old;
                OnLog("GILLAPLAY camera " + cam.name + " at " + cam.transform.position, "", LogType.Log);
            }
            OnLog("GILLAPLAY active scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                  " cameras=" + Camera.allCamerasCount + " time=" + Time.time, "", LogType.Log);
        }
        if (t > limit)
        {
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(0);
        }
    }
}
