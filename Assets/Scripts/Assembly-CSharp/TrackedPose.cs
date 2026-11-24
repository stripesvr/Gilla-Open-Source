// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.XR;

[DefaultExecutionOrder(-100)]
public class TrackedPose : MonoBehaviour
{
	public XRNode node = XRNode.Head;
	[Tooltip("Head only: cap on standing eye height, so a tall player plays the same size as everyone else (~5'8\" body). 0 = uncapped.")]
	public float maxHeadHeight;
	[Tooltip("Hands only: how much longer the ape's arms reach than yours. The hand sits this many times as far from your head as your real hand does (Gorilla Tag's 'longer arms'); 1 = where your hand is.")]
	public float armLength = 1f;
	private const float LostFor = 1.5f;
	private const float FurthestReach = 1.3f;
	private float lostAt = -1f;
	private Transform _head;
	private bool _lookedForHead;

	private void Update()
	{
		Apply();
	}

	private void OnEnable()
	{

		Application.onBeforeRender += Apply;
	}

	private void OnDisable()
	{
		Application.onBeforeRender -= Apply;
	}

	private void Apply()
	{
		InputDevice device = InputDevices.GetDeviceAtXRNode(node);
		bool tracked = false;
		Vector3 pos = Vector3.zero;
		Quaternion rot = default(Quaternion);
		bool isHand = node == XRNode.LeftHand || node == XRNode.RightHand;
		bool lost;

		bool lostPath;
		if (device.isValid)
		{
			bool gotTracked = device.TryGetFeatureValue(CommonUsages.isTracked, out tracked);
			lostPath = !tracked && gotTracked;
		}
		else
		{
			lostPath = true;
		}

		if (lostPath)
		{
			if (!isHand)
			{
				return;
			}
			if (lostAt < 0f)
			{
				lostAt = Time.unscaledTime;
			}
			if (!device.isValid || Time.unscaledTime - lostAt > LostFor)
			{
				ParkAtChest();
				return;
			}
			isHand = true;
			lost = true;
		}
		else
		{
			lost = false;
			lostAt = -1f;
		}

		if (device.TryGetFeatureValue(CommonUsages.devicePosition, out pos))
		{
			if (node == XRNode.Head && maxHeadHeight > 0f && maxHeadHeight < pos.y)
			{
				pos.y = maxHeadHeight;
			}
			Vector3 headPos = Vector3.zero;
			bool apply = true;
			if (isHand)
			{
				InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
				bool gotHead = headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headPos);
				if (lost)
				{
					if (!gotHead || (pos - headPos).sqrMagnitude >= FurthestReach * FurthestReach)
					{
						apply = false;
					}
					gotHead = true;
				}
				if (apply && gotHead && armLength != 1f)
				{
					pos = headPos + (pos - headPos) * armLength;
				}
			}
			else if (lost)
			{
				apply = false;
			}
			if (apply)
			{
				transform.localPosition = pos;
			}
		}

		if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
		{
			transform.localRotation = rot;
		}
	}

	private void ParkAtChest()
	{
		if (!_lookedForHead)
		{
			if (transform.parent != null)
			{
				_head = transform.parent.Find("Head");
				_lookedForHead = true;
			}
		}
		if (_head == null)
		{
			return;
		}
		Vector3 fwd = _head.localRotation * Vector3.forward;
		Vector3 flat;
		if (fwd.x * fwd.x + fwd.z * fwd.z >= 0.0001f)
		{
			flat = new Vector3(fwd.x, 0f, fwd.z);
		}
		else
		{
			flat = Vector3.forward;
		}
		flat = flat.normalized;
		float side = node == XRNode.LeftHand ? -0.16f : 0.16f;
		Vector3 target = _head.localPosition + flat * 0.18f + Vector3.Cross(Vector3.up, flat) * side + Vector3.down * 0.38f;
		float t = 1f - Mathf.Exp(Time.deltaTime * -8f);
		transform.localPosition = Vector3.Lerp(transform.localPosition, target, t);
		transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.LookRotation(flat, Vector3.up) * Quaternion.Euler(20f, 0f, 0f), t);
	}
}
