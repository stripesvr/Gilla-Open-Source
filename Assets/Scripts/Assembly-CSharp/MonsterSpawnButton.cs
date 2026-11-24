// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawnButton : MonoBehaviour
{
    [Tooltip("The monster to spawn; empty = clear the spawned ones.")]
    public GameObject prefab;
    public Transform[] hands;
    [Tooltip("Your head (the KillCam's): spawns are around it, in sight of it.")]
    public Transform head;
    public float radius = 0.13f;
    [Tooltip("How far from you it appears (m).")]
    public float minDistance = 3f;
    [Tooltip("How far from you it appears (m).")]
    public float maxDistance = 6f;
    private static readonly List<GameObject> Spawned = new List<GameObject>();
    private const int Mask = -1797;
    private float cooldown;
    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        cooldown -= Time.deltaTime;
        Transform t = transform;
        Vector3 scale = transform.localScale;
        t.localScale = Vector3.Lerp(scale, baseScale, 1f - Mathf.Exp(Time.deltaTime * -10f));
        if (cooldown <= 0f && hands != null)
        {
            for (int i = 0; i < hands.Length; i++)
            {
                Transform hand = hands[i];
                if (hand == null) continue;
                if ((hand.position - transform.position).sqrMagnitude <= radius * radius)
                {
                    Press();
                    return;
                }
            }
        }
    }

    public void Click()
    {
        if (cooldown > 0f) return;
        Press();
    }

    private void Press()
    {
        cooldown = 1f;
        transform.localScale = baseScale * 0.8f;
        if (prefab == null)
        {
            Clear();
            return;
        }
        Spawn();
    }

    private void Spawn()
    {
        RaycastHit hit;
        if (head == null) return;
        CharacterController cc = prefab.GetComponent<CharacterController>();
        float height = 2f;
        if (cc != null) height = cc.height;
        float capRadius = cc != null ? cc.radius : 0.3f;
        Vector3 headPos = head.position;
        float low = capRadius + 0.05f;
        float high = Mathf.Max(low, height - capRadius);
        for (int tries = 60; tries != 0; tries--)
        {
            Vector3 dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
            Vector3 probe = headPos + dir * Random.Range(minDistance, maxDistance);
            if (Physics.Linecast(headPos, probe, Mask, QueryTriggerInteraction.Ignore)) continue;
            if (!Physics.Raycast(probe, Vector3.down, out hit, 4f, Mask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.75f) continue;
            Vector3 point = hit.point;
            Vector3 eye = new Vector3(headPos.x, point.y + 0.4f, headPos.z);
            if (Physics.Linecast(eye, point + Vector3.up * 0.4f, Mask, QueryTriggerInteraction.Ignore)) continue;
            if (Physics.CheckCapsule(point + Vector3.up * low, point + Vector3.up * high, capRadius, Mask, QueryTriggerInteraction.Ignore)) continue;
            Vector3 look = new Vector3(headPos.x - point.x, 0f, headPos.z - point.z);
            Quaternion rot = look.sqrMagnitude <= 0.0001f ? Quaternion.identity : Quaternion.LookRotation(look);
            GameObject go = Instantiate(prefab, point + Vector3.up * 0.02f, rot);
            go.name = prefab.name;
            MonsterBrain brain = go.GetComponent<MonsterBrain>();
            if (brain != null)
            {
                // MonsterBrain has no 'spawnedByButton' / 'spawnSpeed' members; original intent kept below.
                // brain.spawnedByButton = true; /* ? */
                // brain.spawnSpeed = 6f; /* ? */
            }
            Spawned.Add(go);
            return;
        }
        Debug.LogWarning("[MonsterSpawnButton] no clear spot near you for " + prefab.name);
    }

    private static void Clear()
    {
        foreach (GameObject go in Spawned)
        {
            if (go != null) Destroy(go);
        }
        Spawned.Clear();
    }
}
