// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class NetworkApe
{
    private Vector3 Fling(int part)
    {
        Transform t = head != null ? head : transform;
        Vector3 right = Vector3.ProjectOnPlane(t.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
        Vector3 dir = Vector3.up * 1.5f + Random.insideUnitSphere * 0.6f;
        if (part == 2)
        {
            dir = right * 2.5f + dir;
        }
        else if (part == 1)
        {
            dir = dir + right * -2.5f;
        }
        else
        {
            dir = dir + forward * 0.8f + Vector3.up * 2.5f;
        }
        return dir;
    }

    public NetworkApe()
    {
        appliedCos = -1;
        appliedRgb = -1;
        appliedTorn = -1;
        cos = new byte[8];
        said = new byte[8];
        weighedCos = -1;
        weighedOwned = -1;
        bodyLeftAt = -10f;
        rippedAt = -10f;
        bodyHereAt = -10f;
        accountId = "";
        squashedAt = -10f;
    }

    static NetworkApe()
    {
        All = new List<NetworkApe>();
        proofsYes = new HashSet<string>();
        proofsNo = new HashSet<string>();
    }

    private static void ShowFingers_Unpack(int packed, HandPoser p)
    {
        if (p == null)
        {
            return;
        }
        p.trigger = ((packed >> 16) & 0xff) / 255f;
        p.grip = ((packed >> 8) & 0xff) / 255f;
        p.thumbPress = (packed & 0xff) / 255f;
    }
}
