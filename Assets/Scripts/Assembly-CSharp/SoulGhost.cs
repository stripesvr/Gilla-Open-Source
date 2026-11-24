// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SoulGhost : MonoBehaviour
{
	private const float Out = 0.3f;
	private const float Hang = 0.75f;
	private const float Back = 1.05f;
	private static readonly int Alpha = Shader.PropertyToID("_Alpha");
	private Vector3 from;
	private Vector3 to;
	private float t;
	private readonly List<Mesh> meshes = new List<Mesh>();
	private readonly List<Renderer> parts = new List<Renderer>();
	private MaterialPropertyBlock block;
	private SoulScythe scythe;
	private bool snapped;

	public static void Spawn(List<SkinnedMeshRenderer> body, Vector3 toward, SoulScythe scythe)
	{
		GameObject go = new GameObject("Soul");
		SoulGhost ghost = go.AddComponent<SoulGhost>();
		ghost.scythe = scythe;

		Bounds bounds = body[0].bounds;
		foreach (SkinnedMeshRenderer smr in body)
		{
			bounds.Encapsulate(smr.bounds);
		}
		Vector3 center = bounds.center;

		ghost.from = center;
		go.transform.position = center;

		Vector3 flat = new Vector3(toward.x - center.x, 0f, toward.z - center.z);
		float dist = flat.magnitude;
		float outDist = Mathf.Min(dist * 0.6f, 0.6f);
		Vector3 dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
		ghost.to = center + dir * outDist + Vector3.up * 0.12f;

		foreach (SkinnedMeshRenderer smr in body)
		{
			Mesh mesh = new Mesh();
			mesh.name = "Soul_" + smr.name;
			smr.BakeMesh(mesh, true);
			ghost.meshes.Add(mesh);

			GameObject part = new GameObject(smr.name);
			part.transform.SetParent(go.transform, false);
			part.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
			MeshFilter filter = part.AddComponent<MeshFilter>();
			filter.sharedMesh = mesh;
			MeshRenderer renderer = part.AddComponent<MeshRenderer>();
			Material[] mats = new Material[mesh.subMeshCount];
			for (int i = 0; i < mats.Length; i++)
			{
				mats[i] = scythe.ghost;
			}
			renderer.sharedMaterials = mats;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			ghost.parts.Add(renderer);
		}

		if (scythe.yanks != null)
		{
			Sfx.Play(scythe.yanks, center, 0.8f, 0.92f, 1.08f);
		}
	}

	private void Update()
	{
		t += Time.deltaTime;
		float ease;
		float alpha;
		if (t < Out)
		{
			float k = 1f - t / Out;
			ease = 1f - k * k * k;
			alpha = Mathf.Clamp01(t / Out * 3f);
		}
		else
		{
			ease = 1f;
			alpha = 1f;
			if (t >= Hang)
			{
				float u = Mathf.Clamp01((t - Hang) / (Back - Hang));
				ease = 1f - u * u * u;
				alpha = 1f - u * u;
			}
		}

		if (t >= Hang && !snapped)
		{
			snapped = true;
			if (scythe != null)
			{
				if (scythe.snaps != null)
				{
					Sfx.Play(scythe.snaps, to, 0.6f, 0.95f, 1.1f);
				}
			}
		}

		float shake = 0f;
		if (t >= Out && t < Hang)
		{
			shake = Mathf.Sin(t * 60f) * 0.01f;
		}

		transform.position = from + (to - from) * ease + Vector3.right * shake;
		transform.localScale = Vector3.one * (ease * 0.08f + 1f);

		if (block == null)
		{
			block = new MaterialPropertyBlock();
		}
		block.SetFloat(Alpha, alpha);
		foreach (Renderer part in parts)
		{
			if (part != null)
			{
				part.SetPropertyBlock(block);
			}
		}

		if (t >= Back)
		{
			Destroy(gameObject);
		}
	}

	private void OnDestroy()
	{
		foreach (Mesh mesh in meshes)
		{
			if (mesh != null)
			{
				Destroy(mesh);
			}
		}
	}
}
