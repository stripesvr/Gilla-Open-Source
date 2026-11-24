// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class GillaSplashGame : NetworkBehaviour
{
    public const byte Vote = 0;
    public const byte Start = 1;
    public const byte Race = 2;
    public const byte Results = 3;
    public const byte Over = 4;
    public const byte Racing = 1;
    public const byte Through = 2;
    public const byte Out = 3;
    public const byte Dead = 4;
    public const int Maps = 3;
    public const int Levels = 3;
    public const int MaxRounds = 7;
    public const int MaxPlatforms = 128;
    public static float VoteSeconds = 20f;
    public static float StartSeconds = 6f;
    public static float ResultsSeconds = 6f;
    public static float OverSeconds = 8f;
    public static float FirstRaceSeconds = 90f;
    public static float RaceSecondsLess = 10f;
    public static float LeastRaceSeconds = 40f;
    public static int DarkFromRound = 4;
    public static bool KeepGoing;
    public static bool RaceToTheClock;

    [Networked]
    public byte Phase { get; set; }
    [Networked]
    public float VoteEnds { get; set; }
    [Networked, Capacity(24)]
    public NetworkDictionary<PlayerRef, byte> Votes { get; }
    [Networked]
    public byte Map { get; set; }
    [Networked]
    public int Round { get; set; }
    [Networked]
    public int Starts { get; set; }
    [Networked]
    public float PhaseEnds { get; set; }
    [Networked]
    public NetworkBool Sinking { get; set; }
    [Networked, Capacity(24)]
    public NetworkDictionary<PlayerRef, byte> Players { get; }
    [Networked, Capacity(128)]
    public NetworkArray<float> SinkAt { get; }
    [Networked]
    public int Layouts { get; set; }
    [Networked]
    public int PlatformCount { get; set; }
    [Networked, Capacity(128)]
    public NetworkArray<int> Platforms { get; }
    [Networked]
    public int LayoutCheck { get; set; }

    public static GillaSplashGame Current { get; private set; }

    public bool Live
    {
        get
        {
            if (Object == null) return false;
            NetworkObject obj = Object;
            if (!obj.Runner) return false;
            return obj.Runner.Exists(obj) /* ? */;
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

    public static int LevelOf(int round)
    {
        if (round < 2) round = 1;
        return (round - 1) % 3;
    }

    public int Level
    {
        get
        {
            int r = Round;
            if (r < 2) r = 1;
            return (r - 1) % 3;
        }
    }

    public static float Dimness(int round)
    {
        int dark = DarkFromRound;
        if (round < dark)
        {
            if (dark - 1 == 1) return 0f;
            float t = ((float)round - 1f) / ((float)(dark - 1) - 1f);
            if (t < 0f) return 0f;
            if (1f < t) return 0.3f;
            return t * 0.3f + 0f;
        }
        else
        {
            if (dark == 7) return 0.55f;
            float t = ((float)round - (float)dark) / (7f - (float)dark);
            if (t < 0f) return 0.55f;
            if (1f < t) return 1f;
            return t * 0.45f + 0.55f;
        }
    }

    public static float RaceSeconds(int round)
    {
        float s = FirstRaceSeconds - RaceSecondsLess * (float)Mathf.Max(round - 1, 0);
        return Mathf.Max(LeastRaceSeconds, s);
    }

    public bool Dark
    {
        get
        {
            if (Round < DarkFromRound) return false;
            return Phase == 1 || Phase == 2 || Phase == 3;
        }
    }

    public bool Tied
    {
        get
        {
            if (Phase == 4)
            {
                return 1 < Count(2) + Count(1);
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
        if (map < 3 && Phase == 0)
        {
            Votes.Set(info.Source, map);
            if (VoteEnds <= 0f)
            {
                VoteEnds = Now + VoteSeconds;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Finished(RpcInfo info = default)
    {
        if (Phase == 2)
        {
            byte state;
            if (Players.TryGet(info.Source, out state) && state == 1)
            {
                Players.Set(info.Source, 2);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Died(RpcInfo info = default)
    {
        if (Phase == 2)
        {
            byte state;
            if (Players.TryGet(info.Source, out state) && state == 1)
            {
                Players.Set(info.Source, 4);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Stood(int platform)
    {
        if (Phase == 2)
        {
            bool sinking = Sinking;
            if ((uint)platform < 128 && sinking)
            {
                if (SinkAt[platform] <= 0f)
                {
                    SinkAt.Set(platform, Now);
                }
            }
        }
    }

    public bool InRace(PlayerRef p)
    {
        bool result = false;
        if (Phase == 2)
        {
            byte state;
            if (Players.TryGet(p, out state))
            {
                result = state == 1;
            }
            else
            {
                result = false;
            }
        }
        return result;
    }

    public int Count(byte part)
    {
        return Players.Count(kv => kv.Value == part && InRoom(kv.Key));
    }

    public int VotesFor(int map)
    {
        return Votes.Count(kv => kv.Value == map && InRoom(kv.Key));
    }

    private bool InRoom(PlayerRef p)
    {
        if (Runner != null)
        {
            return Runner.ActivePlayers.Contains(p);
        }
        return false;
    }
}
