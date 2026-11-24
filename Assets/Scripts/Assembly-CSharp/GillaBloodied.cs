// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;

public class GillaBloodied : MonoBehaviour
{
    private static readonly int MapId = Shader.PropertyToID("_StainMap");
    private const int Fur = 0;
    private static MaterialPropertyBlock block;
    private Dismemberment body;
    private DismemberGroup group;
    private SkinnedMeshRenderer[] skins;
    private Transform[][] bonesOf;
    private GillaStains.Sheet sheet;
    private float nextShow;

    public GillaStains.Sheet Sheet { get { return sheet; } }

    public static GillaBloodied On(Dismemberment copy)
    {
        GillaBloodied b = copy.GetComponent<GillaBloodied>();
        if (b == null)
        {
            b = copy.gameObject.AddComponent<GillaBloodied>();
        }
        b.Find();
        return b;
    }

    public static GillaBloodied OnBody(GameObject body)
    {
        GillaBloodied b = body.GetComponent<GillaBloodied>();
        if (b == null)
        {
            b = body.AddComponent<GillaBloodied>();
        }
        b.Find();
        if (b.sheet == null)
        {
            if (block == null) block = new MaterialPropertyBlock();
            SkinnedMeshRenderer[] arr = b.skins;
            for (int i = 0; i < arr.Length; i++)
            {
                SkinnedMeshRenderer r = arr[i];
                if (r == null) continue;
                r.GetPropertyBlock(block, 0);
                if (block.HasTexture(MapId))
                {
                    b.sheet = GillaStains.OfMap(block.GetTexture(MapId));
                }
                if (b.sheet != null) break;
            }
            if (b.sheet == null)
            {
                b.sheet = GillaStains.NewSheet();
            }
            b.sheet.users++;
            b.Show(b.sheet);
        }
        return b;
    }

    public void Scratch(Vector3 at, Vector3 to, float width)
    {
        GillaStains.Job j = new GillaStains.Job();
        j.point = at;
        j.to = to;
        j.radius = width;
        j.line = true;
        j.stuff = GillaSplatter.Stuff.Blood;
        Find();
        GillaBloodied painter = Painter();
        j.pieces = painter.Pieces();
        if (j.pieces.Count == 0) return;
        Share();
        j.sheet = sheet;
        j.big = Mathf.Max(0.05f, transform.lossyScale.y);
        j.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GillaStains.Add(j);
    }

    private void Find()
    {
        if (skins != null) return;
        body = GetComponent<Dismemberment>();
        if (body != null)
        {
            body.PartsReset += Clear;
            body.PartTorn += OnTorn;
        }
        group = GetComponentInParent<DismemberGroup>();
        List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
        SkinnedMeshRenderer[] all = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < all.Length; i++)
        {
            SkinnedMeshRenderer r = all[i];
            if (r.sharedMesh != null && r.name.StartsWith("Gilla_"))
            {
                list.Add(r);
            }
        }
        skins = list.ToArray();
        bonesOf = new Transform[skins.Length][];
        for (int i = 0; i < skins.Length; i++)
        {
            bonesOf[i] = skins[i].bones;
        }
    }

    private void OnDestroy()
    {
        if (body != null)
        {
            body.PartsReset -= Clear;
            body.PartTorn -= OnTorn;
        }
        if (sheet != null)
        {
            GillaStains.Sheet s = sheet;
            sheet = null;
            s.users--;
        }
    }

    public void Clear()
    {
        LetGo(true);
    }

    private void LetGo(bool drawn)
    {
        if (sheet == null) return;
        GillaStains.Sheet s = sheet;
        sheet = null;
        s.users--;
        if (drawn && skins != null)
        {
            if (block == null) block = new MaterialPropertyBlock();
            SkinnedMeshRenderer[] arr = skins;
            for (int i = 0; i < arr.Length; i++)
            {
                SkinnedMeshRenderer r = arr[i];
                if (r == null) continue;
                r.GetPropertyBlock(block, 0);
                bool hasFur = block.HasColor(CosmeticSkin.FurColor);
                Color fur = new Color(0f, 0f, 0f, 0f);
                if (hasFur) fur = block.GetColor(CosmeticSkin.FurColor);
                block.Clear();
                MaterialPropertyBlock pb = null;
                if (hasFur)
                {
                    block.SetColor(CosmeticSkin.FurColor, fur);
                    pb = block;
                }
                r.SetPropertyBlock(pb, 0);
            }
        }
    }

    private void OnTorn(ApePart part, GameObject piece)
    {
        if (piece == null || sheet == null) return;
        if (body == null) return;
        Dismemberment.Part[] parts = body.parts;
        if (parts == null) return;
        Dismemberment.Part p = Array.Find(parts, x => x.id == part);
        if (p != null && p.renderer != null /* ? */)
        {
            Renderer to;
            if (piece.TryGetComponent<Renderer>(out to))
            {
                CarryOver(p.renderer /* ? */, to);
            }
        }
    }

    public static void CarryOver(Renderer from, Renderer to)
    {
        if (from == null || to == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        to.GetPropertyBlock(block, 0);
        if (block.HasTexture(MapId)) return;
        from.GetPropertyBlock(block, 0);
        if (!block.HasTexture(MapId)) return;
        GillaStains.Sheet s = GillaStains.OfMap(block.GetTexture(MapId));
        if (s == null) return;
        to.SetPropertyBlock(block, 0);
        s.users++;
        GillaStainKept kept = to.gameObject.AddComponent<GillaStainKept>();
        kept.sheet = s /* ? */;
    }

    private void Share()
    {
        if (sheet != null) return;
        Dismemberment[] copies;
        if (group != null && group.copies != null && Array.IndexOf(group.copies, body) >= 0)
        {
            copies = group.copies;
        }
        else
        {
            copies = new Dismemberment[] { body };
        }
        GillaStains.Sheet s = null;
        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment c = copies[i];
            if (c == null) continue;
            GillaBloodied b = On(c);
            if (b.sheet != null) s = b.sheet;
        }
        if (s == null) s = GillaStains.NewSheet();
        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment c = copies[i];
            if (c == null) continue;
            GillaBloodied b = On(c);
            if (b.sheet == null)
            {
                b.sheet = s;
                s.users++;
                b.Show(s);
            }
        }
    }

    private void Show(GillaStains.Sheet s)
    {
        if (block == null) block = new MaterialPropertyBlock();
        SkinnedMeshRenderer[] arr = skins;
        for (int i = 0; i < arr.Length; i++)
        {
            SkinnedMeshRenderer r = arr[i];
            if (r == null) continue;
            r.GetPropertyBlock(block, 0);
            block.SetTexture(MapId, s.map);
            r.SetPropertyBlock(block, 0);
        }
    }

    private void Update()
    {
        if (sheet != null && nextShow <= Time.time)
        {
            nextShow = Time.time + 1f;
            Show(sheet);
        }
    }

    private GillaBloodied Painter()
    {
        if (group != null && group.copies != null && Array.IndexOf(group.copies, body) >= 0)
        {
            Dismemberment[] copies = group.copies;
            for (int i = 0; i < copies.Length; i++)
            {
                Dismemberment c = copies[i];
                if (c != null && c.spawnPieces && c.isActiveAndEnabled)
                {
                    return On(c);
                }
            }
        }
        return this;
    }

    private List<GillaStains.Piece> Pieces()
    {
        List<GillaStains.Piece> list = new List<GillaStains.Piece>(skins.Length);
        Matrix4x4[] mats = null;
        Transform[] last = null;
        for (int i = 0; i < skins.Length; i++)
        {
            SkinnedMeshRenderer r = skins[i];
            if (r == null || !r.enabled || r.sharedMesh == null) continue;
            Transform[] bones = bonesOf[i];
            if (bones == null || bones.Length == 0) continue;
            if (mats == null || !SameBones(bones, last))
            {
                mats = new Matrix4x4[bones.Length];
                last = bones;
                for (int k = 0; k < bones.Length; k++)
                {
                    mats[k] = bones[k] != null ? bones[k].localToWorldMatrix : Matrix4x4.identity;
                }
            }
            GillaStains.Piece piece = new GillaStains.Piece();
            piece.mesh = r.sharedMesh;
            piece.name = r.name;
            piece.bones = mats;
            list.Add(piece);
        }
        return list;
    }

    private static bool SameBones(Transform[] x, Transform[] y)
    {
        if (x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i]) return false;
        }
        return true;
    }

    public void Splash(Vector3 from, float reach, float strength)
    {
        GillaStains.Job j = new GillaStains.Job();
        j.point = from;
        j.reach = reach;
        j.strength = strength;
        j.stuff = GillaSplatter.Stuff.Blood;
        Send(j);
    }

    public void Hit(Vector3 at, float radius, GillaSplatter.Stuff stuff)
    {
        GillaStains.Job j = new GillaStains.Job();
        j.point = at;
        j.radius = radius;
        j.stuff = stuff;
        Send(j);
    }

    private void Send(GillaStains.Job j)
    {
        Find();
        GillaBloodied painter = Painter();
        j.pieces = painter.Pieces();
        if (j.pieces.Count == 0) return;
        Share();
        j.sheet = sheet;
        j.big = Mathf.Max(0.05f, transform.lossyScale.y);
        j.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GillaStains.Add(j);
    }
}
