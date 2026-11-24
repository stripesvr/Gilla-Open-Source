// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class MonsterNavMesh : MonoBehaviour
{
    public NavMeshData data;
    private NavMeshDataInstance instance;
    private static int live;
    private static BoxCollider[] barriers;
    private const float Below = 1.5f;

    public static bool Ready
    {
        get { return live > 0; }
    }

    private void OnEnable()
    {

        barriers = null;
        if (data == null)
        {
            return;
        }
        instance = NavMesh.AddNavMeshData(data, transform.position, transform.rotation);
        if (!instance.valid)
        {
            return;
        }
        live++;
    }

    private void OnDisable()
    {
        if (instance.valid)
        {
            instance.Remove();
            live--;
        }
    }

    public static bool Barred(Vector3 at)
    {
        if (barriers == null)
        {
            barriers = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(b => b.name == "MonsterBarrier")
                .ToArray();
        }
        for (int i = 0; i < barriers.Length; i++)
        {
            BoxCollider box = barriers[i];
            if (box == null)
            {
                continue;
            }
            Vector3 local = box.transform.InverseTransformPoint(at);
            Vector3 center = box.center;
            Vector3 size = box.size;
            if (Mathf.Abs(local.x - center.x) <= size.x * 0.5f)
            {
                float halfY = size.y * 0.5f;
                float dy = local.y - center.y;
                if (dy <= halfY && -Below - halfY <= dy && Mathf.Abs(local.z - center.z) <= size.z * 0.5f)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
