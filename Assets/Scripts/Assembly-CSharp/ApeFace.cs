// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class ApeFace : MonoBehaviour
{
    [Header("Model")]
    public SkinnedMeshRenderer face;
    public Transform head;
    public Transform eyeL;
    public Transform eyeR;

    [Header("Voice")]
    [Tooltip("The local player's mic. Leave empty on remote players / NPCs and set ExternalLevel instead.")]
    public MicLevel mic;
    [Range(0, 1)]
    public float externalLevel;
    [Tooltip("Voice level (0..1) where the scream starts to take over.")]
    [Range(0, 1)]
    public float screamFrom = 0.7f;
    [Tooltip("How much the mouth flaps on its own while talking (the funny part).")]
    [Range(0, 1)]
    public float wobble = 0.3f;
    public float openSpeed = 14f;
    public float closeSpeed = 9f;

    [Header("Scream (user 11-04: \"make the eyes scale more... the white part of the eye scales, not the pupil... have the mouth shake a little like Animal Company does\")")]
    [Tooltip("How much bigger again the eyeballs swell at a full scream (on top of the Scream shape's own, about 1.45 times); their pupils stay the size they were. Not behind glasses (EyesCovered): there they keep their size.")]
    public float screamEyes = 0.25f;
    [Tooltip("How far the mouth shakes on a scream (a share of its opening), and how fast.")]
    public float shake = 0.16f;
    [Tooltip("How far the mouth shakes on a scream (a share of its opening), and how fast.")]
    public float shakeSpeed = 34f;
    [Tooltip("The pupil's size on the eye texture: how far down it the black reaches from the top (BludApe_Eye: 0.234).")]
    public float pupilReach = 0.234f;

    [Header("Eyes")]
    [Tooltip("Avatars with the same id are the same player (your first-person body and your mirror copy) - they never stare at each other.")]
    public int playerId;
    public float lookRange = 8f;
    public float maxEyeAngle = 40f;
    public float eyeSpeed = 12f;

    private static readonly List<ApeFace> all = new List<ApeFace>();
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

    private readonly List<(SkinnedMeshRenderer skin, int mouth, int scream)> alongWith = new List<(SkinnedMeshRenderer skin, int mouth, int scream)>();
    private int mouthIdx = -1;
    private int screamIdx = -1;
    private int eyeSlot = -1;
    private float mouth;
    private float scream;
    private float swellShown = 1f;
    private float shapeGrowth;
    private Quaternion restL;
    private Quaternion restR;
    private Vector3 gazeL;
    private Vector3 gazeR;
    private Vector3 sideL;
    private Vector3 sideR;
    private Vector3 baseL;
    private Vector3 baseR;
    private Vector3 setL;
    private Vector3 setR;
    private Vector3 placeL;
    private Vector3 placeR;
    private Vector3 growL;
    private Vector3 growR;
    private bool held;
    private MaterialPropertyBlock eyeBlock;
    private bool ready;

    public float Scream
    {
        get
        {
            if (MouthShut) return 0f;
            return scream;
        }
    }

    public float Talk
    {
        get
        {
            if (MouthShut) return 0f;
            return mouth;
        }
    }

    public bool MouthShut { get; set; }
    public bool EyesCovered { get; set; }
    public float EyesOut { get; set; }

    public static bool Held(Transform bone, out Vector3 place, out Vector3 scale)
    {
        scale = Vector3.zero;
        place = Vector3.zero;
        ApeFace f = null;
        if (bone != null)
        {
            f = bone.GetComponentInParent<ApeFace>(true);
        }
        if (!(f == null))
        {
            if (f.held)
            {
                if (bone != f.eyeL && bone != f.eyeR)
                {
                    return false;
                }
                place = bone == f.eyeL ? f.placeL : f.placeR;
                scale = bone == f.eyeL ? f.baseL : f.baseR;
                return true;
            }
        }
        return false;
    }

    private void OnEnable()
    {

        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    private void Start()
    {
        if (face != null && face.sharedMesh != null)
        {
            mouthIdx = face.sharedMesh.GetBlendShapeIndex("MouthOpen");
            screamIdx = face.sharedMesh.GetBlendShapeIndex("Scream");
            Material[] mats = face.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].name.Contains("Eye"))
                {
                    eyeSlot = i;
                }
            }
            shapeGrowth = EyeGrowth(face, screamIdx, eyeL, out growL);
            EyeGrowth(face, screamIdx, eyeR, out growR);
            if (face.transform.parent != null)
            {
                SkinnedMeshRenderer[] skins = face.transform.parent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int i = 0; i < skins.Length; i++)
                {
                    SkinnedMeshRenderer s = skins[i];
                    if (s == face) continue;
                    if (s.name == "SonarGhost") continue;
                    if (s.sharedMesh == null) continue;
                    int m = s.sharedMesh.GetBlendShapeIndex("MouthOpen");
                    int sc = s.sharedMesh.GetBlendShapeIndex("Scream");
                    if (m > -1 || sc > -1)
                    {
                        alongWith.Add((s, m, sc));
                    }
                }
            }
        }
        if (eyeL != null && eyeR != null)
        {
            restL = eyeL.localRotation;
            restR = eyeR.localRotation;
            gazeL = eyeL.parent.InverseTransformDirection(transform.forward);
            gazeR = eyeR.parent.InverseTransformDirection(transform.forward);
            placeL = eyeL.localPosition;
            placeR = eyeR.localPosition;
            Vector3 side = (placeL - placeR).normalized;
            sideL = side;
            ready = true;
            sideR = -side;
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        float level = mic != null ? mic.Level : externalLevel;

        float talk = level / Mathf.Max(screamFrom, 0.05f);
        if (talk >= 0f)
        {
            float t = talk > 1f ? 1f : talk;
            if (talk > 1f || t > 0.05f)
            {
                float w = wobble;
                float noise = Mathf.PerlinNoise(Time.time * 11f, playerId * 3.7f);
                talk = t * ((1f - w) + w * noise * 1.6f);
            }
        }
        else
        {
            talk = 0f;
        }

        float screamTarget = Mathf.Clamp01((level - screamFrom) / Mathf.Max(1f - screamFrom, 0.05f));

        float speed = talk <= mouth ? closeSpeed : openSpeed;
        mouth = Mathf.MoveTowards(mouth, Mathf.Clamp01(talk), dt * speed);

        float sSpeed = screamTarget <= scream ? closeSpeed * 0.5f : openSpeed;
        scream = Mathf.MoveTowards(scream, screamTarget, dt * sSpeed);

        float jitter = 0f;
        if (scream > 0.01f)
        {
            float noise = Mathf.PerlinNoise(Time.time * shakeSpeed, playerId * 7.3f + 11f);
            jitter = scream * shake * (noise + noise - 1f);
        }

        float mouthW = 0f;
        float screamW = 0f;
        if (!MouthShut)
        {
            screamW = Mathf.Clamp01(jitter + scream) * 100f;
            mouthW = mouth * (1f - scream) * 100f;
        }
        if (mouthIdx > -1) face.SetBlendShapeWeight(mouthIdx, mouthW);
        if (screamIdx > -1) face.SetBlendShapeWeight(screamIdx, screamW);

        foreach (var item in alongWith)
        {
            if (item.skin == null) continue;
            if (item.mouth > -1) item.skin.SetBlendShapeWeight(item.mouth, mouthW);
            if (item.scream > -1) item.skin.SetBlendShapeWeight(item.scream, screamW);
        }

        float open = screamW / 100f;
        if (screamIdx < 0) open = 0f;

        if (ready)
        {
            if (held)
            {
                eyeL.localPosition = placeL;
                eyeR.localPosition = placeR;
            }
            Transform target = ClosestOtherHead();
            Aim(eyeL, restL, gazeL, sideL, target, dt);
            Aim(eyeR, restR, gazeR, sideR, target, dt);
            held = EyesCovered;
            if (!EyesCovered)
            {
                float g = 1f;
                float k = screamEyes * scream + 1f;
                if (!MouthShut)
                {
                    g = scream * shapeGrowth + 1f;
                }
                Swell(k, k * g);
            }
            else
            {
                Hold(open);
            }
        }
    }

    private static float EyeGrowth(SkinnedMeshRenderer smr, int shape, Transform eyeBone, out Vector3 from)
    {
        from = Vector3.zero;
        Mesh mesh = smr.sharedMesh;
        int bone = eyeBone != null ? Array.IndexOf(smr.bones, eyeBone) : -1;
        float growth = 0.45f;
        if ((bone | shape) > -1)
        {
            if (mesh.isReadable)
            {
                Vector3[] deltas = new Vector3[mesh.vertexCount];
                mesh.GetBlendShapeFrameVertices(shape, mesh.GetBlendShapeFrameCount(shape) - 1, deltas, null, null);
                Vector3[] verts = mesh.vertices;
                BoneWeight[] weights = mesh.boneWeights;
                Bounds before = new Bounds();
                Bounds after = new Bounds();
                bool any = false;
                for (int i = 0; i < verts.Length; i++)
                {
                    if (weights[i].boneIndex0 == bone && weights[i].weight0 >= 0.9f)
                    {
                        if (!any)
                        {
                            before = new Bounds(verts[i], Vector3.zero);
                            after = new Bounds(verts[i] + deltas[i], Vector3.zero);
                        }
                        any = true;
                        before.Encapsulate(verts[i]);
                        after.Encapsulate(verts[i] + deltas[i]);
                    }
                }
                if (any)
                {
                    growth = 0.45f;
                    if (before.size.magnitude > 1e-06f)
                    {
                        growth = Mathf.Max(0f, after.size.magnitude / before.size.magnitude - 1f); /* ? */
                        if (growth > 0.0001f)
                        {
                            Matrix4x4[] bindposes = mesh.bindposes;
                            Vector3 pivot = (before.center * (growth + 1f) - after.center) / growth;
                            from = bindposes[bone].MultiplyPoint3x4(pivot); /* ? */
                        }
                    }
                }
            }
            else
            {
                growth = 0.45f;
            }
        }
        return growth;
    }

    public Vector3 Unswelled(Transform eye)
    {
        if (ready)
        {
            if (eye == eyeL) return baseL;
            if (eye == eyeR) return baseR;
        }
        return eye.localScale;
    }

    private void Swell(float k, float total)
    {
        if ((eyeL.localScale - setL).sqrMagnitude > 1e-10f)
        {
            baseL = eyeL.localScale;
        }
        if ((eyeR.localScale - setR).sqrMagnitude > 1e-10f)
        {
            baseR = eyeR.localScale;
        }
        if (baseL.x > 0f && baseR.x > 0f && (baseL - baseR).sqrMagnitude >= 9.9999994e-11f)
        {
            Vector3 smaller = baseL.x >= baseR.x ? baseR : baseL;
            baseL = smaller;
            baseR = smaller;
        }
        setL = baseL * k;
        eyeL.localScale = setL;
        setR = baseR * k;
        eyeR.localScale = setR;
        Pupils(total);
    }

    private void Hold(float open)
    {
        float k = shapeGrowth * open + 1f;
        HoldOne(eyeL, ref baseL, ref setL, placeL, growL, k);
        HoldOne(eyeR, ref baseR, ref setR, placeR, growR, k);
        Pupils(1f);
    }

    private static void HoldOne(Transform eye, ref Vector3 size, ref Vector3 set, Vector3 place, Vector3 from, float k)
    {
        if ((eye.localScale - set).sqrMagnitude > 1e-10f)
        {
            size = eye.localScale;
        }
        set = size / k;
        eye.localScale = set;
        Quaternion rotation = eye.localRotation;
        float f = 1f - 1f / k;
        Vector3 point = new Vector3(f * from.x * size.x, f * from.y * size.y, f * from.z * size.z);
        eye.localPosition = place + rotation * point; /* ? */
    }

    private void Pupils(float total)
    {
        if (eyeSlot < 0 || (Mathf.Abs(total - 1f) < 0.001f && Mathf.Abs(swellShown - 1f) < 0.001f))
        {
            return;
        }
        swellShown = total;
        float reach = pupilReach * Mathf.PI;
        float s = Mathf.Clamp01(Mathf.Sin(Mathf.Min(reach, Mathf.PI / 2f)) / total);
        float angle = Mathf.Asin(s);
        float scale = 1f;
        if (angle > 0.001f)
        {
            scale = Mathf.Max(1f, reach / angle);
        }
        if (eyeBlock == null)
        {
            eyeBlock = new MaterialPropertyBlock();
        }
        face.GetPropertyBlock(eyeBlock, eyeSlot);
        eyeBlock.SetVector(BaseMapST, new Vector4(1f, scale, 0f, 1f - scale));
        face.SetPropertyBlock(eyeBlock, eyeSlot);
    }

    private Transform ClosestOtherHead()
    {
        if (head == null) return null;
        float best = lookRange * lookRange;
        Transform result = null;
        foreach (ApeFace other in all)
        {
            if (other == this) continue;
            if (other.playerId == playerId) continue;
            if (other.head == null) continue;
            float d = (other.head.position - head.position).sqrMagnitude;
            if (d < best)
            {
                result = other.head;
                best = d;
            }
        }
        return result;
    }

    private void Aim(Transform eye, Quaternion rest, Vector3 gaze, Vector3 side, Transform target, float dt)
    {
        Quaternion goal = TurnedOut(gaze, side, EyesOut) * rest;
        if (target != null)
        {
            Transform parent = eye.parent;
            Vector3 dir = (target.position + target.up * 0.08f - eye.position).normalized;
            Vector3 local = parent.InverseTransformDirection(dir);
            Vector3 look = Vector3.RotateTowards(gaze, local, maxEyeAngle * 0.017453292f, 0f);
            goal = Quaternion.FromToRotation(gaze, look) * goal;
        }
        float t = 1f - Mathf.Exp(-(eyeSpeed * dt));
        eye.localRotation = Quaternion.Slerp(eye.localRotation, goal, t);
    }

    private static Quaternion TurnedOut(Vector3 gaze, Vector3 side, float degrees)
    {
        return Quaternion.AngleAxis(degrees, Vector3.Cross(gaze, side));
    }
}
