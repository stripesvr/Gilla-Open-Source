// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class MonsterDirector : MonoBehaviour
{
    [Serializable]
    public class Spawn
    {
        public GameObject prefab;
        public Vector3 position;
        public float yaw;
    }

    public MonsterDirector.Spawn[] spawns = new MonsterDirector.Spawn[0];
    [Tooltip("One killed (by dynamite: MonsterBrain.Blasted) is back on its spot this long after (s).")]
    public float backAfter = 120f;
    private const float CheckEvery = 2;
    private const float Margin = 5;
    private const float FallBelow = 40;
    private const int MostEach = 3;
    public static float LostAfter = 4f;
    public static float DeadTooLong = 30f;
    public static float FallbackAfter = 150f;
    public static float FallbackStagger = 10f;
    private static readonly Dictionary<string, float> deadTill = new Dictionary<string, float>();
    private readonly List<GameObject> local = new List<GameObject>();
    private readonly Dictionary<string, int> inRoom = new Dictionary<string, int>();
    private readonly Dictionary<MonsterNet, float> lostSince = new Dictionary<MonsterNet, float>();
    private readonly Dictionary<MonsterNet, float> deadSince = new Dictionary<MonsterNet, float>();
    private readonly HashSet<string> takenOut = new HashSet<string>();
    private readonly Dictionary<int, float> missingSince = new Dictionary<int, float>();
    private float offlineFor;
    private float nextCheck;
    private float joinedAt;
    private float fallbackQuietTill;
    private NetworkRunner joinedRunner;
    private bool toldNoMaster;
    private bool lookedForArea;
    private bool haveArea;
    private bool toldCrowded;
    private Bounds baseArea;

    private void Update()
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (boot != null)
        {
            runner = boot.Runner /* ? */;
        }
        if (runner != null && runner.IsRunning)
        {
            offlineFor = 0f;
            ClearLocal();
            if (boot.Joining) return;
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 2f;
            if (runner != joinedRunner)
            {
                joinedRunner = runner;
                joinedAt = Time.unscaledTime;
                toldCrowded = false;
                toldNoMaster = false;
                lostSince.Clear();
                deadSince.Clear();
                missingSince.Clear();
                fallbackQuietTill = 0f;
            }
            bool settling;
            if (boot.Kind == NetworkBootstrap.Lobby.Regular)
            {
                settling = Time.unscaledTime - joinedAt < 5f;
            }
            else
            {
                settling = true;
            }
            if (!runner.IsSharedModeMasterClient)
            {
                if (!toldNoMaster && 8f < Time.unscaledTime - joinedAt)
                {
                    if (!AnyLive())
                    {
                        toldNoMaster = true;
                        Debug.Log("[MonsterDirector] no monsters in the room: they're the master client's to spawn");
                    }
                }
                if (settling) return;
                Fallback(runner);
                return;
            }
            missingSince.Clear();
            if (settling) return;
            TakeOutLost(runner);
            MakeMissing(runner);
            return;
        }

        joinedRunner = null;
        toldNoMaster = false;
        if (boot != null && boot.Joining)
        {
            offlineFor = 0f;
            return;
        }
        offlineFor += Time.deltaTime;
        if (local.Count < 1)
        {
            float wait = boot != null ? 2f : 0f;
            if (wait <= offlineFor)
            {
                foreach (Spawn s in spawns)
                {
                    if (s.prefab != null)
                    {
                        GameObject go = Instantiate(s.prefab, s.position, Quaternion.Euler(0f, s.yaw, 0f));
                        local.Add(go);
                    }
                }
            }
        }
    }

    private static bool Master(NetworkRunner runner)
    {
        if (runner.GameMode == GameMode.Shared && runner.IsClient)
        {
            return runner.IsSharedModeMasterClient /* ? */;
        }
        return false;
    }

    private void ClearLocal()
    {
        foreach (GameObject go in local)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        local.Clear();
    }

    private static bool AnyLive()
    {
        MonsterNet[] all = FindObjectsByType<MonsterNet>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].Live) return true;
        }
        return false;
    }

    private static string Kind(MonsterNet m)
    {
        return m.name.Replace("(Clone)", "").Trim();
    }

    public static void Killed(string name)
    {
        MonsterDirector director = FindAnyObjectByType<MonsterDirector>();
        string kind = name.Replace("(Clone)", "").Trim();
        float now = Time.time;
        float after = director != null ? director.backAfter : 120f;
        deadTill[kind] = now + after;
    }

    [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
    private static void Forget()
    {
        deadTill.Clear();
    }

    private void MakeMissing(NetworkRunner runner)
    {
        inRoom.Clear();
        MonsterNet[] all = FindObjectsByType<MonsterNet>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            MonsterNet m = all[i];
            if (m.Live)
            {
                string kind = Kind(m);
                int n;
                inRoom[kind] = inRoom.TryGetValue(kind, out n) ? n + 1 : 1;
            }
        }
        int missing = 0;
        int spawned = 0;
        for (int i = 0; i < spawns.Length; i++)
        {
            Spawn s = spawns[i];
            if (s.prefab == null) continue;
            int have;
            if (inRoom.TryGetValue(s.prefab.name, out have) && 0 < have)
            {
                inRoom[s.prefab.name] = have - 1;
            }
            else
            {
                float till;
                if (!deadTill.TryGetValue(s.prefab.name, out till) || till <= Time.time)
                {
                    if (!takenOut.Contains(s.prefab.name))
                    {
                        missing++;
                        if (SpawnOne(runner, s)) spawned++;
                    }
                }
            }
        }
        if (0 < spawned)
        {
            Debug.Log(string.Format("[MonsterDirector] spawned {0} of the {1} missing monsters on the network in '{2}', as its master client", spawned, missing, runner.SessionInfo.Name));
        }
    }

    private bool SpawnOne(NetworkRunner runner, MonsterDirector.Spawn s)
    {
        NetworkObject prefabObj;
        if (s.prefab.TryGetComponent<NetworkObject>(out prefabObj))
        {
            NetworkObject spawned = runner.Spawn(prefabObj, s.position, Quaternion.Euler(0f, s.yaw, 0f), null, null, (NetworkSpawnFlags)2);
            if (spawned != null)
            {
                return true;
            }
            Debug.LogError(string.Format("[MonsterDirector] the network didn't take {0}: trying again in {1:0} s", s.prefab.name, 2f));
        }
        else
        {
            Debug.LogError("[MonsterDirector] " + s.prefab.name + " has no NetworkObject, so it can't go on the network");
        }
        return false;
    }

    private void TakeOutLost(NetworkRunner runner)
    {
        takenOut.Clear();
        float now = Time.unscaledTime;
        MonsterNet[] all = FindObjectsByType<MonsterNet>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            MonsterNet m = all[i];
            if (!m.Live) continue;
            string why = LostWhy(m, now);
            if (why == null)
            {
                lostSince.Remove(m);
                continue;
            }
            float since;
            if (lostSince.TryGetValue(m, out since))
            {
                bool killing = m.brain != null && m.brain.state == MonsterBrain.State.Kill;
                float extra = killing ? 20f : 0f;
                NetworkObject obj = m.Object;
                if (LostAfter + extra <= now - since && obj != null && obj.IsValid /* ? */ && obj.HasStateAuthority /* ? */)
                {
                    string kind = Kind(m);
                    Debug.LogWarning(string.Format("[MonsterDirector] {0} was lost ({1}, at {2:F1}) in '{3}': taken out, back on its spot at the next check", kind, why, m.transform.position, runner.SessionInfo.Name));
                    lostSince.Remove(m);
                    deadSince.Remove(m);
                    takenOut.Add(kind);
                    runner.Despawn(m.Object);
                }
            }
            else
            {
                lostSince[m] = now;
            }
        }
        Prune(lostSince);
        Prune(deadSince);
    }

    private static void Prune(Dictionary<MonsterNet, float> since)
    {
        if (since.Count == 0) return;
        List<MonsterNet> gone = null;
        foreach (MonsterNet m in since.Keys)
        {
            if (m == null || !m.Live)
            {
                if (gone == null) gone = new List<MonsterNet>();
                gone.Add(m);
            }
        }
        if (gone != null)
        {
            foreach (MonsterNet m in gone)
            {
                since.Remove(m);
            }
        }
    }

    private string LostWhy(MonsterNet m, float now)
    {
        Vector3 pos = m.transform.position;
        if (!Finite(pos.x) || !Finite(pos.y) || !Finite(pos.z))
        {
            return "nowhere: its position isn't a number";
        }
        Bounds area;
        if (Area(out area))
        {
            if (!area.Contains(pos))
            {
                if (area.min.y <= pos.y)
                {
                    return "out of the base";
                }
                return "fallen out under the map";
            }
        }
        else
        {
            if (!(LowestSpot() - 40f <= pos.y))
            {
                return "fallen out under the map";
            }
        }
        if (m.brain != null && m.brain.Dead)
        {
            float since;
            if (deadSince.TryGetValue(m, out since))
            {
                if (now - since <= DeadTooLong)
                {
                    return null;
                }
                return string.Format("dead {0:0} s and never taken out", now - since);
            }
            deadSince[m] = now;
            return null;
        }
        deadSince.Remove(m);
        return null;
    }

    private static bool Finite(float f)
    {
        return !float.IsNaN(f) && !float.IsInfinity(f);
    }

    private bool Area(out Bounds area)
    {
        if (!lookedForArea)
        {
            lookedForArea = true;
            haveArea = GillaPlaces.AreaNamed("the base", out baseArea);
            if (haveArea)
            {
                baseArea.Expand(10f);
                for (int i = 0; i < spawns.Length; i++)
                {
                    Spawn s = spawns[i];
                    if (s.prefab != null && !baseArea.Contains(s.position))
                    {
                        haveArea = false;
                        Debug.LogWarning(string.Format("[MonsterDirector] {0}'s spot is outside the base's area ({1:F0}..{2:F0}): only falling under the map counts as lost here", s.prefab.name, baseArea.min, baseArea.max));
                        break;
                    }
                }
            }
        }
        area = baseArea;
        return haveArea;
    }

    private float LowestSpot()
    {
        float lowest = float.PositiveInfinity;
        for (int i = 0; i < spawns.Length; i++)
        {
            Spawn s = spawns[i];
            if (s.prefab != null && s.position.y <= lowest)
            {
                lowest = s.position.y;
            }
        }
        return float.IsInfinity(lowest) ? 0f : lowest;
    }

    private void Fallback(NetworkRunner runner)
    {
        float now = Time.unscaledTime;
        inRoom.Clear();
        PlayerRef master = PlayerRef.None;
        MonsterNet[] all = FindObjectsByType<MonsterNet>(FindObjectsSortMode.None);
        int live = 0;
        for (int i = 0; i < all.Length; i++)
        {
            MonsterNet m = all[i];
            if (!m.Live) continue;
            if (master == PlayerRef.None)
            {
                master = m.Object.StateAuthority;
            }
            live++;
            if (LostWhy(m, now) == null)
            {
                string kind = Kind(m);
                int n;
                inRoom[kind] = inRoom.TryGetValue(kind, out n) ? n + 1 : 1;
            }
        }
        Prune(deadSince);
        int rank = Rank(runner);
        int tried = 0;
        int made = 0;
        float longest = 0f;
        for (int i = 0; i < spawns.Length; i++)
        {
            Spawn s = spawns[i];
            if (s.prefab == null) continue;
            int have;
            if (inRoom.TryGetValue(s.prefab.name, out have) && 0 < have)
            {
                inRoom[s.prefab.name] = have - 1;
                missingSince.Remove(i);
            }
            else
            {
                float since;
                if (missingSince.TryGetValue(i, out since))
                {
                    if (FallbackAfter + FallbackStagger * rank <= now - since && fallbackQuietTill <= now)
                    {
                        if (made + live < spawns.Length * 3)
                        {
                            tried++;
                            if (longest <= now - since)
                            {
                                longest = now - since;
                            }
                            if (SpawnOne(runner, s))
                            {
                                made++;
                                missingSince.Remove(i);
                            }
                        }
                        else if (!toldCrowded)
                        {
                            toldCrowded = true;
                            Debug.LogWarning(string.Format("[MonsterDirector] the room already has {0} monsters, lost ones too: not spawning more from here", live));
                        }
                    }
                }
                else
                {
                    missingSince[i] = now;
                }
            }
        }
        if (tried != 0)
        {
            fallbackQuietTill = now + 60f;
            string who = "unknown";
            if (master != PlayerRef.None)
            {
                who = "player " + master.PlayerId.ToString();
            }
            string head = string.Format("[MonsterDirector] {0} of the map's monsters missing here for {1:0} s, the master ({2}) not making them: ", tried, longest, who);
            string tail = string.Format("spawned {0} from here (player {1}) in '{2}'", made, runner.LocalPlayer.PlayerId, runner.SessionInfo.Name);
            Debug.LogWarning(head + tail);
        }
    }

    private static int Rank(NetworkRunner runner)
    {
        int me = runner.LocalPlayer.PlayerId;
        int rank = 0;
        foreach (PlayerRef p in runner.ActivePlayers)
        {
            if (p.PlayerId < me)
            {
                rank++;
            }
        }
        return rank;
    }
}
