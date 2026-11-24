// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[ExecuteAlways]
public class GillaUpdateBoard : MonoBehaviour
{
	public Renderer screen;
	[Tooltip("What's new (Assets/Gilla/UpdateNotes.txt).")]
	public TextAsset notes;
	[Tooltip("The keys that turn the pages: back, on.")]
	public PressKey upKey;
	[Tooltip("The keys that turn the pages: back, on.")]
	public PressKey downKey;
	[Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
	public TextAsset titleFont;
	[Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
	public TextAsset textFont;
	private const int W = 320;
	private const int H = 240;
	private const int Top = 42;
	private const int Step = 10;
	private const int Wrap = 49;
	private const int Rows = 18;
	private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
	private Texture2D tex;
	private Color32[] px;
	private MaterialPropertyBlock block;
	private ScreenFont title;
	private ScreenFont text;
	private string heading = "";
	private readonly List<(string line, bool head)> lines = new List<(string line, bool head)>();
	private int page;
	private int read = -1;

	private int Pages
	{
		get
		{
			int n = (lines.Count + 17) / 18;
			if (n < 2) n = 1;
			return n;
		}
	}

	private void OnEnable()
	{

		if (upKey != null)
		{
			upKey.Pressed += OnKey;
		}
		if (downKey != null)
		{
			downKey.Pressed += OnKey;
		}
		Redraw();
	}

	private void OnDisable()
	{
		if (upKey != null)
		{
			upKey.Pressed -= OnKey;
		}
		if (downKey != null)
		{
			downKey.Pressed -= OnKey;
		}
	}

	private void OnKey(PressKey key)
	{
		int next = key == downKey ? page + 1 : page - 1;
		int pages = Pages;
		int clamped = next;
		if (pages - 1 <= next) clamped = pages - 1;
		page = next < 0 ? 0 : clamped;
		Redraw();
	}

	public void Refresh()
	{
		read = -1;
		Redraw();
	}

	private void Read()
	{
		int hash = notes != null ? notes.text.GetHashCode() : 0;
		if (hash == read)
		{
			return;
		}
		read = hash;
		lines.Clear();
		heading = "";
		if (notes == null)
		{
			return;
		}
		string[] split = notes.text.Replace("\r", "").Split('\n');
		heading = split.Length != 0 ? split[0].Trim() : "";
		for (int i = 1; i < split.Length; i++)
		{
			string s = split[i].TrimEnd();
			if (s.StartsWith("# "))
			{
				lines.Add((s.Substring(2), true));
				continue;
			}
			string prefix;
			string indent;
			if (s.StartsWith("- "))
			{
				s = s.Substring(2);
				indent = "  ";
				prefix = "* ";
			}
			else
			{
				indent = "";
				prefix = "";
			}
			while (true)
			{
				int max = 49 - prefix.Length;
				if (s.Length <= max) break;
				int cut = s.LastIndexOf(' ', max);
				if (cut < 1)
				{
					cut = 49 - prefix.Length;
				}
				lines.Add((prefix + s.Substring(0, cut), false));
				s = s.Substring(cut).TrimStart();
				prefix = indent;
			}
			lines.Add((prefix + s, false));
		}
		while (lines.Count > 0)
		{
			if (lines[lines.Count - 1].Item1.Length != 0) break;
			lines.RemoveAt(lines.Count - 1);
		}
		int pages = Pages;
		int clamped = page;
		if (pages - 1 <= page) clamped = pages - 1;
		page = page < 0 ? 0 : clamped;
	}

	private void Redraw()
	{
		Read();
		if (tex == null)
		{
			Texture2D t = new Texture2D(320, 240, TextureFormat.RGBA32, true);
			t.filterMode = FilterMode.Point;
			t.wrapMode = TextureWrapMode.Clamp;
			t.hideFlags = HideFlags.HideAndDontSave;
			t.name = "GillaUpdates";
			tex = t;
			px = new Color32[76800];
		}
		GillaBoardLook.Clear(px, 320, 240);
		GillaBoardLook.Frame(px, 320, 240, 3);
		if (title == null)
		{
			title = titleFont != null ? new ScreenFont(titleFont.text) : null;
		}
		if (text == null)
		{
			text = textFont != null ? new ScreenFont(textFont.text) : null;
		}
		Line(title, 12, 7, "What's New", new ScreenFont.Style { scale = 2, slant = 0f, colours = GillaBoardLook.White }, false);
		Line(text, 12, 25, heading, new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.Dim }, false);
		GillaBoardLook.Rule(px, 320, 240, 10, 309, 36);
		int y = 42;
		for (int i = 0; i < 18; i++)
		{
			int index = i + page * 18;
			if (lines.Count <= index) break;
			ValueTuple<string, bool> entry = lines[index];
			ScreenFont.Style style = new ScreenFont.Style
			{
				scale = 1,
				slant = 0f,
				bold = entry.Item2,
				colours = entry.Item2 ? GillaBoardLook.White : GillaBoardLook.Soft
			};
			Line(text, 10, y, entry.Item1, style, false);
			y += 10;
		}
		if (Pages > 1)
		{
			string label = string.Concat(new string[] { "Page ", (page + 1).ToString(), " of ", Pages.ToString(), "  ^ v" });
			Line(text, 160, 226, label, new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.Dim }, true);
		}
		tex.SetPixels32(px);
		tex.Apply(true);
		if (screen == null)
		{
			return;
		}
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		screen.GetPropertyBlock(block);
		block.SetTexture(BaseMap, tex);
		screen.SetPropertyBlock(block);
	}

	private void Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style, bool centred = true)
	{
		if (font == null)
		{
			int scale = style.scale;
			if (scale < 2) scale = 1;
			if (centred)
			{
				x -= PixelFont.Measure(line, scale) / 2;
			}
			PixelFont.Draw(px, 320, 240, x, y, line, style.colours[0], scale);
		}
		else if (centred)
		{
			font.DrawCentred(px, 320, 240, x, y, line, style);
		}
		else
		{
			font.Draw(px, 320, 240, x, y, line, style);
		}
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
