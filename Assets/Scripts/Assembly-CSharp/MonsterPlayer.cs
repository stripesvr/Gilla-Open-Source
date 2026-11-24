// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using Fusion;
using GorillaLocomotion;

[DefaultExecutionOrder(-50)]
public partial class MonsterPlayer : MonoBehaviour
{
    private struct SquashingScope
    {
        public bool inHand;
        public Matrix4x4 back;
        public float dt;
        public Rigidbody rb;
        public Collider box;
        public MonsterPlayer owner;
    }

    [Header("BE THE MONSTER's doors")]
    [Tooltip("How fast the part of a door you've got that meets an ape must be going to squash them (m/s).")]
    public float squashSpeed = 2.5f;
    [Tooltip("How near a door you've got an ape's head or middle has to come for it to meet them (m).")]
    public float squashReach = 0.25f;
    [Tooltip("The fastest you can throw a door (m/s).")]
    public float maxDoorThrow = 8f;
    private GillaDoor slabDoor;
    private bool slabRight;
    private Matrix4x4 slabWas;
    private bool slabWasSet;
    private GillaDoor slabThrown;
    private float slabThrownTill;
    private readonly Dictionary<int, float> squashed = new Dictionary<int, float>();
    public static bool OwnerPunches;
    public static bool MenuPunches;
    public const float MenuShoveEvery = 2;
    [Header("The rig (set by GillaSetup / Gilla > Monsters > Add Map Monsters To Open Scene)")]
    public Player locomotion;
    public Rigidbody body;
    public Transform head;
    public Transform leftTarget;
    public Transform rightTarget;
    public Transform leftFollower;
    public Transform rightFollower;
    [Tooltip("The ape avatars you wear (first-person and the mirror's): hidden while you're a monster.")]
    public GameObject[] apeBodies;
    [Header("Monster play")]
    [Tooltip("The ape's eye height over the floor (m): a crawling monster's size is its eye height over this.")]
    public float apeEyeHeight = 0.5f;
    [Tooltip("Your own arm's reach, shoulder to controller (m): a monster held up by its arms (MonsterCatalog's hangs) is sized by its reach over this, so its hands are where yours are.")]
    public float apeReach = 0.6f;
    [Tooltip("How close a hand must be to a player's head to pick them up, or to a part to grab it (m, at Skull3's size).")]
    public float grabRadius = 0.45f;
    [Tooltip("How close the hand you see must be to the skin you see of someone, to pick them up (m, at your size): on them (GillaSkin). A new name: grabTouch, 0.12 m off the body's shapes (GillaWounds' capsules), tested with a point a hand's length out from the palm, went 2026-11-09.")]
    public float skinTouch = 0.07f;
    [Tooltip("A grip that's just closed still takes hold of whoever the hand comes onto, this long after (s): it needn't close the very frame it gets there.")]
    public float graspFor = 0.25f;
    [Tooltip("How much farther apart your hands must pull to tear a part off, or rip someone in two (m of your own arm: it's scaled with the rig). (A new name: Main and the sandboxes keep the old one's saved value.)")]
    public float pullApart = 0.2f;
    [Tooltip("How many punches before you can rip someone in half: your other hand's grip on their middle, and a pull of your hands apart (a body you can rip whatever's happened to it).")]
    public int punchesBeforeRip;
    [Tooltip("The fastest you can throw a player you let go of (m/s): thrown hard enough into a wall, they die (KillCam).")]
    public float hardestThrow = 14f;
    [Tooltip("How fast the hand that has someone must be going into a wall, a floor or anything solid to kill them with it (m/s of your own arm: it's scaled with the rig).")]
    public float slamSwing = 2.5f;
    [Tooltip("How far out from your hand the body you've got reaches, for what it hits (m, at an ape's own size).")]
    public float heldReach = 0.2f;
    [Tooltip("How fast your free hand must have been going lately (punchSwingLasts), against the one that's holding them, to punch a player you hold (m/s of your own arm: it's scaled with the rig).")]
    public float punchSwing = 1.5f;
    [Tooltip("How long a swing still counts after its fastest (s): a hand slows as it gets there, with nothing there to stop it.")]
    public float punchSwingLasts = 0.2f;
    [Tooltip("How far off their body your fist can be and still land (m of your own arm: it's scaled with the rig, their body isn't).")]
    public float punchReach = 0.12f;
    [Tooltip("How long after a punch the next can land (s), so one swing's one punch.")]
    public float punchEvery = 0.35f;
    [Tooltip("How fast your hand must have been going lately (punchSwingLasts), against the rest of you, to punch someone you aren't holding (m/s of your own arm: it's scaled with the rig). More than a punch at someone you hold: a hand reaching out to grab someone's going about that fast.")]
    public float freePunchSwing = 2f;
    [Tooltip("How fast a punch at someone you aren't holding sends them, on top of how they were going (m/s): the speed of your arm, this much at least and at most.")]
    public float punchShoveLeast = 2.5f;
    [Tooltip("How fast a punch at someone you aren't holding sends them, on top of how they were going (m/s): the speed of your arm, this much at least and at most.")]
    public float punchShoveMost = 4.5f;
    [Tooltip("Knocked flat, the fastest they can go along the floor from it, however fast they were running (m/s).")]
    public float punchShoveLimit = 5f;
    [Tooltip("Having let go of someone, how long before a swing can land (s): the throw's swing is still going, and they're still right there.")]
    public float punchAfterThrow = 0.6f;
    [Tooltip("Someone who's died in your hand stays in it this long at most, for their body to be made there (s).")]
    public float heldDeadFor = 1.5f;
    [Tooltip("With no word back from someone you've gripped that they're held after this long, they aren't (s).")]
    public float neverHeldAfter = 3f;
    [Tooltip("The fastest you can throw a body or a part of one (m/s).")]
    public float maxBodyThrow = 9f;
    [Tooltip("Hold the left X button this long to be the ape again (s).")]
    public float backHold = 1f;
    [Tooltip("How much faster you are as a monster (the user, 2026-11-04: \"make the person who is the monster slightly faster than the other players\"): your launches on your arms, or your run on the stick.")]
    public float faster = 1.15f;
    private MonsterCatalog.Entry entry;
    private bool locomotionWasOn = true;
    private MonsterAvatar worn;
    private CharacterController walker;
    private Vector3 planar;
    private float vy;
    private float backT;
    private float killCooldown;
    private float standHeight;
    private float wornFor;
    private float standEyes;
    private float crawlSize = 1f;
    private bool fitted;
    private Vector3 floatBy;
    private bool floatThrough;
    private int floatFrame = -10;
    private RigidbodyInterpolation interp = (RigidbodyInterpolation)1;
    private float maxArm;
    private float unstick;
    private float minCast;
    private float velLimit;
    private float maxJump;
    private float jumpBy;
    private Vector3 offL;
    private Vector3 offR;
    private bool crawling;
    private bool gripL;
    private bool gripR;
    private bool wasGripL;
    private bool wasGripR;
    private bool ripping;
    private bool ripWhole;
    private Vector3 lastL;
    private Vector3 lastR;
    private Vector3 velL;
    private Vector3 velR;
    private Vector3 throwL;
    private Vector3 throwR;
    private Vector3 lastRealL;
    private Vector3 lastRealR;
    private Vector3 realVelL;
    private Vector3 realVelR;
    private float punchIn;
    private float swungFor;
    private Vector3 swung;
    private readonly Vector3[] armWas = new Vector3[2];
    private readonly Vector3[] armSwung = new Vector3[2];
    private readonly float[] armSwungAt = new float[2];
    private readonly float[] armLostAt = new[] { -10f, -10f };
    private readonly int[] deadBody = new[] { -1, -1 };
    private readonly float[] deadSince = new float[2];
    private NetworkApe held;
    private GillaDummy heldDummy;
    private bool heldRight;
    private float heldSince;
    private NetworkApe second;
    private GillaDummy secondDummy;
    private bool secondRight;
    private float secondSince;
    private ApePart ripPart;
    private float ripStart;
    private float ripStartReal;
    private readonly int[] bodyOf = new[] { -1, -1 };
    private readonly int[] bodyPart = new[] { -1, -1 };
    private int ripBody = -1;
    private int ripBodyPart;
    private bool ripBodyRight;
    private float ripBodyStart;
    private float ripBodyStartReal;
    private int awaitBody = -1;
    private int awaitPart;
    private bool awaitRight;
    private float awaitUntil;
    private float nextAtRest;
    public static int TestGrips = -1;
    public static Action TestEachFrame;
    private const float PunchNear = 1.6f;
    private const int Floor = -5893;
    private float snaredTill;
    private float struggleBuzz;
    private Vector3 snaredAt;
    private bool wasSnared;
    private int fistsFrame = -10;
    private const float SlamFastest = 30;
    public static bool TestAnyHandPunches;
    private const float OnSomeoneFrom = 2.5f;
    private readonly List<Vector3> touching = new List<Vector3>(4);
    private float closedL = -10f;
    private float closedR = -10f;

    public static MonsterPlayer Local { get; private set; }
    public static int Current { get; private set; }
    public static bool IsMonster { get { return Current != 0; } }
    public static float ApeSize { get; private set; } = 1f;

    public static event Action RigChanging;
    public static event Action RigChanged;

    private static bool MenuShoves
    {
        get
        {
            if (IsMonster) return false;
            return !OwnerPunches;
        }
    }

    public static bool ApePunchesOffHere
    {
        get
        {
            if (NetworkBootstrap.Instance != null)
                return NetworkBootstrap.Instance.Kind != NetworkBootstrap.Lobby.Regular;
            return false;
        }
    }

    // NetworkApe.drawn is not accessible from here: reach it by reflection.
    private static void CallDrawn(NetworkApe ape, string method, params object[] args) /* ? */
    {
        if (ape == null) return;
        FieldInfo f = typeof(NetworkApe).GetField("drawn", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f == null) return;
        object d = f.GetValue(ape);
        if (d == null) return;
        if (d is UnityEngine.Object uo && uo == null) return;
        MethodInfo m = d.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null) m.Invoke(d, args);
    }

    private static GillaDoor[] AllDoors() /* ? */
    {
        return UnityEngine.Object.FindObjectsByType<GillaDoor>(FindObjectsSortMode.None);
    }

    private void DropBoth_b__0(Vector3 velocity)
    {
        if (heldDummy != null)
            DropDummy(velocity);
        if (held != null)
        {
            if (held.Object != null && held.Object.Runner && held.Object.Runner.Exists(held.Object) /* ? */)
            {
                Vector3 v = Vector3.ClampMagnitude(velocity, hardestThrow);
                held.RPC_Release(held.head.position, v);
                CallDrawn(held, "LetGo", held.head.position, v); /* ? */
                punchIn = Mathf.Max(punchIn, punchAfterThrow);
            }
        }
        held = null;
        ripping = false;
    }

    private void TakeSecond_b__0(bool right, float reach)
    {
        HandPoints(right, touching);
        NetworkApe ape = OnSomeone(touching, skinTouch * Mathf.Max(transform.localScale.x, 0.4f));
        if (ape == null)
        {
            Vector3 point = touching[0];
            if (Local != null && Walled(Local.head.position, point))
                return;
            float away;
            GillaDummy d = GillaDummy.Nearest(point, reach, out away);
            if (d == null) return;
            Vector3 hand = Holding(right).position;
            if (!OnBody(d.tearing, null, null, touching, skinTouch * Mathf.Max(transform.localScale.x, 0.4f)))
                return;
            PlayerRef you = You;
            Vector3 eyesFromHand;
            if (d.head != null)
                eyesFromHand = d.head.position - hand;
            else
                eyesFromHand = Vector3.up * 0.36f;
            d.Grab(you, right, eyesFromHand);
            heldDummy = d;
            heldRight = right;
            heldSince = Time.time;
            swung = Vector3.zero;
            Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.8f, 0.15f);
            return;
        }
        if (NetworkApe.Local == null) return;
        Vector3 handPos = Holding(right).position;
        Vector3 off = ape.head.position - handPos;
        ape.RPC_Grab(You /* ? */, right, off);
        CallDrawn(ape, "Take", Holding(right), off); /* ? */
        held = ape;
        heldRight = right;
        heldSince = Time.time;
        swung = Vector3.zero;
        InputDevice dev = InputDevices.GetDeviceAtXRNode(right ? XRNode.RightHand : XRNode.LeftHand);
        if (dev.isValid)
            dev.SendHapticImpulse(0, 0.8f, 0.15f);
    }

    private bool SlabIn(bool right)
    {
        if (slabDoor != null)
            return slabRight == right;
        return false;
    }

    private bool PunchDoor(Vector3 fist, Vector3 swing, float size)
    {
        if (!IsMonster) return false;
        float reach = punchReach * size;
        foreach (GillaDoor d in AllDoors()) /* ? */
        {
            if (d == null) continue;
            if (!d.modeMap || d.Blown) continue;
            Collider box = d.SlabBox;
            if (box == null) continue;
            if (!box.enabled) continue;
            Vector3 at = box.ClosestPoint(fist);
            if ((at - fist).sqrMagnitude > reach * reach) continue;
            Vector3 through = d.Through;
            Vector3 dir = -through;
            if (Vector3.Dot(d.Foot - head.position, through) >= 0f)
                dir = through;
            if (swing.magnitude * 0.3f <= Vector3.Dot(swing, dir))
            {
                d.Punch(at, dir);
                return true;
            }
        }
        return false;
    }

    private void TakeSlabs(float reach)
    {
        if (!IsMonster) return;
        if (slabDoor != null) return;
        for (int i = 0; i < 2; i++)
        {
            bool right = i == 1;
            bool grip = right ? gripR : gripL;
            bool wasGrip = right ? wasGripR : wasGripL;
            if (wasGrip || !grip) continue;
            if (bodyOf[i] >= 0) continue;
            Transform follower = right ? rightFollower : leftFollower;
            GillaCorpse corpse;
            int part;
            if (GillaCorpse.Nearest(follower.position, reach, out corpse, out part)) continue;
            GillaDoor door = NearestLoose(follower.position, reach);
            if (door == null) continue;
            Transform loose = door.Loose;
            Quaternion inv = Quaternion.Inverse(follower.rotation);
            Vector3 localPos = inv * (loose.position - follower.position);
            Quaternion localRot = inv * loose.rotation;
            NetworkApe you = NetworkApe.Local; /* ? */
            if (you != null && you.Object != null && you.Object.HasStateAuthority /* ? */)
            {
                you.RPC_HoldDoor((byte)door.index, right, localPos, localRot);
            }
            else
            {
                door.Hold(PlayerRef.None.PlayerId, right, localPos, localRot);
            }
            slabDoor = door;
            slabRight = right;
            slabWasSet = false;
            if (slabThrown == door)
                slabThrown = null;
            Buzz(right ? XRNode.RightHand : XRNode.LeftHand, 0.8f, 0.15f);
            return;
        }
    }

    private static GillaDoor NearestLoose(Vector3 at, float reach)
    {
        GillaDoor best = null;
        foreach (GillaDoor d in AllDoors()) /* ? */
        {
            if (d == null) continue;
            if (!d.modeMap || d.HeldNow) continue;
            Collider box = d.LooseBox;
            if (box == null) continue;
            float dist = Vector3.Distance(box.ClosestPoint(at), at);
            if (reach <= dist) continue;
            reach = dist;
            best = d;
        }
        return best;
    }

    private void SlabInHand()
    {
        if (slabDoor != null)
        {
            if (slabDoor.HeldNow && slabDoor.Loose != null)
            {
                bool grip = slabRight ? gripR : gripL;
                if (!grip)
                    LetGoSlab(slabRight ? throwR : throwL);
                else
                    Squashing(slabDoor, true);
            }
            else
            {
                slabDoor = null;
            }
        }
        if (slabThrown == null) return;
        if (Time.time <= slabThrownTill && slabThrown.Loose != null && !slabThrown.HeldNow)
        {
            Squashing(slabThrown, false);
            return;
        }
        slabThrown = null;
    }

    private void LetGoSlab(Vector3 velocity)
    {
        GillaDoor d = slabDoor;
        slabDoor = null;
        if (d == null) return;
        if (d.Loose == null) return;
        Transform loose = d.Loose;
        Vector3 v = Vector3.ClampMagnitude(velocity, maxDoorThrow);
        NetworkApe you = NetworkApe.Local;
        if (you != null && you.Object != null && you.Object.Runner && you.Object.Runner.Exists(you.Object) /* ? */)
        {
            you.RPC_LetGoDoor((byte)d.index, loose.position, loose.rotation, v, Vector3.zero);
        }
        else
        {
            d.LetGo(loose.position, loose.rotation, v, Vector3.zero);
        }
        slabThrown = d;
        slabThrownTill = Time.time + 3f;
    }

    private void Squashing(GillaDoor d, bool inHand)
    {
        SquashingScope s = new SquashingScope();
        s.inHand = inHand;
        s.owner = this;
        s.box = d.LooseBox;
        Transform loose = d.Loose;
        if (s.box == null) return;
        if (loose == null) return;
        s.dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Matrix4x4 now = loose.localToWorldMatrix;
        if (!inHand || !slabWasSet)
            s.back = Matrix4x4.identity;
        else
            s.back = slabWas * now.inverse;
        s.rb = null;
        if (inHand || (loose.TryGetComponent<Rigidbody>(out s.rb), inHand).Item2)
        {
            if (!slabWasSet)
            {
                slabWasSet = true;
                slabWas = now;
                return;
            }
        }
        Vector3 way;
        foreach (NetworkApe a in NetworkApe.All)
        {
            if (!CanTouch(a)) continue;
            int key = (int)a.Object.Id.Raw; /* ? */
            float when;
            if (squashed.TryGetValue(key, out when) && Time.time - when < 3f) continue;
            Vector3 eyes = a.head.position;
            if (!Squashing_Meets(eyes, out way, ref s))
            {
                if (!Squashing_Meets(eyes + a.MiddleFromEyes, out way, ref s)) continue;
            }
            squashed[key] = Time.time;
            a.RPC_Squished(eyes, way);
            Buzz(XRNode.LeftHand, 1f, 0.3f);
            Buzz(XRNode.RightHand, 1f, 0.3f);
        }
        GillaDummy[] dummies = GillaDummy.All.ToArray();
        for (int i = 0; i < dummies.Length; i++)
        {
            GillaDummy dm = dummies[i];
            if (dm == null) continue;
            if (!dm.Alive || !dm.Mine) continue;
            if (dm.head == null || dm.tearing == null) continue;
            if (!Squashing_Meets(dm.head.position, out way, ref s))
            {
                if (!Squashing_Meets(dm.Middle, out way, ref s)) continue;
            }
            NetworkApe.Crushed(dm.head.position, way);
            GillaBlast.CrushDummy(dm, dm.head.position + Vector3.up * 0.3f, way * 2.5f + Vector3.up);
            Buzz(XRNode.LeftHand, 1f, 0.3f);
            Buzz(XRNode.RightHand, 1f, 0.3f);
        }
        if (!inHand) return;
        slabWasSet = true;
        slabWas = now;
    }

    // <Squashing>g__Going|16_0: how fast the door's point at `on` is going.
    private Vector3 Squashing_Going(Vector3 on, ref SquashingScope s) /* ? */
    {
        if (s.inHand)
            return (on - s.back.MultiplyPoint3x4(on)) / s.dt;
        if (s.rb != null)
            return s.rb.GetPointVelocity(on);
        return Vector3.zero;
    }

    // <Squashing>g__Meets|16_1: whether the door meets p fast enough to squash.
    private bool Squashing_Meets(Vector3 p, out Vector3 way, ref SquashingScope s) /* ? */
    {
        way = Vector3.zero;
        Vector3 c = s.box.ClosestPoint(p);
        if ((c - p).sqrMagnitude > squashReach * squashReach) return false;
        Vector3 going = Squashing_Going(c, ref s);
        if (going.magnitude < squashSpeed) return false;
        way = going.normalized;
        return true;
    }

    private void TellSlabsAtRest(NetworkApe you)
    {
        foreach (GillaDoor d in AllDoors()) /* ? */
        {
            if (d != null && d.modeMap)
            {
                Vector3 at;
                Quaternion rot;
                if (d.NewlyAtRest(out at, out rot))
                    you.RPC_PlaceDoor((byte)d.index, at, rot);
            }
        }
    }

    private void Awake()
    {

        Local = this;
        TestEachFrame = null;
        ApeSize = 1f;
    }

    private void Start()
    {
        PrepareRips();
    }

    private void OnDestroy()
    {
        if (Local == this)
        {
            Local = null;
            Current = 0;
        }
    }

    public void Become(int monster)
    {
        if (Current == monster) return;
        if (KillCam.Local != null && KillCam.Local.Busy) return;
        if (RigChanging != null) RigChanging();
        Turn(monster);
        if (RigChanged != null) RigChanged();
    }

    private void Turn(int monster)
    {
        if (KillCam.Local != null)
            KillCam.Local.SendMessage("Heal", SendMessageOptions.DontRequireReceiver); /* ? */
        if (Current == 0)
        {
            if (monster != 0)
            {
                ApeLetGo();
                Resize(1f, false);
            }
        }
        else
        {
            BackToApe();
        }
        MonsterCatalog.Entry e = MonsterCatalog.Of(monster);
        if (e == null) return;
        entry = e;
        Current = monster;
        for (int i = 0; i < apeBodies.Length; i++)
        {
            GameObject g = apeBodies[i];
            if (g != null) g.SetActive(false);
        }
        worn = MonsterAvatar.Create(e, null, head, leftFollower, rightFollower, true);
        worn.leftAim = leftTarget;
        worn.rightAim = rightTarget;
        if (!e.legs)
            Crawl(e);
        else
            Walk(e);
        lastL = leftFollower.position;
        lastR = rightFollower.position;
        lastRealL = RealHand(false);
        lastRealR = RealHand(true);
        armWas[0] = transform.InverseTransformPoint(lastRealL);
        armWas[1] = transform.InverseTransformPoint(lastRealR);
        Buzz(XRNode.LeftHand, 0.6f, 0.2f);
        Buzz(XRNode.RightHand, 0.6f, 0.2f);
        Debug.Log("[MonsterPlayer] you're " + e.name);
    }

    private static void PrepareRips()
    {
        NetworkBootstrap nb = NetworkBootstrap.Instance;
        if (nb == null) return;
        if (nb.tearing == null) return;
        Dismemberment[] copies = nb.tearing.copies;
        if (copies == null) return;
        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment b = copies[i];
            if (b != null && b.spawnPieces)
                GillaRip.Prepare(b);
        }
    }

    private void Walk(MonsterCatalog.Entry e)
    {
        standHeight = head.localPosition.y;
        Vector3 floor = FloorUnder(head.position);
        locomotionWasOn = locomotion == null || locomotion.enabled;
        if (locomotion != null)
            locomotion.enabled = false;
        Freeze(true);
        transform.position = new Vector3(transform.position.x, floor.y, transform.position.z);
        if (body != null)
            body.position = transform.position;
        Physics.SyncTransforms();
        walker = gameObject.AddComponent<CharacterController>();
        walker.slopeLimit = 45f;
        standEyes = e.eyeHeight;
        Size(standEyes);
        fitted = false;
        wornFor = 0f;
        vy = 0f;
        planar = Vector3.zero;
    }

    private void Size(float eyeHeight)
    {
        float k = Mathf.Clamp(eyeHeight / standHeight, 0.5f, 2f) * ApeSize;
        ScaleAboutHead(k);
        walker.height = (eyeHeight + 0.15f) * ApeSize / k;
        walker.radius = Mathf.Min(eyeHeight * 0.2f, 0.3f) * ApeSize / k;
        walker.skinWidth = walker.radius * 0.1f;
        walker.stepOffset = ApeSize * 0.3f / k;
    }

    private void Crawl(MonsterCatalog.Entry e)
    {
        float k;
        if (!e.hangs || e.reach <= 0f)
            k = e.eyeHeight / apeEyeHeight;
        else
            k = e.reach / apeReach;
        k = Mathf.Clamp(k, 0.2f, 3.5f);
        Player p = locomotion;
        maxArm = p.maxArmLength;
        unstick = p.unStickDistance;
        minCast = p.minimumRaycastDistance;
        velLimit = p.velocityLimit;
        maxJump = p.maxJumpSpeed;
        jumpBy = p.jumpMultiplier;
        offL = p.leftHandOffset;
        offR = p.rightHandOffset;
        p.leftHandOffset = p.leftHandOffset * k;
        p.jumpMultiplier = faster * p.jumpMultiplier;
        p.minimumRaycastDistance = k * p.minimumRaycastDistance;
        p.velocityLimit = p.velocityLimit * k;
        p.maxJumpSpeed = p.maxJumpSpeed * faster;
        p.maxArmLength = p.maxArmLength * k;
        p.unStickDistance = p.unStickDistance * k;
        locomotion.rightHandOffset = locomotion.rightHandOffset * k;
        crawlSize = k;
        Vector3 floor = FloorUnder(head.position);
        ScaleAboutHead(k);
        float eyes = !e.hangs ? e.eyeHeight : k * apeEyeHeight;
        Settle(floor.y + eyes + 0.05f);
        crawling = true;
    }
}
