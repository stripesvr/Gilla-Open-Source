// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public partial class ModMenu : MonoBehaviour
{
    private void BuildSimpleBoard()
    {
        List<Item> sizes = null;
        List<Item> mods = null;
        foreach (Page p in pages)
        {
            if (p.title == "SIZE")
            {
                sizes = p.items;
            }
            else if (mods == null)
            {
                mods = p.items;
            }
        }
        bool hasMods = mods != null;
        Kind k = kind;
        bool hasSizes = sizes != null;
        bool both = k == Kind.SizeAndOP;
        bool op = false;
        if (k != Kind.Size && k != Kind.Punch)
        {
            op = mods != null;
            if (op && hasMods && hasSizes && both)
            {
                op = !showSize;
            }
        }
        opLook = op;
        simpleItems.Clear();
        swapKey = null;
        titleRim = null;
        wash = null;
        frameDots = null;
        if (k == Kind.Punch)
        {
            if (mods == null)
            {
                mods = new List<Item>();
            }
            BuildPunchBoard(mods);
        }
        else
        {
            if (!opLook)
            {
                if (sizes == null)
                {
                    sizes = new List<Item>();
                }
                BuildSizeBoard(sizes);
            }
            else
            {
                BuildOpBoard(mods);
            }
            if (hasMods && hasSizes && both)
            {
                swapKey = MakeSwapKey(opLook ? "SIZE MENU >" : "OP MENU >");
            }
        }
        if (cursor == null)
        {
            MakeCursor();
        }
        PointLetters(titleFont);
        PointLetters(bodyFont);
    }

    private Text SimpleTop(string name, bool dots)
    {
        Sprite sprite = MakeSimpleBack(Mathf.RoundToInt(W * 0.25f), Mathf.RoundToInt(H * 0.25f), dots);
        backSprite = sprite;
        Img("Frame", sprite, Color.white, Vector2.zero, new Vector2(W, H), Image.Type.Simple, null);
        float y = H * 0.5f - 22f - 75f;
        Text title = Txt("Title", name, new Vector2(0f, y), new Vector2(W - 66f, 130f), 96, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, null);
        title.font = titleFont;
        Text where = Txt("Where", "only works in the regular game", new Vector2(0f, y - 100f), new Vector2(W - 66f, 50f), 32, SimpleWhere, TextAnchor.MiddleCenter, FontStyle.Normal, null);
        where.font = bodyFont;
        return title;
    }

    private void BuildSizeBoard(List<Item> sizes)
    {
        W = 640f;
        float h = sizes.Count * 118f + 222f + 60f + 22f;
        H = h;
        MakeCanvas();
        SimpleTop("SIZE MENU", true);
        float w = W;
        for (int i = 0; i < sizes.Count; i++)
        {
            Row row = MakeSimpleRow(rows.Count, new Vector2(0f, h * 0.5f - 22f - 150f - 50f - 59f + i * -118f), new Vector2(w - 44f - 36f, 98f), 56);
            rows.Add(row);
            simpleItems.Add(sizes[i]);
        }
        SimpleFoot();
    }

    private void BuildPunchBoard(List<Item> items)
    {
        W = 700f;
        float h = items.Count * 236f + 222f + 60f + 22f;
        H = h;
        MakeCanvas();
        Text title = SimpleTop("PUNCH MENU", true);
        title.resizeTextForBestFit = false;
        Vector2 box = title.rectTransform.sizeDelta;
        int fontSize = 96;
        bool more;
        do
        {
            title.fontSize = fontSize;
            if (title.preferredWidth <= box.x) break;
            more = fontSize > 48;
            fontSize -= 8;
        } while (more);
        float w = W;
        for (int i = 0; i < items.Count; i++)
        {
            Row row = MakeSimpleRow(rows.Count, new Vector2(0f, h * 0.5f - 22f - 150f - 50f - 118f + i * -236f), new Vector2(w - 44f - 36f, 216f), 72);
            rows.Add(row);
            simpleItems.Add(items[i]);
        }
        SimpleFoot();
    }

    private void BuildOpBoard(List<Item> mods)
    {
        W = 1080f;
        int count = mods.Count;
        int perCol = (int)(count * 0.5f);
        if (perCol < 2)
        {
            perCol = 1;
        }
        float h = perCol * 118f + 222f + 60f + 22f;
        H = h;
        MakeCanvas();
        Text title = SimpleTop("OP MENU", false);
        wash = Img("Wash", null, new Color(OpGlow.r, OpGlow.g, OpGlow.b, 0f), Vector2.zero, new Vector2(W - 24f, H - 24f), Image.Type.Simple, null);
        wash.transform.SetSiblingIndex(1);
        if (dotsSprite == null)
        {
            dotsSprite = MakeFrameDots(Mathf.RoundToInt(W * 0.25f), Mathf.RoundToInt(H * 0.25f));
        }
        frameDots = Img("FrameDots", dotsSprite, OpGlow, Vector2.zero, new Vector2(W, H), Image.Type.Simple, null);
        frameDots.transform.SetSiblingIndex(2);
        titleRim = title.gameObject.AddComponent<Outline>();
        titleRim.effectColor = OpGlow;
        titleRim.effectDistance = new Vector2(5f, -5f);
        float colW = W - 44f - 36f;
        float cellW = (colW - 24f) * 0.5f;
        for (int i = 0; i < mods.Count; i++)
        {
            int col = perCol != 0 ? i / perCol : 0;
            Vector2 at = new Vector2(
                (cellW * 0.5f - colW * 0.5f) + (cellW + 24f) * col,
                h * 0.5f - 22f - 150f - 50f - 59f + (i - perCol * col) * -118f);
            Row row = MakeSimpleRow(rows.Count, at, new Vector2(cellW, 98f), 48);
            row.onCol = OpLitBand;
            rows.Add(row);
            simpleItems.Add(mods[i]);
        }
        SimpleFoot();
    }

    private void SimpleFoot()
    {
        footText = Txt("Foot", "", new Vector2(0f, H * -0.5f + 22f + 30f), new Vector2(W - 66f, 34f), 24, TextDim, TextAnchor.MiddleCenter, FontStyle.Normal, null);
        footText.font = bodyFont;
    }

    public static float OpPulseNow()
    {
        float t = Time.timeSinceLevelLoad;
        return Mathf.Sin((t + t) * Mathf.PI / 1.6f) * 0.5f + 0.5f;
    }

    public static Color OpColourNow()
    {
        float t = Time.timeSinceLevelLoad;
        float p = Mathf.Sin((t + t) * Mathf.PI / 1.6f) * 0.5f + 0.5f;
        Color c = Color.Lerp(OpGlow, OpGlow2, p);
        float k = p * 0.15f + 0.85f;
        return new Color(c.r * k, c.g * k, c.b * k, 1f);
    }

    private static Sprite MakeSimpleBack(int w, int h, bool dots)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "ModMenu Board";
        Color32[] px = new Color32[h * w];
        GillaBoardLook.Clear(px, w, h);
        if (dots)
        {
            GillaBoardLook.Frame(px, w, h, 3);
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private static Sprite MakeFrameDots(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "ModMenu Frame Dots";
        Color32[] px = new Color32[h * w];
        GillaBoardLook.Frame(px, w, h, 3);
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a != 0)
            {
                px[i] = new Color32(255, 255, 255, 255);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    private void HiddenParts(Row r)
    {
        r.value = Txt("Value", "", Vector2.zero, new Vector2(10f, 10f), 10, new Color(0f, 0f, 0f, 0f), TextAnchor.MiddleCenter, FontStyle.Normal, r.rt);
        r.arrow = Txt("Arrow", "", Vector2.zero, new Vector2(10f, 10f), 10, new Color(0f, 0f, 0f, 0f), TextAnchor.MiddleCenter, FontStyle.Normal, r.rt);
        r.track = Img("Track", square, new Color(0f, 0f, 0f, 0f), Vector2.zero, new Vector2(10f, 10f), Image.Type.Sliced, r.rt);
        r.knob = Img("Knob", square, new Color(0f, 0f, 0f, 0f), Vector2.zero, new Vector2(10f, 10f), Image.Type.Simple, r.track.rectTransform);
        r.value.gameObject.SetActive(false);
        r.arrow.gameObject.SetActive(false);
        r.track.gameObject.SetActive(false);
    }

    private Image[] Lines(string name, Vector2 at, Vector2 size, float t, float past, Color colour, Transform parent)
    {
        Image[] lines = new Image[4];
        float halfH = size.y * 0.5f;
        float halfT = t * 0.5f;
        float longW = size.x + past + past;
        lines[0] = Img(name + "Top", square, colour, new Vector2(at.x + 0f, at.y + (halfH - halfT)), new Vector2(longW, t), Image.Type.Simple, parent);
        lines[1] = Img(name + "Bottom", square, colour, new Vector2(at.x + 0f, at.y + (halfT - halfH)), new Vector2(longW, t), Image.Type.Simple, parent);
        float halfW = size.x * 0.5f;
        float longH = size.y + past + past;
        lines[2] = Img(name + "Left", square, colour, new Vector2(at.x + (halfT - halfW), at.y + 0f), new Vector2(t, longH), Image.Type.Simple, parent);
        lines[3] = Img(name + "Right", square, colour, new Vector2(at.x + (halfW - halfT), at.y + 0f), new Vector2(t, longH), Image.Type.Simple, parent);
        return lines;
    }

    private Row MakeSimpleRow(int i, Vector2 at, Vector2 size, int words)
    {
        Row r = new Row();
        r.size = size;
        r.basePos = at;
        r.offCol = SimpleOff;
        r.onCol = SimpleOn;
        r.glow = Img("RowGlow" + i.ToString(), soft, new Color(Accent.r, Accent.g, Accent.b, 0f), at, new Vector2(size.x + 30f, size.y + 30f), Image.Type.Sliced, null);
        r.bg = Img("Row" + i.ToString(), square, SimpleOff, at, size, Image.Type.Sliced, null);
        r.rt = r.bg.rectTransform;
        r.label = Txt("Label", "", Vector2.zero, new Vector2(size.x - 24f, size.y - 12f), words, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, r.rt);
        r.label.font = bodyFont;
        HiddenParts(r);
        r.box = Lines("Box", Vector2.zero, new Vector2(size.x - 16f, size.y - 16f), 4f, 8f, SimpleBox, r.rt);
        r.label.transform.SetAsLastSibling();
        r.act = () => MakeSimpleRow_b__0(r);
        return r;
    }

    private Hit MakeSwapKey(string label)
    {
        float w = W;
        float h = H;
        Hit hit = new Hit();
        float y = (22f - h * 0.5f) + 30f;
        float x = w * 0.5f - 22f - 18f - 107f;
        hit.size = new Vector2(214f, 46f);
        hit.basePos = new Vector2(x, y);
        hit.bg = Img("Swap", square, SimpleOff, new Vector2(x, y), new Vector2(214f, 46f), Image.Type.Sliced, null);
        hit.rt = hit.bg.rectTransform;
        Lines("SwapEdge", Vector2.zero, new Vector2(214f, 46f), 3f, 0f, SimpleBox, hit.rt);
        hit.label = Txt("SwapText", label, Vector2.zero, new Vector2(202f, 42f), 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, hit.rt);
        hit.label.font = bodyFont;
        hit.act = () => MakeSwapKey_b__182_0();
        RectTransform foot = footText.rectTransform;
        float fw = W - 66f - 214f - 28f;
        foot.sizeDelta = new Vector2(fw, foot.sizeDelta.y);
        foot.anchoredPosition = new Vector2(x - 107f - 14f + fw * -0.5f, foot.anchoredPosition.y);
        return hit;
    }

    private void SwapBoard()
    {
        swapping = false;
        if (root == null)
        {
            return;
        }
        showSize = !showSize;
        PlayerPrefs.SetInt("Gilla.ModMenu.ShowSize", showSize ? 1 : 0);
        Rebuild();
        toast = showSize ? "SIZE MENU" : "OP MENU";
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void Rebuild()
    {
        bool active = root.gameObject.activeSelf;
        root.gameObject.SetActive(false);
        Object.Destroy(root.gameObject);
        if (backSprite != null)
        {
            Object.Destroy(backSprite.texture);
            Object.Destroy(backSprite);
            backSprite = null;
        }
        if (dotsSprite != null)
        {
            Object.Destroy(dotsSprite.texture);
            Object.Destroy(dotsSprite);
            dotsSprite = null;
        }
        rows.Clear();
        dots.Clear();
        next = null;
        prev = null;
        countText = null;
        pageText = null;
        swapKey = null;
        opLook = false;
        SetLook();
        if (kind == Kind.Owner)
        {
            W = 640f;
            H = FullH;
        }
        BuildBoard();
        ShowPage();
        root.gameObject.SetActive(active);
        if (active)
        {
            Place3D();
            Animate();
            lastTouched = Touching();
        }
    }

    private void MakeCursor()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ModMenu Cursor";
        Collider col = go.GetComponent<Collider>();
        col.enabled = false;
        Object.Destroy(col);
        Renderer renderer = go.GetComponent<Renderer>();
        if (unlitMat != null)
        {
            Material mat = new Material(unlitMat);
            mat.name = "ModMenu Cursor";
            renderer.sharedMaterial = mat;
            renderer.sharedMaterial.SetColor("_BaseColor", AccentHot);
        }
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.transform.localScale = Vector3.one * 0.012f;
        cursor = go.transform;
    }

    private Row MakeRow(int i, Vector2 at)
    {
        float w = W;
        Row r = new Row();
        w = w - 44f - 8f;
        r.size = new Vector2(w, 70f);
        r.basePos = at;
        r.offCol = SimpleOff;
        r.onCol = SimpleOn;
        r.glow = Img("RowGlow" + i.ToString(), soft, new Color(Accent.r, Accent.g, Accent.b, 0f), at, new Vector2(w + 30f, 100f), Image.Type.Sliced, null);
        r.bg = Img("Row" + i.ToString(), square, SimpleOff, at, new Vector2(w, 70f), Image.Type.Sliced, null);
        r.rt = r.bg.rectTransform;
        r.box = Lines("Box", Vector2.zero, new Vector2(w - 12f, 58f), 3f, 6f, SimpleBox, r.rt);
        float half = w * 0.5f;
        r.label = Txt("Label", "", new Vector2((26f - half) + 190f, 0f), new Vector2(380f, 40f), 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal, r.rt);
        r.label.font = bodyFont;
        r.value = Txt("Value", "", new Vector2(half - 26f - 90f, 0f), new Vector2(180f, 36f), 28, Color.white, TextAnchor.MiddleRight, FontStyle.Normal, r.rt);
        r.value.font = bodyFont;
        r.arrow = Txt("Arrow", ">", new Vector2(half - 34f, 0f), new Vector2(32f, 40f), 32, SimpleWhere, TextAnchor.MiddleCenter, FontStyle.Normal, r.rt);
        r.arrow.font = bodyFont;
        r.track = Img("Track", square, SimpleOff, new Vector2(half - 26f - 42f, 0f), new Vector2(84f, 38f), Image.Type.Sliced, r.rt);
        Lines("TrackEdge", Vector2.zero, new Vector2(84f, 38f), 3f, 0f, SimpleBox, r.track.rectTransform);
        r.knob = Img("Knob", square, KnobOff, Vector2.zero, new Vector2(26f, 26f), Image.Type.Simple, r.track.rectTransform);
        r.act = () => MakeRow_b__0(r);
        return r;
    }

    private Hit MakeHit(string name, Vector2 at, Vector2 size, string label, Action act)
    {
        Hit hit = new Hit();
        hit.act = act;
        hit.size = size;
        hit.basePos = at;
        hit.bg = Img(name, square, SimpleOff, at, size, Image.Type.Sliced, null);
        hit.rt = hit.bg.rectTransform;
        Lines(name + "Edge", Vector2.zero, size, 3f, 0f, SimpleBox, hit.rt);
        hit.label = Txt("Label", label, Vector2.zero, new Vector2(size.x - 8f, size.y - 4f), 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal, hit.rt);
        hit.label.font = bodyFont;
        return hit;
    }

    private Image Img(string name, Sprite sprite, Color colour, Vector2 at, Vector2 size, Image.Type type, Transform parent)
    {
        GameObject go = new GameObject(name);
        Image img = go.AddComponent<Image>();
        Transform t = go.transform;
        if (!(parent != null))
        {
            parent = root;
        }
        t.SetParent(parent, false);
        img.sprite = sprite;
        img.type = type;
        img.color = colour;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = size;
        return img;
    }

    private Text Txt(string name, string text, Vector2 at, Vector2 size, int fontSize, Color colour, TextAnchor anchor, FontStyle style, Transform parent)
    {
        GameObject go = new GameObject(name);
        Text txt = go.AddComponent<Text>();
        Transform t = go.transform;
        if (!(parent != null))
        {
            parent = root;
        }
        t.SetParent(parent, false);
        txt.font = font;
        txt.text = text;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = colour;
        txt.alignment = anchor;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 10;
        txt.resizeTextMaxSize = fontSize;
        RectTransform rt = txt.rectTransform;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = at;
        rt.sizeDelta = size;
        return txt;
    }

    private static Sprite MakeRounded(int size, float radius, float feather, bool point)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.name = "ModMenu Sprite";
        Color32[] colors = new Color32[size * size];
        float fs = size;
        float inner = feather + radius;
        float outer = (fs - feather) - radius;
        for (int y = 0; y < size; y++)
        {
            float fy = y + 0.5f;
            float cy = fy < inner ? inner : Mathf.Min(fy, outer);
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f;
                float cx = fx < inner ? inner : (fx > outer ? outer : fx);
                float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy)) - radius;
                float a;
                if (feather <= 0f)
                {
                    a = Mathf.Clamp01(0.5f - d);
                }
                else
                {
                    a = 1f;
                    if (d > 0f)
                    {
                        a = Mathf.Pow(Mathf.Clamp01(1f - d / feather), 2.2f);
                    }
                }
                colors[y * size + x] = new Color32(255, 255, 255, (byte)(int)(a * 255f));
            }
        }
        tex.SetPixels32(colors);
        tex.Apply(false, true);
        float b = Mathf.Min(fs * 0.5f - 1f, inner + 1f);
        return Sprite.Create(tex, new Rect(0f, 0f, fs, fs), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    private void Turn(int dir)
    {
        int count = pages.Count;
        slideDir = dir;
        int n = page + dir + count;
        int q = count != 0 ? n / count : 0;
        slide = 1f;
        page = n - q * count;
        if (pages.Count == 0)
        {
            return;
        }
        page = page < 0 ? 0 : Mathf.Min(page, pages.Count - 1);
        Page p = pages[page];
        if (pageText != null)
        {
            pageText.text = p.title;
        }
        if (countText != null)
        {
            countText.text = (page + 1).ToString() + " / " + pages.Count.ToString();
        }
        List<Item> items = kind == Kind.Owner ? p.items : simpleItems;
        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            Item item = i < items.Count ? items[i] : null;
            row.item = item;
            row.bg.gameObject.SetActive(item != null);
            row.glow.gameObject.SetActive(item != null);
            if (item != null)
            {
                row.label.text = row.item.label;
                row.knobT = (row.item.on != null && row.item.on()) ? 1f : 0f;
                row.track.gameObject.SetActive(row.item.on != null && kind == Kind.Owner);
                row.arrow.gameObject.SetActive(row.item.on == null && row.item.value == null && kind == Kind.Owner);
                row.value.gameObject.SetActive(row.item.value != null && kind == Kind.Owner);
                row.pressT = 0f;
                row.hoverT = 0f;
            }
        }
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == page ? Accent : DotOff;
        }
    }
}
