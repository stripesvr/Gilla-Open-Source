// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
public partial class MonsterBrain : MonoBehaviour
{
    public enum State
    {
        Wander = 0,
        Notice = 1,
        Chase = 2,
        Search = 3,
        Kill = 4,
        Recover = 5,
        Pry = 6,
        Break = 7
    }

    private struct Target
    {
        public Transform head;
        public PlayerRef player;
        public bool local;
        public float voice;
        public GillaDummy dummy;
    }

    [Header("Parts (set by Gilla > Monsters > Build Monster Prefabs)")]
    public Animator animator;
    [Tooltip("Where the victim's head is held during the Kill clip (the rig's 'grab' bone).")]
    public Transform grabSocket;
    [Tooltip("A point on the monster's face, for the victim to look at (on its head bone: the head turns to look).")]
    public Transform face;
    [Tooltip("Ground speed the Walk / Run clips were animated at, at this size (m/s).")]
    public float clipWalkSpeed = 0.7f;
    [Tooltip("Ground speed the Walk / Run clips were animated at, at this size (m/s).")]
    public float clipRunSpeed = 2.4f;
    [Tooltip("How long its Kill clip runs (s): a kill on another player's headset is over after this.")]
    public float killSeconds = 5f;
    public MonsterNet net;
    [Header("Movement")]
    public float walkSpeed = 1.1f;
    public float runSpeed = 4.2f;
    [Tooltip("Top turning speed (degrees per second); turns ease in and out.")]
    public float turnSpeed = 260f;
    public float gravity = 20f;
    [Header("Senses")]
    public float sightRange = 14f;
    [Tooltip("How wide it sees (degrees, all told): it doesn't see you behind it...")]
    public float sightAngle = 150f;
    [Tooltip("...unless you're this close (m): it hears / smells you.")]
    public float senseRange = 3f;
    [Tooltip("Gives up the chase past this distance (user 10-30: \"if you get far enough away they will stop chasing, like pretty far though\").")]
    public float loseRange = 30f;
    [Tooltip("Hears you talk (user 10-31: \"make it so the monsters can hear you talk... if you're hiding around a wall and you say something... or if you're behind them\"): out to this far (m) at full voice. It goes by your mic level - NetworkApe's Voice, which every player's game gets - so it hears everyone, on whichever game runs it.")]
    public float hearRange = 18f;
    [Tooltip("The mic level (0-1) that counts as talking: under it is breathing and the room. Just over it carries a third of hearRange.")]
    public float hearThreshold = 0.12f;
    [Tooltip("Through a wall, floor or door your voice carries this much as far.")]
    public float hearThroughWalls = 0.6f;
    [Tooltip("Out of sight it follows your trail: a point every this many metres of where you went.")]
    public float trailSpacing = 0.6f;
    [Tooltip("Grabs the player when their head is this close to its hands.")]
    public float grabRange = 0.9f;
    private const float OnTopOf = 0.45f;
    [Tooltip("...and only when their head is between this far below its feet and this far above them: not from the floor above, a tunnel below, or up on a ledge (user 10-30: \"monsters can kill you when above and below you\").")]
    public float reachBelow = 0.3f;
    [Tooltip("...and only when their head is between this far below its feet and this far above them: not from the floor above, a tunnel below, or up on a ledge (user 10-30: \"monsters can kill you when above and below you\").")]
    public float reachAbove = 2.3f;
    [Tooltip("Wanders to random spots up to this far away: anywhere it can walk to, on a NavMesh (the map's monsters roam 25 m at a time); round where it was placed without one.")]
    public float wanderRadius = 6f;
    [Tooltip("With you up somewhere it can't get to, it waits below this long, then gives up and walks away (user 10-30: \"if you get to a high spot that they cant get they will walk away\").")]
    public float giveUpSeconds = 3f;
    [Tooltip("...and leaves you be for this long after walking away.")]
    public float walkAwaySeconds = 6f;
    [Tooltip("After a kill it ignores players for this long.")]
    public float recoverSeconds = 3f;
    [Tooltip("Seconds the kill cam holds after the killing blow before fading (0 = straight to black, below 0 = KillCam's own).")]
    public float killHoldAfterBite = -1f;
    [Tooltip("Testing only (the spawn stand sets it): gone once its kill is over, so you can spawn it and test again.")]
    public bool despawnAfterKill;
    [Tooltip("Its kill knocks you flat: the kill cam tips your view up to watch it come down on your face (the ankle biter).")]
    public bool killLooksUp;
    [Tooltip("Its voice all through a chase (user 10-31: \"we need to have like a repeating good sounding monster sounds as it chases you, not those\" - the one-off screams it had): a seamless loop of its snarls and growls, up as it comes for you and dying away when it gives up.")]
    public AudioClip chaseLoop;
    public float chaseLoopVolume = 1f;
    [Header("Hurt (dynamite)")]
    [Tooltip("How much harm kills it (the user, 2026-11-04: the monsters \"have a lot more health, so they won't take as much damage\"): a stick of dynamite does GillaBlast.monsterDamage at its heart, less further off.")]
    public float health = 400f;
    [Tooltip("Seconds it reels after a blast: stopped where it is, shoved back.")]
    public float staggerSeconds = 1.1f;
    [Tooltip("Seconds it lies dead before it's gone (MonsterDirector brings one back a while later).")]
    public float deadSeconds = 5f;
    [Header("Doors")]
    [Tooltip("Pries shut hazard doors open when one's in its way (Skull3; user 10-31: \"every time he runs up to a door, he'll just open it, that's closed. If it's open, then he just runs through it\"). Without it a shut door is a wall.")]
    public bool pries;
    [Tooltip("Breaks shut hazard doors down: at one in its way it squares up to it and after Break Seconds the door blows off its frame (the ankle biter; user 10-31: \"after 2 seconds of him at the door it just explodes off\").")]
    public bool breaksDoors;
    public float breakSeconds = 2f;
    [Tooltip("Where it stands to break a door down: its feet this far from the door's face (m).")]
    public float breakStand = 0.35f;
    [Tooltip("Where it stands to pry: its feet this far from the door's face (m; Blender lethal_anim PRY_D x 1.8).")]
    public float pryStand = 0.61f;
    [Tooltip("Its Pry clip, from where it plays from (its first blow on the door; s): when the door gives (GillaDoor.Pry starts it up its slow pried way: lethal_anim PRY_LIFT), and when it lets go of it at the top and runs on through (PRY_LET_GO).")]
    public float pryLiftSeconds = 1.53f;
    [Tooltip("Its Pry clip, from where it plays from (its first blow on the door; s): when the door gives (GillaDoor.Pry starts it up its slow pried way: lethal_anim PRY_LIFT), and when it lets go of it at the top and runs on through (PRY_LET_GO).")]
    public float pryDoneSeconds = 5.67f;
    [Header("Climbing")]
    [Tooltip("How fast it goes up a climbable wall (m/s).")]
    public float climbSpeed = 1.8f;
    [Tooltip("How far it tips nose-up on the wall (degrees): its feet on the wall, its body out from it.")]
    public float climbPitch = 78f;
    [Header("Wall crawling (Weaver)")]
    [Tooltip("It climbs the walls as it wanders (user 10-31: \"make it so the spider walks on the walls\"): up a wall to hang there, flat to it, watching - and drops off it onto whoever it spots.")]
    public bool wallCrawler;
    [Tooltip("About how often it goes up a wall while wandering (s).")]
    public float lurkEvery = 22f;
    [Tooltip("How high up the wall it hangs (m, its feet).")]
    public float lurkHeight = 1.8f;
    [Header("Weeping (the Angel)")]
    [Tooltip("A weeping angel (the user, 2026-11-06: \"whenever someone looks at him, he cannot move, and when you look away, he is extremely fast and runs towards you, the only way to get away from him is to be looking at him until you're out of his line of sight ... he can walk around if no one is looking at him but he shouldn't target a player until they look at him first\"): stone still, just as it was, while anyone has it in view; the moment no one does, after whoever's looked at it and is still in its sight, at a rush. Out of its sight, they're free of it till they look at it again. It neither spots nor hears anyone.")]
    public bool weeping;
    [Tooltip("How far off the middle of your view it can be and still be seen (degrees): about half a headset's view across.")]
    public float gazeAngle = 52f;
    [Tooltip("Seen out to this far (m).")]
    public float gazeRange = 45f;

    public static readonly List<MonsterBrain> All = new List<MonsterBrain>();
    public static bool Frozen;
    public static bool IgnoreLocal;
    public static float TestVoice = -1f;
    private CharacterController cc;
    private Transform headBone;
    private Vector3 home;
    private Vector3 wanderTarget;
    private Vector3 stuckAt;
    private Vector3 pathGoal;
    private Vector3 lookWant;
    private Vector3 lastSeen;
    private readonly List<Vector3> trail = new List<Vector3>();
    private readonly List<Target> targets = new List<MonsterBrain.Target>();
    private readonly Dictionary<PlayerRef, float> killedAt = new Dictionary<PlayerRef, float>();
    private Target prey;
    private bool hasPrey;
    private bool killLocal;
    private bool pathOk;
    private bool pathEnds;
    private float wait;
    private float vy;
    private float speed;
    private float stateT;
    private float unreachT;
    private float ignoreT;
    private float stuckT;
    private float repathAt;
    private float yawVel;
    private float gait = 1f;
    private float nextGlance;
    private float lookWeightWant;
    private NavMeshPath path;
    private NavMeshPath scratch;
    private int corner;
    private const int Solid = -1797;
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int PaceId = Animator.StringToHash("Pace");
    private static readonly int KillId = Animator.StringToHash("Kill");
    private static readonly int PryId = Animator.StringToHash("Pry");
    private bool hasPry;
    private State pryFrom;
    private float pryT;
    private float doorCheckAt;
    private bool pryLifted;
    private Vector3 prySpot;
    private Vector3 pryToward;
    private MonsterClimb climb;
    private bool lurking;
    private int lurkPhase;
    private float lurkT;
    private float lurkHold;
    private float nextLurk;
    private Vector3 lurkFoot;
    private Vector3 lurkInto;
    private Vector3 lurkFrom;
    private Vector3 lurkLand;
    private int climbPhase;
    private float climbT;
    private Vector3 climbFoot;
    private Vector3 climbFrom;
    private Transform model;
    private Quaternion modelRest = Quaternion.identity;
    private AudioSource voice;
    [Tooltip("Lives in the backrooms (the Mannequin): asleep whenever they're not all switched on here.")]
    public bool livesBelow;
    private bool asleep;
    private Renderer[] drawn;
    private readonly List<Renderer> drawnOn = new List<Renderer>();
    private Vector3 shove;
    private Vector3 fallAxis;
    private float staggerT;
    private float deadT;
    private float snareT;
    private float stumbleAt;
    private Quaternion deadFrom;
    private Vector3 deadAt;
    private const float StumblesPast = 0.45f;
    private float noticeFor = 0.6f;
    private readonly Dictionary<ValueTuple<int, int>, float> marked = new Dictionary<ValueTuple<int, int>, float>();
    private readonly List<ValueTuple<int, int>> unmark = new List<ValueTuple<int, int>>();
    private readonly Vector3[] parts = new Vector3[3];
    private const float FreeAfter = 0.4f;
    private static MicLevel localMic;
    private bool listening;
    private float listeningSince;
    private float stoodT;
    private Vector3 stoodAt;
    private const float ListenMost = 12f;
    private const float ListenAgain = 25f;
    private const float StoodMost = 30f;
    private float hearAt;
    private float searchFor = 9f;
    private readonly Dictionary<Transform, (float until, bool ok, float length)> ways = new Dictionary<Transform, ValueTuple<float, bool, float>>();
    private float easiestAt;
    private const float EasiestEvery = 0.4f;
    private const float EasiestBy = 2f;
    private float breakT;

    public State state { get; private set; }
    public float Health { get; private set; }
    public bool Dead { get; private set; }
    public MonsterWounds Hurt { get; private set; }
    public float SnareLeft { get; private set; }
    public float ClimbPitch { get; private set; }
    public GillaDoor PryDoor { get; private set; }
    public bool Prying { get; private set; }
    public bool Puppet { get; set; }
    public float AnimSpeed { get; private set; }
    public float AnimPace { get; private set; } = 1f;
    public Vector3 LookPoint { get; private set; }
    public float LookWeight { get; private set; }
    public bool Chasing { get; private set; }

    public Vector3 Middle
    {
        get
        {
            if (cc != null)
            {
                return transform.TransformPoint(cc.center);
            }
            return transform.position + Vector3.up;
        }
    }

    public bool Climbing
    {
        get { return climb != null; }
    }

    public Transform PreyHead
    {
        get
        {
            if (hasPrey)
            {
                return prey.head;
            }
            return null;
        }
    }

    private void Awake()
    {

        cc = GetComponent<CharacterController>();
        Hurt = gameObject.AddComponent<MonsterWounds>();
        Health = health;
        model = animator != null ? animator.transform : null;
        if (model != null)
        {
            modelRest = model.localRotation;
        }
        home = transform.position;
        path = new NavMeshPath();
        scratch = new NavMeshPath();
        headBone = face != null ? face.parent : null;
        gait = UnityEngine.Random.Range(0.85f, 1.12f);
        if (net == null)
        {
            net = GetComponent<MonsterNet>();
        }
        Transform at = face != null ? face : transform;
        voice = at.gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.loop = true;
        voice.clip = chaseLoop;
        voice.volume = 0f;
        voice.spatialBlend = 1f;
        voice.rolloffMode = AudioRolloffMode.Logarithmic;
        voice.minDistance = 3f;
        voice.maxDistance = 45f;
        voice.dopplerLevel = 0f;
        if (GetComponent<MonsterClimbDebris>() == null)
        {
            gameObject.AddComponent<MonsterClimbDebris>();
        }
    }

    public void ShowChasing(bool on)
    {
        Chasing = on;
    }

    private void ChaseVoice(float dt)
    {
        if (voice == null)
        {
            return;
        }
        if (voice.clip == null)
        {
            return;
        }
        AudioSource src = voice;
        src.mute = !GillaBackroomsSwitch.OnYourSide(transform.position);
        bool loud = Chasing && (!Frozen || Puppet);
        float target = loud ? chaseLoopVolume : 0f;
        float rate = loud ? 0.25f : 1.2f;
        float current = voice.volume;
        voice.volume = Mathf.MoveTowards(current, target, chaseLoopVolume * dt / rate);
        if (voice.volume > 0f && !voice.isPlaying)
        {
            voice.time = UnityEngine.Random.Range(0f, voice.clip.length);
            voice.Play();
            return;
        }
        if (voice.volume > 0f)
        {
            return;
        }
        if (!voice.isPlaying)
        {
            return;
        }
        voice.Stop();
    }

    private void Start()
    {
        home = transform.position;
        PickWander(null);
        if (animator != null)
        {
            AnimatorControllerParameter[] ps = animator.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].nameHash == PryId)
                {
                    hasPry = true;
                }
            }
        }
    }

    private void OnEnable()
    {

        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (livesBelow)
        {
            GillaBackroomsSwitch.ForMonster = false;
        }
    }

    private bool Asleep()
    {
        GillaBackroomsSwitch sw = GillaBackroomsSwitch.Instance;
        if (sw == null)
        {
            return false;
        }
        if (!Puppet)
        {
            bool found = false;
            foreach (NetworkApe ape in NetworkApe.All)
            {
                if (ape == null) continue;
                if (ape.IsLocal) continue;
                Transform h = ape.head /* ? */;
                if (h == null) continue;
                if (sw.below.Contains(h.position))
                {
                    found = true;
                    break;
                }
            }
            GillaBackroomsSwitch.ForMonster = found;
        }
        bool allOn = sw.AllOn;
        bool sleep = !allOn;
        if (asleep == sleep)
        {
            return sleep;
        }
        asleep = sleep;
        if (allOn)
        {
            foreach (Renderer r in drawnOn)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }
        }
        else
        {
            drawn = GetComponentsInChildren<Renderer>(true);
            drawnOn.Clear();
            for (int i = 0; i < drawn.Length; i++)
            {
                Renderer r = drawn[i];
                if (r != null && r.enabled)
                {
                    drawnOn.Add(r);
                    r.enabled = false;
                }
            }
        }
        if (animator != null)
        {
            animator.enabled = allOn;
        }
        if (!allOn)
        {
            if (voice != null)
            {
                voice.Stop();
            }
            if (Puppet)
            {
                return true;
            }
            Chasing = false;
            hasPrey = false;
            state = State.Wander;
            stateT = 0f;
            listening = false;
            return true;
        }
        return false;
    }
}
