// Redux test helper: runs Play from the Loading scene in a batch-mode editor and quits after a
// set time, so start-up can be tested without someone pressing Play.
//   Unity.exe -batchmode -projectPath ... -executeMethod GillaPlayTest.Run -logFile play.log
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GillaPlayTest
{
    private static double quitAt;

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity");
        quitAt = EditorApplication.timeSinceStartup + 150.0;
        EditorApplication.update += Tick;
        Debug.Log("[GillaPlayTest] entering Play");
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup >= quitAt)
        {
            EditorApplication.update -= Tick;
            Debug.Log("[GillaPlayTest] time up, quitting");
            EditorApplication.Exit(0);
        }
    }
}
