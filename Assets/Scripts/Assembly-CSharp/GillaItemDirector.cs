// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class GillaItemDirector : MonoBehaviour
{
    [Serializable]
    public class Kind
    {
        public GameObject prefab;
        [Tooltip("How often it's this kind, against the others' chances.")]
        public float chance = 1f;
        [Tooltip("How it lies (degrees about X, Y, Z), before it's turned about at random.")]
        public Vector3 lie;
        [Tooltip("How high its origin is over the floor, lying so.")]
        public float lift;
    }

    [Serializable]
    public class Spot
    {
        public Kind kind;
        public Vector3 at;
    }

    public Kind[] kinds = new Kind[0];
    [Tooltip("Where they may turn up: points on the floors, out in the open, all over the map the monsters roam.")]
    public Vector3[] places = new Vector3[0];
    [Tooltip("How many lie about at once at most.")]
    public int most = 12;
    [Tooltip("How many come quickly at first, 3 to 12 seconds apart.")]
    public int first = 4;
    [Tooltip("Seconds between the next ones, at random between these.")]
    public Vector2 every = new Vector2(25f, 70f);
    [Tooltip("Seconds one can lie untouched before it goes (once no one's near).")]
    public float lasts = 300f;
    [Tooltip("The keycard, and where it lies (on the desk in the vents' drop room).")]
    public Kind keycard;
    public Vector3 keycardAt;
    [Tooltip("What's laid out in the locked room.")]
    public Spot[] spots = new Spot[0];
    [Tooltip("Seconds till one's back once it's gone from its spot.")]
    public float spotRefill = 180f;
    private const float Apart = 10;
    private const float Unseen = 8;
    private const float KeycardEvery = 2;
    private const float SpotHolds = 0.45f;
    public const float Settle = 5;
    private readonly List<GameObject> local = new List<GameObject>();
    private float offlineFor;
    private float nextAt = -1f;
    private float nextKeycard;
    private float nextCard;
    private float joinedAt;
    private float[] spotGone;
    private NetworkRunner joined;
    private GillaItem card;

    public static GillaItemDirector Instance { get; set; }

    public GillaItem YourCard { get { return card; } }

    private void Awake()
    {

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public ValueTuple<string, int>[] Prices()
    {
        return kinds
            .Where(k => k.prefab != null && k.prefab.GetComponent<GillaItem>() != null)
            .Select(k => k.prefab.GetComponent<GillaItem>())
            .GroupBy(i => i.label)
            .Select(g => (g.Key, g.First().coins))
            .OrderByDescending(p => p.Item2)
            .ThenBy(p => p.Item1)
            .ToArray();
    }

    public static bool AnyoneNear(Vector3 p, float within = 8)
    {
        return Nearest(p) < within;
    }

    private static float Nearest(Vector3 p)
    {
        float best = float.MaxValue;
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape != null && ape.head != null)
            {
                float d = Vector3.Distance(p, ape.head.position);
                if (d <= best) best = d;
            }
        }
        if (KillCam.Local != null && KillCam.Local.head != null)
        {
            float d = Vector3.Distance(p, KillCam.Local.head.position);
            if (d <= best) best = d;
        }
        return best;
    }

    private void Update()
    {
        local.RemoveAll(go => go == null);
        NetworkBootstrap bootstrap = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (bootstrap != null)
        {
            runner = bootstrap.Runner /* ? */;
        }
        if (runner != null && runner.IsRunning)
        {
            offlineFor = 0f;
            ClearLocal();
            if (bootstrap.Kind != NetworkBootstrap.Lobby.Regular)
            {
                nextAt = -1f;
                ThrowAwayCard();
                return;
            }
            if (bootstrap.Joining)
            {
                nextAt = -1f;
                return;
            }
            if (runner != joined)
            {
                joined = runner;
                joinedAt = Time.time;
                spotGone = null;
                nextAt = -1f;
            }
            MakeCard();
            if (!runner.IsSharedModeMasterClient || Time.time - joinedAt < Settle)
            {
                nextAt = -1f;
                return;
            }
            if (Due(true))
            {
                Make(runner, Pick(), null);
            }
        }
        else
        {
            joined = null;
            if (bootstrap != null && bootstrap.Joining)
            {
                offlineFor = 0f;
                nextAt = -1f;
                return;
            }
            offlineFor += Time.deltaTime;
            float wait = bootstrap != null ? 2f : 0f;
            if (offlineFor < wait)
            {
                return;
            }
            if (Due(false))
            {
                Make(null, Pick(), null);
            }
            MakeCard();
            runner = null;
        }
        Spots(runner);
    }

    private void MakeCard()
    {
        if (card == null && nextCard <= Time.time && keycard != null && !(keycard.prefab == null))
        {
            nextCard = Time.time + KeycardEvery;
            card = Make(null, keycard, keycardAt);
            if (card != null)
            {
                local.Remove(card.gameObject);
            }
        }
    }

    private void ThrowAwayCard()
    {
        if (card == null)
        {
            return;
        }
        if (card.Hand >= 0)
        {
            GillaHands.Drop(card);
        }
        Destroy(card.gameObject);
        card = null;
    }

    private void Spots(NetworkRunner runner)
    {
        if (Time.time < nextKeycard)
        {
            return;
        }
        nextKeycard = Time.time + KeycardEvery;
        if (spotGone == null || spotGone.Length != spots.Length)
        {
            bool busy;
            if (runner == null)
            {
                busy = false;
            }
            else
            {
                busy = GillaItem.All.Exists(Spots_b__40_0);
            }
            spotGone = new float[spots.Length];
            for (int j = 0; j < spots.Length; j++)
            {
                spotGone[j] = busy ? Time.time : -spotRefill;
            }
        }
        for (int i = 0; i < spots.Length; i++)
        {
            Spot spot = spots[i];
            Vector3 at = spot.at;
            GillaItem found = null;
            foreach (GillaItem item in GillaItem.All)
            {
                if (!item.keeps && !item.Gone /* ? */)
                {
                    if ((item.transform.position - at).sqrMagnitude < SpotHolds * SpotHolds)
                    {
                        if (found == null)
                        {
                            found = item;
                        }
                        else if (runner != null && item.Hand < 0 && item.Net != null && item.Net.Live)
                        {
                            if ((item.transform.position - at).sqrMagnitude < 0.04f)
                            {
                                item.Net.RemoveFromRoom();
                            }
                        }
                    }
                }
            }
            if (found != null)
            {
                spotGone[i] = 0f;
            }
            else
            {
                if (spotGone[i] == 0f)
                {
                    spotGone[i] = Time.time;
                }
                if (spotRefill <= Time.time - spotGone[i])
                {
                    Make(runner, spots[i].kind, at);
                }
            }
        }
    }

    private bool OnSpot(Vector3 p)
    {
        int i = 0;
        for (; i < spots.Length; i++)
        {
            if ((p - spots[i].at).sqrMagnitude < SpotHolds * SpotHolds)
            {
                break;
            }
        }
        return i < spots.Length;
    }

    public bool Stays(GillaItem item)
    {
        if (item.keeps)
        {
            return true;
        }
        return OnSpot(item.transform.position);
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

    private bool Due(bool online)
    {
        float t;
        bool due;
        if (nextAt >= 0f)
        {
            if (Time.time < nextAt)
            {
                return false;
            }
            int lying = Lying(online);
            float now = Time.time;
            float min, max;
            if (lying < first)
            {
                min = 3f;
                max = 12f;
            }
            else
            {
                min = every.x;
                max = every.y;
            }
            t = now + UnityEngine.Random.Range(min, max);
            due = lying < most;
        }
        else
        {
            t = Time.time + UnityEngine.Random.Range(3f, 12f);
            due = false;
        }
        nextAt = t;
        return due;
    }

    private int Lying(bool online)
    {
        return GillaItem.All.Count(i =>
        {
            if (i.recycles && !Stays(i) && !i.Gone && !GillaBoltShop.Sells(i.label))
            {
                if (online)
                {
                    if (i.Net == null) return false;
                    return i.Net.Live;
                }
                return local.Contains(i.gameObject);
            }
            return false;
        });
    }

    private Kind Pick()
    {
        List<Kind> list = kinds.Where(k => k.prefab != null && k.chance > 0f).ToList();
        float r = UnityEngine.Random.Range(0f, list.Sum(k => k.chance));
        foreach (Kind k in list)
        {
            if (r < k.chance)
            {
                return k;
            }
            r -= k.chance;
        }
        return list.LastOrDefault();
    }

    private bool PlaceFor(out Vector3 at)
    {
        at = Vector3.zero;
        float best = 0.5f;
        bool got = false;
        for (int tries = 0; ; )
        {
            if (places.Length == 0)
            {
                return got;
            }
            int n = UnityEngine.Random.Range(0, places.Length);
            Vector3 p = places[n];
            float score = Nearest(p) * 0.125f;
            foreach (GillaItem item in GillaItem.All)
            {
                if (!item.Gone /* ? */)
                {
                    float s = Vector3.Distance(p, item.transform.position) / Apart;
                    if (s <= score) score = s;
                }
            }
            if (best <= score)
            {
                got = true;
                at = p;
                best = score;
                if (score >= 1f)
                {
                    return true;
                }
            }
            tries++;
            if (tries == 24)
            {
                return got;
            }
        }
    }

    private GillaItem Make(NetworkRunner runner, Kind kind, Nullable<Vector3> place)
    {
        if (kind == null)
        {
            return null;
        }
        if (kind.prefab == null)
        {
            return null;
        }
        Vector3 at;
        if (!place.HasValue)
        {
            if (!PlaceFor(out at))
            {
                return null;
            }
        }
        else
        {
            at = place.Value;
        }
        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Quaternion.Euler(kind.lie);
        Vector3 position = at + Vector3.up * kind.lift;
        if (runner == null)
        {
            GameObject go = Instantiate(kind.prefab, position, rotation, transform);
            local.Add(go);
            return go.GetComponent<GillaItem>();
        }
        NetworkObject netPrefab;
        if (kind.prefab.TryGetComponent<NetworkObject>(out netPrefab))
        {
            NetworkObject spawned = runner.Spawn(netPrefab, position, rotation);
            if (spawned != null)
            {
                return spawned.GetComponent<GillaItem>();
            }
        }
        else
        {
            Debug.LogError("[GillaItemDirector] " + kind.prefab.name + " has no NetworkObject, so it can't go on the network");
        }
        return null;
    }

    public GillaItem MakeNow(int k, Vector3 at)
    {
        NetworkBootstrap bootstrap = NetworkBootstrap.Instance;
        NetworkRunner r = null;
        if (bootstrap != null)
        {
            r = bootstrap.Runner /* ? */;
        }
        NetworkRunner runner = null;
        if (r != null)
        {
            if (r.IsRunning)
            {
                if (!r.IsSharedModeMasterClient)
                {
                    return null;
                }
                runner = r;
            }
        }
        Kind kind;
        if (k < 0 || k >= kinds.Length)
        {
            kind = null;
        }
        else
        {
            kind = kinds[k];
        }
        return Make(runner, kind, at);
    }

    private bool Spots_b__40_0(GillaItem i)
    {
        if (!i.keeps && !i.Gone)
        {
            if (i.Net != null)
            {
                if (!i.Net.Live)
                {
                    return false;
                }
                return OnSpot(i.transform.position);
            }
        }
        return false;
    }
}
