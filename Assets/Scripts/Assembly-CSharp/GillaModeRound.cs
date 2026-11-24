// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public partial class GillaModeRound : NetworkBehaviour
{
    public const byte Vote = 0;
    public const byte Hunt = 1;
    public const byte Over = 2;
    public const byte Alive = 1;
    public const byte Dead = 2;
    public const byte Hunter = 3;
    public const byte MonsterWins = 1;
    public const byte ApesWin = 2;
    public const int Maps = 3;

    public static float VoteSeconds = 20f;
    public static float HuntSeconds = 120f;
    public static float OverSeconds = 6f;
    public static int LeastPlayers = 2;

    [Networked]
    public byte Phase { get; set; }

    [Networked]
    public float VoteEnds { get; set; }

    [Networked]
    [Capacity(24)]
    public NetworkDictionary<PlayerRef, byte> Votes { get; }

    [Networked]
    public byte Map { get; set; }

    [Networked]
    public PlayerRef Monster { get; set; }

    [Networked]
    public int Round { get; set; }

    [Networked]
    public float HuntEnds { get; set; }

    [Networked]
    public float OverEnds { get; set; }

    [Networked]
    [Capacity(24)]
    public NetworkDictionary<PlayerRef, byte> Players { get; }

    [Networked]
    public byte Winner { get; set; }

    [Networked]
    [Capacity(24)]
    public NetworkDictionary<PlayerRef, byte> Turns { get; }

    public static GillaModeRound Current { get; private set; }

    public bool Live
    {
        get
        {
            if (Object == null)
            {
                return false;
            }
            NetworkObject obj = Object;
            if (!obj.Runner)
            {
                return false;
            }
            return obj.Runner.Exists(obj);
        }
    }

    public float Now
    {
        get
        {
            if (Runner != null)
            {
                return Runner.SimulationTime;
            }
            return 0f;
        }
    }

    public bool Enough
    {
        get
        {
            if (Runner != null)
            {
                return Runner.ActivePlayers.Count() >= LeastPlayers;
            }
            return false;
        }
    }

    public override void Spawned()
    {
        Current = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Vote(byte map, RpcInfo info = default)
    {
        if (map < 3 && Phase == Vote)
        {
            Votes.Set(info.Source, map);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Out(RpcInfo info = default)
    {
        if (Phase == Hunt)
        {
            byte state;
            if (Players.TryGet(info.Source, out state))
            {
                if (state == Hunter)
                {
                    End(ApesWin);
                }
                else
                {
                    Players.Set(info.Source, Dead);
                }
            }
        }
    }

    public ValueTuple<int, int> Apes()
    {
        int apes = 0;
        int alive = 0;
        foreach (KeyValuePair<PlayerRef, byte> kv in Players)
        {
            if (kv.Value != Hunter)
            {
                apes++;
                if (kv.Value == Alive)
                {
                    alive += InRoom(kv.Key) ? 1 : 0;
                }
            }
        }
        return new ValueTuple<int, int>(apes, alive);
    }

    public bool IsOut(PlayerRef p)
    {
        bool result = false;
        if (Live)
        {
            byte state;
            if (Players.TryGet(p, out state))
            {
                result = state == Dead;
            }
            else
            {
                result = false;
            }
        }
        return result;
    }

    public int VotesFor(int map)
    {
        return Votes.Count(kv => map == kv.Value && InRoom(kv.Key));
    }

    private bool InRoom(PlayerRef p)
    {
        if (Runner != null)
        {
            return Runner.ActivePlayers.Contains(p);
        }
        return false;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid)
        {
            return;
        }
        if (!Object.HasStateAuthority)
        {
            return;
        }
        byte phase = Phase;
        if (phase == Over)
        {
            if (Now < OverEnds)
            {
                return;
            }
            Phase = Vote;
            Players.Clear();
            Votes.Clear();
            VoteEnds = 0f;
            return;
        }
        if (phase == Hunt)
        {
            ValueTuple<int, int> counts = Apes();
            byte winner;
            if (InRoom(Monster))
            {
                if (counts.Item2 == 0 && counts.Item1 > 0)
                {
                    winner = MonsterWins;
                    goto Finish;
                }
                if (Now < HuntEnds)
                {
                    return;
                }
            }
            winner = ApesWin;
        Finish:
            Winner = winner;
            OverEnds = Now + OverSeconds;
            Phase = Over;
            return;
        }
        if (phase != Vote)
        {
            return;
        }
        if (!Enough)
        {
            VoteEnds = 0f;
            return;
        }
        if (VoteEnds > 0f)
        {
            if (Now < VoteEnds)
            {
                return;
            }
            Begin();
            return;
        }
        if (!Votes.Any(FixedUpdateNetwork_b__69_0) /* ? */)
        {
            return;
        }
        VoteEnds = Now + VoteSeconds;
    }

    private void Begin()
    {
        List<PlayerRef> everyone = Runner.ActivePlayers.ToList();
        int[] counts = new int[3];
        foreach (KeyValuePair<PlayerRef, byte> kv in Votes)
        {
            int map = kv.Value;
            if (map < 3 && everyone.Contains(kv.Key))
            {
                counts[map]++;
            }
        }
        Votes.Clear();
        VoteEnds = 0f;
        if (everyone.Count != 0)
        {
            int most = counts.Max();
            List<int> best = Enumerable.Range(0, 3).Where(i => counts[i] == most).ToList();
            Map = (byte)best[UnityEngine.Random.Range(0, best.Count)];
            Monster = PickMonster(everyone);
            Players.Clear();
            foreach (PlayerRef p in everyone)
            {
                Players.Set(p, p == Monster ? Hunter : Alive);
            }
            Winner = 0;
            HuntEnds = Now + HuntSeconds;
            Round = Round + 1;
            Phase = Hunt;
        }
    }

    private PlayerRef PickMonster(List<PlayerRef> everyone)
    {
        HashSet<PlayerRef> hadTurn = new HashSet<PlayerRef>();
        foreach (KeyValuePair<PlayerRef, byte> kv in Turns)
        {
            if (kv.Value != 0)
            {
                hadTurn.Add(kv.Key);
            }
        }
        PlayerRef pick = Draw(everyone, hadTurn, Monster, n => UnityEngine.Random.Range(0, n));
        Turns.Clear();
        foreach (PlayerRef p in hadTurn)
        {
            Turns.Set(p, 1);
        }
        return pick;
    }
}
