// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[DefaultExecutionOrder(85)]
public class GillaHeldApe : MonoBehaviour
{
    private const int Free = 0;
    private const int Held = 1;
    private const int Dropped = 2;
    private const int Down = 3;
    private const float SeamRate = 25f;
    private const float LongestSeam = 4f;
    private const float EyesOverFloor = 0.5f;
    private const float HeadRadius = 0.14f;
    private const float MostGoing = 12f;
    private const float LongestFall = 4f;
    private const float DrawnBehindWord = 0.25f;
    private const int Solid = -5893;
    private NetworkApe who;
    private Transform yourHand;
    private float yourHandTill;
    private Vector3 yourEyes = Vector3.up * 0.36f;
    private int state;
    private bool started;
    private bool falling;
    private bool landed;
    private Vector3 seam;
    private Vector3 drawn;
    private Vector3 fallAt;
    private Vector3 fallGoing;
    private Vector3 lastNet;
    internal Vector3 going;
    private float fellFor;
    private float landedAfter;
    private float toldFor;
    private float tiltFollow = -1f;
    private float yawTook;
    private bool tookAgain;

    public bool InHand { get { return state == Held; } }
    public bool Falling { get { return falling; } }
    public Vector3 Going { get { return going; } }

    private void Awake()
    {

        who = GetComponent<NetworkApe>();
    }

    public void Take(Transform hand, Vector3 eyesFromHand)
    {
        yourHand = hand;
        yourEyes = eyesFromHand;
        tookAgain = true;
        falling = false;
        yourHandTill = Time.time + float.PositiveInfinity;
    }

    public void Take(Transform hand, float seconds = float.PositiveInfinity)
    {
        yourHand = hand;
        falling = false;
        yourHandTill = Time.time + seconds;
    }

    public void NotFalling()
    {
        falling = false;
    }

    public void LetGo(Vector3 eyes, Vector3 velocity)
    {
        yourHand = null;
        fallAt = eyes;
        falling = true;
        landed = false;
        fallGoing = velocity;
        toldFor = 0f;
        fellFor = 0f;
        landedAfter = 0f;
    }

    private void LateUpdate()
    {
        if (who == null) return;
        if (who.head == null) return;
        Vector3 pos = who.head.position;
        if (!started)
        {
            lastNet = pos;
            drawn = pos;
            started = true;
        }
        float dt = Time.deltaTime;
        if (0f < dt)
        {
            float t = Mathf.Clamp01(1f - Mathf.Exp(dt * -12f));
            going = going + ((pos - lastNet) / dt - going) * t;
            going = Vector3.ClampMagnitude(going, MostGoing);
        }
        lastNet = pos;

        Transform hand;
        bool fromHolder;
        if (yourHand != null && Time.time < yourHandTill)
        {
            hand = yourHand;
            fromHolder = false;
        }
        else
        {
            hand = who.HolderHand;
            fromHolder = true;
        }

        GillaLimp limp = who.GetComponent<GillaLimp>() /* ? */;
        bool down = limp != null ? limp.IsDown : false;
        bool held = hand != null;
        int next = held ? Held : Down;
        bool fall;
        if (!held && !down)
        {
            fall = falling;
            next = falling ? Dropped : Free;
        }
        else
        {
            fall = false;
        }

        ApeAvatarDriver driver = who.AvatarDriver;
        if (driver != null)
        {
            if (tiltFollow < 0f)
            {
                tiltFollow = driver.bodyTiltFollow;
            }
            if (held)
            {
                driver.bodyTiltFollow = 0f;
                driver.turnHeld = true;
                if (state != Held || tookAgain)
                {
                    yawTook = driver.transform.eulerAngles.y /* ? */;
                }
            }
            else
            {
                bool busy = who.Busy;
                driver.bodyTiltFollow = !busy ? tiltFollow : 0f;
                driver.turnHeld = busy;
            }
        }
        tookAgain = false;

        Vector3 target = fromHolder ? who.EyesFromHolder : yourEyes;
        if (held)
        {
            if (driver != null)
            {
                float d = Mathf.DeltaAngle(yawTook, driver.transform.eulerAngles.y /* ? */);
                target = Quaternion.Euler(0f, d, 0f) * target;
            }
            target = hand.position + target;
        }
        else if (!down)
        {
            target = pos;
            if (fall)
            {
                target = Fall(dt);
            }
        }
        else
        {
            target = limp.Eyes;
        }

        Vector3 s;
        if (next == state)
        {
            s = seam;
        }
        else
        {
            s = drawn - target;
            seam = s;
            if (LongestSeam * LongestSeam < s.sqrMagnitude)
            {
                s = Vector3.zero;
                seam = s;
            }
            state = next;
        }
        float k = Mathf.Exp(dt * -SeamRate);
        seam = k * s;
        Vector3 value = target + seam;
        drawn = value;

        if ((!held && !down) || held)
        {
            Vector3 delta = value - pos;
            if (delta.sqrMagnitude < 1e-08f) return;
            who.head.position = who.head.position + delta;
            if (who.leftHand != null)
            {
                who.leftHand.position = who.leftHand.position + delta;
            }
            if (who.rightHand == null) return;
            who.rightHand.position = who.rightHand.position + delta;
        }
        else
        {
            who.head.position = value;
            if (who.leftHand != null)
            {
                who.leftHand.position = limp.Hand(false);
            }
            if (who.rightHand == null) return;
            who.rightHand.position = limp.Hand(true);
        }
    }

    private bool Slammed(Vector3 at, Vector3 normal)
    {
        KillCam cam = KillCam.Local;
        if (cam == null) return false;
        if (who.Object == null) return false;
        Fusion.NetworkObject obj = who.Object;
        if (!obj.Runner) return false;
        if (!obj.HasStateAuthority /* ? */) return false;
        if (Vector3.Dot(-normal, fallGoing) < cam.slamSpeed) return false;
        who.RPC_Slam(at, normal, Vector3.up * 0.8f - fallGoing * 0.2f);
        falling = false;
        return true;
    }

    private Vector3 Fall(float dt)
    {
        fellFor = fellFor + dt;
        RaycastHit ground = default(RaycastHit);
        RaycastHit hit = default(RaycastHit);
        if (!landed)
        {
            landedAfter = fellFor;
            fallGoing = fallGoing + Physics.gravity * dt;
            Vector3 step = fallGoing * dt;
            float dist = step.magnitude;
            if (dist > 1e-05f && Physics.SphereCast(fallAt, HeadRadius, step / dist, out hit, dist, Solid, QueryTriggerInteraction.Ignore))
            {
                fallAt = fallAt + (step / dist) * hit.distance;
                if (Slammed(hit.point, hit.normal)) return fallAt;
                fallGoing = Vector3.ProjectOnPlane(fallGoing, hit.normal);
            }
            else
            {
                fallAt = fallAt + step;
            }
            if (fallGoing.y <= 0f)
            {
                if (Physics.Raycast(fallAt, Vector3.down, out ground, EyesOverFloor, Solid, QueryTriggerInteraction.Ignore))
                {
                    if (Slammed(ground.point, ground.normal)) return fallAt;
                    landed = true;
                    fallAt.y = ground.point.y + EyesOverFloor;
                }
            }
        }
        if (!who.Busy)
        {
            toldFor = toldFor + dt;
        }
        if ((landed && landedAfter + DrawnBehindWord <= toldFor) || LongestFall < fellFor)
        {
            falling = false;
        }
        return fallAt;
    }
}
