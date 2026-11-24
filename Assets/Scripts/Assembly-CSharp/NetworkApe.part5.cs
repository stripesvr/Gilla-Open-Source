using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public partial class NetworkApe : NetworkBehaviour
{
    public static NetworkApe Find(PlayerRef player)
    {
        foreach (NetworkApe ape in All)
        {
            if (ape != null && ape.ownerRef == player)
            {
                return ape;
            }
        }
        return null;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    public void RPC_BreakGlass(int pane, Vector3 at, Vector3 push)
    {
        GillaGlass.BrokenByOther(pane, at, push);
    }

    private void Start()
    {
        started = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object != null && HasStateAuthority /* ? */)
        {
            CopyLocalRig();
            return;
        }
    }

    public override void Render()
    {
        if (Object != null && HasStateAuthority /* ? */)
        {
            return;
        }

        PollProof();
        GillaDoor.Offer(Doors, DoorsVersion, DoorsBy);
        GillaDoor.OfferMode(ModeShut, ModeBroken, ModeDoorsVersion, ModeDoorsBy);

        NetworkString<_32> id = CloudId;
        if (!id.Equals(knownId))
        {
            knownId = id;
            accountId = id.Value;
        }

        mutedHere = GillaMutes.IsMuted(accountId);
        AudioSource vo = VoiceOut;
        if (vo != null)
        {
            if (vo.mute != mutedHere)
            {
                vo.mute = mutedHere;
            }
        }

        face.externalLevel = mutedHere ? 0f : Voice;
        ShowFingers(FingersL, FingersR);

        if (tornHere != 0)
        {
            tornHere = (byte)(tornHere & ~Torn);
            if (Time.time - tornHereAt > 3f)
            {
                tornHere = 0;
            }
        }
        byte torn = (byte)(tornHere | Torn);
        if (appliedTorn != torn)
        {
            ShowTorn(torn);
        }

        if (FurRgb != appliedRgb)
        {
            ShowColour(FurRgb);
        }

        long key = (long)CosHead | ((long)CosFace << 8) | ((long)CosEyes << 16) | ((long)CosBody << 24)
                 | ((long)CosHands << 32) | ((long)CosBack << 40) | ((long)CosSkin << 48) | ((long)CosToy << 56);
        bool trusted = IdentityProven;

        if (key != weighedCos || GillaOwned.Version != weighedOwned || weighedTrusted != trusted
            || (someHidden && recheckAt <= Time.unscaledTime))
        {
            weighedCos = key;
            weighedTrusted = trusted;
            weighedOwned = GillaOwned.Version;
            recheckAt = Time.unscaledTime + 2f;
            said[0] = CosHead;
            said[1] = CosFace;
            said[2] = CosEyes;
            said[3] = CosBody;
            said[4] = CosHands;
            said[5] = CosBack;
            said[6] = CosSkin;
            said[7] = CosToy;
            int hidden = GillaOwned.Filter(said, AccountId, trusted, cos);
            someHidden = hidden > 0;
            long packed = 0;
            for (int i = 0; i < cos.Length; i++)
            {
                packed |= (long)cos[i] << ((i * 8) & 0x38);
            }
            if (packed != appliedCos)
            {
                appliedCos = packed;
                if (wearer != null)
                {
                    wearer.Show(cos);
                }
            }
        }

        if (wearer != null)
        {
            wearer.HoldToy(ToyHeld, leftHand, rightHand);
        }

        if (appliedMonster != Monster)
        {
            ShowMonster(Monster);
        }

        float scale = 1f;
        if (Size > 0.01f)
        {
            scale = Size;
        }
        if (ape != null)
        {
            if (Mathf.Abs(ape.transform.localScale.x - scale) > 0.0001f)
            {
                ape.transform.localScale = scale * Vector3.one;
            }
        }

        if (monsterBody != null)
        {
            monsterBody.sizeBy = scale;
        }

        if (Monster != 0)
        {
            GillaRip.MakeOneMore();
        }

        if (hiddenTill > 0f && hiddenTill < Time.time)
        {
            if (!Caught)
            {
                hiddenTill = 0f;
                ape.SetActive(Monster == 0);
            }
        }

        if (seenHits != Hits)
        {
            if (Hits == 0)
            {
                punches = 0;
                GillaWounds.FinishAll();
                if (tearing != null)
                {
                    GillaWounds.On(tearing).Clear();
                }
            }
            seenHits = Hits;
        }

        if (nameTag != null)
        {
            NetworkString<_32> nick = Nick;
            if (!nick.Equals(shownNick))
            {
                shownNick = nick;
                nameTag.SetName(nick.Value);
            }
            byte title = Title;
            const System.Reflection.BindingFlags tagFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            Type tagType = typeof(NameTag);
            object tagTitle = tagType.GetField("title", tagFlags)?.GetValue(nameTag); /* ? */
            if (tagTitle == null || Convert.ToInt32(tagTitle) != title)
            {
                object tagShown = tagType.GetField("shown", tagFlags)?.GetValue(nameTag); /* ? */
                tagType.GetMethod("Draw", tagFlags, null, new Type[] { typeof(string), typeof(int) }, null)
                    ?.Invoke(nameTag, new object[] { tagShown as string, (int)title }); /* ? */
            }
            bool visible;
            if (Monster != 0 || hiddenTill > 0f)
            {
                visible = false;
            }
            else
            {
                visible = (Torn & 1) == 0;
            }
            nameTag.SetVisible(visible);
        }

        float p;
        if (Monster == 0)
        {
            p = MonsterPlayer.IsMonster ? 1.5f : 1f;
        }
        else
        {
            p = MonsterPlayer.IsMonster ? 1f : 0.7f;
        }

        bool noPitch = pitch == null;
        if (p != 1f && noPitch)
        {
            AudioSource src = GetComponentInChildren<AudioSource>(true);
            if (src != null)
            {
                pitch = src.gameObject.AddComponent<VoicePitch>();
            }
        }
        if (pitch != null)
        {
            pitch.pitch = p;
        }
    }

    public bool IdentityProven
    {
        get
        {
            if (ShiftAuth.IsLoggedIn)
            {
                if (!ShiftAuth.IsOffline && !proofProven && refusals > 1)
                {
                    return ProofsLookBroken;
                }
            }
            return true;
        }
    }

    public static bool ProofsLookBroken
    {
        get
        {
            if (proofsNo.Count < 3)
            {
                return false;
            }
            return proofsYes.Count < proofsNo.Count;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ForgetProofs()
    {
        proofsYes.Clear();
        proofsNo.Clear();
    }

    private void PollProof()
    {
        float now = Time.unscaledTime;
        if (nextProofPoll <= now && ShiftAuth.IsLoggedIn && !ShiftAuth.IsOffline)
        {
            nextProofPoll = now + 1f;
            string sig = AuthSig.Value;
            if (string.IsNullOrEmpty(sig))
            {
                proofProven = false;
                return;
            }
            string player = AuthPlayer.Value;
            string session = AuthSession.Value;
            string account = AccountId;
            string proof = string.Concat(new string[] { player, "|", session, "|", sig, "|", account });
            proofProven = proof == provenProof;
            if (proof != lastProof)
            {
                lastProof = proof;
                lastProofAt = now;
                refusals = 0;
            }
            else if (now - lastProofAt >= 3f
                     && (proof != verifiedProof || (!proofProven && !checkingProof && askAgainAt <= now)))
            {
                verifiedProof = proof;
                checkingProof = true;
                string nick = PlayerName;
                SessionInfo info = Runner.SessionInfo;
                string room = info != null ? info.Name : "";
                StartCoroutine(VerifyProof(player, session, sig, account, nick, room));
            }
        }
    }

    private void ShowMonster(byte monster)
    {
        appliedMonster = monster;
        if (monsterBody != null)
        {
            Destroy(monsterBody.gameObject);
        }
        MonsterCatalog.Entry entry = MonsterCatalog.Of(monster);
        ape.SetActive(entry == null);
        if (entry == null)
        {
            return;
        }
        monsterBody = MonsterAvatar.Create(entry, transform, head, leftHand, rightHand, false);
        monsterBody.fingerSourceL = fingersL;
        monsterBody.fingerSourceR = fingersR;
    }

    private NameTag MakeNameTag()
    {
        if (tagMaterial != null)
        {
            int layer = ape != null ? ape.layer : 0;
            return NameTag.Create(transform, head, tagMaterial, layer);
        }
        return null;
    }

    private void ShowColour(int rgb)
    {
        appliedRgb = rgb;
        Color c = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        if (!started)
        {
            color.furColor = c;
            return;
        }
        color.SetColor(c);
    }

    private void CopyLocalRig()
    {
        NetworkBootstrap rig = NetworkBootstrap.Instance;
        if (rig == null)
        {
            return;
        }

        head.SetPositionAndRotation(rig.head.position, rig.head.rotation);
        leftHand.SetPositionAndRotation(rig.leftHand.position, rig.leftHand.rotation);
        rightHand.SetPositionAndRotation(rig.rightHand.position, rig.rightHand.rotation);

        if (rig.localColor != null)
        {
            Color32 c = rig.localColor.furColor;
            FurRgb = (c.r << 16) | (c.g << 8) | c.b;
        }
        if (rig.mic != null)
        {
            Voice = rig.mic.Level;
        }
        if (rig.fingersL != null)
        {
            FingersL = PackFingers(rig.fingersL);
        }
        if (rig.fingersR != null)
        {
            FingersR = PackFingers(rig.fingersR);
        }
        if (rig.tearing != null)
        {
            Dismemberment[] copies = rig.tearing.copies;
            if (copies != null && copies.Length != 0)
            {
                Torn = TornMask(copies[0]);
            }
        }

        if (sentName != GillaName.Current)
        {
            sentName = GillaName.Current;
            Nick = sentName;
        }
        if (sentId != NetworkBootstrap.PlayerId)
        {
            sentId = NetworkBootstrap.PlayerId;
            CloudId = sentId;
        }
        if (sentAuthSig != ShiftAuth.MetaSig)
        {
            sentAuthSig = ShiftAuth.MetaSig;
            AuthPlayer = ShiftAuth.PlayerId ?? "";
            AuthSession = ShiftAuth.SessionId ?? "";
            AuthSig = sentAuthSig ?? "";
        }

        int level = GillaLevel.Level;
        if (level > 65534)
        {
            level = 65535;
        }
        LevelNow = (ushort)Mathf.Max(level, 0);

        Doors = GillaDoor.Mask;
        DoorsVersion = GillaDoor.Version;
        DoorsBy = GillaDoor.By;
        ModeShut = GillaDoor.ModeShut;
        ModeBroken = GillaDoor.ModeBroken;
        ModeDoorsVersion = GillaDoor.ModeVersion;
        ModeDoorsBy = GillaDoor.ModeBy;
        Monster = (byte)MonsterPlayer.Current;
        Size = MonsterPlayer.ApeSize;
        Title = (byte)NameTitles.Shown;

        bool caught;
        if (KillCam.Local != null && KillCam.Local.Busy)
        {
            caught = true;
        }
        else
        {
            caught = MonsterBrain.IgnoreLocal;
        }
        Caught = caught;

        if (KillCam.Local == null || KillCam.Local.HeldBy == null)
        {
            Holder = PlayerRef.None;
        }

        Hits = KillCam.Local != null ? (byte)KillCam.Local.Hits : (byte)0;
        Down = KillCam.Local != null && KillCam.Local.Knocked;

        CosHead = Cosmetics.In(CosmeticSlot.Head);
        CosFace = Cosmetics.In(CosmeticSlot.Face);
        CosEyes = Cosmetics.In(CosmeticSlot.Eyes);
        CosBody = Cosmetics.In(CosmeticSlot.Body);
        CosHands = Cosmetics.In(CosmeticSlot.Hands);
        CosBack = Cosmetics.In(CosmeticSlot.Back);
        CosSkin = Cosmetics.In(CosmeticSlot.Skin);
        CosToy = Cosmetics.In(CosmeticSlot.Toy);
        ToyHeld = GillaToys.NetState;
        NoPunches = !GillaPlayerPunching.On;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SoulYank(PlayerRef target)
    {
        SoulScythe.Yank(this, target);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    public void RPC_BlackHoleThrow(Vector3 from, Vector3 velocity, float size)
    {
        PocketBlackHole.ShowThrow(this, from, velocity, size);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BlackHoleOpen(Vector3 at)
    {
        PocketBlackHole.Open(this, at);
    }

    private static byte Byte(float v)
    {
        return (byte)(int)Math.Round(Mathf.Clamp01(v) * 255f);
    }

    private static int PackFingers(HandPoser p)
    {
        byte t = Byte(p.trigger);
        byte g = Byte(p.grip);
        byte th = Byte(p.thumbPress);
        return (t << 16) | (g << 8) | th;
    }

    public static byte TornBit(ApePart part)
    {
        return (byte)(1 << ((int)part & 0x1f));
    }

    public static byte TornMask(Dismemberment d)
    {
        int mask = 0;
        foreach (ApePart part in Enum.GetValues(typeof(ApePart)))
        {
            if (d.IsTorn(part))
            {
                mask |= 1 << ((int)part & 0x1f);
            }
        }
        return (byte)mask;
    }

    private void ShowFingers(int left, int right)
    {
        Unpack(left, fingersL);
        Unpack(right, fingersR);

        static void Unpack(int packed, HandPoser p)
        {
            if (p == null)
            {
                return;
            }
            p.trigger = ((packed >> 16) & 0xFF) / 255f;
            p.grip = ((packed >> 8) & 0xFF) / 255f;
            p.thumbPress = (packed & 0xFF) / 255f;
        }
    }

    private void ShowTorn(byte mask)
    {
        if (tearing == null)
        {
            return;
        }
        int prev = appliedTorn;
        appliedTorn = mask;

        bool reset = false;
        foreach (ApePart part in Enum.GetValues(typeof(ApePart)))
        {
            if (tearing.IsTorn(part))
            {
                reset |= ((mask >> ((int)part & 0x1f)) & 1) == 0;
            }
        }
        if (reset)
        {
            tearing.ResetAll();
        }

        bool spawn = tearing.spawnPieces;
        foreach (ApePart part in Enum.GetValues(typeof(ApePart)))
        {
            int bit = 1 << ((int)part & 0x1f);
            if ((bit & mask) != 0 && !tearing.IsTorn(part))
            {
                bool fresh;
                if (prev < 0)
                {
                    fresh = false;
                }
                else
                {
                    fresh = (prev & 0xff & bit) == 0;
                }
                Dismemberment d = tearing;
                d.spawnPieces = spawn && fresh;
                d.Tear(part, Fling((int)part));
            }
        }
        tearing.spawnPieces = spawn;
    }
}
