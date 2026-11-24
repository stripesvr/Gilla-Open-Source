// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using GorillaLocomotion;

public class GillaKeycardReader : MonoBehaviour
{
	public GillaDoor door;
	[Tooltip("The strip of light (a plain colour: red shut, green open).")]
	public Renderer strip;
	[Tooltip("How near its middle (m) the card has to come, and a hand.")]
	public float reach = 0.24f;
	[Tooltip("How near its middle (m) the card has to come, and a hand.")]
	public float handReach = 0.14f;
	[Tooltip("How long the door stays open (s).")]
	public float stayOpen = 12f;
	public AudioClip acceptSound;
	public AudioClip denySound;
	public Color lockedColour = new Color(0.9f, 0.08f, 0.06f, 1f);
	public Color openColour = new Color(0.1f, 0.85f, 0.2f, 1f);
	private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
	private const float Again = 0.6f;
	private MaterialPropertyBlock block;
	private int shown = -1;
	private bool touching;
	private float nextTry;
	private float openSince = -1f;

	public bool ShowsOpen
	{
		get { return shown == 1; }
	}

	private void Update()
	{
		if (door == null)
		{
			return;
		}
		Player gt = Player.Instance /* ? */;
		bool card = CardHere();
		bool hand = gt != null ? HandHere(gt) : false;
		if (!door.IsOpen && nextTry <= Time.time)
		{
			if (card && !door.Busy)
			{
				nextTry = Time.time + Again;
				door.Press();
				GillaQuests.Report(GillaQuests.Stat.Doors, 1);
				Beep(acceptSound, 0.5f);
			}
			else if (hand && !touching)
			{
				nextTry = Time.time + Again;
				Beep(denySound, 0.3f);
			}
		}
		touching = card | hand;
		if (door.IsOpen && !door.IsMoving)
		{
			if (openSince < 0f)
			{
				openSince = Time.time;
			}
			float extra = 0f;
			if (NetworkApe.Local != null)
			{
				extra = (float)(NetworkApe.Local.Object.InputAuthority.PlayerId /* ? */ % 4) * 0.4f;
			}
			if (extra + stayOpen < Time.time - openSince)
			{
				if (!door.Busy && !SomeoneInDoorway())
				{
					door.Press();
				}
			}
		}
		else
		{
			openSince = -1f;
		}
		Show(door.IsOpen);
	}

	private bool CardHere()
	{
		foreach (GillaItem item in GillaItem.All)
		{
			if (item != null && item.name.IndexOf("Keycard", StringComparison.OrdinalIgnoreCase) >= 0 /* ? */ && item.transform.parent != null /* ? */ && item.Mine)
			{
				if ((item.transform.position - transform.position).sqrMagnitude < reach * reach)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool HandHere(Player gt)
	{
		Transform left = gt.leftHandFollower;
		Transform right = gt.rightHandFollower;
		if (left != null)
		{
			if ((left.position - transform.position).sqrMagnitude < handReach * handReach)
			{
				return true;
			}
		}
		if (right == null)
		{
			return false;
		}
		return (right.position - transform.position).sqrMagnitude < handReach * handReach;
	}

	private bool SomeoneInDoorway()
	{
		Vector3 foot = door.Foot;
		Vector3 side = Vector3.Cross(Vector3.up, door.Through);
		if (KillCam.Local != null && KillCam.Local.head != null)
		{
			if (In(KillCam.Local.head.position, foot, side))
			{
				return true;
			}
		}
		foreach (NetworkApe ape in NetworkApe.All)
		{
			if (ape != null && ape.head /* ? */ != null)
			{
				if (In(ape.head /* ? */.position, foot, side))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool In(Vector3 p, Vector3 foot, Vector3 side)
	{
		Vector3 d = p - foot;
		if (Mathf.Abs(Vector3.Dot(d, door.Through)) < door.Thickness /* ? */ * 0.5f + 0.7f
			&& Mathf.Abs(Vector3.Dot(d, side)) < door.Width /* ? */ * 0.5f + 0.3f)
		{
			return Mathf.Abs(d.y) < 3f;
		}
		return false;
	}

	private void Beep(AudioClip clip, float buzz)
	{
		if (clip != null)
		{
			Sfx.Play(new AudioClip[] { clip }, transform.position, 0.8f, 1f, 1f);
		}
		InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
		right.SendHapticImpulse(0, buzz, 0.06f);
		InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
		left.SendHapticImpulse(0, buzz, 0.06f);
	}

	private void Show(bool open)
	{
		if (shown == (open ? 1 : 0))
		{
			return;
		}
		if (strip == null)
		{
			return;
		}
		shown = open ? 1 : 0;
		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		strip.GetPropertyBlock(block);
		block.SetColor(BaseColorId, open ? openColour : lockedColour);
		strip.SetPropertyBlock(block);
	}
}
