// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public partial class CosmeticWearer : MonoBehaviour
{
    [Tooltip("Wears what you're wearing (Cosmetics), for your own ape; else whoever owns it calls Show.")]
    public bool local;
    [Tooltip("Wears only the skin (your first-person body).")]
    public bool skinOnly;
    private Dismemberment body;
    private SkinnedMeshRenderer skin;
    private SkinnedMeshRenderer eyes;
    private readonly List<(SkinnedMeshRenderer suit, Renderer under)> suits = new List<ValueTuple<SkinnedMeshRenderer, Renderer>>();
    private int eyeSlot = -1;
    private Transform headBone;
    private Transform spineBone;
    private Transform handL;
    private Transform handR;
    private Transform knucklesL;
    private readonly byte[] ids = new byte[8];
    private readonly List<GameObject>[] worn = new List<GameObject>[8];
    private readonly List<Renderer> onHead = new List<Renderer>();
    private bool headChanged = true;
    private MaterialPropertyBlock eyeBlock;
    private MaterialPropertyBlock furBlock;
    private readonly List<(Transform bone, Vector3 rest)> eyeBones = new List<ValueTuple<Transform, Vector3>>();
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private readonly List<(Renderer r, int slot)> furSlots = new List<ValueTuple<Renderer, int>>();
    private readonly Dictionary<Renderer, Material[]> beforeSkin = new Dictionary<Renderer, Material[]>();
    private CosmeticSkin look;
    private Material eyeBefore;
    private Material bloodBefore;
    private Material eyeMat;
    private Texture2D eyeTex;
    private float eyeSize = 1f;
    private float eyeOut;
    private ApeColor colour;
    private Color shownFur;
    private CosmeticToy toy;
    private int toyState;
    private Transform toyLeft;
    private Transform toyRight;
    private Vector3 toyFromPos;
    private Quaternion toyFromRot;
    private Quaternion toyRest;
    private float toyHome = 1f;
    private float toyPop = 1f;
    private bool toyOnBody = true;
    private bool toyWasAway;
    private static readonly string[] OverEyes = new[] { "Glasses", "Goggles", "Shades", "Visor", "Mask" };
    private Action<ApePart, GameObject> onTornHandler; /* ? */

    public ApeFace Face { get; private set; }

    private static bool WearSuit_b__64_0(SkinnedMeshRenderer p)
    {
        return p.name == "Gilla_Torso";
    }

    private static bool WearSuit_b__1(CosmeticSkin info, int i, SkinnedMeshRenderer p)
    {
        return p.name == info.suitPieces[i];
    }

    private void Awake()
    {

        for (int i = 0; i < worn.Length; i++)
        {
            worn[i] = new List<GameObject>();
        }
        body = GetComponent<Dismemberment>();
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int r = 0; r < renderers.Length; r++)
        {
            SkinnedMeshRenderer smr = renderers[r];
            if (skin == null)
            {
                Transform[] bones = smr.bones;
                for (int b = 0; b < bones.Length; b++)
                {
                    Transform bone = bones[b];
                    if (bone != null && bone.name == "head")
                    {
                        skin = smr;
                        break;
                    }
                }
            }
            Material[] mats = smr.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null) continue;
                if (mats[m].name.Contains("Eye"))
                {
                    if (smr.name.Contains("Head"))
                    {
                        eyes = smr;
                        eyeSlot = m;
                    }
                }
                else
                {
                    furSlots.Add((smr, m));
                }
            }
        }
        if (skin != null)
        {
            Transform[] bones = skin.bones;
            for (int b = 0; b < bones.Length; b++)
            {
                Transform bone = bones[b];
                if (bone == null) continue;
                if (bone.name == "head")
                {
                    headBone = bone;
                }
                else if (bone.name == "spine")
                {
                    spineBone = bone;
                }
                else if (bone.name == "hand_L")
                {
                    handL = bone;
                }
                else if (bone.name == "hand_R")
                {
                    handR = bone;
                }
                else if (bone.name == "index1_L")
                {
                    knucklesL = bone;
                }
                else if (bone.name == "eye_L" || bone.name == "eye_R")
                {
                    eyeBones.Add((bone, bone.localScale));
                }
            }
        }
        Face = GetComponentInChildren<ApeFace>(true);
        if (Face == null)
        {
            Face = GetComponentInParent<ApeFace>();
        }
        if (body != null)
        {
            if (onTornHandler == null)
            {
                onTornHandler = (part, piece) => OnTorn((int)part, piece); /* ? */
            }
            body.PartTorn += onTornHandler;
            body.PartsReset += PutBack;
        }
    }

    private void OnDestroy()
    {
        if (body != null)
        {
            if (onTornHandler != null)
            {
                body.PartTorn -= onTornHandler; /* ? */
            }
            body.PartsReset -= PutBack;
        }
    }

    private void Update()
    {
        if (local)
        {
            for (int slot = 0; slot != 8; slot++)
            {
                Put((CosmeticSlot)slot, Cosmetics.In((CosmeticSlot)slot));
            }
            NetworkBootstrap nb = NetworkBootstrap.Instance;
            if (nb != null)
            {
                toyState = GillaToys.NetState;
                toyLeft = nb.leftHand;
                toyRight = nb.rightHand;
            }
        }
    }

    private void LateUpdate()
    {
        if (look != null)
        {
            KeepSkin();
        }
        foreach (var (suit, under) in suits)
        {
            if (suit != null && under != null && suit.enabled != under.enabled)
            {
                suit.enabled = under.enabled;
            }
        }
        Tint();
        if (toy != null)
        {
            PoseToy(Time.deltaTime);
        }
    }

    public void Show(byte head, byte face)
    {
        Put(CosmeticSlot.Head, head);
        Put(CosmeticSlot.Face, face);
    }

    public void Show(byte[] bySlot)
    {
        for (int i = 0; i < bySlot.Length; i++)
        {
            Put((CosmeticSlot)i, bySlot[i]);
            if (i + 1 == 8) return;
        }
    }

    public byte In(CosmeticSlot slot)
    {
        return ids[(int)slot];
    }

    public IEnumerable<GameObject> Worn()
    {
        foreach (List<GameObject> list in worn)
        {
            foreach (GameObject go in list)
            {
                if (go != null)
                {
                    yield return go;
                }
            }
        }
    }

    public bool TopOfHead(out float top)
    {
        top = float.MinValue;
        if (headChanged)
        {
            headChanged = false;
            onHead.Clear();
            if (headBone != null)
            {
                foreach (GameObject go in Worn())
                {
                    if (go.transform.IsChildOf(headBone))
                    {
                        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
                        for (int i = 0; i < rs.Length; i++)
                        {
                            Renderer r = rs[i];
                            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                            {
                                onHead.Add(r);
                            }
                        }
                    }
                }
            }
        }
        foreach (Renderer r in onHead)
        {
            if (r != null && r.enabled && r.gameObject.activeInHierarchy)
            {
                Bounds b = r.bounds;
                top = Mathf.Max(top, b.center.y + b.extents.y);
            }
        }
        return float.MinValue < top;
    }

    public bool CoversEyes
    {
        get
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (OverTheEyes(CosmeticCatalog.Of(ids[i])))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static bool OverTheEyes(CosmeticCatalog.Entry e)
    {
        if (e == null) return false;
        if (e.slot == CosmeticSlot.Face) return true;
        return Array.Exists(OverEyes, w => e.name.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public void HoldToy(int state, Transform left, Transform right)
    {
        toyState = state;
        toyLeft = left;
        toyRight = right;
    }

    public void Put(CosmeticSlot slot, byte id, bool again = false)
    {
        int s = (int)slot;
        int newId = id;
        if (skinOnly && slot != CosmeticSlot.Skin)
        {
            newId = 0;
        }
        CosmeticCatalog.Entry entry = CosmeticCatalog.Of(newId);
        if (!string.IsNullOrEmpty(entry != null ? entry.wornWith : null))
        {
            newId = 0;
        }
        if (ids[s] == newId && !again)
        {
            return;
        }
        ids[s] = (byte)newId;
        headChanged = true;
        if (Face != null)
        {
            Face.EyesCovered = CoversEyes;
        }
        foreach (GameObject go in worn[s])
        {
            if (go != null)
            {
                Gone(go);
            }
        }
        worn[s].Clear();
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(newId);
        bool isToy = false;
        if (s < 6)
        {
            if (slot == CosmeticSlot.Face)
            {
                if (Face != null)
                {
                    Face.MouthShut = e != null && e.shutsMouth;
                }
            }
            else if (slot == CosmeticSlot.Eyes)
            {
                if (e == null)
                {
                    eyeTex = null;
                    eyeMat = null;
                    eyeSize = 1f;
                    eyeOut = 0f;
                }
                else
                {
                    eyeTex = e.eyeTexture;
                    eyeMat = e.eyeMaterial;
                    eyeSize = e.eyeScale;
                    eyeOut = e.eyeTurn;
                }
                Eyes();
                return;
            }
        }
        else
        {
            if (slot == CosmeticSlot.Skin)
            {
                Skin(e);
                return;
            }
            if (slot == CosmeticSlot.Toy)
            {
                toy = null;
                isToy = true;
            }
        }
        if (e != null && e.prefab != null && skin != null)
        {
            GameObject go = UnityEngine.Object.Instantiate(e.prefab);
            go.name = e.prefab.name;
            Transform bone = (slot == CosmeticSlot.Body || slot == CosmeticSlot.Back || isToy) ? spineBone : headBone;
            Attach(go, bone);
            worn[s].Add(go);
            if (isToy)
            {
                toy = go.GetComponent<CosmeticToy>();
                toyRest = go.transform.localRotation;
                toyHome = 1f;
                toyOnBody = true;
            }
            bool show;
            if (body != null)
            {
                show = (uint)s > 1 || !body.IsTorn(ApePart.Head);
            }
            else
            {
                show = true;
            }
            foreach (GameObject g in worn[s])
            {
                g.SetActive(show);
            }
            shownFur = new Color(0f, 0f, 0f, 0f);
        }
    }

    private static void Gone(GameObject go)
    {
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(go);
            return;
        }
        UnityEngine.Object.DestroyImmediate(go);
    }

    private void Attach(GameObject go, Transform bone)
    {
        if (bone == null)
        {
            Gone(go);
            return;
        }
        go.transform.SetParent(bone, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = OnBone(skin, bone);
        go.transform.localScale = Vector3.one;
        Layer(go, skin.gameObject.layer);
    }

    private void Skin(CosmeticCatalog.Entry e)
    {
        foreach (KeyValuePair<Renderer, Material[]> kv in beforeSkin)
        {
            if (kv.Key != null)
            {
                kv.Key.sharedMaterials = kv.Value;
            }
        }
        beforeSkin.Clear();
        if (body != null)
        {
            body.SetCaps();
        }
        foreach (var (r, slot) in furSlots)
        {
            if (r != null)
            {
                if (furBlock == null)
                {
                    furBlock = new MaterialPropertyBlock();
                }
                r.GetPropertyBlock(furBlock, slot);
                furBlock.Clear();
                r.SetPropertyBlock(furBlock, slot);
            }
        }
        if (eyes != null && eyeBefore != null)
        {
            Material[] mats = eyes.sharedMaterials;
            mats[eyeSlot] = eyeBefore;
            eyes.sharedMaterials = mats;
        }
        eyeBefore = null;
        if (body != null && bloodBefore != null)
        {
            body.bloodMaterial = bloodBefore;
        }
        bloodBefore = null;
        look = null;
        shownFur = new Color(0f, 0f, 0f, 0f);
        suits.Clear();
        GameObject prefab = e == null ? null : e.prefab;
        CosmeticSkin info = null;
        if (prefab != null)
        {
            info = prefab.GetComponent<CosmeticSkin>();
        }
        if (info == null || skin == null)
        {
            Eyes();
            return;
        }
        look = info;
        if (colour == null)
        {
            colour = FindColour();
        }
        KeepSkin();
        if (body != null && info.blood != null)
        {
            bloodBefore = body.bloodMaterial;
            body.bloodMaterial = info.blood;
        }
        Eyes();
        WearSuit(info);
        foreach (Transform fx in prefab.transform)
        {
            Transform bone;
            if (fx.name == "FX_Head") bone = headBone;
            else if (fx.name == "FX_Spine") bone = spineBone;
            else if (fx.name == "FX_Hand_L") bone = handL;
            else if (fx.name == "FX_Hand_R") bone = handR;
            else bone = null;
            if (bone == null) continue;
            GameObject go = UnityEngine.Object.Instantiate(fx.gameObject);
            go.name = fx.name;
            go.transform.SetParent(bone, false);
            go.transform.localPosition = OnBone(skin, bone) * fx.localPosition;
            go.transform.localRotation = OnBone(skin, bone) * fx.localRotation;
            go.transform.localScale = fx.localScale;
            Layer(go, skin.gameObject.layer);
            go.SetActive(!TornFor(fx.name));
            worn[6].Add(go);
        }
        if (e != null && !skinOnly)
        {
            foreach (CosmeticCatalog.Entry with in CosmeticCatalog.WornWith(e.name))
            {
                GameObject go = UnityEngine.Object.Instantiate(with.prefab);
                go.name = with.prefab.name;
                Transform bone = (with.slot == CosmeticSlot.Body || with.slot == CosmeticSlot.Back || with.slot == CosmeticSlot.Toy) ? spineBone : headBone;
                Attach(go, bone);
                worn[6].Add(go);
            }
        }
    }
}
