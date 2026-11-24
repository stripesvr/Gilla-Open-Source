// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public class MonsterGore : ScriptableObject
{
    [Serializable]
    public class Limb
    {
        public GoreSection section;
        public string bone;
        public Vector3 from;
        public Vector3 to;
        public float radius;
    }

    [Serializable]
    public class Arm
    {
        public GoreSection section;
        public string upper;
        public string lower;
        public string hand;
        [Tooltip("The middle of the arm where it comes off, in the body's coordinates.")]
        public Vector3 cut;
        [Tooltip("The way along the arm there, away from the body.")]
        public Vector3 way;
        public float radius;
        [Tooltip("How far out from its body's middle it has to hang to clear what's below its shoulder (its hips, its legs).")]
        public float clear;
        [Tooltip("What's past that, skinned on the same bones, flesh filling the end of it: baked as it's posed, it's the piece that's thrown.")]
        public Mesh piece;
        [Tooltip("Which of the monster's materials each of `piece`'s submeshes is; its last is the flesh.")]
        public int[] slots = new int[0];
    }

    public Mesh mesh;
    public float units = 1f;
    [Tooltip("Which section each of `mesh`'s points is in (a GoreSection).")]
    public byte[] sectionOf = new byte[0];
    public Limb[] limbs = new Limb[0];
    public Arm[] arms = new Arm[0];
    public GillaGoreAssets.Piece[] pieces = new GillaGoreAssets.Piece[0];
    [Tooltip("Gilla/MonsterWounded, with its flesh and noise photos: a wounded monster's skin is a copy of this.")]
    public Material wound;
    public Material flesh;
    [Tooltip("It has legs (or it's on its arms, and has none).")]
    public bool legs;
}
