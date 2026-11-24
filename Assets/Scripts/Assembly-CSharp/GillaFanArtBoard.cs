// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GillaFanArtBoard : MonoBehaviour
{
    public struct Print
    {
        public Rect paper;
        public Rect art;
    }

    [Tooltip("The pictures, in the order they're laid out (GillaSetup.FanArt fills it from Assets/Gilla/FanArt).")]
    public Texture2D[] pieces = new Texture2D[0];
    [Tooltip("Each print's picture, in the same order.")]
    public Renderer[] arts = new Renderer[0];
    [Tooltip("What's written on the label plate on the board's foot.")]
    public Renderer label;
    [Tooltip("Assets/Gilla/Fonts: the label plate's (PixelOperatorHB8).")]
    public TextAsset font;
    public const string Join = "JOIN THE DISCORD";
    private const float Border = 0.03f;
    private const float Gap = 0.14f;
    private const float RowGap = 0.12f;
    private const float InsetX = 0.14f;
    private const float InsetY = 0.12f;
    private const float LabelPx = 300f;
    private const int LabelScale = 3;
    public static readonly Color32 Paper = new Color32(0xEC, 0xE9, 0xE0, 0xFF);
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private readonly List<Texture2D> made = new List<Texture2D>();
    private MaterialPropertyBlock block;
    private ScreenFont words;

    private void OnEnable()
    {

        Refresh();
    }

    public static float LabelWidth(TextAsset font)
    {
        int px;
        if (font != null)
        {
            ScreenFont sf = new ScreenFont(font.text);
            px = sf.Measure(Join, LabelScale);
        }
        else
        {
            px = PixelFont.Measure(Join, LabelScale);
        }
        return px / LabelPx;
    }

    public static Print[] Layout(Vector2[] sizes, Vector2 area)
    {
        int n = sizes.Length;
        Print[] prints = new Print[n];
        if (n == 0) return prints;

        float[] weights = new float[n];
        int split = n;
        if (n > 0)
        {
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = 1f;
                if (sizes[i].y > 0f) w = sizes[i].x / sizes[i].y;
                total += w;
                weights[i] = w;
            }
            if (n > 3)
            {
                float left = 0f;
                float best = float.MaxValue;
                for (int k = 1; k < n; k++)
                {
                    left += weights[k - 1];
                    float diff = Mathf.Abs(left - (total - left));
                    if (diff < best)
                    {
                        best = diff;
                        split = k;
                    }
                }
            }
        }

        int rows = split < n ? 2 : 1;
        float availW = area.x - 2f * InsetX;
        float rowH = (area.y - 2f * InsetY - (rows - 1) * RowGap) / rows;

        for (int r = 0; r < rows; r++)
        {
            int from = r == 0 ? 0 : split;
            int to = r == 0 ? split : n;
            int count = to - from;
            float sum = 0f;
            for (int i = from; i < to; i++) sum += weights[i];

            float gaps = (count - 1) * Gap;
            float h = (availW - gaps - count * (2f * Border)) / sum;
            if (h > rowH - 2f * Border) h = rowH - 2f * Border;

            float y = (rowH + RowGap) * r + InsetY + (rowH - (h + 2f * Border)) * 0.5f;
            float x = (availW - (count * (2f * Border) + gaps + sum * h)) * 0.5f + InsetX;
            for (int i = from; i < to; i++)
            {
                float artW = h * weights[i];
                float paperW = artW + 2f * Border;
                prints[i].paper = new Rect(x, y, paperW, h + 2f * Border);
                prints[i].art = new Rect(x + Border, y + Border, artW, h);
                x += paperW + Gap;
            }
        }
        return prints;
    }

    public void Refresh()
    {
        foreach (Texture2D t in made)
        {
            Gone(t);
        }
        made.Clear();

        if (words == null)
        {
            words = font != null ? new ScreenFont(font.text) : null;
        }
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        for (int i = 0; i < arts.Length; i++)
        {
            if (arts[i] != null && i < pieces.Length && pieces[i] != null)
            {
                Show(arts[i], pieces[i]);
            }
        }

        if (label != null)
        {
            Vector3 size = label.transform.lossyScale;
            Texture tex = Words(size, Join, new Color32(0, 0, 0, 0xFF), GillaBoardLook.White);
            Show(label, tex);
        }
    }

    private void Show(Renderer r, Texture tex)
    {
        r.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        r.SetPropertyBlock(block);
    }

    private Texture2D Words(Vector3 size, string line, Color32 ground, Color32[] colours)
    {
        ScreenFont.Style style = default(ScreenFont.Style);
        int w = Mathf.RoundToInt(Mathf.Abs(size.x) * LabelPx);
        if (w > 1023) w = 1024;
        if (w < 17) w = 16;
        int h = Mathf.RoundToInt(Mathf.Abs(size.y) * LabelPx);
        if (h > 255) h = 256;
        if (h < 9) h = 8;

        Color32[] px = new Color32[h * w];
        for (int i = 0; i < px.Length; i++)
        {
            px[i] = ground;
        }

        int measured = words == null ? PixelFont.Measure(line, LabelScale) : words.Measure(line, LabelScale);
        int x = (w - measured) / 2;
        int y = (h - 20) / 2;

        if (words == null)
        {
            PixelFont.Draw(px, w, h, x, y, line, colours[0], LabelScale);
        }
        else
        {
            style.scale = LabelScale;
            style.slant = 0f;
            style.colours = colours;
            words.Draw(px, w, h, x, y, line, style);
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.DontSave;
        tex.name = "GillaFanArtWords";
        tex.SetPixels32(px);
        tex.Apply(true);
        made.Add(tex);
        return tex;
    }

    private static void Gone(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying)
        {
            Object.Destroy(o);
            return;
        }
        Object.DestroyImmediate(o);
    }

    private void OnDestroy()
    {
        foreach (Texture2D t in made)
        {
            Gone(t);
        }
        made.Clear();
    }
}
