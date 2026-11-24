// Redux diagnostic: logs how long the map takes to open, in stages, so a slow or stuck Play can
// be pinned down from the log. Safe to delete.
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(30000)]
public class GillaLoadTimer : MonoBehaviour
{
    private static float clickedAt = -1f;
    private int frames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        clickedAt = Time.realtimeSinceStartup;
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
    }

    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(string.Format("[GillaLoadTimer] scene '{0}' finished loading at {1:F1} s", scene.name, Time.realtimeSinceStartup - clickedAt));
        if (scene.name == "Main")
        {
            GameObject go = new GameObject("GillaLoadTimer");
            go.AddComponent<GillaLoadTimer>();
        }
    }

    private void Update()
    {
        frames++;
        if (frames == 1 || frames == 60)
        {
            Debug.Log(string.Format("[GillaLoadTimer] map frame {0} at {1:F1} s", frames, Time.realtimeSinceStartup - clickedAt));
        }
        if (frames >= 60)
        {
            Destroy(gameObject);
        }
    }
}
