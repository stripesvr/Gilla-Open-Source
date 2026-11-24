// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(500)]
public class GillaToast : MonoBehaviour
{
    public Material material;
    public TextAsset titleFont;
    public TextAsset textFont;
    private const float Life = 3f;
    private const float PopIn = 0.5f;
    private const float ShrinkOut = 0.25f;
    private const float Lag = 0.16f;
    private const float OverHand = 0.2f;
    private const float MetresAPixel = 0.0015f;
    private const int Pad = 6;
    private const int BarWide = 4;
    private const int TitleScale = 2;
    private const int Gap = 3;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Color32 Panel = new Color32(14, 14, 16, 235);
    private static readonly Color32 Good = new Color32(84, 232, 112, 255);
    private static readonly Color32 Bad = new Color32(255, 72, 64, 255);
    private static readonly Color32 Line = new Color32(236, 236, 236, 255);
    private static GillaToast here;
    private ScreenFont title;
    private ScreenFont text;
    private Transform quad;
    private MeshRenderer shown;
    private MaterialPropertyBlock block;
    private Texture2D tex;
    private Color32[] px;
    private int hand;
    private float age = 3f;
    private float wide;
    private float high;
    private Vector3 at;
    private Vector3 moving;

    private void OnEnable()
    {

        here = this;
    }

    private void OnDisable()
    {
        if (here == this)
        {
            here = null;
        }
        if (shown != null)
        {
            shown.enabled = false;
        }
        age = Life;
    }

    public static void Bought(int hand, string what, int coins)
    {
        Show(hand, "PURCHASED", what + " - " + coins.ToString("N0") + " COINS SPENT", false);
    }

    public static void NotEnough(int hand, int coins)
    {
        int need = coins - Coins.Balance;
        if (need < 2)
        {
            need = 1;
        }
        Show(hand, "NOT ENOUGH COINS", "YOU NEED " + need.ToString("N0") + " MORE TO BUY THIS", true);
    }

    public static void Show(int hand, string title, string line, bool error)
    {
        if (here != null)
        {
            here.Begin(hand, title, line, error);
        }
    }

    public static int NearerHand(Vector3 to)
    {
        Transform left = Hand(0);
        Transform right = Hand(1);
        if (!(left == null) && !(right == null))
        {
            return (right.position - to).sqrMagnitude < (left.position - to).sqrMagnitude ? 1 : 0;
        }
        if (left != null)
        {
            return 0;
        }
        return right != null ? 1 : 2;
    }

    private static Transform Hand(int h)
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot != null && boot.leftHand != null)
        {
            if (h == 0) return boot.leftHand;
            if (h == 1) return boot.rightHand;
            return null;
        }
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance /* ? */;
        if (player == null)
        {
            return null;
        }
        if (h == 0)
        {
            return player.leftHandFollower;
        }
        if (h != 1)
        {
            return null;
        }
        return player.rightHandFollower;
    }

    private void Begin(int onHand, string titleLine, string line, bool error)
    {
        if (material == null)
        {
            return;
        }
        Draw(titleLine, line, error);
        if (quad == null)
        {
            MakeQuad();
        }
        block.SetTexture(BaseMap, tex);
        shown.SetPropertyBlock(block);
        shown.enabled = true;
        hand = onHand;
        age = 0f;
        at = Target();
        moving = Vector3.zero;
        Place(0f);
    }

    private void Draw(string titleLine, string line, bool error)
    {
        if (title == null)
        {
            title = new ScreenFont(titleFont.text);
        }
        if (text == null)
        {
            text = new ScreenFont(textFont.text);
        }
        int w1 = title.Measure(titleLine, TitleScale);
        int w2 = text.Measure(line, 1);
        if (w1 <= w2)
        {
            w1 = w2;
        }
        int width = w1 + 16;
        int height = text.size + title.size * 2 + 15;
        if (tex == null || tex.width != width || tex.height != height)
        {
            if (tex != null)
            {
                Destroy(tex);
            }
            Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            tex = t;
            px = new Color32[height * width];
        }
        Color32 accent = error ? Bad : Good;
        int row = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                px[row + x] = x < BarWide ? accent : Panel;
            }
            row += width;
        }
        ScreenFont.Style style = new ScreenFont.Style();
        style.scale = TitleScale;
        style.colours = new Color32[] { accent, accent };
        title.Draw(px, width, height, 10, Pad, titleLine, in style);
        int titleSize = title.size;
        ScreenFont.Style style2 = new ScreenFont.Style();
        style2.scale = 1;
        style2.colours = new Color32[] { Line, Line };
        text.Draw(px, width, height, 10, titleSize * 2 + 9, line, in style2);
        tex.SetPixels32(px);
        tex.Apply(false);
        wide = width * MetresAPixel;
        high = height * MetresAPixel;
    }

    private void MakeQuad()
    {
        GameObject go = new GameObject("Toast");
        go.layer = gameObject.layer;
        quad = go.transform;
        quad.SetParent(transform, false);
        Mesh mesh = new Mesh();
        mesh.name = "Toast";
        mesh.SetVertices(new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        });
        mesh.SetUVs(0, new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        });
        mesh.SetTriangles(new int[] { 0, 2, 1, 0, 3, 2 } /* ? */, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        shown = go.AddComponent<MeshRenderer>();
        shown.sharedMaterial = material;
        shown.shadowCastingMode = ShadowCastingMode.Off;
        shown.receiveShadows = false;
        shown.lightProbeUsage = LightProbeUsage.Off;
        shown.reflectionProbeUsage = ReflectionProbeUsage.Off;
        block = new MaterialPropertyBlock();
    }

    private Vector3 Target()
    {
        Transform h = Hand(hand);
        if (h != null)
        {
            return h.position + Vector3.up * OverHand * MonsterPlayer.ApeSize;
        }
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position + cam.transform.forward * 0.6f;
        }
        return at;
    }

    private void LateUpdate()
    {
        if (age >= Life)
        {
            return;
        }
        age += Time.unscaledDeltaTime;
        if (age >= Life)
        {
            shown.enabled = false;
            return;
        }
        Place(Time.unscaledDeltaTime);
    }

    private void Place(float dt)
    {
        if (dt > 0f)
        {
            at = Vector3.SmoothDamp(at, Target(), ref moving, Lag, Mathf.Infinity, dt);
        }
        quad.position = at;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 dir = at - cam.transform.position;
            if (dir.sqrMagnitude > 1e-06f)
            {
                quad.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
        float size = MonsterPlayer.ApeSize;
        float pop = CosmeticToy.Elastic(age / PopIn);
        float shrink = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Life - age) / ShrinkOut));
        float s = size * pop * shrink;
        quad.localScale = new Vector3(s * wide, s * high, 1f);
    }
}
