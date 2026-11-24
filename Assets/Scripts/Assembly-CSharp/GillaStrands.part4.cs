// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaStrands
{
    private void Snap(Strand st, float dt)
    {
        st.snapped = true;
        int c = st.cut;
        Vector3 vel = ((st.at[c] - st.was[c]) + (st.at[c + 1] - st.was[c + 1])) * (0.5f / dt);
        if (vel.sqrMagnitude > 9f)
        {
            vel = vel.normalized * 3f;
        }
        Vector3 pull = (st.at[c] - st.at[c + 1]) * 0.4f;
        st.was[c] -= pull;
        st.was[c + 1] += pull;
        int count = st.kind == Kind.Thread ? UnityEngine.Random.Range(1, 3) : UnityEngine.Random.Range(2, 5);
        for (int i = count; i > 0; i--)
        {
            Vector3 p0 = st.at[c];
            Vector3 p1 = st.at[c + 1];
            Vector3 at = Vector3.Lerp(p0, p1, UnityEngine.Random.value);
            Vector3 sphere = UnityEngine.Random.insideUnitSphere;
            Vector3 going = vel * 0.6f + sphere * 0.5f * size;
            float big = UnityEngine.Random.Range(0.005f, st.kind == Kind.Thread ? 0.0075f : 0.012f);
            Drip(at, going, big * size);
        }
    }

    private void Drip(Vector3 at, Vector3 going, float big)
    {
        for (int i = 0; i < dropLife.Length; i++)
        {
            if (i >= 14) return;
            if (dropLife[i] > 0f) continue;
            dropAt[i] = at;
            dropGoing[i] = going;
            dropSize[i] = big;
            dropLife[i] = 1.4f;
            RaycastHit hit;
            float floor;
            if (Physics.Raycast(at, Vector3.down, out hit, 4f, -16133, QueryTriggerInteraction.Ignore))
            {
                floor = hit.point.y;
            }
            else
            {
                floor = at.y - 4f;
            }
            dropFloor[i] = floor;
            dropsOut++;
            return;
        }
    }

    private void Falling(float dt)
    {
        if (dropsOut == 0) return;
        Vector3 g = Physics.gravity;
        float k = dt * 1.1f;
        for (int i = 0; i < 14; i++)
        {
            if (dropLife[i] > 0f)
            {
                dropGoing[i] += g * k;
                dropAt[i] += dropGoing[i] * dt;
                dropLife[i] -= dt;
                if (dropAt[i].y < dropFloor[i])
                {
                    dropLife[i] = 0f;
                }
                if (dropLife[i] <= 0f)
                {
                    dropsOut--;
                }
            }
        }
    }

    private void Hang(Strand st, Vector3 A, Vector3 B, float far, float dt)
    {
        Vector3[] at = st.at;
        Vector3[] was = st.was;
        Vector3 accel;
        if (!st.snapped)
        {
            accel = (Physics.gravity * st.heavy + st.drift) * (dt * dt);
        }
        else
        {
            accel = Physics.gravity * (dt * dt) * 0.7f;
        }
        for (int i = 0; i < 9; i++)
        {
            Vector3 p = at[i];
            at[i] = accel + p + (p - was[i]) * 0.94f;
            was[i] = p;
        }
        at[0] = A;
        at[8] = B;
        int cut = st.cut;
        float segA;
        float segB;
        if (!st.snapped)
        {
            far = Mathf.Max(far, size * 0.03f);
            segA = st.slack * far * 0.125f;
            segB = segA;
        }
        else
        {
            segA = st.stub / (float)Mathf.Max(cut, 1);
            segB = st.stub / (float)Mathf.Max(7 - cut, 1);
        }
        for (int iter = 0; iter < 5; iter++)
        {
            for (int i = 0; i < 8; i++)
            {
                if (!st.snapped || cut != i)
                {
                    Vector3 d = at[i + 1] - at[i];
                    float len = d.magnitude;
                    if (len >= 1e-06f)
                    {
                        float rest = i < cut ? segA : segB;
                        Vector3 corr = d * ((len - rest) / len * 0.5f);
                        Vector3 moveA = i == 0 ? Vector3.zero : (i == 7 ? corr + corr : corr);
                        at[i] += moveA;
                        Vector3 moveB = i == 7 ? Vector3.zero : (i == 0 ? corr + corr : corr);
                        at[i + 1] -= moveB;
                    }
                }
            }
        }
        if (st.snapped) return;
        int lowest = 4;
        for (int i = 1; i < 8; i++)
        {
            if (at[i].y < at[lowest].y)
            {
                lowest = i;
            }
        }
        float target = Mathf.Clamp(lowest * 2f, 4f, 12f);
        st.beadAt = Mathf.MoveTowards(st.beadAt, target, dt * 9f);
        st.bead = st.bead + st.beadGrows * dt;
        if (st.bead < 1f) return;
        st.bead = 0.15f;
        int k = Mathf.Clamp(Mathf.RoundToInt(st.beadAt * 0.5f), 1, 7);
        Drip(at[k], Vector3.down * 0.3f, st.beadBig * 0.55f);
    }

    private static void Rounded(Strand st)
    {
        Vector3[] at = st.at;
        for (int i = 0; i < 8; i++)
        {
            ringAt[i * 2] = at[i];
            int lo;
            int hi;
            if (!st.snapped)
            {
                lo = i - 1;
                hi = i + 2;
            }
            else
            {
                int cut = st.cut;
                if (i == cut)
                {
                    ringAt[i * 2 + 1] = at[i + 1];
                    continue;
                }
                if (i < cut)
                {
                    lo = i - 1;
                    hi = Mathf.Min(i + 2, cut);
                }
                else
                {
                    lo = Mathf.Max(i - 1, cut + 1);
                    hi = i + 2;
                }
            }
            lo = Mathf.Max(lo, 0);
            hi = Mathf.Min(hi, 8);
            ringAt[i * 2 + 1] = (at[i] + at[i + 1]) * 0.5625f + (at[lo] + at[hi]) * -0.0625f;
        }
        ringAt[16] = at[8];
    }

    private static Vector2[] Round(int sides)
    {
        Vector2[] arr;
        if (rounds.TryGetValue(sides, out arr))
        {
            return arr;
        }
        arr = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float ang = (i * 3.1415927f + i * 3.1415927f) / sides;
            arr[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }
        rounds[sides] = arr;
        return arr;
    }

    private void Draw()
    {
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        for (int s = 0; s < strands.Length; s++)
        {
            Strand st = strands[s];
            int sides = st.sides;
            int v = st.firstVertex;
            if (st.gone >= 1f)
            {
                if (!st.hidden)
                {
                    st.hidden = true;
                    for (int k = 0; k < sides * 17; k++)
                    {
                        verts[v].at = st.at[0];
                        v++;
                    }
                }
                continue;
            }
            Rounded(st);
            Vector2[] round = Round(sides);
            float length = 0f;
            for (int r = 1; r <= 16; r++)
            {
                length += (ringAt[r] - ringAt[r - 1]).magnitude;
            }
            float thin;
            float wobble;
            float beadSize;
            float flat;
            if (!st.snapped)
            {
                float sz = size;
                thin = Mathf.Clamp(Mathf.Sqrt(sz * 0.1f / length), 0.27f, 1f);
                float stretch = Mathf.Clamp01((length + sz * -0.22f) / (sz * 0.5f));
                flat = thin * (st.kind == Kind.Sheet ? thin : 1f);
                wobble = (st.kind != Kind.Thread ? 0.38f : 0.5f) * stretch;
                beadSize = st.bead * st.beadBig;
            }
            else
            {
                thin = 1f;
                wobble = 0f;
                beadSize = 0f;
                flat = 1f;
            }
            Vector3 side;
            if (anchors[st.onA.b0] != null)
            {
                side = anchorNow[st.onA.b0].MultiplyVector(st.flatWay);
            }
            else
            {
                side = st.flatWay;
            }
            Vector3 tangent = Vector3.up;
            for (int r = 0; r < 17; r++)
            {
                Vector3 p = ringAt[r];
                Vector3 d;
                if (r == 0)
                {
                    d = ringAt[1] - p;
                }
                else if (r == 16)
                {
                    d = p - ringAt[15];
                }
                else
                {
                    d = ringAt[r + 1] - ringAt[r - 1];
                }
                if (d.sqrMagnitude > 1e-10f)
                {
                    tangent = d.normalized;
                }
                side = side - tangent * Vector3.Dot(side, tangent);
                if (side.sqrMagnitude <= 1e-08f)
                {
                    Vector3 other = Mathf.Abs(tangent.y) <= 0.9f ? Vector3.up : Vector3.right;
                    side = Vector3.Cross(tangent, other).normalized;
                }
                else
                {
                    side = side.normalized;
                }
                float u = r / 16f;
                float ends = u + u - 1f;
                ends = ends * ends * Mathf.Abs(ends);
                float w;
                float h;
                float ph;
                if (!st.snapped)
                {
                    ph = st.phase;
                    float wave = Mathf.Sin(ph + u * st.waves * 6.283f + age * 0.8f);
                    float near = r - st.beadAt;
                    float lump = Mathf.Exp(near * near * -0.42f);
                    float e01 = Mathf.Clamp01(ends);
                    float tip = st.kind == Kind.Sheet ? 1.12f : 1.34f;
                    wave = (1f - ends) * wobble * wave + 1f;
                    w = beadSize * lump + wave * st.wide * (flat + (tip - flat) * e01);
                    h = beadSize * lump + wave * st.deep * (thin + (tip - thin) * e01);
                }
                else
                {
                    int c2 = st.cut * 2;
                    int c3 = st.cut * 2 + 1;
                    int num;
                    int den;
                    if (r < c2)
                    {
                        den = c2;
                        num = c2 - r;
                    }
                    else
                    {
                        den = Mathf.Max(16 - c3, 1);
                        num = r - c3;
                    }
                    float t = num / (float)den;
                    float k = Mathf.Min(t * t, 1f) * 0.68f + 0.32f;
                    float alive = (r != c2 && r != c3) ? 1f - st.gone : 0f;
                    ph = st.phase;
                    w = alive * k * st.wide * (Mathf.Clamp01(t) * 0.6f + 0.4f);
                    h = Mathf.Min(alive * k * st.deep, w);
                }
                float shade = (1f - thin) * (1f - ends);
                float tint = Mathf.Clamp01(shade * 0.95f + 0.08f + st.tone * 0.22f + Mathf.Sin(r * 0.9f + ph) * 0.06f);
                Color32 dark = Dark;
                Color32 bright = Bright;
                Color32 col = new Color32(
                    (byte)(int)(tint * (bright.r - dark.r) + dark.r),
                    (byte)(int)(tint * (bright.g - dark.g) + dark.g),
                    (byte)(int)(tint * (bright.b - dark.b) + dark.b),
                    (byte)(int)(shade * 255f));
                Vector3 bin = Vector3.Cross(tangent, side);
                for (int k = 0; k < sides; k++)
                {
                    Vector2 rd = round[k];
                    Vector3 n = side * (h * rd.x) + bin * (w * rd.y);
                    verts[v].at = p + side * (w * rd.x) + bin * (h * rd.y);
                    Vector3 normal = side;
                    if (n.sqrMagnitude > 1e-12f)
                    {
                        normal = n.normalized;
                    }
                    verts[v].normal = normal;
                    verts[v].colour = col;
                    v++;
                }
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }
        for (int i = 0; i < 14; i++)
        {
            float life = dropLife[i];
            if (life > 0f || dropShown[i])
            {
                dropShown[i] = life > 0f;
                int baseV = dropFirst + i * DropRound.Length;
                Vector3 p = dropAt[i];
                Vector3 g = dropGoing[i];
                float rad = 0f;
                if (life > 0f)
                {
                    rad = dropSize[i] * Mathf.Min(dropLife[i] * 3f, 1f);
                }
                float speed = g.magnitude;
                Vector3 dir = speed <= 0.01f ? Vector3.down : g / speed;
                Color32 dark = Dark;
                Color32 bright = Bright;
                float tint = Mathf.Clamp01((i * 0.37f) % 0.7f);
                float stretch = Mathf.Min(speed * 0.55f, 1.6f);
                for (int k = 0; k < DropRound.Length; k++)
                {
                    Vector3 o = DropRound[k] * rad;
                    int idx = baseV + k;
                    verts[idx].at = p + o + dir * (stretch * Vector3.Dot(dir, o));
                    verts[idx].normal = DropRound[k];
                    verts[idx].colour = new Color32(
                        (byte)(int)(tint * (bright.r - dark.r) + dark.r),
                        (byte)(int)(tint * (bright.g - dark.g) + dark.g),
                        (byte)(int)(tint * (bright.b - dark.b) + dark.b),
                        0);
                }
                if (life > 0f)
                {
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }
        }
        if (max.x < min.x)
        {
            Vector3 pos = transform.position;
            min = pos;
            max = pos;
        }
        mesh.SetVertexBufferData(verts, 0, 0, verts.Length, 0, (MeshUpdateFlags)9);
        Bounds b = new Bounds();
        Vector3 ext = (max - min) * 0.5f;
        b.extents = ext;
        b.center = min + ext;
        b.Expand(size * 0.2f);
        mesh.bounds = b;
    }

    public GillaStrands()
    {
        rimA = new List<Vector3>();
        anchors = new List<Transform>();
        dropAt = new Vector3[14];
        dropGoing = new Vector3[14];
        dropSize = new float[14];
        dropLife = new float[14];
        dropFloor = new float[14];
        dropShown = new bool[14];
    }

    static GillaStrands()
    {
        Dark = new Color32(78, 5, 7, 0);
        On = false;
        Bright = new Color32(156, 24, 19, 0);
        live = new List<GillaStrands>();
        rounds = new Dictionary<int, Vector2[]>();
        weightsOf = new Dictionary<Mesh, BoneWeight[]>();
        readAt = new List<Vector3>();
        readFaces = new List<Vector3>();
        readDown = new List<Vector2>();
        readTris = new List<int>();
        ringAt = new Vector3[17];
        readWorld = new Vector3[4096];
        DropRound = new Vector3[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        DropTris = new int[] { 2, 4, 0, 2, 0, 5, 2, 5, 1, 2, 1, 4, 3, 0, 4, 3, 5, 0, 3, 1, 5, 3, 4, 1 } /* ? */;
    }

    private static long Places_g__Key_59_0(int v0, int v1)
    {
        if (v1 <= v0)
        {
            return ((long)v1 << 32) | (uint)v0;
        }
        return ((long)v0 << 32) | (uint)v1;
    }
}
