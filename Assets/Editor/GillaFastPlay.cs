// Redux: faster Play in the editor. Skips the script (domain) reload on entering Play mode; the
// game's code already resets its static state for this ([RuntimeInitializeOnLoadMethod] Forget()
// methods). Turn it off with Gilla > Fast Play if something carries over between plays.
using UnityEditor;

[InitializeOnLoad]
public static class GillaFastPlay
{
    // Off by default: skipping the reload froze Play on the map (state left over from the last
    // play). Opt in with Gilla > Fast Play.
    private const string Key = "GillaFastPlay.On";

    static GillaFastPlay()
    {
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        bool on = EditorPrefs.GetBool(Key, false);
        EditorSettings.enterPlayModeOptionsEnabled = on;
        EditorSettings.enterPlayModeOptions = on ? EnterPlayModeOptions.DisableDomainReload : EnterPlayModeOptions.None;
    }

    [MenuItem("Gilla/Fast Play")]
    private static void Toggle()
    {
        EditorPrefs.SetBool(Key, !EditorPrefs.GetBool(Key, false));
        Apply();
    }

    [MenuItem("Gilla/Fast Play", true)]
    private static bool ToggleCheck()
    {
        Menu.SetChecked("Gilla/Fast Play", EditorPrefs.GetBool(Key, false));
        return true;
    }
}
