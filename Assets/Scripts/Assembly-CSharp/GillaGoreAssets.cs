// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public class GillaGoreAssets : ScriptableObject
{
    [Serializable]
    public class Piece
    {
        [Tooltip("The bone it goes on.")]
        public string bone;
        public GoreSection section;
        public Mesh mesh;
        public Material material;
        [Tooltip("How much of a wound's radius it's gone from, in the middle of it. Past that, it's left standing in the hole.")]
        public float gone;
    }

    [Tooltip("Gilla/ApeWounded, with its flesh and noise photos: a wounded body's fur is a copy of this.")]
    public Material wound;
    [Tooltip("Gilla/GoreInside, its bone photo: what the bones are drawn with, on each wounded body a copy of it.")]
    public Material bone;
    public Piece[] pieces;
    [Tooltip("The bits that fly out of a punch.")]
    public Mesh chunk;
    public Material chunkMaterial;
    [Tooltip("The chips of bone that fly out with them.")]
    public Material chipMaterial;
    [Tooltip("Gilla/GoreCrater, the flesh photo: a wound's walls.")]
    public Material lump;
    [Tooltip("Gilla/GoreSpray: the droplets of a punch's blood.")]
    public Material spray;
    [Tooltip("Gilla/GoreStrand: the strands still joining what's come apart (GillaStrands).")]
    public Material strand;
    [Tooltip("Gilla/BloodSplat: blood on the floors and walls (GillaSplatter).")]
    public Material splat;
    [Tooltip("Gilla/BloodSplat, lit by its own probes: blood on someone's fur (GillaBloodied).")]
    public Material stain;
    [Tooltip("The sound of a punch landing, one at random (Tools/gilla_punch_sounds.py).")]
    public AudioClip[] punchSounds;
}
