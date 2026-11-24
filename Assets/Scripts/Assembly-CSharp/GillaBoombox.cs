// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

public class GillaBoombox : MonoBehaviour
{
	public AudioSource source;
	public PressKey muteKey;
	[Tooltip("The MUTE key's face.")]
	public PixelLabel keyFace;
	[Tooltip("The little screen in the tuner's window.")]
	public PixelLabel screen;
	[Tooltip("The lamp by the key: green while it plays, red while it's muted.")]
	public Renderer lamp;
	[Tooltip("The shop's spaces, in world space: you hear it while your head's in one.")]
	public Bounds[] shop = new Bounds[0];
	[Tooltip("How loud it plays at its loudest (the user: \"make sure the music isn't very loud\", then \"make it even quieter\").")]
	[Range(0, 1)]
	public float volume = 0.15f;
	[Tooltip("Metres in from the shop's edges over which it fades in.")]
	public float fade = 0.8f;
	public const string MutedKey = "Gilla.Boombox.Muted";
	private const float Ease = 0.6f;
	private const float OutOfStep = 0.4f;
	public static readonly Color32 Amber = new Color32(255, 176, 0, 255) /* ? */;
	public static readonly Color32 Red = new Color32(200, 30, 30, 255) /* ? */;
	public static readonly Color32 Dark = new Color32(20, 20, 20, 255) /* ? */;
	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor") /* ? */;
	private bool muted;
	private float level;
	private float nextCheck;
	private MaterialPropertyBlock block;

	public bool Muted { get { return muted; } }

	public float Level { get { return level; } }

	public float InShop { get { return Inside(Ears()); } }

	// Redux: the one in the shop, for the song player and for carrying it
	public static GillaBoombox Instance { get; private set; }

	private void OnEnable()
	{
		Instance = this;
		GillaBoomboxGrab.Attach(this);

		muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
		if (muteKey != null)
		{
			muteKey.Pressed += OnKey;
		}
		level = 0f;
		if (source != null)
		{
			source.volume = 0f;
		}
		Show();
	}

	private void OnDisable()
	{
		if (muteKey != null)
		{
			muteKey.Pressed -= OnKey;
		}
		if (source != null)
		{
			source.Pause();
		}
	}

	private void OnKey(PressKey _)
	{
		SetMuted(!muted);
	}

	public void SetMuted(bool on)
	{
		muted = on;
		PlayerPrefs.SetInt(MutedKey, on ? 1 : 0);
		PlayerPrefs.Save();
		Show();
	}

	// Redux: the front display shows the song that's playing, scrolling if it's too long
	private string screenShown;
	private float scrollAt;
	private int scrollPos;

	private void ScreenSong()
	{
		if (screen == null) return;
		string song = GillaMusic.NowPlaying;
		string want;
		if (muted) want = "MUTED";
		else if (string.IsNullOrEmpty(song)) want = "NOW PLAYING";
		else
		{
			int fits = Mathf.Max(4, (screen.width + 1) / (6 * Mathf.Max(screen.scale, 1)));
			if (song.Length <= fits) want = song;
			else
			{
				string band = song + "   *   ";
				if (Time.unscaledTime >= scrollAt)
				{
					scrollAt = Time.unscaledTime + 0.22f;
					scrollPos = (scrollPos + 1) % band.Length;
				}
				string twice = band + band;
				want = twice.Substring(scrollPos, fits);
			}
		}
		if (want == screenShown) return;
		screenShown = want;
		screen.text = want;
		screen.colour = muted ? Red : Amber;
		screen.Redraw();
	}

	private void Update()
	{
		ScreenSong();
		if (source == null)
		{
			return;
		}
		if (source.clip == null)
		{
			return;
		}
		float target = 0f;
		if (!muted && !GillaMusic.Playing)   // Redux: a requested song takes over from the shop tune
		{
			target = Inside(Ears());
		}
		level = Mathf.MoveTowards(level, target, Time.unscaledDeltaTime / Ease);
		if (level <= 0.001f)
		{
			if (source.isPlaying)
			{
				source.Pause();
			}
			source.volume = 0f;
			return;
		}
		if (source.isPlaying)
		{
			if (nextCheck <= Time.unscaledTime)
			{
				nextCheck = Time.unscaledTime + 5f;
				float length = source.clip.length;
				float time = source.time;
				float delta = Mathf.DeltaAngle(time / length * 360f, InStep(length) / length * 360f);
				if (length * (Mathf.Abs(delta) / 360f) > OutOfStep)
				{
					source.time = InStep(length);
				}
			}
		}
		else
		{
			source.time = InStep(source.clip.length);
			source.Play();
			nextCheck = Time.unscaledTime + 2f;
		}
		source.volume = level * volume;
	}

	public static float InStep(float length)
	{
		float result = 0f;
		if (length > 0f)
		{
			long ticks = DateTime.UtcNow.Ticks;
			result = (float)(((double)ticks / 10000000.0) % (double)length);
		}
		return result;
	}

	private Vector3 Ears()
	{
		AudioListener ears = GillaEars.Listening;
		if (ears != null)
		{
			return ears.transform.position;
		}
		Camera cam = Camera.main;
		if (!(cam != null))
		{
			return new Vector3(0f, -1000000f, 0f);
		}
		return cam.transform.position;
	}

	public float Inside(Vector3 p)
	{
		float best = 0f;
		for (int i = 0; i < shop.Length; i++)
		{
			Bounds b = shop[i];
			Vector3 c = b.center;
			Vector3 e = b.extents;
			if (c.y - e.y <= p.y && p.y <= c.y + e.y)
			{
				float dx = Mathf.Min(p.x - (c.x - e.x), (c.x + e.x) - p.x);
				float dz = Mathf.Min(p.z - (c.z - e.z), (c.z + e.z) - p.z);
				float d = Mathf.Min(dx, dz);
				if (d > 0f)
				{
					float t = 1f;
					if (fade > 0f)
					{
						t = Mathf.Clamp01(d / fade);
					}
					best = Mathf.Max(best, t);
				}
			}
		}
		return best;
	}

	private void Show()
	{
		if (keyFace != null)
		{
			keyFace.text = muted ? "MUTED" : "MUTE";
			keyFace.colour = muted ? new Color32(255, 235, 230, 255) : Dark;
			keyFace.background = muted ? Red : Amber;
			keyFace.Redraw();
		}
		if (screen != null)
		{
			screen.text = muted ? "MUTED" : "NOW PLAYING";
			screen.colour = muted ? Red : Amber;
			screen.Redraw();
		}
		if (!(lamp != null))
		{
			return;
		}
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		lamp.GetPropertyBlock(block);
		Color c = muted ? new Color(2.2f, 0.12f, 0.08f, 1f) : new Color(0.15f, 2f, 0.3f, 1f);
		block.SetColor(BaseColorId, c);
		lamp.SetPropertyBlock(block);
	}
}
