using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Fusion;

public partial class NetworkApe : NetworkBehaviour
{
    public static void TearYou(ApePart part)
    {
        tearsComing = (byte)(tearsComing | (byte)(1 << ((int)part & 31)));
        if (Local != null && Local.Object != null)
        {
            NetworkObject obj = Local.Object;
            if (obj.Runner && obj.Runner.IsRunning /* ? */)
            {
                Local.RPC_Tear((byte)part);
                return;
            }
        }
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (rig != null)
        {
            if (rig.tearing != null)
            {
                GillaWounds.ThenAll(() => TearYou_b__294_0(part, rig) /* ? */);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Kill()
    {
        if (KillCam.Local != null)
        {
            if (!MonsterPlayer.IsMonster)
            {
                KillCam.Local.Die(true);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Rip(PlayerRef by, byte seed, NetworkBool plusInRight, Quaternion rotRight, Quaternion rotLeft)
    {
        rippedAt = Time.time;
        GillaCorpse corpse = GillaCorpse.Find(owner);
        if (corpse == null || !corpse.Has(0) || 2f < Time.time - corpse.MadeAt)
        {
            byte torn;
            if (tornFrom != null)
            {
                torn = TornMask(tornFrom);
            }
            else
            {
                torn = 0;
            }
            if (!HasStateAuthority)
            {
                torn = (byte)(Torn | torn);
            }
            BodyHere(torn, Vector3.zero, true);
        }
        GillaCorpse.RipIt(by, owner, seed, plusInRight, rotRight, rotLeft, true);
        if (!HasStateAuthority)
        {
            if (ape != null)
            {
                hiddenTill = Time.time + 1.5f;
                ape.SetActive(false);
            }
        }
        else
        {
            bodyLeftAt = Time.time;
            if (KillCam.Local != null)
            {
                if (!MonsterPlayer.IsMonster)
                {
                    KillCam.Local.Die(false);
                    KillCam.Local.SendMessage("HideBody", SendMessageOptions.DontRequireReceiver) /* ? */;
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Slam(Vector3 at, Vector3 normal, Vector3 going)
    {
        if (IsMonster)
        {
            return;
        }
        if (tearing != null)
        {
            Sfx.Play(tearing.tearSounds, at, 1f, 0.75f, 0.95f);
        }
        if (!HasStateAuthority)
        {
            if (tearing != null)
            {
                if (tearing.bloodMaterial != null)
                {
                    Blood.Burst(at + normal * 0.05f, normal, tearing.bloodMaterial, 0.8f);
                }
            }
            DeadHere(going);
        }
        else
        {
            if (KillCam.Local != null)
            {
                if (!MonsterPlayer.IsMonster)
                {
                    KillCam.Local.Slammed(going);
                }
            }
        }
    }

    private void DeadHere(Vector3 going)
    {
        if (Time.time - bodyHereAt < 3f || Time.time - rippedAt < 3f)
        {
            return;
        }
        GillaCorpse corpse = GillaCorpse.Find(owner);
        if (corpse != null)
        {
            if (corpse.Has(0) && Time.time - corpse.MadeAt < 2f)
            {
                return;
            }
        }
        bodyHereAt = Time.time;
        byte mask;
        if (tornFrom != null)
        {
            mask = TornMask(tornFrom);
        }
        else
        {
            mask = 0;
        }
        BodyNow((byte)(Torn | mask), going);
    }

    public void LeaveBody()
    {
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (Time.time - bodyLeftAt < 1f)
        {
            return;
        }
        if (rig == null)
        {
            return;
        }
        if (rig.tearing == null)
        {
            return;
        }
        Dismemberment[] copies = rig.tearing.copies;
        if (copies == null)
        {
            return;
        }
        if (copies.Length == 0)
        {
            return;
        }
        bodyLeftAt = Time.time;
        Dismemberment d = rig.tearing.copies[0];
        KillCam cam = KillCam.Local;
        byte mask = TornMask(d);
        byte coming = tearsComing;
        tearsComing = 0;
        Vector3 velocity;
        if (cam != null)
        {
            velocity = cam.DeathVelocity;
        }
        else
        {
            velocity = Vector3.zero;
        }
        if (cam != null && cam.BlownApart)
        {
            int seed = GillaRip.PickSeed();
            RPC_BlownApart((byte)(coming | mask), velocity, (byte)seed);
            return;
        }
        RPC_Corpse((byte)(coming | mask), velocity);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Corpse(byte torn, Vector3 velocity)
    {
        if (3f <= Time.time - rippedAt && 3f <= Time.time - bodyHereAt)
        {
            GillaWounds.ThenAll(() => RPC_Corpse_b__301_0(torn, velocity) /* ? */);
        }
    }

    private void BodyNow(byte torn, Vector3 velocity)
    {
        if (limp != null && limp.IsDown)
        {
            GillaCorpse corpse = BodyHere(torn, limp.Going, false);
            if (corpse != null)
            {
                object way = typeof(GillaLimp).GetField("way", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(limp) /* ? */;
                typeof(GillaCorpse).GetMethod("FallsFlat", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(corpse, new object[] { way }) /* ? */;
            }
            limp.End(false);
        }
        else
        {
            BodyHere(torn, velocity, false);
        }
        if (!HasStateAuthority)
        {
            if (ape == null)
            {
                return;
            }
            hiddenTill = Time.time + 1.5f;
            ape.SetActive(false);
        }
        else
        {
            if (KillCam.Local == null)
            {
                return;
            }
            KillCam.Local.SendMessage("HideBody", SendMessageOptions.DontRequireReceiver) /* ? */;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BlownApart(byte torn, Vector3 velocity, byte seed)
    {
        if (3f <= Time.time - rippedAt)
        {
            void CallHelper(string name)
            {
                MethodInfo m = typeof(NetworkApe).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (m == null)
                {
                    return;
                }
                ParameterInfo[] ps = m.GetParameters();
                object[] args = new object[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    Type t = ps[i].ParameterType;
                    if (t == typeof(Vector3)) args[i] = velocity;
                    else if (t == typeof(NetworkApe)) args[i] = this;
                    else if (t == typeof(byte)) args[i] = ps[i].Name == "seed" ? seed : torn;
                    else args[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
                }
                m.Invoke(m.IsStatic ? null : this, args);
            }
            GillaWounds.ThenAll(() => CallHelper("RPC_BlownApart_b__303_0")) /* ? */;
            GillaWounds.ThenAll(() => CallHelper("RPC_BlownApart_b__303_1")) /* ? */;
        }
    }

    private GillaCorpse BodyHere(byte torn, Vector3 velocity, bool quiet)
    {
        GillaCorpse corpse = GillaCorpse.Make(tornFrom, owner, torn, velocity, quiet);
        GillaModeRound round = GillaModeRound.Current;
        if (corpse != null)
        {
            if (round != null)
            {
                if (round.Live && round.Phase == 1)
                {
                    return corpse;
                }
            }
            corpse.GoIn(GillaCorpse.OutOfHunt);
        }
        return corpse;
    }

    public Dismemberment PartsThrownBy()
    {
        if (!HasStateAuthority)
        {
            return tearing;
        }
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (rig != null)
        {
            if (rig.tearing != null)
            {
                Dismemberment[] copies = rig.tearing.copies;
                if (copies != null && copies.Length >= 1)
                {
                    for (int i = 0; i < copies.Length; i++)
                    {
                        Dismemberment d = copies[i];
                        if (d != null && d.spawnPieces)
                        {
                            return d;
                        }
                    }
                }
                return null;
            }
        }
        return null;
    }

    private void OnPartTorn(ApePart part, GameObject piece)
    {
        GillaCorpse.Torn(tornFrom, owner, (int)part, piece);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaceCorpse(int corpse, byte part, Vector3 at, Quaternion rot)
    {
        GillaCorpse.Place(corpse, part, at, rot);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_HoldCorpse(int corpse, byte part, NetworkBool right, Vector3 pos, Quaternion rot)
    {
        GillaCorpse.HoldIt(ownerRef, right, corpse, part, pos, rot);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_TearCorpse(int corpse, byte part, NetworkBool right, Quaternion rot)
    {
        GillaCorpse.TearOff(ownerRef, right, corpse, part, rot);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_RipCorpse(int corpse, byte seed, NetworkBool plusInRight, Quaternion rotRight, Quaternion rotLeft)
    {
        GillaCorpse.RipIt(ownerRef, corpse, seed, plusInRight, rotRight, rotLeft, true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_LetGoCorpse(NetworkBool right, Vector3 at, Quaternion rot, Vector3 velocity, Vector3 spin)
    {
        GillaCorpse.LetGo(ownerRef, right, at, rot, velocity, spin);
    }
}
