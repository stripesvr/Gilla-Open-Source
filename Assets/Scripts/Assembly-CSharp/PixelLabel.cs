// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class PixelLabel : MonoBehaviour
{
    [TextArea(2, 12)]
    public string text = "LABEL";
    public int width = 64;
    public int height = 32;
    [Tooltip("Texture pixels per font pixel.")]
    public int scale = 1;
    public Color32 colour = new Color32(110, 255, 120, 255);
    public Color32 accent = new Color32(255, 210, 60, 255);
    public Color32 background = new Color32(4, 6, 6, 255);
    [Tooltip("The texture repeats (a band of text round something, ShopScroll sliding it along): else it stops at its edges.")]
    public bool repeat;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private Texture2D tex;
    private MaterialPropertyBlock block;

    private void OnEnable()
    {

        Redraw();
    }

    public void Redraw()
    {
        int w = width;
        int h = height;
        if (w < 9) w = 8;
        if (h < 9) h = 8;

        if (tex == null || tex.width != w || tex.height != h)
        {
            if (tex != null)
            {
                Kill(tex);
            }
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            t.hideFlags = HideFlags.DontSave;
            t.name = "PixelLabel";
            tex = t;
        }

        tex.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        Color32[] px = new Color32[h * w];
        for (int i = 0; i < px.Length; i++)
        {
            px[i] = background;
        }

        string[] lines = (text ?? "").Split('\n');
        if (lines.Length > 0)
        {
            int lineStep = scale * 9;
            int y = (h + scale * 2 - lineStep * lines.Length) / 2;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool accented = line.StartsWith("*");
                if (accented)
                {
                    line = line.Substring(1);
                }
                int lineWidth = 0;
                if (line.Length != 0)
                {
                    lineWidth = (line.Length * 6 - 1) * scale;
                }
                int x = (w - lineWidth) / 2;
                PixelFont.Draw(px, w, h, x, y, line, accented ? accent : colour, scale);
                y += lineStep;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false);
        Renderer renderer = GetComponent<Renderer>();
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        renderer.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        renderer.SetPropertyBlock(block);
    }

    private void OnDestroy()
    {
        if (tex != null)
        {
            Kill(tex);
        }
    }

    private static void Kill(Object o)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(o);
            return;
        }
        Object.DestroyImmediate(o);
    }
}
