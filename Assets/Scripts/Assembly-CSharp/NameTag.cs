// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(90)]
public class NameTag : MonoBehaviour
{
	private const int W = 128;
	private const int H = 24;
	private const int Line = 12;
	private const float TitleEvery = 0.5f;
	private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
	private static readonly int TagColour = Shader.PropertyToID("_TagColour");
	private static readonly int TagLight = Shader.PropertyToID("_TagLight");
	private static readonly List<NameTag> live = new List<NameTag>();
	[Tooltip("The head it floats over.")]
	public Transform head;
	[Tooltip("How far over the head's middle (m).")]
	public float above = 0.34f;
	[Tooltip("How wide the tag is (m); its height follows.")]
	public float width = 1.07f;
	[Tooltip("What they're wearing: the tag floats over anything on their head that's taller than where it would be (a chef's hat).")]
	public CosmeticWearer wearer;
	private const float OverWorn = 0.02f;
	private Texture2D tex;
	private Color32[] px;
	private Renderer quad;
	private MaterialPropertyBlock block;
	private string shown;
	private int title;
	private bool visible = true;
	private bool backwards;
	private bool mine;
	private GameObject body;
	private Dismemberment parts;
	private string nameFor;
	private float titleAt;
	private static bool nameChanged = true;
	private static NetworkBootstrap yoursFor;

	public Color32[] Pixels { get { return px; } }
	public string Text { get { return shown; } }
	public int TitleId { get { return title; } }
	public bool Showing
	{
		get
		{
			if (quad != null)
				return quad.enabled;
			return false;
		}
	}
	public static NameTag Yours { get; private set; }

	public static NameTag Create(Transform parent, Transform head, Material material, int layer = 0)
	{
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
		go.name = "NameTag";
		go.layer = layer;
		UnityEngine.Object.Destroy(go.GetComponent<Collider>());
		go.transform.SetParent(parent, false);
		NameTag tag = go.AddComponent<NameTag>();
		tag.head = head;
		tag.quad = go.GetComponent<Renderer>();
		tag.quad.sharedMaterial = material;
		tag.quad.shadowCastingMode = ShadowCastingMode.Off;
		tag.quad.receiveShadows = false;
		go.transform.localScale = new Vector3(tag.width, tag.width * H / W, 1f);
		tag.quad.enabled = false;
		return tag;
	}

	public void SetName(string name)
	{
		if (name != shown)
			Draw(name, title);
		SetVisible(visible);
	}

	public void SetTitle(int id)
	{
		if (title == id) return;
		Draw(shown, id);
	}

	public void SetVisible(bool show)
	{
		visible = show;
		bool on = show ? !string.IsNullOrEmpty(shown) : false;
		if (quad == null) return;
		if (on == quad.enabled) return;
		quad.enabled = on;
	}

	private void Draw(string name, int titleId)
	{
		shown = name;
		title = titleId;
		if (tex == null)
		{
			Texture2D t = new Texture2D(W, H, TextureFormat.RGBA32, false);
			t.filterMode = FilterMode.Point;
			t.wrapMode = TextureWrapMode.Clamp;
			t.name = "NameTag";
			tex = t;
			px = new Color32[W * H];
		}
		for (int i = 0; i < px.Length; i++)
			px[i] = new Color32(0, 0, 0, 0);
		if (!string.IsNullOrEmpty(name))
			Edged(name, 14, new Color32(255, 255, 255, 255), false);
		if (title > 0)
		{
			string text = NameTitles.Label(title);
			int w = PixelFont.Measure(text, 1);
			Color32 light = NameTitles.Light(title);
			Color32 bottom = NameTitles.Colour(title);
			PixelFont.Draw(px, W, H, (W - w) / 2, 2, text, light, bottom);
		}
		tex.SetPixels32(px);
		tex.Apply(false);
		if (quad == null) return;
		if (block == null)
			block = new MaterialPropertyBlock();
		quad.GetPropertyBlock(block);
		block.SetTexture(BaseMap, tex);
		block.SetColor(TagColour, NameTitles.Colour(title));
		block.SetColor(TagLight, NameTitles.Light(title));
		quad.SetPropertyBlock(block);
	}

	private void Edged(string s, int y, Color32 colour, bool edged = true)
	{
		int w = PixelFont.Measure(s, 1);
		int x = (W - w) / 2;
		if (edged)
		{
			for (int dx = -1; dx < 2; dx++)
			{
				for (int dy = -1; dy < 2; dy++)
				{
					if (dy != 0 || dx != 0)
						PixelFont.Draw(px, W, H, x + dx, y + dy, s, new Color32(10, 10, 14, 255), 1);
				}
			}
		}
		PixelFont.Draw(px, W, H, x, y, s, colour, 1);
	}

	public static void FaceMirror(Vector3 reflectedEye)
	{
		for (int i = 0; i < live.Count; i++)
		{
			if (live[i] != null)
				live[i].Face(reflectedEye, true);
		}
	}

	public static void FaceViewer(Vector3 eye)
	{
		for (int i = 0; i < live.Count; i++)
		{
			if (live[i] != null)
				live[i].Face(eye, false);
		}
	}

	private void Face(Vector3 eye, bool mirror)
	{
		if (head == null) return;
		if (!mirror)
			Flip(false);
		if (quad == null) return;
		if (!quad.enabled) return;
		Vector3 d = transform.position - eye;
		if (d.sqrMagnitude > 1e-06f)
			transform.rotation = Quaternion.LookRotation(d, Vector3.up);
		if (mirror)
			Flip(true);
	}

	private void Flip(bool reversed)
	{
		if (backwards == reversed) return;
		backwards = reversed;
		Vector3 s = transform.localScale;
		s.x = -s.x;
		transform.localScale = s;
	}

	public static void MakeYours()
	{
		NetworkBootstrap rig = NetworkBootstrap.Instance;
		if (!Application.isPlaying) return;
		if (Yours != null) return;
		if (rig == null) return;
		if (rig == yoursFor) return;
		yoursFor = rig;
		NetworkApe ape = null;
		if (rig.apePrefab != null)
			ape = rig.apePrefab.GetComponent<NetworkApe>();
		if (ape == null) return;
		Material tagMaterial = ApeTagMaterial(ape) /* ? */;
		if (tagMaterial == null) return;
		if (rig.head == null) return;
		if (rig.localColor == null) return;
		Transform parent = rig.transform;
		NameTag tag = Create(parent, rig.head, tagMaterial /* ? */, rig.localColor.gameObject.layer);
		tag.name = "NameTag (yours)";
		tag.mine = true;
		tag.body = rig.localColor.gameObject;
		Dismemberment d = null;
		if (rig.tearing != null)
		{
			Dismemberment[] copies = rig.tearing.copies;
			d = (copies == null || copies.Length == 0) ? null : copies[0];
		}
		tag.parts = d;
		GillaName.Changed -= NameChanged;
		GillaName.Changed += NameChanged;
		nameChanged = true;
		Yours = tag;
	}

	// ? NetworkApe has no 'nameTagMaterial' member in this build; look up the name-tag Material field by reflection.
	private static Material ApeTagMaterial(NetworkApe ape)
	{
		if (ape == null) return null;
		var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
		foreach (var f in ape.GetType().GetFields(flags))
		{
			if (f.FieldType == typeof(Material) && f.Name.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0)
				return f.GetValue(ape) as Material;
		}
		return null;
	}

	private static void NameChanged()
	{
		nameChanged = true;
	}

	private static CosmeticWearer HatsOn(NetworkBootstrap rig)
	{
		if (rig == null) return null;
		if (rig.tearing == null) return null;
		Dismemberment[] copies = rig.tearing.copies;
		if (copies != null)
		{
			for (int i = 0; i < copies.Length; i++)
			{
				Dismemberment d = copies[i];
				CosmeticWearer w = null;
				if (d != null)
					w = d.GetComponent<CosmeticWearer>();
				if (w != null && !w.skinOnly)
					return w;
			}
		}
		return null;
	}

	private void KeepYours()
	{
		if (wearer == null)
			wearer = HatsOn(yoursFor);
		string id = NetworkBootstrap.PlayerId;
		if (nameChanged || (object)id != (object)nameFor)
		{
			nameChanged = false;
			nameFor = id;
			SetName(GillaName.Current);
		}
		if (titleAt <= Time.unscaledTime)
		{
			titleAt = Time.unscaledTime + TitleEvery;
			int t = NameTitles.Shown;
			if (title != t)
				Draw(shown, t);
		}
		bool show;
		if (body != null && body.activeInHierarchy)
		{
			if (parts == null)
				show = true;
			else
				show = !parts.IsTorn(ApePart.Head) /* ? */;
		}
		else
		{
			show = false;
		}
		SetVisible(show);
	}

	private void LateUpdate()
	{
		float top = 0f;
		if (mine)
			KeepYours();
		if (quad == null) return;
		if (!quad.enabled) return;
		Flip(false);
		Camera cam = Camera.main;
		if (head != null)
		{
			Vector3 p = head.position;
			float up = above + width * Line / W * 0.5f;
			Vector3 pos = p + Vector3.up * up;
			if (wearer != null)
			{
				if (wearer.TopOfHead(out top))
				{
					float over = top + OverWorn + width * H / W * 0.5f;
					if (pos.y <= over)
						pos.y = over;
				}
			}
			transform.position = pos;
		}
		if (cam == null) return;
		Vector3 d = transform.position - cam.transform.position;
		if (d.sqrMagnitude <= 1e-06f) return;
		transform.rotation = Quaternion.LookRotation(d, Vector3.up);
	}

	private void OnEnable()
	{

		live.Add(this);
	}

	private void OnDisable()
	{
		live.Remove(this);
	}

	private void OnDestroy()
	{
		if (tex != null)
			UnityEngine.Object.Destroy(tex);
		if (Yours == this)
			Yours = null;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Forget()
	{
		live.Clear();
		Yours = null;
		yoursFor = null;
		nameChanged = true;
	}
}
