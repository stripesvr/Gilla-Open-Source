// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class GillaWounds : MonoBehaviour
{
    internal static void Later(Action then)
    {
        if (KillCam.Local == null)
        {
            FinishAll();
            then();
        }
        else
        {
            after.Enqueue(then);
        }
    }

    public static void Pump()
    {
        if (waiting.Count > 0)
        {
            AddWaiting(true, null);
        }
        putFor.Restart();
        for (int i = busy.Count - 1; i >= 0; i--)
        {
            GillaWounds w = busy[i];
            if (w == null)
            {
                busy.RemoveAt(i);
            }
            else if (w.Step(true) && !Waits(w))
            {
                busy.RemoveAt(i);
            }
        }
        if (busy.Count == 0 && waiting.Count == 0 && after.Count > 0)
        {
            Run(after.Dequeue());
        }
    }

    private static bool PutEnough(bool budget)
    {
        if (budget && putFor.IsRunning)
        {
            return putFor.Elapsed.TotalMilliseconds > 1.0;
        }
        return false;
    }

    private static void Run(Action then)
    {
        then();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Fresh()
    {
        busy.Clear();
        after.Clear();
        waiting.Clear();
        addFrame = -1;
        adding = false;
    }

    public void Finish()
    {
        if (Waits(this))
        {
            AddWaiting(false, this);
        }
        if (busy.Contains(this))
        {
            for (int tries = 0; tries != 8; tries++)
            {
                if (Step(false)) break;
                foreach (Soon s in soons)
                {
                    if (s.made != null)
                    {
                        s.made.Wait();
                    }
                }
                if (breaking != null)
                {
                    foreach (var b in breaking)
                    {
                        b.made.Wait();
                    }
                }
            }
            busy.Remove(this);
        }
    }

    public static void FinishAll()
    {
        if (waiting.Count > 0)
        {
            AddWaiting(false, null);
        }
        GillaWounds[] all = busy.ToArray();
        for (int i = 0; i < all.Length; i++)
        {
            GillaWounds w = all[i];
            if (w != null)
            {
                w.Finish();
            }
        }
        busy.Clear();
        while (after.Count >= 1)
        {
            Run(after.Dequeue());
        }
    }

    private void Mark(int wound)
    {
        Vector4 w = wounds[wound];
        Vector4 a = axes[wound];
        for (int i = breakable.Count - 1; i >= 0; i--)
        {
            float reach = w.w * 1.5f * Mathf.Max(a.w, 1.4f);
            Broken b = breakable[i];
            if (b.filter == null)
            {
                breakable.RemoveAt(i);
                continue;
            }
            Bounds box = ShapeOf(b.whole).bounds;
            box.Expand(reach + reach);
            if (b.left != null)
            {
                Vector4 at = wounds[wound];
                if (!box.Contains(new Vector3(at.x, at.y, at.z))) continue;
            }
            toBreak.Add(b);
        }
    }

    private bool Step(bool budget)
    {
        if (breaking == null)
        {
            if (toBreak.Count < 1)
            {
                brokenUpTo = serials;
            }
            else
            {
                Vector4[] w = (Vector4[])wounds.Clone();
                Vector4[] ax = (Vector4[])axes.Clone();
                Vector4[] sd = (Vector4[])sides.Clone();
                int n = count;
                breaking = new List<ValueTuple<Broken, int, Task<BoneOut>>>(toBreak.Count);
                foreach (Broken b in toBreak)
                {
                    BoneShape shape = ShapeOf(b.whole);
                    float gone = b.gone;
                    float thick = b.thick;
                    Vector4 cut = b.cut;
                    Task<BoneOut> made = Task.Run(() => BreakMath(shape, w, ax, sd, n, gone, thick, cut));
                    breaking.Add((b, b.version, made));
                }
                toBreak.Clear();
                breakingUpTo = serials;
            }
        }
        else if (breaking.TrueForAll(x => x.Item3.IsCompleted /* ? */))
        {
            using (PutMark.Auto())
            {
                while (breaking.Count >= 1 && !PutEnough(budget))
                {
                    var last = breaking[breaking.Count - 1];
                    Broken b = last.Item1;
                    int version = last.Item2;
                    Task<BoneOut> made = last.Item3;
                    breaking.RemoveAt(breaking.Count - 1);
                    if (made.IsFaulted)
                    {
                        Debug.LogException(made.Exception);
                    }
                    else if (b.filter != null && b.version == version)
                    {
                        BreakPut(b, made.Result);
                    }
                }
            }
            if (breaking.Count == 0)
            {
                breaking = null;
                brokenUpTo = breakingUpTo;
            }
        }

        bool changed = false;
        for (int i = 0; i < soons.Count; i++)
        {
            if (PutEnough(budget)) break;
            Soon s = soons[i];
            if (!s.gone)
            {
                if ((s.made == null || s.made.IsCompleted) && s.serial <= brokenUpTo)
                {
                    soons.RemoveAt(i);
                    i--;
                    if (!Gone(s.section) || s.stump)
                    {
                        using (PutMark.Auto())
                        {
                            bool put;
                            if (s.made == null)
                            {
                                put = true;
                            }
                            else if (s.made.IsFaulted)
                            {
                                Debug.LogException(s.made.Exception);
                                put = false;
                            }
                            else
                            {
                                put = PutCrater(s.crater, s.made.Result);
                            }
                            if (put)
                            {
                                Draw(s.at, s.wound, s.axis, s.side);
                                KnockOutEyes(s.section, s.at);
                                if (s.burst)
                                {
                                    Burst(s.section, s.point);
                                }
                                changed = true;
                            }
                        }
                    }
                }
            }
            else
            {
                soons.RemoveAt(i);
                i--;
                if (s.burst)
                {
                    Burst(s.section, s.point);
                }
            }
        }
        if (changed)
        {
            Uniforms();
        }
        if (soons.Count == 0 && breaking == null)
        {
            return toBreak.Count == 0;
        }
        return false;
    }

    private Vector4 Spot(int wound, int which)
    {
        Vector4 d = drawn[wound];
        Vector4 sd = drawnSides[wound];
        Vector4 ax = drawnAxes[wound];
        float size = d.w;
        float seed = sd.w;
        float k = which;
        float f = (k + 1f) * seed * 1.37f;
        float dist = size * ((f - (int)f) * 0.9f + 1.7f);
        float angle = k * 2.1f + (k * 2.3f + 1.7f) * seed;
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);
        Vector3 side = new Vector3(sd.x, sd.y, sd.z);
        Vector3 axis = new Vector3(ax.x, ax.y, ax.z);
        Vector3 v = (side * cos + Vector3.Cross(axis, side) * sin) * dist;
        float g = (k + 2f) * seed * 2.11f;
        return new Vector4(v.x, v.y, v.z, size * ((g - (int)g) * 0.4f + 0.4f));
    }

    public static int MeshesReady
    {
        get { return carrying.Count; }
    }

    public void Practice()
    {
        Find();
        var spots = new ValueTuple<GoreSection, Vector3>[4];
        spots[0] = (GoreSection.Torso, new Vector3(0f, -0.08f, 0.14f));
        spots[1] = (GoreSection.Head, new Vector3(0f, -0.12f, 0.33f));
        spots[2] = (GoreSection.ArmL, new Vector3(-0.153f, 0.01f, 0.205f));
        spots[3] = (GoreSection.ArmR, new Vector3(0.154f, 0.009f, 0.203f));
        for (int i = 0; i < spots.Length; i++)
        {
            GoreSection section = spots[i].Item1;
            Vector3 point = spots[i].Item2;
            Add(section, NearestSkin(section, point), -1f);
        }
    }

    private Vector3 Inward(GoreSection section, Vector3 point)
    {
        Vector3 normal = SkinNormal(section, point);
        if ((int)section < 2)
        {
            Vector3 target;
            if (section == GoreSection.Head)
            {
                target = new Vector3(0f, -0.02f, 0.36f);
            }
            else
            {
                target = new Vector3(0f, 0.01f, Mathf.Clamp(point.z, 0.07f, 0.21f));
            }
            float tx = 0f;
            if (fitted != null)
            {
                MonsterGore.Limb limb = LimbNear(section, point);
                if (limb != null)
                {
                    ToSegment(point, limb.from, limb.to, out target);
                    tx = target.x;
                }
            }
            target.x = tx;
            Vector3 toward = Vector3.Normalize(target - point);
            return Vector3.Normalize(toward - normal);
        }
        return -normal;
    }

    private Vector3 SkinNormal(GoreSection section, Vector3 point)
    {
        string name = PartName(section);
        Vector3 best = Vector3.up;
        if (fitted != null)
        {
            Vector3[] verts = Vertices(fittedMesh);
            Vector3[] norms = Normals(fittedMesh);
            byte[] sectionOf = fitted.sectionOf;
            int[] pts = SkinPoints(fittedMesh, null);
            float nearest = float.MaxValue;
            for (int i = 0; i < pts.Length; i++)
            {
                int v = pts[i];
                if (sectionOf[v] == (byte)section)
                {
                    float d = (verts[v] - point).sqrMagnitude;
                    if (d < nearest)
                    {
                        best = norms[v];
                        nearest = d;
                    }
                }
            }
            return best;
        }
        float near = float.MaxValue;
        foreach (var w in whole)
        {
            Mesh mesh = w.Item2;
            Material[] mats = w.Item3;
            if (w.Item1.name.EndsWith(name))
            {
                Vector3[] verts = Vertices(mesh);
                Vector3[] norms = Normals(mesh);
                int[] pts = SkinPoints(mesh, mats);
                for (int i = 0; i < pts.Length; i++)
                {
                    int v = pts[i];
                    float d = (verts[v] - point).sqrMagnitude;
                    if (d < near)
                    {
                        best = norms[v];
                        near = d;
                    }
                }
            }
        }
        return best;
    }

    private void FitToBody(Vector3 point, Vector3 inward, ref float radius, ref float length)
    {
        float depth = ToOutside(point + inward * 0.002f, inward);
        Vector3 up = Mathf.Abs(inward.y) >= 0.9f ? Vector3.right : Vector3.up;
        Vector3 side = Vector3.Normalize(Vector3.Cross(inward, up));
        Vector3 side2 = Vector3.Cross(inward, side);
        Vector3 mid = point + inward * Mathf.Min(radius, depth * 0.5f);
        float across = Mathf.Min(ToOutside(mid, side), ToOutside(mid, -side));
        float across2 = Mathf.Min(ToOutside(mid, side2), ToOutside(mid, -side2));
        float room = Mathf.Min(Mathf.Min(across, across2), depth);
        radius = Mathf.Max(Mathf.Min(radius, room * 0.75f), 0.006f);
        length = Mathf.Max(Mathf.Min(length, (depth * 0.65f) / (radius * 1.12f)), 0.6f);
    }

    private float ToOutside(Vector3 from, Vector3 way)
    {
        float best = 0.2f;
        foreach (var w in whole)
        {
            Mesh mesh = w.Item2;
            Material[] mats = w.Item3;
            Vector3[] verts = Vertices(mesh);
            int[] tris = SkinTriangles(mesh, mats);
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = verts[tris[i]];
                if ((a - from).sqrMagnitude > 0.080000006f) continue;
                Vector3 e1 = verts[tris[i + 1]] - a;
                Vector3 e2 = verts[tris[i + 2]] - a;
                if (Vector3.Dot(Vector3.Cross(e1, e2), way) > 0f)
                {
                    Vector3 p = Vector3.Cross(way, e2);
                    float det = Vector3.Dot(e1, p);
                    if (Mathf.Abs(det) >= 1e-12f)
                    {
                        Vector3 s = from - a;
                        float u = Vector3.Dot(s, p) / det;
                        if (u >= 0f && u <= 1f)
                        {
                            Vector3 q = Vector3.Cross(s, e1);
                            float v = Vector3.Dot(way, q) / det;
                            if (v >= 0f)
                            {
                                float t = Vector3.Dot(e2, q) / det;
                                if (u + v <= 1f && t > 0.0005f && t < best)
                                {
                                    best = t;
                                }
                            }
                        }
                    }
                }
            }
        }
        return best;
    }

    private static Func<Vector3, float> UnderSkinAlong(Skin[] skin, Vector3 from, Vector3 axis, Vector3 side, Vector3 side2, float radius, float pocket)
    {
        float half = radius * 3f;
        float size = (half + half) * 0.015625f;
        List<Vector2>[] grid = new List<Vector2>[4096];
        for (int s = 0; s < skin.Length; s++)
        {
            Bounds box = skin[s].box;
            Vector3 c = box.center - from;
            Vector3 e = box.extents;
            if (Mathf.Abs(Vector3.Dot(c, side)) - (Mathf.Abs(e.x * side.x) + Mathf.Abs(e.y * side.y) + Mathf.Abs(e.z * side.z)) > half) continue;
            if (Mathf.Abs(Vector3.Dot(c, side2)) - (Mathf.Abs(e.x * side2.x) + Mathf.Abs(e.y * side2.y) + Mathf.Abs(e.z * side2.z)) > half) continue;
            int[] tris = skin[s].tris;
            Vector3[] vs = skin[s].vs;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = vs[tris[i]] - from;
                Vector3 b = vs[tris[i + 1]] - from;
                Vector3 cc = vs[tris[i + 2]] - from;
                float ua = Vector3.Dot(a, side), ub = Vector3.Dot(b, side), uc = Vector3.Dot(cc, side);
                float va = Vector3.Dot(a, side2), vb = Vector3.Dot(b, side2), vc = Vector3.Dot(cc, side2);
                float minU = Mathf.Min(Mathf.Min(ub, uc), ua);
                float maxU = Mathf.Max(ua, Mathf.Max(ub, uc));
                float minV = Mathf.Min(Mathf.Min(vb, vc), va);
                float maxV = Mathf.Max(va, Mathf.Max(vb, vc));
                if (maxU < -half || minU > half || maxV < -half || minV > half) continue;
                float det = (ub - ua) * (vc - va) - (uc - ua) * (vb - va);
                if (Mathf.Abs(det) < 1e-12f) continue;
                float sign = Vector3.Dot(Vector3.Cross(b - a, cc - a), axis) >= 0f ? -1f : 1f;
                float da = Vector3.Dot(a, axis), db = Vector3.Dot(b, axis), dc = Vector3.Dot(cc, axis);
                int x1 = Mathf.Min((int)((maxU + half) / size), 63);
                int y0 = Mathf.Max((int)((minV + half) / size), 0);
                int y1 = Mathf.Min((int)((maxV + half) / size), 63);
                int x0 = Mathf.Max((int)((half + minU) / size), 0);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float cu = (x + 0.5f) * size - half;
                        float cv = (y + 0.5f) * size - half;
                        float w0 = ((ub - cu) * (vc - cv) - (uc - cu) * (vb - cv)) / det;
                        if (w0 < 0f) continue;
                        float w1 = ((uc - cu) * (va - cv) - (ua - cu) * (vc - cv)) / det;
                        if (w1 < 0f) continue;
                        float w2 = (1f - w0) - w1;
                        if (w2 < 0f) continue;
                        int cell = y * 64 + x;
                        List<Vector2> list = grid[cell];
                        if (list == null)
                        {
                            list = new List<Vector2>(2);
                            grid[cell] = list;
                        }
                        list.Add(new Vector2(da * w0 + db * w1 + dc * w2, sign));
                    }
                }
            }
        }
        return p => UnderSkinAlong_b__0(p, from, side, side2, axis, half, size, grid, pocket, radius) /* ? */;
    }
}
