using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public partial class ModMenu : MonoBehaviour
{
    private void BuildPages()
    {
        if (!extra)
        {
            switch (kind)
            {
                case Kind.Size:
                    SizePage();
                    return;
                case Kind.OP:
                    OpPage();
                    return;
                case Kind.SizeAndOP:
                    OpPage();
                    SizePage();
                    return;
                case Kind.Punch:
                    pages.Add(PunchItems());
                    return;
            }

            Page p = AddPage("MOVEMENT");
            Toggle(p, "FLY  (RIGHT TRIGGER)", BuildPages_b__147_0, BuildPages_b__147_1);
            Toggle(p, "FAST FLY", BuildPages_b__147_2, BuildPages_b__147_3);
            Toggle(p, "SPEED BOOST", BuildPages_b__147_4, BuildPages_b__147_5);
            Toggle(p, "SUPER JUMP", BuildPages_b__147_6, BuildPages_b__147_7);
            Toggle(p, "LONG ARMS", BuildPages_b__147_8, SetLongArms);
            Toggle(p, "HOVER", BuildPages_b__147_9, BuildPages_b__147_10);
            Toggle(p, "NOCLIP", BuildPages_b__147_11, SetNoclip);
            SizePage();

            p = AddPage("TRAVEL");
            Toggle(p, "LOW GRAVITY", BuildPages_b__147_12, BuildPages_b__147_13);
            Toggle(p, "NO GRAVITY", BuildPages_b__147_14, BuildPages_b__147_15);
            Toggle(p, "UPSIDE DOWN GRAVITY", BuildPages_b__147_16, BuildPages_b__147_17);
            Action(p, "TELEPORT FORWARD", TeleportForward, null);
            Action(p, "SET CHECKPOINT", SetCheckpoint, BuildPages_b__147_18);
            Action(p, "GO TO CHECKPOINT", GoToCheckpoint, null);
            Action(p, "TP TO SPAWN", TeleportToSpawn, null);
            Action(p, "TP TO PLAYER", TeleportToPlayer, null);

            p = AddPage("FUN");
            Toggle(p, "PLATFORMS  (GRIPS)", BuildPages_b__147_19, SetPlatforms);
            Action(p, "DROP PLATFORM", DropPlatform, null);
            Action(p, "CLEAR PLATFORMS", ClearPlatforms, BuildPages_b__147_20);
            Action(p, "CONFETTI", Confetti, null);
            Action(p, "FIREWORKS", BuildPages_b__147_21, null);
            Held(p, "SPAWN DYNAMITE", BuildPages_b__147_22);
            Held(p, "SPAWN EGG", BuildPages_b__147_23);
            Held(p, "SPAWN BEAR TRAP", BuildPages_b__147_24);
            Held(p, "SPAWN PICKAXE", BuildPages_b__147_25);
            Action(p, "OPEN ALL DOORS", OpenAllDoors, null);
            Toggle(p, "FULLBRIGHT", BuildPages_b__147_26, SetFullbright);

            p = AddPage("MONSTERS");
            Action(p, "KILL ALL", KillAllMonsters, null);
            Toggle(p, "FREEZE THEM", BuildPages_b__147_27, SetFreeze);
            Toggle(p, "IGNORE ME", BuildPages_b__147_28, BuildPages_b__147_29);
            Toggle(p, "MONSTER ESP", BuildPages_b__147_30, BuildPages_b__147_31);
            MonsterCatalog catalog = MonsterCatalog.Get();
            if (catalog != null)
            {
                foreach (MonsterCatalog.Entry entry in catalog.monsters)
                {
                    if (entry == null || entry.prefab == null) continue;
                    MonsterCatalog.Entry captured = entry;
                    Action(p, "SPAWN " + Label(entry), () => BuildPages_b__147_70(captured), null);
                }
            }

            p = AddPage("PLAY AS");
            if (catalog != null)
            {
                for (int i = 0; i < catalog.monsters.Length; i++)
                {
                    MonsterCatalog.Entry e = catalog.monsters[i];
                    if (e != null)
                    {
                        int index = i + 1;
                        Action(p, "BE " + Label(e), () => BuildPages_b__147_71(index, e), null);
                    }
                }
            }
            Action(p, "BACK TO APE", BuildPages_b__147_32, null);

            p = AddPage("PLAYERS");
            Toggle(p, "PLAYER ESP", BuildPages_b__147_33, BuildPages_b__147_34);
            Toggle(p, "TRACERS", BuildPages_b__147_35, BuildPages_b__147_36);
            Toggle(p, "MONSTER PUNCHES", BuildPages_b__147_37, BuildPages_b__147_38);
            Action(p, "TP TO PLAYER", TeleportToPlayer, null);
            Action(p, "HEAL MY ARMS", HealArms, null);
            Action(p, "SPAWN DUMMY", SpawnDummy, BuildPages_b__147_39);
            Action(p, "CLEAR MY DUMMIES", ClearDummies, null);

            p = AddPage("WALLET");
            Action(p, "+1,000 COINS", BuildPages_b__147_40, BuildPages_b__147_41);
            Action(p, "+10,000 COINS", BuildPages_b__147_42, BuildPages_b__147_43);
            Action(p, "+100 BOLTS", BuildPages_b__147_44, BuildPages_b__147_45);
            Action(p, "+1,000 BOLTS", BuildPages_b__147_46, BuildPages_b__147_47);
            Action(p, "+1,000 XP", BuildPages_b__147_48, BuildPages_b__147_49);
            Action(p, "+10,000 XP", BuildPages_b__147_50, BuildPages_b__147_51);

            p = AddPage("ROOM");
            Action(p, "JOIN PUBLIC ROOM", BuildPages_b__147_52, BuildPages_b__147_53);
            Action(p, "REJOIN THIS ROOM", Rejoin, null);
            Action(p, "REGULAR LOBBY", BuildPages_b__147_54, null);
            Action(p, "MONSTER LOBBY", BuildPages_b__147_55, null);
            Action(p, "SPLASH LOBBY", BuildPages_b__147_56, null);

            p = AddPage("MENU");
            Setting(p, "SITS", BuildPages_b__147_57, BuildPages_b__147_58);
            Setting(p, "SIZE", BuildPages_b__147_59, BuildPages_b__147_60);
            Setting(p, "UP / DOWN", BuildPages_b__147_61, BuildPages_b__147_62);
            Setting(p, "OFF THE PALM", BuildPages_b__147_63, BuildPages_b__147_64);
            Setting(p, "TURN", BuildPages_b__147_65, BuildPages_b__147_66);
            Setting(p, "FLY SPEED", BuildPages_b__147_67, BuildPages_b__147_68);
            Action(p, "TURN EVERYTHING OFF", BuildPages_b__147_69, null);

            AdminPages();   // Redux: players, ban, grant and debug pages (ModMenu.Admin.cs)

            List<Page> split = new List<Page>();
            foreach (Page src in pages)
            {
                int n = Mathf.CeilToInt(src.items.Count / 7f);
                if (n < 2) n = 1;
                int k = 0;
                do
                {
                    Page part = new Page();
                    string title = src.title;
                    if (n > 1)
                        title = string.Format("{0} {1}/{2}", title, k + 1, n);
                    part.title = title;
                    int index = k * 7;
                    k++;
                    int end = index + 7;
                    while (index < Mathf.Min(end, src.items.Count))
                    {
                        part.items.Add(src.items[index]);
                        index++;
                    }
                    split.Add(part);
                } while (k != n);
            }
            pages.Clear();
            pages.AddRange(split);
        }
        else
        {
            Page p;
            if (kind == Kind.OP)
                p = host.OpItems();
            else if (kind == Kind.Punch)
                p = host.PunchItems();
            else
                p = host.SizeItems(true);
            pages.Add(p);
        }
    }

    private void SizePage()
    {
        pages.Add(SizeItems(kind != Kind.Owner));
    }

    private Page SizeItems(bool sold)
    {
        Page page = new Page();
        page.title = "SIZE";
        List<ValueTuple<string, float>> sizes = new List<ValueTuple<string, float>>();
        if (!sold)
        {
            sizes.AddRange(OwnerSizes);
        }
        else
        {
            foreach (float s in SoldSizes)
            {
                float f = s;
                sizes.Add(new ValueTuple<string, float>(f.ToString("0.##", CultureInfo.InvariantCulture) + "x Size", f));
            }
        }
        foreach (ValueTuple<string, float> t in sizes)
        {
            float size = t.Item2;
            string label = t.Item1;
            Item item = new Item();
            string text = label;
            if (!sold)
                text = text + "   " + size.ToString("0.##") + "X";
            item.label = text;
            item.on = () => SizeItems_b__0(size);
            item.press = () => SizeItems_b__1(size, label, sold);
            page.items.Add(item);
        }
        return page;
    }

    private void OpPage()
    {
        pages.Add(OpItems());
    }

    private Page OpItems()
    {
        Page p = new Page();
        p.title = "OP MODS";
        Toggle(p, "Fly", OpItems_b__153_0, OpItems_b__153_1);
        Toggle(p, "Fast Fly", OpItems_b__153_2, OpItems_b__153_3);
        Toggle(p, "Speed Boost", OpItems_b__153_4, OpItems_b__153_5);
        Toggle(p, "Super Jump", OpItems_b__153_6, OpItems_b__153_7);
        Toggle(p, "Platforms", OpItems_b__153_8, SetPlatforms);
        Toggle(p, "Long Arms", OpItems_b__153_9, SetLongArms);
        Toggle(p, "Low Gravity", OpItems_b__153_10, OpItems_b__153_11);
        Toggle(p, "No Gravity", OpItems_b__153_12, OpItems_b__153_13);
        Toggle(p, "Hover", OpItems_b__153_14, OpItems_b__153_15);
        Toggle(p, "Upside Down Gravity", OpItems_b__153_16, OpItems_b__153_17);
        return p;
    }

    private Page PunchItems()
    {
        Page p = new Page();
        p.title = "PUNCH";
        Toggle(p, "Punching", PunchItems_b__154_0, PunchItems_b__154_1);
        return p;
    }

    private static string Label(MonsterCatalog.Entry e)
    {
        string s = string.IsNullOrEmpty(e.label) ? e.name : e.label;
        return s.ToUpper();
    }

    private static string Cm(float m)
    {
        int cm = Mathf.RoundToInt(m * 100f);
        string sign = m >= 0f ? "+" : "";
        return sign + cm.ToString() + " CM";
    }

    private static float Cycle(float v, float min, float max, float step)
    {
        if (v + step <= max + 0.0001f)
            return v + step;
        return min;
    }

    private Page AddPage(string title)
    {
        Page p = new Page();
        p.title = title;
        pages.Add(p);
        return p;
    }

    private void Toggle(Page p, string label, Func<bool> on, Action<bool> set)
    {
        Item item = new Item();
        item.label = label;
        item.on = on;
        item.press = () => Toggle_b__0(label, on, set) /* ? */;
        p.items.Add(item);
    }

    private void Action(Page p, string label, Action press, Func<string> value = null)
    {
        Item item = new Item();
        item.label = label;
        item.press = press;
        item.value = value;
        p.items.Add(item);
    }

    private void Held(Page p, string label, Action press)
    {
        Item item = new Item();
        item.label = label;
        item.press = press;
        item.repeats = true;
        p.items.Add(item);
    }

    private static void Setting(Page p, string label, Func<string> value, Action press)
    {
        Item item = new Item();
        item.label = label;
        item.value = value;
        item.press = press;
        p.items.Add(item);
    }

    private static string Clean(string label)
    {
        int i = label.IndexOf("  (");
        if (i > 0)
            return label.Substring(0, i);
        return label;
    }

    private void Toast(string text)
    {
        toast = text;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void BuildBoard()
    {
        if (rounded == null) rounded = MakeRounded(64, 22f, 0f, false);
        if (circle == null) circle = MakeRounded(64, 31.5f, 0f, false);
        if (soft == null) soft = MakeRounded(128, 30f, 26f, false);
        if (square == null) square = MakeRounded(32, 3f, 0f, true);
        if (kind != Kind.Owner)
        {
            BuildSimpleBoard();
            return;
        }
        MakeCanvas();
        backSprite = MakeSimpleBack(Mathf.RoundToInt(W * 0.25f), Mathf.RoundToInt(H * 0.25f), true);
        Img("Frame", backSprite, Color.white, Vector2.zero, new Vector2(W, H), 0, null);

        float y = H * 0.5f - 16.5f;
        Text title = Txt("Title", "ADMIN MENU", new Vector2(0f, y - 52f + 4f), new Vector2(W - 66f, 80f), 64, Color.white, (TextAnchor)4, 0, null);
        title.font = titleFont;
        y -= 104f;
        float tabY = y - 39f;
        pageText = Txt("Page", "", new Vector2(0f, tabY + 9f), new Vector2(W - 240f, 40f), 36, Color.white, (TextAnchor)4, 0, null);
        pageText.font = bodyFont;
        countText = Txt("Count", "", new Vector2(0f, tabY - 21f), new Vector2(200f, 24f), 20, SimpleWhere, (TextAnchor)4, 0, null);
        countText.font = bodyFont;
        prev = MakeHit("Prev", new Vector2((22f - W * 0.5f) + 50f, tabY), new Vector2(84f, 60f), "<", BuildBoard_b__165_0);
        next = MakeHit("Next", new Vector2(W * 0.5f - 22f - 50f, tabY), new Vector2(84f, 60f), ">", BuildBoard_b__165_1);

        int count = Mathf.Max(pages.Count, 1);
        float step = Mathf.Min((W - 88f) / count, 26f);
        y -= 78f;
        for (int i = 0; i < pages.Count; i++)
        {
            Image dot = Img("Dot" + i.ToString(), square, DotOff,
                new Vector2(step * (i + (pages.Count - 1) * -0.5f), y - 15f), new Vector2(12f, 12f), 0, null);
            dots.Add(dot);
        }
        for (int i = 0; i < 7; i++)
        {
            rows.Add(MakeRow(i, new Vector2(0f, y - 30f - 39f + i * -78f)));
        }
        footText = Txt("Foot", "", new Vector2(0f, H * -0.5f + 16.5f + 29f), new Vector2(W - 66f, 30f), 24, TextDim, (TextAnchor)4, 0, null);
        footText.font = bodyFont;
        if (cursor == null)
            MakeCursor();
        PointLetters(titleFont);
        PointLetters(bodyFont);
    }

    private void MakeCanvas()
    {
        GameObject go = new GameObject("ModMenu");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;
        root = go.transform;
        RectTransform rt = (RectTransform)root;
        rt.sizeDelta = new Vector2(W, H);
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private void PointLetters(Font f)
    {
        if (f == null) return;
        if (f != titleFont && f != bodyFont) return;
        if (f.material == null) return;
        if (f.material.mainTexture == null) return;
        f.material.mainTexture.filterMode = FilterMode.Point;
    }
}
