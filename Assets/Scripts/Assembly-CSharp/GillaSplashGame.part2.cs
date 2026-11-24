// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;

public partial class GillaSplashGame : NetworkBehaviour
{
    static GillaSplashGame()
    {
        ResultsSeconds = 6f;
        OverSeconds = 8f;
        VoteSeconds = 20f;
        StartSeconds = 6f;
        FirstRaceSeconds = 90f;
        RaceSecondsLess = 10f;
        LeastRaceSeconds = 40f;
        DarkFromRound = 4;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null) return;
        if (!HasStateAuthority) return;

        byte phase = Phase;
        if (phase == Vote)
        {
            if (VoteEnds <= 0f) return;
            if (Now < VoteEnds) return;
            BeginGame();
            return;
        }
        if (phase == Start)
        {
            if (Now < PhaseEnds) return;
            Phase = Race;
            PhaseEnds = Now + RaceSeconds(Round);
            return;
        }
        if (phase == Race)
        {
            var times = GillaSinkPlatform.Times(Round);
            int count = PlatformCount;
            if (0 < count)
            {
                for (int i = 0; ; )
                {
                    if (0f < SinkAt[i])
                    {
                        float now = Now;
                        if (times.Item4 + 0.8f < now - SinkAt[i])
                        {
                            SinkAt.Set(i, 0f);
                        }
                    }
                    count = PlatformCount;
                    if (i >= 127) break;
                    i++;
                    if (i >= count) break;
                }
            }
            int racing = Count(Racing);
            if (racing == 0 && !RaceToTheClock)
            {
                EndRace();
                return;
            }
            if (Now < PhaseEnds) return;
            EndRace();
            return;
        }
        if (phase == Results)
        {
            if (Now < PhaseEnds) return;
            NextRound();
            return;
        }
        if (phase == Over)
        {
            if (Now < PhaseEnds) return;
            Phase = Vote;
            Players.Clear();
            Votes.Clear();
            VoteEnds = 0f;
            ClearSinking();
        }
    }

    private void BeginGame()
    {
        List<PlayerRef> active = Runner.ActivePlayers.ToList();
        int[] counts = new int[3];
        foreach (var kv in Votes)
        {
            if (kv.Value < 3 && active.Contains(kv.Key))
            {
                counts[kv.Value]++;
            }
        }
        Votes.Clear();
        VoteEnds = 0f;
        if (active.Count != 0)
        {
            int most = counts.Max();
            List<int> best = Enumerable.Range(0, 3).Where(m => counts[m] == most).ToList();
            Map = (byte)best[UnityEngine.Random.Range(0, best.Count)];
            Players.Clear();
            foreach (PlayerRef player in active)
            {
                Players.Set(player, Racing);
            }
            Round = 0;
            StartRound();
        }
    }

    private void StartRound()
    {
        Round = Round + 1;
        foreach (KeyValuePair<PlayerRef, byte> kv in Players.ToList())
        {
            if (kv.Value == Through)
            {
                Players.Set(kv.Key, Racing);
            }
        }
        Sinking = Round > 1;
        ClearSinking();
        LayOut();
        Phase = Start;
        PhaseEnds = Now + StartSeconds;
        Starts = Starts + 1;
    }

    private void LayOut()
    {
        GillaSplashDirector director = GillaSplashDirector.Instance;
        List<GillaSplashLayout.Spot> spots;
        if (director != null)
        {
            spots = GillaSplashLayout.Make(director.Halls, Map, Round, new System.Random());
        }
        else
        {
            spots = new List<GillaSplashLayout.Spot>();
        }
        int total = spots.Count;
        int n = total > 127 ? 128 : total;
        if (0 < total)
        {
            for (int i = 0; i != n; i++)
            {
                GillaSplashLayout.Spot spot = spots[i];
                Platforms.Set(i, GillaSplashLayout.Pack(spot)); /* ? */
            }
        }
        PlatformCount = n;
        Layouts = Layouts + 1;
        LayoutCheck = CheckNow();
    }

    public int CheckNow()
    {
        unchecked
        {
            uint hash = (uint)(Layouts * 16777619) ^ (uint)PlatformCount;
            int count = PlatformCount;
            if (0 < count)
            {
                int i = 0;
                do
                {
                    hash = ((uint)Platforms[i] ^ hash) * 16777619u;
                    count = PlatformCount;
                    if (i > 126) return (int)hash;
                    i++;
                } while (i < count);
            }
            return (int)hash;
        }
    }

    private void EndRace()
    {
        foreach (KeyValuePair<PlayerRef, byte> kv in Players.ToList())
        {
            if (kv.Value == Dead || kv.Value == Racing)
            {
                Players.Set(kv.Key, Out);
            }
        }
        Phase = Results;
        PhaseEnds = Now + ResultsSeconds;
    }

    private void NextRound()
    {
        int through = Count(Through);
        if (through != 0 && Round < MaxRounds && (through != 1 || KeepGoing))
        {
            StartRound();
            return;
        }
        Phase = Over;
        PhaseEnds = Now + OverSeconds;
    }

    private void ClearSinking()
    {
        for (int i = 0; i != MaxPlatforms; i++)
        {
            if (0f < SinkAt[i])
            {
                SinkAt.Set(i, 0f);
            }
        }
    }
}
