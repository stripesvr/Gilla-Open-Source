// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using GorillaLocomotion;

[DefaultExecutionOrder(160)]
public class GillaToys : MonoBehaviour
{
    private static GillaToys you;
    private Player gt;
    private byte shown;
    private CosmeticToy toy;
    private Transform spine;
    private Quaternion rest;
    private int hand = -1;
    private int layer;
    private bool away;
    private float home = 1f;
    private float pop = 1f;
    private float hop = 1f;
    private Vector3 fromPos;
    private Vector3 hopFrom;
    private Quaternion fromRot;
    private SkinnedMeshRenderer body;

    public static int Held
    {
        get
        {
            if (you != null)
            {
                return you.hand;
            }
            return -1;
        }
    }

    public static byte NetState
    {
        get
        {
            if (you == null) return 0;
            if (you.toy == null) return 0;
            if (you.away) return 3;
            if (you.hand == 0) return 1;
            return (byte)(you.hand == 1 ? 2 : 0);
        }
    }

    public static CosmeticToy Toy
    {
        get
        {
            CosmeticToy result = null;
            if (you != null)
            {
                result = you.toy;
            }
            return result;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToPlayer();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Player player = UnityEngine.Object.FindAnyObjectByType<Player>();
        if (!(player != null)) return;
        if (!(player.GetComponent<GillaToys>() == null)) return;
        player.gameObject.AddComponent<GillaToys>();
    }

    public static void AttachToPlayer()
    {
        Player player = UnityEngine.Object.FindAnyObjectByType<Player>();
        if (!(player != null)) return;
        if (!(player.GetComponent<GillaToys>() == null)) return;
        player.gameObject.AddComponent<GillaToys>();
    }

    private void Awake()
    {

        you = this;
        gt = GetComponent<Player>();
    }

    private void OnDestroy()
    {
        if (you == this)
        {
            you = null;
        }
    }

    private void LateUpdate()
    {
        byte id;
        if (MonsterPlayer.IsMonster)
        {
            id = 0;
        }
        else
        {
            id = Cosmetics.In(CosmeticSlot.Toy);
        }
        if (shown != id)
        {
            Make(id);
        }
        if (toy == null) return;

        if (MonsterPlayer.IsMonster || (KillCam.Local != null && KillCam.Local.Busy))
        {
            if (hand >= 0 && toy.kind != ToyKind.Monkey)
            {
                Release(hand, Vector3.zero);
            }
        }

        Transform t = toy.transform;
        if (away) return;
        if (toy.kind == ToyKind.Monkey && !KeepMonkey()) return;

        if (hand < 0)
        {
            if (pop < 1f)
            {
                pop = Mathf.Min(pop + Time.deltaTime / 0.7f, 1f);
                t.localScale = Vector3.one * CosmeticToy.Elastic(pop);
            }
            if (home < 1f)
            {
                home = Mathf.Min(home + Time.deltaTime / 0.6f, 1f);
                float e = CosmeticToy.Elastic(home);
                t.localPosition = Vector3.LerpUnclamped(fromPos, Vector3.zero, e);
                t.localRotation = Quaternion.SlerpUnclamped(fromRot, rest, e);
            }
        }
        else
        {
            Transform handT = HandPose(hand);
            if (handT != null)
            {
                toy.PoseIn(handT, hand != 0 ? -1f : 1f);
            }
            if (hop < 1f)
            {
                float dt = Time.deltaTime;
                hop = Mathf.Min(hop + dt + dt, 1f);
                Vector3 target = t.position;
                float e = CosmeticToy.Elastic(hop);
                t.position = hopFrom + (target - hopFrom) * e;
            }
        }
    }

    private bool KeepMonkey()
    {
        if (gt == null || hand < 0)
        {
            return true;
        }
        int h = hand;
        if (Gone(h))
        {
            if (!Gone(1 - h))
            {
                hand = 1 - h;
                hop = 1f;
            }
        }
        bool gone = Gone(hand);
        if (gone == toy.gameObject.activeSelf)
        {
            toy.gameObject.SetActive(!gone);
        }
        return !gone;
    }

    private static object CallMove(IToyMove move, string name, params object[] args)
    {
        if (move == null) return null;
        MethodInfo m = move.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m == null) return null;
        return m.Invoke(move, args);
    }

    public static bool TryTake(int h, Vector3 at)
    {
        if (you != null && you.toy != null && you.toy.kind == ToyKind.Monkey)
        {
            you.HopTo(h, at);
            return false;
        }
        if (you == null) return false;
        if (you.toy == null) return false;
        if (you.away || you.hand >= 0 || !(0.5f <= you.home)) return false;
        if (MonsterPlayer.IsMonster) return false;
        if (KillCam.Local != null && KillCam.Local.Busy) return false;

        float dist = (you.toy.GrabPoint - at).magnitude;
        if (dist <= you.toy.reach)
        {
            you.hand = h;
            you.toy.transform.SetParent(you.transform, true);
            InputDevice device = InputDevices.GetDeviceAtXRNode(h != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, 0.5f, 0.06f);
            IToyMove[] moves = you.toy.GetComponents<IToyMove>();
            for (int i = 0; i < moves.Length; i++)
            {
                CallMove(moves[i], "Grab", h) /* ? */;
            }
            return true;
        }
        return false;
    }

    private void HopTo(int h, Vector3 at)
    {
        if (hand == h) return;
        Vector3 grab = toy.GrabPoint;
        if ((grab - at).magnitude <= toy.reach)
        {
            hand = h;
            hop = 0f;
            hopFrom = toy.transform.position;
            InputDevice device = InputDevices.GetDeviceAtXRNode(h != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, 0.5f, 0.06f);
        }
    }

    public static bool HeldIn(int h)
    {
        if (!(you != null)) return false;
        if (!(you.toy != null)) return false;
        if (you.hand != h) return false;
        return you.toy.kind != ToyKind.Monkey;
    }

    public static void Release(int h, Vector3 velocity)
    {
        if (you == null) return;
        if (you.toy == null) return;
        if (you.hand != h) return;
        you.hand = -1;
        IToyMove[] moves = you.toy.GetComponents<IToyMove>();
        bool thrown = false;
        for (int i = 0; i < moves.Length; i++)
        {
            thrown |= CallMove(moves[i], "Throw", velocity) is bool b && b /* ? */;
        }
        if (!thrown)
        {
            you.GoHome(false);
            return;
        }
        you.away = true;
        you.toy.gameObject.SetActive(false);
    }

    public static void Back()
    {
        if (you == null) return;
        if (you.toy == null) return;
        if (!you.away) return;
        you.away = false;
        you.toy.gameObject.SetActive(true);
        you.GoHome(true);
    }

    private void GoHome(bool popIn)
    {
        Transform t = toy.transform;
        if (spine == null)
        {
            t.SetParent(transform, true);
            return;
        }
        t.SetParent(spine, true);
        if (popIn)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = rest;
            home = 1f;
            pop = 0f;
            t.localScale = Vector3.zero;
            return;
        }
        fromPos = t.localPosition;
        fromRot = t.localRotation;
        home = 0f;
    }

    private void Make(byte id)
    {
        shown = id;
        if (toy != null)
        {
            Destroy(toy.gameObject);
        }
        toy = null;
        hand = -1;
        away = false;
        CosmeticCatalog.Entry entry = CosmeticCatalog.Of(id);
        if (entry == null) return;
        if (entry.prefab == null || entry.slot != CosmeticSlot.Toy) return;
        FindBody();
        if (body == null) return;
        if (spine == null) return;

        GameObject go = Instantiate<GameObject>(entry.prefab);
        go.name = entry.prefab.name + " (yours)";
        toy = go.GetComponent<CosmeticToy>();
        if (toy == null)
        {
            Destroy(go);
            return;
        }
        go.transform.SetParent(spine, false);
        go.transform.localPosition = Vector3.zero /* ? */;
        MethodInfo onBone = typeof(CosmeticWearer).GetMethod("OnBone", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        rest = onBone != null ? (Quaternion)onBone.Invoke(null, new object[] { body, spine }) : Quaternion.identity /* ? */;
        go.transform.localRotation = rest;
        go.transform.localScale = Vector3.one /* ? */;
        Transform[] all = go.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            all[i].gameObject.layer = layer;
        }
        IToyMove[] moves = go.GetComponents<IToyMove>();
        for (int i = 0; i < moves.Length; i++)
        {
            CallMove(moves[i], "Setup") /* ? */;
        }
        hop = 1f;
        home = 1f;
        pop = 1f;
        if (toy.kind != ToyKind.Monkey) return;
        hand = 0;
        go.transform.SetParent(transform, true);
    }

    private void FindBody()
    {
        if (body != null) return;
        NetworkBootstrap nb = NetworkBootstrap.Instance;
        if (nb == null) return;
        if (nb.tearing == null) return;
        Dismemberment[] copies = nb.tearing.copies;
        if (copies == null) return;
        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment d = copies[i];
            if (d == null) continue;
            Dismemberment.Part[] parts = d.parts;
            if (parts == null) continue;
            if (Array.Exists(parts, p => p.id == 0 && p.renderer != null)) continue;
            SkinnedMeshRenderer[] skins = d.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int j = 0; j < skins.Length; j++)
            {
                SkinnedMeshRenderer skin = skins[j];
                Transform[] bones = skin.bones;
                for (int k = 0; k < bones.Length; k++)
                {
                    Transform bone = bones[k];
                    if (bone != null && bone.name == "spine")
                    {
                        body = skin;
                        spine = bone;
                        layer = skin.gameObject.layer;
                        return;
                    }
                }
            }
        }
    }

    private Transform HandPose(int h)
    {
        NetworkBootstrap nb = NetworkBootstrap.Instance;
        if (nb != null)
        {
            return h == 0 ? nb.leftHand : nb.rightHand;
        }
        if (!(gt != null))
        {
            return null;
        }
        return h == 0 ? gt.leftHandFollower : gt.rightHandFollower;
    }

    private bool Gone(int h)
    {
        return h == 0 ? gt.leftHandDisabled : gt.rightHandDisabled;
    }
}
