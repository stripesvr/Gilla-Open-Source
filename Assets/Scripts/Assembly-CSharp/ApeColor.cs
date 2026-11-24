// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class ApeColor : MonoBehaviour
{
    public Renderer body;
    [Tooltip("The other body pieces (dismemberment parts): they share the same coloured material.")]
    public Renderer[] parts;
    public int materialIndex;
    public Texture2D tintTexture;
    public Color furColor = new Color(0.298f, 0.585f, 0.155f, 1f);
    private static Color32[] source;
    private static Texture2D sourceTex;
    private Texture2D tex;
    private Material mat;

    private void Start()
    {
        if (mat == null)
        {
            SetColor(furColor);
        }
    }

    private void MakeMaterial()
    {
        Material[] materials = body.materials;
        mat = materials[materialIndex];
        if (parts != null)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                Renderer part = parts[i];
                if (part == null) continue;
                Material[] shared = part.sharedMaterials;
                if (materialIndex < shared.Length)
                {
                    shared[materialIndex] = mat;
                    part.sharedMaterials = shared;
                }
            }
        }
    }

    public void SetColorCode(int r, int g, int b)
    {
        r = Mathf.Max(Mathf.Min(r, 9), 0);
        g = Mathf.Max(Mathf.Min(g, 9), 0);
        b = Mathf.Max(Mathf.Min(b, 9), 0);
        SetColor(new Color(r / 9f, g / 9f, b / 9f, 1f));
    }

    public bool SetColorHex(string hex)
    {
        Color c;
        if (!hex.StartsWith("#"))
        {
            hex = "#" + hex;
        }
        bool ok = ColorUtility.TryParseHtmlString(hex, out c);
        if (ok)
        {
            SetColor(c);
        }
        return ok;
    }

    public void SetColor(Color c)
    {
        furColor = c;
        if (mat == null)
        {
            MakeMaterial();
        }
        if (source == null || sourceTex != tintTexture)
        {
            source = tintTexture.GetPixels32();
            sourceTex = tintTexture;
        }
        if (tex == null)
        {
            tex = new Texture2D(tintTexture.width, tintTexture.height, TextureFormat.RGBA32, true, false);
            tex.name = "Gilla_Fur_" + name;
            tex.filterMode = tintTexture.filterMode;
            tex.wrapMode = tintTexture.wrapMode;
        }
        int r = Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
        int g = Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
        int b = Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
        Color32[] pixels = new Color32[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Color32 s = source[i];
            int a = s.a;
            pixels[i] = new Color32(
                (byte)((a * (r - 255) + 65025) * s.r / 65025),
                (byte)((a * (g - 255) + 65025) * s.g / 65025),
                (byte)((a * (b - 255) + 65025) * s.b / 65025),
                255);
        }
        tex.SetPixels32(pixels);
        tex.Apply(true);
        mat.mainTexture = tex;
    }

    private void OnDestroy()
    {
        if (tex != null)
        {
            Destroy(tex);
        }
        if (mat != null)
        {
            Destroy(mat);
        }
    }
}
