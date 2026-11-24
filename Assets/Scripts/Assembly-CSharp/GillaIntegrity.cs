// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

public class GillaIntegrity : MonoBehaviour
{
    public enum Verdict
    {
        Unchecked = 0,
        Checking = 1,
        Cleared = 2,
        Outdated = 3,
        Tampered = 4
    }

    [Serializable]
    public class Stamp
    {
        public string identifier;
        public string version;
        public string signer;
        public string commit;
        public string built;
        public int code;
        public int net;
    }

    [Serializable]
    public class VersionFile
    {
        public string latest;
        public string message;
        public int minimumBuild;
        public int netVersion;
        public string[] signers;
    }

    // Anti-cheat removed for the Gilla Redux test build.
    // The original checked the APK's signer and installer, looked for injected libraries
    // (frida, mod loaders, hooking frameworks) and compared the build against a version file
    // online, then refused to join rooms unless the verdict was Cleared. Here the verdict is
    // always Cleared and nothing is checked. The original is backed up outside the project,
    // in the removed/anticheat folder next to it.
    public static Verdict Result { get; private set; } = Verdict.Cleared;

    public static bool Done { get { return true; } }
    public static bool Cleared { get { return true; } }
    public static string Status { get; private set; } = "";
    public static string Codes { get; private set; } = "";
    public static Stamp Build { get; private set; }
    public static string Latest { get; private set; } = "";
    public static string Message { get; private set; } = "";

    public static void Begin()
    {
        if (Build == null)
        {
            TextAsset asset = Resources.Load<TextAsset>("GillaBuildStamp");
            if (asset != null) Build = JsonUtility.FromJson<Stamp>(asset.text);
        }
        Result = Verdict.Cleared;
    }

    public static IEnumerator WaitForVerdict()
    {
        Begin();
        yield break;
    }
}
