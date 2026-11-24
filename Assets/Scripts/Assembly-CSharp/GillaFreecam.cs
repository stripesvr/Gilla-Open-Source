using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

public partial class GillaFreecam : MonoBehaviour
{
    private struct Pressable
    {
        public Component thing;
        public float radius;
        public Action press;
        public string label;
    }

    public static bool GIsTaken;
    private const int PlayerRig = 8;
    private const int FirstPerson = 9;
    private const int Sees = -769;
    private const float LookSpeed = 0.12f;
    private const float Shove = 3f;
    private Camera cam;
    private Camera player;
    private AudioListener ear;
    private AudioListener playerEar;
    private float yaw;
    private float pitch;
    private float speed = 4f;
    private float hintUntil;
    private float nextLook;
    private float nextNet;
    private float pressablesAt = -10f;
    private string under = "";
    private string net = "";
    private readonly List<Pressable> pressables = new List<GillaFreecam.Pressable>();
    private GUIStyle style;

    public static bool On { get; private set; }

    public Camera View
    {
        get { return cam; }
    }

    private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static object Peek(object o, string name)
    {
        if (o == null) return null;
        Type t = o.GetType();
        FieldInfo f = t.GetField(name, AnyMember);
        if (f != null) return f.GetValue(o);
        PropertyInfo p = t.GetProperty(name, AnyMember);
        if (p != null) return p.GetValue(o);
        return null;
    }

    private static void Poke(object o, string name, object value)
    {
        if (o == null) return;
        Type t = o.GetType();
        PropertyInfo p = t.GetProperty(name, AnyMember);
        if (p != null)
        {
            MethodInfo set = p.GetSetMethod(true);
            if (set != null) set.Invoke(set.IsStatic ? null : o, new object[] { value });
            return;
        }
        FieldInfo f = t.GetField(name, AnyMember);
        if (f != null) f.SetValue(f.IsStatic ? null : o, value);
    }

    private static bool GlassBroken(GillaGlass g)
    {
        object v = Peek(g, "broken") /* ? */;
        return v is bool b && b;
    }

    private void Start()
    {
        hintUntil = Time.unscaledTime + 8f;
    }

    private void OnDestroy()
    {
        if (On)
        {
            Leave();
            return;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }
        Mouse mouse = Mouse.current;
        if (keyboard.pKey.wasPressedThisFrame)
        {
            if (!On)
            {
                Enter();
            }
            else
            {
                Leave();
            }
        }
        if (!On)
        {
            return;
        }
        if (cam == null)
        {
            Leave();
            return;
        }
        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            float p = pitch - delta.y * LookSpeed;
            yaw = yaw + delta.x * LookSpeed;
            pitch = Mathf.Clamp(p, -89f, 89f);
        }
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        if (mouse != null)
        {
            Vector2 scroll = mouse.scroll.ReadValue();
            if (scroll.y != 0f)
            {
                float factor = 1.25f;
                if (scroll.y < 0f)
                {
                    factor = 0.8f;
                }
                speed = Mathf.Clamp(factor * speed, 0.25f, 60f);
            }
        }
        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move.z += 1f;
        if (keyboard.sKey.isPressed) move.z -= 1f;
        if (keyboard.dKey.isPressed) move.x += 1f;
        if (keyboard.aKey.isPressed) move.x -= 1f;
        if (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed) move.y += 1f;
        if (keyboard.qKey.isPressed || keyboard.leftCtrlKey.isPressed) move.y -= 1f;
        float boost = (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed) ? 4f : 1f;
        Vector3 dir = cam.transform.rotation * new Vector3(move.x, 0f, move.z) + Vector3.up * move.y;
        Transform t = cam.transform;
        Vector3 pos = t.position;
        if (dir.sqrMagnitude > 1f)
        {
            dir = dir.normalized;
        }
        t.position = pos + dir * (boost * speed * Time.unscaledDeltaTime);
        if (mouse != null)
        {
            Vector2 mp = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
            bool handled = false;
            if (mouse.leftButton.wasPressedThisFrame && !mouse.rightButton.isPressed)
            {
                Action act = Resolve(ray, out under);
                if (act != null)
                {
                    act();
                }
                handled = true;
            }
            if (!handled && nextLook <= Time.unscaledTime)
            {
                nextLook = Time.unscaledTime + 0.1f;
                Resolve(ray, out under);
            }
        }
        if (keyboard.tKey.wasPressedThisFrame)
        {
            BringPlayer();
        }
        if (nextNet <= Time.unscaledTime)
        {
            nextNet = Time.unscaledTime + 0.5f;
            net = Network();
        }
    }

    public void Switch()
    {
        if (On)
        {
            Leave();
            return;
        }
        Enter();
    }

    public void LookFrom(Vector3 from, Vector3 at)
    {
        if (cam == null)
        {
            return;
        }
        Vector3 euler = Quaternion.LookRotation(at - from, Vector3.up).eulerAngles;
        float x = euler.x;
        float p = x - 360f;
        if (x <= 180f)
        {
            p = x;
        }
        yaw = euler.y;
        pitch = p;
        cam.transform.SetPositionAndRotation(from, Quaternion.Euler(pitch, yaw, 0f));
    }

    public string ClickAt(Vector2 at)
    {
        string label = null;
        if (On && !(cam == null))
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(at.x, at.y, 0f));
            Action act = Resolve(ray, out label);
            if (act != null)
            {
                act();
            }
            return label;
        }
        return "off";
    }

    private void Enter()
    {
        player = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Freecam");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 300f;
            cam.fieldOfView = 80f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 1f);
            cam.cullingMask = Sees;
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            ear = go.AddComponent<AudioListener>();
        }
        Transform from = null;
        if (player != null)
        {
            from = player.transform;
        }
        Transform camT = cam.transform;
        camT.position = from != null ? from.position : Vector3.up * 1.5f;
        Vector3 euler = from != null ? from.eulerAngles : Vector3.zero;
        float p = euler.x - 360f;
        if (euler.x <= 180f)
        {
            p = euler.x;
        }
        yaw = euler.y;
        pitch = p;
        float depth = 0f;
        if (player != null)
        {
            depth = player.depth;
        }
        cam.depth = depth + 10f;
        cam.gameObject.SetActive(true);
        playerEar = player != null ? player.GetComponent<AudioListener>() : null;
        bool xr = XRSettings.isDeviceActive;
        ear.enabled = !xr;
        if (xr)
        {
            cam.tag = "Untagged";
        }
        else
        {
            if (player != null)
            {
                player.enabled = false;
                player.tag = "Untagged";
            }
            if (playerEar != null)
            {
                playerEar.enabled = false;
            }
            cam.tag = "MainCamera";
        }
        under = "";
        net = Network();
        On = true;
    }

    private void Leave()
    {
        On = false;
        if (cam != null)
        {
            cam.tag = "Untagged";
            cam.gameObject.SetActive(false);
        }
        if (player != null)
        {
            player.tag = "MainCamera";
            player.enabled = true;
        }
        if (!(playerEar != null))
        {
            return;
        }
        playerEar.enabled = true;
    }

    private Action Resolve(Ray ray, out string label)
    {
        RaycastHit hit;
        bool picked = Pick(ray, out hit);
        float reach = picked ? hit.distance + 0.1f : 300f;
        float best = float.MaxValue;
        Component bestThing = null;
        string bestLabel = null;
        Action bestPress = null;
        foreach (Pressable p in Pressables())
        {
            if (p.thing == null)
            {
                continue;
            }
            if (picked && hit.collider.transform.IsChildOf(p.thing.transform))
            {
                bestThing = p.thing;
                bestLabel = p.label;
                bestPress = p.press;
                break;
            }
            Vector3 to = p.thing.transform.position - ray.origin;
            float along = Vector3.Dot(to, ray.direction);
            if (along > 0f && along <= reach)
            {
                float off = (to - ray.direction * along).magnitude;
                if (off <= p.radius && off < best)
                {
                    best = off;
                    bestThing = p.thing;
                    bestLabel = p.label;
                    bestPress = p.press;
                }
            }
        }
        if (bestThing != null)
        {
            label = bestLabel + ": a click presses it";
            return bestPress;
        }
        string text = "nothing";
        if (picked)
        {
            GillaGlass glass = hit.collider.GetComponentInParent<GillaGlass>();
            if (glass != null && !GlassBroken(glass) /* ? */)
            {
                label = "glass: a click breaks it";
                Vector3 at = hit.point;
                Vector3 push = ray.direction * 4f;
                return () =>
                {
                    if (!GlassBroken(glass) /* ? */)
                    {
                        Poke(glass, "LastBreak", "a click") /* ? */;
                        glass.Break(at, push);
                        NetworkApe ape = NetworkApe.Local;
                        if (ape != null && ape.Object != null && ape.Object.Runner && ape.Object.Runner.Exists(ape.Object) /* ? */)
                        {
                            ape.RPC_BreakGlass(Convert.ToInt32(Peek(glass, "id")) /* ? */, at, push);
                        }
                    }
                };
            }
            MonsterBrain brain = hit.collider.GetComponentInParent<MonsterBrain>();
            if (brain != null)
            {
                string s = string.Format("{0}, {1}: ", brain.name.Replace("(Clone)", ""), brain.state /* ? */);
                string how;
                if (brain.net /* ? */ != null && brain.net.Live)
                {
                    how = brain.net.IsAuthority ? "on the network, run by you" : "on the network, the master client's puppet";
                }
                else
                {
                    how = "only yours";
                }
                label = s + how;
                return null;
            }
            Rigidbody body = hit.rigidbody;
            if (body != null && !body.isKinematic)
            {
                label = body.name + ": a click shoves it";
                Vector3 at2 = hit.point;
                Vector3 push2 = ray.direction * Shove;
                return () => body.AddForceAtPosition(push2, at2, ForceMode.Impulse);
            }
            text = string.Format("{0} ({1:0.0} m)", hit.collider.name, hit.distance);
        }
        label = text;
        return null;
    }

    private static bool Pick(Ray ray, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 300f, Sees, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit h = hits[i];
            if (!h.collider.isTrigger
                || h.collider.GetComponentInParent<GillaDoorButton>() != null
                || h.collider.GetComponentInParent<PressKey>() != null)
            {
                hit = h;
                return true;
            }
        }
        hit = default(RaycastHit);
        return false;
    }

    private List<Pressable> Pressables()
    {
        if (Time.unscaledTime - pressablesAt < 1f)
        {
            return pressables;
        }
        pressablesAt = Time.unscaledTime;
        pressables.Clear();
        foreach (GillaDoorButton b in UnityEngine.Object.FindObjectsByType<GillaDoorButton>(FindObjectsSortMode.None))
        {
            Pressable p = new Pressable();
            p.thing = b;
            p.radius = 0.16f;
            p.press = b.Click;
            string extra = "";
            if (b.door /* ? */ != null)
            {
                extra = " (" + b.door.name + ")";
            }
            p.label = "door button" + extra;
            pressables.Add(p);
        }
        foreach (PressKey k in UnityEngine.Object.FindObjectsByType<PressKey>(FindObjectsSortMode.None))
        {
            Pressable p = new Pressable();
            p.thing = k;
            p.radius = 0.03f;
            p.press = k.Click;
            p.label = "key " + Peek(k, "key") /* ? */;
            pressables.Add(p);
        }
        foreach (BecomeMonsterButton m in UnityEngine.Object.FindObjectsByType<BecomeMonsterButton>(FindObjectsSortMode.None))
        {
            Pressable p = new Pressable();
            p.thing = m;
            p.radius = m.radius /* ? */;
            p.press = m.Click;
            string s = "BE THE APE button";
            int index = Convert.ToInt32(Peek(m, "index")) /* ? */;
            if (index != 0)
            {
                s = "BE THE MONSTER button " + index.ToString();
            }
            p.label = s;
            pressables.Add(p);
        }
        foreach (MonsterSpawnButton m in UnityEngine.Object.FindObjectsByType<MonsterSpawnButton>(FindObjectsSortMode.None))
        {
            Pressable p = new Pressable();
            p.thing = m;
            p.radius = m.radius /* ? */;
            p.press = m.Click;
            string s = "clear the spawned monsters";
            if (m.prefab /* ? */ != null)
            {
                s = "spawn " + m.prefab.name;
            }
            p.label = s;
            pressables.Add(p);
        }
        foreach (RipButton r in UnityEngine.Object.FindObjectsByType<RipButton>(FindObjectsSortMode.None))
        {
            Pressable p = new Pressable();
            p.thing = r;
            p.radius = r.radius /* ? */;
            p.press = r.Click;
            p.label = "rip stand: " + r.action /* ? */.ToString();
            pressables.Add(p);
        }
        foreach (GillaModeButton g in UnityEngine.Object.FindObjectsByType<GillaModeButton>(FindObjectsSortMode.None))
        {
            Pressable p = new Pressable();
            p.thing = g;
            p.radius = 0.12f;
            p.press = g.Click;
            p.label = "PLAY (to the " + Peek(g, "lobby") /* ? */ + " lobby)";
            pressables.Add(p);
        }
        return pressables;
    }
}
