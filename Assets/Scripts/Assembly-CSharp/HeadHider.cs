// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public static class HeadHider
{
	public static readonly string[] HeadBones = new string[] { "head", "eye_L", "eye_R" };

	public static Mesh BuildHeadless(Mesh src, Transform[] bones, float threshold = 0.3f)
	{
		string[] cut = HeadBones;
		bool[] isCut = new bool[bones.Length];
		for (int i = 0; i < bones.Length; i++)
		{
			isCut[i] = bones[i] != null && Array.IndexOf(cut, bones[i].name) >= 0;
		}
		BoneWeight[] weights = src.boneWeights;
		float[] headWeight = new float[weights.Length];
		for (int i = 0; i < weights.Length; i++)
		{
			BoneWeight w = weights[i];
			float w0 = isCut[w.boneIndex0] ? w.weight0 : 0f;
			float w1 = isCut[w.boneIndex1] ? w.weight1 : 0f;
			float w2 = isCut[w.boneIndex2] ? w.weight2 : 0f;
			float w3 = isCut[w.boneIndex3] ? w.weight3 : 0f;
			headWeight[i] = w3 + w2 + w0 + w1;
		}
		Mesh mesh = UnityEngine.Object.Instantiate<Mesh>(src);
		mesh.name = src.name + "_FirstPerson";
		List<int> kept = new List<int>();
		for (int sub = 0; sub < src.subMeshCount; sub++)
		{
			int[] tris = src.GetTriangles(sub);
			kept.Clear();
			for (int t = 0; t < tris.Length; t += 3)
			{
				if (headWeight[tris[t]] < threshold && headWeight[tris[t + 1]] < threshold && headWeight[tris[t + 2]] < threshold)
				{
					kept.Add(tris[t]);
					kept.Add(tris[t + 1]);
					kept.Add(tris[t + 2]);
				}
			}
			mesh.SetTriangles(kept, sub);
		}
		return mesh;
	}

	public static Mesh BuildHeadless(Mesh src, Transform[] bones, string[] cut, float threshold)
	{
		bool[] isCut = new bool[bones.Length];
		for (int i = 0; i < bones.Length; i++)
		{
			isCut[i] = bones[i] != null && Array.IndexOf(cut, bones[i].name) >= 0;
		}
		BoneWeight[] weights = src.boneWeights;
		float[] headWeight = new float[weights.Length];
		for (int i = 0; i < weights.Length; i++)
		{
			BoneWeight w = weights[i];
			float w0 = isCut[w.boneIndex0] ? w.weight0 : 0f;
			float w1 = isCut[w.boneIndex1] ? w.weight1 : 0f;
			float w2 = isCut[w.boneIndex2] ? w.weight2 : 0f;
			float w3 = isCut[w.boneIndex3] ? w.weight3 : 0f;
			headWeight[i] = w3 + w2 + w0 + w1;
		}
		Mesh mesh = UnityEngine.Object.Instantiate<Mesh>(src);
		mesh.name = src.name + "_FirstPerson";
		List<int> kept = new List<int>();
		for (int sub = 0; sub < src.subMeshCount; sub++)
		{
			int[] tris = src.GetTriangles(sub);
			kept.Clear();
			for (int t = 0; t < tris.Length; t += 3)
			{
				if (headWeight[tris[t]] < threshold && headWeight[tris[t + 1]] < threshold && headWeight[tris[t + 2]] < threshold)
				{
					kept.Add(tris[t]);
					kept.Add(tris[t + 1]);
					kept.Add(tris[t + 2]);
				}
			}
			mesh.SetTriangles(kept, sub);
		}
		return mesh;
	}
}
