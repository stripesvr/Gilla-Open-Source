// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public static class GillaTurn
{
	public const int Snap = 0;
	public const int Smooth = 1;
	public const int Off = 2;
	private const string ModeKey = "GillaTurnMode";
	private const string AngleKey = "GillaSnapAngle";
	private const string SpeedKey = "GillaSmoothSpeed";
	private static bool loaded;
	private static int mode = 0;
	private static int snapAngle = 45;
	private static int smoothSpeed = 5;

	public static int Mode
	{
		get
		{
			Load();
			return mode;
		}
		set
		{
			Load();
			mode = Mathf.Clamp(value, 0, 2);
			Save();
		}
	}

	public static int SnapAngle
	{
		get
		{
			Load();
			return snapAngle;
		}
		set
		{
			Load();
			snapAngle = value >= 90 ? 90 : 45;
			Save();
		}
	}

	public static int SmoothSpeed
	{
		get
		{
			Load();
			return smoothSpeed;
		}
		set
		{
			Load();
			smoothSpeed = Mathf.Clamp(value, 0, 9);
			Save();
		}
	}

	public static float DegreesPerSecond
	{
		get { return SmoothSpeed * 60f + 30f; }
	}

	public static string Describe()
	{
		if (Mode == Snap)
		{
			return "SNAP " + SnapAngle.ToString();
		}
		if (Mode != Smooth)
		{
			return "OFF";
		}
		return "SMOOTH " + SmoothSpeed.ToString();
	}

	private static void Load()
	{
		if (!loaded)
		{
			loaded = true;
			mode = Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, 2);
			snapAngle = PlayerPrefs.GetInt(AngleKey, 45) >= 90 ? 90 : 45;
			smoothSpeed = Mathf.Clamp(PlayerPrefs.GetInt(SpeedKey, 5), 0, 9);
		}
	}

	private static void Save()
	{
		PlayerPrefs.SetInt(ModeKey, mode);
		PlayerPrefs.SetInt(AngleKey, snapAngle);
		PlayerPrefs.SetInt(SpeedKey, smoothSpeed);
		PlayerPrefs.Save();
	}

	[RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)4)]
	private static void Forget()
	{
		loaded = false;
	}
}
