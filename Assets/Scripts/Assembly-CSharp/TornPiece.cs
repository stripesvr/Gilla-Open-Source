// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TornPiece : MonoBehaviour
{
	public AudioClip[] dropSounds;
	[Tooltip("Seconds till it's gone.")]
	public float life = float.PositiveInfinity;
	[Tooltip("Impact speed (m/s) for a full-volume thud.")]
	public float hardHitSpeed = 4f;
	[Tooltip("Slower than this (m/s) is a roll, not a landing - no sound.")]
	public float minSpeed = 0.7f;
	[HideInInspector]
	public TornPiece thudsOn;
	private int thuds;
	private float cool;
	private Dictionary<Material, Material> own;

	public Material[] Own(Material[] mats)
	{
		if (own == null)
		{
			own = new Dictionary<Material, Material>();
		}
		Material[] result = new Material[mats.Length];
		for (int i = 0; i < mats.Length; i++)
		{
			if (mats[i] == null)
			{
				continue;
			}
			Material copy;
			if (!own.TryGetValue(mats[i], out copy))
			{
				Material source = mats[i];
				copy = new Material(source);
				own[source] = copy;
			}
			result[i] = copy;
		}
		return result;
	}

	public void Again()
	{
		thuds = 0;
	}

	private void OnDestroy()
	{
		if (own != null)
		{
			foreach (Material m in own.Values)
			{
				if (m != null)
				{
					Destroy(m);
				}
			}
		}
	}

	private void Update()
	{
		cool -= Time.deltaTime;
		life -= Time.deltaTime;
		if (life > 0f)
		{
			return;
		}
		Destroy(gameObject);
	}

	private void OnCollisionEnter(Collision c)
	{
		TornPiece piece = this;
		while (piece.thudsOn != null)
		{
			piece = piece.thudsOn;
		}
		Vector3 v = c.relativeVelocity;
		float speed = v.magnitude;
		if (piece.thuds < 3 && piece.cool <= 0f && speed >= piece.minSpeed && c.contactCount != 0)
		{
			float strength = Math.Clamp(speed / piece.hardHitSpeed, 0f, 1f);
			float scale = piece.thuds != 0 ? 0.5f : 1f;
			ContactPoint contact = c.GetContact(0);
			float volume = Mathf.Max(0.25f, strength * scale);
			Sfx.Play(piece.dropSounds, contact.point, volume, 0.9f, 1.1f);
			piece.thuds++;
			piece.cool = 0.15f;
		}
	}
}
