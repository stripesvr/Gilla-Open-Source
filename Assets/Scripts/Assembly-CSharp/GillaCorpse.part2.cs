using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaCorpse : MonoBehaviour
{
    private void Weigh(Bounds bounds, Vector3 velocity, Vector3 spin)
    {
        box = gameObject.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.extents * 2f * 0.85f, Vector3.one * 0.15f);
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.mass = 12f;
        rb.solverIterations = 20;
        rb.solverVelocityIterations = 8;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxDepenetrationVelocity = 3f;
        rb.linearVelocity = velocity;
        rb.angularVelocity = spin;
        IgnoreYou(box);
        Ragdoll(rb, velocity);
    }

    public Bounds WorldBox()
    {
        Bounds b = new Bounds(transform.TransformPoint(box.center), Vector3.zero);
        for (int k = 0; k < 8; k++)
        {
            Vector3 c = box.center;
            Vector3 s = box.size;
            Vector3 corner = new Vector3(
                c.x + s.x * 0.5f * (((k * 2) & 2) - 1),
                c.y + s.y * 0.5f * ((k & 2) - 1),
                c.z + s.z * 0.5f * (((k >> 1) & 2) - 1));
            b.Encapsulate(transform.TransformPoint(corner));
        }
        return b;
    }

    private void CopySkeleton(Dismemberment from, bool inside)
    {
        using (CopyMark.Auto())
        {
            HashSet<Transform> wanted = new HashSet<Transform>();
            SkinnedMeshRenderer[] skins = from.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer smr = skins[i];
                if (smr.name == "SonarGhost") continue;
                if (smr.name == "SuitTrim") continue;
                if (smr.GetComponentInParent<CosmeticToy>() != null) continue;
                Want(smr.transform);
                Transform[] smrBones = smr.bones;
                for (int j = 0; j < smrBones.Length; j++)
                {
                    Want(smrBones[j]);
                }
                if (smr.sharedMesh == null) continue;
                Matrix4x4[] poses = smr.sharedMesh.bindposes;
                smrBones = smr.bones;
                for (int j = 0; j < smrBones.Length; j++)
                {
                    if (j >= poses.Length) break;
                    if (smrBones[j] != null)
                    {
                        binds[smrBones[j].name] = poses[j];
                    }
                }
            }
            GameObject root = new GameObject("Skeleton");
            root.layer = 2;
            Transform rootT = root.transform;
            rootT.SetParent(transform, false);
            rootT.localScale = from.transform.lossyScale;
            List<(GillaWoundBend made, Transform other, Matrix4x4 bind)> bends = new List<(GillaWoundBend made, Transform other, Matrix4x4 bind)>();
            Copy(from.transform, rootT);
            foreach (var bend in bends)
            {
                Transform mapped;
                if (bend.other != null)
                {
                    if (!skeleton.TryGetValue(bend.other, out mapped)) mapped = null;
                }
                else
                {
                    mapped = null;
                }
                bend.made.other = mapped /* ? */;
                bend.made.bind = bend.bind /* ? */;
                bend.made.Bend();
            }

            void Want(Transform t)
            {
                Transform top = from.transform;
                while (t != null && wanted.Add(t))
                {
                    if (t == top) break;
                    t = t.parent;
                }
            }

            void Copy(Transform src, Transform dst)
            {
                skeleton[src] = dst;
                for (int i = 0; i < src.childCount; i++)
                {
                    Transform child = src.GetChild(i);
                    if (!wanted.Contains(child)) continue;
                    GameObject made = new GameObject(child.name);
                    made.layer = 2;
                    Transform madeT = made.transform;
                    madeT.SetParent(dst, false);
                    madeT.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
                    madeT.localScale = child.localScale;
                    bones[child.name] = madeT;
                    Copy(child, madeT);
                }
            }
        }
    }

    private void Wear(Dismemberment from)
    {
        CosmeticWearer wearer = from.GetComponent<CosmeticWearer>();
        if (wearer == null) return;
        GameObject holder = new GameObject("Wearing");
        holder.SetActive(false);
        foreach (GameObject worn in wearer.Worn())
        {
            if (!worn.activeInHierarchy) continue;
            if (worn.GetComponentInChildren<CosmeticToy>(true) != null) continue;
            if (worn.transform.parent == null) continue;
            Transform onto;
            if (!skeleton.TryGetValue(worn.transform.parent, out onto)) continue;
            GameObject copy = Instantiate(worn, holder.transform);
            copy.name = "Worn_" + worn.name;
            Joint[] joints = copy.GetComponentsInChildren<Joint>(true);
            for (int i = 0; i < joints.Length; i++) DestroyImmediate(joints[i]);
            MonoBehaviour[] behaviours = copy.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = behaviours.Length - 1; i >= 0; i--) DestroyImmediate(behaviours[i]);
            Rigidbody[] rbs = copy.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rbs.Length; i++) DestroyImmediate(rbs[i]);
            Collider[] cols = copy.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) DestroyImmediate(cols[i]);
            Transform[] all = copy.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++) all[i].gameObject.layer = 2;
            copy.transform.SetParent(onto, false);
            copy.transform.SetLocalPositionAndRotation(worn.transform.localPosition, worn.transform.localRotation);
            copy.transform.localScale = worn.transform.localScale;
        }
        Destroy(holder);
    }

    private void TakeWorn(GillaCorpse c, int sign)
    {
        Transform spine;
        if (!c.bones.TryGetValue("spine", out spine)) return;
        Vector3 plusX = c.PlusX();
        List<Transform> worn = new List<Transform>();
        Transform[] all = c.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name.StartsWith("Worn_")) worn.Add(all[i]);
        }
        foreach (Transform t in worn)
        {
            if (t.parent == null) continue;
            Transform onto;
            if (!bones.TryGetValue(t.parent.name, out onto)) continue;
            Vector3 center = t.position;
            Renderer[] renderers = t.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 0; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                center = b.center;
            }
            int side = Vector3.Dot(plusX, center - spine.position) >= 0f ? 1 : -1;
            if (side == sign) t.SetParent(onto, true);
        }
    }

    public bool Woundable
    {
        get
        {
            if (Owner == Id && !Ripped && !ripping)
            {
                return Has(0);
            }
            return false;
        }
    }

    public GillaWounds Hurt
    {
        get
        {
            if (hurtHere != null) return hurtHere;
            hurtHere = GillaWounds.OnBody(this, holes, holeWays, holeSides, holeCount);
            return hurtHere;
        }
    }

    public bool Surface(Vector3 at, float reach, out GoreSection section, out Vector3 world, out Vector3 body, out Vector3 normal, out float inside)
    {
        section = GoreSection.Torso;
        normal = Vector3.zero;
        body = Vector3.zero;
        world = Vector3.zero;
        inside = 0f;
        if (!Woundable) return false;
        return Hurt.Surface(at, reach, out section, out world, out body, out normal, out inside);
    }

    public Vector3 WorldOn(GoreSection section, Vector3 point)
    {
        if (Woundable)
        {
            point = Hurt.WorldOn(section, point);
        }
        return point;
    }

    public Vector3 OnBody(GoreSection section, Vector3 world)
    {
        if (!Woundable) return world;
        string name;
        if (section == GoreSection.Torso)
        {
            name = "spine";
        }
        else if (section == GoreSection.Head)
        {
            name = "head";
        }
        else
        {
            string side = section == GoreSection.ArmL ? "_L" : "_R";
            Transform upper;
            Transform lower;
            bones.TryGetValue("upperarm" + side, out upper);
            bones.TryGetValue("lowerarm" + side, out lower);
            string part = "upperarm";
            if (upper != null && lower != null)
            {
                Vector3 toLower = world - lower.position;
                Vector3 mid = upper.position + (lower.position - upper.position) * 0.5f;
                Vector3 toMid = world - mid;
                if (toMid.sqrMagnitude > toLower.sqrMagnitude) part = "lowerarm";
            }
            name = part + side;
        }
        // The instance OnBody(string, Vector3) on GillaWounds is not accessible here; invoked via reflection.
        Vector3 result;
        CallHidden(out result, Hurt, "OnBody", name, world) /* ? */;
        return result;
    }

    public void Stab(GoreSection section, Vector3 point, float radius)
    {
        if (!Woundable) return;
        GillaWounds hurt = Hurt;
        // NearestSkin, wounds, axes, sides, count are private in GillaWounds.cs; accessed via reflection.
        Vector3 skin;
        CallHidden(out skin, hurt, "NearestSkin", section, point) /* ? */;
        hurt.Add(section, skin, radius);
        Vector4[] w;
        GetHidden(out w, hurt, "wounds") /* ? */;
        holes = (Vector4[])w.Clone();
        GetHidden(out w, hurt, "axes") /* ? */;
        holeWays = (Vector4[])w.Clone();
        GetHidden(out w, hurt, "sides") /* ? */;
        holeSides = (Vector4[])w.Clone();
        GetHidden(out holeCount, hurt, "count") /* ? */;
    }

    public void Stain(Vector3 at, float radius)
    {
        if (!Woundable) return;
        GillaBloodied bloodied = GillaBloodied.OnBody(gameObject);
        GillaStains.Job j = new GillaStains.Job();
        j.point = at;
        j.radius = radius;
        j.stuff = GillaSplatter.Stuff.Blood;
        // Send is private in GillaBloodied.cs; invoked via reflection.
        InvokeHidden(bloodied, "Send", j) /* ? */;
    }

    public void Scratch(Vector3 at, Vector3 to, float width)
    {
        if (!Woundable) return;
        GillaBloodied bloodied = GillaBloodied.OnBody(gameObject);
        GillaStains.Job j = new GillaStains.Job();
        j.point = at;
        j.to = to;
        j.radius = width;
        j.line = true;
        j.stuff = GillaSplatter.Stuff.Blood;
        // Find, Painter, Pieces, Share, sheet are private in GillaBloodied.cs; accessed via reflection.
        InvokeHidden(bloodied, "Find", j) /* ? */;
        GillaBloodied painter;
        CallHidden(out painter, bloodied, "Painter") /* ? */;
        CallHidden(out j.pieces, painter, "Pieces", j) /* ? */;
        if (j.pieces.Count == 0) return;
        InvokeHidden(bloodied, "Share") /* ? */;
        GetHidden(out j.sheet, bloodied, "sheet") /* ? */;
        j.big = Mathf.Max(0.05f, bloodied.transform.lossyScale.y);
        j.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GillaStains.Add(j);
    }

    private SkinnedMeshRenderer CopySkin(SkinnedMeshRenderer smr)
    {
        SkinnedMeshRenderer copy = skeleton[smr.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
        copy.sharedMesh = smr.sharedMesh;
        copy.sharedMaterials = Copies(Dismemberment.WithCaps(smr));
        Transform[] src = smr.bones;
        Transform[] mapped = new Transform[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            if (src[i] != null)
            {
                skeleton.TryGetValue(src[i], out mapped[i]);
            }
        }
        copy.bones = mapped;
        if (smr.rootBone != null)
        {
            Transform root;
            if (skeleton.TryGetValue(smr.rootBone, out root))
            {
                copy.rootBone = root;
            }
        }
        copy.localBounds = smr.localBounds;
        copy.updateWhenOffscreen = true;
        copy.shadowCastingMode = smr.shadowCastingMode;
        GillaSplatter.CarryOver(smr, copy);
        return copy;
    }

    private const BindingFlags HiddenFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static object InvokeHidden(object target, string method, params object[] args)
    {
        MethodInfo[] methods = target.GetType().GetMethods(HiddenFlags);
        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name != method) continue;
            ParameterInfo[] ps = methods[i].GetParameters();
            if (ps.Length == args.Length)
            {
                return methods[i].Invoke(target, args);
            }
            if (ps.Length == 0)
            {
                return methods[i].Invoke(target, null);
            }
        }
        return null;
    }

    private static void CallHidden<T>(out T result, object target, string method, params object[] args)
    {
        object r = InvokeHidden(target, method, args);
        result = r is T t ? t : default(T);
    }

    private static void GetHidden<T>(out T result, object target, string field)
    {
        FieldInfo f = target.GetType().GetField(field, HiddenFlags);
        object r = f != null ? f.GetValue(target) : null;
        result = r is T t ? t : default(T);
    }
}
