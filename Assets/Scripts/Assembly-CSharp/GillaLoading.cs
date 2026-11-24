// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GillaLoading : MonoBehaviour
{
    public struct Words
    {
        public GillaIntegrity.Verdict verdict;
        public string status;
        public string latest;
        public string message;
        public string codes;
        public float progress;
        public int build;
    }

    [Tooltip("The camera (TrackedPose on the headset).")]
    public Transform head;
    [Tooltip("What's hung in front of you: the picture and the words under it.")]
    public Transform panel;
    [Tooltip("The head's picture, and the words under it (both Gilla/Dither).")]
    public Renderer picture;
    [Tooltip("The head's picture, and the words under it (both Gilla/Dither).")]
    public Renderer words;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset textFont;
    [Tooltip("The scene to go on to: the lobby.")]
    public string next = "Main";
    [Tooltip("The intro (Assets/Audio/Gilla_LoadingIntro.mp3), played as the head comes up.")]
    public AudioSource intro;

    private const float Distance = 3f;
    private const float Settle = 0.6f;
    private const float ExitTime = 0.9f;
    private const float MinShown = 4.5f;
    // Redux: these two came out of the decompile as zeros, which drew the face as one big pixel
    // (a solid square). Values read from the original game's data.
    private static readonly float[] StepAt = new float[] { 0f, 0.5f, 0.9f, 1.3f };
    private const double Lead = 0.1;
    private const float TurnAfter = 35f;
    private static readonly int[] Steps = new int[] { 20, 40, 80, 160 };
    private const int SourceSize = 512;
    private const int W = 256;
    private const int H = 88;
    private static readonly Color32[] Blue = new Color32[] { C(170, 240, 255), C(60, 170, 255) };
    private static readonly Color32[] Milk = new Color32[] { C(255, 255, 255), C(225, 225, 235) };
    private static readonly Color32[] Gold = new Color32[] { C(255, 240, 170), C(230, 170, 50) };
    private static readonly Color32[] Red = new Color32[] { C(255, 150, 140), C(220, 40, 40) };
    private static readonly Color32[] Grey = new Color32[] { C(150, 148, 155), C(120, 118, 125) };
    private static readonly Color32 BarOff = new Color32(28, 36, 52, 255);
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly int CellsId = Shader.PropertyToID("_Cells");
    private static readonly int LodId = Shader.PropertyToID("_Lod");
    private static readonly int LevelsId = Shader.PropertyToID("_Levels");
    private static readonly int GainId = Shader.PropertyToID("_Gain");
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
    private static readonly int BandId = Shader.PropertyToID("_GlitchBand");
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private AsyncOperation load;
    private float started;
    private float exitAt = -1f;
    private float shownProgress;
    private float yaw;
    private float height;
    private bool going;
    private bool placed;
    private bool queued;
    private double introAt;
    private Material pictureMat;
    private Material wordsMat;
    private Texture2D tex;
    private Color32[] px;
    private ScreenFont title;
    private ScreenFont text;
    private string drawn;

    private static Color32 C(byte r, byte g, byte b)
    {
        return new Color32(r, g, b, 255);
    }

    private void Start()
    {
        started = Time.unscaledTime;
        GillaIntegrity.Begin();
        load = SceneManager.LoadSceneAsync(next);
        if (load != null)
        {
            load.allowSceneActivation = false;
            return;
        }
        Debug.LogError("[GillaLoading] there's no scene '" + next + "' in the build to go on to");
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        float p = 0f;
        if (load != null)
        {
            p = Mathf.Clamp01(load.progress / 0.9f);
        }
        shownProgress = Mathf.MoveTowards(shownProgress, p, Time.unscaledDeltaTime * 1.5f);
        float t = now - started;

        bool ok;
        float exit;
        if (GillaIntegrity.Result == GillaIntegrity.Verdict.Cleared)
        {
            ok = true;
            exit = exitAt;
            if (ok && exit < 0f && p >= 1f && load != null && t >= MinShown && shownProgress >= 1f)
            {
                exitAt = t;
                exit = t;
            }
        }
        else if (Application.isEditor)
        {
            ok = GillaIntegrity.Cleared;
            exit = exitAt;
            if (ok && exit < 0f && p >= 1f && load != null && t >= MinShown && shownProgress >= 1f)
            {
                exitAt = t;
                exit = t;
            }
        }
        else
        {
            ok = false;
            exit = exitAt;
        }

        if (exit >= 0f && !going && t - exit >= ExitTime)
        {
            going = true;
            load.allowSceneActivation = true;
            if (intro != null && intro.isPlaying && intro.clip != null)
            {
                intro.transform.SetParent(null, false);
                DontDestroyOnLoad(intro.gameObject);
                GameObject go = intro.gameObject;
                float left = intro.clip.length - intro.time;
                Destroy(go, Mathf.Max(0.1f, left));
            }
        }

        if (t >= 0.5f && !queued)
        {
            if (intro != null && intro.clip != null)
            {
                queued = true;
                introAt = AudioSettings.dspTime + Lead;
                intro.PlayScheduled(introAt);
            }
        }

        float shown = t;
        if (queued)
        {
            shown = Mathf.Max((float)(AudioSettings.dspTime - introAt) + Settle, t - 0.25f);
        }

        float ex = exitAt;
        Words w = new Words();
        w.verdict = Application.isEditor ? GillaIntegrity.Verdict.Cleared : GillaIntegrity.Result;
        w.status = ok ? "LOADING" : GillaIntegrity.Status;
        w.progress = shownProgress;
        w.build = GillaIntegrity.Build != null ? GillaIntegrity.Build.code : 0;
        w.latest = GillaIntegrity.Latest;
        w.message = GillaIntegrity.Message;
        w.codes = GillaIntegrity.Codes;
        float exitArg = -1f;
        if (ex >= 0f)
        {
            exitArg = shown - (t - ex);
        }
        Show(shown, exitArg, w);
    }

    private void LateUpdate()
    {
        if (head == null || panel == null)
        {
            return;
        }
        Vector3 pos = head.position;
        Vector3 fwd = head.forward;
        float target;
        if (fwd.x * fwd.x + fwd.z * fwd.z <= 0.0001f)
        {
            target = yaw;
        }
        else
        {
            target = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
        }

        if (!placed || Time.unscaledTime - started < Settle)
        {
            yaw = target;
            height = pos.y;
            placed = true;
        }
        else
        {
            float delta = Mathf.DeltaAngle(yaw, target);
            if (Mathf.Abs(delta) > TurnAfter)
            {
                yaw = yaw + delta * (1f - Mathf.Exp(Time.unscaledDeltaTime * -3f));
            }
            float k = Mathf.Clamp01(1f - Mathf.Exp(Time.unscaledDeltaTime * -2f));
            height = height + (pos.y - height) * k;
        }

        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 dir = rotation * Vector3.forward;
        Vector3 position = new Vector3(pos.x + dir.x * Distance, height + dir.y * Distance, pos.z + dir.z * Distance);
        panel.SetPositionAndRotation(position, rotation);
    }

    public void Preview(float t, float exitAt, GillaLoading.Words w)
    {
        Show(t, exitAt, w);
    }

    private void Show(float t, float exitAt, GillaLoading.Words w)
    {
        Draw(w);
        bool bad = w.verdict == GillaIntegrity.Verdict.Outdated || w.verdict == GillaIntegrity.Verdict.Tampered;
        float lastStep = StepAt[3];
        float s = t - Settle;
        float levels = 3f;
        int idx;
        if (s < StepAt[1]) idx = 0;
        else if (s < StepAt[2]) idx = 1;
        else if (s < StepAt[3]) idx = 2;
        else { idx = 3; levels = 6f; }

        float gain = 0f;
        float up = Mathf.Clamp01(s / 0.8f);
        float reveal = 0f;
        if (s > 0f)
        {
            float ease = Mathf.Clamp01(1f - (1f - up) * (1f - up));
            gain = ease * 0.7f + 0.3f;
            reveal = Mathf.Min(s * 3f + 0.35f, 1.5f);
        }
        if (s < lastStep)
        {
            float since = Mathf.Max(s, 0f);
            gain = gain * ((1f - since / StepAt[3]) * -0.3f * Hash((int)(t * 14f)) + 1f);
        }
        float wordsGain = Mathf.Clamp01((s - 0.9f) / 0.6f);
        if (exitAt >= 0f)
        {
            float e = Mathf.Clamp01((t - exitAt) / ExitTime);
            idx = Mathf.Min(idx, 3 - (int)(e * 3.99f));
            gain = gain * (1f - e);
            levels = 3f;
            wordsGain = wordsGain * (1f - Mathf.Clamp01(e * 1.6f));
        }

        float chance = lastStep <= s ? 0.035f : 0.3f;
        if (bad) chance = 0.14f;
        float bandY = 0f;
        float bandX = 0.5f;
        float glitch = 0f;
        if (exitAt < 0f && s > 0f)
        {
            float f = (int)(t / 0.11f);
            if (Hash(f * 1.7f + 3f) < chance)
            {
                float a = Hash(f + 11f);
                bandX = Hash(f + 23f);
                float c = Hash(f + 31f);
                glitch = (a * 2f - 1f) * 0.06f;
                levels = 3f;
                bandY = c * 0.07f + 0.02f;
            }
        }

        // Redux: the face picture itself is red now (Gilla_LoadingHead.png), so no tint by default
        float tintG = bad ? 0.42f : 1f;
        float tintB = bad ? 0.4f : 1f;
        int cells = Steps[idx];

        Material pm = Mat(picture, ref pictureMat);
        if (pm != null)
        {
            float cf = cells;
            pm.SetVector(CellsId, new Vector4(cf, cf, 0f, 0f));
            pm.SetFloat(LodId, (float)Math.Log(SourceSize / cf, 2.0));
            pm.SetFloat(LevelsId, levels);
            pm.SetFloat(GainId, gain);
            pm.SetFloat(RevealId, reveal);
            pm.SetFloat(GlitchId, glitch * cf);
            pm.SetVector(BandId, new Vector4(bandX, bandY, 0f, 0f));
            pm.SetColor(TintId, new Color(1f, tintG, tintB, 1f));
        }

        Material wm = Mat(words, ref wordsMat);
        if (wm != null)
        {
            if (wm.GetTexture(MainTexId) != tex)
            {
                wm.SetTexture(MainTexId, tex);
            }
            wm.SetVector(CellsId, new Vector4(W, H, 0f, 0f));
            wm.SetFloat(LodId, 0f);
            wm.SetFloat(LevelsId, 6f);
            wm.SetFloat(GainId, wordsGain);
            wm.SetFloat(RevealId, 1.5f);
            wm.SetFloat(GlitchId, glitch * 256f * 0.5f);
            wm.SetVector(BandId, new Vector4(bandX, bandY, 0f, 0f));
            wm.SetColor(TintId, new Color(1f, 1f, 1f, 1f));
        }
    }

    private static Material Mat(Renderer r, ref Material own)
    {
        if (r == null)
        {
            return null;
        }
        if (!Application.isPlaying)
        {
            return r.sharedMaterial;
        }
        if (own != null)
        {
            return own;
        }
        own = r.material;
        return own;
    }

    private static float Hash(float n)
    {
        float v = Mathf.Sin(n * 12.9898f) * 43758.547f;
        return Mathf.Clamp01(v - (int)v);
    }

    private void Draw(GillaLoading.Words w)
    {
        GillaIntegrity.Verdict verdict = w.verdict;
        int bars = Mathf.RoundToInt(w.progress * 30f);
        string key = string.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}", w.verdict, w.status, bars, w.build, w.latest, w.message, w.codes);
        if (key == drawn && tex != null)
        {
            return;
        }
        drawn = key;
        if (tex == null)
        {
            Texture2D t2 = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t2.filterMode = FilterMode.Point;
            t2.wrapMode = TextureWrapMode.Clamp;
            t2.hideFlags = HideFlags.DontSave;
            t2.name = "GillaLoadingWords";
            tex = t2;
            px = new Color32[W * H];
        }
        for (int i = 0; i < px.Length; i++)
        {
            px[i] = Clear;
        }
        if (title == null)
        {
            title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        }
        if (text == null)
        {
            text = textFont != null ? new ScreenFont(textFont.text) : null;
        }

        bool bad = verdict == GillaIntegrity.Verdict.Outdated || verdict == GillaIntegrity.Verdict.Tampered;
        ScreenFont.Style ts = new ScreenFont.Style();
        ts.scale = 3;
        ts.slant = 0.25f;
        if (!bad)
        {
            ts.colours = Blue;
            ts.shadow = new Color32(0, 40, 90, 255);
        }
        else
        {
            ts.colours = Red;
            ts.shadow = new Color32(70, 0, 0, 255);
        }
        if (title != null && title.Measure("GILLA REDUX", ts.scale) > W - 8)
        {
            ts.scale = 2;   // Redux: the longer name has to fit the screen
        }
        Line(title, 128, 2, "GILLA REDUX", ts);

        string ver = "editor";
        if (w.build > 0)
        {
            ver = "build " + w.build.ToString();
        }

        if (w.verdict == GillaIntegrity.Verdict.Tampered)
        {
            Line(text, 128, 32, "THIS COPY OF GILLA WAS CHANGED", new ScreenFont.Style { scale = 1, slant = 0f, bold = true, colours = Red });
            Line(text, 128, 44, "Reinstall the game to play.", new ScreenFont.Style { scale = 1, slant = 0f, colours = Milk });
            Line(text, 128, 54, "code " + w.codes, new ScreenFont.Style { scale = 1, slant = 0f, colours = Grey });
        }
        else if (w.verdict == GillaIntegrity.Verdict.Outdated)
        {
            Line(text, 128, 32, "UPDATE NEEDED", new ScreenFont.Style { scale = 1, slant = 0f, bold = true, colours = Gold });
            Line(text, 128, 44, "You have " + ver + ".", new ScreenFont.Style { scale = 1, slant = 0f, colours = Milk });
            string newest = "There's a newer one.";
            if (!string.IsNullOrEmpty(w.latest))
            {
                newest = "The newest is " + w.latest + ".";
            }
            Line(text, 128, 54, newest, new ScreenFont.Style { scale = 1, slant = 0f, colours = Milk });
            if (!string.IsNullOrEmpty(w.message))
            {
                Line(text, 128, 64, Fit(w.message), new ScreenFont.Style { scale = 1, slant = 0f, colours = Gold });
            }
        }
        else
        {
            string label;
            if (w.status == "LOADING")
            {
                int pct = Mathf.RoundToInt(w.progress * 100f);
                label = "LOADING " + pct.ToString() + "%";
            }
            else
            {
                label = w.status ?? "";
            }
            Line(text, 128, 34, label, new ScreenFont.Style { scale = 1, slant = 0f, colours = Milk });
            for (int i = 0; i < 30; i++)
            {
                for (int r = 47; r < 52; r++)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        Color32 c;
                        if (i < bars)
                        {
                            c = ScreenFont.Gradient(Blue, (r - 47) / 4f);
                        }
                        else
                        {
                            c = BarOff;
                        }
                        px[(H - 1 - r) * W + 23 + i * 7 + k] = c;
                    }
                }
            }
        }

        Line(text, 128, 78, "v" + 39.ToString() + "  " + ver, new ScreenFont.Style { scale = 1, slant = 0f, colours = Grey });
        tex.SetPixels32(px);
        tex.Apply(false);
    }

    private static string Fit(string line)
    {
        if (line.Length < 43)
        {
            return line;
        }
        return line.Substring(0, 39) + "...";
    }

    private void Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style)
    {
        if (font == null)
        {
            int scale = Mathf.Max(style.scale, 1);
            int width = PixelFont.Measure(line, scale);
            PixelFont.Draw(px, W, H, x - width / 2, y, line, style.colours[0], scale);
        }
        else
        {
            font.DrawCentred(px, W, H, x, y, line, style);
        }
    }

    private void OnDestroy()
    {
        if (tex != null)
        {
            if (Application.isPlaying)
            {
                Destroy(tex);
            }
            else
            {
                DestroyImmediate(tex);
            }
        }
        if (pictureMat != null)
        {
            Destroy(pictureMat);
        }
        if (wordsMat != null)
        {
            Destroy(wordsMat);
        }
    }
}
