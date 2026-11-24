// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class ModMenu : MonoBehaviour
{
    private void BuildBoard_b__165_1()
    {
        int count = pages.Count;
        slideDir = 1;
        int n = page + 1 + count;
        int q = 0;
        if (count != 0)
        {
            q = n / count;
        }
        slide = 1f;
        page = n - q * count;

        if (pages.Count == 0)
        {
            return;
        }
        int last = pages.Count - 1;
        int clamped = page;
        if (last <= page)
        {
            clamped = last;
        }
        page = page < 0 ? 0 : clamped;
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
            Row r = rows[i];
            Item item = i < items.Count ? items[i] : null;
            r.item = item;
            r.bg.gameObject.SetActive(item != null);
            r.glow.gameObject.SetActive(item != null);
            if (item != null)
            {
                r.label.text = r.item.label;
                r.knobT = r.item.on != null ? (r.item.on() ? 1f : 0f) : 0f;
                r.track.gameObject.SetActive(r.item.on != null && kind == Kind.Owner);
                r.arrow.gameObject.SetActive(r.item.on == null && r.item.value == null && kind == Kind.Owner);
                r.value.gameObject.SetActive(r.item.value != null && kind == Kind.Owner);
                r.pressT = 0f;
                r.hoverT = 0f;
            }
        }

        for (int i = 0; i < dots.Count; i++)
        {
            Image dot = dots[i];
            dot.color = i != page ? DotOff : Accent;
        }
    }

    private void MakeSwapKey_b__182_0()
    {
        swapping = true;
    }
}
