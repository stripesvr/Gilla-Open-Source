using System;
using System.Reflection;
using UnityEngine;
using Fusion;

public partial class NetworkApe : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Shove(Vector3 going)
    {
        if (MonsterPlayer.ApePunchesOffHere)
        {
            return;
        }
        if (Object != null && Object.HasStateAuthority /* ? */)
        {
            KillCam cam = KillCam.Local;
            if (cam == null)
            {
                return;
            }
            if (cam.Knocked)
            {
                return;
            }
            if (!cam.KnockDown(going, cam.shoveDownSeconds))
            {
                return;
            }
        }
        else
        {
            if (DownHere)
            {
                return;
            }
            if (limp != null)
            {
                limp.Begin(going);
            }
            if (drawn != null)
            {
                drawn.NotFalling();
            }
        }
        if (Gore != null)
        {
            AudioClip[] clips = Gore.punchSounds;
            if (clips != null && clips.Length != 0 && head != null)
            {
                Vector3 at = head.position + UnityEngine.Random.insideUnitSphere * 0.3f;
                Sfx.Play(clips, at, 1f, 0.93f, 1.07f);
            }
        }
    }

    private static GillaGoreAssets Gore
    {
        get
        {
            if (gore != null)
            {
                return gore;
            }
            gore = Resources.Load<GillaGoreAssets>("GillaGore");
            return gore;
        }
    }

    public void PunchLanded(byte section, Vector3 point)
    {
        punches++;
        if (Object == null || !Object.HasStateAuthority /* ? */)
        {
            if (tearing != null)
            {
                GillaWounds wounds = GillaWounds.On(tearing);
                wounds.Add((GoreSection)section, point, -1f);
                typeof(GillaWounds).GetMethod("Burst", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(wounds, new object[] { (GoreSection)section, point }); /* ? */
                if (!IsMonster && KillCam.Local != null)
                {
                    if (KillCam.Local.punchesToKill <= punches)
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
                        byte mask = tornFrom != null ? TornMask(tornFrom) : (byte)0;
                        BodyNow((byte)(Torn | mask), point /* ? */);
                        return;
                    }
                }
            }
        }
        else
        {
            NetworkBootstrap rig = NetworkBootstrap.Instance;
            if (rig != null && rig.tearing != null)
            {
                Dismemberment[] copies = rig.tearing.copies;
                if (copies != null)
                {
                    for (int i = 0; i < copies.Length; i++)
                    {
                        GillaWounds wounds = GillaWounds.On(copies[i]);
                        if (!(wounds == null))
                        {
                            wounds.Add((GoreSection)section, point, -1f);
                            typeof(GillaWounds).GetMethod("Burst", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(wounds, new object[] { (GoreSection)section, point }); /* ? */
                        }
                    }
                }
            }
            if (KillCam.Local != null && !MonsterPlayer.IsMonster)
            {
                KillCam cam = KillCam.Local;
                // dying check, Hits++, Buzz and Die on lethal hit are handled by the public Punched()
                cam.Punched(); /* ? */
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Stab(byte section, Vector3 point, float radius, byte does)
    {
        if (does != 0)
        {
            punches++;
        }
        if (Object == null || !Object.HasStateAuthority /* ? */)
        {
            if (tearing != null)
            {
                GillaWounds wounds = GillaWounds.On(tearing);
                wounds.Add((GoreSection)section, point, radius);
                if (!IsMonster && KillCam.Local != null)
                {
                    if (does != 2)
                    {
                        if (does != 1)
                        {
                            return;
                        }
                        if (punches < KillCam.Local.punchesToKill)
                        {
                            return;
                        }
                    }
                    DeadHere(point /* ? */);
                }
            }
        }
        else
        {
            NetworkBootstrap rig = NetworkBootstrap.Instance;
            if (rig != null && rig.tearing != null)
            {
                Dismemberment[] copies = rig.tearing.copies;
                if (copies != null)
                {
                    for (int i = 0; i < copies.Length; i++)
                    {
                        GillaWounds wounds = GillaWounds.On(copies[i]);
                        if (wounds != null)
                        {
                            wounds.Add((GoreSection)section, point, radius);
                        }
                    }
                }
            }
            if (!(KillCam.Local == null) && !MonsterPlayer.IsMonster)
            {
                if (does == 1)
                {
                    KillCam.Local.Punched();
                }
                else if (does == 2)
                {
                    KillCam.Local.Die(false);
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Wound(byte section, Vector3 point)
    {
        if (Object != null && Object.HasStateAuthority /* ? */)
        {
            WoundHere(section, point);
            return;
        }
        if (!(tearing == null))
        {
            GillaWounds.On(tearing).AddSoon((GoreSection)section, point, true);
        }
    }

    private static void WoundHere(byte s, Vector3 point)
    {
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (!(rig == null))
        {
            if (!(rig.tearing == null))
            {
                Dismemberment[] copies = rig.tearing.copies;
                if (copies != null)
                {
                    for (int i = 0; i < copies.Length; i++)
                    {
                        GillaWounds wounds = GillaWounds.On(copies[i]);
                        if (!(wounds == null))
                        {
                            wounds.AddSoon((GoreSection)s, point, true);
                        }
                    }
                }
            }
        }
    }

    public static void WoundYou(byte s, Vector3 point)
    {
        if (Local != null && Local.Object != null && Local.Object.Runner && Local.Object.IsValid /* ? */)
        {
            Local.RPC_Wound(s, point);
            return;
        }
        WoundHere(s, point);
    }

    public static void Crushed(Vector3 at, Vector3 way)
    {
        if (Local != null && Local.Object != null && Local.Object.Runner && Local.Object.IsValid /* ? */)
        {
            Local.RPC_Crushed(at, way);
            return;
        }
        CrushGore(at, way);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Crushed(Vector3 at, Vector3 way)
    {
        CrushGore(at, way);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_DoorPunch(byte door, byte hit, Vector3 at, Vector3 way)
    {
        GillaDoor d = GillaDoor.ModeDoor(door);
        if (d != null)
        {
            d.Punched(hit, at, way);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_HoldDoor(byte door, NetworkBool right, Vector3 pos, Quaternion rot)
    {
        GillaDoor d = GillaDoor.ModeDoor(door);
        if (d != null)
        {
            d.Hold(owner, right, pos, rot);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_LetGoDoor(byte door, Vector3 at, Quaternion rot, Vector3 velocity, Vector3 spin)
    {
        GillaDoor d = GillaDoor.ModeDoor(door);
        if (d != null)
        {
            d.LetGo(at, rot, velocity, spin);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaceDoor(byte door, Vector3 at, Quaternion rot)
    {
        GillaDoor d = GillaDoor.ModeDoor(door);
        if (d != null)
        {
            d.Rest(at, rot);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Squished(Vector3 at, Vector3 way)
    {
        KillCam cam = KillCam.Local;
        if (!(cam == null))
        {
            if (!(cam.head == null))
            {
                if (!MonsterPlayer.IsMonster && 3f <= Time.time - squashedAt)
                {
                    squashedAt = Time.time;
                    Crushed(cam.head.position, way);
                    Vector3 from = cam.head.position + UnityEngine.Random.insideUnitSphere * 0.3f;
                    Vector3 dir = way * 2.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                    GillaBlast.CrushYou(from, dir);
                }
            }
        }
    }

    private static void CrushGore(Vector3 at, Vector3 way)
    {
        Dismemberment body = null;
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (rig != null)
        {
            if (rig.tearing != null)
            {
                Dismemberment[] copies = rig.tearing.copies;
                if (copies == null || copies.Length == 0)
                {
                    body = null;
                }
                else
                {
                    body = copies[0];
                }
            }
        }
        if (body == null)
        {
            return;
        }
        Sfx.Play(body.tearSounds, at, 1f, 0.6f, 0.8f);
        Sfx.Play(body.tearSounds, at, 1f, 0.75f, 0.95f);
        Blood.Burst(at, way + Vector3.up * 0.35f, body.bloodMaterial, 1.4f);
        MethodInfo later = typeof(GillaWounds).GetMethod("Later", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); /* ? */
        later?.Invoke(null, new object[] { (Action)(() => CrushGore_b__293_0(at, way, body)) }); /* ? */
        later?.Invoke(null, new object[] { (Action)(() => CrushGoreLater1(at, way, body)) }); /* ? */
    }

    private static void CrushGoreLater1(Vector3 at, Vector3 way, Dismemberment body)
    {
        MethodInfo m = typeof(NetworkApe).GetMethod("CrushGore_b__293_1", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); /* ? */
        if (m == null)
        {
            return;
        }
        ParameterInfo[] ps = m.GetParameters();
        object[] args = new object[ps.Length];
        int vec = 0;
        for (int i = 0; i < ps.Length; i++)
        {
            Type t = ps[i].ParameterType;
            if (t == typeof(Vector3))
            {
                args[i] = vec++ == 0 ? at : way;
            }
            else if (typeof(Dismemberment).IsAssignableFrom(t))
            {
                args[i] = body;
            }
        }
        m.Invoke(m.IsStatic ? null : Local, args); /* ? */
    }
}
