using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Fusion;

public partial class GillaDummy : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Stab(byte section, Vector3 point, float radius, byte does)
    {
        Stabbed((GoreSection)section, point, radius, does);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Tear(byte part)
    {
        Torn((ApePart)part);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Rip(PlayerRef by, byte seed, NetworkBool plusInRight, Quaternion rotRight, Quaternion rotLeft)
    {
        Ripped(by, seed, plusInRight, rotRight, rotLeft);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Blasted(Vector3 velocity, byte torn, NetworkBool apart, byte seed)
    {
        Blown(velocity, torn, apart, seed);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Eaten(NetworkId monster)
    {
        NetworkObject obj = null;
        if (Runner != null)
        {
            obj = Runner.FindObject(monster);
        }
        MonsterBrain brain = null;
        if (obj != null)
        {
            brain = obj.GetComponent<MonsterBrain>();
        }
        if (brain != null)
        {
            EatenBy(brain);
        }
    }

    private void Grabbed(PlayerRef by, bool right, Vector3 eyesFromHand)
    {
        if (mode != Mode.Standing)
        {
            return;
        }
        mode = Mode.Held;
        holder = by;
        holderRight = right;
        heldEyes = Vector3.ClampMagnitude(eyesFromHand, 2.5f);
    }

    private void Released(Vector3 at, Vector3 velocity)
    {
        if (mode == Mode.Held)
        {
            if (velocity.magnitude >= ThrownDies)
            {
                Die(velocity, false);
                return;
            }
            mode = Mode.Standing;
            standAt = FloorUnder(at);
            Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (flat.sqrMagnitude <= 0.0001f)
            {
                standFacing = standFacing;
            }
            else
            {
                standFacing = Quaternion.LookRotation(flat, Vector3.up);
            }
            if (Mine)
            {
                transform.SetPositionAndRotation(standAt, standFacing);
            }
        }
    }

    private static void CallLater(Action action)
    {
        MethodInfo m = typeof(GillaWounds).GetMethod("Later", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(Action) }, null); /* ? */
        if (m != null)
        {
            m.Invoke(null, new object[] { action });
        }
    }

    private void InvokeBlownPart(string name, byte torn, Vector3 velocity, int id, byte seed)
    {
        MethodInfo m = typeof(GillaDummy).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public); /* ? */
        if (m == null)
        {
            return;
        }
        ParameterInfo[] ps = m.GetParameters();
        object[] args = new object[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            Type t = ps[i].ParameterType;
            string n = ps[i].Name;
            if (n == "torn" || (n != "seed" && t == typeof(byte) && i == 0)) args[i] = torn;
            else if (n == "seed") args[i] = seed;
            else if (t == typeof(Vector3)) args[i] = velocity;
            else if (t == typeof(int)) args[i] = id;
            else if (t == typeof(byte)) args[i] = seed;
            else args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
        }
        m.Invoke(m.IsStatic ? null : this, args);
    }

    private void Punched(GoreSection section, Vector3 point, bool counts)
    {
        if (mode != Mode.Gone)
        {
            if (tearing == null)
            {
                return;
            }
            GillaWounds wounds = GillaWounds.On(tearing);
            if (wounds == null)
            {
                return;
            }
            if (counts)
            {
                wounds.Add(section, point, -1f);
                MethodInfo burst = typeof(GillaWounds).GetMethod("Burst", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(GoreSection), typeof(Vector3) }, null); /* ? */
                if (burst != null)
                {
                    burst.Invoke(wounds, new object[] { section, point });
                }
                hits++;
                int toKill = KillCam.Local != null ? KillCam.Local.punchesToKill : 3;
                if (hits < toKill)
                {
                    return;
                }
                Die(Vector3.zero, false);
                return;
            }
            wounds.AddSoon(section, point, true);
        }
    }

    private void Stabbed(GoreSection section, Vector3 point, float radius, byte does)
    {
        if (mode != Mode.Gone)
        {
            if (tearing == null)
            {
                return;
            }
            GillaWounds wounds = GillaWounds.On(tearing);
            if (wounds != null)
            {
                wounds.Add(section, point, radius);
            }
            if (does != 0)
            {
                hits++;
                if (does != 2)
                {
                    int toKill = KillCam.Local != null ? KillCam.Local.punchesToKill : 3;
                    if (hits < toKill)
                    {
                        return;
                    }
                }
                Die(Vector3.zero, false);
            }
        }
    }

    private void Torn(ApePart part)
    {
        if (mode != Mode.Gone && tearing != null)
        {
            if (!tearing.IsTorn(part))
            {
                tearing.Tear(part, Vector3.up * 2f + UnityEngine.Random.insideUnitSphere);
                if (part == (ApePart)0)
                {
                    Die(Vector3.zero, false);
                }
            }
        }
    }

    private void Ripped(PlayerRef by, byte seed, bool plusInRight, Quaternion rotRight, Quaternion rotLeft)
    {
        if (mode == Mode.Gone)
        {
            return;
        }
        Die(Vector3.zero, true);
        int id = BodyId;
        GillaCorpse.RipIt(by, id, seed, plusInRight, rotRight, rotLeft, true);
    }

    private void Blown(Vector3 velocity, byte torn, bool apart, byte seed)
    {
        if (mode != Mode.Gone && !blown)
        {
            if (tearing != null)
            {
                blown = true;
                int id = BodyId;
                CallLater(() => InvokeBlownPart("Blown_b__0", torn, velocity, id, seed)); /* ? */
                CallLater(() => InvokeBlownPart("Blown_b__1", torn, velocity, id, seed)); /* ? */
                if (apart)
                {
                    CallLater(() => InvokeBlownPart("Blown_b__2", torn, velocity, id, seed)); /* ? */
                }
            }
        }
    }

    private void EatenBy(MonsterBrain brain)
    {
        if (mode == Mode.Standing && brain != null)
        {
            eater = brain;
            mode = Mode.Eaten;
            eatenAt = Time.time;
            biteAt = -1f;
        }
    }

    public static void BittenBy(MonsterBrain brain, bool arm)
    {
        GillaDummy[] dummies = All.ToArray();
        for (int i = 0; i < dummies.Length; i++)
        {
            GillaDummy d = dummies[i];
            if (d == null || d.mode != Mode.Eaten || d.eater != brain)
            {
                continue;
            }
            if (arm)
            {
                if (d.tearing != null && !d.tearing.IsTorn((ApePart)2))
                {
                    d.tearing.Tear((ApePart)2, Vector3.up * 2f);
                }
            }
            else if (d.biteAt < 0f)
            {
                d.biteAt = Time.time;
                if (d.tearing != null && !d.tearing.IsTorn((ApePart)0))
                {
                    d.tearing.Tear((ApePart)0, Vector3.up * 2f);
                }
            }
        }
    }

    private void Die(Vector3 velocity, bool quiet)
    {
        if (mode == Mode.Gone)
        {
            return;
        }
        mode = Mode.Gone;
        goneAt = Time.time;
        if (tearing != null)
        {
            int id = BodyId;
            byte tornMask = NetworkApe.TornMask(tearing);
            GillaCorpse corpse = GillaCorpse.Make(tearing, id, tornMask, velocity, quiet);
            GillaModeRound round = GillaModeRound.Current;
            if (corpse != null)
            {
                if (round == null || !round.Live || round.Phase != 1)
                {
                    corpse.GoIn(GillaCorpse.OutOfHunt);
                }
            }
            tearing.gameObject.SetActive(false);
        }
        All.Remove(this);
    }

    private void LateUpdate()
    {
        if (paintPending && colour != null)
        {
            paintPending = false;
            colour.SetColor(colour.furColor);
        }
        if (mode == Mode.Gone)
        {
            if (Mine && Time.time - goneAt > GoneAfter)
            {
                if (!Live)
                {
                    Destroy(gameObject);
                    return;
                }
                Runner.Despawn(Object);
            }
            return;
        }
        if (head == null || leftHand == null || rightHand == null)
        {
            return;
        }
        if (mode == Mode.Held)
        {
            if (!Hold())
            {
                mode = Mode.Standing;
                Stand();
                return;
            }
        }
        if (mode == Mode.Eaten && !InJaws())
        {
            Die(Vector3.zero /* ? */, false);
        }
        if (mode == Mode.Standing)
        {
            Stand();
        }
    }

    private void Stand()
    {
        float t = Time.time + phase;
        float eyes = eyeHeight + Mathf.Sin(t * 1.6f) * 0.012f;
        Quaternion sway = Quaternion.Euler(Mathf.Sin(t * 0.7f) * 2f, Mathf.Sin(t * 0.37f) * 4f, 0f);
        Vector3 eyePos = standAt + Vector3.up * eyes;
        head.SetPositionAndRotation(eyePos, standFacing * sway);
        float breathe = Mathf.Sin(t * 1.6f + 0.6f);
        Vector3 left = standFacing * new Vector3(-0.5f, breathe * 0.015f - 0.45f, 0.2f);
        leftHand.SetPositionAndRotation(eyePos + left, standFacing);
        Vector3 right = standFacing * new Vector3(0.5f, -0.45f - breathe * 0.015f, 0.2f);
        rightHand.SetPositionAndRotation(eyePos + right, standFacing);
    }

    private bool Hold()
    {
        Transform hand;
        Transform holderHead;
        if (MonsterPlayer.Local != null && (!Live || holder == Runner.LocalPlayer))
        {
            hand = null;
            MethodInfo holdPoint = typeof(MonsterPlayer).GetMethod("HoldPoint", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[] { typeof(bool) }, null); /* ? */
            if (holdPoint != null)
            {
                hand = holdPoint.Invoke(MonsterPlayer.Local, new object[] { holderRight }) as Transform;
            }
            holderHead = KillCam.Local != null ? KillCam.Local.head : null;
        }
        else
        {
            NetworkApe ape = NetworkApe.Find(holder);
            holderHead = null;
            hand = null;
            if (ape != null)
            {
                hand = ape.HandOf(holderRight);
                holderHead = ape.head;
            }
        }
        bool missing = hand == null;
        if (!missing)
        {
            Vector3 eyes = hand.position + heldEyes;
            Vector3 flat;
            if (holderHead != null)
            {
                flat = Vector3.ProjectOnPlane(holderHead.position - eyes, Vector3.up);
            }
            else
            {
                flat = Vector3.zero;
            }
            Quaternion facing;
            if (flat.sqrMagnitude <= 0.0001f)
            {
                facing = standFacing;
            }
            else
            {
                facing = Quaternion.LookRotation(flat, Vector3.up);
            }
            Hang(eyes, facing);
        }
        return !missing;
    }

    private bool InJaws()
    {
        if (eater == null)
        {
            return false;
        }
        if (eater.grabSocket == null)
        {
            return false;
        }
        if (biteAt >= 0f && Time.time - biteAt > 0.6f)
        {
            return false;
        }
        if (Time.time - eatenAt > eater.killSeconds)
        {
            return false;
        }
        Vector3 eyes = eater.grabSocket.position;
        Transform face = eater.face != null ? eater.face : eater.transform;
        Vector3 look = face.position - eyes;
        Quaternion facing;
        if (look.sqrMagnitude <= 0.0001f)
        {
            facing = standFacing;
        }
        else
        {
            facing = Quaternion.LookRotation(look, Vector3.up);
        }
        Hang(eyes, facing);
        return true;
    }

    private void Hang(Vector3 eyes, Quaternion facing)
    {
        head.SetPositionAndRotation(eyes, facing);
        leftHand.SetPositionAndRotation(eyes + facing * new Vector3(-0.35f, -0.6f, 0.05f), facing);
        rightHand.SetPositionAndRotation(eyes + facing * new Vector3(0.35f, -0.6f, 0.05f), facing);
    }

    private static Vector3 FloorUnder(Vector3 from)
    {
        RaycastHit hit;
        if (Physics.Raycast(from + Vector3.up * 0.1f, Vector3.down, out hit, 8f, Solid, QueryTriggerInteraction.Ignore))
        {
        }
        else
        {
            hit.point = from + Vector3.down * 1.2f;
        }
        return hit.point;
    }

    public static GillaDummy Nearest(Vector3 at, float reach, out float away)
    {
        away = reach;
        GillaDummy best = null;
        foreach (GillaDummy d in All)
        {
            if (d == null || d.mode != Mode.Standing || d.head == null)
            {
                continue;
            }
            Vector3 headPos = d.head.position;
            float y = d.standAt.y + 0.05f;
            if (y <= at.y)
            {
                y = at.y;
                if (headPos.y < at.y)
                {
                    y = headPos.y;
                }
            }
            float dx = at.x - headPos.x;
            float dz = at.z - headPos.z;
            float dist = Mathf.Sqrt(dz * dz + dx * dx + (at.y - y) * (at.y - y)) - 0.12f;
            float best1 = 0f;
            if (dist >= 0f)
            {
                best1 = dist;
            }
            Transform[] hands = new Transform[] { d.leftHand, d.rightHand };
            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i] != null)
                {
                    float h = Vector3.Distance(hands[i].position, at);
                    if (h <= best1)
                    {
                        best1 = h;
                    }
                }
            }
            if (best1 < away)
            {
                away = best1;
                best = d;
            }
        }
        return best;
    }

    public bool PartNear(Vector3 hand, float reach, out ApePart part, out float away)
    {
        part = ApePart.Head;
        away = reach;
        (ApePart, Transform)[] parts = new (ApePart, Transform)[]
        {
            (ApePart.Head, head),
            (ApePart.ArmL, leftHand),
            (ApePart.ArmR, rightHand)
        };
        bool found = false;
        for (int i = 0; i < parts.Length; i++)
        {
            ApePart p = parts[i].Item1;
            Transform t = parts[i].Item2;
            if (t == null || IsTorn(p))
            {
                continue;
            }
            float d = Vector3.Distance(t.position, hand);
            if (d < away)
            {
                found = true;
                away = d;
                part = p;
            }
        }
        return found;
    }

    public GillaDummy()
    {
        eyeHeight = 1.2f;
        biteAt = -1f;
        standFacing = Quaternion.identity;
    }

    static GillaDummy()
    {
        All = new List<GillaDummy>();
        Furs = new Color[]
        {
            new Color(0.85f, 0.32f, 0.1f, 1f),
            new Color(0.2f, 0.45f, 0.95f, 1f),
            new Color(0.75f, 0.1f, 0.12f, 1f),
            new Color(0.25f, 0.7f, 0.2f, 1f),
            new Color(0.55f, 0.2f, 0.8f, 1f),
            new Color(0.95f, 0.75f, 0.1f, 1f),
            new Color(0.1f, 0.65f, 0.7f, 1f),
            new Color(0.9f, 0.9f, 0.9f, 1f)
        };
    }
}
