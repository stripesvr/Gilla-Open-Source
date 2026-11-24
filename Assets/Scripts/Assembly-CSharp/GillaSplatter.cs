// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;

public class GillaSplatter : MonoBehaviour
{
    public enum Stuff
    {
        Blood = 0,
        Yolk = 1
    }

    private const int MostSplats = 96;
    private const float Stays = 55;
    private const float Thins = 6;
    internal const int Solid = -16133;
    public const int Tiles = 4;
    private static GillaSplatter here;
    private static GillaGoreAssets gore;
    private Mesh mesh;
    private Vector3[] verts;
    private Vector3[] normals;
    private Vector2[] uvs;
    private Color32[] colours;
    private readonly float[] born = new float[96];
    private readonly byte[] lit = new byte[96];
    private int next;
    private int showing;
    private float nextFade;
    private readonly Vector3[] dirs = new Vector3[] { Vector3.up };
    private readonly Color[] heard = new Color[1];
    public static bool OnBodies = true;

    private static GillaGoreAssets Gore
    {
        get
        {
            if (gore != null)
            {
                return gore;
            }
            gore = Resources.Load<GillaGoreAssets>("GillaGore");
            return gore;
        }
    }

    public static void Around(Vector3 at, float strength, Dismemberment not)
    {
        if (Application.isPlaying)
        {
            if (!(Gore == null))
            {
                if (!(Gore.splat == null))
                {
                    Here();
                    here.Ground(at, strength);
                    if (OnBodies)
                    {
                        Bodies(at, strength, not);
                        return;
                    }
                }
            }
        }
    }

    public static void Stain(int playerId, Vector3 worldPoint, float radius, GillaSplatter.Stuff stuff)
    {
        NetworkApe local = NetworkApe.Local;
        int localId;
        if (local == null)
        {
            localId = Fusion.PlayerRef.None.PlayerId;
        }
        else
        {
            localId = local.GetComponent<Fusion.NetworkObject>().InputAuthority.PlayerId /* ? */;
        }
        if (localId == playerId)
        {
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            if (boot != null)
            {
                if (boot.tearing != null)
                {
                    Dismemberment[] copies = boot.tearing.copies;
                    if (copies != null && copies.Length != 0)
                    {
                        Stain(copies[0], worldPoint, radius, stuff);
                    }
                }
            }
        }
        else
        {
            foreach (NetworkApe ape in NetworkApe.All)
            {
                if (ape != null && ape != local && ape.GetComponent<Fusion.NetworkObject>().InputAuthority.PlayerId /* ? */ == playerId && ape.GetComponentInChildren<Dismemberment>() /* ? */ != null)
                {
                    Stain(ape.GetComponentInChildren<Dismemberment>() /* ? */, worldPoint, radius, stuff);
                    break;
                }
            }
        }
    }

    public static void Stain(Dismemberment copy, Vector3 worldPoint, float radius, GillaSplatter.Stuff stuff)
    {
        bool missing = copy == null;
        if (0f < radius && !missing)
        {
            if (Application.isPlaying)
            {
                if (!(Gore == null))
                {
                    if (Gore.splat == null)
                    {
                        return;
                    }
                    if (stuff == Stuff.Blood)
                    {
                        if (!OnBodies)
                        {
                            return;
                        }
                    }
                    Here();
                    GillaBloodied.On(copy).Hit(worldPoint, radius, stuff);
                }
            }
        }
    }

    public static void CarryOver(Renderer from, Renderer to)
    {
        if (from == null)
        {
            return;
        }
        if (to == null)
        {
            return;
        }
        MaterialPropertyBlock block = new MaterialPropertyBlock() /* ? */;
        int mapId = Shader.PropertyToID("_StainMap") /* ? */;
        to.GetPropertyBlock(block, 0);
        if (block.HasTexture(mapId))
        {
            return;
        }
        from.GetPropertyBlock(block, 0);
        if (!block.HasTexture(mapId))
        {
            return;
        }
        Texture map = block.GetTexture(mapId);
        GillaStains.Sheet sheet = GillaStains.OfMap(map);
        if (sheet == null)
        {
            return;
        }
        to.SetPropertyBlock(block, 0);
        sheet.users = sheet.users + 1;
        GillaStainKept kept = to.gameObject.AddComponent<GillaStainKept>();
        kept.sheet = sheet;
    }

    private static void Here()
    {
        if (here == null)
        {
            GameObject go = new GameObject("BloodSplats");
            here = go.AddComponent<GillaSplatter>();
        }
    }

    private void Awake()
    {

        verts = new Vector3[384];
        normals = new Vector3[verts.Length];
        uvs = new Vector2[verts.Length];
        colours = new Color32[verts.Length];
        int[] tris = new int[576];
        int v = 0;
        for (int i = 0; i < 576; i += 6)
        {
            tris[i] = v;
            tris[i + 1] = v + 1;
            tris[i + 2] = v + 2;
            tris[i + 3] = v;
            tris[i + 4] = v + 2;
            tris[i + 5] = v + 3;
            v += 4;
        }
        Mesh m = new Mesh();
        m.name = "BloodSplats";
        mesh = m;
        mesh.MarkDynamic();
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.colors32 = colours;
        mesh.triangles = tris;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Gore.splat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.allowOcclusionWhenDynamic = false;
    }

    private void OnDestroy()
    {
        if (mesh != null)
        {
            Object.Destroy(mesh);
        }
        if (here == this)
        {
            here = null;
        }
    }

    private void Update()
    {
        GillaStains.Pump();
        if (showing != 0 && nextFade <= Time.time)
        {
            showing = 0;
            nextFade = Time.time + 0.25f;
            bool changed = false;
            for (int i = 0; i < 96; i++)
            {
                if (0f < born[i])
                {
                    float alpha = 1f - Mathf.Clamp01((Time.time - born[i] - 55f) / 6f);
                    if (alpha <= 0f)
                    {
                        born[i] = 0f;
                        for (int k = 0; k < 4; k++)
                        {
                            verts[i * 4 + k] = Vector3.zero;
                        }
                    }
                    else
                    {
                        showing++;
                        if (1f <= alpha)
                        {
                            continue;
                        }
                        for (int k = 0; k < 4; k++)
                        {
                            colours[i * 4 + k].a = (byte)(int)(alpha * 255f);
                        }
                    }
                    changed = true;
                }
            }
            if (changed)
            {
                Push();
            }
        }
    }

    private void Push()
    {
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.colors32 = colours;
    }

    private void Ground(Vector3 at, float strength)
    {
        int count = Mathf.RoundToInt(strength * 10f);
        float s = Mathf.Sqrt(strength);
        Vector3 origin = at + Vector3.up * 0.05f;
        RaycastHit hit;
        for (int i = 0; i < count + 4; i++)
        {
            Vector3 d = Random.onUnitSphere;
            bool down = (i & 3) == 3;
            float lift = down ? 0f : 0.35f;
            d.y = Mathf.Abs(d.y) * (down ? -0.15f : -1.3f) - lift;
            Vector3 dir = d.normalized;
            if (Physics.Raycast(origin, dir, out hit, s * 2.6f, Solid, QueryTriggerInteraction.Ignore))
            {
                float w = 1f - hit.distance / (s * 2.6f);
                w = Mathf.Min(w * w, 1f);
                Splat(hit.point, hit.normal, s * (w * 0.5f + 0.12f) * Random.Range(0.75f, 1.25f), dir);
            }
        }
        RaycastHit floor;
        if (Physics.Raycast(origin, Vector3.down, out floor, 4f, Solid, QueryTriggerInteraction.Ignore))
        {
            Splat(floor.point, floor.normal, s * 0.75f, Vector3.down);
        }
        Push();
    }

    private void Splat(Vector3 at, Vector3 normal, float size, Vector3 came)
    {
        came = Vector3.ProjectOnPlane(came, normal);
        if (came.sqrMagnitude < 0.01f)
        {
            came = Vector3.ProjectOnPlane(Random.onUnitSphere, normal);
        }
        Quaternion rotation = Quaternion.AngleAxis(Random.Range(-25f, 25f), normal);
        Vector3 fwd = rotation * came.normalized;
        Vector3 right = Vector3.Cross(normal, fwd);
        float half;
        float neg;
        int attempt = 0;
        while (true)
        {
            half = size * 0.5f;
            neg = -(size * 0.5f);
            bool hitAll;
            uint k = 0;
            do
            {
                float u = (k == 3 || k == 0) ? neg : half;
                float v = k > 1 ? half : neg;
                Vector3 origin = at + normal * 0.03f + right * v + fwd * u;
                hitAll = Physics.Raycast(origin, -normal, 0.08f, Solid, QueryTriggerInteraction.Ignore);
            } while (k < 3 && (++k > 0) && hitAll);
            if (hitAll)
            {
                break;
            }
            if (attempt == 2)
            {
                return;
            }
            attempt++;
            size *= 0.55f;
        }
        int slot = next;
        next = (slot + 1) % 96;
        if (born[slot] <= 0f)
        {
            showing++;
        }
        born[slot] = Time.time;
        nextFade = Mathf.Min(nextFade, Time.time + 1f);
        float r = 1f;
        float g;
        float b;
        if (LightmapSettings.lightProbes != null)
        {
            SphericalHarmonicsL2 sh;
            LightProbes.GetInterpolatedProbe(at + normal * 0.1f, null, out sh);
            dirs[0] = normal;
            sh.Evaluate(dirs, heard);
            r = heard[0].r;
            g = heard[0].g;
            b = heard[0].b;
        }
        else
        {
            g = 1f;
            b = 1f;
        }
        Color32 tint = new Color(Mathf.Clamp01(r * 0.8f + 0.03f), Mathf.Clamp01(g * 0.8f + 0.03f), Mathf.Clamp01(b * 0.8f + 0.03f), 1f);
        int tile = Random.Range(0, 16);
        int baseIndex = slot * 4;
        for (int k = 0; k < 4; k++)
        {
            bool mid = k == 1 || k == 2;
            float u = mid ? half : neg;
            float v = k < 2 ? neg : half;
            int idx = baseIndex + k;
            verts[idx] = at + normal * 0.004f + right * v + fwd * u;
            normals[idx] = normal;
            uvs[idx] = new Vector2((tile % 4) / 4f + (mid ? 0.25f : 0f), (tile / 4) / 4f + (k < 2 ? 0f : 0.25f));
            colours[idx] = tint;
        }
    }

    private static void Bodies(Vector3 at, float strength, Dismemberment not)
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        float reach = Mathf.Sqrt(strength) * 2.4f;
        if (boot != null)
        {
            if (boot.tearing != null)
            {
                if (boot.tearing.copies != null)
                {
                    if (!MonsterPlayer.IsMonster)
                    {
                        if (System.Array.IndexOf<Dismemberment>(boot.tearing.copies, not) < 0)
                        {
                            Dismemberment[] copies = boot.tearing.copies;
                            for (int i = 0; i < copies.Length; i++)
                            {
                                Dismemberment copy = copies[i];
                                if (copy != null && copy.isActiveAndEnabled)
                                {
                                    Body(copy, at, reach, strength, not);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape != null && ape != NetworkApe.Local)
            {
                Dismemberment apeCopy = ape.GetComponentInChildren<Dismemberment>() /* ? */;
                if (apeCopy != null)
                {
                    Body(apeCopy, at, reach, strength, not);
                }
            }
        }
        foreach (GillaDummy dummy in GillaDummy.All)
        {
            if (dummy != null)
            {
                Dismemberment dummyCopy = dummy.GetComponentInChildren<Dismemberment>() /* ? */;
                if (dummyCopy != null)
                {
                    Body(dummyCopy, at, reach, strength, not);
                }
            }
        }
    }

    private static void Body(Dismemberment copy, Vector3 at, float reach, float strength, Dismemberment not)
    {
        if (copy == null)
        {
            return;
        }
        if (copy == not)
        {
            return;
        }
        if (!copy.isActiveAndEnabled)
        {
            return;
        }
        Vector3 d = copy.transform.position - at;
        if (reach * reach < d.sqrMagnitude)
        {
            return;
        }
        GillaBloodied bloodied = GillaBloodied.On(copy);
        GillaStains.Job job = new GillaStains.Job();
        job.point = at;
        job.reach = reach;
        job.strength = strength;
        job.stuff = Stuff.Blood;
        bloodied.Hit(job.point, job.reach, job.stuff) /* ? */;
    }
}
