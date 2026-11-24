using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class NetworkApe : NetworkBehaviour
{
    public static NetworkApe Local { get; private set; }

    private bool Live
    {
        get
        {
            return Object != null && Object.Runner && Object.Runner.Exists(Object) /* ? */;
        }
    }

    public bool IsLocal
    {
        get
        {
            if (Live && Object != null && Object.IsValid /* ? */)
            {
                return Object.HasStateAuthority /* ? */;
            }
            return false;
        }
    }

    public PlayerRef Owner { get { return ownerRef; } }

    public bool IsMonster
    {
        get
        {
            if (Live)
            {
                return Monster != 0;
            }
            return false;
        }
    }

    public MonsterAvatar MonsterBody { get { return monsterBody; } }

    public bool Busy
    {
        get
        {
            if (Live)
            {
                return Caught;
            }
            return false;
        }
    }

    public GillaHeldApe Drawn { get { return drawn; } }

    public GillaLimp Limp { get { return limp; } }

    public bool DownSaid
    {
        get
        {
            if (Live)
            {
                return Down;
            }
            return false;
        }
    }

    public bool DownHere
    {
        get
        {
            if (DownSaid) return true;
            if (limp != null) return limp.IsDown;
            return false;
        }
    }

    public bool LetGoHere
    {
        get
        {
            if (drawn != null) return drawn.Falling /* ? */;
            return false;
        }
    }

    public bool IsTorn(int part)
    {
        if (Live)
        {
            byte torn = Torn;
            return ((tornHere | torn) & (1 << (part & 31))) != 0;
        }
        return false;
    }

    public Transform HolderHand
    {
        get
        {
            if (!(Object == null) && Live)
            {
                if (Holder != PlayerRef.None && Holder != Runner.LocalPlayer)
                {
                    NetworkApe holderApe = Find(Holder);
                    if (holderApe != null)
                    {
                        return holderApe.HandOf(HolderRight);
                    }
                }
            }
            return null;
        }
    }

    public Vector3 EyesFromHolder
    {
        get
        {
            if (Live)
            {
                return HolderEyes;
            }
            return Vector3.up * 0.36f;
        }
    }

    public Transform HandOf(bool right)
    {
        Transform t = null;
        if (monsterBody != null)
        {
            t = monsterBody.Grip(right);
        }
        if (!(t != null))
        {
            t = right ? rightHand : leftHand;
        }
        return t;
    }

    public float VoiceLevel
    {
        get
        {
            if (Live) return Voice;
            return 0f;
        }
    }

    public int Punches { get { return punches; } }

    public bool SaysNoToPunches
    {
        get
        {
            if (Live) return NoPunches;
            return false;
        }
    }

    public Vector3 MiddleFromEyes
    {
        get
        {
            ApeAvatarDriver d = Driver;
            Transform neck = null;
            if (d != null)
            {
                neck = null;
                if (d.headBone != null)
                {
                    neck = d.headBone.parent;
                }
            }
            Transform chest = null;
            if (neck != null)
            {
                chest = neck.parent;
            }
            if (!(chest == null) && !(d.eyeL == null) && !(d.eyeR == null))
            {
                return (neck.position + chest.position - d.eyeL.position - d.eyeR.position) * 0.5f;
            }
            float over = KillCam.Local != null ? KillCam.Local.heldEyesOver : 0.36f;
            return Vector3.down * over;
        }
    }

    public ApeAvatarDriver Driver
    {
        get
        {
            if (!(driver != null) && !(ape == null))
            {
                driver = ape.GetComponentInChildren<ApeAvatarDriver>(true);
                return driver;
            }
            return driver;
        }
    }

    public ApeAvatarDriver AvatarDriver
    {
        get
        {
            if (!(driver != null) && !(ape == null))
            {
                driver = ape.GetComponentInChildren<ApeAvatarDriver>(true);
                return driver;
            }
            return driver;
        }
    }

    public int PlayerId { get { return owner; } }

    public string PlayerName
    {
        get
        {
            if (Live)
            {
                return Nick.Value;
            }
            return "";
        }
    }

    public string AccountId
    {
        get
        {
            if (Live)
            {
                return CloudId.Value;
            }
            return "";
        }
    }

    public int PlayerLevel
    {
        get
        {
            if (Live) return LevelNow;
            return 0;
        }
    }

    public Color32 FurColour
    {
        get
        {
            int rgb = 0;
            if (Live)
            {
                rgb = FurRgb;
            }
            return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        }
    }

    public AudioSource VoiceOut
    {
        get
        {
            if (voiceOut == null && head != null)
            {
                Transform t = head.Find("Voice");
                if (t != null)
                {
                    voiceOut = t.GetComponent<AudioSource>();
                }
            }
            return voiceOut;
        }
    }

    public bool MutedHere { get { return mutedHere; } }

    public override void Spawned()
    {
        owner = Object.StateAuthority.PlayerId;
        ownerRef = Object.StateAuthority;
        name = "NetworkApe_Player" + owner.ToString();
        All.Add(this);
        tornFrom = PartsThrownBy();
        if (tornFrom != null)
        {
            // NOTE: CS0123 here originates in the other partial file. Per metadata the declaration must be
            // private void OnPartTorn(ApePart part, GameObject piece); correct the signature there.
            // This subscription is already correct and is intentionally left unchanged.
            tornFrom.PartTorn += OnPartTorn;
        }
        if (Object == null || !Object.IsValid /* ? */ || !Object.HasStateAuthority /* ? */)
        {
            GillaRip.Prepare(tearing);
            face.playerId = owner + 100;
            drawn = gameObject.AddComponent<GillaHeldApe>();
            limp = gameObject.AddComponent<GillaLimp>();
            RemoteHandHits hits = gameObject.AddComponent<RemoteHandHits>();
            hits.left = leftHand /* ? */;
            hits.right = rightHand /* ? */;
            GameObject wearOn = tearing != null ? tearing.gameObject : ape;
            wearer = wearOn.AddComponent<CosmeticWearer>();
            nameTag = MakeNameTag();
            if (nameTag != null)
            {
                nameTag.wearer = wearer;
            }
            Debug.Log(string.Format("[Network] player {0} is here", owner));
            return;
        }
        Local = this;
        ape.SetActive(false);
        GillaDoor.NewRoom();
        CopyLocalRig();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
        if (tornFrom != null)
        {
            tornFrom.PartTorn -= OnPartTorn;
        }
        if (Local == this)
        {
            Local = null;
        }
        if (monsterBody != null)
        {
            Destroy(monsterBody.gameObject);
        }
        if (Object == null || !Object.IsValid /* ? */ || !Object.HasStateAuthority /* ? */)
        {
            Debug.Log(string.Format("[Network] player {0} left", owner));
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Grab(PlayerRef holder, NetworkBool rightHand, Vector3 eyesFromHand)
    {
        NetworkApe holderApe = Find(holder);
        if (holderApe == null) return;
        if (KillCam.Local == null) return;
        if (MonsterPlayer.IsMonster) return;
        if (GillaMapVote.OutOfHunt) return;
        Vector3 eyes = Vector3.ClampMagnitude(eyesFromHand, 2.5f) /* ? */;
        KillCam cam = KillCam.Local;
        Transform hand = holderApe.HandOf(rightHand);
        Transform face = holderApe.head;
        if (!cam.BeginHeld(hand, face, new Vector3?(eyes)))
        {
            return;
        }
        Holder = holder;
        HolderRight = rightHand;
        HolderEyes = eyes;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Release(Vector3 at, Vector3 velocity)
    {
        if (KillCam.Local != null && KillCam.Local.HeldBy != null)
        {
            KillCam cam = KillCam.Local;
            Vector3 v = Vector3.ClampMagnitude(velocity, 16f);
            cam.EndHeld(at, v);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Tear(byte part)
    {
        if (Object == null || !Object.IsValid /* ? */ || !Object.HasStateAuthority /* ? */)
        {
            GillaWounds.ThenAll(() => RPC_Tear_b__272_0(part));
            return;
        }
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (part == 0)
        {
            if (KillCam.Local != null)
            {
                KillCam.Local.Die(true);
            }
            return;
        }
        if (!(rig != null)) return;
        if (!(rig.tearing != null)) return;
        GillaWounds.ThenAll(() => RPC_Tear_b__272_1(part, rig));
    }

    private void TearHere(ApePart part)
    {
        if (tearing == null) return;
        if (tearing.IsTorn(part)) return;
        tornHere = (byte)(tornHere | (byte)(1 << ((int)part & 31)));
        tornHereAt = Time.time;
        Dismemberment d = tearing;
        Vector3 impulse = Fling((int)part) /* ? */;
        d.Tear(part, impulse);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Punch(byte section, Vector3 point)
    {
        PunchLanded(section, point);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PunchDown(byte section, Vector3 point, Vector3 going)
    {
        if (Object == null || !Object.IsValid /* ? */ || !Object.HasStateAuthority /* ? */)
        {
            if (limp != null)
            {
                limp.Begin(going);
            }
            if (drawn != null)
            {
                drawn.NotFalling();
            }
        }
        else
        {
            if (KillCam.Local != null)
            {
                KillCam.Local.KnockDown(going, -1f);
            }
        }
        PunchLanded(section, point);
    }
}
