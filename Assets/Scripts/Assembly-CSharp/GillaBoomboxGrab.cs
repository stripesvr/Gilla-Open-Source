// Redux addition (not in the original game): the shop jukebox is an item, picked up, carried and
// thrown exactly like gold bars and bolts (GillaItem + GillaHands). It is a physics object that
// lands on floors and walls but never collides with players. Whoever holds or threw it last sends
// its position to the room (NetworkApe.RPC_BoomboxAt); everyone else follows smoothly.
using UnityEngine;

public class GillaBoomboxGrab : MonoBehaviour
{
    public static GillaBoomboxGrab Instance { get; private set; }
    private const float SendEvery = 1f / 15f;

    private Rigidbody rb;
    private GillaItem item;
    private float nextSend;
    private bool moving;              // I'm holding it or it's flying from my throw
    private Vector3 remotePos;
    private Quaternion remoteRot;
    private bool remote;
    private Vector3 homePos;
    private Quaternion homeRot;

    public static Bounds LocalBounds { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Forget() { Instance = null; }

    public static void Attach(GillaBoombox box)
    {
        if (box == null || box.GetComponent<GillaBoomboxGrab>() != null) return;
        box.gameObject.AddComponent<GillaBoomboxGrab>();
    }

    private void Awake()
    {
        Instance = this;
        // the ceiling lamp and the table belong to the shop: leave them behind
        Transform lamp = transform.Find("Gilla_ShopLampSmall");
        if (lamp != null) lamp.SetParent(transform.parent, true);
        Transform stand = transform.Find("Gilla_BoomboxStand");
        if (stand != null) stand.SetParent(transform.parent, true);

        // one box round all of its meshes, in its own space
        Bounds local = new Bounds();
        bool any = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            Bounds b = r.bounds;
            Vector3 c = b.center, e = b.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = transform.InverseTransformPoint(corner);
                if (!any) { local = new Bounds(p, Vector3.zero); any = true; } else local.Encapsulate(p);
            }
        }
        LocalBounds = local;
        // its detailed mesh colliders can't move with it: replace them with one box. Removed now
        // (not later) so the item below only ever sees the box.
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.GetComponentInParent<PressKey>() != null) continue;   // the MUTE key keeps working
            DestroyImmediate(c);
        }
        BoxCollider hull = gameObject.AddComponent<BoxCollider>();
        hull.center = local.center;
        hull.size = Vector3.Max(local.size, Vector3.one * 0.05f);

        rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 12f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = true;   // stays on its table until someone picks it up

        item = gameObject.AddComponent<GillaItem>();
        item.label = "JUKEBOX";
        item.coins = 0;
        item.keeps = true;       // never tidied away
        item.recycles = false;   // the recycler won't eat it

        homePos = transform.position;
        homeRot = transform.rotation;
        GillaMusicPanel.Attach(transform);
    }

    public void Remote(Vector3 pos, Quaternion rot)
    {
        if (item.Hand >= 0) return;   // I'm holding it
        moving = false;
        rb.isKinematic = true;
        remotePos = pos;
        remoteRot = rot;
        remote = true;
    }

    private void Update()
    {
        bool held = item.Hand >= 0;
        if (held || (!rb.isKinematic && !rb.IsSleeping()))
        {
            moving = true;
            remote = false;
            if (Time.unscaledTime >= nextSend)
            {
                nextSend = Time.unscaledTime + SendEvery;
                Send();
            }
        }
        else if (moving)
        {
            moving = false;   // came to rest: one last update
            if (transform.position.y < homePos.y - 25f)
            {
                rb.isKinematic = true;
                transform.SetPositionAndRotation(homePos, homeRot);   // fell out of the map
            }
            Send();
        }
        else if (remote)
        {
            float k = 1f - Mathf.Exp(-Time.deltaTime * 15f);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, remotePos, k), Quaternion.Slerp(transform.rotation, remoteRot, k));
        }
    }

    private void Send()
    {
        NetworkApe me = NetworkApe.Local;
        if (me != null && me.Object != null && me.Object.IsValid && me.Runner != null && me.Runner.IsRunning)
        {
            me.RPC_BoomboxAt(transform.position, transform.rotation);
        }
    }
}
