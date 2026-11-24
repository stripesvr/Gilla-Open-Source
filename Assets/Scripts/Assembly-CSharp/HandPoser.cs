// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

public class HandPoser : MonoBehaviour
{
    [Serializable]
    public class Finger
    {
        [Tooltip("Knuckle first, fingertip last.")]
        public Transform[] bones;
    }

    private class Curler
    {
        public Transform[] bones;
        public Quaternion[] rest;
        public Vector3[] axis;
        public float value;
        public float vel;
    }

    public XRNode node = XRNode.RightHand;
    [Header("Bones")]
    [Tooltip("The hand/wrist these fingers hang off - used to work out which way is 'closed'.")]
    public Transform hand;
    public HandPoser.Finger thumb;
    public HandPoser.Finger index;
    [Tooltip("Every finger the grip button closes.")]
    public HandPoser.Finger[] gripFingers;
    [Header("Feel")]
    public float fingerCurl = 85f;
    public float thumbCurl = 55f;
    [Tooltip("How hard the finger is pulled to the pressed pose. Higher = faster.")]
    public float responsiveness = 17f;
    [Tooltip("How far past the pose a finger swings before settling. 0 = dead stop.")]
    [Range(0, 1)]
    public float overshoot = 0.35f;
    [Tooltip("Knuckles bend less than the joints above them, like a real hand.")]
    [Range(0, 1)]
    public float knuckleWeight = 0.75f;
    [Tooltip("Flip if this hand's fingers bend backwards. The auto-detect gets it right on most rigs but a mirrored hand can fool it.")]
    public bool invertCurl;
    [Header("Rig facts (set by the setup pass)")]
    [Tooltip("Which way the palm faces, in the hand bone's own space. When set, every finger curls TOWARD it and the thumb folds toward the middle of the palm - no guessing from knuckle positions (which picked backwards on the Blender hand: its knuckles sit right on the line the guess measured from).")]
    public Vector3 palmNormalLocal;
    [Tooltip("Share of the curl each joint of a finger takes, knuckle first (empty = the knuckle takes Knuckle Weight, the rest take all). Three-joint fingers need less per joint or they fold through the palm.")]
    public float[] fingerJointWeights;
    [Tooltip("The same for the thumb.")]
    public float[] thumbJointWeights;
    [Tooltip("Forced grip while holding an item - the fingers wrap the object no matter what the buttons say. Set by ItemGrabber.")]
    [Range(0, 1)]
    public float gripOverride;
    [Header("Desktop testing (no headset)")]
    [Tooltip("Left mouse = trigger, right mouse = grip, space = thumb.")]
    public bool desktopFallback = true;
    [Header("Another player's ape")]
    [Tooltip("Pose from Trigger, Grip and Thumb Press as NetworkApe sets them from the network, not from this machine's controllers (which would copy your fingers onto everyone).")]
    public bool remote;
    [HideInInspector]
    public float trigger;
    [HideInInspector]
    public float grip;
    [HideInInspector]
    public float thumbPress;
    private HandPoser.Curler _thumb;
    private HandPoser.Curler _index;
    private HandPoser.Curler[] _grip;
    private Vector3 _palmCentre;
    private Vector3 _palmN;
    private Vector3 _palmFace;
    private bool _ready;

    private void Start()
    {
        Vector3 across = Vector3.zero;
        if (hand == null || (across = KnuckleAxis()).sqrMagnitude < 1e-06f)
        {
            enabled = false;
            return;
        }
        _palmCentre = PalmCentre();
        if (palmNormalLocal.sqrMagnitude <= 1e-06f)
            _palmN = Vector3.zero;
        else
            _palmN = hand.TransformDirection(palmNormalLocal).normalized;

        if (_palmN != Vector3.zero)
        {
            Vector3 knuckles = KnuckleCentre();
            float len = Vector3.Distance(hand.position, knuckles);
            Vector3 wrist = hand.position;
            _palmFace = Vector3.Lerp(wrist, knuckles, 0.55f) + _palmN * len * 0.6f;
        }

        _thumb = Build(thumb, true, across);
        _index = Build(index, false, across);
        if (gripFingers != null)
        {
            List<Curler> list = new List<Curler>();
            for (int i = 0; i < gripFingers.Length; i++)
            {
                Curler c = Build(gripFingers[i], false, across);
                if (c != null)
                    list.Add(c);
            }
            _grip = list.ToArray();
        }
        _ready = true;
    }

    private Vector3 KnuckleAxis()
    {
        List<Vector3> pts = new List<Vector3>();
        if (index != null && index.bones != null && index.bones.Length != 0 && index.bones[0] != null)
            pts.Add(index.bones[0].position);
        if (gripFingers != null)
        {
            for (int i = 0; i < gripFingers.Length; i++)
            {
                Finger f = gripFingers[i];
                if (f != null && f.bones != null && f.bones.Length != 0 && f.bones[0] != null)
                    pts.Add(f.bones[0].position);
            }
        }
        if (pts.Count < 2)
            return hand.right;

        Vector3 best = Vector3.zero;
        float bestDist = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            for (int j = i + 1; j < pts.Count; j++)
            {
                Vector3 d = pts[j] - pts[i];
                float m = d.magnitude;
                if (bestDist < m)
                {
                    bestDist = m;
                    best = d;
                }
            }
        }
        return best.normalized;
    }

    private Vector3 KnuckleCentre()
    {
        Vector3 sum = Vector3.zero;
        int n = 0;
        if (index != null && index.bones != null && index.bones.Length != 0 && index.bones[0] != null)
        {
            sum += index.bones[0].position;
            n = 1;
        }
        if (gripFingers != null)
        {
            for (int i = 0; i < gripFingers.Length; i++)
            {
                Finger f = gripFingers[i];
                if (f != null && f.bones != null && f.bones.Length != 0 && f.bones[0] != null)
                {
                    sum += f.bones[0].position;
                    n++;
                }
            }
        }
        if (n < 1)
            return hand.position;
        return sum / n;
    }

    private Vector3 PalmCentre()
    {
        Vector3 sum = Vector3.zero;
        int n = 0;
        void Add(Finger f)
        {
            if (f != null && f.bones != null && f.bones.Length != 0 && f.bones[0] != null)
            {
                sum += f.bones[0].position;
                n++;
            }
        }
        Add(index);
        Add(thumb);
        if (gripFingers != null)
        {
            for (int i = 0; i < gripFingers.Length; i++)
                Add(gripFingers[i]);
        }
        if (n < 1)
            return hand.position;
        return sum / n;
    }

    private HandPoser.Curler Build(HandPoser.Finger f, bool isThumb, Vector3 across)
    {
        if (f == null || f.bones == null || f.bones.Length == 0)
            return null;
        for (int i = 0; i < f.bones.Length; i++)
        {
            if (f.bones[i] == null)
                return null;
        }

        Curler c = new Curler();
        c.bones = f.bones;
        c.rest = new Quaternion[f.bones.Length];
        c.axis = new Vector3[f.bones.Length];

        int last = f.bones.Length - 1;
        Vector3 tip = f.bones[last].position;
        if (last < 1)
        {
            if (f.bones[last].childCount > 0)
                tip = f.bones[last].GetChild(0).position;
        }
        else
        {
            tip += f.bones[last].position - f.bones[last - 1].position;
        }

        for (int i = 0; i < f.bones.Length; i++)
        {
            Transform b = f.bones[i];
            c.rest[i] = b.localRotation;

            Vector3 next = tip;
            if (i + 1 < f.bones.Length)
                next = f.bones[i + 1].position;
            Vector3 dir = next - b.position;
            if (dir.sqrMagnitude < 1e-08f && b.childCount > 0)
                dir = b.GetChild(0).position - b.position;
            if (dir.sqrMagnitude < 1e-08f)
                dir = b.up;
            dir = dir.normalized;

            Vector3 side = across;
            if (isThumb)
            {
                side = Vector3.Cross(dir, across);
                if (_palmN != Vector3.zero)
                {
                    Vector3 toFace = _palmFace - b.position;
                    Vector3 cr = Vector3.Cross(dir, toFace);
                    if (cr.sqrMagnitude > 1e-08f)
                        side = cr;
                }
            }

            Vector3 axis = across;
            if (side.sqrMagnitude >= 1e-08f)
                axis = side;
            axis = Vector3.ProjectOnPlane(axis, dir);
            if (axis.sqrMagnitude < 1e-08f)
                axis = Vector3.Cross(dir, Vector3.up);
            axis = axis.normalized;

            Vector3 toTip = tip - b.position;
            bool flip = false;
            if (_palmN == Vector3.zero)
            {
                Vector3 a1 = b.position + Quaternion.AngleAxis(-55f, axis) * toTip;
                Vector3 a2 = b.position + Quaternion.AngleAxis(55f, axis) * toTip;
                if (Vector3.Distance(a1, _palmCentre) < Vector3.Distance(a2, _palmCentre))
                    flip = true;
            }
            else
            {
                Vector3 moved = Quaternion.AngleAxis(30f, axis) * toTip - toTip;
                if (isThumb)
                {
                    float dPlus = Vector3.Distance(b.position + toTip + moved, _palmFace);
                    float dMinus = Vector3.Distance(b.position + toTip - moved, _palmFace);
                    if (dMinus <= dPlus)
                        flip = true;
                }
                else if (Vector3.Dot(moved, _palmN) <= 0f)
                {
                    flip = true;
                }
            }
            if (flip)
                axis = -axis;
            if (invertCurl)
                axis = -axis;

            c.axis[i] = b.InverseTransformDirection(axis);
        }
        return c;
    }

    private void LateUpdate()
    {
        if (!_ready)
            return;
        if (!remote)
            ReadInput(out trigger, out grip, out thumbPress);

        float g = gripOverride;
        float t = trigger;
        float gr = grip;
        float th = thumbPress;
        if (0f < g)
        {
            if (gr <= g) gr = g;
            if (t <= g * 0.9f) t = g * 0.9f;
            if (th <= g * 0.7f) th = g * 0.7f;
        }
        float dt = Time.deltaTime;
        Apply(_index, t, fingerCurl, dt, fingerJointWeights);
        Apply(_thumb, th, thumbCurl, dt, thumbJointWeights);
        if (_grip != null)
        {
            for (int i = 0; i < _grip.Length; i++)
                Apply(_grip[i], gr, fingerCurl, dt, fingerJointWeights);
        }
    }

    private void Apply(HandPoser.Curler c, float target, float degrees, float dt, float[] weights)
    {
        if (c == null)
            return;
        float k = Mathf.Max(responsiveness, 0.01f);
        float o = Mathf.Clamp01(overshoot);
        float damp = 1f - o * 0.6f;
        c.vel += (k * k * (target - c.value) - k * (damp + damp) * c.vel) * dt;
        c.value = Mathf.Clamp(c.value + c.vel * dt, -0.25f, 1.25f);

        for (int i = 0; i < c.bones.Length; i++)
        {
            float w;
            if (weights == null || weights.Length == 0)
                w = i == 0 ? knuckleWeight : 1f;
            else
                w = weights[Mathf.Min(i, weights.Length - 1)];
            c.bones[i].localRotation = c.rest[i] * Quaternion.AngleAxis(w * degrees * c.value, c.axis[i]);
        }
    }

    private void ReadInput(out float trigger, out float grip, out float thumbPress)
    {
        thumbPress = 0f;
        grip = 0f;
        trigger = 0f;
        bool primary = false;
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            device.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            device.TryGetFeatureValue(CommonUsages.grip, out grip);
            if (!device.TryGetFeatureValue(CommonUsages.primaryButton, out primary))
                return;
            thumbPress = primary ? 1f : 0f;
            return;
        }
        if (!desktopFallback)
            return;
        if (GillaFreecam.On)
            return;
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            trigger = mouse.leftButton.isPressed ? 1f : 0f;
            grip = mouse.rightButton.isPressed ? 1f : 0f;
        }
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;
        if (!kb.spaceKey.isPressed)
            return;
        thumbPress = 1f;
    }
}
