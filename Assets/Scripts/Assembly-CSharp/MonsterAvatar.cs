// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(80)]
public partial class MonsterAvatar : MonoBehaviour
{
    private struct DisplayClass82_0
    {
        public MonsterAvatar owner;
        public string side;
    }

    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    [Tooltip("What the hands turn by (the tracked controllers); the hands themselves when not set.")]
    public Transform leftAim;
    [Tooltip("What the hands turn by (the tracked controllers); the hands themselves when not set.")]
    public Transform rightAim;
    [Tooltip("Another player's: their finger posers (NetworkApe, posed from the network), copied onto this body's.")]
    public HandPoser fingerSourceL;
    [Tooltip("Another player's: their finger posers (NetworkApe, posed from the network), copied onto this body's.")]
    public HandPoser fingerSourceR;
    public MonsterCatalog.Entry entry;
    public bool firstPerson;
    [Tooltip("How many times its own size it is (MonsterPlayer.ApeSize: the owner's SIZE page, on every headset).")]
    public float sizeBy = 1f;
    private Animator anim;
    private MonsterAnimEvents events;
    private readonly bool[] onFloor = new bool[2];
    private readonly Vector3[] handWas = new Vector3[2];
    private readonly float[] handSpeed = new float[2];
    private readonly float[] offFloor = new float[2];
    private readonly float[] stompIn = new float[2];
    private bool handsKnown;
    private Transform model;
    private Transform root;
    private Transform skull;
    private Transform neck;
    private Transform upL;
    private Transform loL;
    private Transform endL;
    private Transform upR;
    private Transform loR;
    private Transform endR;
    private Transform i1L;
    private Transform p1L;
    private Transform i1R;
    private Transform p1R;
    private Transform eyeL;
    private Transform eyeR;
    private Transform gripL;
    private Transform gripR;
    private float risen;
    private HandPoser posL;
    private HandPoser posR;
    private Vector3 lastHead;
    private Vector3 alignLocal;
    private Vector3 restUpL;
    private Vector3 restLoL;
    private Vector3 restUpR;
    private Vector3 restLoR;
    private int axisUpL;
    private int axisLoL;
    private int axisUpR;
    private int axisLoR;
    private float speed;
    private float yaw;
    private float pace;
    private bool placed;
    private bool aligned;
    private Transform chest;
    private Quaternion[] neckBase;
    private Quaternion[] skullBase;
    private Quaternion neckShare = Quaternion.identity;
    private Quaternion skullOnFace = Quaternion.identity;
    private bool looking;
    private bool baseLater;
    private bool faceKnown;
    private int lookFrames;
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int PaceId = Animator.StringToHash("Pace");
    private const int Floor = -5893;
    private const float GripOut = 1f;
    private const float StumpOverRoot = 0.04f;
    private const float NeckShare = 0.4f;
    private const float NeckEase = 10f;
    public static Action Stomped;

    public float EyeHeightNow { get; private set; }
    public string HeadStands { get; private set; } = "";

    public static MonsterAvatar Create(MonsterCatalog.Entry entry, Transform parent, Transform head, Transform l, Transform r, bool firstPerson)
    {
        GameObject go = new GameObject("MonsterBody_" + entry.name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * entry.playedSize;
        MonsterAvatar a = go.AddComponent<MonsterAvatar>();
        a.entry = entry;
        a.head = head;
        a.leftHand = l;
        a.rightHand = r;
        a.firstPerson = firstPerson;
        Transform src = null;
        if (entry.prefab != null)
            src = entry.prefab.transform.Find("Model");
        if (src == null)
            return a;
        GameObject m = UnityEngine.Object.Instantiate<GameObject>(src.gameObject, go.transform, false);
        m.name = "Model";
        Collider[] cols = m.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            UnityEngine.Object.Destroy(cols[i]);
        MonsterAnimEvents ev = m.GetComponent<MonsterAnimEvents>();
        if (ev != null)
        {
            ev.brain = null;
            ev.stepsByHand = entry.onArms;
        }
        a.events = ev;
        a.model = m.transform;
        a.anim = m.GetComponent<Animator>();
        a.root = a.Bone("root");
        a.skull = a.Bone("head");
        a.neck = a.Bone("neck");
        a.upL = a.Bone("upperarm_L");
        a.loL = a.Bone("lowerarm_L");
        a.endL = a.Bone("hand_L");
        a.upR = a.Bone("upperarm_R");
        a.loR = a.Bone("lowerarm_R");
        a.endR = a.Bone("hand_R");
        a.i1L = a.Bone("index1_L");
        a.p1L = a.Bone("pinkie1_L");
        a.i1R = a.Bone("index1_R");
        a.p1R = a.Bone("pinkie1_R");
        a.eyeL = a.Bone("eye_L");
        a.eyeR = a.Bone("eye_R");
        a.chest = a.neck != null ? a.neck.parent : null;
        if (!firstPerson)
        {
            a.Bases();
            a.Face();
        }
        if (a.upL != null && a.loL != null && a.endL != null)
        {
            a.restUpL = a.upL.localScale;
            a.restLoL = a.loL.localScale;
            a.axisUpL = Along(a.upL, a.loL);
            a.axisLoL = Along(a.loL, a.endL);
        }
        if (a.upR != null && a.loR != null && a.endR != null)
        {
            a.restUpR = a.upR.localScale;
            a.restLoR = a.loR.localScale;
            a.axisUpR = Along(a.upR, a.loR);
            a.axisLoR = Along(a.loR, a.endR);
        }
        if (firstPerson && entry.firstPersonMesh != null)
        {
            SkinnedMeshRenderer[] smrs = m.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
                smrs[i].sharedMesh = entry.firstPersonMesh;
        }
        a.posL = a.Poser("_L", XRNode.LeftHand, entry.palmL, !firstPerson);
        a.posR = a.Poser("_R", XRNode.RightHand, entry.palmR, !firstPerson);
        a.gripL = a.GripOf(a.endL, a.i1L, a.p1L, entry.palmL);
        a.gripR = a.GripOf(a.endR, a.i1R, a.p1R, entry.palmR);
        return a;
    }

    public Transform Grip(bool right)
    {
        return right ? gripR : gripL;
    }

    public void CloseFingers(bool right, float k)
    {
        HandPoser p = right ? posR : posL;
        if (p != null)
        {
            p.gripOverride = k; /* ? */
        }
    }

    public Vector3 FistFromHand(bool right)
    {
        Transform i1 = right ? i1R : i1L;
        Transform p1 = right ? p1R : p1L;
        Transform end = right ? endR : endL;
        if (i1 != null && p1 != null && end != null)
        {
            return ((i1.position + p1.position) * 0.5f - end.position) * 0.5f;
        }
        return Vector3.zero;
    }

    public bool Hand(bool right, out Vector3 wrist, out Vector3 palm, out Vector3 knuckles)
    {
        Transform end = right ? endR : endL;
        Transform i1 = right ? i1R : i1L;
        Transform p1 = right ? p1R : p1L;
        knuckles = Vector3.zero;
        palm = Vector3.zero;
        wrist = Vector3.zero;
        if (end == null)
            return false;
        wrist = end.position;
        Vector3 k;
        if (i1 != null && p1 != null)
        {
            k = (i1.position + p1.position) * 0.5f;
        }
        else
        {
            k = Vector3.zero;
            int count = 0;
            foreach (Transform c in end)
            {
                if (c.name != "Grip")
                {
                    k += c.position;
                    count++;
                }
            }
            if (count < 1)
                k = end.position;
            else
                k /= count;
        }
        knuckles = k;
        palm = (end.position + knuckles) * 0.5f;
        return true;
    }

    private Transform GripOf(Transform hand, Transform index1, Transform pinkie1, Vector3 palm)
    {
        if (hand == null || index1 == null || pinkie1 == null)
            return hand;
        Transform g = new GameObject("Grip").transform;
        g.SetParent(hand, false);
        Vector3 knuckles = (index1.position + pinkie1.position) * 0.5f;
        float depth = (hand.position - knuckles).magnitude;
        Vector3 wrist = hand.position;
        Vector3 n = hand.TransformDirection(palm).normalized;
        g.position = wrist + (knuckles - wrist) * 0.6f + n * depth;
        return g;
    }

    private Transform Bone(string n)
    {
        Transform[] all = model.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == n)
                return all[i];
        }
        return null;
    }

    private HandPoser Poser(string side, XRNode node, Vector3 palm, bool remote)
    {
        Transform hand = Bone("hand" + side);
        if (hand == null)
            return null;

        HandPoser.Finger Chain(string f)
        {
            return new HandPoser.Finger
            {
                bones = new Transform[] { Bone(f + "1" + side), Bone(f + "2" + side), Bone(f + "3" + side) } /* ? */
            } /* ? */;
        }

        HandPoser p = model.gameObject.AddComponent<HandPoser>();
        p.node = node;
        p.remote = remote;
        p.hand = hand;
        p.thumb = Chain("thumb");
        p.index = Chain("index");
        p.gripFingers = new HandPoser.Finger[] { Chain("pinkie") };
        p.fingerCurl = 80f;
        p.thumbCurl = 50f;
        p.fingerJointWeights = new float[] { 1f, 1f, 1f } /* ? */;
        p.thumbJointWeights = new float[] { 1f, 1f, 1f } /* ? */;
        p.palmNormalLocal = palm;
        p.desktopFallback = !remote;
        return p;
    }

    private void LateUpdate()
    {
        if (head == null) return;
        if (model == null) return;
        float dt = Time.deltaTime;
        float size = entry.playedSize * sizeBy;
        if (Mathf.Abs(transform.localScale.x - size) > 0.0001f)
            transform.localScale = Vector3.one * size;
        Vector3 headPos = head.position;
        float eye = sizeBy * entry.eyeHeight;
        float reach = entry.hangs ? (firstPerson ? 1.6f : 4f) : 1.6f;
        RaycastHit hit;
        Vector3 floor;
        if (Physics.Raycast(headPos, Vector3.down, out hit, eye * reach + 0.5f, Floor, QueryTriggerInteraction.Ignore))
            floor = hit.point;
        else
            floor = headPos + Vector3.down * eye;

        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        float targetYaw = fwd.sqrMagnitude <= 0.0001f ? yaw : Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        if (dt <= 0.0001f) dt = 0.0001f;
        if (placed)
            targetYaw = yaw + Mathf.DeltaAngle(yaw, targetYaw) * Mathf.Clamp01(1f - Mathf.Exp(dt * -6f));
        yaw = targetYaw;
        transform.SetPositionAndRotation(floor, Quaternion.Euler(0f, yaw, 0f));
        if (!firstPerson)
            Look(dt);

        Vector3 eyes;
        if (eyeL != null && eyeR != null)
            eyes = (eyeL.position + eyeR.position) * 0.5f;
        else if (skull != null)
            eyes = skull.position;
        else
            eyes = headPos;

        float drop;
        if (!entry.hangs)
            drop = 0f;
        else if (!firstPerson)
            drop = Propped(headPos.y - eyes.y, headPos.y - floor.y, floor.y, dt);
        else
            drop = Mathf.Clamp(headPos.y - eyes.y, eye * -0.5f, eye * 0.4f);

        Vector3 offset = Quaternion.Inverse(transform.rotation) * new Vector3(headPos.x - eyes.x, 0f, headPos.z - eyes.z) + Vector3.up * drop;
        float dist;
        if (!firstPerson && skull != null)
        {
            dist = Mathf.Abs(drop)
                 + Vector3.ProjectOnPlane(headPos - skull.position, Vector3.up).magnitude
                 + (eyes - skull.position).magnitude;
        }
        else
        {
            dist = offset.magnitude;
        }
        if (Mathf.Abs(drop) + sizeBy * 1.5f < dist)
            offset = Vector3.zero;
        if (aligned && !(firstPerson && entry.hangs))
            offset = Vector3.Lerp(alignLocal, offset, Mathf.Clamp01(1f - Mathf.Exp(dt * -12f)));
        alignLocal = offset;
        aligned = true;
        transform.position = transform.position + transform.rotation * alignLocal;
        EyeHeightNow = eyes.y - floor.y;

        float spd;
        if (!placed)
        {
            spd = 0f;
        }
        else
        {
            float dx = headPos.x - lastHead.x;
            float dz = headPos.z - lastHead.z;
            float k = Mathf.Clamp01(1f - Mathf.Exp(dt * -6f));
            spd = speed + (Mathf.Sqrt(dx * dx + dz * dz) / dt - speed) * k;
        }
        speed = spd;
        lastHead = headPos;
        placed = true;

        float walk = Mathf.Max(entry.walkSpeed * sizeBy, 0.1f);
        float run = Mathf.Max(sizeBy * entry.runSpeed, walk + 0.1f);
        bool skipAnim = false;
        if (anim != null)
        {
            if (!entry.onArms)
            {
                if (firstPerson)
                {
                    Steps(walk, dt);
                    skipAnim = true;
                }
                else
                {
                    float v;
                    if (speed <= walk)
                        v = speed / walk;
                    else
                        v = Mathf.InverseLerp(walk, run, speed) + 1f;
                    anim.SetFloat(SpeedId, v, 0.1f, dt);
                    anim.SetFloat(PaceId, 1f);
                }
            }
            else
            {
                anim.SetFloat(SpeedId, firstPerson ? 1f : 0f);
                anim.SetFloat(PaceId, firstPerson ? 0f : 1f);
            }
        }

        float w = 1f;
        bool doHands = true;
        if (!firstPerson && !entry.onArms)
        {
            w = 1f - Mathf.InverseLerp(walk * 1.2f, walk * 2.2f, speed);
            doHands = 0.01f < w;
        }
        if (doHands)
        {
            Vector3 right = transform.right;
            Vector3 forward = transform.forward;
            if (leftHand != null && endL != null)
            {
                Transform aim = leftAim != null ? leftAim : leftHand;
                Quaternion turned = Turned(endL, i1L, p1L, entry.palmL, aim.rotation, 1f);
                Vector3 wrist = WristFor(leftHand.position, endL, i1L, p1L, turned);
                if (entry.hangs)
                    Stretch(upL, loL, endL, wrist, restUpL, restLoL, axisUpL, axisLoL, 1f);
                Reach(upL, loL, endL, wrist, -right, -forward, 1f);
                endL.rotation = Quaternion.Slerp(endL.rotation, turned, w);
            }
            if (rightHand != null && endR != null)
            {
                Transform aim = rightAim != null ? rightAim : rightHand;
                Quaternion turned = Turned(endR, i1R, p1R, entry.palmR, aim.rotation, -1f);
                Vector3 wrist = WristFor(rightHand.position, endR, i1R, p1R, turned);
                if (entry.hangs)
                    Stretch(upR, loR, endR, wrist, restUpR, restLoR, axisUpR, axisLoR, 1f /* ? */);
                Reach(upR, loR, endR, wrist, right, -forward, 1f /* ? */);
                endR.rotation = Quaternion.Slerp(endR.rotation, turned, w);
            }
        }

        Copy(fingerSourceL, posL);
        Copy(fingerSourceR, posR);
        if (entry.onArms && events != null)
            Stomps(dt);
        if (firstPerson && entry.firstPersonMesh == null)
        {
            if (skull != null)
                skull.localScale = Vector3.one * 0.001f; /* ? */
            if (neck != null)
                neck.localScale = Vector3.one * 0.001f; /* ? */
        }
    }

    private float Propped(float wanted, float above, float floorY, float dt)
    {
        float eyeH = entry.eyeHeight * sizeBy;
        float stump;
        if (root != null)
            stump = Mathf.Max(root.position.y + StumpOverRoot - floorY, 0f);
        else
            stump = eyeH * 0.5f;
        bool loose = offFloor[0] < 0.6f || offFloor[1] < 0.6f;
        float lift = entry.playedSize * 0.7f * sizeBy;
        float v = -stump;
        if (loose && -stump <= wanted)
        {
            float cap = entry.playedSize * 0.1f * sizeBy;
            v = wanted;
            if (cap < wanted) v = cap;
        }
        float t = v;
        if (v <= above - eyeH - lift) t = above - eyeH - lift;
        if (above <= eyeH + lift) t = v;
        if (upL != null && loL != null && endL != null)
        {
            float arm = (loL.position - upL.position).magnitude + (endL.position - loL.position).magnitude;
            float hy = floorY;
            if (leftHand != null) hy = leftHand.position.y;
            float ry = floorY;
            if (rightHand != null) ry = rightHand.position.y;
            float top = Mathf.Max(hy, ry);
            float need = top - (arm * 1.25f + upL.position.y);
            if (t <= need) t = need;
        }
        float result = t;
        if (placed)
        {
            float rate = t <= risen ? 6f : 5f;
            result = Mathf.MoveTowards(risen, t, rate * dt);
        }
        risen = result;
        return result;
    }

    private void Look(float dt)
    {
        if (skull == null) return;
        if (neck == null) return;
        if (chest == null) return;
        if (head == null) return;
        if (!faceKnown) return;
        if (neckBase == null)
        {
            if (!baseLater) return;
            lookFrames++;
            if (lookFrames < 2) return;
            baseLater = false;
            Quaternion n = neck.localRotation;
            Quaternion s = skull.localRotation;
            neckBase = new Quaternion[] { n, n, n };
            skullBase = new Quaternion[] { s, s, s };
            HeadStands = "first frame";
            if (neckBase == null) return;
        }
        float sp = 0f;
        if (anim != null)
            sp = Mathf.Clamp(anim.GetFloat(SpeedId), 0f, 2f);
        int i = (int)sp;
        if (0 < i) i = 1;
        float f = sp - i; /* ? */
        Quaternion chestRot = chest.rotation;
        Quaternion neckRest = chestRot * Quaternion.Slerp(neckBase[i], neckBase[i + 1], f);
        Quaternion skullRest = neckRest * Quaternion.Slerp(skullBase[i], skullBase[i + 1], f);
        Quaternion want = head.rotation * skullOnFace; /* ? */
        Quaternion turn = Quaternion.Inverse(chest.rotation) * want * Quaternion.Inverse(skullRest) * chest.rotation; /* ? */
        Quaternion share = Quaternion.Slerp(Quaternion.identity, turn, NeckShare);
        if (looking)
            share = Quaternion.Slerp(neckShare, share, 1f - Mathf.Exp(dt * -NeckEase));
        neckShare = share;
        looking = true;
        neck.rotation = chest.rotation * neckShare * Quaternion.Inverse(chest.rotation) * neckRest; /* ? */
        skull.rotation = want;
    }

    private void Face()
    {
        SkinnedMeshRenderer[] smrs = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < smrs.Length; i++)
        {
            SkinnedMeshRenderer smr = smrs[i];
            if (smr.sharedMesh == null)
                continue;
            Transform[] bones = smr.bones;
            Matrix4x4[] binds = smr.sharedMesh.bindposes;
            int si = Array.IndexOf<Transform>(bones, skull);
            int el = eyeL != null ? Array.IndexOf<Transform>(bones, eyeL) : -1;
            int er = eyeR != null ? Array.IndexOf<Transform>(bones, eyeR) : -1;
            if (si < 0 || si >= binds.Length)
                continue;
            Quaternion toLocal = Quaternion.Inverse(transform.rotation) * smr.transform.rotation;
            Vector3 fwd;
            if ((el | er) < 0 || el >= binds.Length || er >= binds.Length)
            {
                fwd = Vector3.forward;
            }
            else
            {
                Vector3 a = binds[el].inverse.rotation * Vector3.up;
                Vector3 b = binds[er].inverse.rotation * Vector3.up;
                fwd = toLocal * (a + b);
            }
            Quaternion faceInv = Quaternion.Inverse(Quaternion.LookRotation(fwd, Vector3.up));
            Quaternion skullRot = binds[si].inverse.rotation;
            faceKnown = true;
            skullOnFace = Quaternion.Inverse(faceInv * toLocal * skullRot) /* ? */;
            return;
        }
    }
}
