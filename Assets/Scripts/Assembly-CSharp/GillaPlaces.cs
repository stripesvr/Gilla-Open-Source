// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Reflection;
using UnityEngine;

public class GillaPlaces : MonoBehaviour
{
    [Serializable]
    public class Place
    {
        public string name;
        [Tooltip("Where you are when you're in it (world).")]
        public Bounds area;
        [Tooltip("What's switched on and off with it.")]
        public GameObject[] roots = new GameObject[0];
        [Tooltip("The place that's out with it (BE THE MONSTER's lobby and its map), or -1.")]
        public int with = -1;
    }

    [Tooltip("Smaller first: a point in two areas is in the first one's place.")]
    public Place[] places = new Place[0];
    private static GillaPlaces instance;
    private int current = -1;

    public static int Current
    {
        get
        {
            if (instance != null)
            {
                return instance.current;
            }
            return -1;
        }
    }

    public static string CurrentName
    {
        get
        {
            if (!(instance != null))
            {
                return null;
            }
            int c = instance.current;
            if (c < 0)
            {
                return null;
            }
            if (instance.places.Length <= c)
            {
                return null;
            }
            return instance.places[c].name;
        }
    }

    public static bool AreaNamed(string name, out Bounds area)
    {
        area = default(Bounds);
        if (!(instance == null))
        {
            Place[] arr = instance.places;
            for (int i = 0; i < arr.Length; i++)
            {
                Place place = arr[i];
                if (place.name == name)
                {
                    area = place.area;
                    return true;
                }
            }
        }
        return false;
    }

    private void Awake()
    {

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        int p = Find(Eyes());
        if (p > -1)
        {
            Apply(p);
        }
    }

    private void LateUpdate()
    {
        int p = Find(Eyes());
        if (p > -1 && p != current)
        {
            Apply(p);
        }
        KeepOut();
    }

    private void KeepOut()
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        KillCam cam = KillCam.Local;
        if (boot == null)
        {
            return;
        }
        if (cam == null)
        {
            return;
        }
        if (cam.head == null || cam.Busy)
        {
            return;
        }
        if (GillaFreecam.On)
        {
            return;
        }
        string tag = NetworkBootstrap.RoomTag;
        if (tag.Length > 0)
        {
            return;
        }
        NetworkBootstrap.Lobby mode;
        if (!InMode(cam.head.position, out mode))
        {
            return;
        }
        if (mode == boot.Going)
        {
            return;
        }
        cam.SendHome();
        int hand = GillaToast.NearerHand(cam.head.position);
        GillaToast toast = UnityEngine.Object.FindAnyObjectByType<GillaToast>(); /* ? */
        if (!(toast != null))
        {
            return;
        }
        MethodInfo begin = typeof(GillaToast).GetMethod("Begin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(string), typeof(string), typeof(bool) }, null); /* ? */
        if (begin != null)
        {
            begin.Invoke(toast, new object[] { hand, "PLAY BUTTON ONLY", "PRESS PLAY TO GO TO THAT MAP", true }); /* ? */
        }
    }

    public bool InMode(Vector3 at, out NetworkBootstrap.Lobby mode)
    {
        mode = NetworkBootstrap.Lobby.Regular;
        for (int i = 0; i < places.Length; i++)
        {
            Place place = places[i];
            if (place.area.Contains(at))
            {
                bool splash = place.name.Contains("SPLASH");
                bool monster = place.name.Contains("MONSTER");
                if (!splash && !monster)
                {
                    mode = NetworkBootstrap.Lobby.Regular;
                    return false;
                }
                mode = splash ? NetworkBootstrap.Lobby.Splash : NetworkBootstrap.Lobby.Monster;
            }
        }
        return mode != NetworkBootstrap.Lobby.Regular;
    }

    public static void Prepare(Vector3 at)
    {
        if (!(instance == null))
        {
            int p = instance.Find(at);
            if (p > -1 && p != instance.current)
            {
                instance.Apply(p);
            }
        }
    }

    private static Vector3 Eyes()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position;
        }
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance; /* ? */
        if (player != null && player.headCollider != null)
        {
            return player.headCollider.transform.position;
        }
        return Vector3.positiveInfinity;
    }

    private int Find(Vector3 at)
    {
        for (int i = 0; i < places.Length; i++)
        {
            if (places[i].area.Contains(at))
            {
                return i;
            }
        }
        return -1;
    }

    private void Apply(int p)
    {
        current = p;
        int with = places[p].with;
        for (int pass = 0; pass != 2; pass++)
        {
            for (int i = 0; i < places.Length; i++)
            {
                bool value = i == p || i == with;
                if ((pass != 1) != value)
                {
                    GameObject[] roots = places[i].roots;
                    for (int j = 0; j < roots.Length; j++)
                    {
                        GameObject go = roots[j];
                        if (go != null)
                        {
                            if (value != go.activeSelf)
                            {
                                go.SetActive(value);
                            }
                        }
                    }
                }
            }
        }
        string str1 = places[p].name;
        string str2 = "";
        if (with > -1)
        {
            str2 = " and " + places[with].name;
        }
        Debug.Log("[GillaPlaces] in " + str1 + str2 + ": the rest switched off");
    }
}
