// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;

public partial class GillaRecycler : MonoBehaviour
{
    static GillaRecycler()
    {
        BaseMap = Shader.PropertyToID("_BaseMap");
        BaseColor = Shader.PropertyToID("_BaseColor");
        PriceRect = new RectInt(12, 30, 150, 132);
        WalletRect = new RectInt(12, 170, 150, 76);
        MainRect = new RectInt(172, 30, 328, 216);
        Black = new Color32(2, 6, 4, 255);
        Scan = new Color32(4, 12, 8, 255);
        Green = new Color32(60, 255, 120, 255);
        Dim = new Color32(20, 110, 55, 255);
        Dark = new Color32(8, 40, 20, 255);
        Yellow = new Color32(255, 230, 60, 255);
        GreenText = new Color32[] { new Color32(170, 255, 190, 255), new Color32(60, 235, 110, 255) };
        DimText = new Color32[] { new Color32(40, 150, 80, 255), new Color32(20, 110, 55, 255) };
        BoltText = new Color32[] { new Color32(240, 246, 252, 255), new Color32(160, 182, 205, 255) };
        all = new List<GillaRecycler>();

        var icons = new Dictionary<string, (string[] rows, Color32 l, Color32 m, Color32 d)>();
        icons.Add("GOLD BAR", (new string[]
        {
            "...LLLLLL...",
            "..LMMMMMMM..",
            ".LMMMMMMMMD.",
            "LMMMMMMMMMMD",
            "MMMMMMMMMMMD",
            "DDDDDDDDDDDD"
        }, new Color32(255, 240, 150, 255), new Color32(240, 190, 40, 255), new Color32(160, 110, 20, 255)));
        icons.Add("DIAMOND", (new string[]
        {
            "..LLLLLLL..",
            ".LLMMLMMLL.",
            "LLMMMLMMMLD",
            "DMMMMLMMMDD",
            ".DMMMLMMDD.",
            "..DMMLMDD..",
            "...DMLDD...",
            "....DMD....",
            ".....D....."
        }, new Color32(230, 250, 255, 255), new Color32(140, 220, 255, 255), new Color32(60, 150, 210, 255)));
        icons.Add("RUSTY COG", (new string[]
        {
            "...M.M...",
            ".MMMMMMM.",
            "MMMDDDMMM",
            ".MMD.DMM.",
            "MMD...DMM",
            ".MMD.DMM.",
            "MMMDDDMMM",
            ".MMMMMMM.",
            "...M.M..."
        }, new Color32(230, 140, 80, 255), new Color32(190, 95, 45, 255), new Color32(120, 55, 25, 255)));
        icons.Add("BIG BOLT", (new string[]
        {
            ".LLL........",
            "LMMML.......",
            "MMMMMMMMMMMM",
            "MMMMMDMDMDMD",
            "DMMMD.......",
            ".DDD........"
        }, new Color32(220, 225, 230, 255), new Color32(160, 165, 175, 255), new Color32(95, 100, 110, 255)));
        icons.Add("SCRAP PLATE", (new string[]
        {
            "LLLLLLLL.L..",
            "LMMMMMMMMML.",
            "MMDMMMMMMMMM",
            "MMMMMMMMMMD.",
            "MMDMMMMMMMMM",
            "DDDDDDDDDD.."
        }, new Color32(200, 195, 190, 255), new Color32(150, 135, 125, 255), new Color32(110, 80, 60, 255)));
        Icons = icons;
    }
}
