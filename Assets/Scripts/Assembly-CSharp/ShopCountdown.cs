// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;

[RequireComponent(typeof(PixelLabel))]
public class ShopCountdown : MonoBehaviour
{
	private PixelLabel label;
	private float next;

	private void OnEnable()
	{

		label = GetComponent<PixelLabel>();
		next = 0f;
	}

	private void Update()
	{
		if (Time.unscaledTime < next)
		{
			return;
		}
		next = Time.unscaledTime + 1f;
		string text = Text();
		if (text == label.text)
		{
			return;
		}
		label.text = text;
		label.Redraw();
	}

	public static string Text()
	{
		TimeSpan left = ShopStand.NextStock - DateTime.UtcNow;
		if (left < TimeSpan.Zero)
		{
			left = TimeSpan.Zero;
		}
		string[] values = new string[6];
		string days = "";
		if (left.Days > 0)
		{
			days = left.Days.ToString() + "D ";
		}
		values[0] = days;
		values[1] = left.Hours.ToString("00");
		values[2] = ":";
		values[3] = left.Minutes.ToString("00");
		values[4] = ":";
		values[5] = left.Seconds.ToString("00");
		return "NEW STOCK IN\n*" + string.Concat(values);
	}
}
