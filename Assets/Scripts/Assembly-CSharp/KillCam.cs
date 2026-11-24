// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;

[DefaultExecutionOrder(90)]
public partial class KillCam : MonoBehaviour
{
    public static KillCam Local;
    public Transform head;
    public Player locomotion;
    public Rigidbody body;
    public DismemberGroup dismember;
    [Tooltip("How quickly the rig follows the monster's hands (higher = tighter).")]
    public float follow = 18f;
    [Tooltip("After the bite: seconds before the fade to black starts.")]
    public float holdAfterBite = 0.9f;
    public float fadeSeconds = 0.45f;
    [Tooltip("Black for this long before you respawn.")]
    public float blackSeconds = 0.6f;
    [Tooltip("If the bite never comes (an interrupted clip), give up after this long.")]
    public float maxSeconds = 5f;
    [Tooltip("Turns you to face the monster for this long as it grabs you, then leaves your body's heading alone.")]
    public float faceTurnSeconds = 0.4f;
    [Tooltip("A monster player holds you by the middle of your body: your eyes this far over the grip (m).")]
    public float heldEyesOver = 0.36f;
    private Vector3 heldEyes;
    [Tooltip("Thrown at least this fast (m/s), you fly: no hands till you land, or you'd catch yourself on what you're thrown at.")]
    public float thrownSpeed = 4f;
    [Tooltip("Going into anything this fast (m/s, the part of your speed that's straight at it), thrown, you die.")]
    public float slamSpeed = 7f;
    [Tooltip("Thrown, you've landed by this long after at the latest (s).")]
    public float thrownSeconds = 3f;
    [Tooltip("Thrown and coming down, your hands are back this far (m) over the floor, to land on.")]
    public float handsBackOver = 0.2f;
    [Tooltip("A blast's throw: your hands catch on nothing for this long (s), so what they were on can't stop you dead; then they're yours again in the air.")]
    public float launchGrace = 0.15f;
    [Tooltip("Picked up by a monster player, how long it takes to bring you right into their hand (s).")]
    public float heldEase = 0.2f;
    [Tooltip("How many punches from a monster player kill you (the user, 2026-11-02: \"punched a few times like in one in the head, torso, and something else\").")]
    public int punchesToKill = 3;
    [Tooltip("Knocked flat by a monster player's punch, you're up again this long after the last one (s), if they haven't finished you or picked you up.")]
    public float downSeconds = 3.5f;
    [Tooltip("Pushed over by a PUNCH menu's punch, you're up again this long after it (s): sooner than after a monster player's, as it's only a shove (the user, 2026-11-11: \"if someone gets punched they stand back up faster\"). Getting up takes downRise on top.")]
    public float shoveDownSeconds = 1.5f;
    [Tooltip("Knocked flat, how far your view goes over the way you fall (degrees): looking down at the floor if you went on your face, at the ceiling if you went on your back.")]
    public float downTip = 75f;
    [Tooltip("How long you take to go over (s).")]
    public float downFall = 0.55f;
    [Tooltip("How long you take to get up again (s).")]
    public float downRise = 0.45f;
    public const float ThrowLimit = 16f;
    public const float NotHeldAfterBack = 3f;
    public Transform respawnAt;
    private Transform onceAt;
    private Transform heldFace;
    private bool dying;
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private float t;
    private float biteAt = -1f;
    private float fadeBack;
    private float backAt = -100f;
    private float blinkTill;
    private float twistLeft;
    private float twistRate;
    private Renderer fade;
    private Material fadeMat;
    private RigidbodyInterpolation interp;
    private bool restoreInterp;
    private float flying;
    private Vector3 flew;
    private float downLeft;
    private float tipped;
    private float eyesWere;
    private Vector3 downGoing;
    private Vector3 tipAxis;
    private const float EyesLying = 0.2f;
    private const float DownFriction = 6f;
    private const int Solid = -5893;
    private const float GoreWait = 2.5f;
    private readonly List<Renderer> hiddenBody = new List<Renderer>();

    public static event Action Died;
    public static event Action Dying;

    public int Hits { get; private set; }
    public Vector3 DeathVelocity { get; private set; }
    public bool BlownApart { get; private set; }
    public bool Busy { get; private set; }
    public bool Knocked { get; private set; }
    public MonsterBrain Killer { get; private set; }
    public bool FellInPit { get; private set; }
    public Transform HeldBy { get; private set; }

    private static int Unblocked_b__120_0(RaycastHit x, RaycastHit y)
    {
        return x.distance.CompareTo(y.distance);
    }

    private void Awake()
    {

        Local = this;
        spawnPos = transform.position;
        spawnRot = transform.rotation;
        BuildFade();
    }

    private void OnDestroy()
    {
        if (Local == this)
        {
            Local = null;
        }
        if (fadeMat != null)
        {
            Destroy(fadeMat);
        }
    }

    public bool Begin(MonsterBrain killer, Transform backTo)
    {
        if (!Busy && head != null)
        {
            Freeze();
            Killer = killer;
            onceAt = backTo;
            Buzz(0.8f, 0.25f);
            return true;
        }
        return false;
    }

    public bool BeginHeld(Transform hand, Transform face, Nullable<Vector3> eyesFromHand)
    {
        if ((!Busy || Knocked) && !dying && head != null && hand != null && Time.time - backAt >= NotHeldAfterBack)
        {
            if (!Knocked)
            {
                Freeze();
            }
            else
            {
                Knocked = false;
                t = 0f;
            }
            HeldBy = hand;
            heldFace = face;
            heldEyes = eyesFromHand.HasValue ? eyesFromHand.Value : Vector3.up * heldEyesOver;
            Buzz(0.8f, 0.25f);
            return true;
        }
        return false;
    }

    public void EndHeld(Vector3 velocity)
    {
        Vector3 eyes = (head != null ? head : transform).position;
        EndHeld(eyes, velocity);
    }

    public void EndHeld(Vector3 eyesAt, Vector3 velocity)
    {
        if (!Busy)
        {
            return;
        }
        if (HeldBy == null && heldFace == null)
        {
            return;
        }
        if (dying)
        {
            return;
        }
        Tip(0f);
        Transform from = heldFace != null ? heldFace : head;
        Vector3 eyes = Unblocked(from.position, eyesAt);
        Release(eyes + transform.position - head.position, transform.rotation, velocity);
        if (velocity.magnitude < thrownSpeed)
        {
            return;
        }
        flew = velocity;
        flying = thrownSeconds;
        if (locomotion != null)
        {
            locomotion.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        if (flying > 0f)
        {
            float dt = Time.fixedDeltaTime;
            flying -= dt;
            Vector3 v = body != null ? body.linearVelocity : Vector3.zero;
            float speed2 = v.sqrMagnitude;
            float speed = Mathf.Sqrt(speed2);
            float impact = (flew + Physics.gravity * dt - v).magnitude;
            Vector3 going = speed2 > flew.sqrMagnitude * 0.25f ? v : flew;
            if (speed > 0.1f && head != null)
            {
                if (Physics.SphereCast(head.position, 0.14f, v / speed, out hit, dt * speed + dt * speed + 0.05f, Solid, QueryTriggerInteraction.Ignore))
                {
                    float into = -Vector3.Dot(hit.normal, v);
                    if (impact <= into)
                    {
                        impact = into;
                    }
                }
            }
            if (slamSpeed <= impact)
            {
                flying = 0f;
                DeathVelocity = Vector3.up * 0.8f - going * 0.2f;
                Die(false);
            }
            else
            {
                flew = v;
                if ((v.y < 0f && HandNearFloor(handsBackOver - (dt * v.y + dt * v.y))) || flying <= 0f || (speed <= 1.5f && flying <= thrownSeconds - 0.25f))
                {
                    flying = 0f;
                    if (locomotion != null && !MonsterPlayer.IsMonster)
                    {
                        locomotion.InitializeValues();
                        locomotion.enabled = true;
                    }
                }
            }
        }
    }

    private bool HandNearFloor(float reach)
    {
        if (locomotion == null)
        {
            return false;
        }
        bool[] hands = new bool[] { false, true };
        foreach (bool left in hands)
        {
            Vector3 p = locomotion.HandNow(left);
            if (Physics.Raycast(p + Vector3.up * 0.02f, Vector3.down, reach + 0.02f, Solid, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }
        return false;
    }

    public void Slammed(Vector3 going)
    {
        if (dying)
        {
            return;
        }
        DeathVelocity = going;
        Die(false);
    }

    public bool KnockDown(Vector3 going, float downFor = -1f)
    {
        DeathVelocity = going;
        if (head != null && !dying && (!Busy || Knocked))
        {
            if (!MonsterPlayer.IsMonster)
            {
                if (!GillaMapVote.OutOfHunt && Time.time - backAt >= NotHeldAfterBack)
                {
                    if (!Knocked)
                    {
                        eyesWere = EyesOverFloor(head.position);
                        Vector3 flat = Vector3.ProjectOnPlane(going, Vector3.up);
                        if (flat.sqrMagnitude < 0.01f)
                        {
                            flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                        }
                        tipped = 0f;
                        downGoing = going;
                        tipAxis = Vector3.Cross(Vector3.up, flat.normalized); /* ? */
                        Freeze();
                        Knocked = true;
                    }
                    else
                    {
                        downGoing += going * 0.5f;
                    }
                    if (downFor < 0f)
                    {
                        downFor = downSeconds;
                    }
                    downLeft = downFor;
                    return true;
                }
            }
        }
        return false;
    }

    private float EyesOverFloor(Vector3 eyes)
    {
        RaycastHit hit;
        if (Physics.Raycast(eyes, Vector3.down, out hit, 2.5f, Solid, QueryTriggerInteraction.Ignore))
        {
            return Mathf.Clamp(hit.distance, 0.2f, 1.6f);
        }
        return 0.6f;
    }

    private void Lying(float dt)
    {
        RaycastHit floor;
        RaycastHit hit;
        SetFade(0f);
        downLeft -= dt;
        float left = downLeft;
        Vector3 eyes = head.position;
        Vector3 flat = Vector3.MoveTowards(new Vector3(downGoing.x, 0f, downGoing.z), Vector3.zero, dt * DownFriction);
        downGoing = new Vector3(flat.x, downGoing.y, flat.z) + Physics.gravity * dt;
        Vector3 step = downGoing * dt;
        float len = step.magnitude;
        if (len > 1e-05f)
        {
            if (Physics.SphereCast(eyes, 0.14f, step / len, out hit, len, Solid, QueryTriggerInteraction.Ignore))
            {
                downGoing = Vector3.ProjectOnPlane(downGoing, hit.normal);
                step = step / len * hit.distance;
            }
        }
        float eyesOver = eyesWere;
        if (left > 0f)
        {
            eyesOver = eyesOver + (EyesLying - eyesOver) * Mathf.Clamp01(tipped / Mathf.Max(downTip, 1f));
        }
        Vector3 at = eyes + step;
        if (Physics.Raycast(at + Vector3.up * 0.1f, Vector3.down, out floor, eyesOver + 0.1f, Solid, QueryTriggerInteraction.Ignore))
        {
            float y = eyesOver + floor.point.y;
            if (left > 0f)
            {
                at.y = Mathf.Max(at.y, y);
            }
            else
            {
                at.y = Mathf.MoveTowards(at.y, y, Mathf.Max(eyesWere - EyesLying, 0.1f) / downRise * dt);
            }
            downGoing.y = 0f;
        }
        float target;
        float rate;
        if (left > 0f)
        {
            target = downTip;
            rate = downTip * 3f * Mathf.Max(tipped / downTip, 0.15f) / Mathf.Max(downFall, 0.01f);
        }
        else
        {
            target = 0f;
            rate = downTip / downRise;
        }
        Tip(Mathf.MoveTowards(tipped, target, rate * dt), at);
        if (left > 0f || tipped > 0f)
        {
            return;
        }
        Knocked = false;
        Release(transform.position, transform.rotation, Vector3.zero);
    }

    private void Tip(float to)
    {
        Transform from = head != null ? head : transform;
        Tip(to, from.position);
    }

    private void Tip(float to, Vector3 eyes)
    {
        if (tipped != to)
        {
            transform.RotateAround(head.position, tipAxis, to - tipped);
            tipped = to;
        }
        if (tipped <= 0f)
        {
            if ((transform.up - Vector3.up).sqrMagnitude > 1e-06f)
            {
                Transform tr = transform;
                Vector3 pivot = head.position;
                Vector3 axis = Vector3.Cross(transform.up, Vector3.up).normalized;
                float angle = Vector3.Angle(transform.up, Vector3.up);
                tr.RotateAround(pivot, axis, angle);
            }
        }
        transform.position = transform.position + (eyes - head.position);
    }

    public void Punched()
    {
        if (!dying)
        {
            Hits = Hits + 1;
            Buzz(1f, 0.25f);
            if (punchesToKill <= Hits)
            {
                Die(false);
            }
        }
    }

    public void Blasted(Vector3 velocity, int hits, bool blownApart)
    {
        if (dying)
        {
            return;
        }
        Buzz(1f, 0.45f);
        Hits = Hits + hits;
        if (blownApart || punchesToKill <= Hits)
        {
            DeathVelocity = velocity;
            BlownApart = blownApart;
            Die(false);
            return;
        }
        if (Busy)
        {
            return;
        }
        if (body == null)
        {
            return;
        }
        if (body.isKinematic)
        {
            return;
        }
        flying = 0f;
        if (locomotion != null && locomotion.enabled)
        {
            locomotion.Launch(velocity, launchGrace);
            return;
        }
        body.linearVelocity = velocity;
    }

    public void Shoved(Vector3 velocity)
    {
        if (!Busy && body != null)
        {
            if (body.isKinematic)
            {
                return;
            }
            body.linearVelocity = velocity + body.linearVelocity;
        }
    }

    public void Die(bool tearHead = true)
    {
        if (!Busy)
        {
            Freeze();
        }
        if (!dying)
        {
            Knocked = false;
            dying = true;
            biteAt = t;
            if (tearHead && dismember != null)
            {
                dismember.Tear(ApePart.Head);
            }
            Buzz(1f, 0.5f);
            Dying?.Invoke();
            if (NetworkApe.Local != null)
            {
                NetworkApe.Local.LeaveBody();
            }
        }
    }

    public void DieInPit()
    {
        FellInPit = true;
        Die(false);
    }

    private void Freeze()
    {
        flying = 0f;
        Busy = true;
        Killer = null;
        heldFace = null;
        HeldBy = null;
        onceAt = null;
        blinkTill = 0f;
        twistLeft = 0f;
        dying = false;
        fadeBack = 0f;
        t = 0f;
        biteAt = -1f;
        if (locomotion != null)
        {
            locomotion.enabled = false;
        }
        if (body == null)
        {
            return;
        }
        interp = body.interpolation;
        body.interpolation = RigidbodyInterpolation.None;
        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
    }

    public void Blink(float seconds)
    {
        blinkTill = Time.time + seconds;
    }

    public void Twist(float degrees, float seconds)
    {
        twistLeft = degrees;
        if (seconds <= 0.01f)
        {
            seconds = 0.01f;
        }
        twistRate = degrees / seconds;
    }

    public void TearArm(ApePart part)
    {
        if (Busy && !dying && biteAt < 0f && dismember != null)
        {
            dismember.Tear(part);
            Buzz(1f, 0.4f);
        }
    }

    public void Bite()
    {
        if (Busy && biteAt < 0f)
        {
            biteAt = t;
            if (dismember != null)
            {
                dismember.Tear(ApePart.Head);
            }
            Buzz(1f, 0.5f);
            Dying?.Invoke();
        }
    }

    public bool Fell()
    {
        bool busy = Busy;
        if (!busy)
        {
            bool restore = restoreInterp;
            Release(spawnPos, spawnRot, Vector3.zero);
            restoreInterp = restore;
            Home(Back());
            fadeBack = 1f;
            SetFade(1f);
        }
        return !busy;
    }

    private void LateUpdate()
    {
        if (Local == this)
        {
            GillaWounds.Pump();
        }
        if (restoreInterp && body != null)
        {
            body.interpolation = interp;
            restoreInterp = false;
        }
        if (fadeBack > 0f && !Busy)
        {
            fadeBack = Mathf.Max(fadeBack - Time.deltaTime / fadeSeconds, 0f);
            SetFade(fadeBack);
        }
        if (!Busy)
        {
            return;
        }
        float dt = Time.deltaTime;
        t = dt + t;
        if (Knocked)
        {
            Lying(dt);
            return;
        }
        if (tipped > 0f && !dying)
        {
            Tip(Mathf.MoveTowards(tipped, 0f, dt * (downTip / Mathf.Max(downRise, 0.01f))));
        }
        if (!dying && Killer == null && HeldBy == null && heldFace != null)
        {
            Release(transform.position, transform.rotation, Vector3.zero); /* ? */
            return;
        }
        Transform grip = Killer != null ? Killer.grabSocket : HeldBy;
        if (grip != null && !(dying && Killer == null))
        {
            float k = Mathf.Exp(-(follow * dt));
            Vector3 face;
            if (Killer != null)
            {
                if (Killer.face != null)
                {
                    face = Killer.face.position;
                }
                else
                {
                    face = Killer.transform.position + Vector3.up;
                }
            }
            else if (heldFace != null)
            {
                face = heldFace.position;
            }
            else
            {
                face = grip.position + Vector3.up * 0.3f;
            }
            Vector3 eyes = head.position;
            Vector3 fwd = head.forward;
            k = 1f - k;
            if (Killer != null && Killer.killLooksUp)
            {
                LookUpAt(face, k);
            }
            else if (t < faceTurnSeconds)
            {
                float dx = face.x - eyes.x;
                float dz = face.z - eyes.z;
                if (dx * dx + dz * dz > 0.04f && fwd.x * fwd.x + fwd.z * fwd.z > 0.0001f)
                {
                    Transform tr = transform;
                    float angle = Vector3.SignedAngle(new Vector3(fwd.x, 0f, fwd.z), new Vector3(dx, 0f, dz), Vector3.up);
                    tr.RotateAround(head.position, Vector3.up, k * angle);
                }
            }
            if (Killer != null)
            {
                Transform tr = transform;
                Vector3 pos = tr.position;
                Vector3 target = Unblocked(head.position, grip.position);
                Vector3 now = head.position;
                float w = Time.time < blinkTill ? 1f : k;
                tr.position = pos + (target - now) * w;
            }
            if (twistLeft <= 0f)
            {
                Transform tr = transform;
                Vector3 pos = tr.position;
                Vector3 gripPos = grip.position;
                Vector3 now = head.position;
                float e = Mathf.Max(k, Mathf.Clamp01(t / heldEase));
                tr.position = pos + (gripPos + heldEyes - now) * e;
            }
            else
            {
                float step = Mathf.Min(twistLeft, dt * twistRate);
                transform.RotateAround(head.position, head.forward, step);
                twistLeft -= step;
            }
        }
        float bite = biteAt;
        if (bite < 0f)
        {
            if (Killer == null || t <= maxSeconds)
            {
                bite = biteAt;
            }
            else
            {
                bite = t;
                biteAt = bite;
            }
        }
        if (bite < 0f)
        {
            float a = Time.time < blinkTill ? 1f : 0f;
            if (fade != null)
            {
                fade.enabled = a > 0.001f;
                fadeMat.SetColor("_BaseColor", new Color(0f, 0f, 0f, a));
            }
            return;
        }
        float hold = 0f;
        float fadeFor = 0f;
        if (Killer != null)
        {
            hold = Killer.killHoldAfterBite;
            if (hold < 0f)
            {
                hold = holdAfterBite;
            }
            if (hold > 0f)
            {
                fadeFor = fadeSeconds;
            }
        }
        float since = t - bite;
        float alpha = 0f;
        if (hold <= since)
        {
            alpha = 1f;
            if (fadeFor > 0f)
            {
                alpha = Mathf.Clamp01((since - hold) / fadeFor);
            }
        }
        SetFade(alpha);
        float end = hold + fadeFor + blackSeconds;
        if (end <= since)
        {
            if (!GillaWounds.Pending || end + GoreWait <= since)
            {
                backAt = Time.time;
                Heal();
                Release(spawnPos, spawnRot, Vector3.zero);
                Transform back = onceAt != null ? onceAt : Back();
                onceAt = null;
                Home(back);
                SetFade(0f);
                Died?.Invoke();
                FellInPit = false;
            }
        }
    }

    private void LookUpAt(Vector3 face, float k)
    {
        Vector3 eyes = head.position;
        if (biteAt < 0f)
        {
            Vector3 to = face - eyes;
            if (to.sqrMagnitude < 0.16f)
            {
                return;
            }
            float fx = to.x;
            float fz = to.z;
            if (fx * fx + fz * fz < 0.0001f)
            {
                fx = head.forward.x;
                fz = head.forward.z;
            }
            Vector3 flat = new Vector3(fx, 0f, fz);
            float flatLen = flat.magnitude;
            float pitch = Mathf.Atan2(to.y, flatLen) * Mathf.Rad2Deg;
            pitch = Mathf.Max(pitch, Mathf.Clamp01(t / 0.6f) * 45f);
            Vector3 dir = flat.normalized;
            Quaternion q = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, dir));
            Vector3 want = q * dir;
            Quaternion turn = Quaternion.FromToRotation(head.forward, want);
            float angle;
            Vector3 axis;
            turn.ToAngleAxis(out angle, out axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }
            if (Mathf.Abs(angle) > 0.01f)
            {
                transform.RotateAround(head.position, axis, angle * k * 0.6f);
            }
        }
    }
}
