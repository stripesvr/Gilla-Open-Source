// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Text;
using UnityEngine;

[ExecuteAlways]
public class GillaQuestBoard : MonoBehaviour
{
	public Renderer screen;
	[Tooltip("The GET keys, one beside each quest, top to bottom.")]
	public PressKey[] claimKeys = new PressKey[0];
	[Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
	public TextAsset titleFont;
	[Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
	public TextAsset textFont;
	public AudioClip claimSound;
	public AudioClip nopeSound;
	public AudioClip levelUpSound;
	private const int W = 320;
	private const int H = 240;
	private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
	private Texture2D tex;
	private Color32[] px;
	private MaterialPropertyBlock block;
	private ScreenFont title;
	private ScreenFont text;
	private string shown;
	private float nextRefresh;
	private float levelUpUntil;

	private void OnEnable()
	{

		foreach (PressKey key in claimKeys)
		{
			if (key != null)
			{
				key.Pressed += OnKey;
			}
		}
		GillaQuests.Changed += OnChanged;
		GillaLevel.Changed += OnChanged;
		Redraw(true);
	}

	private void OnDisable()
	{
		foreach (PressKey key in claimKeys)
		{
			if (key != null)
			{
				key.Pressed -= OnKey;
			}
		}
		GillaQuests.Changed -= OnChanged;
		GillaLevel.Changed -= OnChanged;
	}

	private void OnChanged()
	{
		Redraw(false);
	}

	private void Update()
	{
		if (Application.isPlaying && nextRefresh <= Time.time)
		{
			nextRefresh = Time.time + 0.5f;
			Redraw(false);
		}
	}

	private void OnKey(PressKey key)
	{
		int index = Array.IndexOf(claimKeys, key);
		int result = GillaQuests.Claim(index);
		AudioClip clip;
		if (result < 1)
		{
			if (result < 0)
			{
				clip = nopeSound;
			}
			else
			{
				clip = claimSound;
			}
		}
		else
		{
			levelUpUntil = Time.time + 3f;
			clip = levelUpSound;
			if (!(clip != null))
			{
				clip = claimSound;
			}
		}
		if (clip != null)
		{
			Sfx.Play(new AudioClip[] { clip }, key.transform.position, result >= 0 ? 0.8f : 0.4f, 0.95f, 1.05f);
		}
		Redraw(false);
	}

	public void Refresh()
	{
		Redraw(true);
	}

	private void Redraw(bool force)
	{
		TimeSpan untilNew = GillaQuests.UntilNew;
		bool blink;
		if (Application.isPlaying)
		{
			blink = ((int)(Time.time + Time.time) & 1) == 0;
		}
		else
		{
			blink = false;
		}
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < 3; i++)
		{
			GillaQuests.Quest q = GillaQuests.QuestAt(i);
			sb.Append(q.text).Append(GillaQuests.ProgressAt(i)).Append(GillaQuests.ClaimedAt(i)).Append('|');
		}
		bool levelUp;
		if (Application.isPlaying)
		{
			levelUp = Time.time < levelUpUntil;
		}
		else
		{
			levelUp = false;
		}
		string key = string.Concat(new string[]
		{
			sb.ToString(),
			untilNew.Hours.ToString(),
			":",
			untilNew.Minutes.ToString(),
			":",
			untilNew.Seconds.ToString(),
			blink.ToString(),
			GillaLevel.Xp.ToString(),
			levelUp.ToString()
		});
		if (!force && key == shown && tex != null)
		{
			return;
		}
		shown = key;
		if (tex == null)
		{
			Texture2D t = new Texture2D(320, 240, TextureFormat.RGBA32, true);
			t.filterMode = FilterMode.Point;
			t.wrapMode = TextureWrapMode.Clamp;
			t.hideFlags = (HideFlags)0x34;
			t.name = "GillaQuests";
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
		Line(title, 12, 9, "Daily Quests", new ScreenFont.Style { scale = 2, colours = GillaBoardLook.White }, false);
		string countdown = string.Concat(new string[]
		{
			"NEW IN ",
			untilNew.Hours.ToString("00"),
			":",
			untilNew.Minutes.ToString("00"),
			":",
			untilNew.Seconds.ToString("00")
		});
		int cw = Measure(text, countdown, 1);
		Line(text, 308 - cw, 15, countdown, new ScreenFont.Style { scale = 1, colours = GillaBoardLook.Dim }, false);
		GillaBoardLook.Rule(px, 320, 240, 10, 309, 30);
		int y = 69;
		for (int i = 0; i < 3; i++)
		{
			GillaQuests.Quest q = GillaQuests.QuestAt(i);
			int progress = GillaQuests.ProgressAt(i);
			int goal = q.goal;
			bool claimed = GillaQuests.ClaimedAt(i);
			Fill(10, y - 31, 300, 48, GillaBoardLook.Band);
			ScreenFont nameFont = Measure(title, q.text, 1) > 222 ? text : title;
			Line(nameFont, 16, y - 25, q.text, new ScreenFont.Style { scale = 1, colours = claimed ? GillaBoardLook.Dim : GillaBoardLook.White }, false);
			string reward = "+" + GillaLevel.ForYou(q.xp).ToString() + " XP   +" + 50.ToString() + " COINS";
			Line(text, 16, y - 12, reward, new ScreenFont.Style { scale = 1, colours = claimed ? GillaBoardLook.Dim : GillaBoardLook.Soft }, false);
			if (claimed)
			{
				Line(text, 16, y, "CLAIMED", new ScreenFont.Style { scale = 1, colours = GillaBoardLook.Dim }, false);
			}
			else if (goal <= progress)
			{
				if (blink || !Application.isPlaying)
				{
					Line(text, 16, y, "DONE! PRESS GET >>", new ScreenFont.Style { bold = true, scale = 1, colours = GillaBoardLook.White }, false);
				}
			}
			string count;
			if (q.stat == GillaQuests.Stat.FlashlightSeconds)
			{
				count = (progress / 60).ToString() + ":" + (progress % 60).ToString("00") + "/" + (goal / 60).ToString() + ":00";
			}
			else
			{
				count = progress.ToString() + " / " + goal.ToString();
			}
			GillaBoardLook.Box(px, 320, 240, 240, y - 23, 64, 24, claimed ? GillaBoardLook.Edge : GillaBoardLook.Solid);
			string label = claimed ? "DONE" : (progress < goal ? count : "GET");
			Line(text, 272, y - 15, label, new ScreenFont.Style { bold = goal <= progress && !claimed, scale = 1, colours = null /* ? */ }, true);
			y += 58;
		}
		GillaBoardLook.Rule(px, 320, 240, 10, 309, 211);
		int into = GillaLevel.IntoLevel;
		int forNext = GillaLevel.ForNext;
		string levelLine;
		if (!(blink && levelUp))
		{
			levelLine = "Level " + GillaLevel.Level.ToString() + "   " + into.ToString() + "/" + forNext.ToString() + " XP";
		}
		else
		{
			levelLine = "LEVEL UP!  Level " + GillaLevel.Level.ToString();
		}
		Line(text, 14, 219, levelLine, new ScreenFont.Style { bold = levelUp, scale = 1, colours = GillaBoardLook.White }, false);
		GillaBoardLook.Box(px, 320, 240, 186, 219, 118, 8, GillaBoardLook.Edge);
		int denom = forNext < 2 ? 1 : forNext;
		Fill(188, 221, Mathf.RoundToInt(into * 114f / denom), 4, GillaBoardLook.Solid);
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

	private static int Measure(ScreenFont font, string s, int scale)
	{
		if (font != null)
		{
			return font.Measure(s, scale);
		}
		return PixelFont.Measure(s, scale);
	}

	private void Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style, bool centred = true)
	{
		if (font == null)
		{
			int scale = style.scale;
			if (scale < 2)
			{
				scale = 1;
			}
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
