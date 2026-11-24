// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaGlass : MonoBehaviour
{
    public GillaGlass()
    {
        shardCount = 10;
        slowTo = 0.75f;
        runSpeed = 2.5f;
        regrow = 10f;
        shardReach = 0.3f;
        shardOpacity = 0.35f;
        fade = -1f;
        LastBreak = "";
    }

    static GillaGlass()
    {
        FadeId = Shader.PropertyToID("_Fade");
        panes = new Dictionary<int, GillaGlass>();
        shardMats = new Dictionary<ValueTuple<Material, float>, Material>();
        notShards = -1;
    }

    private void Piece(Shard s, Vector2[] poly, Vector3 c, Vector3 u, Vector3 w, float depth, Vector2 hit, Vector3 push, Vector3 side)
    {
        int n = poly.Length;
        Vector2 sum = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            sum += poly[i];
        }
        List<Vector3> verts = new List<Vector3>(n * 6);
        List<Vector3> normals = new List<Vector3>(n * 6);
        List<int> tris = new List<int>(n * 12);
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;
        float cx = sum.x / n;
        float cy = sum.y / n;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = new Vector2(poly[i].x - cx, poly[i].y - cy);
            verts.Add(new Vector3(p.x, p.y, depth));
            normals.Add(Vector3.forward);
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        for (int i = 0; i < n; i++)
        {
            verts.Add(new Vector3(poly[i].x - cx, poly[i].y - cy, -depth));
            normals.Add(Vector3.back);
        }
        for (int i = 2; i < n; i++)
        {
            tris.Add(0);
            tris.Add(i - 1);
            tris.Add(i);
            tris.Add(n);
            tris.Add(n + i);
            tris.Add(n + i - 1);
        }
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1 == n) ? 0 : i + 1;
            Vector2 a = new Vector2(poly[i].x - cx, poly[i].y - cy);
            Vector2 b = new Vector2(poly[j].x - cx, poly[j].y - cy);
            Vector3 normal = new Vector3(b.y - a.y, -(b.x - a.x), 0f).normalized;
            int baseIndex = verts.Count;
            verts.Add(new Vector3(a.x, a.y, depth));
            verts.Add(new Vector3(a.x, a.y, -depth));
            verts.Add(new Vector3(b.x, b.y, -depth));
            verts.Add(new Vector3(b.x, b.y, depth));
            for (int k = 0; k < 4; k++)
            {
                normals.Add(normal);
            }
            tris.Add(baseIndex);
            tris.Add(baseIndex + 1);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex);
            tris.Add(baseIndex + 2);
            tris.Add(baseIndex + 3);
        }
        s.mesh.Clear();
        s.mesh.SetVertices(verts);
        s.mesh.SetNormals(normals);
        s.mesh.SetTriangles(tris, 0);
        s.mesh.RecalculateBounds();
        Quaternion rotation = transform.rotation;
        Vector3 position = c + u * cx + w * cy;
        s.t.SetPositionAndRotation(position, rotation);
        s.t.localScale = Vector3.one;
        s.r.SetPropertyBlock(null);
        s.box.center = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
        Vector2 size = Vector2.Max((max - min) * 0.8f, new Vector2(0.01f, 0.01f));
        s.box.size = new Vector3(size.x, size.y, depth + depth + 0.006f);
        s.t.gameObject.SetActive(true);
        s.body.position = position;
        s.body.rotation = rotation;
        float dx = cx - hit.x;
        float dy = cy - hit.y;
        Vector3 dir = (u * dx + w * dy).normalized;
        Rigidbody rb = s.body;
        float a1 = UnityEngine.Random.Range(0.35f, 0.75f);
        float a2 = UnityEngine.Random.Range(0.2f, 1f);
        float a3 = UnityEngine.Random.Range(0.3f, 1.2f);
        Vector3 jitter = UnityEngine.Random.insideUnitSphere;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float near = 1f / (dist + dist + 1f);
        a1 = (near + 0.5f) * a1;
        a3 = (near + 0.4f) * a3;
        rb.linearVelocity = push * a1 + dir * a2 + side * a3 + jitter * 0.25f;
        s.body.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
    }

    private void EnsureShards(int count)
    {
        if (shards == null || shards.Length < count)
        {
            DropShards();
            Material mat = ShardMaterial(glass.sharedMaterial, shardOpacity);
            shards = new Shard[count];
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject(name + " shard " + i.ToString());
                go.layer = 2;
                go.SetActive(false);
                Mesh mesh = new Mesh();
                mesh.name = "Glass shard";
                MeshFilter filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.excludeLayers = notShards;
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.excludeLayers = notShards;
                Shard shard = new Shard();
                shard.t = go.transform;
                shard.mesh = mesh;
                shard.r = renderer;
                shard.body = rb;
                shard.box = box;
                shards[i] = shard;
            }
        }
    }

    private static Material ShardMaterial(Material pane, float opacity)
    {
        if (pane == null)
        {
            return null;
        }
        Material mat;
        if (shardMats.TryGetValue((pane, opacity), out mat) && mat != null)
        {
            return mat;
        }
        mat = new Material(pane);
        mat.name = pane.name + " (shards)";
        if (mat.HasProperty("_BaseColor"))
        {
            Color col = mat.GetColor("_BaseColor");
            col.a = Mathf.Clamp01(col.a * opacity);
            mat.SetColor("_BaseColor", col);
        }
        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", Mathf.Min(mat.GetFloat("_Smoothness"), 0.45f));
        }
        if (mat.HasProperty("_Cull"))
        {
            mat.SetFloat("_Cull", 0f);
        }
        shardMats[(pane, opacity)] = mat;
        return mat;
    }
}
