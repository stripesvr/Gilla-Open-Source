// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using GorillaLocomotion;

public partial class GillaHands : MonoBehaviour
{
    [Tooltip("How close (m) a hand must come to a thing's surface to take it.")]
    public float reach = 0.1f;
    [Tooltip("A throw leaves at your hand's speed times this.")]
    public float throwBoost = 1.25f;
    [Tooltip("Played as you take something, and as you let it go (one at random).")]
    public AudioClip[] pickUps;
    [Tooltip("Played as you take something, and as you let it go (one at random).")]
    public AudioClip[] putDowns;
    private static GillaHands you;
    private const int Mouse = 2;
    private const int Trail = 5;
    private const float SettleRate = 28f;
    private const float GripTake = 0.7f;
    private const float GripLetGo = 0.4f;
    private readonly GillaItem[] holding = new GillaItem[3];
    private readonly Vector3[] heldAt = new Vector3[3];
    private readonly Vector3[] settling = new Vector3[3];
    private readonly Quaternion[] heldTurn = new Quaternion[3];
    private readonly HeldShape[] shapes = new HeldShape[2];
    private readonly bool[] knocking = new bool[2];
    private readonly bool[] was = new bool[2];
    private readonly bool[] testHeld = new bool[2];
    private readonly Vector3[,] trailAt = new Vector3[3, 5];
    private readonly Quaternion[,] trailTurn = new Quaternion[3, 5];
    private readonly float[] trailTime = new float[5];
    private int head;
    private int frames;
    private Player gt;
    private Collider[] rig = new Collider[0];
    private HandPoser[] posers = new HandPoser[0];
    private const int MostBalls = 24;
    private const float LeastBall = 0.008f;
    private const float MostBall = 0.03f;

    private void Awake()
    {

        you = this;
        gt = GetComponent<Player>();
        rig = GetComponentsInChildren<Collider>(true);
    }

    private void Start()
    {
        posers = UnityEngine.Object.FindObjectsByType<HandPoser>(FindObjectsSortMode.None).Where(p => !p.remote).ToArray();
    }

    private void OnDestroy()
    {
        if (you == this)
        {
            you = null;
        }
    }

    public static void IgnoreYou(Collider[] colliders)
    {
        if (you == null) return;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider[] mine = you.rig;
            Collider a = colliders[i];
            for (int j = 0; j < mine.Length; j++)
            {
                Collider b = mine[j];
                if (a != null && b != null)
                {
                    Physics.IgnoreCollision(a, b);
                }
            }
        }
    }

    public static void Drop(GillaItem item)
    {
        if (you == null) return;
        for (int i = 0; i < 3; i++)
        {
            if (you.holding[i] == item)
            {
                you.LetGo(i, false);
            }
        }
    }

    public static GillaItem Holding(int hand)
    {
        GillaItem result = null;
        if (you != null)
        {
            result = you.holding[hand];
        }
        return result;
    }

    public static Vector3 HandPoint(int hand)
    {
        if (you != null)
        {
            return you.Point(hand);
        }
        return Vector3.one * 1000000f;
    }

    public static bool Reaches(int hand)
    {
        if (you == null) return false;
        if (you.holding[hand] != null) return false;
        return you.Nearest(you.Point(hand)) != null;
    }

    public static void TakeForTest(int hand, GillaItem item)
    {
        if (you == null) return;
        GillaHands h = you;
        h.testHeld[hand] = true;
        h.was[hand] = true;
        for (int i = 0; i < 3; i++)
        {
            if (h.holding[i] == item)
            {
                h.LetGo(i, false);
            }
        }
        Vector3 p = h.Point(hand);
        Vector3 from = item.transform.position;
        Vector3 grab = p;
        if (hand != 2)
        {
            grab = p + from - NearestOn(item, p);
        }
        h.holding[hand] = item;
        item.Take(hand);
        Quaternion turn = h.Turn(hand);
        h.heldAt[hand] = Quaternion.Inverse(turn) * (grab - p);
        h.settling[hand] = from - grab;
        h.heldTurn[hand] = Quaternion.Inverse(turn) * item.transform.rotation;
        if (hand < 2)
        {
            h.shapes[hand] = ShapeOf(item, Quaternion.Inverse(turn), h.heldAt[hand], from);
            h.knocking[hand] = true;
        }
        h.Fingers(hand, 1f);
        Sfx.Play(h.pickUps, item.transform.position, 0.55f, 0.94f, 1.08f);
        if (hand < 2)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, 0.45f, 0.05f);
        }
    }

    public static void ThrowForTest(int hand, Vector3 velocity)
    {
        if (you == null) return;
        you.testHeld[hand] = false;
        if (you.holding[hand] == null) return;
        GillaItem item = you.holding[hand];
        you.LetGo(hand, false);
        Vector3 spin = Vector3.zero;
        const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(GillaItem).GetProperty("Hand", any)?.SetValue(item, -1); /* ? */
        Collider[] cols = item.Colliders;
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].enabled = true;
        }
        if (!item.Mine)
        {
            typeof(GillaItem).GetField("throwVelocity", any)?.SetValue(item, velocity); /* ? */
            typeof(GillaItem).GetField("throwWaits", any)?.SetValue(item, true); /* ? */
            typeof(GillaItem).GetField("throwSpin", any)?.SetValue(item, spin); /* ? */
            return;
        }
        typeof(GillaItem).GetMethod("Fly", any, null, new[] { typeof(Vector3), typeof(Vector3) }, null)?.Invoke(item, new object[] { velocity, spin }); /* ? */
    }

    public static void GripForTest(int hand, bool closed)
    {
        if (you != null)
        {
            you.testHeld[hand] = closed;
        }
    }

    private void LateUpdate()
    {
        bool act;
        bool free;
        if (KillCam.Local != null && KillCam.Local.Busy)
        {
            act = false;
            free = false;
        }
        else
        {
            free = !MonsterPlayer.IsMonster;
            act = true;
        }
        Record();
        for (int h = 0; h < 2; h++)
        {
            bool toy = GillaToys.HeldIn(h);
            XRNode node = h != 0 ? XRNode.RightHand : XRNode.LeftHand;
            bool down;
            if (Grip(node, toy || holding[h] != null))
            {
                down = true;
            }
            else
            {
                down = testHeld[h];
            }
            bool allowed = act && Allowed(holding[h]);

            if (down && !was[h] && !toy && act && holding[h] == null)
            {
                GillaItem nearest = Nearest(Point(h));
                if (free && nearest == null && GillaToys.TryTake(h, Point(h)))
                {
                    Fingers(h, 1f);
                }
                else if (nearest != null)
                {
                    Take(h, nearest);
                }
            }
            else
            {
                bool release = !down || !allowed;
                if (release && holding[h] != null)
                {
                    LetGo(h, !down && allowed);
                }
                else if (toy && !(down && free))
                {
                    Vector3 velocity;
                    Vector3 spin;
                    Throw(h, out velocity, out spin);
                    GillaToys.Release(h, free ? velocity : Vector3.zero);
                    Fingers(h, 0f);
                }
            }
            was[h] = down;
        }
        MouseHands(act);
        for (int i = 0; i < 3; i++)
        {
            GillaItem item = holding[i];
            if (item == null)
            {
                if ((object)item != null)
                {
                    holding[i] = null;
                    Fingers(i, 0f);
                }
            }
            else if (item.Gone || item.Swallowed)
            {
                holding[i] = null;
                Fingers(i, 0f);
            }
            else if (item.HeldByOther)
            {
                LetGo(i, false);
            }
            else
            {
                Quaternion turn = Turn(i);
                settling[i] = settling[i] * Mathf.Exp(Time.deltaTime * -SettleRate);
                Transform t = item.transform;
                Vector3 position = Point(i) + turn * heldAt[i] + settling[i];
                t.SetPositionAndRotation(position, turn * heldTurn[i]);
            }
        }
        Touches();
    }

    private void Touches()
    {
        if (gt == null) return;
        for (int h = 0; h < 2; h++)
        {
            GillaItem item = holding[h];
            bool ok;
            if (item != null)
            {
                ok = !item.Stuck && !item.Gone && !item.Swallowed;
            }
            else
            {
                ok = false;
            }
            if (h == 0)
            {
                gt.leftHeld = ok ? shapes[0] : null;
            }
            else
            {
                gt.rightHeld = ok ? shapes[1] : null;
            }
            Vector3 at;
            Vector3 faces;
            Collider hit;
            bool touching = gt.HeldTouching(h == 1, out at, out faces, out hit);
            if (ok && touching && !knocking[h])
            {
                Vector3 velocity;
                Vector3 spin;
                Throw(h, out velocity, out spin);
                item.HeldKnock(at, faces, velocity.magnitude / throwBoost, hit);
            }
            knocking[h] = ok && touching;
        }
    }

    private static HeldShape ShapeOf(GillaItem item, Quaternion toHand, Vector3 at, Vector3 from)
    {
        List<Vector3> balls = new List<Vector3>();
        List<float> sizes = new List<float>();
        void Ball(Vector3 world, float r)
        {
            balls.Add(at + toHand * (world - from));
            sizes.Add(r);
        }

        Collider[] cols = item.Colliders;
        for (int c = 0; c < cols.Length; c++)
        {
            Collider col = cols[c];
            if (col == null) continue;
            Transform t = col.transform;
            Vector3 scale = Abs(t.lossyScale);
            if (col is CapsuleCollider capsule)
            {
                int dir = capsule.direction;
                float axisScale = scale[dir];
                float radius = capsule.radius * Mathf.Max(scale[(dir + 1) % 3], scale[(dir + 2) % 3]);
                if (axisScale <= 0.0001f) axisScale = 0.0001f;
                float half = Mathf.Max(axisScale * capsule.height * 0.5f - radius, 0f);
                Vector3 axis = Vector3.zero;
                axis[dir] = 1f / axisScale;
                int n = Mathf.Clamp((int)((half + half) / (radius * 1.5f)) + 1, 1, 12);
                for (int i = 0; i < n; i++)
                {
                    Vector3 local = capsule.center + axis * (half * Spread(i, n));
                    Ball(t.TransformPoint(local), radius);
                }
            }
            else if (col is SphereCollider sphere)
            {
                Vector3 world = t.TransformPoint(sphere.center);
                float r = sphere.radius;
                Ball(world, Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) * r);
            }
            else if (col is BoxCollider box)
            {
                Vector3 middle = box.center;
                Vector3 half = box.size * 0.5f;
                Box(t, middle, Vector3.Scale(scale, half), scale, Ball);
            }
            else if (col is MeshCollider meshCol)
            {
                if (meshCol.sharedMesh != null)
                {
                    Mesh mesh = meshCol.sharedMesh;
                    if (!mesh.isReadable)
                    {
                        Vector3 middle = mesh.bounds.center;
                        Vector3 ext = mesh.bounds.extents;
                        Box(t, middle, Vector3.Scale(scale, ext), scale, Ball);
                    }
                    else
                    {
                        Vector3 ext = mesh.bounds.extents;
                        float r = Mathf.Min(scale.x * ext.x, Mathf.Min(scale.y * ext.y, scale.z * ext.z));
                        r = Mathf.Clamp(r, LeastBall, 0.015f);
                        Vector3 centre = t.TransformPoint(mesh.bounds.center);
                        List<Vector3> far = Furthest(mesh.vertices, mesh.bounds.center, 10);
                        foreach (Vector3 p in far)
                        {
                            Vector3 w = t.TransformPoint(p);
                            Vector3 d = centre - w;
                            float dist = d.magnitude;
                            Vector3 pos = centre;
                            if (r < dist)
                            {
                                pos = w + d.normalized * r;
                            }
                            Ball(pos, r);
                        }
                    }
                }
            }
        }
        if (balls.Count > MostBalls)
        {
            List<Vector3> b2 = new List<Vector3>();
            List<float> s2 = new List<float>();
            for (int i = 0; i < MostBalls; i++)
            {
                int idx = balls.Count * i / MostBalls;
                b2.Add(balls[idx]);
                s2.Add(sizes[idx]);
            }
            balls = b2;
            sizes = s2;
        }
        return new HeldShape(balls.ToArray(), sizes.ToArray());
    }

    private static void Box(Transform t, Vector3 middle, Vector3 e, Vector3 s, Action<Vector3, float> ball)
    {
        float r = Mathf.Clamp(Mathf.Min(e.x, Mathf.Min(e.y, e.z)), LeastBall, MostBall);
        Vector3 h = Vector3.Max(e - r * Vector3.one, Vector3.zero);
        int nx = Count(h.x, r);
        int ny = Count(h.y, r);
        int nz = Count(h.z, r);
        if (nx <= 0) return;
        if (s.x <= 0.0001f) s.x = 0.0001f;
        if (s.y <= 0.0001f) s.y = 0.0001f;
        if (s.z <= 0.0001f) s.z = 0.0001f;
        for (int i = 0; i < nx; i++)
        {
            float fx = Spread(i, nx);
            for (int j = 0; j < ny; j++)
            {
                float fy = Spread(j, ny);
                for (int k = 0; k < nz; k++)
                {
                    if (j == 0 || i == 0 || i >= nx - 1 || j >= ny - 1 || k == 0 || k >= nz - 1)
                    {
                        float fz = Spread(k, nz);
                        Vector3 local = new Vector3(
                            middle.x + (h.x * fx) / s.x,
                            middle.y + (h.y * fy) / s.y,
                            middle.z + (h.z * fz) / s.z);
                        ball(t.TransformPoint(local), r);
                    }
                }
            }
        }
    }

    private static int Count(float half, float r)
    {
        if (r * 0.75f < half)
        {
            return Mathf.Clamp((int)((half + half) / (r * 1.8f)) + 1, 2, 4);
        }
        return 1;
    }

    private static float Spread(int i, int n)
    {
        float f = 0f;
        if (n != 1)
        {
            f = (float)i / ((float)n - 1f);
            f = f + f - 1f;
        }
        return f;
    }

    private static Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private static List<Vector3> Furthest(Vector3[] points, Vector3 middle, int many)
    {
        List<Vector3> result = new List<Vector3>();
        if (points.Length == 0) return result;
        float[] dist = new float[points.Length];
        int best = 0;
        for (int i = 0; i < points.Length; i++)
        {
            float d = (points[i] - middle).sqrMagnitude;
            dist[i] = d;
            if (d > dist[best]) best = i;
        }
        if (many > 0)
        {
            for (int k = 0; k < points.Length; k++)
            {
                Vector3 picked = points[best];
                result.Add(picked);
                best = -1;
                for (int i = 0; i < points.Length; i++)
                {
                    float d = (points[i] - picked).sqrMagnitude;
                    if (k != 0)
                    {
                        d = Mathf.Min(dist[i], d);
                    }
                    dist[i] = d;
                    if (best < 0 || dist[best] < d)
                    {
                        best = i;
                    }
                }
                if (dist[best] < 1e-06f) return result;
                if (k + 1 == many) return result;
            }
        }
        return result;
    }

    private Vector3 Point(int h)
    {
        if (h != 2 && gt != null)
        {
            if (h == 0)
            {
                return gt.leftHandFollower.position;
            }
            return gt.rightHandFollower.position;
        }
        Transform t = Camera.main.transform;
        return t.position + t.forward * 0.45f + t.right * 0.1f - t.up * 0.12f;
    }

    private Quaternion Turn(int h)
    {
        Transform t;
        if (h == 2 || gt == null)
        {
            t = Camera.main.transform;
        }
        else if (h == 0)
        {
            t = gt.leftHandTransform;
        }
        else
        {
            t = gt.rightHandTransform;
        }
        return t.rotation;
    }

    private void Record()
    {
        head = (head + 1) % Trail;
        frames++;
        trailTime[head] = Time.time;
        for (int i = 0; i < 3; i++)
        {
            trailAt[i, head] = Point(i);
            trailTurn[i, head] = Turn(i);
        }
    }

    private static bool Allowed(GillaItem item)
    {
        if (MonsterPlayer.IsMonster)
        {
            if (item != null)
            {
                return item.ForMonsters;
            }
        }
        return true;
    }
}
