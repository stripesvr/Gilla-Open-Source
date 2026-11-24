// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class MonsterWounds : MonoBehaviour
{
    private void Show(float dt)
    {
        clock += dt;
        float snare = brain.SnareLeft;
        float wantThrash = snare > 0f ? 1f : 0f;
        float rate = snare > 0f ? 9f : 3f;
        thrash = Mathf.MoveTowards(thrash, wantThrash, rate * dt);
        float floor = transform.position.y;
        if (brain.Dead || brain.ClimbPitch > 1f)
        {
            Arms(dt, floor);
            return;
        }
        KillCam cam = KillCam.Local;
        if (cam != null && cam.Busy && cam.Killer == brain)
        {
            Arms(dt, floor);
            return;
        }
        Vector3 ahead = transform.forward;
        Vector3 across = Across();
        float go = Mathf.Clamp01(brain.AnimSpeed * 1.5f);
        for (int i = 0; i < 2; i++)
        {
            Keep(legs[i]);
            if (!hangs[i])
            {
                Keep(arms[i]);
            }
        }
        float sway;
        float sink;
        float shove;
        float bend;
        if (!gore.legs)
        {
            Chain c = arms[Side < 0f ? 1 : 0];
            float st = c == null ? 1f : Stance(c, floor, dt);
            float l = lurch;
            if (dt > 0f)
            {
                l = Mathf.MoveTowards(lurch, st, dt * 9f);
            }
            lurch = l;
            float w = Mathf.Clamp01(armWeight / 0.28f);
            float inv = 1f - l;
            sway = Side * (go * inv * 0.32f + l * -0.14f);
            sink = inv * height * 0.07f * go * w;
            shove = (l - 0.5f) * height * 0.1f * go * w;
            bend = Wreck * 0.16f + 0.06f + w * go * inv * 0.12f;
        }
        else
        {
            float s = Side;
            Chain c = legs[s >= 0f ? 1 : 0];
            float st;
            if (c == null)
            {
                st = 0.5f;
            }
            else
            {
                st = Stance(c, floor, dt);
                s = Side;
            }
            float wreck = Wreck;
            sink = height * (wreck * 0.012f + go * st * limp * 0.05f);
            sway = s * (wreck * 0.1f + 0.04f) + go * st * s * -0.22f * limp;
            shove = 0f;
            bend = wreck * 0.16f + 0.04f;
        }
        float n1 = Mathf.PerlinNoise(clock * 0.9f, seed);
        float n2 = Mathf.PerlinNoise(seed, clock * 0.7f);
        sway += (n1 - 0.5f) * 0.38f * Wreck;
        bend += Wreck * (n2 - 0.5f) * 0.28f;
        float env = thrash * (Mathf.Sin(clock * 3.1f) * 0.28f + 0.72f);
        float shakeA;
        float shakeB;
        if (env <= 0f)
        {
            shakeA = 0f;
            shakeB = 0f;
        }
        else
        {
            shakeA = Wave(9.1f, 14.7f, 0f);
            shakeB = Wave(7.3f, 12.1f, 1.7f);
            float bob = Wave(11.3f, 17.9f, 3.1f);
            bend += env * shakeB * 0.4f;
            sway += env * shakeA * 0.55f;
            sink += env * height * 0.035f * (bob * 0.5f + 0.5f);
        }
        bool noLimbs = legs[0] == null && legs[1] == null && arms[0] == null && arms[1] == null;
        Vector3 tilt = ahead * bend + across * sway;
        if (pelvis != null)
        {
            pelvis.position = pelvis.position + ahead * shove + Vector3.down * sink;
            Tilt(pelvis, tilt * (spiderCount > 0 ? 0.1f : 0.35f));
        }
        Tilt(spine, tilt * 0.35f);
        Tilt(chest, tilt * 0.3f);
        if (!noLimbs)
        {
            for (int i = 0; i < 2; i++)
            {
                Chain leg = legs[i];
                if (leg != null)
                {
                    Vector3 want;
                    if (i != trappedSide || thrash <= 0f)
                    {
                        want = leg.at;
                        float k;
                        if (i == (Side >= 0f ? 1 : 0) && (k = Mathf.Abs(Side) * limp) > 0f)
                        {
                            Stance(leg, floor, 0f);
                            Vector3 flat = Vector3.ProjectOnPlane(want - leg.hip, Vector3.up);
                            float kc = Mathf.Clamp01(k);
                            float ground = floor + leg.low;
                            float lift = Mathf.Max(want.y - ground, 0f);
                            float squeeze = kc * -0.39999998f + 1f;
                            want = new Vector3(leg.hip.x + flat.x * squeeze,
                                               ground + lift * (kc * -0.7f + 1f),
                                               flat.z * squeeze + leg.hip.z);
                        }
                    }
                    else
                    {
                        if (!trapHeld)
                        {
                            trapHeld = true;
                            trapAt = leg.at;
                        }
                        want = trapAt;
                    }
                    Solve(leg, want, 1f);
                    leg.end.rotation = leg.turn;
                }
                Chain arm = arms[i];
                if (arm != null && !hangs[i])
                {
                    float s = Stance(arm, floor, dt);
                    if (s > 0f)
                    {
                        Solve(arm, arm.at, s);
                        Transform end = arm.end;
                        end.rotation = Quaternion.Slerp(end.rotation, arm.turn, s);
                    }
                }
            }
        }
        Arms(dt, floor);
        if (head != null)
        {
            float hurt = Mathf.Clamp01(headHits * 0.25f + Wreck * 0.4f);
            float roll = shakeA * 0.5f * env + (n2 - 0.5f) * 0.5f * hurt;
            float nod = shakeB * 0.4f * env + hurt * 0.12f;
            Tilt(head, across * roll + ahead * nod);
        }
        if (env > 0f)
        {
            Flail(ahead, across, env);
        }
    }

    private static void Tilt(Transform bone, Vector3 over)
    {
        if (bone != null)
        {
            Vector3 up = Vector3.up;
            Vector3 to = (over + up).normalized;
            bone.rotation = Quaternion.FromToRotation(up, to) * bone.rotation;
        }
    }

    private Vector3 Across()
    {
        Transform a;
        if (arms[0] != null) a = arms[0].upper;
        else if (legs[0] != null) a = legs[0].upper;
        else a = null;
        Transform b;
        if (arms[1] != null) b = arms[1].upper;
        else if (legs[1] != null) b = legs[1].upper;
        else b = null;
        if (!(a == null) && !(b == null))
        {
            Vector3 d = b.position - a.position;
            Vector3 flat = new Vector3(d.x, 0f, d.z);
            if (flat.sqrMagnitude > 1e-06f)
            {
                return flat.normalized;
            }
        }
        return transform.right;
    }

    private static void Keep(Chain c)
    {
        if (c == null)
        {
            return;
        }
        c.at = c.end.position;
        c.turn = c.end.rotation;
        c.hip = c.upper.position;
    }

    private float Stance(Chain c, float floor, float dt)
    {
        float h = c.at.y - floor;
        float low = h;
        if (c.low != float.MaxValue)
        {
            float moved = Mathf.MoveTowards(c.low, h, height * dt * 0.01f);
            if (moved <= h)
            {
                low = moved;
            }
        }
        c.low = low;
        float t = Mathf.Clamp01(((h - low) + height * -0.012f) / (height * 0.08f));
        return 1f - Mathf.SmoothStep(0f, 1f, t);
    }

    private float Wave(float one, float other, float from)
    {
        return Mathf.Sin(seed + clock * one + from) * 0.6f + Mathf.Sin(seed + from * 1.7f + clock * other) * 0.4f;
    }

    private void Arms(float dt, float floor)
    {
        Vector3 across = Across();
        float hangRate = dt / 0.35f;
        float damp = Mathf.Pow(0.965f, dt * 72f);
        float step = Mathf.Min(dt, 0.04f);
        float step2 = step * step;
        for (int i = 0; i < 2; i++)
        {
            Chain c = arms[i];
            if (c == null)
            {
                continue;
            }
            float h;
            if (dt <= 0f)
            {
                h = hanging[i];
            }
            else
            {
                h = Mathf.MoveTowards(hanging[i], hangs[i] ? 1f : 0f, hangRate);
            }
            hanging[i] = h;
            if (!(h > 0f))
            {
                continue;
            }
            Vector3 shoulder = c.upper.position;
            Vector3 el = c.lower.position;
            Vector3 wr = c.end.position;
            float upperLen = Vector3.Distance(el, shoulder);
            if (!swinging[i] || (elbow[i] - shoulder).sqrMagnitude > upperLen * upperLen * 4f)
            {
                swinging[i] = true;
                elbowWas[i] = el;
                elbow[i] = el;
                wristWas[i] = wr;
                wrist[i] = wr;
            }
            if (dt > 0f)
            {
                float sideSign = i == 0 ? -1f : 1f;
                Transform from = chest != null ? chest : transform;
                Vector3 body = from.position;
                Vector3 g = Physics.gravity * step2;
                Vector3 e = elbow[i];
                Vector3 w = wrist[i];
                elbow[i] = e + g + (e - elbowWas[i]) * damp;
                wrist[i] = wrist[i] + g + (w - wristWas[i]) * damp;
                elbowWas[i] = e;
                wristWas[i] = w;
                Vector3 outward = across * sideSign;
                float push = Mathf.Max(armClear[i] - Vector3.Dot(outward, elbow[i] - body), 0f);
                elbow[i] = elbow[i] + outward * push;
                push = Mathf.Max(armClear[i] - Vector3.Dot(outward, wrist[i] - body), 0f);
                wrist[i] = wrist[i] + outward * push;
                elbow[i] = shoulder + (elbow[i] - shoulder).normalized * upperLen;
                Vector3 eNow = elbow[i];
                float lowerLen = Vector3.Distance(wr, el);
                wrist[i] = eNow + (wrist[i] - eNow).normalized * lowerLen;
                if (wrist[i].y < floor)
                {
                    wrist[i].y = floor;
                    wristWas[i] = wristWas[i] + (wrist[i] - wristWas[i]) * 0.5f;
                }
                if (elbow[i].y < floor)
                {
                    elbow[i].y = floor;
                }
            }
            Aim(c.upper, el, elbow[i], hanging[i]);
            if (armState[i] != 2)
            {
                Transform lower = c.lower;
                Aim(lower, c.end.position, wrist[i], hanging[i]);
            }
        }
    }

    private static void Aim(Transform bone, Vector3 tipNow, Vector3 tipWanted, float much)
    {
        Vector3 now = tipNow - bone.position;
        Vector3 pos = bone.position;
        if (now.sqrMagnitude >= 1e-08f)
        {
            Vector3 want = tipWanted - pos;
            if (want.sqrMagnitude >= 1e-08f)
            {
                Quaternion q = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(now, want), much);
                bone.rotation = q * bone.rotation;
            }
        }
    }

    private void Flail(Vector3 ahead, Vector3 across, float env)
    {
        float k = env * 0.35f;
        if (gore.legs)
        {
            k = env;
        }
        for (int i = 0; i < 2; i++)
        {
            Chain arm = arms[i];
            float off = i * 2.3f;
            if (arm != null && !hangs[i])
            {
                Transform upper = arm.upper;
                Quaternion q1 = Quaternion.AngleAxis(k * Wave(8.3f, 13.1f, off) * 55f, across);
                Quaternion q2 = Quaternion.AngleAxis(k * Wave(6.7f, 11.9f, off + 1f) * 32f, ahead);
                upper.rotation = q1 * q2 * arm.upper.rotation;
                Transform lower = arm.lower;
                Quaternion q3 = Quaternion.AngleAxis(k * Wave(10.1f, 15.3f, off + 2f) * 40f, across);
                lower.rotation = q3 * arm.lower.rotation;
            }
            Chain leg = legs[i];
            if (leg != null && i != trappedSide)
            {
                Transform upper = leg.upper;
                Quaternion q = Quaternion.AngleAxis(Wave(9.7f, 12.7f, off + 3f) * 30f * env, across);
                upper.rotation = q * leg.upper.rotation;
            }
        }
        for (int j = 0; j < spiderCount; j++)
        {
            Transform bone = spiderLegs[j];
            Quaternion q = Quaternion.AngleAxis(Wave(11f, 16f, j * 0.9f) * 16f * env, across);
            bone.rotation = q * spiderLegs[j].rotation;
        }
    }

    private static void Solve(Chain c, Vector3 target, float much)
    {
        Vector3 a = c.upper.position;
        Vector3 b = c.lower.position;
        Vector3 e = c.end.position;
        float m = Mathf.Clamp01(much);
        float ab = Vector3.Distance(b, a);
        if (ab < 1e-05f)
        {
            return;
        }
        Vector3 be = e - b;
        float bc = be.magnitude;
        if (bc < 1e-05f)
        {
            return;
        }
        Vector3 tip = Vector3.Lerp(e, target, m) - a;
        float reach = Mathf.Clamp(tip.magnitude, Mathf.Abs(ab - bc) + 0.001f, ab + bc - 0.001f);
        Vector3 ba = a - b;
        Vector3 axis = Vector3.Cross(ba, be);
        if (axis.sqrMagnitude < 1e-10f)
        {
            axis = Vector3.Cross(ba, Vector3.forward);
            if (axis.sqrMagnitude <= 1e-08f)
            {
                axis = Vector3.Cross(ba, Vector3.right);
            }
        }
        float now = Vector3.Angle(ba, be);
        float want = Mathf.Acos(Mathf.Clamp((ab * ab + bc * bc - reach * reach) / ((ab + ab) * bc), -1f, 1f));
        Transform lower = c.lower;
        Quaternion bendQ = Quaternion.AngleAxis(want * 57.29578f - now, axis.normalized);
        lower.rotation = bendQ * c.lower.rotation;
        Vector3 cur = c.end.position - a;
        if (cur.sqrMagnitude <= 1e-08f)
        {
            return;
        }
        if (tip.sqrMagnitude <= 1e-08f)
        {
            return;
        }
        Transform upper = c.upper;
        Quaternion turnQ = Quaternion.FromToRotation(cur, tip);
        upper.rotation = turnQ * c.upper.rotation;
    }

    public static void WarmUp(Transform under)
    {
        Material mat = Resources.Load<Material>("MonsterWoundSkin");
        if (mat == null)
        {
            return;
        }
        if (warmMesh == null)
        {
            warmMesh = new Mesh();
            warmMesh.name = "MonsterWoundWarmUp";
            Vector3[] verts = new Vector3[3];
            verts[0] = Vector3.zero;
            verts[1] = Vector3.up;
            verts[2] = Vector3.right;
            Vector2[] uvs = new Vector2[3];
            uvs[0] = Vector2.zero;
            uvs[1] = Vector2.up;
            uvs[2] = Vector2.right;
            warmMesh.SetVertices(verts);
            Vector3[] normals = new Vector3[3];
            normals[0] = Vector3.back;
            normals[1] = Vector3.back;
            normals[2] = Vector3.back;
            warmMesh.SetNormals(normals);
            Vector4[] tangents = new Vector4[3];
            tangents[0] = new Vector4(1f, 0f, 0f, 1f);
            tangents[1] = new Vector4(1f, 0f, 0f, 1f);
            tangents[2] = new Vector4(1f, 0f, 0f, 1f);
            warmMesh.SetTangents(tangents);
            warmMesh.SetUVs(0, uvs);
            warmMesh.SetUVs(1, uvs);
            warmMesh.SetUVs(2, new List<Vector3>(verts));
            warmMesh.SetUVs(3, new List<Vector3>(verts));
            BoneWeight[] weights = new BoneWeight[3];
            for (int i = 0; i < 3; i++)
            {
                weights[i].weight0 = 1f;
                weights[i].weight1 = 0f;
                weights[i].weight2 = 0f;
                weights[i].weight3 = 0f;
                weights[i].boneIndex0 = 0;
                weights[i].boneIndex1 = 0;
                weights[i].boneIndex2 = 0;
                weights[i].boneIndex3 = 0;
            }
            warmMesh.boneWeights = weights;
            Matrix4x4[] binds = new Matrix4x4[1];
            binds[0] = Matrix4x4.identity;
            warmMesh.bindposes = binds;
            int[] tris = new int[3];
            tris[1] = 1;
            tris[2] = 2;
            warmMesh.SetTriangles(tris, 0);
            warmMesh.RecalculateBounds();
        }
        GameObject go = new GameObject("PracticeMonster");
        go.transform.SetParent(under, false);
        SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = warmMesh;
        Transform[] bones = new Transform[1];
        bones[0] = go.transform;
        smr.bones = bones;
        smr.rootBone = go.transform;
        smr.sharedMaterial = mat;
        GameObject piece = new GameObject("PracticeMonsterPiece");
        piece.transform.SetParent(under, false);
        MeshFilter filter = piece.AddComponent<MeshFilter>();
        filter.sharedMesh = warmMesh;
        MeshRenderer mr = piece.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
    }

    public MonsterWounds()
    {
        slowest = 0.32f;
        pieceLife = 9f;
        tearReach = 1.2f;
        tearAgainReach = 2.4f;
        pieceShrink = 0.5f;
        throwSpeed = 6f;
        bleedPulses = 6;
        gushDroplets = 110;
        bleedSeconds = 4f;
        Pace = 1f;
        perUnit = 1f;
        height = 1.8f;
        named = new Dictionary<string, Transform>();
        waiting = new List<(Vector4 said, bool loud)>();
        waitingCalls = new List<Action>();
        pickFrom = new List<MonsterGore.Limb>();
        pickWeights = new List<float>();
        armState = new int[2];
        hangs = new bool[2];
        legHits = new int[2];
        brokeFirst = -1;
        trappedSide = -1;
        arms = new Chain[2];
        legs = new Chain[2];
        spiderLegs = new Transform[8];
        hanging = new float[2];
        elbow = new Vector3[2];
        elbowWas = new Vector3[2];
        wrist = new Vector3[2];
        wristWas = new Vector3[2];
        swinging = new bool[2];
        armClear = new float[2];
        lurch = 1f;
    }
}
