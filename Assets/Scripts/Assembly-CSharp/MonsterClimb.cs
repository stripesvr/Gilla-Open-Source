// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterClimb : MonoBehaviour
{
	[Tooltip("On the floor at the wall's foot, a little out from it.")]
	public Vector3 bottom;
	[Tooltip("The wall's top edge (on the floor above, right at the drop).")]
	public Vector3 rim;
	[Tooltip("On the floor above, a little back from the edge.")]
	public Vector3 top;
	[Tooltip("How wide a stretch of wall the link spans (m).")]
	public float width = 1.2f;
	private NavMeshLinkInstance link;
	public static readonly List<MonsterClimb> All = new List<MonsterClimb>();

	public Vector3 Into
	{
		get
		{
			Vector3 d = rim - bottom;
			d.y = 0f;
			if (d.sqrMagnitude <= 1e-06f)
			{
				return Vector3.forward;
			}
			return d.normalized;
		}
	}

	private void OnEnable()
	{

		All.Add(this);
	}

	private void Start()
	{
		NavMeshLinkData data = new NavMeshLinkData
		{
			startPosition = bottom,
			endPosition = top,
			width = width,
			bidirectional = true,
			area = 0,
			agentTypeID = 0,
			costModifier = -1f
		};
		link = NavMesh.AddLink(data);
	}

	private void OnDisable()
	{
		All.Remove(this);
		if (link.valid)
		{
			link.Remove();
		}
	}

	public static MonsterClimb Up(Vector3 at, Vector3 next, float near = 0.9f)
	{
		foreach (MonsterClimb c in All)
		{
			if (c == null)
			{
				continue;
			}
			float dx = c.bottom.x - at.x;
			float dz = c.bottom.z - at.z;
			if (Mathf.Sqrt(dx * dx + dz * dz) >= near || Mathf.Abs(at.y - c.bottom.y) >= 1f)
			{
				continue;
			}
			if (Vector3.Distance(c.top, next) < 1.2f)
			{
				return c;
			}
		}
		return null;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1f, 0.92156863f, 0.015686275f, 1f);
		Vector3 foot = new Vector3(rim.x, bottom.y, rim.z);
		Gizmos.DrawLine(bottom, foot);
		Gizmos.DrawLine(foot, rim);
		Gizmos.DrawLine(rim, top);
		Gizmos.DrawWireSphere(bottom, 0.15f);
		Gizmos.DrawWireSphere(top, 0.15f);
	}
}
