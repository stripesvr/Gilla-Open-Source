// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

public static class GillaSticks
{
	private static readonly List<InputDevice> devices = new List<InputDevice>();

	// Redux: the keyboard rig's WASD, standing in for the left stick on PC.
	public static bool DeskOn;
	public static Vector2 DeskLeft;

	public static Vector2 Read(XRNode hand, float deadzone = 0.15f)
	{
		if (DeskOn && hand == XRNode.LeftHand)
		{
			return DeskLeft;
		}
		Vector2 best = Vector2.zero;
		devices.Clear();
		InputDeviceCharacteristics chars = hand == XRNode.LeftHand
			? (InputDeviceCharacteristics)0x140
			: (InputDeviceCharacteristics)0x240;
		InputDevices.GetDevicesWithCharacteristics(chars, devices);
		InputDevice nodeDevice = InputDevices.GetDeviceAtXRNode(hand);
		if (nodeDevice.isValid && !devices.Contains(nodeDevice))
		{
			devices.Add(nodeDevice);
		}
		foreach (InputDevice device in devices)
		{
			if (!device.isValid)
			{
				continue;
			}
			Vector2 value;
			if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out value) && value.sqrMagnitude > best.sqrMagnitude)
			{
				best = value;
			}
		}
		XRController controller = hand == XRNode.LeftHand ? XRController.leftHand : XRController.rightHand;
		if (controller != null)
		{
			Vector2Control stick = controller.TryGetChildControl<Vector2Control>("thumbstick");
			if (stick == null)
			{
				stick = controller.TryGetChildControl<Vector2Control>("primary2DAxis");
			}
			if (stick != null)
			{
				Vector2 v = stick.ReadValue();
				if (best.sqrMagnitude < v.sqrMagnitude)
				{
					best = v;
				}
			}
		}
		float magnitude = best.magnitude;
		if (magnitude < deadzone)
		{
			return Vector2.zero;
		}
		if (best.sqrMagnitude > 1f)
		{
			best = best / magnitude;
		}
		return best;
	}
}
