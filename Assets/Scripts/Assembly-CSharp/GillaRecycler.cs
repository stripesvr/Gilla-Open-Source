// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public partial class GillaRecycler : MonoBehaviour
{
    private class Going
    {
        public GillaItem item;
        public float t;
        public Vector3 from;
        public Vector3 size;
        public Vector3 spin;
        public Quaternion turn;
        public bool moves;
    }

    [Header("Parts (GillaSetup.Recycler builds them)")]
    [Tooltip("The screen: a quad, its front -Z, scaled to the screen's size.")]
    public Renderer screen;
    [Tooltip("The space in its mouth that swallows what goes in: a box, this transform's scale its size.")]
    public Transform mouth;
    [Tooltip("The space over its lip, sloping into its mouth: what lands on it goes in too (a box, as Mouth).")]
    public Transform lip;
    [Tooltip("Where what it swallows is pulled down to: between the rollers' tops.")]
    public Transform nip;
    [Tooltip("The way its front faces, in its own frame (its model's forward).")]
    public Vector3 front = Vector3.forward;
    [Tooltip("What turns in its mouth (the two rollers, the back wall's gears), each round its axis (in its own frame) at its rate, for one of a roller's turns (the rollers -1 and 1, pulling in at their tops; the gears by their teeth).")]
    public Transform[] spinners = new Transform[0];
    public Vector3[] axes = new Vector3[0];
    public float[] rates = new float[0];
    [Tooltip("Its body, and which of its materials is its lamps' (the beacon's and the panel's).")]
    public Renderer body;
    public int lampSlot = -1;
    [Tooltip("The beacon's light, on while it works.")]
    public Light beacon;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), everything else's (GillaLCD).")]
    public TextAsset textFont;
    [Tooltip("It boots when your head comes this close (m), and goes dark past WakeRange + 2.")]
    public float wakeRange = 6f;
    [Tooltip("The rollers' turn idling and working (degrees a second).")]
    public float idleSpeed = 30f;
    [Tooltip("The rollers' turn idling and working (degrees a second).")]
    public float workSpeed = 380f;
    [Header("Sounds")]
    public AudioClip hum;
    public AudioClip[] grind;
    public AudioClip[] crunch;
    public AudioClip coinSound;
    public AudioClip sparkleSound;
    public AudioClip tickSound;
    public AudioClip bootSound;
    public AudioClip offSound;
    private const int W = 512;
    private const int H = 256;
    private const float Swallowing = 0.65f;
    private const float Recycling = 1f;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly RectInt PriceRect = new RectInt(12, 30, 150, 132);
    private static readonly RectInt WalletRect = new RectInt(12, 170, 150, 76);
    private static readonly RectInt MainRect = new RectInt(172, 30, 328, 216);
    private static readonly Color32 Black = new Color32(2, 6, 4, 255);
    private static readonly Color32 Scan = new Color32(4, 12, 8, 255);
    private static readonly Color32 Green = new Color32(60, 255, 120, 255);
    private static readonly Color32 Dim = new Color32(20, 110, 55, 255);
    private static readonly Color32 Dark = new Color32(8, 40, 20, 255);
    private static readonly Color32 Yellow = new Color32(255, 230, 60, 255);
    private static readonly Color32[] GreenText = new[] { new Color32(170, 255, 190, 255), new Color32(60, 235, 110, 255) };
    private static readonly Color32[] DimText = new[] { new Color32(40, 150, 80, 255), new Color32(20, 110, 55, 255) };
    private static readonly Color32[] BoltText = new[] { new Color32(240, 246, 252, 255), new Color32(160, 182, 205, 255) };
    private static readonly List<GillaRecycler> all = new List<GillaRecycler>();
    private readonly List<Going> going = new List<GillaRecycler.Going>();
    private readonly Queue<(string label, int coins, bool mine)> waiting = new Queue<(string label, int coins, bool mine)>();
    private readonly List<(string label, int coins)> log = new List<(string label, int coins)>();
    private (string label, int coins, bool mine) current;
    private float shown = -1f;
    private int counted;
    private bool paid;
    private float work;
    private float turned;
    private float awake;
    private float flash;
    private float nextDraw;
    private bool wanted;
    private bool dirty = true;
    private Texture2D tex;
    private Color32[] px;
    private MaterialPropertyBlock block;
    private MaterialPropertyBlock lampBlock;
    private ScreenFont title;
    private ScreenFont text;
    private AudioSource humming;
    private ParticleSystem sparks;
    private Quaternion[] rest;
    private Color lampColour = new Color(0.12f, 0.55f, 0.2f, 1f);
    private int lastBalance = -1;
    private (string label, int coins)[] prices = new (string label, int coins)[0];
    private static readonly Dictionary<string, (string[] rows, Color32 l, Color32 m, Color32 d)> Icons = new Dictionary<string, (string[] rows, Color32 l, Color32 m, Color32 d)> { { "GOLD BAR", (new[] { "...LLLLLL...", "..LMMMMMMM..", ".LMMMMMMMMD.", "LMMMMMMMMMMD", "MMMMMMMMMMMD", "DDDDDDDDDDDD" }, new Color32(255, 240, 150, 255), new Color32(240, 190, 40, 255), new Color32(160, 110, 20, 255)) }, { "DIAMOND", (new[] { "..LLLLLLL..", ".LLMMLMMLL.", "LLMMMLMMMLD", "DMMMMLMMMDD", ".DMMMLMMDD.", "..DMMLMDD..", "...DMLDD...", "....DMD....", ".....D....." }, new Color32(230, 250, 255, 255), new Color32(140, 220, 255, 255), new Color32(60, 150, 210, 255)) }, { "RUSTY COG", (new[] { "...M.M...", ".MMMMMMM.", "MMMDDDMMM", ".MMD.DMM.", "MMD...DMM", ".MMD.DMM.", "MMMDDDMMM", ".MMMMMMM.", "...M.M..." }, new Color32(230, 140, 80, 255), new Color32(190, 95, 45, 255), new Color32(120, 55, 25, 255)) }, { "BIG BOLT", (new[] { ".LLL........", "LMMML.......", "MMMMMMMMMMMM", "MMMMMDMDMDMD", "DMMMD.......", ".DDD........" }, new Color32(220, 225, 230, 255), new Color32(160, 165, 175, 255), new Color32(95, 100, 110, 255)) }, { "SCRAP PLATE", (new[] { "LLLLLLLL.L..", "LMMMMMMMMML.", "MMDMMMMMMMMM", "MMMMMMMMMMD.", "MMDMMMMMMMMM", "DDDDDDDDDD.." }, new Color32(200, 195, 190, 255), new Color32(150, 135, 125, 255), new Color32(110, 80, 60, 255)) } };

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void OnEnable()
    {

        if (!all.Contains(this))
        {
            all.Add(this);
            all.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }
        if (Application.isPlaying)
        {
            return;
        }
        if (screen == null)
        {
            return;
        }
        Texture2D t = new Texture2D(512, 256, TextureFormat.RGBA32, true);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.name = "GillaRecycle";
        t.hideFlags = HideFlags.DontSave;
        tex = t;
        px = new Color32[131072];
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        awake = 0f;
        Draw();
    }

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        Texture2D t = new Texture2D(512, 256, TextureFormat.RGBA32, true);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.name = "GillaRecycle";
        tex = t;
        px = new Color32[131072];
        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        rest = spinners.Select(s => s != null ? s.localRotation : Quaternion.identity).ToArray();
        if (body != null && lampSlot >= 0 && lampSlot < body.sharedMaterials.Length)
        {
            if (body.sharedMaterials[lampSlot] != null)
            {
                lampColour = body.sharedMaterials[lampSlot].GetColor(BaseColor);
            }
        }
        IEnumerable<(string label, int coins)> list;
        if (GillaItemDirector.Instance != null)
        {
            list = GillaItemDirector.Instance.Prices();
        }
        else
        {
            list = GillaItem.All
                .Where(i => !i.keeps && i.recycles)
                .GroupBy(i => i.label)
                .Select(g => (g.Key, g.First().coins /* ? */))
                .OrderByDescending(p => p.Item2)
                .ThenBy(p => p.Item1);
        }
        prices = list.Take(5).ToArray();
        if (hum != null)
        {
            humming = gameObject.AddComponent<AudioSource>();
            humming.clip = hum;
            humming.loop = true;
            humming.volume = 0.2f;
            humming.spatialBlend = 1f;
            humming.minDistance = 1.2f;
            humming.maxDistance = 14f;
            humming.rolloffMode = AudioRolloffMode.Linear;
            humming.dopplerLevel = 0f;
            humming.Play();
        }
        Draw();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        float dt = Time.deltaTime;
        Wake(dt);
        Swallow();
        GoDown(dt);
        Count(dt);
        Spin(dt);
        Lamps(dt);
        if (Coins.Bolts != lastBalance)
        {
            lastBalance = Coins.Bolts;
            dirty = true;
        }
        bool animating;
        if ((awake > 0f && awake < 1f) || shown >= 0f)
        {
            animating = true;
        }
        else
        {
            animating = false;
            if (wanted)
            {
                animating = awake >= 1f;
            }
        }
        if (!animating && !dirty)
        {
            return;
        }
        if (Time.time < nextDraw)
        {
            return;
        }
        nextDraw = Time.time + 0.1f;
        Draw();
    }

    public Vector3 Front
    {
        get { return transform.TransformDirection(front).normalized; }
    }

    private void OnDisable()
    {
        all.Remove(this);
    }

    public static GillaRecycler Number(int i)
    {
        if (i >= 0 && i < all.Count)
        {
            return all[i];
        }
        return null;
    }

    public (string label, int coins) Last
    {
        get
        {
            if (log.Count < 1)
            {
                return default;
            }
            return log[0];
        }
    }

    public static bool Inside(Transform box, Vector3 p)
    {
        if (box == null)
        {
            return false;
        }
        Vector3 l = box.InverseTransformPoint(p);
        return Mathf.Abs(l.x) <= 0.5f && Mathf.Abs(l.y) <= 0.5f && Mathf.Abs(l.z) <= 0.5f;
    }

    private void Swallow()
    {
        foreach (GillaItem item in GillaItem.All)
        {
            if (item.keeps || !item.recycles /* ? */)
            {
                continue;
            }
            // Restored from the original: skip what's gone, already swallowed or still in a hand.
            if (item.Gone || item.Swallowed || item.Hand >= 0)
            {
                continue;
            }
            if (!item.Mine)
            {
                continue;
            }
            if (Inside(mouth, item.transform.position) || Inside(lip, item.transform.position))
            {
                item.Swallow(all.IndexOf(this));
                Begin(item, true);
            }
        }
    }

    public void Watch(GillaItem item)
    {
        if (going.Exists(g => g.item == item))
        {
            return;
        }
        Begin(item, false);
    }

    private void Begin(GillaItem item, bool moves)
    {
        Going g = new Going();
        g.item = item;
        g.from = item.transform.position;
        g.turn = item.transform.rotation;
        g.size = item.transform.localScale;
        Vector3 axis = UnityEngine.Random.onUnitSphere;
        float speed = UnityEngine.Random.Range(220f, 420f);
        g.moves = moves;
        g.spin = axis * speed;
        going.Add(g);
        if (grind != null && grind.Length != 0)
        {
            Sfx.Play(grind, mouth.position, 0.85f, 0.85f, 1.05f);
        }
        dirty = true;
    }

    private void GoDown(float dt)
    {
        int index = going.Count;
        if (index - 1 >= 0)
        {
            do
            {
                index--;
                Going g = going[index];
                if (g.item == null)
                {
                    going.RemoveAt(index);
                }
                else
                {
                    g.t += dt / 0.65f;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(g.t));
                    Transform target = nip != null ? nip : mouth;
                    Vector3 at = target.position;
                    if (g.moves)
                    {
                        Transform tr = g.item.transform;
                        tr.position = Vector3.Lerp(g.from, at, k) + UnityEngine.Random.insideUnitSphere * (k * 0.012f);
                        tr.rotation = Quaternion.Euler(g.spin * g.t) * g.turn;
                        tr.localScale = g.size * (1f - 0.88f * Mathf.Min(k * k, 1f));
                    }
                    if (k > 0.35f)
                    {
                        Spark(at, dt);
                    }
                    if (g.t >= 1f)
                    {
                        if (g.moves)
                        {
                            g.item.Recycle();
                        }
                        going.RemoveAt(index);
                        GillaItem it = g.item;
                        waiting.Enqueue((it.label, it.coins /* ? */, it.PaysMe));
                        if (crunch != null && crunch.Length != 0)
                        {
                            Sfx.Play(crunch, at, 0.8f, 0.9f, 1.1f);
                        }
                    }
                }
            } while (index > 0);
        }
        float goal;
        float rate;
        if (going.Count < 1)
        {
            rate = 1.2f;
            goal = (shown >= 1f || shown < 0f) ? 0f : 0.45f;
        }
        else
        {
            rate = 4f;
            goal = 1f;
        }
        work = Mathf.MoveTowards(work, goal, rate * dt);
    }

    private void Count(float dt)
    {
        float s = shown;
        if (s < 0f)
        {
            if (waiting.Count == 0)
            {
                return;
            }
            current = waiting.Dequeue();
            s = 0f;
            paid = false;
            shown = 0f;
            counted = 0;
            dirty = true;
        }
        float coins = current.coins;
        shown = s + dt;
        float duration = Mathf.Min(coins * 0.08f, 1.2f);
        int n = (int)((s + dt - 1f) / duration * coins) + 1;
        int reached = n < 0 ? 0 : Mathf.Min(n, current.coins);
        if (shown >= 1f && counted < reached)
        {
            counted = reached;
            if (tickSound != null)
            {
                AudioClip[] clips = new AudioClip[1];
                clips[0] = tickSound;
                Vector3 at = screen.transform.position;
                float p = counted * 0.6f / Mathf.Max(current.coins, 1);
                Sfx.Play(clips, at, 0.5f, p + 0.8f, p + 0.82f);
            }
        }
        if (!paid && current.coins <= counted && shown >= 1f)
        {
            paid = true;
            if (current.mine)
            {
                Coins.AddBolts(current.coins);
                GillaQuests.Report(GillaQuests.Stat.Recycle, 1);
                GillaQuests.Report(GillaQuests.Stat.RecycleBolts, current.coins);
                if (current.label == "GOLD BAR")
                {
                    GillaQuests.Report(GillaQuests.Stat.RecycleGold, 1);
                }
                if (current.label == "DIAMOND")
                {
                    GillaQuests.Report(GillaQuests.Stat.RecycleDiamond, 1);
                }
            }
            log.Insert(0, (current.label, current.coins));
            if (log.Count > 3)
            {
                log.RemoveAt(3);
            }
            Play(coinSound, 0.8f);
            Play(sparkleSound, 0.55f);
            flash = 1f;
        }
        if (paid)
        {
            float hold = waiting.Count < 1 ? 4f : 0.9f;
            if (duration + 1f + hold <= shown)
            {
                shown = -1f;
                dirty = true;
            }
        }
    }

    private void Spin(float dt)
    {
        turned = (turned + Mathf.Lerp(idleSpeed, workSpeed, Mathf.Clamp01(work)) * dt) % 36000f;
        for (int i = 0; i < spinners.Length && i < axes.Length && i < rates.Length; i++)
        {
            if (spinners[i] != null)
            {
                spinners[i].localRotation = rest[i] * Quaternion.AngleAxis(turned * rates[i], axes[i]);
            }
        }
        if (humming != null)
        {
            humming.pitch = Mathf.Lerp(0.92f, 1.3f, Mathf.Clamp01(work));
        }
    }

    private void Lamps(float dt)
    {
        flash = Mathf.MoveTowards(flash, 0f, dt * 1.5f);
        float pulse = Mathf.Sin(Time.time * 1.6f) * 0.3f + 0.7f;
        float blink = Mathf.PingPong(Time.time * 6f, 1f);
        float level = Mathf.Lerp(pulse, blink * 2.3f + 0.9f, Mathf.Clamp01(work)) + flash * 3f;
        if (body != null && lampSlot >= 0)
        {
            if (lampBlock == null)
            {
                lampBlock = new MaterialPropertyBlock();
            }
            body.GetPropertyBlock(lampBlock, lampSlot);
            lampBlock.SetColor(BaseColor, lampColour * level);
            body.SetPropertyBlock(lampBlock, lampSlot);
        }
        if (beacon == null)
        {
            return;
        }
        beacon.intensity = Mathf.Max(level - 0.8f, 0f) * 0.9f;
        beacon.enabled = beacon.intensity > 0.02f;
    }

    private void Wake(float dt)
    {
        Transform head = Camera.main != null ? Camera.main.transform : null;
        float dist;
        if (head != null && screen != null)
        {
            dist = Vector3.Distance(head.position, screen.transform.position);
        }
        else
        {
            dist = 99f;
        }
        bool was = wanted;
        bool want;
        if (dist < wakeRange)
        {
            want = true;
        }
        else if (dist < wakeRange + 2f && was)
        {
            want = true;
        }
        else if (going.Count >= 1)
        {
            want = true;
        }
        else
        {
            want = shown >= 0f;
        }
        if (was != want)
        {
            wanted = want;
            Play(want ? bootSound : offSound, 0.6f);
        }
        float before = awake;
        awake = Mathf.MoveTowards(awake, wanted ? 1f : 0f, dt / (wanted ? 0.7f : 0.25f));
        if (awake != before)
        {
            dirty = true;
        }
    }

    private void Spark(Vector3 at, float dt)
    {
        if (sparks == null)
        {
            sparks = MakeSparks();
        }
        if (sparks == null)
        {
            return;
        }
        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
        Vector3 f = Front;
        Vector3 side = Vector3.Cross(Vector3.up, f);
        if (UnityEngine.Random.value < dt * 60f)
        {
            int count = UnityEngine.Random.Range(1, 4);
            for (int i = 0; i < count; i++)
            {
                float s = UnityEngine.Random.Range(-0.3f, 0.3f);
                emit.position = at + side * s + Vector3.up * 0.06f;
                float up = UnityEngine.Random.Range(0.8f, 2.4f);
                float out_ = UnityEngine.Random.Range(0.3f, 1.6f);
                float across = UnityEngine.Random.Range(-0.8f, 0.8f);
                emit.velocity = Vector3.up * up + f * out_ + side * across;
                emit.startLifetime = UnityEngine.Random.Range(0.3f, 0.7f);
                emit.startSize = UnityEngine.Random.Range(0.012f, 0.025f);
                sparks.Emit(emit, 1);
            }
        }
    }
}
