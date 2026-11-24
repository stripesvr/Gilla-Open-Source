// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gilla/Monster Catalog")]
public class MonsterCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string name;
        [Tooltip("Its button on the stands.")]
        public string label;
        public GameObject prefab;
        [Tooltip("Its eyes' height standing, at the size it's played (m): a monster player's view is put here.")]
        public float eyeHeight = 1.6f;
        [Tooltip("How far it reaches from its shoulder, at the size it's played (m).")]
        public float reach = 1f;
        public float walkSpeed = 1f;
        public float runSpeed = 4f;
        [Tooltip("Played by a player: moves on the thumbstick (it has legs) - else it crawls on its arms, the ape's way, at its size.")]
        public bool legs;
        [Tooltip("Played by a player: picks players up (and tears them apart) - else it kills with a touch.")]
        public bool grabs;
        [Tooltip("Its body without its head, neck, jaw and eyes, for the player wearing it: their eyes are in its head (baked by Build Monster Prefabs - an imported mesh can't be cut at runtime on the Quest).")]
        public Mesh firstPersonMesh;
        [Tooltip("Which way each palm faces, in its hand bone's own space (left, right): its fingers curl toward it, and a player's hands turn the monster's to their controllers by it.")]
        public Vector3 palmL;
        [Tooltip("Which way each palm faces, in its hand bone's own space (left, right): its fingers curl toward it, and a player's hands turn the monster's to their controllers by it.")]
        public Vector3 palmR;
        [Tooltip("Its body is held up by its arms alone (legless, nothing else on the floor: the Grinner). Played, you're sized to its arms, not its eyes, so its hands reach yours, and its body hangs from your head, its eyes on yours as you crouch or rise.")]
        public bool hangs;
        [Tooltip("Played by a player: it goes on its arms and its arms are always theirs. It never walks or gallops by itself, and its stomps are their hands coming down on the floor, not its animation's.")]
        public bool onArms;
        [Tooltip("Its size played by a player, against the one that hunts you, which stays as it is (the user, 2026-11-03: \"can you make the grinner smaller when you're the monster?\"). eyeHeight and reach are at this size.")]
        public float playedSize = 1f;
    }

    public Entry[] monsters = new Entry[0];
    private static MonsterCatalog loaded;

    public static MonsterCatalog Get()
    {
        if (loaded == null)
        {
            loaded = Resources.Load<MonsterCatalog>("MonsterCatalog");
        }
        return loaded;
    }

    public static Entry Of(int monster)
    {
        MonsterCatalog catalog = Get();
        bool ok = catalog != null;
        Entry entry = null;
        if (0 < monster && ok)
        {
            Entry[] arr = catalog.monsters;
            if (arr.Length < monster)
            {
                entry = null;
            }
            else
            {
                entry = arr[monster - 1];
            }
        }
        return entry;
    }
}
