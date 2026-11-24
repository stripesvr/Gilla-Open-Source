// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public partial class CosmeticTerminal
{
    private void DrawUi_b__148_0()
    {
        FillRect(new RectInt(12, Mathf.RoundToInt((catAt - firstAt) * 38f) + 32, 108, 32), Line);
        int first = (int)firstAt;
        for (int i = first; i < Categories.Length && i <= first + 5; i++)
        {
            int y = Mathf.RoundToInt((i - firstAt) * 38f);
            float d = Mathf.Abs(i - catAt);
            RectInt r = new RectInt(12, y + 32, 108, 32);
            if (d >= 0.5f && hovered == "cat" + i.ToString())
            {
                FillRect(r, Dark);
            }
            if (pressFlash > 0f && pressedThing == "cat" + i.ToString())
            {
                FillRect(r, White);
            }
            Color32 edge;
            if (d >= 0.5f && hovered != "cat" + i.ToString())
            {
                edge = Dim;
            }
            else
            {
                edge = Line;
            }
            Outline(r, edge);
            Outline(new RectInt(15, y + 35, 108, 32), Dark);
            Text(text, 20, y + 41, Categories[i].Item1, d >= 0.5f ? Plain : DarkText, 2, false);
        }
    }

    private void DrawUi_b__148_1()
    {
        foreach (Tile t in leaving)
        {
            DrawTile(t, t.place, -1);
        }
        for (int i = 0; i < tiles.Length; i++)
        {
            DrawTile(tiles[i], new RectInt(132 + (i % 4) * 62, 28 + (i / 4) * 108, 58, 104), i);
        }
    }
}
