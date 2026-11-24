// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public class GillaMeta : MonoBehaviour
{
    // Redux: the original game's Meta App ID was here. This build isn't entitled to it, so the
    // Platform SDK never initialized and threw an error every frame (lag, dropped connections).
    // Put your own Meta App ID here to turn Meta usernames on.
    public const string AppId = "";

    [RuntimeInitializeOnLoadMethod(0)]
    private static void Boot()
    {
        if (UnityEngine.Application.isEditor)
            return;
        if (string.IsNullOrEmpty(AppId))
        {
            Debug.Log("[GillaMeta] no App ID yet: names are GILLA and four digits");
            return;
        }
        if (UnityEngine.Object.FindAnyObjectByType<GillaMeta>() != null)
            return;
        GameObject go = new GameObject("GillaMeta");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<GillaMeta>();
    }

    private void Start()
    {
        Core.AsyncInitialize(AppId, null).OnComplete(init =>
        {
            if (init.IsError)
            {
                Debug.LogWarning("[GillaMeta] the Platform SDK didn't start: " + init.GetError().Message);
                return;
            }
            Entitlements.IsUserEntitledToApplication().OnComplete(entitled =>
            {
                if (entitled.IsError)
                {
                    Debug.LogWarning("[GillaMeta] this account isn't entitled to the game: " + entitled.GetError().Message);
                    return;
                }
                Users.GetLoggedInUser().OnComplete(user =>
                {
                    if (user.IsError || user.Data == null)
                    {
                        string suffix = "";
                        if (user.IsError)
                            suffix = ": " + user.GetError().Message;
                        Debug.LogWarning("[GillaMeta] couldn't read the logged-in user" + suffix);
                        return;
                    }
                    string name = string.IsNullOrEmpty(user.Data.OculusID) ? user.Data.DisplayName : user.Data.OculusID;
                    Debug.Log("[GillaMeta] logged in as " + name);
                    ModMenuAccess.MetaUserName = name;
                    GillaName.SetMeta(name);
                    ModMenu.AttachIfOwner();
                    GillaCloud.MetaUser(user.Data.ID.ToString(), done =>
                    {
                        Users.GetUserProof().OnComplete(proof =>
                        {
                            string value = null;
                            if (!proof.IsError && proof.Data != null)
                                value = proof.Data.Value;
                            done(value);
                        });
                    });
                });
            });
        });
    }

    private void Update()
    {
        Request.RunCallbacks(0);
    }
}
