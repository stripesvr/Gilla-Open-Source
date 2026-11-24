// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using GorillaLocomotion; /* ? Player type assumed to live in GorillaLocomotion namespace */

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(Player))]
public class MovementFeel : MonoBehaviour
{
	[Tooltip("Earth is 9.81. Lower = floatier.")]
	[Range(4, 12)]
	public float gravity = 7.6f;
	[Tooltip("How much of your arm swing becomes launch speed.")]
	[Range(0.8f, 2)]
	public float launchMultiplier = 1.25f;
	[Tooltip("Cap on launch speed, m/s.")]
	[Range(4, 12)]
	public float maxLaunchSpeed = 7.5f;
	[Tooltip("How fast (m/s) the physics may push your body back out of a wall or ledge it was pushed into. Unity's default, 10, threw you; low eases you out.")]
	[Range(0.5f, 10)]
	public float wallPushOut = 1.5f;

	private void Awake()
	{

		Apply();
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			Apply();
		}
	}

	private void Apply()
	{
		Physics.gravity = new Vector3(0f, -gravity, 0f);
		Physics.simulationMode = SimulationMode.Update;
		Player player = GetComponent<Player>();
		player.maxJumpSpeed = maxLaunchSpeed; /* ? field at 0x90; GorillaLocomotion.Player.maxJumpSpeed assumed */
		player.jumpMultiplier = launchMultiplier; /* ? field at 0x94; GorillaLocomotion.Player.jumpMultiplier assumed */
		Rigidbody body = GetComponent<Rigidbody>();
		if (body != null)
		{
			body.interpolation = RigidbodyInterpolation.None;
			body.maxDepenetrationVelocity = wallPushOut;
		}
	}
}
