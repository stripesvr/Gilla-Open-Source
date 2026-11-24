using System.Collections.Generic;
using System.Reflection;
using Fusion;
using UnityEngine;
using UnityEngine.XR;

public partial class GillaPickaxe : NetworkBehaviour
{
    private bool OnSkin(Vector3 tip, out Skin s)
    {
        s = default(Skin);
        KillCam cam = KillCam.Local;
        int most = cam != null ? cam.punchesToKill : 3;
        bool any = false;
        float reach = 0.012f;

        foreach (NetworkApe a in NetworkApe.All)
        {
            if (a == null || a == NetworkApe.Local || a.IsMonster) continue;
            if (a.tearing == null) continue;
            if (a.head /* ? */ == null) continue;
            if (a.Punches /* ? */ >= most) continue;
            if ((a.head /* ? */.position - tip).sqrMagnitude > 9f) continue;
            GillaWounds w = GillaWounds.On(a.tearing);
            if (w.Surface(tip, reach, out GoreSection sec, out Vector3 world, out Vector3 body, out Vector3 normal, out float inside))
            {
                reach = -inside;
                Skin k = new Skin();
                k.into = Into.Player;
                k.id = (int)a.Object.Id.Raw /* ? */;
                k.section = sec;
                k.world = world;
                k.body = body;
                k.normal = normal;
                k.inside = inside;
                k.wounds = w;
                k.corpse = null;
                s = k;
                any = true;
            }
        }

        foreach (GillaDummy d in GillaDummy.All)
        {
            if (d == null || !d.Alive) continue;
            if (d.tearing == null) continue;
            if (d.transform /* ? */ == null) continue;
            if ((d.transform /* ? */.position - tip).sqrMagnitude > 9f) continue;
            GillaWounds w = GillaWounds.On(d.tearing);
            if (w.Surface(tip, reach, out GoreSection sec, out Vector3 world, out Vector3 body, out Vector3 normal, out float inside))
            {
                reach = -inside;
                Skin k = new Skin();
                k.into = Into.Dummy;
                k.id = GillaDummy.All.IndexOf(d);
                k.thing = (d.Object != null && d.Object.Runner) ? d.Object.Id : default(NetworkId);
                k.section = sec;
                k.world = world;
                k.body = body;
                k.normal = normal;
                k.inside = inside;
                k.wounds = w;
                s = k;
                any = true;
            }
        }

        foreach (GillaCorpse c in GillaCorpse.All)
        {
            if (c == null) continue;
            if (c.Woundable
                && (c.MiddleNow - tip).sqrMagnitude <= 9f
                && c.Surface(tip, reach, out GoreSection sec, out Vector3 world, out Vector3 body, out Vector3 normal, out float inside))
            {
                Skin k = new Skin();
                k.into = Into.Body;
                k.id = c.Id /* ? */;
                k.section = sec;
                k.world = world;
                k.body = body;
                k.normal = normal;
                k.inside = inside;
                k.wounds = null;
                k.corpse = c;
                reach = -inside;
                s = k;
                any = true;
            }
        }
        return any;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Scratch(byte kind, int id, NetworkId thing, byte s, Vector3 from, Vector3 to)
    {
        Scratch(kind, id, thing, s, from, to);
    }

    private void Scratch(byte kind, int id, NetworkId thing, byte s, Vector3 from, Vector3 to)
    {
        Dismemberment copy = null;
        Vector3 a;
        Vector3 b;
        GillaBloodied bloodied;

        if (kind == 1)
        {
            NetworkApe found = NetworkApe.All.Find(x => Scratch_b__155_0(x, id));
            if (found != null)
            {
                copy = found == NetworkApe.Local ? YourBody() : found.tearing;
            }
        }
        else if (kind == 2)
        {
            GillaDummy d = null;
            if (Live && Runner != null)
            {
                NetworkObject o = Runner.FindObject(thing);
                if (o != null) d = o.GetComponent<GillaDummy>();
            }
            else if (id >= 0 && id < GillaDummy.All.Count)
            {
                d = GillaDummy.All[id];
            }
            if (d != null) copy = d.tearing;
        }
        else if (kind == 3)
        {
            GillaCorpse corpse = GillaCorpse.Find(id);
            if (corpse == null) return;
            a = corpse.WorldOn((GoreSection)s, from);
            b = corpse.WorldOn((GoreSection)s, to);
            if (!corpse.Woundable) return;
            bloodied = GillaBloodied.OnBody(corpse.gameObject);
            Paint(bloodied, a, b);
            return;
        }

        if (copy == null) return;
        GillaWounds wounds = GillaWounds.On(copy);
        bloodied = GillaBloodied.On(copy);
        a = wounds.WorldOn((GoreSection)s, from);
        b = wounds.WorldOn((GoreSection)s, to);
        Paint(bloodied, a, b);
    }

    private const BindingFlags HiddenFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static object CallHidden(object target, string name, params object[] args)
    {
        MethodInfo m = target.GetType().GetMethod(name, HiddenFlags);
        return m != null ? m.Invoke(target, args) : null;
    }

    private static T CallHidden<T>(object target, string name, T like)
    {
        return (T)CallHidden(target, name);
    }

    private static T FieldHidden<T>(object target, string name, T like)
    {
        FieldInfo f = target.GetType().GetField(name, HiddenFlags);
        return f != null ? (T)f.GetValue(target) : like;
    }

    private static void Paint(GillaBloodied bloodied, Vector3 a, Vector3 b)
    {
        GillaStains.Job j = new GillaStains.Job();
        j.point = a;
        j.to = b;
        j.radius = 0.0055f;
        j.line = true;
        j.stuff = GillaSplatter.Stuff.Blood;
        CallHidden(bloodied, "Find", j) /* ? */;
        GillaBloodied painter = (GillaBloodied)CallHidden(bloodied, "Painter") /* ? */;
        j.pieces = CallHidden(painter, "Pieces", j.pieces) /* ? */;
        if (j.pieces.Count == 0) return;
        CallHidden(bloodied, "Share") /* ? */;
        j.sheet = FieldHidden(bloodied, "sheet", j.sheet) /* ? */;
        j.big = Mathf.Max(0.05f, bloodied.transform.lossyScale.y);
        j.seed = Random.Range(int.MinValue, int.MaxValue);
        GillaStains.Add(j);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Knock(Vector3 at, float hard)
    {
        Knock(at, hard);
    }

    private void Knock(Vector3 at, float hard)
    {
        Sfx.Play(item.knocks, at, Mathf.Clamp01(hard) * 0.49999997f + 0.4f, 0.9f, 1.05f);
        float c = Mathf.Clamp01(hard);
        XRNode node;
        if (item.Hand == 0) node = XRNode.LeftHand;
        else if (item.Hand == 1) node = XRNode.RightHand;
        else return;
        InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, c * 0.39999998f + 0.3f, 0.08f);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_In(byte e, byte kind, int id, NetworkId thing, byte s, Vector3 p, Vector3 way, float first, float hard)
    {
        In(e, kind, id, thing, s, p, way, first, hard);
    }

    private void In(int e, byte kind, int id, NetworkId thing, byte s, Vector3 p, Vector3 way, float first, float hard)
    {
        if (into != Into.Nothing || !Hold(kind, id, thing, s, p)) return;
        into = (Into)kind;
        item.Stuck = true;
        scraping[0] = false;
        scraping[1] = false;
        targetId = id;
        end = e;
        section = (GoreSection)s;
        point = p;
        stuckAt = Time.time;
        hiddenSince = -1f;
        harmed = false;
        through = false;
        Vector3 scale = on.lossyScale;
        float most;
        if (s == 0) most = Mathf.Min(Blade(e), scale.x * skullStops);
        else most = Blade(e);
        mostIn = most;
        first = Mathf.Min(first, most);
        slidWas = first;
        depth = first;
        if (Live && Object != null && Object.HasStateAuthority)
        {
            Depth = depth;
        }
        axisOn = on.InverseTransformDirection(way);
        turnOn = Quaternion.Inverse(on.rotation) * transform.rotation;
        comeFrom = transform.position;
        comeTurn = transform.rotation;
        comeFor = 0.12f;
        Vector3 at = on.TransformPoint(onLocal);
        AudioClip[] clips = s != 0 ? stabSounds : skullSounds;
        Sfx.Play(clips, at, Mathf.Clamp01(hard) * 0.65f + 0.35f, 0.94f, 1.06f);
        Buzz(Mathf.Clamp01(hard) * 0.65f + 0.35f, 0.12f);
        WetsTo(e, depth);
    }

    private bool Hold(byte kind, int id, NetworkId thing, byte s, Vector3 p)
    {
        on = null;
        pulls = null;
        ape = null;
        copy = null;
        dummy = null;
        monster = null;

        if (kind < 3)
        {
            if (kind == 1)
            {
                ape = NetworkApe.All.Find(a => a != null && a.owner == id);
                Dismemberment c = null;
                if (ape != null)
                {
                    c = ape == NetworkApe.Local ? YourBody() : ape.tearing;
                }
                copy = c;
                if (copy == null) return false;
                on = GillaWounds.On(copy).BoneAt((GoreSection)s, p, out onLocal);
                bodyId = id;
            }
            else if (kind == 2)
            {
                GillaDummy d = null;
                if (Live && Runner != null)
                {
                    NetworkObject o = Runner.FindObject(thing);
                    if (o != null) d = o.GetComponent<GillaDummy>();
                }
                else if (id < GillaDummy.All.Count)
                {
                    d = GillaDummy.All[id];
                }
                dummy = d;
                copy = dummy != null ? dummy.tearing : null;
                if (copy == null) return false;
                on = GillaWounds.On(copy).BoneAt((GoreSection)s, p, out onLocal);
                bodyId = dummy.BodyId;
            }
            return on != null;
        }

        if (kind == 3)
        {
            return OnBody(id, p);
        }

        if (kind != 4) return on != null;

        MonsterBrain m = null;
        if (Live && Runner != null)
        {
            NetworkObject o = Runner.FindObject(thing);
            if (o != null) m = o.GetComponent<MonsterBrain>();
        }
        else if (id < MonsterBrain.All.Count)
        {
            m = MonsterBrain.All[id];
        }
        monster = m;
        if (monster == null) return false;
        if (monster.Hurt == null) return false;
        MonsterWounds hurt = monster.Hurt;
        if (!hurt.Ready /* ? */) return false;
        on = hurt.Wounds /* ? */.BoneAt((GoreSection)s, p, out onLocal);
        return on != null;
    }

    private static Dismemberment YourBody()
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot != null && boot.tearing != null)
        {
            Dismemberment[] copies = boot.tearing.copies;
            if (copies == null || copies.Length == 0) return null;
            return copies[0];
        }
        return null;
    }

    private bool OnBody(int id, Vector3 p)
    {
        GillaCorpse corpse = GillaCorpse.Find(id);
        if (corpse == null) return false;
        Collider best = null;
        float bestSq = float.MaxValue;
        Vector3 bestPoint = p;
        for (int part = 0; part < 4; part++)
        {
            if (part != 0 && !corpse.IsOff(part)) continue;
            Transform t = corpse.PartTransform(part);
            if (t == null) continue;
            t.GetComponentsInChildren<Collider>(found);
            foreach (Collider c in found)
            {
                if (c == null || !c.enabled) continue;
                Vector3 q = c.ClosestPoint(p);
                float sq = (q - p).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestPoint = q;
                    bestSq = sq;
                    best = c;
                }
            }
        }
        if (best == null) return false;
        on = best.transform;
        onLocal = on.InverseTransformPoint(bestPoint);
        pulls = best.attachedRigidbody;
        return true;
    }

    private void InIt(float dt)
    {
        if (StillThere())
        {
            Vector3 hole = on.TransformPoint(onLocal);
            if (item.Mine)
            {
                if (item.Hand < 0)
                {
                    Quaternion rot = on.rotation * turnOn;
                    Transform t = transform;
                    Vector3 off = rot * HoleOn;
                    t.SetPositionAndRotation(hole - off, rot);
                    if (Live && Object != null && Object.HasStateAuthority)
                    {
                        if (Pulling)
                        {
                            Pulling = false;
                        }
                    }
                }
                else
                {
                    if (!Held(hole, dt)) return;
                }
            }
            else
            {
                float d = depth;
                if (Live)
                {
                    d = Mathf.MoveTowards(depth, Depth, dt * 1.5f);
                }
                depth = d;
                Transform t = transform;
                Vector3 off = transform.rotation * HoleOn;
                t.position = hole - off;
            }

            if (item.Hand < 0)
            {
                Pin();
            }
            if (item.Mine)
            {
                if (!harmed && countsAt <= depth)
                {
                    Harms();
                }
                if (!through && ThroughAt() <= depth)
                {
                    GoesThrough();
                }
            }
            WetsTo(end, depth);
            float was = slidWas;
            slidWas = depth;
            float speed = Mathf.Abs(depth - was) / dt;
            if (speed < 0.05f) return;
            if (slideAt <= Time.time)
            {
                slideAt = Time.time + Random.Range(0.22f, 0.38f);
                Vector3 at = on.TransformPoint(onLocal);
                Sfx.Play(slideSounds, at, Mathf.Clamp01(speed + speed) * 0.8f, 0.92f, 1.1f);
            }
            if (!item.Mine) return;
            float amp = Mathf.Clamp(speed, 0.1f, 0.55f);
            XRNode node;
            if (item.Hand == 0) node = XRNode.LeftHand;
            else if (item.Hand == 1) node = XRNode.RightHand;
            else return;
            InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, amp, dt * 1.5f);
            return;
        }

        if (into == Into.Nothing) return;
        into = Into.Nothing;
        item.Stuck = false;
        on = null;
        pulls = null;
        ape = null;
        copy = null;
        dummy = null;
        monster = null;
        if (Live && Object != null && Object.HasStateAuthority)
        {
            Pulling = false;
            Depth = 0f;
        }
        GillaItem it = item;
        it.Pinned = false;
        if (it.Hand >= 0)
        {
            if (it.Mine)
            {
                comeFrom = transform.position;
                comeTurn = transform.rotation;
                comeFor = 0.12f;
                againAt = Time.time + 0.4f;
                Remember();
                return;
            }
            it = item;
        }
        if (it.Hand < 0)
        {
            Collider[] cols = it.Colliders;
            for (int i = 0; i < cols.Length; i++)
            {
                Collider c = cols[i];
                if (c != null) c.enabled = true;
            }
            if (item.Mine)
            {
                item.Body.isKinematic = false;
                item.Body.interpolation = RigidbodyInterpolation.Interpolate;
                item.Body.WakeUp();
            }
        }
        againAt = Time.time + 0.4f;
        Remember();
    }
}
