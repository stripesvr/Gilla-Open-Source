// Editor-only: adjusts the Android manifest during the build (not part of the game).
// Unity asks for the microphone permission by itself every time the Microphone API is touched
// without it. Photon Voice retries the microphone whenever the app resumes, and the permission
// prompt pauses the app, so a denied permission turned into an endless pause/resume loop: the
// headset flipped between the game and passthrough several times a second.
// "unityplayer.SkipPermissionsDialog" turns Unity's automatic prompt off. The game still asks
// once itself (NetworkBootstrap.Start, MicLevel).
using System.IO;
using System.Xml;
using UnityEditor.Android;

public class GillaManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder { get { return 100; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifest)) return;
        const string android = "http://schemas.android.com/apk/res/android";
        var doc = new XmlDocument();
        doc.Load(manifest);
        XmlNode app = doc.SelectSingleNode("/manifest/application");
        if (app == null) return;
        foreach (XmlNode child in app.ChildNodes)
        {
            XmlAttribute name = child.Attributes != null ? child.Attributes["name", android] : null;
            if (child.Name == "meta-data" && name != null && name.Value == "unityplayer.SkipPermissionsDialog") return;
        }
        XmlElement meta = doc.CreateElement("meta-data");
        meta.SetAttribute("name", android, "unityplayer.SkipPermissionsDialog");
        meta.SetAttribute("value", android, "true");
        app.AppendChild(meta);
        doc.Save(manifest);
    }
}
