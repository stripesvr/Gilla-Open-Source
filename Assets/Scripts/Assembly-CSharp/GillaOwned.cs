// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public static class GillaOwned
{
    private class Seen
    {
        public HashSet<string> names;
        public bool unpublished;
        public float at;
        public float asked;
        public bool pending;
    }

    public const string Key = "Shown";
    public const string Tag = "v1:";
    public static float Again = 15f;
    public static float Stale = 600f;
    private static readonly Dictionary<string, Seen> seen = new Dictionary<string, Seen>();
    private static bool? live;
    private static bool askingLive;
    public static Action<string, Action<string>, Action<string>> Read = ReadFromPlayFab;
    public static Func<bool?> Checking = CheckingNow;

    public static int Version { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Forget()
    {
        seen.Clear();
        live = null;
        askingLive = false;
        Version = 0;
        Read = ReadFromPlayFab;
        Checking = CheckingNow;
    }

    public static int Filter(byte[] said, string account, bool trusted, byte[] shown)
    {
        bool? checking = Checking();
        int count = 0;
        for (int i = 0; i < said.Length; i++)
        {
            if (i >= shown.Length)
            {
                return count;
            }
            byte id = said[i];
            bool allowed = checking != true;
            if (id == 0)
            {
                allowed = true;
            }
            if (id != 0 && checking == true && trusted)
            {
                allowed = Allows(account, id);
            }
            shown[i] = allowed ? id : (byte)0;
            if (!allowed)
            {
                count++;
            }
        }
        return count;
    }

    public static bool Allows(string account, byte id)
    {
        var entry = CosmeticCatalog.Of(id);
        if (entry == null)
        {
            return false;
        }
        if (string.IsNullOrEmpty(account))
        {
            return false;
        }
        float now = Time.unscaledTime;
        Seen s;
        bool ok;
        if (seen.TryGetValue(account, out s))
        {
            if (s.names != null)
            {
                if (s.names.Contains(entry.name))
                {
                    ok = true;
                }
                else if (string.IsNullOrEmpty(entry.comesWith))
                {
                    ok = false;
                }
                else
                {
                    ok = s.names.Contains(entry.comesWith);
                }
                if (s.pending)
                {
                    return ok;
                }
                if (now - s.at < Stale)
                {
                    if (ok)
                    {
                        return true;
                    }
                    if (now - s.asked < Again)
                    {
                        return false;
                    }
                }
                Ask(account, s);
                return ok;
            }
            if (s.pending)
            {
                return false;
            }
            if (now - s.asked < Again)
            {
                return false;
            }
        }
        else
        {
            s = new Seen();
            seen[account] = s;
        }
        ok = false;
        Ask(account, s);
        return ok;
    }

    public static HashSet<string> Known(string account)
    {
        Seen s;
        if (seen.TryGetValue(account, out s))
        {
            return s.names;
        }
        return null;
    }

    private static void Ask(string account, Seen s)
    {
        s.pending = true;
        s.asked = Time.unscaledTime;
        Read(account,
            value =>
            {
                s.pending = false;
                s.at = Time.unscaledTime;
                s.unpublished = value == null;
                s.names = Parse(value);
                Version = Version + 1;
            },
            error =>
            {
                s.pending = false;
                Debug.LogWarning("[GillaOwned] couldn't read what " + account + " owns: " + error);
            });
    }

    public static HashSet<string> Parse(string value)
    {
        HashSet<string> set = new HashSet<string>();
        if (!string.IsNullOrEmpty(value))
        {
            if (value.StartsWith(Tag))
            {
                string[] parts = value.Substring(Tag.Length).Split(',', StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (part.Length > 0)
                    {
                        set.Add(part.Trim());
                    }
                }
            }
        }
        return set;
    }

    private static bool? CheckingNow()
    {
        if (!GillaCloud.SignedIn)
        {
            return new bool?(false);
        }
        if (!live.HasValue && !askingLive)
        {
            askingLive = true;
            ReadFromPlayFab(null,
                value =>
                {
                    live = new bool?(value != null);
                    askingLive = false;
                    Version = Version + 1;
                },
                error =>
                {
                    askingLive = false;
                });
        }
        return live;
    }

    private static void ReadFromPlayFab(string account, Action<string> done, Action<string> failed)
    {
        GetUserDataRequest request = new GetUserDataRequest();
        List<string> keys = new List<string>();
        keys.Add(Key);
        request.Keys = keys;
        if (account != null)
        {
            request.PlayFabId = account;
        }
        PlayFabClientAPI.GetUserReadOnlyData(request,
            result =>
            {
                UserDataRecord record;
                string v = null;
                if (result.Data != null && result.Data.TryGetValue(Key, out record))
                {
                    v = record.Value;
                }
                done(v);
            },
            error => failed(error.GenerateErrorReport()),
            null, null);
    }
}
