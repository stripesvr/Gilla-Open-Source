// Redux addition (not in the original game): song search for the shop boombox.
// Searching happens on the GillaOS computer's MUSIC page. Two free, legal sources:
//   - Deezer: almost any released song, but only its official 30-second preview.
//   - Audius: full-length songs, mostly from independent artists.
// The chosen song is sent to everyone in the room (NetworkApe.RPC_Song); each player streams it
// from the source and hears it from the boombox, wherever it has been carried.
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GillaMusic : MonoBehaviour
{
    public struct Result
    {
        public string title;
        public string artist;
        public string url;
        public bool full;
        public int seconds;
        public string art;            // album cover url ("" when there is none)
        public string Label { get { return (artist + " - " + title).ToUpperInvariant(); } }
    }

    [Serializable] private class DeezerArtist { public string name; }
    [Serializable] private class DeezerAlbum { public string cover_medium; }
    [Serializable] private class DeezerTrack { public string title; public string preview; public int duration; public DeezerArtist artist; public DeezerAlbum album; }
    [Serializable] private class DeezerReply { public DeezerTrack[] data; }
    [Serializable] private class AudiusUser { public string name; }
    [Serializable] private class AudiusTrack { public string id; public string title; public int duration; public bool is_streamable; public AudiusUser user; }
    [Serializable] private class AudiusReply { public AudiusTrack[] data; }

    private const string AppName = "GillaRedux";
    private static GillaMusic instance;

    public static readonly List<Result> Results = new List<Result>();
    public static string Status = "";
    public static bool Searching { get; private set; }
    public static string NowPlaying { get; private set; } = "";
    public static string NowArt { get; private set; } = "";
    public static bool Playing
    {
        get { return instance != null && instance.source != null && (instance.source.isPlaying || instance.loading); }
    }
    public static event Action Changed;

    private AudioSource source;
    private bool loading;
    private int ticket;

    private static GillaMusic Get()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("GillaMusic");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GillaMusic>();
        }
        return instance;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Forget()
    {
        instance = null;
        Results.Clear();
        Status = "";
        Searching = false;
        NowPlaying = "";
        Changed = null;
    }

    // ---- search ----

    public static void Search(string query)
    {
        query = (query ?? "").Trim();
        if (query.Length == 0) return;
        Get().StartCoroutine(Get().SearchBoth(query));
    }

    private IEnumerator SearchBoth(string query)
    {
        Searching = true;
        Results.Clear();
        Status = "SEARCHING...";
        Changed?.Invoke();
        string q = UnityWebRequest.EscapeURL(query);

        using (UnityWebRequest req = UnityWebRequest.Get("https://api.deezer.com/search?limit=3&q=" + q))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                DeezerReply r = Parse<DeezerReply>(req.downloadHandler.text);
                if (r != null && r.data != null)
                {
                    foreach (DeezerTrack t in r.data)
                    {
                        if (t == null || string.IsNullOrEmpty(t.preview)) continue;
                        Results.Add(new Result { title = t.title, artist = t.artist != null ? t.artist.name : "", url = t.preview, full = false, seconds = 30, art = t.album != null ? t.album.cover_medium : "" });
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GillaMusic] Deezer search failed: " + req.error);
            }
        }

        using (UnityWebRequest req = UnityWebRequest.Get("https://api.audius.co/v1/tracks/search?app_name=" + AppName + "&query=" + q))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                AudiusReply r = Parse<AudiusReply>(req.downloadHandler.text);
                if (r != null && r.data != null)
                {
                    int added = 0;
                    foreach (AudiusTrack t in r.data)
                    {
                        if (t == null || string.IsNullOrEmpty(t.id) || !t.is_streamable) continue;
                        Results.Add(new Result
                        {
                            title = t.title,
                            artist = t.user != null ? t.user.name : "",
                            url = "https://api.audius.co/v1/tracks/" + t.id + "/stream?app_name=" + AppName,
                            full = true,
                            art = "",
                            seconds = t.duration
                        });
                        if (++added >= 2) break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GillaMusic] Audius search failed: " + req.error);
            }
        }

        Searching = false;
        Status = Results.Count > 0 ? "PRESS 1-" + Results.Count + " TO PLAY" : "NOTHING FOUND";
        Changed?.Invoke();
    }

    private static T Parse<T>(string json) where T : class
    {
        try { return JsonUtility.FromJson<T>(json); }
        catch (Exception e) { Debug.LogWarning("[GillaMusic] couldn't read the reply: " + e.Message); return null; }
    }

    // ---- album covers ----

    private static readonly Dictionary<string, Texture2D> covers = new Dictionary<string, Texture2D>();
    private static readonly HashSet<string> fetching = new HashSet<string>();

    // The cover for a url, or null while it downloads (Changed fires when it arrives).
    public static Texture2D Cover(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        Texture2D t;
        if (covers.TryGetValue(url, out t)) return t;
        if (fetching.Add(url)) Get().StartCoroutine(Get().FetchCover(url));
        return null;
    }

    private IEnumerator FetchCover(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();
            fetching.Remove(url);
            if (req.result == UnityWebRequest.Result.Success)
            {
                covers[url] = DownloadHandlerTexture.GetContent(req);
                Changed?.Invoke();
            }
        }
    }

    // ---- playing ----

    // Called on the computer: tell the whole room.
    public static void Pick(int index)
    {
        if (index < 0 || index >= Results.Count) return;
        Result r = Results[index];
        string label = r.Label;
        if (label.Length > 60) label = label.Substring(0, 60);
        if (!string.IsNullOrEmpty(r.art)) label = label + "\n" + r.art;   // the cover travels with the label
        NetworkApe me = NetworkApe.Local;
        if (me != null && me.Object != null && me.Object.IsValid && me.Runner != null && me.Runner.IsRunning)
        {
            me.RPC_Song(r.url, label);
        }
        else
        {
            Play(r.url, label);
        }
    }

    // Redux: loop the song for the whole room
    public static bool Loop { get; private set; }

    public static void LoopForAll(bool on)
    {
        NetworkApe me = NetworkApe.Local;
        if (me != null && me.Object != null && me.Object.IsValid && me.Runner != null && me.Runner.IsRunning)
        {
            me.RPC_MusicLoop(on);
        }
        else
        {
            SetLoop(on);
        }
    }

    // Runs on every player.
    public static void SetLoop(bool on)
    {
        Loop = on;
        GillaMusic m = Get();
        if (m.source != null) m.source.loop = on;
        Changed?.Invoke();
    }

    public static void StopForAll()
    {
        NetworkApe me = NetworkApe.Local;
        if (me != null && me.Object != null && me.Object.IsValid && me.Runner != null && me.Runner.IsRunning)
        {
            me.RPC_Song("", "");
        }
        else
        {
            Play("", "");
        }
    }

    // Runs on every player when a song is picked ("" stops it).
    public static void Play(string url, string label)
    {
        GillaMusic m = Get();
        m.ticket++;
        if (m.source != null) m.source.Stop();
        if (string.IsNullOrEmpty(url))
        {
            NowPlaying = "";
            NowArt = "";
            m.loading = false;
            Changed?.Invoke();
            return;
        }
        int cut = label.IndexOf('\n');
        NowArt = cut >= 0 ? label.Substring(cut + 1) : "";
        NowPlaying = cut >= 0 ? label.Substring(0, cut) : label;
        Changed?.Invoke();
        m.StartCoroutine(m.Load(url, m.ticket));
    }

    private IEnumerator Load(string url, int mine)
    {
        loading = true;
        using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
            req.timeout = 30;
            yield return req.SendWebRequest();
            if (mine != ticket) yield break;
            loading = false;
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[GillaMusic] couldn't load the song: " + req.error);
                Status = "COULDN'T PLAY THAT ONE";
                NowPlaying = "";
                Changed?.Invoke();
                yield break;
            }
            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null) yield break;
            EnsureSource();
            source.clip = clip;
            source.loop = Loop;
            source.Play();
            Debug.Log("[GillaMusic] playing " + NowPlaying);
        }
    }

    private void EnsureSource()
    {
        if (source != null) return;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = Loop;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 25f;
        source.dopplerLevel = 0f;
        source.volume = 0.6f;
    }

    private void Update()
    {
        if (source == null) return;
        GillaBoombox box = GillaBoombox.Instance;
        if (box != null)
        {
            transform.position = box.transform.position;   // the song comes out of the boombox
            source.mute = box.Muted;
        }
        if (!loading && !source.isPlaying && NowPlaying.Length > 0 && source.clip != null && source.time == 0f)
        {
            NowPlaying = "";   // finished
            Changed?.Invoke();
        }
    }
}
