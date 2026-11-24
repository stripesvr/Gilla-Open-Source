// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.Universal;

public class GillaAreas : MonoBehaviour
{
    public const float Near = 55;
    public const float Far = 65;
    private const int Looked = 120;
    private const int LookedAfterJump = 2000;
    private const float Jump = 16;
    public float near = 55f;
    public float far = 65f;
    private Renderer[] renderers;
    private Bounds[] boxes;
    private bool[] on;
    private int next;
    private int jumpLeft;
    private Vector3 last;

    public int Count
    {
        get
        {
            if (renderers != null)
            {
                return renderers.Length;
            }
            return 0;
        }
    }

    public IReadOnlyList<Renderer> Renderers
    {
        get { return renderers; }
    }

    public static GillaAreas Make(Transform baseRoot)
    {
        GameObject go = new GameObject("GillaAreas");
        GillaAreas areas = go.AddComponent<GillaAreas>();
        SceneManager.MoveGameObjectToScene(areas.gameObject, baseRoot.gameObject.scene);
        areas.Gather(baseRoot);
        return areas;
    }

    private void OnDestroy()
    {
        ShowAll();
    }

    private void Gather(Transform baseRoot)
    {
        List<Renderer> list = new List<Renderer>();
        foreach (Transform child in baseRoot)
        {
            if (child.name == "ModeLobby" || child.name == "ModeMap" || child.name == "ModeBackrooms" || child.name == "SplashMap")
            {
                continue;
            }
            MeshRenderer[] found = child.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < found.Length; i++)
            {
                MeshRenderer r = found[i];
                if (r.enabled && !Moves(r.transform))
                {
                    list.Add(r);
                }
            }
        }
        GameObject halo = GameObject.Find("HaloFit");
        if (halo != null)
        {
            MeshRenderer[] found = halo.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < found.Length; i++)
            {
                MeshRenderer r = found[i];
                if (r.enabled)
                {
                    list.Add(r);
                }
            }
        }
        renderers = list.ToArray();
        boxes = new Bounds[renderers.Length];
        on = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            boxes[i] = renderers[i].bounds;
            on[i] = true;
        }
        Settle();
    }

    public static bool Moves(Transform part)
    {
        Transform t = part;
        while (t != null)
        {
            if (t != part)
            {
                if (t.parent == null)
                {
                    return false;
                }
            }
            Rigidbody body;
            if (t.TryGetComponent<Rigidbody>(out body))
            {
                return true;
            }
            MonoBehaviour[] behaviours = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour b = behaviours[i];
                if ((object)b == null)
                {
                    return true;
                }
                if (b is ProBuilderMesh)
                {
                    continue;
                }
                if (!(b is UniversalAdditionalLightData))
                {
                    return true;
                }
            }
            t = t.parent;
        }
        return false;
    }

    private static Vector3 Eyes()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position;
        }
        return Vector3.zero;
    }

    private void Update()
    {
        Step();
    }

    public void Step()
    {
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }
        Vector3 eyes = Eyes();
        int left;
        if ((eyes - last).sqrMagnitude <= 256f)
        {
            left = jumpLeft;
        }
        else
        {
            left = renderers.Length;
            jumpLeft = left;
        }
        int count = 2000;
        if (left < 1)
        {
            count = 120;
        }
        last = eyes;
        jumpLeft = Mathf.Max(left - count, 0);
        if (renderers.Length <= count)
        {
            count = renderers.Length;
        }
        Look(eyes, count);
    }

    private void Look(Vector3 eyes, int count)
    {
        for (; count > 0; count--)
        {
            int i = next;
            next = (i + 1) % renderers.Length;
            float d = boxes[i].SqrDistance(eyes);
            bool wasOn = on[i];
            bool flip;
            if (!wasOn)
            {
                flip = d < near * near;
            }
            else
            {
                flip = far * far <= d;
            }
            if (flip)
            {
                on[i] = !wasOn;
                if (renderers[i] != null)
                {
                    renderers[i].enabled = !wasOn;
                }
            }
        }
    }

    public void Settle()
    {
        Vector3 eyes = Eyes();
        last = eyes;
        jumpLeft = 0;
        if (renderers != null && renderers.Length > 0)
        {
            Look(eyes, renderers.Length);
        }
    }

    public void ShowAll()
    {
        if (renderers == null)
        {
            return;
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !on[i])
            {
                renderers[i].enabled = true;
                on[i] = true;
            }
        }
    }
}
