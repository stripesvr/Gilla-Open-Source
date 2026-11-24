// Reconstructed from the IL2CPP build (decompiled, not the original source).
// NOTE: GillaWounds.World, NearestSkin, Later and the (string, Vector3) OnBody overload must be made
// internal/public in GillaWounds; those CS0122/CS7036 errors cannot be fixed from this file.
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class MonsterWounds : MonoBehaviour
{
    private bool Pick(Vector3 at, bool low, out MonsterGore.Limb limb, out Vector3 point)
    {
        Vector3 nearest = Vector3.zero;
        limb = null;
        point = Vector3.zero;
        pickFrom.Clear();
        pickWeights.Clear();
        Vector3 unused = transform.position;

        float lowest = float.MaxValue;
        if (low)
        {
            MonsterGore.Limb[] limbs0 = gore.limbs;
            for (int i = 0; i < limbs0.Length; i++)
            {
                MonsterGore.Limb l = limbs0[i];
                int sec = (int)l.section;
                if (sec < 6 && ((1 << sec) & 0x32) != 0)
                {
                    Vector3 a0 = wounds.World(l.bone, l.from);
                    Vector3 b0 = wounds.World(l.bone, l.to);
                    float m = Mathf.Min(a0.y, b0.y);
                    if (m <= lowest)
                    {
                        lowest = m;
                    }
                }
            }
        }

        MonsterGore.Limb[] limbs = gore.limbs;
        float total = 0f;
        for (int i = 0; i < limbs.Length; i++)
        {
            MonsterGore.Limb l = limbs[i];
            if (l.section == GoreSection.ArmR)
            {
                if (armState[1] == 2) continue;
            }
            else if (l.section == GoreSection.ArmL)
            {
                if (armState[0] == 2) continue;
            }
            Vector3 a = wounds.World(l.bone, l.from);
            Vector3 b = wounds.World(l.bone, l.to);
            if (low)
            {
                if (!((int)l.section > 3 || l.section == GoreSection.Torso)) continue;
                if (l.bone.StartsWith("shoulder")) continue;
                if (!(Mathf.Min(a.y, b.y) <= lowest + height * 0.15f)) continue;
            }
            float dist = ToSegment(at, a, b, out nearest);
            Vector3 ab = a - b;
            float d = Mathf.Max(dist - l.radius * perUnit, 0f);
            pickFrom.Add(l);
            d += 0.15f;
            float weight = perUnit * l.radius * (ab.magnitude + l.radius * perUnit) / (d * d * d);
            pickWeights.Add(weight);
            total += weight;
        }

        if (total <= 0f)
        {
            return 0f < total;
        }

        float r = total * UnityEngine.Random.value;
        int k = 0;
        while (k < pickWeights.Count - 1)
        {
            r -= pickWeights[k];
            if (r <= 0f) break;
            k++;
        }

        limb = pickFrom[k];
        Vector3 pa = wounds.World(limb.bone, limb.from);
        Vector3 pb = wounds.World(limb.bone, limb.to);
        Vector3 near;
        ToSegment(at, pa, pb, out near);
        Vector3 seg = pb - pa;
        float t = 0.5f;
        float sq = seg.sqrMagnitude;
        if (1e-06f < sq)
        {
            t = Vector3.Dot(seg, near - pa) / sq;
        }
        t += UnityEngine.Random.Range(-0.3f, 0.3f);
        t = Mathf.Clamp01(t);
        Vector3 p = pa + seg * t;
        Vector3 dir = (at - p).normalized;
        dir = (dir + UnityEngine.Random.insideUnitSphere * 0.8f).normalized;
        GoreSection section = limb.section;
        float reach = limb.radius * perUnit;
        Vector3 onBody = wounds.OnBody(limb.bone, p + dir * reach) /* ? */;
        point = wounds.NearestSkin(section, onBody);
        return 0f < total;
    }

    private static float ToSegment(Vector3 at, Vector3 a, Vector3 b, out Vector3 nearest)
    {
        Vector3 ab = b - a;
        float sq = ab.sqrMagnitude;
        float t = 0f;
        if (1e-08f < sq)
        {
            float f = Vector3.Dot(at - a, ab) / sq;
            if (0f <= f)
            {
                t = f;
                if (1f < f) t = 1f;
            }
        }
        nearest = a + ab * t;
        return (at - nearest).magnitude;
    }

    private void Say(int says, GoreSection section, Vector3 point)
    {
        MonsterNet net = brain != null ? brain.net : null;
        float code = (float)(says * 8 + (int)section);
        Vector4 said = new Vector4(point.x, point.y, point.z, code);
        if (net != null && net.Live)
        {
            if (!net.CanSay)
            {
                return;
            }
            net.Say(said);
        }
        Take(said, !Quiet);
    }

    private void Take(Vector4 said, bool loud)
    {
        Begin();
        if (!ready)
        {
            waiting.Add((said, loud));
            return;
        }
        int code = Mathf.RoundToInt(said.w);
        int sec = code % 8;
        GoreSection section = (GoreSection)(byte)sec;
        if ((code & ~7) == 8)
        {
            MonsterGore.Arm[] list = gore.arms;
            for (int i = 0; i < list.Length; i++)
            {
                MonsterGore.Arm a = list[i];
                if (a.section == section)
                {
                    ArmOff(a, loud);
                }
            }
        }
        else
        {
            Vector3 p = new Vector3(said.x, said.y, said.z);
            wounds.AddSoon(section, p, loud);
            Note(section, p);
            if ((code & ~7) == 16)
            {
                int side;
                if (section == GoreSection.LegL) side = 0;
                else if (section == GoreSection.LegR) side = 1;
                else side = 0f <= said.x ? 1 : 0;
                trappedSide = side;
                trapHeld = false;
            }
        }
        Recount();
    }

    private void Note(GoreSection section, Vector3 point)
    {
        if (section == GoreSection.Head)
        {
            headHits++;
            return;
        }
        if (section == GoreSection.Torso)
        {
            torsoHits++;
            torsoSide += 0f <= point.x ? 1f : -1f;
            return;
        }
        if (section == GoreSection.LegL)
        {
            legHits[0]++;
            return;
        }
        if (section == GoreSection.LegR)
        {
            legHits[1]++;
            return;
        }
        int idx = section != GoreSection.ArmL ? 1 : 0;
        if (armState[idx] == 0)
        {
            armState[idx] = 1;
            if (brokeFirst < 0)
            {
                brokeFirst = idx;
            }
        }
    }

    private void Recount()
    {
        bool onLegs = gore.legs;
        float armL = ArmWeight(0, onLegs);
        float legL = LegWeight(0);
        float armR = ArmWeight(1, onLegs);
        float legR = LegWeight(1);
        armWeight = ArmWeight(0, onLegs) + ArmWeight(1, onLegs);
        float legs0 = LegWeight(0);
        float legs1 = LegWeight(1);
        float wreck = Mathf.Clamp01(armL + legL + armR + legR + torsoHits * 0.05f + headHits * 0.04f);
        Wreck = wreck;
        Side = Mathf.Clamp(((armR + legR) - (armL + legL) + torsoSide * 0.04f) / 0.2f, -1f, 1f);
        limp = Mathf.Clamp01(torsoHits * 0.06f + (legs0 + legs1) * 1.8f + armWeight * 1.2f);
        float f = Mathf.Clamp01(Mathf.Pow(wreck, 0.85f));
        Pace = (slowest - 1f) * f + 1f;

        for (int side = 0; side < 2; side++)
        {
            bool hang;
            int state = armState[side];
            if (state == 2)
            {
                hang = true;
            }
            else if (state == 1)
            {
                if (onLegs)
                {
                    hang = true;
                }
                else
                {
                    int other = armState[side ^ 1];
                    if (other == 0) hang = true;
                    else if (other == 1) hang = side == brokeFirst;
                    else hang = false;
                }
            }
            else
            {
                hang = false;
            }
            hangs[side] = hang;
        }
    }

    private float ArmWeight(int side, bool onLegs)
    {
        float broken = onLegs ? 0.1f : 0.28f;
        float off = onLegs ? 0.2f : 0.42f;
        if (armState[side] != 1) broken = 0f;
        if (armState[side] != 2) off = broken;
        return off;
    }

    private float LegWeight(int side)
    {
        float trapped = 0.2f;
        float hits = Mathf.Min(legHits[side] * 0.22f, 0.45f);
        if (trappedSide != side) trapped = 0f;
        return trapped + hits;
    }

    private void ArmOff(MonsterGore.Arm a, bool loud)
    {
        int idx = a.section != GoreSection.ArmL ? 1 : 0;
        if (armState[idx] == 2)
        {
            return;
        }
        armState[idx] = 2;
        if (brokeFirst < 0)
        {
            brokeFirst = idx;
        }
        if (!loud)
        {
            ArmComesOff(a, false);
            return;
        }
        GillaWounds.Later(() => ArmComesOff(a, true) /* ? */);
    }

    private void ArmComesOff(MonsterGore.Arm a, bool loud)
    {
        GameObject thrown = null;
        if (loud)
        {
            if (skin.enabled)
            {
                thrown = Throw(a);
            }
        }
        wounds.ArmTorn(a.section, a.cut, a.way, thrown);
        float across = 1.2f;
        wounds.Stump(a.section, a.cut, -a.way, a.radius * 1.2f);
        Transform parent = Bone(a.upper);
        if (!loud) return;
        if (parent == null) return;
        if (!Application.isPlaying) return;

        Vector3 p = wounds.World(a.upper, a.cut);
        Vector3 q = wounds.World(a.upper, a.cut + a.way * 0.1f);
        Vector3 outWay = (q - p).normalized;
        Vector3 flat = Vector3.ProjectOnPlane(outWay, Vector3.up).normalized;
        Vector3 dir = (Vector3.up + flat * 0.8f).normalized;
        Dismemberment x = Tearing();
        Material mat = null;
        if (x != null)
        {
            mat = x.bloodMaterial;
        }
        if (x != null)
        {
            Sfx.Play(x.tearSounds, p, x.tearVolume, 0.9f, 1.08f);
        }
        Blood.Burst(p, dir, mat, 0.6f);
        GameObject go = new GameObject("WoundBlood");
        Transform t = go.transform;
        t.SetParent(parent, true);
        t.position = p;
        Blood.Spray(t, dir, mat, bleedSeconds, gushDroplets, bleedPulses, 1f);
        Destroy(t.gameObject, bleedSeconds + 2f);
        GillaSplatter.Around(p, 0.9f, x /* ? */);
        if (thrown == null)
        {
            return;
        }
        Transform tt = thrown.transform;
        GillaStrands.Between(parent, tt, p, outWay, Vector3.zero, across, a.radius * perUnit * 1.3f, 15, 0f);
    }

    private static Dismemberment Tearing()
    {
        NetworkBootstrap inst = NetworkBootstrap.Instance;
        DismemberGroup group = null;
        if (inst != null)
        {
            group = inst.tearing;
        }
        Dismemberment result = null;
        if (group != null)
        {
            Dismemberment[] copies = group.copies;
            if (copies == null || copies.Length == 0)
            {
                result = null;
            }
            else
            {
                result = copies[0];
            }
        }
        return result;
    }

    private GameObject Throw(MonsterGore.Arm a)
    {
        if (a.piece == null)
        {
            return null;
        }
        if (poser == null)
        {
            GameObject pose = new GameObject("ArmPose");
            pose.transform.SetParent(skin.transform.parent, false);
            pose.transform.SetLocalPositionAndRotation(skin.transform.localPosition, skin.transform.localRotation);
            pose.transform.localScale = skin.transform.localScale;
            poser = pose.AddComponent<SkinnedMeshRenderer>();
            poser.bones = skin.bones;
            poser.rootBone = skin.rootBone;
            poser.enabled = false;
        }
        poser.sharedMesh = a.piece;
        Mesh mesh = new Mesh();
        mesh.name = "Torn_" + a.section.ToString();
        poser.BakeMesh(mesh, false);
        mesh.RecalculateBounds();
        GameObject go = new GameObject("Torn_" + a.section.ToString());
        go.transform.SetPositionAndRotation(poser.transform.position, poser.transform.rotation);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        Rigidbody body = go.AddComponent<Rigidbody>();
        TornPiece piece = go.AddComponent<TornPiece>();
        Material[] look = wounds.PieceLook(a.slots, gore.flesh);
        renderer.sharedMaterials = piece.Own(look);
        go.layer = 2;
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = mesh.bounds.center;
        Bounds bounds = mesh.bounds;
        box.size = (bounds.extents + bounds.extents) * 0.85f;
        body.mass = 3f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Vector3 q = wounds.World(a.upper, a.cut + a.way * 0.1f);
        Vector3 p = wounds.World(a.upper, a.cut);
        Vector3 outWay = (q - p).normalized;
        Vector3 up = Vector3.up;
        Vector3 rnd = UnityEngine.Random.insideUnitSphere;
        Vector3 flyDir = (outWay + up * 0.5f + rnd * 0.4f).normalized;
        float speed = throwSpeed;
        float k = UnityEngine.Random.Range(0.75f, 1.15f);
        body.linearVelocity = flyDir * speed * k;
        body.angularVelocity = UnityEngine.Random.insideUnitSphere * 9f;

        Collider[] mine = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < mine.Length; i++)
        {
            Physics.IgnoreCollision(box, mine[i]);
        }
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
        if (player != null)
        {
            Collider[] theirs = player.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < theirs.Length; i++)
            {
                Physics.IgnoreCollision(box, theirs[i]);
            }
        }

        Dismemberment x = Tearing();
        AudioClip[] drops = null;
        if (x != null)
        {
            drops = x.dropSounds;
        }
        piece.dropSounds = drops;
        piece.life = pieceLife;
        Thrown thrownPart = go.AddComponent<Thrown>();
        thrownPart.Begin(mesh, pieceLife - pieceShrink, pieceShrink);
        Material mat = null;
        if (x != null)
        {
            mat = x.bloodMaterial;
        }
        if (Application.isPlaying)
        {
            GameObject anchor = new GameObject("BloodAnchor");
            Transform at = anchor.transform;
            at.SetParent(go.transform, false);
            at.position = wounds.World(a.upper, a.cut);
            Blood.Spray(at, -outWay, mat, 2f, 45, 3, 1f);
        }
        GillaDoor.OutOfSlabs(go.transform, renderer.bounds, body);
        return go;
    }

    public void Died()
    {
        if (Quiet)
        {
            return;
        }
        GillaWounds.Later(Died_b__105_0 /* ? */);
    }

    public float StepLoud()
    {
        if (!ready)
        {
            return 1f;
        }
        if (Wreck <= 0f)
        {
            return 1f;
        }
        Chain[] chains = gore.legs ? legs : arms;
        float side = Side;
        int s = 0f <= side ? 1 : 0;
        int o = side < 0f ? 1 : 0;
        Chain sc = chains[s];
        if (sc == null || chains[o] == null)
        {
            return 1f;
        }
        Vector3 sp = sc.end.position;
        Vector3 op = chains[o].end.position;
        if (op.y < sp.y)
        {
            return 1f;
        }
        if (!gore.legs)
        {
            if (hangs[s])
            {
                return 0.3f;
            }
        }
        return Mathf.Clamp01(limp) * -0.5f + 1f;
    }

    private void LateUpdate()
    {
        if (!ready)
        {
            return;
        }
        if (Wreck <= 0f && thrash <= 0f)
        {
            if (brain.SnareLeft <= 0f)
            {
                return;
            }
        }
        if (brain.animator == null)
        {
            return;
        }
        if (!brain.animator.isActiveAndEnabled)
        {
            return;
        }

        float dt = 0f;
        if (!brain.weeping || 0f < brain.AnimPace)
        {
            if (brain.Dead || brain.animator.speed > 0f)
            {
                dt = Time.deltaTime;
            }
        }
        clock += dt;

        bool snared = brain.SnareLeft > 0f;
        thrash = Mathf.MoveTowards(thrash, snared ? 1f : 0f, (snared ? 9f : 3f) * dt);

        float y = transform.position.y;
        if (brain.Dead || 1f < brain.ClimbPitch)
        {
            Arms(dt, y);
            return;
        }
        KillCam cam = KillCam.Local;
        if (cam != null && cam.Busy && cam.Killer == brain)
        {
            Arms(dt, y);
            return;
        }

        Vector3 fwd = transform.forward;
        Vector3 across = Across();
        float move = Mathf.Clamp01(brain.AnimSpeed * 1.5f);

        for (int i = 0; i < 2; i++)
        {
            Keep(legs[i]);
            if (!hangs[i])
            {
                Keep(arms[i]);
            }
        }

        float roll;
        float pitch;
        float drop;
        float push;
        if (!gore.legs)
        {
            Chain c = arms[Side < 0f ? 1 : 0];
            float st = c == null ? 1f : Stance(c, y, dt);
            float l = lurch;
            if (0f < dt)
            {
                l = Mathf.MoveTowards(lurch, st, dt * 9f);
            }
            lurch = l;
            float w = Mathf.Clamp01(armWeight / 0.28f);
            float inv = 1f - l;
            roll = Side * (move * inv * 0.32f + l * -0.14f);
            drop = inv * height * 0.07f * move * w;
            push = (l - 0.5f) * height * 0.1f * move * w;
            pitch = Wreck * 0.16f + 0.06f + w * move * inv * 0.12f;
        }
        else
        {
            float side = Side;
            Chain c = legs[0f <= side ? 1 : 0];
            float st;
            if (c == null)
            {
                st = 0.5f;
            }
            else
            {
                st = Stance(c, y, dt);
                side = Side;
            }
            float wreck = Wreck;
            drop = height * (wreck * 0.012f + move * st * limp * 0.05f);
            roll = side * (wreck * 0.1f + 0.04f) + move * st * side * -0.22f * limp;
            push = 0f;
            pitch = wreck * 0.16f + 0.04f;
        }

        float n1 = Mathf.PerlinNoise(clock * 0.9f, seed);
        float n2 = Mathf.PerlinNoise(seed, clock * 0.7f);
        float c0 = clock;
        roll += (n1 - 0.5f) * 0.38f * Wreck;
        pitch += Wreck * (n2 - 0.5f) * 0.28f;
        float shake = thrash * (Mathf.Sin(c0 * 3.1f) * 0.28f + 0.72f);
        float sx;
        float sy;
        if (shake <= 0f)
        {
            sx = 0f;
            sy = 0f;
        }
        else
        {
            float s0 = Mathf.Sin(c0 * 7.3f + 1.7f + seed);
            float s1 = Mathf.Sin(c0 * 14.7f + 0f + seed);
            float s2 = Mathf.Sin(c0 * 12.1f + 2.89f + seed);
            float s3 = Mathf.Sin(c0 * 9.1f + 0f + seed);
            sx = s1 * 0.4f + s3 * 0.6f;
            sy = s0 * 0.6f + s2 * 0.4f;
            float s4 = Mathf.Sin(c0 * 11.3f + 3.1f + seed);
            float s5 = Mathf.Sin(c0 * 17.9f + 5.27f + seed);
            pitch += shake * sy * 0.4f;
            roll += shake * sx * 0.55f;
            drop += shake * height * 0.035f * ((s4 * 0.6f + s5 * 0.4f) * 0.5f + 0.5f);
        }

        bool none = legs[0] == null && legs[1] == null && arms[0] == null && arms[1] == null;

        Vector3 tilt = fwd * pitch + across * roll;
        if (pelvis != null)
        {
            pelvis.position = pelvis.position + fwd * push + drop * Vector3.down;
            Tilt(pelvis, tilt * (0 < spiderCount ? 0.1f : 0.35f));
        }
        Tilt(spine, tilt * 0.35f);
        Tilt(chest, tilt * 0.3f);

        if (!none)
        {
            for (int i = 0; i < 2; i++)
            {
                Chain leg = legs[i];
                if (leg != null)
                {
                    Vector3 target;
                    if (i != trappedSide || thrash <= 0f)
                    {
                        target = leg.at;
                        float k;
                        if (i == (0f <= Side ? 1 : 0) && 0f < (k = Mathf.Abs(Side) * limp))
                        {
                            Stance(leg, y, 0f);
                            Vector3 off = Vector3.ProjectOnPlane(target - leg.hip, Vector3.up);
                            float k01 = Mathf.Clamp01(k);
                            float lowY = y + leg.low;
                            float above = Mathf.Max(target.y - lowY, 0f);
                            float scale = k01 * -0.39999998f + 1f;
                            target.x = leg.hip.x + off.x * scale;
                            target.z = off.z * scale + leg.hip.z;
                            target.y = lowY + above * (k01 * -0.7f + 1f);
                        }
                    }
                    else
                    {
                        if (!trapHeld)
                        {
                            trapHeld = true;
                            trapAt = leg.at;
                        }
                        target = trapAt;
                    }
                    Solve(leg, target, 1f);
                    leg.end.rotation = leg.turn;
                }

                Chain arm = arms[i];
                if (arm != null)
                {
                    if (!hangs[i])
                    {
                        float w = Stance(arm, y, dt);
                        if (0f < w)
                        {
                            Solve(arm, arm.at, w);
                            Transform end = arm.end;
                            end.rotation = Quaternion.Slerp(end.rotation, arm.turn, w);
                        }
                    }
                }
            }
        }

        Arms(dt, y);
        if (head != null)
        {
            float h = Mathf.Clamp01(headHits * 0.25f + Wreck * 0.4f);
            float a1 = sx * 0.5f * shake + (n2 - 0.5f) * 0.5f * h;
            float a2 = sy * 0.4f * shake + h * 0.12f;
            Tilt(head, across * a1 + fwd * a2);
        }
        if (0f < shake)
        {
            Flail(fwd, across, shake);
        }
    }
}
