// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.XR;

public class SnapTurn : MonoBehaviour
{
	public float deadzone = 0.7f;
	[Tooltip("A smooth turn starts past this much stick, and is at full speed with it right over.")]
	public float smoothDeadzone = 0.2f;
	public static Nullable<Vector2> TestStick;
	private bool latched;

	private void Update()
	{
		GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance;
		if (player == null)
		{
			return;
		}
		float x;
		if (TestStick.HasValue)
		{
			x = TestStick.Value.x;
		}
		else
		{
			x = GillaSticks.Read(XRNode.RightHand, 0f).x;
		}
		int mode = GillaTurn.Mode;
		if (mode == 1)
		{
			float dz = smoothDeadzone;
			latched = false;
			float over = Mathf.Abs(x) - dz;
			if (0f < over)
			{
				float degPerSec = GillaTurn.DegreesPerSecond;
				float dt = Time.deltaTime;
				float signed = 0f <= x ? over : -over;
				player.Turn(signed / (1f - dz) * degPerSec * dt);
			}
			return;
		}
		if (mode == 0)
		{
			if (!latched && deadzone < Mathf.Abs(x))
			{
				int angle = GillaTurn.SnapAngle;
				player.Turn(0f <= x ? (float)angle : -(float)angle);
				latched = true;
				return;
			}
			if (0.3f <= Mathf.Abs(x))
			{
				return;
			}
		}
		latched = false;
	}
}
