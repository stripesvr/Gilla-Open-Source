// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fusion;
using UnityEngine;

[DefaultExecutionOrder(110)]
public partial class GillaPull : MonoBehaviour
{
    private class Limb
    {
        public Transform drawn;
        public GillaCorpse body;
        public int part;
        public Transform shoulder;
        public Transform up;
        public Transform lo;
        public Transform end;
        public Transform head;
        public Vector3 shoulderAt;
        public Vector3 loAt;
        public Vector3 endAt;
        public Vector3 upRest;
        public Vector3 loRest;
        public Vector3 endRest;
        public Vector3 headAt;
        public Vector3 upHad;
        public Vector3 loHad;
        public Vector3 offset;
        public Quaternion upTurn;
        public Quaternion loTurn;
        public Quaternion endTurn;
        public Quaternion headTurn;
        public float keepOffset;
        public float lengthUp;
        public float lengthLo;
        public float reach;
        public bool off;
        public Transform spine;
        public Vector3 spineAt;
        public int across;
        public float width;
        public readonly List<(Transform bone, Vector3 scale)> onSpine = new List<(Transform bone, Vector3 scale)>();
    }

    private class Pull
    {
        public NetworkApe by;
        public bool right;
        public NetworkApe who;
        public GillaDummy dummy;
        public GillaCorpse body;
        public Dismemberment ape;
        public bool yours;
        public ApePart part;
        public bool middle;
        public Transform grip;
        public Transform pulling;
        public Transform holding;
        public float start;
        public float tears;
        public Vector3 from;
        public Vector3 way = Vector3.up;
        public float strain;
        public float blend;
        public float x;
        public float v;
        public bool live = true;
        public bool snapped;
        public int said;
        public readonly List<GillaPull.Limb> limbs = new List<GillaPull.Limb>();
    }

    private const float LeastStretch = 0.15f;
    private const float ShoulderShare = 0.08f;
    private const float DrawShare = 0.12f;
    private const float MiddleWide = 0.156f;
    private const float Takes = 0.12f;
    private const float EasesBack = 0.25f;
    private const float Follows = 18f;
    private const float ArmFromHead = 0.35f;
    private const float Spring = 30f;
    private const float Damping = 0.3f;
    private static readonly List<GillaPull.Pull> pulls = new List<GillaPull.Pull>();
    private static GillaPull.Pull mine;
    private readonly Dictionary<int, int> grips = new Dictionary<int, int>();

    private const BindingFlags AnyMember = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /* ? */ // Reflection access for members that are not accessible from this class.
    private static T Get<T>(object o, string name)
    {
        if (o == null) return default(T);
        for (Type t = o.GetType(); t != null; t = t.BaseType)
        {
            FieldInfo f = t.GetField(name, AnyMember);
            if (f != null) return (T)f.GetValue(o);
            PropertyInfo p = t.GetProperty(name, AnyMember);
            if (p != null) return (T)p.GetValue(o);
        }
        return default(T);
    }

    /* ? */
    private static object Call(Type type, object target, string name, object[] args)
    {
        for (Type t = type; t != null; t = t.BaseType)
        {
            MethodInfo m = t.GetMethod(name, AnyMember);
            if (m != null) return m.Invoke(target, args);
        }
        return null;
    }

    public static float MineStrain
    {
        get
        {
            float result = 0f;
            if (mine != null)
            {
                if (!mine.live)
                {
                    return 0f;
                }
                if (!mine.snapped)
                {
                    result = mine.strain;
                }
            }
            return result;
        }
    }

    [RuntimeInitializeOnLoadMethod(0)]
    private static void Make()
    {
        pulls.Clear();
        mine = null;
        GameObject go = new GameObject("GillaPull");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<GillaPull>();
    }

    public static void Mine(Dismemberment ape, GillaCorpse body, ApePart part, Transform grip, Transform a, Transform b, float tears)
    {
        if (mine == null || !mine.live || mine.snapped || mine.ape != ape || mine.body != body || mine.part != part || mine.middle)
        {
            if (mine != null)
            {
                mine.live = false;
            }
            mine = Pulling(ape, body, false, part, false, grip, a, b, tears);
            if (mine == null)
            {
                return;
            }
        }
        mine.said = Time.frameCount;
    }

    public static void MineMiddle(Dismemberment ape, Transform grip, Transform a, Transform b, float tears)
    {
        if (mine == null || !mine.live || mine.snapped || mine.ape != ape || mine.body != null || mine.part != ApePart.Head || !mine.middle)
        {
            if (mine != null)
            {
                mine.live = false;
            }
            mine = Pulling(ape, null, false, ApePart.Head, true, grip, a, b, tears);
            if (mine == null)
            {
                return;
            }
        }
        mine.said = Time.frameCount;
    }

    private static void Mine(Dismemberment ape, GillaCorpse body, ApePart part, bool middle, Transform grip, Transform a, Transform b, float tears)
    {
        if (mine == null || !mine.live || mine.snapped || mine.ape != ape || mine.body != body || mine.part != part || mine.middle != middle)
        {
            if (mine != null)
            {
                mine.live = false;
            }
            mine = Pulling(ape, body, false, part, middle, grip, a, b, tears);
            if (mine == null)
            {
                return;
            }
        }
        mine.said = Time.frameCount;
    }

    public static bool Tearing(Dismemberment ape, ApePart part)
    {
        foreach (Pull p in pulls)
        {
            if (!p.middle && p.part == part && Owns(p, ape))
            {
                Snap(p);
                return true;
            }
        }
        return false;
    }

    public static bool Tearing(GillaCorpse body, ApePart part)
    {
        foreach (Pull p in pulls)
        {
            if (!p.middle && p.body == body && p.part == part)
            {
                Snap(p);
                return true;
            }
        }
        return false;
    }

    public static void Done(Dismemberment ape)
    {
        for (int index = pulls.Count - 1; index >= 0; index--)
        {
            Pull p = pulls[index];
            if (Owns(p, ape))
            {
                foreach (Limb l in p.limbs)
                {
                    Back(l);
                    if (l.body == null && l.up != null && l.lo != null)
                    {
                        l.up.localScale = l.upRest;
                        l.lo.localScale = l.loRest;
                    }
                }
                pulls.RemoveAt(index);
                if (p == mine)
                {
                    mine = null;
                }
            }
        }
    }

    private static bool Owns(GillaPull.Pull p, Dismemberment ape)
    {
        if (p.ape == ape)
        {
            return true;
        }
        if (p.yours)
        {
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            if (boot != null && boot.tearing != null)
            {
                Dismemberment[] copies = boot.tearing.copies;
                if (copies == null)
                {
                    return false;
                }
                return Array.IndexOf(copies, ape) >= 0;
            }
        }
        return false;
    }

    public static void WetSnap(Vector3 at)
    {
        GillaGoreAssets assets = Resources.Load<GillaGoreAssets>("GillaGore");
        if (assets != null)
        {
            Sfx.Play(Get<AudioClip[]>(assets, "snaps") /* ? */, at, 0.75f, 1.15f, 1.35f);
        }
    }

    private static void Snap(GillaPull.Pull p)
    {
        if (!p.snapped)
        {
            p.snapped = true;
            p.v = 0f;
            p.x = Mathf.SmoothStep(0f, 1f, p.strain);
            foreach (Limb l in p.limbs)
            {
                l.off = true;
                if (l.head != null)
                {
                    l.head.localPosition = l.headAt;
                }
                if (l.lo != null)
                {
                    l.lo.localPosition = l.loAt;
                }
                if (l.end != null)
                {
                    l.end.localPosition = l.endAt;
                }
            }
        }
    }

    private static GillaPull.Pull Pulling(Dismemberment ape, GillaCorpse body, bool yours, ApePart part, bool middle, Transform grip, Transform a, Transform b, float tears)
    {
        if (grip == null) return null;
        if (a == null) return null;
        if (b == null) return null;

        int index = pulls.Count;
        if (index - 1 >= 0)
        {
            do
            {
                index--;
                if (pulls[index].part == part && pulls[index].middle == middle && pulls[index].body == body
                    && pulls[index].ape == ape && pulls[index].yours == yours)
                {
                    Finish(pulls[index]);
                    pulls.RemoveAt(index);
                }
            } while (index > 0);
        }

        Pull p = new Pull();
        p.ape = ape;
        p.body = body;
        p.part = part;
        p.yours = yours;
        p.middle = middle;
        p.grip = grip;
        p.pulling = a;
        p.holding = b;
        p.tears = tears;
        p.from = a.position - b.position;
        p.start = p.from.magnitude;

        if (middle)
        {
            if (ape != null)
            {
                Add(p, OfMiddle(ape, true));
            }
            else if (yours)
            {
                NetworkBootstrap boot = NetworkBootstrap.Instance; /* ? */
                if (boot != null && boot.tearing != null)
                {
                    Dismemberment[] copies = boot.tearing.copies;
                    if (copies != null)
                    {
                        for (int i = 0; i < copies.Length; i++)
                        {
                            if (copies[i] != null)
                            {
                                Add(p, OfMiddle(copies[i], false));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (ape != null)
            {
                Add(p, OfApe(ape, part, grip, true));
            }
            else if (body != null)
            {
                Add(p, OfBody(body, part, grip));
            }
            else if (yours)
            {
                NetworkBootstrap boot = NetworkBootstrap.Instance; /* ? */
                if (boot != null && boot.tearing != null)
                {
                    Dismemberment[] copies = boot.tearing.copies;
                    if (copies != null)
                    {
                        for (int i = 0; i < copies.Length; i++)
                        {
                            if (copies[i] != null)
                            {
                                Add(p, OfApe(copies[i], part, grip, false));
                            }
                        }
                    }
                }
            }
        }

        if (p.limbs.Count == 0)
        {
            return null;
        }
        pulls.Add(p);
        return p;
    }

    private static void Add(GillaPull.Pull p, GillaPull.Limb l)
    {
        if (l != null)
        {
            p.limbs.Add(l);
        }
    }

    private static GillaPull.Limb OfApe(Dismemberment ape, ApePart part, Transform grip, bool drawnAlong)
    {
        var found = ape.Find(part);
        if (found == null || found.joint == null)
        {
            return null;
        }
        Limb l = new Limb();
        l.drawn = drawnAlong ? ape.transform : null;
        if (part == ApePart.Head)
        {
            if (found.renderer == null)
            {
                return null;
            }
            l.head = found.joint;
            l.headAt = l.head.localPosition;
            l.reach = Mathf.Abs(l.head.lossyScale.x) * 0.35f;
            l.offset = l.head.position - grip.position;
            return l;
        }
        if (found.jointOut == null)
        {
            return null;
        }
        l.up = found.joint;
        l.lo = found.jointOut;
        l.end = l.lo.Find(part == ApePart.ArmL ? "hand_L" : "hand_R");
        if (l.end == null)
        {
            return null;
        }
        Arm(l, grip);
        return l;
    }

    private static GillaPull.Limb OfBody(GillaCorpse body, ApePart part, Transform grip)
    {
        int bodyPart = GillaCorpse.Part(part);
        Limb l = new Limb();
        l.body = body;
        l.part = bodyPart;
        l.drawn = body.transform;
        if (part == ApePart.Head)
        {
            Transform head = body.HeadBone;
            if (head == null)
            {
                return null;
            }
            l.head = head;
            l.headAt = head.localPosition;
            l.headTurn = head.localRotation;
            l.reach = Mathf.Abs(head.lossyScale.x) * 0.35f;
            l.offset = head.position - grip.position;
        }
        else
        {
            object[] bones = new object[] { bodyPart, null, null, null };
            if (!(bool)Call(typeof(GillaCorpse), body, "ArmBones", bones) /* ? */)
            {
                return null;
            }
            l.up = (Transform)bones[1];
            l.lo = (Transform)bones[2];
            l.end = (Transform)bones[3];
            Arm(l, grip);
            l.upTurn = l.up.localRotation;
            l.loTurn = l.lo.localRotation;
            l.endTurn = l.end.localRotation;
            l.upHad = l.up.localScale;
            l.loHad = l.lo.localScale;
        }
        Call(typeof(GillaCorpse), body, "LimbStill", new object[] { bodyPart, true }); /* ? */
        return l;
    }

    private static void Arm(GillaPull.Limb l, Transform grip)
    {
        l.shoulder = l.up.parent;
        l.shoulderAt = l.shoulder != null ? l.shoulder.localPosition : Vector3.zero;
        l.loAt = l.lo.localPosition;
        l.endAt = l.end.localPosition;
        Vector3 upScale = l.up.localScale;
        Vector3 loScale = l.lo.localScale;
        l.upRest = new Vector3(upScale.x, upScale.x, upScale.z);
        l.loRest = new Vector3(loScale.x, loScale.x, loScale.z);
        l.endRest = l.end.localScale;
        l.lengthUp = l.lo.localPosition.magnitude * Mathf.Abs(l.up.lossyScale.x);
        l.lengthLo = l.end.localPosition.magnitude * Mathf.Abs(l.up.lossyScale.x) * Mathf.Abs(l.lo.localScale.x);
        l.reach = l.lengthUp + l.lengthLo;
        l.offset = l.end.position - grip.position;
        l.keepOffset = Mathf.Clamp01((l.offset.magnitude / (l.reach / 0.6f) - 0.1f) / 0.20000002f);
    }

    private static float Across(Transform bone)
    {
        return Mathf.Abs(bone.lossyScale.x);
    }

    private static GillaPull.Limb OfMiddle(Dismemberment ape, bool drawnAlong)
    {
        HashSet<Transform> bones = new HashSet<Transform>();
        SkinnedMeshRenderer[] renderers = ape.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Transform spine = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform[] list = renderers[i].bones;
            for (int j = 0; j < list.Length; j++)
            {
                Transform bone = list[j];
                if (bone != null && bones.Add(bone) && bone.name == "spine")
                {
                    spine = bone;
                }
            }
        }
        if (spine == null)
        {
            return null;
        }
        GillaWounds wounds = GillaWounds.On(ape);
        Vector3 local = spine.InverseTransformDirection(wounds.PlusX());
        int across;
        if (Mathf.Abs(local.x) < Mathf.Abs(local.y) || Mathf.Abs(local.x) < Mathf.Abs(local.z))
        {
            across = Mathf.Abs(local.z) <= Mathf.Abs(local.y) ? 1 : 2;
        }
        else
        {
            across = 0;
        }
        Limb l = new Limb();
        l.drawn = drawnAlong ? ape.transform : null;
        l.spine = spine;
        l.spineAt = spine.localScale;
        l.across = across;
        l.width = Mathf.Abs(spine.lossyScale.x) * 0.156f;
        foreach (Transform child in spine)
        {
            if (bones.Contains(child))
            {
                l.onSpine.Add((child, child.localScale));
            }
        }
        return l;
    }

    private void Watch()
    {
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null) continue;
            if (ape.IsLocal) continue;
            if (ape.fingersL == null) continue;
            if (ape.fingersR == null) continue;
            int had;
            int owner = Get<int>(ape, "owner"); /* ? */
            grips.TryGetValue(owner, out had);
            float right = ape.fingersR.grip;
            float left = ape.fingersL.grip;
            int value = (left > 0.7f ? 1 : 0) | (right > 0.7f ? 2 : 0);
            if (value != had)
            {
                grips[owner] = value;
                if (left > 0.7f && (had & 1) == 0)
                {
                    Closed(ape, false);
                }
                if (right > 0.7f && ((had >> 1) & 1) == 0)
                {
                    Closed(ape, true);
                }
            }
        }
    }

    private static void Closed(NetworkApe by, bool right)
    {
        Transform hand = right ? by.rightHand : by.leftHand;
        Transform other = right ? by.leftHand : by.rightHand;
        if (hand == null) return;
        if (other == null) return;

        MonsterPlayer local = MonsterPlayer.Local;
        float rig = MonsterPlayer.RigOf(by);
        float radius = local != null ? local.grabRadius : 0.45f;
        int punches = local != null ? local.punchesBeforeRip : 0;
        float apart = local != null ? local.pullApart : 0.2f;
        Transform grip = by.HandOf(right);
        Transform otherGrip = by.HandOf(!right);
        radius = rig * radius;
        float tears = rig * apart;

        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null) continue;
            if (ape == by) continue;
            if (ape.HolderHand != otherGrip) continue;
            Vector3 handAt = hand.position;
            Vector3 middleAt = ape.head /* ? */.position + ape.MiddleFromEyes;
            ApePart part;
            float dist;
            object[] partArgs = new object[] { ape, hand.position, radius, null, null };
            bool hit = (bool)Call(typeof(MonsterPlayer), null, "Part", partArgs); /* ? */
            part = partArgs[3] != null ? (ApePart)partArgs[3] : default(ApePart);
            dist = partArgs[4] != null ? (float)partArgs[4] : 0f;
            float d = (handAt - middleAt).magnitude;
            bool middle = d < radius && punches <= Get<int>(ape, "punches") /* ? */;
            if (middle && hit)
            {
                middle = d < dist * 1.5f;
            }
            if (hit || middle)
            {
                StartOn(by, right, ape, null, null, part, middle, grip, hand, other, tears);
            }
            return;
        }

        foreach (GillaDummy dummy in GillaDummy.All)
        {
            if (dummy == null) continue;
            if (!dummy.HeldBy(Get<PlayerRef>(by, "ownerRef") /* ? */, !right)) continue;
            if (dummy.tearing == null) continue;
            Vector3 handAt = hand.position;
            Vector3 middleAt = dummy.Middle;
            ApePart part;
            float dist;
            bool hit = dummy.PartNear(hand.position, radius, out part, out dist);
            float d = (handAt - middleAt).magnitude;
            bool middle = d < radius && punches <= Get<int>(dummy, "punches") /* ? */;
            if (middle && hit)
            {
                middle = d < dist * 1.5f;
            }
            if (hit || middle)
            {
                StartOn(by, right, null, dummy, null, part, middle, grip, hand, other, tears);
            }
            return;
        }

        GillaCorpse corpse = GillaCorpse.BodyIn(Get<int>(by, "owner") /* ? */, !right);
        if (corpse == null) return;
        if (corpse.RipsHere(hand.position, radius)) return;
        int nearest = corpse.NearestOn(hand.position, radius);
        if (nearest == 0) return;
        StartOn(by, right, null, null, corpse, (ApePart)(nearest - 1), false, grip, hand, other, tears);
    }

    private static void StartOn(NetworkApe by, bool right, NetworkApe who, GillaDummy dummy, GillaCorpse body, ApePart part, bool middle, Transform grip, Transform a, Transform b, float tears)
    {
        Dismemberment ape;
        bool yours;
        if (who != null && who.IsLocal)
        {
            ape = null;
            yours = true;
        }
        else if (who != null)
        {
            ape = who.tearing;
            yours = false;
        }
        else if (dummy != null)
        {
            ape = dummy.tearing;
            yours = false;
        }
        else
        {
            yours = false;
            ape = null;
        }
        Pull p = Pulling(ape, body, yours, part, middle, grip, a, b, tears);
        if (p != null)
        {
            p.by = by;
            p.right = right;
            p.who = who;
            p.dummy = dummy;
        }
    }

    private static bool Still(GillaPull.Pull p)
    {
        NetworkApe by = p.by;
        if (by == null) return false;
        HandPoser fingers = p.right ? by.fingersR : by.fingersL;
        if (fingers == null) return false;
        if (fingers.grip <= 0.7f) return false;
        Transform other = by.HandOf(!p.right);
        if (p.who != null)
        {
            if (p.who.HolderHand != other) return false;
            if (p.middle) return true;
            return !p.who.IsTorn((int)p.part);
        }
        if (p.dummy != null)
        {
            if (!p.dummy.HeldBy(Get<PlayerRef>(by, "ownerRef") /* ? */, !p.right)) return false;
            if (p.middle) return true;
            return !p.dummy.IsTorn(p.part);
        }
        if (p.body == null) return false;
        return GillaCorpse.BodyIn(Get<int>(by, "owner") /* ? */, !p.right) == p.body;
    }

    private void Update()
    {
        foreach (Pull p in pulls)
        {
            foreach (Limb l in p.limbs)
            {
                if (l.shoulder != null)
                {
                    l.shoulder.localPosition = l.shoulderAt;
                }
                if (l.head != null)
                {
                    l.head.localPosition = l.headAt;
                }
                if (l.spine != null)
                {
                    Unstretch(l);
                }
            }
        }
    }
}
