// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public partial class GillaComputer : MonoBehaviour
{
    static GillaComputer()
    {
        PageNames = new string[] { "CODE", "COLOR", "SETTINGS", "MUSIC" };
        BaseMap = Shader.PropertyToID("_BaseMap");
        rgb = new int[3] /* ? initial values from static data */;
        typed = "";
        Silver = new Color32[]
        {
            new Color32(255, 255, 255, 255),
            new Color32(200, 200, 200, 255),
            new Color32(140, 140, 140, 255)
        };
        Heading = new Color32[]
        {
            new Color32(170, 170, 170, 255),
            new Color32(205, 205, 205, 255),
            new Color32(232, 232, 232, 255),
            new Color32(252, 252, 252, 255)
        };
        Milk = new Color32[]
        {
            new Color32(255, 255, 255, 255),
            new Color32(205, 205, 205, 255)
        };
        Orange = new Color32[]
        {
            new Color32(255, 190, 105, 255),
            new Color32(240, 120, 25, 255)
        };
        Lime = new Color32[]
        {
            new Color32(140, 255, 110, 255),
            new Color32(40, 215, 40, 255)
        };
        Grey = new Color32[]
        {
            new Color32(226, 226, 226, 255),
            new Color32(196, 196, 196, 255)
        };
        Dim = new Color32[]
        {
            new Color32(124, 124, 124, 255),
            new Color32(94, 94, 94, 255)
        };
        Shadow = new Color32(48, 48, 48, 255);
        Divider = new Color32(56, 56, 56, 255);
        Cursor = new Color32(116, 116, 116, 255);
        Channels = new Color32[][]
        {
            new Color32[] { new Color32(255, 120, 70, 255), new Color32(255, 60, 20, 255) },
            new Color32[] { new Color32(120, 255, 100, 255), new Color32(40, 225, 30, 255) },
            new Color32[] { new Color32(90, 90, 255, 255), new Color32(40, 40, 245, 255) }
        };
        Corner = new Bounds(new Vector3(-9.25f, 1.2f, 62.575f), new Vector3(4.9f, 3.6f, 4.75f));
    }

    private void DrawSettings(ScreenFont.Style hint)
    {
        Line(text, 198, 40, "TURN", new ScreenFont.Style { scale = 2, slant = 0f, colours = Heading, along = true }, true);
        int mode = (int)GillaTurn.Mode;
        ScreenFont.Style sel = new ScreenFont.Style { bold = true, scale = 1, slant = 0f, colours = Milk };
        ScreenFont.Style dim = new ScreenFont.Style { bold = false, scale = 1, slant = 0f, colours = Dim };
        string[] options = new string[] { "OPTION 1: SNAP TURN", "OPTION 2: SMOOTH TURN", "OPTION 3: NO TURN" };
        int y = 59;
        for (int i = 0; i < 3; i++)
        {
            if (mode == i)
            {
                Line(text, 86, y, ">", sel, false);
                Line(text, 96, y, options[i], sel, false);
            }
            else
            {
                Line(text, 96, y, options[i], dim, false);
            }
            y += 11;
        }

        ScreenFont.Style bigSel = new ScreenFont.Style { bold = true, scale = 2, slant = 0f, colours = Milk };
        ScreenFont.Style bigDim = new ScreenFont.Style { bold = false, scale = 2, slant = 0f, colours = Dim };
        ScreenFont.Style bigGrey = new ScreenFont.Style { bold = true, scale = 2, slant = 0f, colours = Grey };

        Line(text, 96, 97, "SNAP", mode == 0 ? sel : dim, false);
        Line(text, 156, 94, "45", GillaTurn.SnapAngle != 45 ? bigDim : (mode == 0 ? bigSel : bigGrey), false);
        Line(text, 196, 94, "90", GillaTurn.SnapAngle != 90 ? bigDim : (mode == 0 ? bigSel : bigGrey), false);

        Line(text, 96, 115, "SMOOTH", mode == 1 ? sel : dim, false);
        int speed = GillaTurn.SmoothSpeed;
        Line(text, 156, 112, speed.ToString(), mode == 1 ? bigSel : bigGrey, false);
        Line(text, 176, 115, "(0-9)", dim, false);

        bool on = GillaPlayerPunching.On;
        Line(text, 96, 133, "PLAYER PUNCHING", sel, false);
        Line(text, 196, 130, "ON", on ? bigSel : bigDim, false);
        Line(text, 228, 130, "OFF", on ? bigDim : bigSel, false);

        Line(text, 160, 172, "LEFT 45, RIGHT 90 degrees", hint, true);
        Line(text, 160, 181, "0-9: how fast it turns", hint, true);
        Line(text, 160, 190, "ENTER: player punching on/off", hint, true);
    }

    private static ScreenFont Load(TextAsset baked)
    {
        ScreenFont font = null;
        if (baked != null)
        {
            font = new ScreenFont(baked.text);
        }
        return font;
    }

    public static string Build
    {
        get
        {
            if (string.IsNullOrEmpty(Application.buildGUID))
            {
                return "editor";
            }
            if (Application.buildGUID.Trim('0').Length == 0)
            {
                return "editor";
            }
            string guid = Application.buildGUID;
            return guid.Substring(0, Math.Min(Application.buildGUID.Length, 6));
        }
    }

    private int Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style, bool centred)
    {
        int width;
        if (font == null)
        {
            int scale = style.scale;
            if (scale < 2)
            {
                scale = 1;
            }
            width = PixelFont.Measure(line, scale);
            if (centred)
            {
                x -= width / 2;
            }
            PixelFont.Draw(px, 320, 240, x, y, line, style.colours[0], scale);
        }
        else if (centred)
        {
            width = font.DrawCentred(px, 320, 240, x, y, line, style);
        }
        else
        {
            width = font.Draw(px, 320, 240, x, y, line, style);
        }
        return width;
    }

    private void Fill(int x, int y, int w, int h, Color32 colour)
    {
        PixelFont.Fill(px, 320, 240, x, y, w, h, colour);
    }

    private void OnDestroy()
    {
        if (tex != null)
        {
            if (Application.isPlaying)
            {
                Destroy(tex);
                return;
            }
            DestroyImmediate(tex);
        }
    }
}
