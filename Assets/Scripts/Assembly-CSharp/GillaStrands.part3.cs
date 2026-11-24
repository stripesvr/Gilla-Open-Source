// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaStrands : MonoBehaviour
{
    private void LateUpdate()
    {
        Step(Time.deltaTime);
    }

    private void Fetch()
    {
        for (int i = 0; i < anchorNow.Length; i++)
        {
            if (anchors[i] != null)
            {
                anchorNow[i] = anchors[i].localToWorldMatrix;
            }
        }
    }

    private Vector3 Where(ref End e)
    {
        Vector3 p = anchorNow[e.b0].MultiplyPoint3x4(e.p0);
        if (e.b1 < 0)
        {
            if (e.b2 < 0)
            {
                return p;
            }
            p = p * e.w0;
        }
        else
        {
            p = p * e.w0 + anchorNow[e.b1].MultiplyPoint3x4(e.p1) * e.w1;
        }
        if (e.b2 >= 0)
        {
            p = p + anchorNow[e.b2].MultiplyPoint3x4(e.p2) * e.w2;
        }
        return p;
    }

    private void Step(float dt)
    {
        dt = Mathf.Clamp(dt, 0.0001f, 0.033333335f);
        age += dt;
        bool holding;
        float heldNow;
        if (pulled == null || pulled.isKinematic)
        {
            holding = true;
            held += dt;
            heldNow = held;
        }
        else
        {
            heldNow = held;
            holding = false;
        }
        float over = heldNow <= 14f ? 0f : 1f;
        Fetch();
        Vector3 sumA = Vector3.zero;
        Vector3 sumB = Vector3.zero;
        left = 0;
        bool alive = dropsOut > 0;
        float fade = dt / 0.6f;
        float t = (age - heldNow) + heldNow * over;
        for (int i = 0; i < strands.Length; i++)
        {
            Strand s = strands[i];
            if (s.gone < 1f)
            {
                Vector3 pa = a != null ? Where(ref s.onA) : s.at[0];
                Vector3 pb = b != null ? Where(ref s.onB) : s.at[8];
                float dist = Vector3.Distance(pa, pb);
                if (!s.snapped)
                {
                    sumA += pa;
                    sumB += pb;
                    left++;
                    if (!(dist <= s.goesAt && t <= s.goesAfter && a != null && b != null))
                    {
                        Snap(s, dt);
                    }
                }
                Hang(s, pa, pb, dist, dt);
                if (s.snapped)
                {
                    s.hangsFor -= dt;
                    if (s.hangsFor <= 0f)
                    {
                        s.gone = Mathf.Min(s.gone + fade, 1f);
                    }
                }
                alive = true;
            }
        }
        if (left < 1)
        {
            holding = true;
        }
        if (!holding && a != null && b != null)
        {
            float n = left;
            HoldBack((sumA - sumB) / n, n / strands.Length, dt);
        }
        Falling(dt);
        if (!alive)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            return;
        }

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        for (int i = 0; i < strands.Length; i++)
        {
            Strand s = strands[i];
            int sides = s.sides;
            int v = s.firstVertex;
            if (s.gone >= 1f)
            {
                if (!s.hidden)
                {
                    s.hidden = true;
                    for (int k = 0; k < sides * Rings; k++)
                    {
                        verts[v].at = s.at[0];
                        v++;
                    }
                }
                continue;
            }

            Rounded(s);
            Vector2[] round = Round(sides);
            float length = 0f;
            for (int k = 0; k < Rings - 1; k++)
            {
                length += Vector3.Distance(ringAt[k + 1], ringAt[k]);
            }

            float thin;
            float sway;
            float bulge;
            float widthScale;
            if (!s.snapped)
            {
                thin = Mathf.Clamp(Mathf.Sqrt(size * 0.1f / length), 0.27f, 1f);
                float stretch = Mathf.Clamp01((length - size * 0.22f) / (size * 0.5f));
                widthScale = thin * (s.kind == Kind.Sheet ? thin : 1f);
                sway = (s.kind == Kind.Thread ? 0.5f : 0.38f) * stretch;
                bulge = s.bead * s.beadBig;
            }
            else
            {
                thin = 1f;
                sway = 0f;
                bulge = 0f;
                widthScale = 1f;
            }

            Vector3 flat = anchors[s.onA.b0] != null ? anchorNow[s.onA.b0].MultiplyVector(s.flatWay) : s.flatWay;
            Vector3 tangent = Vector3.up;
            Color32 dark = Dark;
            Color32 bright = Bright;
            for (int r = 0; r < Rings; r++)
            {
                Vector3 p = ringAt[r];
                Vector3 d;
                if (r == 0) d = ringAt[1] - p;
                else if (r == 16) d = p - ringAt[15];
                else d = ringAt[r + 1] - ringAt[r - 1];
                if (d.sqrMagnitude > 1e-10f)
                {
                    tangent = d.normalized;
                }
                flat = flat - tangent * Vector3.Dot(flat, tangent);
                if (flat.sqrMagnitude <= 1e-8f)
                {
                    flat = Vector3.Cross(tangent, Mathf.Abs(tangent.y) <= 0.9f ? Vector3.up : Vector3.right).normalized;
                }
                else
                {
                    flat = flat.normalized;
                }

                float u = r / 16f;
                float e = u + u - 1f;
                e = e * e * Mathf.Abs(e);
                float w;
                float h;
                if (!s.snapped)
                {
                    float wave = Mathf.Sin(s.phase + u * s.waves * 6.283f + age * 0.8f);
                    float g = (r - s.beadAt);
                    g = Mathf.Exp(g * g * -0.42f);
                    float c = Mathf.Clamp01(e);
                    float endScale = s.kind == Kind.Sheet ? 1.12f : 1.34f;
                    wave = (1f - e) * sway * wave + 1f;
                    w = bulge * g + wave * s.wide * (widthScale + (endScale - widthScale) * c);
                    h = bulge * g + wave * s.deep * (thin + (endScale - thin) * c);
                }
                else
                {
                    int c2 = s.cut * 2;
                    int c21 = (s.cut << 1) | 1;
                    int num;
                    int den;
                    if (r < c2)
                    {
                        den = c2;
                        num = c2 - r;
                    }
                    else
                    {
                        den = 16 - c21;
                        num = r - c21;
                        if (den < 2) den = 1;
                    }
                    float f = num / (float)den;
                    float k = Mathf.Min(f * f, 1f) * 0.68f + 0.32f;
                    float liveness = 0f;
                    if (r != c2 && r != c21)
                    {
                        liveness = 1f - s.gone;
                    }
                    w = liveness * k * s.wide * (Mathf.Clamp01(f) * 0.6f + 0.4f);
                    h = Mathf.Min(liveness * k * s.deep, w);
                }

                float shade = (1f - thin) * (1f - e);
                float tint = Mathf.Clamp01(shade * 0.95f + 0.08f + s.tone * 0.22f + Mathf.Sin(r * 0.9f + s.phase) * 0.06f);
                tint = Mathf.Clamp01(tint);
                Color32 col = new Color32(
                    (byte)(int)(tint * (bright.r - dark.r) + dark.r),
                    (byte)(int)(tint * (bright.g - dark.g) + dark.g),
                    (byte)(int)(tint * (bright.b - dark.b) + dark.b),
                    (byte)(int)(shade * 255f));
                if (sides > 0)
                {
                    Vector3 side = Vector3.Cross(flat, tangent);
                    for (int k = 0; k < sides; k++)
                    {
                        Vector2 rp = round[k];
                        int idx = v + k;
                        verts[idx].at = p + flat * (w * rp.x) + side * (h * rp.y);
                        Vector3 n = flat * (h * rp.x) + side * (w * rp.y);
                        Vector3 normal = flat;
                        if (n.sqrMagnitude > 1e-12f)
                        {
                            normal = n.normalized;
                        }
                        verts[idx].normal = normal;
                        verts[idx].colour = col;
                    }
                    v += sides;
                }
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        for (int d = 0; d < MostDrops; d++)
        {
            float life = dropLife[d];
            if (life > 0f || dropShown[d])
            {
                int first = dropFirst;
                dropShown[d] = life > 0f;
                int n = DropRound.Length;
                Vector3 at = dropAt[d];
                Vector3 going = dropGoing[d];
                float radius = 0f;
                if (life > 0f)
                {
                    radius = dropSize[d] * Mathf.Min(dropLife[d] * 3f, 1f);
                }
                float speed = going.magnitude;
                Vector3 dir = speed <= 0.01f ? Vector3.down : going / speed;
                Color32 dark = Dark;
                Color32 bright = Bright;
                float tint = Mathf.Clamp01((d * 0.37f) % 0.7f);
                float stretch = Mathf.Min(speed * 0.55f, 1.6f);
                Color32 col = new Color32(
                    (byte)(int)(tint * (bright.r - dark.r) + dark.r),
                    (byte)(int)(tint * (bright.g - dark.g) + dark.g),
                    (byte)(int)(tint * (bright.b - dark.b) + dark.b),
                    0);
                for (int j = 0; j < DropRound.Length; j++)
                {
                    int idx = first + d * n + j;
                    Vector3 o = DropRound[j] * radius;
                    verts[idx].at = at + o + dir * (stretch * Vector3.Dot(dir, o));
                    verts[idx].normal = DropRound[j];
                    verts[idx].colour = col;
                }
                if (life > 0f)
                {
                    min = Vector3.Min(min, at);
                    max = Vector3.Max(max, at);
                }
            }
        }

        if (max.x < min.x)
        {
            Vector3 pos = transform.position;
            min = pos;
            max = pos;
        }
        if (verts != null && mesh != null)
        {
            mesh.SetVertexBufferData(verts, 0, 0, verts.Length, 0, (MeshUpdateFlags)9);
            Bounds bounds = new Bounds();
            bounds.extents = (max - min) * 0.5f;
            bounds.center = min + bounds.extents;
            bounds.Expand(size * 0.2f);
            mesh.bounds = bounds;
        }
    }

    private void HoldBack(Vector3 toA, float share, float dt)
    {
        float m = toA.magnitude;
        if (m < size * 0.05f)
        {
            return;
        }
        float f = Mathf.Min(m / size * 34f, 26f);
        pulled.AddForce(toA / m * f * share, ForceMode.Acceleration);
        pulled.linearVelocity = pulled.linearVelocity /* ? */;
    }
}
