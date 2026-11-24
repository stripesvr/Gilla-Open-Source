// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public partial class GillaModeRound : NetworkBehaviour
{
    static GillaModeRound()
    {
        VoteSeconds = 20f;
        HuntSeconds = 120f;
        OverSeconds = 6f;
        LeastPlayers = 2;
    }

    public static PlayerRef Draw(List<PlayerRef> everyone, HashSet<PlayerRef> hadTurn, PlayerRef last, Func<int, int> roll)
    {
        bool fresh;
        List<PlayerRef> pool = Pool(everyone, hadTurn.Contains, last, out fresh);
        if (pool.Count == 0)
        {
            return default(PlayerRef);
        }
        int r = roll(pool.Count);
        int index = 0;
        if (r >= 0)
        {
            index = Mathf.Min(r, pool.Count - 1);
        }
        PlayerRef item = pool[index];
        if (fresh)
        {
            hadTurn.Clear();
        }
        hadTurn.RemoveWhere(p => !everyone.Contains(p)) /* ? */;
        hadTurn.Add(item);
        return item;
    }

    public static List<PlayerRef> Pool(IEnumerable<PlayerRef> everyone, Func<PlayerRef, bool> hadTurn, PlayerRef last, out bool fresh)
    {
        List<PlayerRef> source = everyone.ToList();
        List<PlayerRef> pool = source.Where(p => !hadTurn(p) && p != last /* ? */).ToList();
        fresh = pool.Count == 0;
        if (pool.Count == 0)
        {
            pool = source.Where(p => p != last /* ? */).ToList();
        }
        if (pool.Count != 0)
        {
            source = pool;
        }
        return source;
    }

    private bool HadTurn(PlayerRef p)
    {
        byte value;
        return Turns.TryGet(p, out value) && value != 0;
    }

    public float MonsterChance(PlayerRef p)
    {
        if (Runner == null)
        {
            return 0f;
        }
        bool fresh;
        List<PlayerRef> pool = Pool(Runner.ActivePlayers, HadTurn, Monster, out fresh);
        if (!pool.Contains(p))
        {
            return 0f;
        }
        return 1f / (float)pool.Count;
    }

    private void End(byte winner)
    {
        Winner = winner;
        OverEnds = Now + OverSeconds;
        Phase = Over;
    }

    private bool FixedUpdateNetwork_b__69_0(KeyValuePair<PlayerRef, byte> kv)
    {
        return InRoom(kv.Key);
    }
}
