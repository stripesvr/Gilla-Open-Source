using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaWounds
{
private (Transform bone, Matrix4x4 bind) BoneOf(GoreSection section, Vector3 middle)
{
	if (fitted != null)
	{
		MonsterGore.Limb limb = LimbNear(section, middle);
		if (limb == null)
		{
			limb = fitted.limbs[0];
		}
		var found = bones[limb.bone];
		return (found.bone, found.bind);
	}

	string name = null;
	if (section == GoreSection.Torso)
	{
		name = "spine";
	}
	else if (section == GoreSection.Head)
	{
		name = "head";
	}
	if (name != null)
	{
		string key = bones.ContainsKey(name) ? name : "spine";
		var found = bones[key];
		return (found.bone, found.bind);
	}

	string suffix = section == GoreSection.ArmL ? "_L" : "_R";
	var upper = bones["upperarm" + suffix];
	var lower = bones["lowerarm" + suffix];
	Vector3 a = upper.bind.inverse.MultiplyPoint3x4(lower.bone.localPosition * 0.5f) - middle;
	Vector3 b = lower.bind.inverse.MultiplyPoint3x4(Vector3.up * 0.09f) - middle;
	var pick = b.sqrMagnitude <= a.sqrMagnitude ? lower : upper;
	return (pick.bone, pick.bind);
}

private void Wall(int w, GoreSection section)
{
	CraterIn c = Prep(w, section);
	if (c != null)
	{
		CraterOut o = Crater(c);
		PutCrater(c, o);
	}
}

private CraterIn Prep(int w, GoreSection section)
{
	GillaGoreAssets a = Assets;
	if (a == null)
	{
		return null;
	}
	if (a.lump == null)
	{
		return null;
	}
	Vector4 wound = wounds[w];
	Vector4 ax = axes[w];
	var found = BoneOf(section, (Vector3)wound);
	if (found.bone == null)
	{
		return null;
	}
	CraterIn c = new CraterIn();
	c.bone = found.bone;
	c.w = w;
	c.section = section;
	c.middle = new Vector3(wound.x, wound.y, wound.z);
	c.axis = new Vector3(ax.x, ax.y, ax.z);
	c.radius = wound.w;
	c.length = ax.w;
	float under = wound.w * ax.w * 0.12f;
	c.bind = found.bind;
	c.openingAt = c.middle - c.axis * under;
	Vector4 sd = sides[w];
	c.side = new Vector3(sd.x, sd.y, sd.z);
	c.side2 = Vector3.Cross(c.axis, c.side);
	c.shape = sides[w].w;
	c.pocket = section == GoreSection.Head ? wound.w * 0.7f : 0f;
	if (fitted != null && stumps[w])
	{
		c.cut = cuts[(int)section];
		string key = section == GoreSection.ArmL ? "upperarm_L" : "upperarm_R";
		(Transform, Matrix4x4) arm;
		if (bones.TryGetValue(key, out arm))
		{
			c.bone = arm.Item1;
			c.bind = arm.Item2;
		}
	}
	c.skin = new Skin[whole.Count];
	for (int i = 0; i < whole.Count; i++)
	{
		var entry = whole[i];
		Skin s = new Skin();
		s.vs = Vertices(entry.mesh);
		s.tris = SkinTriangles(entry.mesh, entry.mats);
		s.box = Box(entry.mesh);
		c.skin[i] = s;
	}
	if (a.chunk != null && a.chunk.isReadable)
	{
		if (lumpFrom != a.chunk)
		{
			lumpAt = a.chunk.vertices;
			lumpTris = a.chunk.triangles;
			lumpFrom = a.chunk;
		}
		c.lump = lumpAt;
		c.lumpTris = lumpTris;
	}
	if (fitted != null && stumps[w])
	{
		return c;
	}
	c.other = BendsWith(section, c.openingAt, wound.w * 3f, out c.bone, out c.bind, out c.otherBind, out c.places);
	return c;
}

private static CraterOut Crater(CraterIn c)
{
	Vector3 middle = c.middle;
	Vector3 axis = c.axis;
	Vector3 side = c.side;
	Vector3 side2 = c.side2;
	Vector3 openingAt = c.openingAt;
	float radius = c.radius;
	float length = c.length;
	float shape = c.shape;

	System.Random rng = new System.Random(
		Mathf.RoundToInt(middle.x * 5000f) * 73 +
		Mathf.RoundToInt(middle.y * 5000f) * 179 +
		Mathf.RoundToInt(middle.z * 5000f) * 283);
	Func<Vector3, float> inSkin = UnderSkinAlong(c.skin, openingAt, axis, side, side2, radius, c.pocket);
	Vector4 cut = c.cut;
	Func<Vector3, float> depthOf;
	if ((cut - Vector4.zero).sqrMagnitude >= 9.9999994e-11f)
	{
		depthOf = p => Crater_b__1(p, cut, inSkin) /* ? */;
	}
	else
	{
		depthOf = inSkin;
	}

	float turnPhoto = R() * Mathf.PI + R() * Mathf.PI /* ? */;
	Vector2 shiftPhoto = new Vector2(R(), R());

	List<Vector3> verts = new List<Vector3>();
	List<Vector2> skinDepth = new List<Vector2>();
	List<Vector2> uv = new List<Vector2>();
	List<int> faces = new List<int>();

	var spikes = new (Vector3, float, float)[44];
	for (int i = 0; i < spikes.Length; i++)
	{
		float t = Mathf.Clamp01(R());
		float ang = R();
		Vector3 dir = Way(t * 1.5f - 0.5f, ang * Mathf.PI + ang * Mathf.PI).normalized;
		float s1 = Mathf.Clamp01(R());
		float s2 = Mathf.Clamp01(R());
		spikes[i] = (dir, s1 * 0.12f + 0.04f, s2 * 0.08f + 0.02f);
	}

	for (int ring = 0; ring <= Rings; ring++)
	{
		float h = Mathf.Cos(ring * Mathf.PI / Rings);
		float s = Mathf.Clamp01((h - 0.12f) / 0.4f);
		float fall = s * s * 3f - s * s * (s + s);
		fall = fall + (1f - fall) * 0f;
		for (int j = 0; j <= Round; j++)
		{
			Vector3 way = Way(h, j * 6.2831855f / Round);
			Vector3 n = way.normalized;
			float r = 1.02f;
			if (fall > 0f)
			{
				for (int k = 0; k < spikes.Length; k++)
				{
					float d = (Vector3.Dot(n, spikes[k].Item1) - 1f) / spikes[k].Item2;
					if (d > -7f)
					{
						r += Mathf.Exp(d) * fall * spikes[k].Item3;
					}
				}
			}
			r *= radius;
			float along = Vector3.Dot(way, axis) * (length - 1f);
			Vector3 p = middle + (way + axis * along) * r;
			verts.Add(p);
			skinDepth.Add(new Vector2(Down(p), depthOf(p)));
			uv.Add(shiftPhoto);
		}
	}

	int row = 0;
	for (int ring = 0; ring < Rings; ring++)
	{
		for (int j = 0; j < Round; j++)
		{
			int i0 = row + j;
			int i2 = i0 + 57;
			if (skinDepth[i0].y >= -0.1f || skinDepth[i0 + 1].y >= -0.1f || skinDepth[i2].y >= -0.1f || skinDepth[row + j + 58].y >= -0.1f)
			{
				faces.Add(i0);
				faces.Add(row + j + 1);
				faces.Add(i2);
				faces.Add(row + j + 1);
				faces.Add(row + j + 58);
				faces.Add(i2);
			}
		}
		row += 57;
	}

	Vector3[] lump = c.lump;
	if (lump != null)
	{
		int[] tris = c.lumpTris;
		for (int i = 0; i < 9; i++)
		{
			float ang = (i * 2.4f + R()) % 6.2831855f;
			float t = Mathf.Clamp01((R() + i) / 9f);
			Vector3 way = Way(t * 0.75f + 0.05f, ang);
			float along = Vector3.Dot(way, axis) * (length - 1f);
			float far = radius * 1.02f;
			Vector3 at = middle + (way + axis * along) * far;
			if (inSkin(at) /* ? */ >= 0.04f)
			{
				float size = radius * (Mathf.Clamp01(R()) * 0.33f + 0.22f);
				Quaternion rot = Quaternion.Euler(R() * 360f, R() * 360f, R() * 360f);
				int baseIndex = verts.Count;
				for (int k = 0; k < lump.Length; k++)
				{
					Vector3 p = at + rot * (lump[k] * size);
					verts.Add(p);
					skinDepth.Add(new Vector2(Down(p), depthOf(p)));
					uv.Add(Photo(p));
				}
				for (int k = 0; k < tris.Length; k++)
				{
					faces.Add(tris[k] + baseIndex);
				}
			}
		}
	}

	List<Vector4> opening = new List<Vector4>(verts.Count);
	List<Vector3> wayOut = new List<Vector3>(verts.Count);
	for (int i = 0; i < verts.Count; i++)
	{
		opening.Add(new Vector4(openingAt.x, openingAt.y, openingAt.z, radius));
		wayOut.Add(-axis);
	}

	CraterOut o = new CraterOut();
	o.verts = verts;
	o.wayOut = wayOut;
	o.uv = uv;
	o.skinDepth = skinDepth;
	o.opening = opening;
	o.faces = faces;
	o.bends = c.places == null ? null : Bends(c.places, verts);
	return o;

	float R()
	{
		return (float)rng.NextDouble() /* ? */;
	}

	float Wobble(float round)
	{
		return WobbleOf(round, shape) /* ? */;
	}

	Vector3 Way(float down, float round)
	{
		float across = Mathf.Sqrt(Mathf.Max(0f, 1f - down * down)) * Wobble(round);
		return axis * down + (side * Mathf.Cos(round) + side2 * Mathf.Sin(round)) * across /* ? */;
	}

	Vector2 Photo(Vector3 p)
	{
		Vector3 d = (p - middle) / Mathf.Max(radius, 1e-5f);
		float x = Vector3.Dot(d, side);
		float y = Vector3.Dot(d, side2);
		float cs = Mathf.Cos(turnPhoto);
		float sn = Mathf.Sin(turnPhoto);
		return new Vector2(x * cs - y * sn, x * sn + y * cs) + shiftPhoto /* ? */;
	}

	float Down(Vector3 p)
	{
		return Vector3.Dot(p - middle, axis) / Mathf.Max(radius * length, 1e-5f) /* ? */;
	}
}

private bool PutCrater(CraterIn c, CraterOut o)
{
	GillaGoreAssets a = Assets;
	if (a == null)
	{
		return false;
	}
	if (c.bone == null)
	{
		return false;
	}
	int w = c.w;
	Mesh mesh = new Mesh();
	mesh.name = "Gore_Wall";
	mesh.SetVertices(o.verts);
	mesh.SetUVs(0, o.uv);
	mesh.SetUVs(1, o.opening);
	mesh.SetUVs(2, o.skinDepth);
	mesh.SetUVs(3, o.wayOut);
	List<Vector2> bends = o.bends;
	if (bends == null)
	{
		bends = new List<Vector2>(new Vector2[o.verts.Count]);
	}
	mesh.SetUVs(4, bends);
	mesh.SetTriangles(o.faces, 0);
	mesh.RecalculateNormals();
	Vector3[] normals = mesh.normals;
	for (int ring = 0; ring <= Rings; ring++)
	{
		int i0 = ring * 57;
		Vector3 n = (normals[i0] + normals[i0 + 56]).normalized;
		normals[i0 + 56] = n;
		normals[i0] = n;
	}
	int[] poles = new int[2];
	poles[1] = 26;
	for (int p = 0; p < poles.Length; p++)
	{
		int start = poles[p] * 57;
		Vector3 sum = Vector3.zero;
		for (int k = 0; k < 57; k++)
		{
			sum += normals[start + k];
		}
		Vector3 n = sum.normalized;
		for (int k = 0; k < 57; k++)
		{
			normals[start + k] = n;
		}
	}
	mesh.normals = normals;
	mesh.RecalculateBounds();
	wallMeshes.Add(mesh);
	if (walls[w] != null)
	{
		Destroy(walls[w]);
	}
	if (wallMat == null)
	{
		Material m = new Material(a.lump);
		m.name = a.lump.name + "_Here";
		wallMat = m;
	}
	GameObject go = new GameObject("GoreBone_Wall");
	Component layerFrom = skins.Length != 0 ? (Component)skins[0] : this;
	go.layer = layerFrom.gameObject.layer;
	go.transform.SetParent(c.bone, false);
	go.transform.SetLocalPositionAndRotation(new Vector3(c.bind.m03, c.bind.m13, c.bind.m23), c.bind.rotation);
	go.transform.localScale = c.bind.lossyScale;
	MeshFilter filter = go.AddComponent<MeshFilter>();
	filter.sharedMesh = mesh;
	MeshRenderer renderer = go.AddComponent<MeshRenderer>();
	renderer.sharedMaterial = wallMat;
	renderer.shadowCastingMode = ShadowCastingMode.Off;
	if (c.other != null)
	{
		GillaWoundBend bend = go.AddComponent<GillaWoundBend>();
		bend.other = c.other;
		bend.bind = c.otherBind;
		bend.Bend();
	}
	pieces.Add((go, c.section));
	walls[w] = go;
	return true;
}
}
