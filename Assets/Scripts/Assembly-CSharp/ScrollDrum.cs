// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ScrollDrum : MonoBehaviour
{
    [Tooltip("The points that roll it (the first-person ape's index fingertips), left then right.")]
    public Transform[] fingertips;
    [Tooltip("The ribbed wheel's mesh, turned as it rolls (about this object's X).")]
    public Transform wheel;
    public float radius = 0.05f;
    public float length = 0.2f;
    [Tooltip("Steps in one turn of the wheel.")]
    public int stepsPerTurn = 10;
    [Tooltip("The range it rolls over (steps); Max is set by what it scrolls.")]
    public float min;
    [Tooltip("The range it rolls over (steps); Max is set by what it scrolls.")]
    public float max = 3f;
    public AudioClip tick;
    [Tooltip("Degrees either side of its front where a fingertip takes hold (0: all round). For a big wheel that's only its front showing, as the leaderboard's.")]
    public float arc;
    [Tooltip("The wheel's mesh turned round and round within this many degrees (0: the whole way). For a wheel that's only its front: its slats, one step apart, look as if they go round.")]
    public float wrapDegrees;
    [Tooltip("How much bigger it gets with a fingertip near it (not one that's only its front, in a bezel).")]
    public float hoverGrow = 0.07f;
    private const float MostCarry = 5f;
    private const float CarryDies = 30f;
    private const float Reach = 0.06f;
    private const float ReachAcross = 0.05f;
    private const float HoverOver = 0.06f;
    private const float KeptOver = 0.04f;
    private int finger = -1;
    private float lastAngle = float.NaN;
    private float velocity;
    private float ticked;
    private float turned;
    private float hover;
    private float hoverGoing;

    public float Value { get; private set; }

    public int Step
    {
        get { return (int)System.Math.Round((double)Value); }
    }

    public float Turned
    {
        get { return turned; }
    }

    private void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        if (finger < 0)
        {
            finger = Touching();
        }
        else if (!Near(finger, KeptOver))
        {
            finger = -1;
        }

        if (finger >= 0)
        {
            float angle = Angle(fingertips[finger].position);
            if (!float.IsNaN(lastAngle))
            {
                float delta = Mathf.DeltaAngle(lastAngle, angle);
                float steps = delta / 360f * stepsPerTurn;
                Value = Mathf.Clamp(Value + steps, min - 0.3f, max + 0.3f);
                velocity = Mathf.Clamp(velocity + (steps / dt - velocity) * 0.5f, -MostCarry, MostCarry);
            }
            lastAngle = angle;
        }
        else
        {
            velocity = Mathf.MoveTowards(velocity, 0f, dt * CarryDies);
            lastAngle = float.NaN;
            float target = Mathf.Clamp(Mathf.Round(Value + velocity * 0.15f), min, max);
            Value = Mathf.Lerp(Value + dt * velocity, target, 1f - Mathf.Exp(dt * -10f));
        }

        if (Mathf.Abs(Value - ticked) >= 1f)
        {
            ticked = Mathf.Round(Value);
            if (tick != null)
            {
                Sfx.Play(new AudioClip[] { tick }, transform.position, 0.5f, 0.95f, 1.08f);
            }
            if (finger >= 0)
            {
                InputDevice device = InputDevices.GetDeviceAtXRNode(finger == 0 ? XRNode.LeftHand : XRNode.RightHand);
                device.SendHapticImpulse(0, 0.15f, 0.02f);
            }
        }

        if (finger >= 0)
        {
            turned = Value;
        }
        else
        {
            turned = Mathf.Lerp(turned, Value, 1f - Mathf.Exp(dt * -14f));
        }

        float wheelAngle = turned / stepsPerTurn * 360f;
        if (wrapDegrees > 0f)
        {
            wheelAngle = Mathf.Repeat(wheelAngle, wrapDegrees);
        }
        if (wheel != null)
        {
            wheel.localRotation = Quaternion.AngleAxis(wheelAngle, Vector3.right);
        }

        if (arc <= 0f)
        {
            float want = 0f;
            if (finger >= 0)
            {
                want = 1f;
            }
            else
            {
                for (int i = 0; fingertips != null && i < fingertips.Length; i++)
                {
                    if (Near(i, HoverOver))
                    {
                        want = 1f;
                        break;
                    }
                }
            }
            float h = Mathf.Min(dt, 1f / 30f);
            float going = hoverGoing + h * (want - hover) * 240f + h * hoverGoing * -16f;
            hover = hover + h * going;
            hoverGoing = going;
            if (wheel != null)
            {
                wheel.localScale = Vector3.one * (hoverGrow * hover + 1f);
            }
        }
    }

    public void Set(float v)
    {
        Value = Mathf.Clamp(v, min, max);
        ticked = Mathf.Round(Value);
        velocity = 0f;
    }

    private int Touching()
    {
        if (fingertips != null)
        {
            for (int i = 0; i < fingertips.Length; i++)
            {
                if (Near(i, 0f))
                {
                    return i;
                }
            }
        }
        return -1;
    }

    private bool Near(int i, float slack)
    {
        if (fingertips != null && i < fingertips.Length)
        {
            if (!(fingertips[i] == null))
            {
                Vector3 local = transform.InverseTransformPoint(fingertips[i].position);
                Vector3 scale = transform.lossyScale;
                float y = local.y * scale.y;
                if (arc > 0f && arc + 3f < Mathf.Abs(Mathf.Atan2(y, -(local.z * scale.z)) * Mathf.Rad2Deg))
                {
                    return false;
                }
                if (length * 0.5f + ReachAcross + slack <= Mathf.Abs(local.x * scale.x))
                {
                    return false;
                }
                float z = local.z * scale.z;
                float d = Mathf.Sqrt(y * y + z * z);
                if (radius + Reach + slack <= d)
                {
                    return false;
                }
                return radius * 0.3f < d;
            }
        }
        return false;
    }

    private float Angle(Vector3 world)
    {
        Vector3 local = transform.InverseTransformPoint(world);
        return Mathf.Atan2(local.y, -local.z) * Mathf.Rad2Deg;
    }

    public static Mesh WheelMesh(float radius, float length, int ribs)
    {
        float R(int i)
        {
            return (i % 4 > 1 ? 0.86f : 1f) * radius;
        }

        int n = ribs * 4;
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int i = 0; i <= n; i++)
        {
            float a = (i * Mathf.PI + i * Mathf.PI) / n;
            float s = Mathf.Sin(a);
            float c = Mathf.Cos(a);
            float r = R(i);
            verts.Add(new Vector3(-length * 0.5f, r * s, -r * c));
            normals.Add(new Vector3(0f, s, -c));
            verts.Add(new Vector3(length * 0.5f, r * s, -r * c));
            normals.Add(new Vector3(0f, s, -c));
        }
        for (int j = 0; j < n; j++)
        {
            int k = 1 + j * 2;
            tris.AddRange(new int[] { k - 1, k + 1, k, k, k + 1, k + 2 });
        }

        float[] sides = new float[] { -1f, 1f };
        for (int si = 0; si < sides.Length; si++)
        {
            float side = sides[si];
            float x = side * length * 0.5f;
            int center = verts.Count;
            verts.Add(new Vector3(x, 0f, 0f));
            normals.Add(new Vector3(side, 0f, 0f));
            for (int i = 0; i <= n; i++)
            {
                float a = (i * Mathf.PI + i * Mathf.PI) / n;
                float r = R(i);
                verts.Add(new Vector3(x, r * Mathf.Sin(a), -(Mathf.Cos(a) * r)));
                normals.Add(new Vector3(side, 0f, 0f));
            }
            for (int i = 0; i < n; i++)
            {
                if (side <= 0f)
                {
                    tris.AddRange(new int[] { center, center + i + 2, center + i + 1 });
                }
                else
                {
                    tris.AddRange(new int[] { center, center + i + 1, center + i + 2 });
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "ScrollWheel";
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
