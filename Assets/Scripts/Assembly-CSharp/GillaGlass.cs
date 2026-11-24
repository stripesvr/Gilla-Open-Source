// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
public partial class GillaGlass : MonoBehaviour
{
    private class Shard
    {
        public Transform t;
        public Mesh mesh;
        public MeshRenderer r;
        public Rigidbody body;
        public BoxCollider box;

        public Shard()
        {
        }
    }

    private struct Track
    {
        public Vector3 at;
        public Vector3 v;
        public float clear;
    }

    [Tooltip("How fast (m/s) you yourself, your body and not just a hand, have to be going into the glass to break it: a run, not a tap (whichever of you meets it first, head, body or a hand).")]
    public float runSpeed = 2.5f;
    [Tooltip("Seconds until a broken pane is back.")]
    public float regrow = 10f;
    [Tooltip("How many shards burst from where it was hit (two for each spoke of the crack, so an even number).")]
    public int shardCount = 10;
    [Tooltip("How far out from the hit the shards reach, as a share of the way to the pane's edge.")]
    [Range(0.1f, 1f)]
    public float shardReach = 0.3f;
    [Tooltip("How see-through the shards are next to the pane: 0.35 is about a third as solid as the glass they came from.")]
    [Range(0f, 2f)]
    public float shardOpacity = 0.35f;
    [Tooltip("How much of your speed you keep, crashing through it.")]
    [Range(0.3f, 1f)]
    public float slowTo = 0.75f;
    [Tooltip("Played where it breaks (one of them, at a random pitch).")]
    public AudioClip[] breakSounds;
    private const float ShardLife = 3.5f;
    private const float ShardFade = 0.5f;
    private const float BreakFade = 0.3f;
    private const float FadeSeconds = 0.4f;
    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private const int ShardLayer = 2;
    private const float Teleport = 25f;
    private const float Lift = 0.003f;
    private static readonly Dictionary<int, GillaGlass> panes = new Dictionary<int, GillaGlass>();
    private static readonly Dictionary<ValueTuple<Material, float>, Material> shardMats = new Dictionary<ValueTuple<Material, float>, Material>();
    private static int notShards = -1;
    private BoxCollider solid;
    private MeshRenderer glass;
    private MaterialPropertyBlock block;
    private int id;
    private bool broken;
    private bool tracking;
    private float brokenAt;
    private float fade = -1f;
    private Track head;
    private Track body;
    private Track handL;
    private Track handR;
    private Shard[] shards;
    private const float Smooth = 0.05f;
    private const float ClearGap = 0.06f;
    private const float ClearFor = 0.3f;

    public string LastBreak { get; private set; } = "";

    public bool IsBroken
    {
        get { return broken; }
    }

    public int ShardsOut
    {
        get
        {
            int count = 0;
            if (shards != null)
            {
                for (int i = 0; i < shards.Length; i++)
                {
                    if (shards[i].t.gameObject.activeSelf) count++;
                }
            }
            return count;
        }
    }

    private void Awake()
    {

        solid = GetComponent<BoxCollider>();
        glass = GetComponent<MeshRenderer>();
        if (notShards >= 0) return;
        notShards = LayerMask.GetMask(new string[] { "PlayerRig", "FirstPerson", "MirrorOnly", "Buttons", "Apes" }) | 4;
    }

    private void OnEnable()
    {

        id = StableId();
        panes[id] = this;
    }

    private void OnDisable()
    {
        GillaGlass found;
        if (panes.TryGetValue(id, out found))
        {
            if (found == this)
            {
                panes.Remove(id);
            }
        }
    }

    private void OnDestroy()
    {
        DropShards();
    }

    private void DropShards()
    {
        if (shards == null) return;
        for (int i = 0; i < shards.Length; i++)
        {
            Shard s = shards[i];
            if (s.mesh != null) Destroy(s.mesh);
            if (s.t != null) Destroy(s.t.gameObject);
        }
        shards = null;
    }

    private int StableId()
    {
        StringBuilder sb = new StringBuilder();
        Transform tr = transform;
        while (tr != null)
        {
            sb.Insert(0, "/" + tr.name);
            tr = tr.parent;
        }
        Vector3 p = transform.position;
        sb.Append('@').Append(Mathf.RoundToInt(p.x * 100f)).Append(',').Append(Mathf.RoundToInt(p.y * 100f)).Append(',').Append(Mathf.RoundToInt(p.z * 100f));
        string s = sb.ToString();
        uint h = 0x811c9dc5;
        for (int i = 0; i < s.Length; i++)
        {
            h = unchecked((h ^ s[i]) * 0x1000193);
        }
        return unchecked((int)h);
    }

    private void Update()
    {
        if (broken)
        {
            WhileBroken();
            return;
        }
        if (fade >= 0f)
        {
            FadeIn();
        }
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player == null || player.headCollider == null || player.bodyCollider == null)
        {
            tracking = false;
            return;
        }
        float dt = Time.deltaTime;
        CapsuleCollider bodyCol = player.bodyCollider;
        Vector3 headPos = player.headCollider.transform.position;
        Vector3 bodyPos = bodyCol.transform.TransformPoint(bodyCol.center);
        Vector3 left = Hand(player.leftHandTransform, player.leftHandOffset);
        Vector3 right = Hand(player.rightHandTransform, player.rightHandOffset);
        float now = Time.time;
        if (!tracking || dt <= 0f)
        {
            head.v = Vector3.zero;
            head.at = headPos;
            head.clear = now;
            body.v = Vector3.zero;
            body.at = bodyPos;
            body.clear = now;
            handL.at = left;
            handL.v = Vector3.zero;
            handL.clear = now;
            handR.at = right;
            handR.v = Vector3.zero;
            handR.clear = now;
            tracking = true;
            return;
        }
        float k = 1f - Mathf.Exp(dt / -0.05f);
        Follow(ref head, headPos, dt, k);
        Follow(ref body, bodyPos, dt, k);
        Follow(ref handL, left, dt, k);
        Follow(ref handR, right, dt, k);
        float ahead = dt + dt + Time.fixedDeltaTime;
        float handR0 = player.minimumRaycastDistance;
        float headR = player.headCollider.radius * Biggest(player.headCollider.transform);
        Vector3 atHead, atBody, atL, atR;
        bool hitHead = Meets(ref head, headR, ahead, now, out atHead);
        float bodyR = bodyCol.radius * Biggest(bodyCol.transform);
        bool hitBody = Meets(ref body, bodyR, ahead, now, out atBody);
        bool hitL = Meets(ref handL, handR0, ahead, now, out atL);
        bool hitR = Meets(ref handR, handR0, ahead, now, out atR);
        float speed = Into(body);
        if (speed < runSpeed) return;
        if (hitHead)
        {
            BreakByYou(atHead, body.v, true, true, string.Format("your head, you going {0:F1} m/s into it", speed));
        }
        else if (hitBody)
        {
            BreakByYou(atBody, body.v, true, true, string.Format("your body, going {0:F1} m/s into it", speed));
        }
        else if (hitL)
        {
            BreakByYou(atL, body.v, true, false, string.Format("your left hand, you going {0:F1} m/s into it", speed));
        }
        else if (hitR)
        {
            BreakByYou(atR, body.v, false, true, string.Format("your right hand, you going {0:F1} m/s into it", speed));
        }
    }

    private static void Follow(ref Track t, Vector3 now, float dt, float k)
    {
        Vector3 prev = t.at;
        t.at = now;
        Vector3 vel = (now - prev) / dt;
        if (vel.sqrMagnitude <= 625f)
        {
            t.v = Vector3.Lerp(t.v, vel, k);
        }
        else
        {
            t.v = Vector3.zero;
        }
    }

    private static Vector3 Hand(Transform controller, Vector3 offset)
    {
        if (controller != null)
        {
            return controller.position + controller.rotation * offset;
        }
        return Vector3.one * 1e6f;
    }

    private static float Biggest(Transform t)
    {
        Vector3 s = t.lossyScale;
        return Mathf.Max(new float[] { Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z) });
    }

    private void Frame(out Vector3 c, out Vector3 u, out Vector3 w, out Vector3 n, out float hw, out float hh, out float ht)
    {
        Transform tr = transform;
        Vector3 scale = tr.lossyScale;
        c = tr.TransformPoint(solid.center);
        u = tr.right;
        w = tr.up;
        n = tr.forward;
        hw = Mathf.Abs(scale.x * solid.size.x) * 0.5f;
        hh = Mathf.Abs(scale.y * solid.size.y) * 0.5f;
        ht = Mathf.Abs(scale.z * solid.size.z) * 0.5f;
    }

    private bool Meets(ref Track t, float r, float ahead, float now, out Vector3 at)
    {
        at = Vector3.zero;
        Vector3 c, u, w, n;
        float hw, hh, ht;
        Frame(out c, out u, out w, out n, out hw, out hh, out ht);
        Vector3 d = t.at - c;
        float x = Vector3.Dot(d, u);
        if (Mathf.Abs(x) <= r * 0.5f + hw)
        {
            float y = Vector3.Dot(d, w);
            if (Mathf.Abs(y) <= r * 0.5f + hh)
            {
                float z = Vector3.Dot(d, n);
                float gap = Mathf.Abs(z) - ht - r;
                float clear;
                if (gap <= 0.06f)
                {
                    clear = t.clear;
                }
                else
                {
                    t.clear = now;
                    clear = now;
                }
                if (now - clear <= 0.3f)
                {
                    float side = z >= 0f ? 1f : -1f;
                    float closing = Mathf.Max(-(side * Vector3.Dot(n, t.v)), 0f);
                    if (gap <= closing * ahead + 0.01f)
                    {
                        float cx = Mathf.Clamp(x, -hw, hw);
                        float cy = Mathf.Clamp(y, -hh, hh);
                        at = c + u * cx + w * cy;
                        return true;
                    }
                }
                return false;
            }
        }
        t.clear = now;
        return false;
    }

    private float Into(Track t)
    {
        Vector3 fwd = transform.forward;
        Vector3 c = transform.TransformPoint(solid.center);
        float side = Vector3.Dot(fwd, t.at - c) >= 0f ? 1f : -1f;
        return -(side * Vector3.Dot(fwd, t.v));
    }

    public void Click(Vector3 at, Vector3 push)
    {
        if (!broken)
        {
            LastBreak = "a click";
            Break(at, push);
            NetworkApe local = NetworkApe.Local;
            if (local != null)
            {
                if (local.Object != null)
                {
                    if (local.Object.Runner)
                    {
                        if (local.Object.Runner.Exists(local.Object) /* ? */)
                        {
                            local.RPC_BreakGlass(id, at, push);
                        }
                    }
                }
            }
        }
    }

    public void Struck(Vector3 at, Vector3 push)
    {
        if (broken) return;
        LastBreak = "a hit";
        Break(at, push);
        GillaQuests.Report(GillaQuests.Stat.Glass, 1);
        NetworkApe local = NetworkApe.Local;
        if (local == null) return;
        if (local.Object == null) return;
        if (!local.Object.Runner) return;
        if (!local.Object.Runner.Exists(local.Object) /* ? */) return;
        local.RPC_BreakGlass(id, at, push);
    }

    private void BreakByYou(Vector3 at, Vector3 push, bool left, bool right, string how)
    {
        LastBreak = how;
        Break(at, push);
        GillaQuests.Report(GillaQuests.Stat.Glass, 1);
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player != null)
        {
            player.Slow(slowTo);
        }
        if (left) Buzz(XRNode.LeftHand);
        if (right) Buzz(XRNode.RightHand);
        NetworkApe local = NetworkApe.Local;
        if (local == null) return;
        if (local.Object == null) return;
        if (!local.Object.Runner) return;
        if (!local.Object.Runner.Exists(local.Object) /* ? */) return;
        local.RPC_BreakGlass(id, at, push);
    }

    private static void Buzz(XRNode node)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            device.SendHapticImpulse(0, 0.9f, 0.15f);
        }
    }

    public static void BrokenByOther(int pane, Vector3 at, Vector3 push)
    {
        GillaGlass g;
        if (panes.TryGetValue(pane, out g))
        {
            g.Break(at, push);
        }
    }

    public void Break(Vector3 at, Vector3 push)
    {
        if (broken) return;
        broken = true;
        brokenAt = Time.time;
        solid.enabled = false;
        glass.enabled = true;
        fade = -1f;
        SetFade(glass, 1f);
        Shatter(at, push);
        Sfx.Play(breakSounds, at, 1f, 0.9f, 1.1f);
    }

    private void WhileBroken()
    {
        float since = Time.time - brokenAt;
        if (glass.enabled)
        {
            float f = since / -0.3f + 1f;
            if (f <= 0f)
            {
                glass.enabled = false;
                glass.SetPropertyBlock(null);
            }
            else
            {
                SetFade(glass, f);
            }
        }
        if (shards != null)
        {
            float shardF = 1f - ((since - 3.5f) + (since - 3.5f));
            for (int i = 0; i < shards.Length; i++)
            {
                Shard s = shards[i];
                if (!s.t.gameObject.activeSelf) continue;
                if (shardF > 0f)
                {
                    if (s.t.position.y >= transform.position.y - 40f)
                    {
                        if (shardF < 1f)
                        {
                            SetFade(s.r, shardF);
                        }
                        continue;
                    }
                }
                s.t.gameObject.SetActive(false);
            }
        }
        if (since < regrow || YouAreInIt()) return;
        broken = false;
        solid.enabled = true;
        glass.enabled = true;
        fade = 0f;
        SetFade(glass, 0f);
        if (shards == null) return;
        for (int i = 0; i < shards.Length; i++)
        {
            shards[i].t.gameObject.SetActive(false);
        }
    }

    private void SetFade(Renderer r, float f)
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.Clear();
        block.SetFloat(FadeId, f);
        r.SetPropertyBlock(block);
    }

    private bool YouAreInIt()
    {
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player == null) return false;
        if (player.headCollider == null) return false;
        if (player.bodyCollider == null) return false;
        CapsuleCollider col = player.bodyCollider;
        float seg = Mathf.Max(col.height * 0.5f - col.radius, 0f);
        float segScale = Biggest(col.transform);
        float radius = col.radius * Biggest(col.transform);
        Vector3 center = col.transform.TransformPoint(col.center);
        Vector3 up = col.transform.up;
        Vector3 headPos = player.headCollider.transform.position;
        float headR = player.headCollider.radius * Biggest(player.headCollider.transform);
        if (Inside(headPos, headR)) return true;
        if (Inside(center, radius)) return true;
        Vector3 offset = up * (seg * segScale);
        if (Inside(center + offset, radius)) return true;
        return Inside(center - offset, radius) /* ? */;
    }

    private bool Inside(Vector3 p, float r)
    {
        Vector3 c, u, w, n;
        float hw, hh, ht;
        Frame(out c, out u, out w, out n, out hw, out hh, out ht);
        Vector3 d = p - c;
        bool result = false;
        if (Mathf.Abs(Vector3.Dot(d, n)) < ht + r)
        {
            if (hw + r <= Mathf.Abs(Vector3.Dot(d, u)))
            {
                result = false;
            }
            else
            {
                result = Mathf.Abs(Vector3.Dot(d, w)) < hh + r;
            }
        }
        return result;
    }

    private void FadeIn()
    {
        float f = fade + Time.deltaTime / 0.4f;
        fade = f;
        if (f >= 1f)
        {
            fade = -1f;
            glass.SetPropertyBlock(null);
            return;
        }
        SetFade(glass, f);
    }

    private void Shatter(Vector3 at, Vector3 push)
    {
        Vector3 c, u, w, n;
        float hw, hh, ht;
        Frame(out c, out u, out w, out n, out hw, out hh, out ht);
        Vector3 scale = transform.lossyScale;
        Vector3 d = at - c;
        float px = Mathf.Clamp(Vector3.Dot(d, u), hw * -0.8f, hw * 0.8f);
        float py = Mathf.Clamp(Vector3.Dot(d, w), hh * -0.8f, hh * 0.8f);
        Vector2 hit = new Vector2(px, py);
        float depth = Mathf.Abs(scale.z) * 0.5f;
        int spokes = shardCount / 2;
        if (spokes < 4) spokes = 3;
        float turn = Random.value;
        Vector2[] inner = new Vector2[spokes];
        Vector2[] outer = new Vector2[spokes];
        for (int i = 0; i < spokes; i++)
        {
            float a = (Random.Range(-0.3f, 0.3f) + i) * Mathf.PI;
            float angle = turn * Mathf.PI + turn * Mathf.PI + (a + a) / spokes;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float reach = ToEdge(hit, dir, hw, hh) * shardReach;
            Vector2 ray = dir * reach;
            inner[i] = hit + ray * Random.Range(0.35f, 0.55f);
            outer[i] = hit + ray * Random.Range(0.85f, 1.15f);
        }
        EnsureShards(spokes * 2);
        Vector3 dirPush = push;
        if (push.magnitude <= 0.01f)
        {
            dirPush = -n;
        }
        Vector3 side = Vector3.Dot(dirPush, n) >= 0f ? n : -n;
        for (int i = 0; i < spokes; i++)
        {
            int next = (i == spokes - 1) ? 0 : i + 1;
            Piece(shards[i * 2], new Vector2[] { hit, inner[i], inner[next] }, c, u, w, depth /* ? */, hit, push, side /* ? */);
            Piece(shards[i * 2 + 1], new Vector2[] { inner[i], outer[i], outer[next], inner[next] }, c, u, w, depth /* ? */, hit, push, side /* ? */);
        }
    }

    private static float ToEdge(Vector2 p, Vector2 d, float hw, float hh)
    {
        float best;
        if (Mathf.Abs(d.x) <= 1e-05f)
        {
            best = float.MaxValue;
        }
        else
        {
            if (d.x <= 0f) hw = -hw;
            best = Mathf.Min((hw - p.x) / d.x, float.MaxValue);
        }
        if (Mathf.Abs(d.y) > 1e-05f)
        {
            if (d.y <= 0f) hh = -hh;
            best = Mathf.Min((hh - p.y) / d.y, best);
        }
        return Mathf.Max(best, 0f);
    }
}
