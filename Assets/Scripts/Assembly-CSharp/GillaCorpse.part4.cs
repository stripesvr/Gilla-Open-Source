using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaCorpse : MonoBehaviour
{
    public static void Take(PlayerRef by, bool right, GillaCorpse half, Quaternion rot)
    {
        Vector3 center = half.box != null ? half.box.center : Vector3.zero;
        Vector3 offset = rot * center;
        HoldIt(by, right, half.Id, 0, -offset, rot);
        Transform hand = Hand(by.PlayerId, right);
        if (hand != null)
        {
            Vector3 halfPos = half.transform.position;
            half.holdSeam = halfPos - (hand.position + hand.rotation * -offset);
        }
    }

    private GillaCorpse MakeHalf(int sign, int seed, Vector3 velocity)
    {
        using (HalfMark.Auto())
        {
            float into = Mathf.Min(Vector3.Dot(velocity, outOfDoor), 0f);
            GillaCorpse half = New(Id + (sign < 1 ? 200000 : 100000));
            half.Owner = Id;
            half.goneAt = goneAt;
            half.blood = blood;
            half.MadeAt = Time.time;
            half.tearSounds = tearSounds;
            half.dropSounds = dropSounds;
            GameObject go = half.gameObject;
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            half.parts[0] = go.transform;
            half.CopyBones(this, sign, seed);
            half.TakeWorn(this, sign);
            GillaGoreAssets gore = Resources.Load<GillaGoreAssets>("GillaGore");
            SkinnedMeshRenderer[] skins = GetComponentsInChildren<SkinnedMeshRenderer>();
            GillaRip.Half headCut = null;
            GillaRip.Half bodyCut = null;
            Bounds local = default;
            bool any = false;
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer r = skins[i];
                if (!r.enabled) continue;
                if (r.sharedMesh == null) continue;
                bool isHead = r.name.EndsWith("Head");
                bool armL = r.name.EndsWith("ArmL");
                bool armR = r.name.EndsWith("ArmR");
                if (armL || armR)
                {
                    int part = armL ? 2 : 3;
                    float side = r.sharedMesh.bounds.center.x >= 0f ? 1f : -1f;
                    if (side == (float)sign)
                    {
                        half.parts[part] = half.CopySkin(r).transform;
                        half.ArmStump(this, part);
                    }
                }
                else
                {
                    GillaRip.Half cut = GillaRip.Cut(r.sharedMesh, sign, seed);
                    SkinnedMeshRenderer copy = half.CopySkin(r);
                    copy.sharedMesh = cut.skin;
                    copy.sharedMaterials = half.Torn(r.sharedMaterials);
                    copy.localBounds = cut.skin.bounds;
                    if (isHead)
                    {
                        half.parts[1] = copy.transform;
                        headCut = cut;
                    }
                    else
                    {
                        Mesh baked = new Mesh();
                        copy.BakeMesh(baked, false);
                        baked.RecalculateBounds();
                        foreach (Vector3 corner in Corners(baked.bounds))
                        {
                            Vector3 p = go.transform.InverseTransformPoint(copy.transform.position + copy.transform.rotation * corner);
                            if (any)
                            {
                                local.Encapsulate(p);
                            }
                            else
                            {
                                local = new Bounds(p, Vector3.zero);
                                any = true;
                            }
                        }
                        Destroy(baked);
                        bodyCut = cut;
                    }
                    if (cut.inside != null && gore != null)
                    {
                        half.AddInside(cut, copy, isHead ? "head" : "spine", gore);
                    }
                }
            }

            if (gore != null && gore.pieces != null)
            {
                for (int i = 0; i < gore.pieces.Length; i++)
                {
                    GillaGoreAssets.Piece piece = gore.pieces[i];
                    if (piece.section == GoreSection.ArmL || piece.section == GoreSection.ArmR) continue;
                    if (piece.mesh == null) continue;
                    if (piece.material != gore.bone) continue;
                    GillaRip.Half cut = piece.section == GoreSection.Head ? headCut : bodyCut;
                    if (cut == null) continue;
                    Transform on;
                    if (!half.bones.TryGetValue(piece.bone, out on)) continue;
                    Matrix4x4 bind;
                    if (!half.binds.TryGetValue(piece.bone, out bind)) continue;
                    int count = holes == null ? 0 : holeCount;
                    Task<object> made = GillaRip.Bones(piece.mesh, piece.gone, 0.0045f, sign, seed, holes, holeWays, holeSides, count, cut.middle, cut.across);
                    half.coming.Add((made, on, bind, piece.mesh.name, piece.material));
                }
            }

            half.box = go.AddComponent<BoxCollider>();
            half.box.center = local.center;
            half.box.size = Vector3.Max(local.size * 0.85f, Vector3.one * 0.1f);
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.mass = 7f;
            rb.solverIterations = 20;
            rb.solverVelocityIterations = 8;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxDepenetrationVelocity = 3f;
            Vector3 vel = velocity - outOfDoor * into;
            rb.linearVelocity = vel;
            Vector3 forward = go.transform.forward;
            Vector3 rightV = go.transform.right;
            rb.angularVelocity = forward * (float)sign * 2.5f + rightV * UnityEngine.Random.Range(-1f, 1f);
            IgnoreYou(half.box);
            half.Ragdoll(rb, vel);
            for (int i = 0; i < 3; i++)
            {
                if (off[i + 1])
                {
                    IgnorePart(half.box, parts[i + 1]);
                }
            }
            TornPiece torn = go.AddComponent<TornPiece>();
            torn.dropSounds = dropSounds; /* ? */
            return half;
        }
    }

    private void AddBone(object made, Transform on, Matrix4x4 bind, string name, Material material)
    {
        using (BoneMark.Auto())
        {
            Mesh mesh = GillaRip.BoneMesh(made);
            if (mesh != null)
            {
                GameObject go = new GameObject("GoreBone_" + name);
                go.layer = 2;
                go.transform.SetParent(on, false);
                go.transform.SetLocalPositionAndRotation(new Vector3(bind.m03, bind.m13, bind.m23), bind.rotation);
                go.transform.localScale = bind.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = Copies(new Material[] { material });
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.enabled = !keptBack;
                renderer.sharedMaterial.SetFloat("_Open", 1f);
                renderer.sharedMaterial.SetColor("_Marrow", new Color(0.3f, 0.09f, 0.08f, 1f));
            }
        }
    }

    private void CopyBones(GillaCorpse c, int sign, int seed)
    {
        using (CopyBonesMark.Auto())
        {
            foreach (KeyValuePair<string, Matrix4x4> pair in c.binds)
            {
                binds[pair.Key] = pair.Value;
            }
            Transform src = c.transform.Find("Skeleton");
            GameObject skel = new GameObject("Skeleton");
            skel.layer = 2;
            Transform skelT = skel.transform;
            skelT.SetParent(transform, false);
            skelT.localScale = src.localScale;
            List<(GillaWoundBend made, Transform other, Matrix4x4 bind)> bends = new List<(GillaWoundBend made, Transform other, Matrix4x4 bind)>();

            void Copy(Transform from, Transform to)
            {
                skeleton[from] = to; /* ? */
                if (!bones.ContainsKey(to.name)) bones[to.name] = to; /* ? */
                GillaWoundBend srcBend;
                if (from.TryGetComponent<GillaWoundBend>(out srcBend)) /* ? */
                {
                    GillaWoundBend made = to.gameObject.AddComponent<GillaWoundBend>(); /* ? */
                    bends.Add((made, srcBend.other, srcBend.bind)); /* ? */
                }
                for (int i = 0; i < from.childCount; i++)
                {
                    Transform child = from.GetChild(i);
                    GameObject g = new GameObject(child.name);
                    g.layer = 2;
                    Transform t = g.transform;
                    t.SetParent(to, false);
                    t.SetLocalPositionAndRotation(child.localPosition, child.localRotation);
                    t.localScale = child.localScale;
                    Copy(child, t);
                }
            }

            Copy(src, skelT); /* ? */
            foreach (var bend in bends)
            {
                Transform mapped = null;
                if (bend.other != null)
                {
                    Transform t;
                    mapped = skeleton.TryGetValue(bend.other, out t) ? t : null;
                }
                bend.made.other = mapped; /* ? */
                bend.made.bind = bend.bind; /* ? */
                bend.made.Bend();
            }
        }
    }

    private Material[] Torn(Material[] mats)
    {
        Material[] result = Copies(mats);
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == null) continue;
            if (result[i].shader.name != "Gilla/ApeWounded")
            {
                result[i] = WoundedOwn(result[i]);
            }
            result[i].SetFloat("_RipOn", 1f);
        }
        return result;
    }

    private Material WoundedOwn(Material from)
    {
        Material m;
        if (!copies.TryGetValue(from, out m))
        {
            m = new Material(from); /* ? */
            Shader wounded = Shader.Find("Gilla/ApeWounded"); /* ? */
            if (wounded != null) m.shader = wounded; /* ? */
            copies[from] = m;
        }
        return m;
    }

    private void AddInside(GillaRip.Half cut, SkinnedMeshRenderer skin, string on, GillaGoreAssets gore)
    {
        using (InsideMark.Auto())
        {
            Transform bone;
            Matrix4x4 bind;
            if (bones.TryGetValue(on, out bone) && binds.TryGetValue(on, out bind))
            {
                GameObject posing = new GameObject("Posing");
                posing.transform.SetParent(skin.transform, false);
                SkinnedMeshRenderer temp = posing.AddComponent<SkinnedMeshRenderer>();
                temp.sharedMesh = cut.inside;
                temp.bones = skin.bones;
                temp.rootBone = skin.rootBone;
                Mesh baked = new Mesh();
                temp.BakeMesh(baked, false);
                Matrix4x4 m = bind.inverse * bone.worldToLocalMatrix;
                m = m * Matrix4x4.TRS(posing.transform.position, posing.transform.rotation, Vector3.one);
                Vector3[] verts = baked.vertices;
                Vector3[] normals = baked.normals;
                for (int i = 0; i < verts.Length; i++)
                {
                    verts[i] = m.MultiplyPoint3x4(verts[i]);
                    normals[i] = m.MultiplyVector(normals[i]).normalized;
                }
                DestroyImmediate(baked);
                DestroyImmediate(posing);
                Mesh mesh = Instantiate<Mesh>(cut.inside);
                mesh.name = "Corpse_Inside";
                mesh.bindposes = new Matrix4x4[0];
                mesh.boneWeights = new BoneWeight[0];
                mesh.vertices = verts;
                mesh.normals = normals;
                mesh.RecalculateBounds();
                GameObject go = new GameObject("GoreBone_Inside");
                go.layer = 2;
                go.transform.SetParent(bone, false);
                go.transform.SetLocalPositionAndRotation(new Vector3(bind.m03, bind.m13, bind.m23), bind.rotation);
                go.transform.localScale = bind.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = Copies(new Material[] { gore.lump });
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                Material mat = renderer.sharedMaterial;
                mat.SetFloat("_Open", 1f);
                mat.SetFloat("_Film", 0.26f);
                mat.SetFloat("_Runs", 0.3f);
                mat.SetFloat("_Least", 0.1f);
                mat.SetFloat("_Pool", 0.45f);
            }
        }
    }

    private void ArmStump(GillaCorpse c, int part)
    {
        if (c.wounds[part] != null)
        {
            GameObject w = new GameObject(c.wounds[part].name);
            Transform t = w.transform;
            t.SetParent(transform, false);
            t.SetPositionAndRotation(c.wounds[part].position, c.wounds[part].rotation);
            wounds[part] = t;
        }
        if (c.stumps[part] == null) return;
        GameObject stump = Instantiate<GameObject>(c.stumps[part], c.stumps[part].transform.position, c.stumps[part].transform.rotation, transform);
        stump.transform.localScale = c.stumps[part].transform.lossyScale;
        stump.SetActive(false);
        stumps[part] = stump;
    }

    private void Hollow(GillaCorpse plus, GillaCorpse minus)
    {
        Ripped = true;
        List<(int, bool)> drop = new List<(int, bool)>();
        foreach (KeyValuePair<(int, bool), Hold> pair in holds)
        {
            if (pair.Value.corpse == this && !IsOff(pair.Value.part))
            {
                drop.Add(pair.Key);
            }
        }
        foreach ((int, bool) key in drop)
        {
            holds.Remove(key);
        }
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter f = filters[i];
            if (f.sharedMesh != null && f.sharedMesh.name.StartsWith("Corpse_"))
            {
                Destroy(f.sharedMesh);
            }
        }
        int index = transform.childCount;
        while (index - 1 >= 0)
        {
            index--;
            GameObject child = transform.GetChild(index).gameObject;
            child.SetActive(false);
            Destroy(child);
            if (index <= 0) break;
        }
        TornPiece torn;
        if (TryGetComponent<TornPiece>(out torn))
        {
            DestroyImmediate(torn);
        }
        Rigidbody rb;
        if (TryGetComponent<Rigidbody>(out rb))
        {
            DestroyImmediate(rb);
        }
        if (box != null)
        {
            DestroyImmediate(box);
        }
        box = null;
        for (int i = 0; i < 4; i++)
        {
            if (i == 0 || !off[i])
            {
                parts[i] = null;
                limbs[i] = null;
            }
        }
        Array.Clear(wounds, 0, wounds.Length);
        Array.Clear(stumps, 0, stumps.Length);
    }

    public static void Torn(Dismemberment from, int id, int part, GameObject piece)
    {
        if (piece == null) return;
        GillaCorpse c = Find(id);
        if (c == null)
        {
            c = New(id);
        }
        int i = part + 1;
        c.parts[i] = piece.transform;
        c.off[i] = true;
        c.told[i] = false;
        c.dropSounds = from != null ? from.dropSounds : c.dropSounds;
        GillaModeRound round = GillaModeRound.Current;
        if (round == null) return;
        if (round.Live && (int)round.Phase == 1)
        {
            if (from != null)
            {
                from.Take(piece);
            }
            c.Keep(i, piece);
        }
    }
}
