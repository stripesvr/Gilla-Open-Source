// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class GillaMonsterCam : MonoBehaviour
{
	[Tooltip("Where the picture goes (an unlit quad: its _BaseMap).")]
	public Renderer screen;
	[Tooltip("The strip over it (MONSTER CAM, LIVE).")]
	public Renderer label;
	[Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), the rest's (GillaLCD).")]
	public TextAsset titleFont;
	[Tooltip("Assets/Gilla/Fonts: the titles' (PixelOperatorHB8), the rest's (GillaLCD).")]
	public TextAsset textFont;
	public int width = 320;
	public int height = 180;
	public float fps = 15f;
	[Tooltip("Only drawn while you're this close to the screen (m).")]
	public float range = 22f;
	public float fov = 80f;
	public static bool ShowYours;
	private static float drawnAt = float.NegativeInfinity;
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
	private GillaMapVote.Canvas strip;
	private ScreenFont title;
	private ScreenFont text;
	private MaterialPropertyBlock block;
	private float next;
	private int shown = -1;

	public static bool Watched
	{
		get { return Time.unscaledTime - drawnAt < 0.5f; }
	}

	private void OnEnable()
	{

		title = titleFont != null ? new ScreenFont(titleFont.text) : null;
		text = textFont != null ? new ScreenFont(textFont.text) : null;
		if (label != null)
		{
			strip = new GillaMapVote.Canvas(label, LW, LH);
		}
		shown = -1;
		drawnAt = float.NegativeInfinity;
		Show(false);
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

	private static void Gone(Object o)
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
		if (!Application.isPlaying)
		{
			return;
		}
		GillaModeRound round = GillaModeRound.Current;
		NetworkBootstrap boot = NetworkBootstrap.Instance;
		NetworkApe ape = null;
		if (round != null)
		{
			if (round.Live && (int)round.Phase == 1)
			{
				ape = null;
				if (boot != null)
				{
					if (boot.Kind != NetworkBootstrap.Lobby.Monster)
					{
						ape = null;
					}
					else
					{
						ape = NetworkApe.Find(round.Monster);
					}
				}
			}
			else
			{
				ape = null;
			}
		}
		if (ape != null && ape.head != null && ape.IsMonster)
		{
			if (MonsterPlayer.IsMonster)
			{
				bool yours = ShowYours;
				Show(yours);
				if (!yours)
				{
					return;
				}
			}
			else
			{
				Show(true);
			}
			if (next <= Time.unscaledTime)
			{
				Camera main = Camera.main;
				if (main == null || screen == null)
				{
					return;
				}
				Vector3 diff = screen.transform.position - main.transform.position;
				if (!ShowYours)
				{
					if (range < diff.magnitude)
					{
						return;
					}
					if (Vector3.Dot(diff, main.transform.forward) < 0f)
					{
						return;
					}
				}
				float now = Time.unscaledTime;
				next = now + 1f / Mathf.Max(1f, fps);
				Render(ape.head);
			}
			return;
		}
		Show(false);
	}

	private void Render(Transform head)
	{
		if (cam == null)
		{
			RenderTexture tex = new RenderTexture(width, height, 16, (RenderTextureFormat)0);
			tex.name = "GillaMonsterCam";
			tex.antiAliasing = 1;
			rt = tex;
			GameObject go = new GameObject("MonsterCam");
			go.hideFlags = HideFlags.DontSave;
			cam = go.AddComponent<Camera>();
			cam.enabled = false;
			cam.targetTexture = rt;
			cam.fieldOfView = fov;
			cam.nearClipPlane = 0.25f;
			cam.farClipPlane = 60f;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color(0f, 0f, 0f, 1f);
			cam.cullingMask = ~0x700;
			UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
			data.renderPostProcessing = false;
			data.renderShadows = false;
		}
		Transform t = cam.transform;
		Vector3 pos = head.position;
		Vector3 fwd = head.forward;
		Quaternion rot = head.rotation;
		t.SetPositionAndRotation(pos + fwd * 0.12f, rot);
		drawnAt = Time.unscaledTime;
		GillaSonar.ForScreen(true);
		cam.Render();
		GillaSonar.ForScreen(false);
	}

	private void Show(bool live)
	{
		int want = live ? 1 : 0;
		if (shown == want)
		{
			return;
		}
		shown = want;
		if (screen != null)
		{
			if (!live && standby == null)
			{
				standby = NoSignal();
			}
			if (block == null)
			{
				block = new MaterialPropertyBlock();
			}
			screen.GetPropertyBlock(block);
			Texture tex = (live && rt != null) ? (Texture)rt : standby;
			block.SetTexture(BaseMap, tex);
			screen.SetPropertyBlock(block);
			if (live && rt == null)
			{
				shown = -1;
			}
		}
		if (strip == null)
		{
			return;
		}
		strip.Clear(Black, Scan);
		strip.Text(title, 8, 6, "MONSTER CAM", HeadText, 1, false);
		if (live)
		{
			strip.Fill(new RectInt(198, 9, 6, 6), Lit);
			strip.Text(text, 208, 7, "LIVE", HeadText, 1, false);
		}
		else
		{
			strip.Text(text, 186, 7, "OFF AIR", DimText, 1, false);
		}
		strip.Show();
	}

	private Texture2D NoSignal()
	{
		GillaMapVote.Canvas c = new GillaMapVote.Canvas(null, width / 2, height / 2);
		c.Clear(Black, Scan);
		c.Text(text, width / 4, height / 4 - 10, "NO SIGNAL", DimText, 2, true);
		c.Text(text, width / 4, height / 4 + 12, "WAITING FOR THE HUNT", DimText, 1, true);
		return c.Bake();
	}
}
