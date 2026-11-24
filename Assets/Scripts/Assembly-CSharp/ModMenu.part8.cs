// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class ModMenu : MonoBehaviour
{
    static ModMenu()
    {
        FullH = 849f;
        OpLitBand = new Color(0.34f, 0.06f, 0.12f, 1f);
        OpGlow = new Color(0.86f, 0.06f, 0.13f, 1f);
        OpGlow2 = new Color(0.95f, 0.32f, 0.62f, 1f);
        SimpleOff = new Color(0.078431375f, 0.078431375f, 0.078431375f, 1f);
        float on = GillaBoardLook.PickedBand.r / 255f;
        SimpleOn = new Color(on, on, on, 1f);
        SimpleBox = new Color(0.47058824f, 0.47058824f, 0.47058824f, 1f);
        float boxOn = GillaBoardLook.Solid.r / 255f;
        SimpleBoxOn = new Color(boxOn, boxOn, boxOn, 1f);
        SimpleWhere = new Color(0.74509805f, 0.74509805f, 0.74509805f, 1f);
        KnobOff = new Color(0.43137255f, 0.43137255f, 0.43137255f, 1f);
        TextDim = new Color(1f, 1f, 1f, 0.55f);
        DotOff = new Color(1f, 1f, 1f, 0.16f);
        AimFromGrip = Quaternion.Euler(60f, 0f, 0f);
        PlaceNames = new string[] { "HELD", "PALM", "PALM (FLIPPED)", "BACK OF HAND", "BESIDE HAND" };
        SoldSizes = new float[5] /* ? */;
        OwnerSizes = new ValueTuple<string, float>[]
        {
            ("TINY", 0.1f),
            ("MINI", 0.25f),
            ("SMALL", 0.5f),
            ("SHORT", 0.75f),
            ("NORMAL", 1f),
            ("BIG", 1.5f),
            ("GIANT", 2f),
        };
        rowBoards = new List<ModMenu>();
    }

    public ModMenu()
    {
        fingerLength = 0.07f;
        pressReach = 0.022f;
        standWidth = 0.46f;
        standReach = 2.4f;
        pressThrough = 0.03f;
        pressCooldown = 0.22f;
        fullbrightAmbient = new Color(0.85f, 0.85f, 0.85f, 1f);
        spawnMin = 2.5f;
        spawnMax = 5f;
        monsterColour = new Color(1f, 0.18f, 0.12f, 1f);
        W = 640f;
        playerColour = new Color(0.25f, 0.85f, 1f, 1f);
        boostMultiplier = 1f /* ? */;
        boostMaxJump = 1f /* ? */;
        superJump = 1f /* ? */;
        superMaxJump = 1f /* ? */;
        longArmStretch = 0.6f;
        platformSize = 0.32f;
        lowGravity = 0.35f;
        longArmReach = 0.28f;
        H = FullH;
        simpleItems = new List<Item>();
        Accent = new Color(1f, 1f, 1f, 1f);
        AccentHot = new Color(1f, 1f, 1f, 1f);
        pages = new List<Page>();
        rows = new List<Row>();
        dots = new List<Image>();
        slideDir = 1;
        toast = "";
        offColliders = new List<Collider>();
        dropped = new List<GameObject>();
        frozenAnims = new Dictionary<Animator, float>();
        stilled = new List<GillaAtmosphere>();
        markers = new Dictionary<UnityEngine.Object, Marker>();
        lines = new Dictionary<UnityEngine.Object, LineRenderer>();
        gone = new List<UnityEngine.Object>();
        seen = new HashSet<UnityEngine.Object>();
        corners = new Vector3[16];
    }

    private void Line(UnityEngine.Object key, Vector3 from, Vector3 to, Color colour)
    {
        seen.Add(key);
        LineRenderer line;
        if (!lines.TryGetValue(key, out line) || line == null)
        {
            GameObject go = new GameObject("ModMenu Tracer");
            line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = EspMat();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.endColor = colour;
            line.startColor = colour;
            line.widthMultiplier = 0.006f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            lines[key] = line;
        }
        line.SetPosition(0, from);
        line.SetPosition(1, to);
    }

    private void ClearTracers()
    {
        foreach (LineRenderer line in lines.Values)
        {
            if (line != null)
            {
                UnityEngine.Object.Destroy(line.gameObject);
            }
        }
        lines.Clear();
    }

    private void OpModsOff()
    {
        fly = false;
        fastFly = false;
        hover = false;
        if (gt != null)
        {
            speed = false;
            superJumpOn = false;
            ApplyJump();
            SetLongArms(false);
        }
        SetFloating(false);
        if (gravityMode != 0)
        {
            gravityMode = 0;
            ApplyGravity();
        }
        SetPlatforms(false);
    }

    private void AllMods(bool on)
    {
        if (on)
        {
            return;
        }
        hover = false;
        fly = false;
        fastFly = false;
        if (gt != null)
        {
            speed = false;
            superJumpOn = false;
            ApplyJump();
            SetNoclip(false);
            SetLongArms(false);
        }
        SetFloating(false);
        if (gravityMode != 0)
        {
            gravityMode = 0;
            ApplyGravity();
        }
        SetPlatforms(false);
        SetFullbright(false);
        monsterEsp = false;
        playerEsp = false;
        foreach (Marker m in markers.Values)
        {
            DestroyMarker(m);
        }
        markers.Clear();
        tracers = false;
        ClearTracers();
        if (MonsterBrain.Frozen)
        {
            SetFreeze(false);
        }
        MonsterBrain.IgnoreLocal = false;
        MonsterPlayer.OwnerPunches = false;
        MonsterPlayer.MenuPunches = false;
    }

    private void AllOff()
    {
        AllMods(false);
        foreach (GameObject go in dropped)
        {
            if (go != null)
            {
                UnityEngine.Object.Destroy(go);
            }
        }
        dropped.Clear();
        if (root != null)
        {
            GameObject rootGo = root.gameObject;
            shown = false;
            rootGo.SetActive(false);
        }
        if (cursor != null)
        {
            cursor.gameObject.SetActive(false);
        }
        HideToy(false);
    }

    private bool BuildPages_b__147_0() { return fly; }
    private void BuildPages_b__147_1(bool on) { fly = on; }
    private bool BuildPages_b__147_2() { return fastFly; }
    private void BuildPages_b__147_3(bool on) { fastFly = on; }
    private bool BuildPages_b__147_4() { return speed; }
    private void BuildPages_b__147_5(bool on) { speed = on; ApplyJump(); }
    private bool BuildPages_b__147_6() { return superJumpOn; }
    private void BuildPages_b__147_7(bool on) { superJumpOn = on; ApplyJump(); }
    private bool BuildPages_b__147_8() { return longArms; }
    private bool BuildPages_b__147_9() { return hover; }
    private void BuildPages_b__147_10(bool on) { hover = on; }
    private bool BuildPages_b__147_11() { return noclip; }
    private bool BuildPages_b__147_12() { return gravityMode == 1; }

    private void BuildPages_b__147_13(bool on)
    {
        int mode = on ? 1 : 0;
        if (gravityMode == mode) return;
        gravityMode = mode;
        ApplyGravity();
    }

    private bool BuildPages_b__147_14() { return gravityMode == 2; }

    private void BuildPages_b__147_15(bool on)
    {
        int mode = on ? 2 : 0;
        if (gravityMode == mode) return;
        gravityMode = mode;
        ApplyGravity();
    }

    private bool BuildPages_b__147_16() { return gravityMode == 3; }

    private void BuildPages_b__147_17(bool on)
    {
        int mode = on ? 3 : 0;
        if (gravityMode == mode) return;
        gravityMode = mode;
        ApplyGravity();
    }

    private string BuildPages_b__147_18() { return checkpoint.HasValue ? "SAVED" : ""; }
    private bool BuildPages_b__147_19() { return platforms; }

    private string BuildPages_b__147_20()
    {
        int count = dropped.Count;
        return count > 0 ? count.ToString() : "";
    }

    private void BuildPages_b__147_21() { StartCoroutine(Fireworks()); }

    private void BuildPages_b__147_22()
    {
        GillaSpawnables s = GillaSpawnables.Get();
        SpawnItem(s != null ? s.dynamite : null, "DYNAMITE - TOP BUTTON LIGHTS IT");
    }

    private void BuildPages_b__147_23()
    {
        GillaSpawnables s = GillaSpawnables.Get();
        SpawnItem(s != null ? s.egg : null, "EGG - THROW IT");
    }

    private void BuildPages_b__147_24()
    {
        GillaSpawnables s = GillaSpawnables.Get();
        SpawnItem(s != null ? s.bearTrap : null, "BEAR TRAP - SETS WHEN IT LIES FLAT");
    }

    private void BuildPages_b__147_25()
    {
        GillaSpawnables s = GillaSpawnables.Get();
        SpawnItem(s != null ? s.pickaxe : null, "PICKAXE");
    }

    private bool BuildPages_b__147_26() { return fullbright; }
    private bool BuildPages_b__147_30() { return monsterEsp; }

    private void BuildPages_b__147_31(bool on)
    {
        monsterEsp = on;
        if (on) return;
        gone.Clear();
        foreach (KeyValuePair<UnityEngine.Object, Marker> kv in markers)
        {
            UnityEngine.Object key = kv.Key;
            if (key == null || key is MonsterBrain)
            {
                gone.Add(key);
            }
        }
        foreach (UnityEngine.Object key in gone)
        {
            DestroyMarker(markers[key]);
            markers.Remove(key);
        }
    }

    private void BuildPages_b__147_32() { BecomeMonster(0, "THE APE"); }
    private bool BuildPages_b__147_33() { return playerEsp; }

    private void BuildPages_b__147_34(bool on)
    {
        playerEsp = on;
        if (on) return;
        gone.Clear();
        foreach (KeyValuePair<UnityEngine.Object, Marker> kv in markers)
        {
            UnityEngine.Object key = kv.Key;
            if (key == null || !(key is MonsterBrain))
            {
                gone.Add(key);
            }
        }
        foreach (UnityEngine.Object key in gone)
        {
            DestroyMarker(markers[key]);
            markers.Remove(key);
        }
    }

    private bool BuildPages_b__147_35() { return tracers; }

    private void BuildPages_b__147_36(bool on)
    {
        tracers = on;
        if (on) return;
        ClearTracers();
    }

    private void Give(int coins, int bolts, int xp, int shown, string unit)
    {
        string msg;
        if (!Application.isEditor && !ModMenuAccess.Granted)
        {
            msg = "YOUR ID ISN'T IN ModMenuPlayers";
        }
        else
        {
            Coins.Add(coins);
            Coins.AddBoltsHere(bolts);
            GillaLevel.Add(xp);
            GillaCloud.OwnerGave(coins, bolts, xp);
            msg = "+" + shown.ToString("N0") + unit;
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void BuildPages_b__147_40() { Give(1000, 0, 0, 1000, " COINS"); }
    private void BuildPages_b__147_42() { Give(10000, 0, 0, 10000, " COINS"); }
    private void BuildPages_b__147_44() { Give(0, 100, 0, 100, " BOLTS"); }
    private void BuildPages_b__147_46() { Give(0, 1000, 0, 1000, " BOLTS"); }
    private void BuildPages_b__147_48() { Give(0, 0, 1000, 1000, " XP"); }
    private void BuildPages_b__147_50() { Give(0, 0, 10000, 10000, " XP"); }

    private void BuildPages_b__147_52() { Join(null, ""); }
    private void BuildPages_b__147_54() { Join(NetworkBootstrap.Lobby.Regular, ""); }
    private void BuildPages_b__147_55() { Join(NetworkBootstrap.Lobby.Monster, ""); }
    private void BuildPages_b__147_56() { Join(NetworkBootstrap.Lobby.Splash, ""); }

    private string BuildPages_b__147_57() { return PlaceNames[(int)place]; }

    private void BuildPages_b__147_58()
    {
        place = (Place)(((int)place + 1) % PlaceNames.Length);
        SavePlacement();
    }

    private string BuildPages_b__147_59()
    {
        return Mathf.RoundToInt(sizeM * 100f).ToString() + " CM";
    }

    private void BuildPages_b__147_60()
    {
        float v = sizeM + 0.02f;
        sizeM = v <= 0.3401f ? v : 0.18f;
        SavePlacement();
    }

    private string BuildPages_b__147_61()
    {
        int cm = Mathf.RoundToInt(upM * 100f);
        return (upM >= 0f ? "+" : "") + cm.ToString() + " CM";
    }

    private void BuildPages_b__147_62()
    {
        float v = upM + 0.02f;
        upM = v <= 0.1201f ? v : -0.08f;
        SavePlacement();
    }

    private string BuildPages_b__147_63()
    {
        int cm = Mathf.RoundToInt(outM * 100f);
        return (outM >= 0f ? "+" : "") + cm.ToString() + " CM";
    }

    private void BuildPages_b__147_64()
    {
        float v = outM + 0.01f;
        outM = v <= 0.1201f ? v : 0.02f;
        SavePlacement();
    }

    private string BuildPages_b__147_65()
    {
        return Mathf.RoundToInt(turnDeg).ToString() + "°";
    }

    private void BuildPages_b__147_66()
    {
        turnDeg = Mathf.Clamp(Mathf.Repeat(turnDeg + 15f, 360f), 0f, 360f);
        SavePlacement();
    }

    private string BuildPages_b__147_67()
    {
        return flySpeed.ToString("0") + " M/S";
    }

    private void BuildPages_b__147_68()
    {
        float v = flySpeed + 2f;
        flySpeed = v <= 25.0001f ? v : 5f;
        SavePlacement();
    }

    private void BuildPages_b__147_69()
    {
        AllMods(false);
        toast = "EVERYTHING OFF";
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private bool OpItems_b__153_0() { return fly; }
    private void OpItems_b__153_1(bool on) { fly = on; }
    private bool OpItems_b__153_2() { return fastFly; }
    private void OpItems_b__153_3(bool on) { fastFly = on; }
    private bool OpItems_b__153_4() { return speed; }
    private void OpItems_b__153_5(bool on) { speed = on; ApplyJump(); }
    private bool OpItems_b__153_6() { return superJumpOn; }
    private void OpItems_b__153_7(bool on) { superJumpOn = on; ApplyJump(); }
    private bool OpItems_b__153_8() { return platforms; }
    private bool OpItems_b__153_9() { return longArms; }
    private bool OpItems_b__153_10() { return gravityMode == 1; }

    private void OpItems_b__153_11(bool on)
    {
        int mode = on ? 1 : 0;
        if (gravityMode == mode) return;
        gravityMode = mode;
        ApplyGravity();
    }

    private bool OpItems_b__153_12() { return gravityMode == 2; }

    private void OpItems_b__153_13(bool on)
    {
        int mode = on ? 2 : 0;
        if (gravityMode == mode) return;
        gravityMode = mode;
        ApplyGravity();
    }

    private bool OpItems_b__153_14() { return hover; }
    private void OpItems_b__153_15(bool on) { hover = on; }
    private bool OpItems_b__153_16() { return gravityMode == 3; }

    private void OpItems_b__153_17(bool on)
    {
        int mode = on ? 3 : 0;
        if (gravityMode == mode) return;
        gravityMode = mode;
        ApplyGravity();
    }

    private void BuildBoard_b__165_0()
    {
        int count = pages.Count;
        slideDir = -1;
        slide = 1f;
        page = (page - 1 + count) % count;
        if (pages.Count == 0)
        {
            return;
        }
        page = page < 0 ? 0 : Mathf.Min(page, pages.Count - 1);
        Page p = pages[page];
        if (pageText != null)
        {
            pageText.text = p.title;
        }
        if (countText != null)
        {
            countText.text = (page + 1).ToString() + " / " + pages.Count.ToString();
        }
        List<Item> items = kind == Kind.Owner ? p.items : simpleItems;
        for (int i = 0; i < rows.Count; i++)
        {
            Row r = rows[i];
            r.item = i < items.Count ? items[i] : null;
            Item item = r.item;
            r.bg.gameObject.SetActive(item != null);
            r.glow.gameObject.SetActive(item != null);
            if (item != null)
            {
                r.label.text = r.item.label;
                r.knobT = r.item.on != null && r.item.on() ? 1f : 0f;
                r.track.gameObject.SetActive(r.item.on != null && kind == Kind.Owner);
                r.arrow.gameObject.SetActive(r.item.on == null && r.item.value == null && kind == Kind.Owner);
                r.value.gameObject.SetActive(r.item.value != null && kind == Kind.Owner);
                r.pressT = 0f;
            }
        }
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == page ? Accent : DotOff;
        }
    }
}
