// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public static class GillaBoardLook
{
	public static readonly Color32[] White = new Color32[] { new Color32(255, 255, 255, 255), new Color32(232, 232, 232, 255) };
	public static readonly Color32[] Soft = new Color32[] { new Color32(214, 214, 214, 255), new Color32(190, 190, 190, 255) };
	public static readonly Color32[] Dim = new Color32[] { new Color32(138, 138, 138, 255), new Color32(112, 112, 112, 255) };
	public static readonly Color32 Band = new Color32(8, 8, 8, 255);
	public static readonly Color32 OtherBand = new Color32(13, 13, 13, 255);
	public static readonly Color32 PickedBand = new Color32(62, 62, 62, 255);
	public static readonly Color32 Dots = new Color32(176, 176, 176, 255);
	public static readonly Color32 Edge = new Color32(58, 58, 58, 255);
	public static readonly Color32 Solid = new Color32(240, 240, 240, 255);
	private static Color32[] back;
	private static int backW;

	private static Color32 G(byte v)
	{
		return new Color32(v, v, v, 255);
	}

	public static void Clear(Color32[] px, int w, int h)
	{
		if (back == null || back.Length != px.Length || backW != w)
		{
			back = new Color32[px.Length];
			backW = w;
			for (int y = 0; y < h; y++)
			{
				float dy = (y - h * 0.55f) / (h / 2f);
				for (int x = 0; x < w; x++)
				{
					float dx = (x - w / 2f) / (w / 2f);
					float t = Mathf.Clamp01((dy * dy + dx * dx) * 0.75f);
					back[y * w + x] = G((byte)Mathf.RoundToInt(24f - 15f * t));
				}
			}
		}
		Array.Copy(back, px, px.Length);
	}

	public static void Frame(Color32[] px, int w, int h, int inset = 3)
	{
		Rule(px, w, h, inset, w - inset - 1, inset);
		Rule(px, w, h, inset, w - inset - 1, h - inset - 1);
		for (int y = inset; y < h - inset; y += 4)
		{
			PixelFont.Fill(px, w, h, inset, y, 1, 2, Dots);
			PixelFont.Fill(px, w, h, w - inset - 1, y, 1, 2, Dots);
		}
	}

	public static void Rule(Color32[] px, int w, int h, int x0, int x1, int y)
	{
		for (; x0 <= x1; x0 += 4)
		{
			PixelFont.Fill(px, w, h, x0, y, 2, 1, Dots);
		}
	}

	public static void Box(Color32[] px, int w, int h, int x, int y, int bw, int bh, Color32 colour)
	{
		PixelFont.Fill(px, w, h, x - 2, y, bw + 4, 1, colour);
		PixelFont.Fill(px, w, h, x - 2, y + bh - 1, bw + 4, 1, colour);
		PixelFont.Fill(px, w, h, x, y - 2, 1, bh + 4, colour);
		PixelFont.Fill(px, w, h, x + bw - 1, y - 2, 1, bh + 4, colour);
	}
}
