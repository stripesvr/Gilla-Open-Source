// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[ExecuteAlways]
public class GillaLeaderboard : MonoBehaviour
{
    public struct Row
    {
        public string name;
        public string id;
        public int level;
        public Color32 colour;
        public bool you;
    }

    public const int Rows = 10;
    public const int W = 320;
    public const int H = 240;
    public const int RowTop = 38;
    public const int RowStep = 18;

    public static readonly string[] Reasons = new string[] { "HATE SPEECH", "CHEATING", "TOXIC BEHAVIOUR" };

    public Renderer screen;
    [Tooltip("MUTE and REPORT, one of each level with each row, top to bottom.")]
    public PressKey[] muteKeys = new PressKey[0];
    [Tooltip("MUTE and REPORT, one of each level with each row, top to bottom.")]
    public PressKey[] reportKeys = new PressKey[0];
    [Tooltip("Under the screen: HATE, CHEAT, TOXIC, CANCEL.")]
    public PressKey[] reasonKeys = new PressKey[0];
    [Tooltip("The reasons' keys and what they sit on: only out once REPORT's been pressed on someone (the user, 2026-11-04: \"make sure the report options only pop up if someone has reported a player\").")]
    public GameObject reasonPanel;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset titleFont;
    [Tooltip("Assets/Gilla/Fonts: the title's (PixelOperatorHB8), and everything else's (GillaLCD).")]
    public TextAsset textFont;
    public AudioClip sentSound;
    public AudioClip nopeSound;
    [Tooltip("The wheel beside the screen that scrolls it, a row a step (ScrollDrum). None: the first Rows players only.")]
    public ScrollDrum drum;
    public static List<Row> Preview;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly HashSet<string> reported = new HashSet<string>();
    private readonly List<Row> rows = new List<Row>();
    private Texture2D tex;
    private Color32[] px;
    private MaterialPropertyBlock block;
    private ScreenFont title;
    private ScreenFont text;
    private string shown;
    private float nextRefresh;
    private float pickUntil;
    private float saidUntil;
    private Row picking;
    private string said = "";
    private bool keysOut;
    private bool keysHidden;
    private int top;

    public static string LastReport { get; private set; }

    public int Top { get { return top; } }

    public IReadOnlyList<Row> Shown { get { return rows; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Forget()
    {
        Preview = null;
        LastReport = null;
        reported.Clear();
    }

    public static bool Reported(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }
        return reported.Contains(id);
    }

    private void OnEnable()
    {

        for (int i = 0; i < muteKeys.Length; i++)
        {
            PressKey k = muteKeys[i];
            if (k != null) k.Pressed += OnMute;
        }
        for (int i = 0; i < reportKeys.Length; i++)
        {
            PressKey k = reportKeys[i];
            if (k != null) k.Pressed += OnReport;
        }
        for (int i = 0; i < reasonKeys.Length; i++)
        {
            PressKey k = reasonKeys[i];
            if (k != null) k.Pressed += OnReason;
        }
        Redraw(true);
    }

    private void OnDisable()
    {
        for (int i = 0; i < muteKeys.Length; i++)
        {
            PressKey k = muteKeys[i];
            if (k != null) k.Pressed -= OnMute;
        }
        for (int i = 0; i < reportKeys.Length; i++)
        {
            PressKey k = reportKeys[i];
            if (k != null) k.Pressed -= OnReport;
        }
        for (int i = 0; i < reasonKeys.Length; i++)
        {
            PressKey k = reasonKeys[i];
            if (k != null) k.Pressed -= OnReason;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        bool go = false;
        if (drum != null)
        {
            int step = drum.Step;
            int max = Mathf.Max(rows.Count - 10, 0);
            int t = step < 0 ? 0 : Mathf.Min(step, max);
            if (t != top) go = true;
        }
        if (go || nextRefresh <= Time.time)
        {
            nextRefresh = Time.time + 0.25f;
            if (picking.id != null && pickUntil < Time.time)
            {
                picking = default(Row);
            }
            Redraw(false);
        }
    }

    public void Refresh()
    {
        Redraw(true);
    }

    private void Gather()
    {
        rows.Clear();
        if (Preview == null)
        {
            if (Application.isPlaying)
            {
                foreach (NetworkApe ape in NetworkApe.All)
                {
                    if (ape == null) continue;
                    if (ape.Object == null) continue;
                    Fusion.NetworkObject obj = ape.Object;
                    if (!obj.Runner) continue;
                    if (!obj.IsValid /* ? */) continue;
                    Row row = new Row();
                    string playerName = ape.PlayerName;
                    row.name = playerName.Length < 1 ? "..." : playerName;
                    row.id = ape.AccountId;
                    row.level = ape.PlayerLevel;
                    row.colour = ape.FurColour;
                    row.you = ape.IsLocal;
                    rows.Add(row);
                }
            }
        }
        else
        {
            rows.AddRange(Preview);
        }
        rows.Sort((p, q) =>
        {
            if (p.level != q.level) return q.level.CompareTo(p.level);
            return string.CompareOrdinal(p.name, q.name);
        });
        int max = Mathf.Max(rows.Count - 10, 0);
        if (drum != null)
        {
            drum.max = max;
        }
        int t;
        if (drum != null)
        {
            int step = drum.Step;
            t = step < 0 ? 0 : Mathf.Min(step, max);
        }
        else
        {
            t = 0;
        }
        top = t;
    }

    private bool Target(PressKey[] keys, PressKey key, out Row row)
    {
        int i = Array.IndexOf(keys, key);
        row = default(Row);
        if (i < 0)
        {
            return false;
        }
        int index = top + i;
        if (rows.Count <= index)
        {
            return false;
        }
        if (rows[index].you)
        {
            return false;
        }
        if (string.IsNullOrEmpty(rows[index].id))
        {
            return false;
        }
        row = rows[index];
        return true;
    }

    private void Sound(AudioClip clip, PressKey key, float volume)
    {
        if (clip == null)
        {
            return;
        }
        AudioClip[] clips = new AudioClip[] { clip };
        Sfx.Play(clips, key.transform.position, volume, 0.95f, 1.05f);
    }

    private void OnMute(PressKey key)
    {
        Row row;
        if (Target(muteKeys, key, out row))
        {
            bool muted = GillaMutes.Toggle(row.id);
            said = (muted ? "MUTED " : "YOU CAN HEAR ") + row.name;
            saidUntil = Time.time + 5f;
            Redraw(false);
        }
        else
        {
            Sound(nopeSound, key, 0.4f);
        }
    }

    private void OnReport(PressKey key)
    {
        Row row;
        if (Target(reportKeys, key, out row))
        {
            if (!Reported(row.id))
            {
                picking = row;
                said = "";
                pickUntil = Time.time + 20f;
                Redraw(false);
                return;
            }
        }
        Sound(nopeSound, key, 0.4f);
    }

    private void OnReason(PressKey key)
    {
        int i = Array.IndexOf(reasonKeys, key);
        string id;
        if (i < 0 || (id = picking.id) == null)
        {
            Sound(nopeSound, key, 0.4f);
            return;
        }
        string name = picking.name;
        picking = default(Row);
        if (i < Reasons.Length)
        {
            reported.Add(id);
            NetworkBootstrap boot = NetworkBootstrap.Instance;
            string msg = string.Concat(new string[]
            {
                Reasons[i], " | ", name, " reported by ", GillaName.Current, " | room ",
                boot != null ? boot.RoomLabel : "?"
            });
            LastReport = id + " " + msg;
            GillaCloud.Reported(id, msg);
            Debug.Log("[Leaderboard] reported " + LastReport);
            Sound(sentSound, key, 0.8f);
            said = "REPORTED " + name + ". THANK YOU";
            saidUntil = Time.time + 5f;
        }
        Redraw(false);
    }

    private void Say(string words)
    {
        said = words;
        saidUntil = Time.time + 5f;
    }

    private void ShowKeys()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        for (int i = 0; i < 10; i++)
        {
            int index = i + top;
            bool on;
            if (index < rows.Count && !rows[index].you)
            {
                on = !string.IsNullOrEmpty(rows[index].id);
            }
            else
            {
                on = false;
            }
            if (i < muteKeys.Length && muteKeys[i] != null)
            {
                if (on != muteKeys[i].gameObject.activeSelf)
                {
                    muteKeys[i].gameObject.SetActive(on);
                }
            }
            if (i < reportKeys.Length && reportKeys[i] != null)
            {
                if (on != reportKeys[i].gameObject.activeSelf)
                {
                    reportKeys[i].gameObject.SetActive(on);
                }
            }
        }
        string pid = picking.id;
        if (reasonPanel != null)
        {
            if ((pid != null) != reasonPanel.activeSelf)
            {
                reasonPanel.SetActive(pid != null);
            }
        }
    }

    private static bool AnyRunnerRunning()
    {
        foreach (Fusion.NetworkRunner r in Fusion.NetworkRunner.Instances)
        {
            if (r != null && r.IsRunning) return true;
        }
        return false;
    }

    private void Redraw(bool force)
    {
        Gather();
        ShowKeys();
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        bool live;
        if (Application.isPlaying)
        {
            if (Preview == null)
            {
                live = boot != null && AnyRunnerRunning() /* ? boot.runner is private: boot.runner != null && boot.runner.IsRunning */;
            }
            else
            {
                live = true;
            }
        }
        else
        {
            live = false;
        }

        if (said.Length > 0 && saidUntil < Time.time)
        {
            said = "";
        }

        StringBuilder sb = new StringBuilder();
        foreach (Row r in rows)
        {
            sb.Append(r.name).Append(r.level).Append(r.colour.r).Append(r.colour.g).Append(r.colour.b)
              .Append(r.you).Append(GillaMutes.IsMuted(r.id)).Append(Reported(r.id)).Append('|');
        }
        string key = string.Concat(new string[]
        {
            sb.ToString(), picking.id, said, live.ToString(),
            boot != null ? boot.RoomLabel : "", "@", top.ToString()
        });
        if (!force && key == shown && tex != null)
        {
            return;
        }
        shown = key;

        if (tex == null)
        {
            Texture2D t = new Texture2D(320, 240, TextureFormat.RGBA32, true);
            t.filterMode = FilterMode.Point;
            t.wrapMode = TextureWrapMode.Clamp;
            t.hideFlags = HideFlags.DontSave;
            t.name = "GillaLeaderboard";
            tex = t;
            px = new Color32[76800];
        }
        GillaBoardLook.Clear(px, 320, 240);
        GillaBoardLook.Frame(px, 320, 240, 3);

        if (title == null)
        {
            title = titleFont != null ? new ScreenFont(titleFont.text) : null;
        }
        if (text == null)
        {
            text = textFont != null ? new ScreenFont(textFont.text) : null;
        }

        Line(title, 12, 8, "Leaderboard", new ScreenFont.Style { scale = 2, slant = 0f, colours = GillaBoardLook.White }, false);

        if (live)
        {
            string head = string.Concat(new string[]
            {
                "ROOM ", boot != null ? boot.RoomLabel : "", "   ", rows.Count.ToString(),
                rows.Count != 1 ? " PLAYERS" : " PLAYER"
            });
            int hw = Measure(text, head, 1);
            Line(text, 308 - hw, 14, head, new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.Dim }, false);
        }

        GillaBoardLook.Rule(px, 320, 240, 8, 311, 30);

        int count = rows.Count;
        int w = count < 11 ? 304 : 296;
        int y = 42;
        for (int i = 0; y != 222; i++, y += 18)
        {
            int index = i + top;
            bool inRange = index < rows.Count;
            if (inRange && picking.id != null && rows[index].id == picking.id)
            {
                Fill(8, y - 4, w, 16, GillaBoardLook.PickedBand);
            }
            else
            {
                Fill(8, y - 4, w, 16, (index & 1) == 0 ? GillaBoardLook.OtherBand : GillaBoardLook.Band);
                if (!inRange) continue;
            }

            Row row = rows[index];
            bool muted;
            bool sent;
            if (row.you)
            {
                muted = false;
                sent = false;
            }
            else
            {
                muted = GillaMutes.IsMuted(row.id);
                sent = Reported(row.id);
            }
            int place = index + 1;
            int nw = Measure(text, place.ToString(), 1);
            Line(text, 22 - nw, y, place.ToString(),
                new ScreenFont.Style { scale = 1, slant = 0f, colours = index == 0 ? GillaBoardLook.White : GillaBoardLook.Dim }, false);
            Fill(26, y - 2, 12, 12, GillaBoardLook.Edge);
            Fill(27, y - 1, 10, 10, row.colour);
            Line(text, 44, y, Cut(row.name),
                new ScreenFont.Style
                {
                    bold = row.you,
                    scale = 1,
                    slant = 0f,
                    colours = muted ? GillaBoardLook.Dim : (row.you ? GillaBoardLook.White : GillaBoardLook.Soft)
                }, false);
            string lv = "LV " + row.level.ToString();
            int lw = Measure(text, lv, 1);
            Line(text, 256 - lw, y, lv, new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.Soft }, false);
            string tag = row.you ? "YOU" : (muted ? "MUTED" : (sent ? "SENT" : ""));
            if (tag.Length > 0)
            {
                Line(text, 264, y, tag,
                    new ScreenFont.Style
                    {
                        bold = row.you,
                        scale = 1,
                        slant = 0f,
                        colours = row.you ? GillaBoardLook.White : (muted ? GillaBoardLook.Soft : GillaBoardLook.Dim)
                    }, false);
            }
        }

        if (count > 10)
        {
            Fill(307, 38, 5, 178, GillaBoardLook.Band);
            int n = rows.Count;
            int thumb = 0;
            if (n != 0) thumb = 1780 / n;
            int span = n - 10;
            if (thumb < 9) thumb = 8;
            if (span < 2) span = 1;
            int offset = 0;
            if (span != 0) offset = ((178 - thumb) * top) / span;
            Fill(307, offset + 38, 5, thumb, GillaBoardLook.Dots);
        }

        string foot;
        ScreenFont.Style style;
        if (picking.id == null)
        {
            if (said.Length < 1)
            {
                if (!live)
                {
                    foot = "NOT IN A ROOM";
                }
                else if (count < 11)
                {
                    foot = "MUTE IS JUST FOR YOU. REPORTS COME TO US";
                }
                else
                {
                    foot = "ROLL THE WHEEL TO SEE EVERYONE";
                }
                style = new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.Dim };
            }
            else
            {
                foot = said;
                style = new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.White };
            }
        }
        else
        {
            foot = "WHY REPORT " + Cut(picking.name) + "? KEYS BELOW";
            style = new ScreenFont.Style { scale = 1, slant = 0f, colours = GillaBoardLook.White };
        }
        Line(text, 160, 224, foot, style, true);

        tex.SetPixels32(px);
        tex.Apply(true);
        if (screen == null)
        {
            return;
        }
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }

    private static string Cut(string name)
    {
        if (name.Length > 20)
        {
            return name.Substring(0, 20);
        }
        return name;
    }

    private static int Measure(ScreenFont font, string s, int scale)
    {
        if (font != null)
        {
            return font.Measure(s, scale);
        }
        return PixelFont.Measure(s, scale);
    }

    private void Line(ScreenFont font, int x, int y, string line, ScreenFont.Style style, bool centred = true)
    {
        if (font == null)
        {
            int scale = style.scale;
            if (scale < 2) scale = 1;
            if (centred)
            {
                x -= PixelFont.Measure(line, scale) / 2;
            }
            PixelFont.Draw(px, 320, 240, x, y, line, style.colours[0], scale);
        }
        else if (centred)
        {
            font.DrawCentred(px, 320, 240, x, y, line, style);
        }
        else
        {
            font.Draw(px, 320, 240, x, y, line, style);
        }
    }

    private void Fill(int x, int y, int w, int h, Color32 colour)
    {
        PixelFont.Fill(px, 320, 240, x, y, w, h, colour);
    }

    private void OnDestroy()
    {
        if (tex != null)
        {
            if (Application.isPlaying)
            {
                Destroy(tex);
                return;
            }
            DestroyImmediate(tex);
        }
    }
}
