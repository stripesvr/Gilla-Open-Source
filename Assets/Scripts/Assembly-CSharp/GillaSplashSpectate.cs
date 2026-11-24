// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Fusion;

[ExecuteAlways]
public class GillaSplashSpectate : MonoBehaviour
{
    [Tooltip("Where the picture goes (unlit quads: their _BaseMap), and the strips over them.")]
    public Renderer[] screens = new Renderer[0];
    public Renderer[] labels = new Renderer[0];
    [Tooltip("The place (SplashMap): the course runs up its +Z, so the furthest along is the furthest up it.")]
    public Transform place;
    [Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), the rest's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), the rest's (GillaLCD).")]
    public TextAsset textFont;
    public int width = 320;
    public int height = 180;
    public float fps = 15f;
    [Tooltip("Only drawn while you're this close to a screen (m).")]
    public float range = 18f;
    public float fov = 75f;
    [Tooltip("Seconds it stays on one racer before it may go to another.")]
    public float hold = 4f;
    private const int LW = 256;
    private const int LH = 24;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly Color32 Black = new Color32(6, 6, 6, 255);
    private static readonly Color32 Scan = new Color32(13, 13, 13, 255);
    private static readonly Color32 Lit = GillaBoardLook.Dots;
    private static readonly Color32[] HeadText = GillaBoardLook.White;
    private static readonly Color32[] DimText = GillaBoardLook.Dim;
    private Camera cam;
    private RenderTexture rt;
    private Texture2D standby;
    private GillaMapVote.Canvas[] strips = new GillaMapVote.Canvas[0];
    private ScreenFont title;
    private ScreenFont text;
    private MaterialPropertyBlock block;
    private NetworkApe watching;
    private float next;
    private float heldUntil;
    private string shown;

    private void OnEnable()
    {

        title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        text = textFont != null ? new ScreenFont(textFont.text) : null;
        strips = new GillaMapVote.Canvas[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null)
            {
                strips[i] = new GillaMapVote.Canvas(labels[i], LW, LH);
            }
        }
        shown = null;
        Show(null);
    }

    private void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            Gone(rt);
        }
        if (standby != null)
        {
            Gone(standby);
        }
        if (cam != null)
        {
            Gone(cam.gameObject);
        }
    }

    private static void Gone(UnityEngine.Object o)
    {
        if (Application.isPlaying)
        {
            Destroy(o);
            return;
        }
        DestroyImmediate(o);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        GillaSplashGame game = GillaSplashGame.Current;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkApe who = null;
        if (game != null && game.Live && (int)game.Phase == 2 /* ? */ && boot != null)
        {
            who = boot.Kind == NetworkBootstrap.Lobby.Splash ? Pick(game) : null;
        }
        watching = who;
        Show(watching);
        if (watching == null || Time.unscaledTime < next) return;
        Camera main = Camera.main;
        if (main == null) return;
        if (!Watched(main.transform)) return;
        next = Time.unscaledTime + 1f / Mathf.Max(1f, fps);
        Render(watching.head);
    }

    private NetworkApe Pick(GillaSplashGame game)
    {
        bool Racing(NetworkApe a)
        {
            if (a != null && a.head != null /* ? */)
            {
                byte v;
                if (game.Players.TryGet(a.Object.InputAuthority /* ? */, out v))
                {
                    return v == 1;
                }
            }
            return false;
        }

        if (Racing(watching) && Time.unscaledTime < heldUntil)
        {
            return watching;
        }
        NetworkApe best = null;
        float bestZ = float.MinValue;
        foreach (NetworkApe a in NetworkApe.All)
        {
            if (!Racing(a)) continue;
            float z;
            if (place != null)
                z = place.InverseTransformPoint(a.head.position).z;
            else
                z = a.head.position.z;
            if (bestZ < z)
            {
                best = a;
                bestZ = z;
            }
        }
        if (best != watching)
        {
            heldUntil = Time.unscaledTime + hold;
        }
        return best;
    }

    private bool Watched(Transform eye)
    {
        for (int i = 0; i < screens.Length; i++)
        {
            Renderer screen = screens[i];
            if (screen == null) continue;
            Vector3 d = screen.transform.position - eye.position;
            if (d.magnitude <= range && Vector3.Dot(d, eye.forward) > 0f)
            {
                return true;
            }
        }
        return false;
    }

    private void Render(Transform head)
    {
        RaycastHit hit;
        if (cam == null)
        {
            RenderTexture tex = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
            tex.name = "GillaSplashSpectate";
            tex.antiAliasing = 1;
            rt = tex;
            GameObject go = new GameObject("SpectateCam");
            go.hideFlags = HideFlags.DontSave;
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.targetTexture = rt;
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.cullingMask = ~0x700;
            UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            for (int i = 0; i < screens.Length; i++)
            {
                Set(screens[i], rt);
            }
        }
        Vector3 back = Vector3.ProjectOnPlane(-head.forward, Vector3.up);
        if (back.sqrMagnitude < 0.0001f)
        {
            back = -Vector3.forward;
        }
        Vector3 pos = head.position + back.normalized * 2.4f + Vector3.up * 1.3f;
        Vector3 dir = pos - head.position;
        float dist = dir.magnitude;
        if (Physics.Raycast(head.position, dir.normalized, out hit, dist, ~0x1704, QueryTriggerInteraction.Ignore))
        {
            pos = head.position + dir.normalized * Mathf.Max(0.4f, hit.distance - 0.25f);
        }
        Transform t = cam.transform;
        Quaternion rotation = Quaternion.LookRotation(head.position + Vector3.up * 0.15f - pos, Vector3.up);
        t.SetPositionAndRotation(pos, rotation);
        cam.backgroundColor = RenderSettings.fogColor;
        cam.Render();
    }

    private void Show(NetworkApe who)
    {
        string name = null;
        if (who != null)
        {
            if (string.IsNullOrEmpty(who.PlayerName))
            {
                name = "PLAYER " + who.Object.InputAuthority.PlayerId.ToString() /* ? */;
            }
            else
            {
                name = who.PlayerName.ToUpperInvariant();
            }
        }
        string key = name ?? "";
        if (key == shown) return;
        shown = key;
        if (name == null || rt == null)
        {
            if (standby == null)
            {
                standby = NoSignal();
            }
            for (int i = 0; i < screens.Length; i++)
            {
                Set(screens[i], standby);
            }
        }
        else
        {
            for (int i = 0; i < screens.Length; i++)
            {
                Set(screens[i], rt);
            }
        }
        for (int i = 0; i < strips.Length; i++)
        {
            GillaMapVote.Canvas strip = strips[i];
            if (strip == null) continue;
            strip.Clear(Black, Scan);
            ScreenFont font;
            int x;
            Color32[] colours;
            string s;
            if (name == null)
            {
                strip.Text(title, 8, 6, "SPECTATE", DimText, 1, false);
                font = text;
                x = 186;
                colours = DimText;
                s = "NO RACE";
            }
            else
            {
                strip.Text(title, 8, 6, "SPECTATING", HeadText, 1, false);
                string n = name;
                if (name.Length > 18)
                {
                    n = name.Substring(0, 18);
                }
                strip.Text(text, 104, 7, n, HeadText, 1, false);
                strip.Fill(new RectInt(210, 9, 6, 6), Lit);
                font = text;
                x = 220;
                colours = HeadText;
                s = "LIVE";
            }
            strip.Text(font, x, 7, s, colours, 1, false);
            strip.Show();
        }
        if (name != null && rt == null)
        {
            shown = null;
        }
    }

    private void Set(Renderer screen, Texture tex)
    {
        if (screen == null) return;
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }

    private Texture2D NoSignal()
    {
        GillaMapVote.Canvas canvas = new GillaMapVote.Canvas(null, width / 2, height / 2);
        canvas.Clear(Black, Scan);
        canvas.Text(text, width / 4, height / 4 - 10, "NO SIGNAL", HeadText, 2, true);
        canvas.Text(text, width / 4, height / 4 + 12, "WAITING FOR A RACE", DimText, 1, true);
        return canvas.Bake();
    }
}
