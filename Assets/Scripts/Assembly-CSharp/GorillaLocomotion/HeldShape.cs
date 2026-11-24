// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

namespace GorillaLocomotion
{
    public class HeldShape
    {
        public readonly Vector3[] at;
        public readonly float[] radius;
        public readonly float reach;

        public HeldShape(Vector3[] at, float[] radius)
        {
            this.at = at;
            this.radius = radius;
            for (int i = 0; i < at.Length; i++)
            {
                reach = Math.Max(reach, at[i].magnitude + radius[i]);
            }
        }
    }
}
