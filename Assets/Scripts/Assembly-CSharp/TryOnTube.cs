// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.XR;

public class TryOnTube : MonoBehaviour
{
    [Tooltip("What spins: the little ape stands on it.")]
    public Transform turntable;
    [Tooltip("The little ape (a copy of the avatar): it wears what's being tried on.")]
    public CosmeticWearer ape;
    public ApeColor apeColour;
    [Tooltip("The yellow ring at the tube's foot, turned with a fingertip (about this object's up).")]
    public Transform ring;
    public float ringRadius = 0.32f;
    public Transform[] fingertips;
    [Tooltip("How fast it turns by itself (degrees a second).")]
    public float idleSpin = 18f;
    [Tooltip("A glow that rises up the tube when something's bought (Flash).")]
    public Renderer glow;
    [Tooltip("The little ape floats (user 11-04: \"make him like float, like have a little float animation\"): how high over the turntable (m), how far up and down it bobs (m), one bob every this many seconds, how far it rocks (degrees).")]
    public float hover = 0.06f;
    [Tooltip("The little ape floats (user 11-04: \"make him like float, like have a little float animation\"): how high over the turntable (m), how far up and down it bobs (m), one bob every this many seconds, how far it rocks (degrees).")]
    public float bob = 0.025f;
    [Tooltip("The little ape floats (user 11-04: \"make him like float, like have a little float animation\"): how high over the turntable (m), how far up and down it bobs (m), one bob every this many seconds, how far it rocks (degrees).")]
    public float bobTime = 2.6f;
    [Tooltip("The little ape floats (user 11-04: \"make him like float, like have a little float animation\"): how high over the turntable (m), how far up and down it bobs (m), one bob every this many seconds, how far it rocks (degrees).")]
    public float rock = 3f;
    private int finger = -1;
    private float lastAngle = float.NaN;
    private float spin;
    private float flash = -1f;
    private Vector3 apeRest;
    private Quaternion apeRestTurn;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;

    private void Start()
    {
        InYourColour();
        if (ape != null)
        {
            apeRest = ape.transform.localPosition;
            apeRestTurn = ape.transform.localRotation;
        }
    }

    private void InYourColour()
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (apeColour != null && boot != null && boot.localColor != null)
        {
            if (apeColour.furColor != boot.localColor.furColor)
            {
                apeColour.SetColor(boot.localColor.furColor);
            }
        }
    }

    private void Update()
    {
        InYourColour();
        float dt = Time.deltaTime;
        bool touching = false;
        if (finger < 0)
        {
            finger = Touching(0f);
            touching = finger >= 0;
        }
        else
        {
            int t = Touching(0.02f);
            if (t == finger) touching = t >= 0;
            else finger = -1;
        }

        if (touching)
        {
            float angle = Angle(fingertips[finger].position);
            if (!float.IsNaN(lastAngle))
            {
                float delta = Mathf.DeltaAngle(lastAngle, angle);
                spin += (delta / dt - spin) * 0.5f;
                Turn(delta);
                if (Mathf.Abs(delta) > 0.5f)
                {
                    InputDevice device = InputDevices.GetDeviceAtXRNode(finger != 0 ? XRNode.RightHand : XRNode.LeftHand);
                    device.SendHapticImpulse(0, 0.06f, 0.02f);
                }
            }
            lastAngle = angle;
        }
        else
        {
            lastAngle = float.NaN;
            spin = Mathf.Lerp(spin, idleSpin, 1f - Mathf.Exp(dt * -1.5f));
            Turn(dt * spin);
        }

        if (flash >= 0f)
        {
            Glow(dt);
        }
        Float();
    }

    private void Float()
    {
        if (ape == null) return;
        float phase = Time.time * (6.2831855f / bobTime);
        ape.transform.localPosition = apeRest + Vector3.up * (hover + Mathf.Sin(phase) * bob);
        Transform t = ape.transform;
        Quaternion wobble = Quaternion.Euler(Mathf.Sin(phase - 1.2f) * rock, 0f, Mathf.Sin(phase * 0.5f) * rock * 0.6f);
        t.localRotation = apeRestTurn * wobble;
    }

    private void Turn(float degrees)
    {
        if (turntable != null)
        {
            turntable.Rotate(0f, degrees, 0f, Space.Self);
        }
        if (ring == null) return;
        ring.localRotation = Quaternion.Euler(0f, degrees, 0f) * ring.localRotation;
    }

    public void TryOn(byte[] bySlot)
    {
        if (ape != null)
        {
            ape.Show(bySlot);
        }
    }

    public void Flash()
    {
        flash = 0f;
    }

    private void Glow(float dt)
    {
        flash = dt / 0.9f + flash;
        if (glow != null)
        {
            float t = Mathf.Clamp01(flash);
            glow.enabled = t < 1f;
            glow.transform.localPosition = new Vector3(0f, Mathf.Clamp01(t) * 1.4000001f + 0.55f, 0f);
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
            float a = 1f - t;
            float v = a * 1.6f;
            block.SetColor(BaseColor, new Color(v, v, v, a));
            glow.SetPropertyBlock(block);
        }
        if (flash >= 1f)
        {
            flash = -1f;
        }
    }

    private int Touching(float slack)
    {
        if (fingertips == null || ring == null) return -1;
        for (int i = 0; i < fingertips.Length; i++)
        {
            if (fingertips[i] == null) continue;
            Transform parent = ring.parent;
            Vector3 p = parent.InverseTransformPoint(fingertips[i].position);
            Vector3 c = ring.localPosition;
            float dx = p.x - c.x;
            float dz = p.z - c.z;
            float dy = Mathf.Abs(p.y - c.y);
            if (Mathf.Abs(Mathf.Sqrt(dx * dx + dz * dz) - ringRadius) < slack + 0.04f && dy < slack + 0.05f)
            {
                return i;
            }
        }
        return -1;
    }

    private float Angle(Vector3 world)
    {
        Vector3 p = ring.parent.InverseTransformPoint(world);
        Vector3 c = ring.localPosition;
        return Mathf.Atan2(p.z - c.z, p.x - c.x) * -57.29578f;
    }
}
