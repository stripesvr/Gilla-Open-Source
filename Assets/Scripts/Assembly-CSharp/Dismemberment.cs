// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-200)]
public class Dismemberment : MonoBehaviour
{
    [Serializable]
    public class Part
    {
        public ApePart id;
        [Tooltip("The part's own mesh. Empty when this copy has no such part (the first-person body has no head).")]
        public SkinnedMeshRenderer renderer;
        [Tooltip("Where it tears: the neck or shoulder end of the part.")]
        public Transform joint;
        [Tooltip("Points from the body out along the part (shoulder -> elbow, neck -> head).")]
        public Transform jointOut;
        [Tooltip("Bloody cap on the body, hidden until the part is torn.")]
        public GameObject stump;
        [HideInInspector]
        public bool torn;
    }

    public Part[] parts;
    [Tooltip("Only one copy of a player should throw the torn piece (the full-body one); the others just hide the part.")]
    public bool spawnPieces = true;
    public Material bloodMaterial;
    public Material fleshMaterial;
    [Tooltip("Torn pieces disappear after this many seconds.")]
    public float pieceLifetime = 45f;
    [Header("Sound")]
    [Tooltip("One is picked at random when a part comes off (played by the copy that sprays, so it never doubles).")]
    public AudioClip[] tearSounds;
    [Range(0, 1)]
    public float tearVolume = 1f;
    [Tooltip("A torn piece landing on the floor.")]
    public AudioClip[] dropSounds;
    [Header("Blood")]
    [Tooltip("How long the stump keeps spurting, seconds.")]
    public float bleedSeconds = 4f;
    [Tooltip("Heartbeat spurts over that time, each weaker than the last.")]
    public int bleedPulses = 6;
    [Tooltip("Droplets in the first gush.")]
    public int gushDroplets = 110;
    private readonly List<GameObject> pieces = new List<GameObject>();
    private static readonly ProfilerMarker TearMark = new ProfilerMarker("Gilla.Tear");
    private List<SkinnedMeshRenderer> capped;
    private Material capFlesh;
    private static Material anyFlesh;

    public event Action<ApePart, GameObject> PartTorn;
    public event Action PartsReset;

    private void Awake()
    {

        SetCaps();
    }

    public void SetCaps()
    {
        if (capped == null)
        {
            capped = new List<SkinnedMeshRenderer>();
            SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer s = skins[i];
                if (s.sharedMesh == null) continue;
                Material[] mats = s.sharedMaterials;
                Material last = null;
                if (mats.Length != 0 && s.sharedMesh.subMeshCount == mats.Length)
                    last = mats[mats.Length - 1];
                if (last == null) continue;
                if (last.name.Contains("Flesh"))
                {
                    capped.Add(s);
                    anyFlesh = last;
                    capFlesh = last;
                }
            }
        }

        bool anyTorn = false;
        for (int i = 0; i < parts.Length; i++)
            anyTorn = parts[i].torn || anyTorn;

        foreach (SkinnedMeshRenderer s in capped)
        {
            SkinnedMeshRenderer skin = s;
            if (skin == null) continue;
            if (skin.sharedMesh == null) continue;
            bool owned = Array.Exists(parts, p => p.renderer == skin);
            int count = skin.sharedMesh.subMeshCount;
            Material[] mats = skin.sharedMaterials;
            int lastIndex = count - 1;
            bool cap = anyTorn && !owned;
            if (cap == (mats.Length <= lastIndex))
            {
                Material[] arr = new Material[cap ? count : lastIndex];
                Array.Copy(mats, arr, Mathf.Min(mats.Length, arr.Length));
                if (cap)
                    arr[lastIndex] = capFlesh;
                skin.sharedMaterials = arr;
            }
        }
    }

    public static Material[] WithCaps(SkinnedMeshRenderer skin)
    {
        Material[] mats = skin.sharedMaterials;
        int count = skin.sharedMesh != null ? skin.sharedMesh.subMeshCount : 0;
        if (anyFlesh != null)
        {
            int len = mats.Length;
            if (count - 1 == len)
            {
                Material[] arr = new Material[count];
                mats.CopyTo(arr, 0);
                arr[len] = anyFlesh;
                mats = arr;
            }
        }
        return mats;
    }

    public bool IsTorn(ApePart id)
    {
        Part p = Find(id);
        return p != null && p.torn;
    }

    public void Tear(ApePart id, Vector3 impulse)
    {
        TearMark.Begin();
        Part p = Find(id);
        if (p != null && !p.torn)
        {
            p.torn = true;
            bool tearing = GillaPull.Tearing(this, id);
            GameObject piece;
            if (p.renderer != null)
            {
                if (spawnPieces && p.renderer.enabled)
                    piece = SpawnPiece(p, impulse);
                else
                    piece = null;
                p.renderer.enabled = false;
            }
            else
            {
                piece = null;
            }
            if (p.stump != null)
                p.stump.SetActive(true);
            SetCaps();
            if (spawnPieces && p.joint != null)
            {
                Sfx.Play(tearSounds, p.joint.position, tearVolume, 0.9f, 1.08f);
                if (tearing)
                    GillaPull.WetSnap(p.joint.position);
                if (bloodMaterial != null)
                {
                    Blood.Burst(p.joint.position, SprayDir(p), bloodMaterial, p.id != ApePart.Head ? 0.6f : 1f);
                    Blood.Spray(p.joint, SprayDir(p), bloodMaterial, bleedSeconds, gushDroplets, bleedPulses, 1f);
                }
                GillaSplatter.Around(p.joint.position, p.id == ApePart.Head ? 1.2f : 0.9f, this);
                if (piece != null)
                {
                    Vector3 scale = transform.lossyScale;
                    Vector3 outWay;
                    if (p.jointOut != null)
                        outWay = p.jointOut.position - p.joint.position;
                    else
                        outWay = Vector3.zero /* ? */;
                    bool head = p.id == ApePart.Head;
                    GillaStrands.Between(p.joint, piece.transform, p.joint.position, outWay, Vector3.zero, 0f /* ? */,
                        scale.y * (head ? 0.065f : 0.05f), head ? 18 : 15, 0f);
                }
            }
            PartTorn?.Invoke(id, piece);
        }
        TearMark.End();
    }

    private Vector3 SprayDir(Part p)
    {
        Part head = Find(ApePart.Head);
        Vector3 dir;
        if (p.id != ApePart.Head && head != null && head.joint != null)
            dir = p.joint.position - head.joint.position;
        else if (p.jointOut != null)
            dir = p.jointOut.position - p.joint.position;
        else
            dir = Vector3.zero;
        Vector3 flat = new Vector3(dir.x, 0f, dir.z).normalized;
        return (Vector3.up + flat * 0.8f).normalized;
    }

    public void ResetAll()
    {
        for (int i = 0; i < parts.Length; i++)
        {
            Part p = parts[i];
            p.torn = false;
            if (p.renderer != null)
                p.renderer.enabled = true;
            if (p.stump != null)
                p.stump.SetActive(false);
            if (p.joint != null)
            {
                foreach (Transform child in p.joint)
                {
                    if (child.name == "Blood")
                        Destroy(child.gameObject);
                }
            }
        }
        foreach (GameObject go in pieces)
        {
            if (go != null)
                Destroy(go);
        }
        pieces.Clear();
        PartsReset?.Invoke();
        SetCaps();
    }

    public void Take(GameObject piece)
    {
        pieces.Remove(piece);
    }

    public List<GameObject> TakePieces()
    {
        List<GameObject> result = new List<GameObject>(pieces);
        pieces.Clear();
        return result;
    }

    public Part Find(ApePart id)
    {
        if (parts != null)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].id == id)
                    return parts[i];
            }
        }
        return null;
    }

    private GameObject SpawnPiece(Part p, Vector3 impulse)
    {
        TornArm arm = null;
        BoxCollider box = null;
        SkinnedMeshRenderer skin = p.renderer;
        if (p.id != ApePart.Head)
        {
            string armName = "Torn_" + p.id.ToString();
            arm = TornArm.Make(skin, armName, p.id == ApePart.ArmL, WithCaps(skin), transform.forward, impulse,
                Random.insideUnitSphere * 7f);
        }

        void Clear(Collider c)
        {
            if (arm != null)
            {
                arm.Ignore(c);
                return;
            }
            Physics.IgnoreCollision(box, c);
        }

        GameObject go;
        Rigidbody rb;
        MeshRenderer mr;
        if (arm != null)
        {
            go = arm.gameObject;
            rb = null;
            mr = null;
        }
        else
        {
            Mesh mesh = new Mesh();
            mesh.name = "Torn_" + p.id.ToString();
            skin.BakeMesh(mesh, false);
            mesh.RecalculateBounds();
            go = new GameObject("Torn_" + p.id.ToString());
            go.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = WithCaps(skin);
            box = go.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.extents * 2f * 0.85f;
            go.layer = 2;
            rb = go.AddComponent<Rigidbody>();
            rb.mass = p.id != ApePart.Head ? 2f : 3f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = impulse;
            rb.angularVelocity = Random.insideUnitSphere * 7f;
        }

        Rigidbody parentBody = GetComponentInParent<Rigidbody>();
        if (parentBody != null)
        {
            Collider[] cols = parentBody.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                Clear(cols[i]);
        }
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player != null)
        {
            Collider[] cols = player.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                Clear(cols[i]);
        }

        TornPiece tp;
        if (arm != null)
            tp = arm.Piece;
        else
            tp = go.AddComponent<TornPiece>();
        tp.dropSounds = dropSounds;
        tp.life = pieceLifetime;
        if (arm != null)
            arm.CopyMaterials(tp.Own);
        else
            mr.sharedMaterials = tp.Own(mr.sharedMaterials);

        if (p.joint != null)
        {
            if (p.stump != null)
            {
                GameObject stumpCopy = Instantiate(p.stump, p.stump.transform.position, p.stump.transform.rotation, go.transform);
                stumpCopy.SetActive(true);
                Vector3 a = p.stump.transform.lossyScale;
                Vector3 b = go.transform.lossyScale;
                stumpCopy.transform.localScale = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
                Transform[] ts = stumpCopy.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < ts.Length; i++)
                    ts[i].gameObject.layer = 2;
            }
            if (bloodMaterial != null)
            {
                GameObject anchorGo = new GameObject("BloodAnchor");
                Transform anchor = anchorGo.transform;
                anchor.SetParent(go.transform, false);
                anchor.position = p.joint.position;
                Vector3 dir;
                if (p.jointOut != null)
                    dir = (p.joint.position - p.jointOut.position).normalized;
                else
                    dir = Vector3.down;
                Blood.Spray(anchor, dir, bloodMaterial, 2f, 45, 3, 1f);
            }
        }

        if (arm != null)
            arm.OutOfDoors();
        else
            GillaDoor.OutOfSlabs(go.transform, mr.bounds, rb);

        pieces.Add(go);
        return go;
    }
}
