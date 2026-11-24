// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class GillaPull : MonoBehaviour
{
    private static void Unstretch(Limb l)
    {
        if (l.spine != null)
        {
            l.spine.localScale = l.spineAt;
        }
        foreach (var (bone, scale) in l.onSpine)
        {
            if (bone != null)
            {
                bone.localScale = scale;
            }
        }
    }

    private void LateUpdate()
    {
        Watch();
        Tick(Time.deltaTime);
    }

    private static void Tick(float dt)
    {
        for (int i = pulls.Count - 1; i >= 0; i--)
        {
            Pull p = pulls[i];
            if (Step(p, dt))
            {
                foreach (Limb l in p.limbs)
                {
                    Pose(p, l);
                }
            }
            else
            {
                Finish(p);
                pulls.RemoveAt(i);
                if (p == mine)
                {
                    mine = null;
                }
            }
        }
    }

    private static bool Step(Pull p, float dt)
    {
        if (p.snapped)
        {
            float v = p.v + (p.x * -900f + p.v * -18f) * dt;
            float x = p.x + v * dt;
            p.x = x;
            p.v = v;
            if (Mathf.Abs(x) <= 0.01f)
            {
                return Mathf.Abs(v) > 0.2f;
            }
            return true;
        }

        if (p.pulling == null || p.holding == null || p.grip == null)
        {
            return false;
        }

        if (p.live)
        {
            if (p.by != null)
            {
                if (!Still(p))
                {
                    p.live = false;
                }
            }
            else if (mine != p || p.said < Time.frameCount)
            {
                p.live = false;
            }
        }

        if (!p.live)
        {
            p.strain = Mathf.MoveTowards(p.strain, 0f, dt * 4f);
            p.blend = Mathf.MoveTowards(p.blend, 0f, dt * 4f);
            if (p.blend <= 0f && p.strain <= 0f)
            {
                return false;
            }
        }
        else
        {
            Vector3 d = p.pulling.position - p.holding.position;
            float target = Mathf.Clamp01((d.magnitude - p.start) / p.tears);
            float k = Mathf.Clamp01(1f - Mathf.Exp(dt * -18f));
            p.strain = p.strain + (target - p.strain) * k;
            p.blend = Mathf.MoveTowards(p.blend, 1f, dt / 0.12f);
            if (d.sqrMagnitude > 1e-06f)
            {
                p.way = d.normalized;
            }
        }

        p.x = Mathf.SmoothStep(0f, 1f, p.strain);
        return true;
    }

    private static void Pose(Pull p, Limb l)
    {
        if (l.spine != null)
        {
            PoseMiddle(p, l);
            return;
        }
        if (l.drawn != null && (l.body == null || l.body.InAHand))
        {
            l.drawn.position = l.drawn.position + p.way * (l.reach * 0.12f * p.x);
        }
        if (l.off)
        {
            Back(l);
            if (l.shoulder != null)
            {
                l.shoulder.position = l.shoulder.position + p.way * (l.reach * 0.08f * p.x);
            }
            return;
        }
        if (l.head != null)
        {
            PoseHead(p, l);
            return;
        }
        if (l.up != null && l.lo != null && l.end != null)
        {
            PoseArm(p, l);
        }
    }

    private static void PoseHead(Pull p, Limb l)
    {
        if (l.body != null)
        {
            l.head.localPosition = l.headAt;
            l.head.localRotation = l.headTurn;
        }
        Vector3 at = l.head.position;
        Vector3 to = p.grip.position + l.offset;
        l.head.position = at + (to - at) * Mathf.Clamp01(p.blend);
    }

    private static void PoseMiddle(Pull p, Limb l)
    {
        float stretch = Mathf.Max(0f, (p.pulling.position - p.holding.position).magnitude - p.start);
        stretch = p.blend * stretch;
        Vector3 s = l.spineAt;
        float factor = stretch / Mathf.Max(l.width, 0.05f) + 1f;
        s[l.across] = factor * s[l.across];
        l.spine.localScale = s;

        Vector3 axis = Vector3.zero;
        axis[l.across] = 1f;
        float f = factor - 1f;
        foreach (var (bone, scale) in l.onSpine)
        {
            if (bone == null) continue;
            Vector3 local = Quaternion.Inverse(bone.localRotation) * axis;
            Vector3 ls = bone.localScale;
            bone.localScale = new Vector3(
                ls.x / (local.x * f * local.x + 1f),
                ls.y / (local.y * f * local.y + 1f),
                ls.z / (local.z * f * local.z + 1f));
        }

        if (l.drawn != null)
        {
            l.drawn.position = l.drawn.position + stretch * 0.5f * p.way;
        }
    }

    private static void PoseArm(Pull p, Limb l)
    {
        if (l.body != null)
        {
            l.up.localRotation = l.upTurn;
            l.up.localScale = l.upHad;
            l.lo.localRotation = l.loTurn;
            l.lo.localScale = l.loHad;
            l.lo.localPosition = l.loAt;
            l.end.localRotation = l.endTurn;
            l.end.localScale = l.endRest;
            l.end.localPosition = l.endAt;
        }

        Vector3 endPos = l.end.position;
        Vector3 loPos = l.lo.position;
        Quaternion endRot = l.end.rotation;

        if (l.shoulder != null)
        {
            l.shoulder.position = l.shoulder.position + p.way * (l.reach * 0.08f * p.x);
        }

        Vector3 upPos = l.up.position;
        Vector3 gripPos = p.grip.position;
        float b = Mathf.Clamp01(p.blend);
        float keep = (l.keepOffset - 1f) * b + 1f;
        Vector3 target = endPos + (gripPos + l.offset * keep - endPos) * b;

        Vector3 dir = target - upPos;
        float dist = Mathf.Max(dir.magnitude, 0.001f);
        float ratio = dist / l.reach;
        float least = p.x * 0.15f + 1f;
        float loStretch = b * (Mathf.Max(least, ratio) - 1f) + 1f;
        float loLen = loStretch * l.lengthLo;
        float upStretch = Mathf.Max((least - 1f) * b + 1f, (dist - loLen) / l.lengthUp);
        float upLen = l.lengthUp * upStretch;
        upLen = upLen * upLen;
        Vector3 n = dir / dist;
        float a = (dist * dist + (upLen - loLen * loLen)) / (dist + dist);

        Vector3 bend = Vector3.ProjectOnPlane(loPos - upPos, n);
        if (bend.sqrMagnitude <= 1e-06f)
        {
            Vector3 helper = Mathf.Abs(n.y) >= 0.9f ? Vector3.right : Vector3.up;
            bend = Vector3.Cross(n, helper).normalized;
        }
        else
        {
            bend = bend.normalized;
        }

        float h = Mathf.Sqrt(Mathf.Max(0f, upLen - a * a));
        Vector3 elbow = upPos + n * a + bend * h;
        Vector3 upper = elbow - upPos;
        Vector3 lower = target - elbow;

        float straight = Mathf.Clamp01(Mathf.Clamp01((Vector3.Angle(upper, lower) - 5f) / 25f) * p.blend);
        float upScale = upStretch + (1f - upStretch) * straight;

        l.up.localScale = AlongY(l.upRest, upScale);
        l.lo.localPosition = (upStretch / upScale) * l.loAt;
        l.lo.localScale = AlongY(l.loRest, loStretch / upScale);
        l.end.localScale = AlongY(l.endRest, 1f / loStretch);

        l.up.rotation = Quaternion.FromToRotation(l.up.up, upper) * l.up.rotation;
        l.lo.rotation = Quaternion.FromToRotation(l.lo.up, lower) * l.lo.rotation;
        l.end.rotation = Quaternion.Slerp(endRot, l.lo.rotation, p.blend);
        l.end.position = target;
    }

    private static Vector3 AlongY(Vector3 scale, float by)
    {
        return new Vector3(scale.x, scale.y * by, scale.z);
    }

    private static void Back(Limb l)
    {
        if (l.spine != null)
        {
            Unstretch(l);
            return;
        }
        if (l.shoulder != null)
        {
            l.shoulder.localPosition = l.shoulderAt;
        }
        if (l.head != null)
        {
            l.head.localPosition = l.headAt;
            if (l.body != null)
            {
                l.head.localRotation = l.headTurn;
            }
        }
        if (l.up == null || l.lo == null || l.end == null)
        {
            if (l.body == null)
            {
                return;
            }
        }
        else
        {
            l.end.localScale = l.endRest;
            l.lo.localPosition = l.loAt;
            l.end.localPosition = l.endAt;
            if (l.body == null)
            {
                return;
            }
            l.up.localScale = l.upHad;
            l.lo.localScale = l.loHad;
            if (!l.off)
            {
                l.up.localRotation = l.upTurn;
                l.lo.localRotation = l.loTurn;
                l.end.localRotation = l.endTurn;
            }
        }
        typeof(GillaCorpse).GetMethod("LimbStill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int), typeof(bool) }, null)?.Invoke(l.body, new object[] { l.part, false }); /* ? */
    }

    private static void Finish(Pull p)
    {
        foreach (Limb l in p.limbs)
        {
            Back(l);
        }
    }
}
