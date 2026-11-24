using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Fusion;

public class GillaSonar : MonoBehaviour
{
    private sealed class Copy
    {
        public SkinnedMeshRenderer glow;
        public int wanted = -1;
        public bool screenOnly;
    }

    [Tooltip("Gilla/Sonar: a body's glow through the walls.")]
    public Material ghost;
    public AudioClip ping;
    [Tooltip("Seconds into a hunt between pings (the first one this far in).")]
    public float every = 45f;
    public float lasts = 4f;
    public Color apeColour = new Color(0.35f, 0.95f, 1f, 1f);
    public Color monsterColour = new Color(1f, 0.1f, 0.06f, 1f);
    public static readonly List<(GameObject body, bool monster)> Extra = new List<(GameObject body, bool monster)>();
    private const float JustDied = 3;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static GillaSonar current;
    private readonly Dictionary<SkinnedMeshRenderer, Copy> ghosts = new Dictionary<SkinnedMeshRenderer, Copy>();
    private readonly List<Copy> lit = new List<Copy>();
    private readonly List<SkinnedMeshRenderer> gone = new List<SkinnedMeshRenderer>();
    private readonly List<SkinnedMeshRenderer> parts = new List<SkinnedMeshRenderer>();
    private MaterialPropertyBlock block;
    private AudioClip[] pings;
    private int lastPulse = -1;
    private float nextSweep;

    public static bool On { get; private set; }
    public static float Fade { get; private set; }
    public static bool Pulsing { get; private set; }
    public static int Pings { get; private set; }

    private void OnEnable()
    {

        current = this;
    }

    private void OnDisable()
    {
        if (current == this)
        {
            current = null;
        }
        Pulsing = false;
        On = false;
        Fade = 0f;
        foreach (Copy copy in lit)
        {
            if (copy.glow != null)
            {
                copy.glow.enabled = false;
            }
        }
        lit.Clear();
    }

    public static bool Yours(GillaModeRound round)
    {
        if (round == null) return false;
        if (round.Live && round.Runner /* ? */ != null && !GillaMapVote.OutOfHunt)
        {
            var players = round.Players;
            byte part;
            if (players.TryGet(round.Runner /* ? */.LocalPlayer, out part))
            {
                return (part & 0xfd) == 1;
            }
        }
        return false;
    }

    public static bool Glows(GillaModeRound round, PlayerRef who, out bool hunter)
    {
        hunter = false;
        if (round == null) return false;
        if (round.Live)
        {
            var players = round.Players;
            byte part;
            if (players.TryGet(who, out part) && (part & 0xfd) == 1)
            {
                hunter = part == 3;
                GillaCorpse corpse = GillaCorpse.Find(who.PlayerId);
                if (corpse != null)
                {
                    if (!corpse.OnATimer && (corpse.Has(0) || corpse.Ripped))
                    {
                        return Time.time - corpse.MadeAt > JustDied;
                    }
                }
                return true;
            }
        }
        return false;
    }

    private static bool InIt(byte part)
    {
        return (part & 0xfd) == 1;
    }

    public static void ForScreen(bool drawing)
    {
        if (current == null) return;
        foreach (Copy copy in current.lit)
        {
            if (copy.screenOnly && copy.glow != null)
            {
                copy.glow.forceRenderingOff = !drawing;
            }
        }
    }

    private void Update()
    {
        GillaModeRound round = GillaModeRound.Current;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        float fade = 0f;
        bool mine = false;
        if (round != null && round.Live && round.Phase == 1 && boot != null && boot.Kind == NetworkBootstrap.Lobby.Monster)
        {
            mine = Yours(round);
            float t = GillaModeRound.HuntSeconds - (round.HuntEnds - round.Now);
            int n = (int)Math.Floor(t / every);
            if (n > 0)
            {
                t -= every * n;
                if (t < lasts)
                {
                    fade = Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01((lasts - t) * 2f);
                    if (n != lastPulse)
                    {
                        lastPulse = n;
                        if (mine)
                        {
                            if (ping != null)
                            {
                                Vector3 at = (Camera.main != null ? Camera.main.transform : transform).position;
                                if (pings == null)
                                {
                                    pings = new AudioClip[] { ping };
                                }
                                Sfx.Play(pings, at, 0.9f, 1f, 1f);
                                Pings = Pings + 1;
                            }
                            mine = true;
                        }
                        else
                        {
                            mine = false;
                        }
                    }
                }
            }
        }
        else
        {
            fade = 0f;
            mine = false;
            lastPulse = -1;
        }

        Pulsing = fade > 0f;
        On = mine && Pulsing;
        Fade = On ? fade : 0f;
        bool watch;
        if (!Pulsing || mine)
        {
            watch = false;
        }
        else
        {
            watch = GillaMonsterCam.Watched;
        }
        int frame = Time.frameCount;

        if (On || watch)
        {
            if (ghost != null)
            {
                if (block == null)
                {
                    block = new MaterialPropertyBlock();
                }
                foreach (NetworkApe ape in NetworkApe.All)
                {
                    if (ape == null || ape.IsLocal) continue;
                    bool hunter;
                    if (!Glows(round, ape.Object.InputAuthority /* ? */, out hunter)) continue;
                    if (hunter && !On) continue;
                    GameObject body;
                    if (ape.IsMonster && ape.transform /* ? */ != null)
                    {
                        body = ape.transform.gameObject /* ? */;
                    }
                    else
                    {
                        body = ape.gameObject /* ? */;
                    }
                    Color c = hunter ? monsterColour : apeColour;
                    Glow(body, c, fade, !On, frame);
                }
                foreach ((GameObject body, bool monster) extra in Extra)
                {
                    if (On || !extra.monster)
                    {
                        Color c = extra.monster ? monsterColour : apeColour;
                        Glow(extra.body, c, fade, !On, frame);
                    }
                }
            }
        }

        for (int i = lit.Count - 1; i >= 0; i--)
        {
            Copy copy = lit[i];
            if (copy.glow == null || copy.wanted != frame)
            {
                if (copy.glow != null)
                {
                    copy.glow.enabled = false;
                }
                lit.RemoveAt(i);
            }
        }

        if (Time.unscaledTime < nextSweep) return;
        nextSweep = Time.unscaledTime + 1f;
        gone.Clear();
        foreach (KeyValuePair<SkinnedMeshRenderer, Copy> pair in ghosts)
        {
            if (pair.Key == null || pair.Value.glow == null)
            {
                gone.Add(pair.Key);
            }
        }
        foreach (SkinnedMeshRenderer key in gone)
        {
            ghosts.Remove(key);
        }
    }

    private void Glow(GameObject body, Color c, float fade, bool screenOnly, int frame)
    {
        if (body == null) return;
        body.GetComponentsInChildren<SkinnedMeshRenderer>(false, parts);
        foreach (SkinnedMeshRenderer smr in parts)
        {
            if (smr == null) continue;
            if (!smr.enabled) continue;
            if (smr.sharedMesh == null) continue;
            if (smr.name == "SonarGhost") continue;
            Copy item = Ghost(smr);
            SkinnedMeshRenderer glow = item.glow;
            item.wanted = frame;
            item.screenOnly = screenOnly;
            if (!glow.enabled)
            {
                glow.enabled = true;
                lit.Add(item);
            }
            if (glow.forceRenderingOff != screenOnly)
            {
                glow.forceRenderingOff = screenOnly;
            }
            block.SetColor(ColorId, c);
            block.SetFloat(FadeId, fade);
            glow.SetPropertyBlock(block);
        }
    }

    private Copy Ghost(SkinnedMeshRenderer src)
    {
        Copy found;
        if (ghosts.TryGetValue(src, out found))
        {
            if (found.glow != null)
            {
                return found;
            }
        }
        GameObject go = new GameObject("SonarGhost");
        go.transform.SetParent(src.transform, false);
        go.layer = src.gameObject.layer;
        SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = src.sharedMesh;
        smr.rootBone = src.rootBone;
        smr.bones = src.bones;
        smr.localBounds = src.localBounds;
        smr.updateWhenOffscreen = true;
        smr.allowOcclusionWhenDynamic = false;
        int count = src.sharedMesh.subMeshCount;
        if (count < 2)
        {
            count = 1;
        }
        Material[] mats = new Material[count];
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i] = ghost;
        }
        smr.sharedMaterials = mats;
        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        smr.receiveShadows = false;
        smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        smr.enabled = false;
        Copy copy = new Copy();
        copy.glow = smr;
        ghosts[src] = copy;
        return copy;
    }
}
