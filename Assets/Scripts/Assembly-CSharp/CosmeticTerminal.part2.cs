// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CosmeticTerminal : MonoBehaviour
{
    private void Act(string action)
    {
        CosmeticCatalog.Entry entry = CosmeticCatalog.Of(picked);
        CosmeticCatalog.Bundle bundle = CosmeticCatalog.BundleAt(pickedBundle);

        if (0 < pickedTitle)
        {
            NameTitles.Wear(action == "EQUIP" ? pickedTitle : 0);
            Play(equipSound, action == "EQUIP" ? 0.8f : 0.6f);
            Banner(action == "EQUIP" ? "EQUIPPED" : "TAKEN OFF", false);
            return;
        }

        if (action == "TAKE OFF" && entry == null && bundle == null)
        {
            CosmeticSlot slot = Categories[category].slot;
            if (slot == BundleSlot)
            {
                CosmeticCatalog.Bundle b = CosmeticCatalog.BundleAt(pickedBundle);
                List<byte> wearing;
                if (b == null)
                {
                    wearing = new List<byte>();
                }
                else
                {
                    wearing = CosmeticCatalog.IdsOf(b).FindAll(Cosmetics.IsWearing);
                }
                if (wearing.Count != 0)
                {
                    foreach (byte id in wearing)
                    {
                        Cosmetics.TakeOff(CosmeticCatalog.Of(id).slot);
                    }
                    Play(equipSound, 0.6f);
                    Banner("UNEQUIPPED", false);
                    UpdateTryOn();
                    dirty = true;
                    return;
                }
            }
            else if (slot == TitleSlot)
            {
                if (NameTitles.Worn != 0)
                {
                    NameTitles.Wear(0);
                    Play(equipSound, 0.6f);
                    Banner("UNEQUIPPED", false);
                    return;
                }
            }
            else
            {
                if (Cosmetics.In(slot) != 0)
                {
                    Cosmetics.TakeOff(slot);
                    Play(equipSound, 0.6f);
                    Banner("UNEQUIPPED", false);
                    UpdateTryOn();
                    dirty = true;
                    return;
                }
            }
            Banner("NOTHING ON", true);
            Play(errorSound, 0.5f);
            return;
        }

        if (bundle != null && (action == "EQUIP" || action == "TAKE OFF"))
        {
            foreach (byte id in CosmeticCatalog.IdsOf(bundle))
            {
                if (action == "EQUIP")
                {
                    Cosmetics.Wear(id);
                }
                else
                {
                    Cosmetics.TakeOff(CosmeticCatalog.Of(id).slot);
                }
            }
            Play(equipSound, action == "EQUIP" ? 0.8f : 0.6f);
            Banner(action == "EQUIP" ? "EQUIPPED" : "TAKEN OFF", false);
            UpdateTryOn();
            dirty = true;
            return;
        }

        if (action == "BUY")
        {
            List<byte> ids = new List<byte>();
            for (int i = 0; i < cart.Length; i++)
            {
                byte id = cart[i];
                if (id != 0)
                {
                    ids.Add(id);
                }
            }
            int total = CartTotal();
            int hand = GillaToast.NearerHand(transform.position);
            string label;
            bool ok;
            if (bundle == null)
            {
                if (ids.Count == 1)
                {
                    label = CosmeticCatalog.Of(ids[0]).label;
                }
                else
                {
                    label = ids.Count.ToString() + " COSMETICS";
                }
                ok = Coins.Buy(ids, -1);
            }
            else
            {
                label = bundle.label;
                ok = Coins.Buy(bundle);
            }
            if (!ok)
            {
                Banner("NOT ENOUGH COINS", true);
                GillaToast.NotEnough(hand, total);
                Play(errorSound, 0.7f);
                return;
            }
            GillaToast.Bought(hand, label, total);
            if (bundle != null)
            {
                ids = CosmeticCatalog.IdsOf(bundle);
            }
            foreach (byte id in ids)
            {
                Cosmetics.Wear(id);
            }
            Array.Clear(cart, 0, cart.Length);
            Banner("PURCHASED!", false);
            if (buyCoins != null)
            {
                buyCoins.Emit(36);
            }
            Play(buySound, 0.9f);
            Play(coinSound, 0.7f);
            Play(sparkleSound, 0.5f);
            reboot = 0f;
            if (tube != null)
            {
                tube.Flash();
            }
            Refill();
            UpdateTryOn();
            dirty = true;
            return;
        }

        if (action == "EQUIP" && entry != null)
        {
            Cosmetics.Wear(picked);
            Play(equipSound, 0.8f);
            Banner("EQUIPPED", false);
        }
        else if (action == "TAKE OFF" && entry != null)
        {
            Cosmetics.TakeOff(entry.slot);
            Play(equipSound, 0.6f);
            Banner("TAKEN OFF", false);
        }
        UpdateTryOn();
        dirty = true;
    }

    private void Banner(string s, bool bad)
    {
        bannerText = s;
        bannerBad = bad;
        dirty = true;
        banner = 1.6f;
    }

    private int CartTotal()
    {
        CosmeticCatalog.Bundle b = CosmeticCatalog.BundleAt(pickedBundle);
        if (b == null)
        {
            int total = 0;
            for (int i = 0; i < cart.Length; i++)
            {
                byte id = cart[i];
                CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
                if (e != null && !Coins.Owns(id))
                {
                    total += e.price;
                }
            }
            return total;
        }
        return Coins.PriceOf(b);
    }

    private void UpdateTryOn()
    {
        for (int i = 0; i < tryOn.Length; i++)
        {
            byte id = cart[i];
            if (id == 0)
            {
                id = Cosmetics.In((CosmeticSlot)i);
            }
            tryOn[i] = id;
        }
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(picked);
        if (e != null)
        {
            tryOn[(int)e.slot] = picked;
        }
        CosmeticCatalog.Bundle b = CosmeticCatalog.BundleAt(pickedBundle);
        if (b != null)
        {
            foreach (byte id in CosmeticCatalog.IdsOf(b))
            {
                tryOn[(int)CosmeticCatalog.Of(id).slot] = id;
            }
        }
        if (tube != null)
        {
            tube.TryOn(tryOn);
        }
    }

    private void Refill()
    {
        filter = 1;
        shown.Clear();
        shownBundles.Clear();
        shownTitles.Clear();
        CosmeticSlot slot = Categories[category].slot;
        if (slot == BundleSlot)
        {
            for (int i = 0; i < CosmeticCatalog.BundleCount; i++)
            {
                CosmeticCatalog.Bundle b = CosmeticCatalog.BundleAt(i);
                List<byte> ids = CosmeticCatalog.IdsOf(b);
                if (ids.Count != 0)
                {
                    bool owns = Coins.OwnsAll(b);
                    if ((filter != 1 || owns) && (!owns || filter != 2))
                    {
                        byte first = ids.Find(id => Refill_b__135_0(id));
                        if (first == 0)
                        {
                            first = ids[0];
                        }
                        shown.Add(first);
                        shownBundles.Add(i);
                    }
                }
            }
        }
        else if (slot == TitleSlot)
        {
            for (int i = 1; i <= NameTitles.Count; i++)
            {
                int f = NameTitles.MayWear(i) ? 2 : 1;
                if (filter != f)
                {
                    shown.Add(0);
                    shownTitles.Add(i);
                }
            }
        }
        else
        {
            for (int i = 1; i <= CosmeticCatalog.Count; i++)
            {
                CosmeticCatalog.Entry e = CosmeticCatalog.Of(i);
                if (e.slot == slot && string.IsNullOrEmpty(e.wornWith))
                {
                    byte id = (byte)i;
                    bool owns = Coins.Owns(id);
                    if ((filter != 1 || owns) && (!owns || filter != 2) && (!e.retired || owns))
                    {
                        shown.Add(id);
                    }
                }
            }
        }

        int max = MaxScroll();
        scroll = scroll < 0 ? 0 : Mathf.Min(scroll, max);

        if (glideDir != 0)
        {
            GlideTiles(glideDir);
        }
        else
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                (byte id, int bundle, int title) w = Wanted(i);
                Tile t = tiles[i];
                if (t.id != w.id || t.bundle != w.bundle || t.title != w.title)
                {
                    t.title = w.title;
                    t.bundle = w.bundle;
                    Fill(t, w.id);
                    t = tiles[i];
                    t.pop = i * -0.1f;
                    t.at = Vector2.zero;
                }
            }
        }
        glideDir = 0;
        UpdateTryOn();
        dirty = true;
    }

    private (byte, int, int) Wanted(int i)
    {
        int n = i + scroll * Cols;
        byte id = n < shown.Count ? shown[n] : (byte)0;
        int bundle = n < shownBundles.Count ? shownBundles[n] : -1;
        int title = n < shownTitles.Count ? shownTitles[n] : 0;
        return (id, bundle, title);
    }

    private void GlideTiles(int dir)
    {
        Tile[] old = (Tile[])tiles.Clone();
        bool[] used = new bool[old.Length];
        Array.Clear(tiles, 0, tiles.Length);
        bool moved = false;
        for (int i = 0; i < tiles.Length; i++)
        {
            (byte id, int bundle, int title) w = Wanted(i);
            if (w.id != 0 || w.title != 0)
            {
                for (int j = 0; j < old.Length; j++)
                {
                    if (!used[j])
                    {
                        Tile o = old[j];
                        if (o.id == w.id && o.bundle == w.bundle && o.title == w.title)
                        {
                            used[j] = true;
                            tiles[i] = o;
                            moved = true;
                            tiles[i].at += new Vector2((j % 4 - i % 4) * 62, (j / 4 - i / 4) * 108);
                            break;
                        }
                    }
                }
            }
        }

        float dist = moved ? 108f : 216f;
        List<Tile> spare = new List<Tile>();
        for (int j = 0; j < old.Length; j++)
        {
            if (!used[j])
            {
                Tile o = old[j];
                if (o.id == 0 && o.title == 0)
                {
                    spare.Add(o);
                }
                else
                {
                    o.place = new RectInt(GridX + (j % 4) * 62, GridY + (j / 4) * 108, TileW, TileH);
                    o.to = new Vector2(0f, dist * -dir);
                    leaving.Add(o);
                }
            }
        }

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null)
            {
                Tile t;
                if (spare.Count < 1)
                {
                    t = MakeTile(i);
                }
                else
                {
                    t = spare[spare.Count - 1];
                    spare.RemoveAt(spare.Count - 1);
                }
                (byte id, int bundle, int title) w = Wanted(i);
                t.title = w.title;
                t.bundle = w.bundle;
                if (t.id != w.id || t.model != null)
                {
                    Fill(t, w.id);
                }
                t.pop = 1f;
                if (t.id == 0 && t.title == 0)
                {
                    t.at = Vector2.zero;
                }
                else
                {
                    t.at = new Vector2(0f, dist * dir);
                }
                t.to = Vector2.zero;
                tiles[i] = t;
            }
        }

        foreach (Tile s in spare)
        {
            UnityEngine.Object.Destroy(s.root);
        }
    }

    private int MaxScroll()
    {
        return Mathf.Max((shown.Count + 3) / 4, 2) - 2;
    }

    private static bool Has(Tile t)
    {
        if (t.id != 0)
        {
            return true;
        }
        return t.title != 0;
    }

    private Tile MakeTile(int i)
    {
        Tile t = new Tile();
        t.root = new GameObject("Tile" + i.ToString());
        t.root.transform.SetParent(tileRoot, false);
        Vector2 size = Size;
        t.root.transform.localPosition = new Vector3(
            (((i % 4) * 62 + 161 + 0.5f) * (1f / 512f) - 0.5f) * size.x,
            (((i / 4) * 108 + 68 + 0.5f) * -(1f / 256f) + 0.5f) * size.y,
            -0.06f);
        t.root.SetActive(false);
        return t;
    }

    private void Fill(Tile t, byte id)
    {
        t.id = id;
        if (t.model != null)
        {
            UnityEngine.Object.Destroy(t.model);
        }
        t.model = null;
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(id);
        if (e == null)
        {
            return;
        }

        GameObject go;
        if (e.slot == CosmeticSlot.Eyes)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = wire;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetTexture(BaseMap, e.eyeTexture);
            mpb.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
            mr.SetPropertyBlock(mpb);
        }
        else
        {
            if (e.prefab == null)
            {
                return;
            }
            go = CosmeticCatalog.Display(e);
            CosmeticMotion motion = go.GetComponent<CosmeticMotion>();
            if (motion != null)
            {
                motion.enabled = true /* ? */;
            }
        }

        Vector3 offset = Vector3.zero;
        float scale;
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            scale = 1f;
        }
        else
        {
            Bounds b = renderers[0].bounds;
            for (int i = 0; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            Vector3 sz = b.size;
            scale = 0.12f / Mathf.Max(sz.x, sz.y, sz.z, 0.01f);
            offset = b.center - go.transform.position;
        }

        GameObject model = new GameObject("Model");
        Transform mt = model.transform;
        mt.SetParent(t.root.transform, false);
        go.transform.SetParent(mt, false);
        Transform gt = go.transform;
        Quaternion rot;
        if (e.slot == CosmeticSlot.Eyes)
        {
            rot = Quaternion.Inverse(mt.rotation) * Quaternion.AngleAxis(e.eyeTurn, tileRoot.up)
                * Quaternion.FromToRotation(Vector3.up, -tileRoot.forward);
        }
        else
        {
            rot = Quaternion.identity;
        }
        gt.localRotation = rot;
        go.transform.localScale = scale * Vector3.one;
        go.transform.localPosition = -offset * scale;
        Renderer[] all = go.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < all.Length; i++)
        {
            all[i].shadowCastingMode = ShadowCastingMode.Off;
        }
        t.model = mt.gameObject;
    }

    private void ShowTiles(float on, float dt)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            ShowTile(tiles[i], new RectInt(GridX + (i % 4) * 62, GridY + (i / 4) * 108, TileW, TileH), on, dt);
        }
        for (int i = leaving.Count - 1; i >= 0; i--)
        {
            Tile t = leaving[i];
            ShowTile(t, t.place, on, dt);
            if (on < 1f || (t.at - t.to).sqrMagnitude < 1e-10f)
            {
                UnityEngine.Object.Destroy(t.root);
                leaving.RemoveAt(i);
                dirty = true;
            }
        }
    }

    private void ShowTile(Tile t, RectInt place, float on, float dt)
    {
        bool show = false;
        if (1f <= on)
        {
            show = Has(t);
        }
        if (show != t.root.activeSelf)
        {
            t.root.SetActive(show);
        }
        if (!show)
        {
            return;
        }
        if (t.pop < 1f)
        {
            dirty = true;
        }
        float d = (t.at - t.to).sqrMagnitude;
        t.pop = Mathf.Min(t.pop + dt / 1.4f, 1f);
        if (d >= 1e-10f)
        {
            Vector2 to = t.to;
            if (d >= 0.25f)
            {
                to = Vector2.Lerp(t.at, t.to, 1f - Mathf.Exp(dt * -GlideRate));
            }
            t.at = to;
            dirty = true;
        }
        float y = t.at.y + (place.yMin + 40);
        float fade = Mathf.Clamp01(Mathf.Min(y - 28f, 240f - y) / 34f);
        float x = t.at.x;
        Transform tr = t.root.transform;
        Vector2 size = Size;
        tr.localPosition = new Vector3(
            ((x + (place.xMin + place.width / 2) + 0.5f) * (1f / 512f) - 0.5f) * size.x,
            ((y + 0.5f) * -(1f / 256f) + 0.5f) * size.y,
            -0.06f);
        t.root.transform.localScale = Vector3.one * Elastic(t.pop) * fade;
        CosmeticCatalog.Entry e = CosmeticCatalog.Of(t.id);
        if (t.model == null)
        {
            return;
        }
        if (e != null && e.slot == CosmeticSlot.Eyes)
        {
            return;
        }
        t.model.transform.Rotate(0f, dt * 40f, 0f, Space.Self);
    }

    private static float Elastic(float k)
    {
        float r = 0f;
        if (0f < k)
        {
            r = 1f;
            if (k < 1f)
            {
                r = Mathf.Pow(2f, k * -10f) * Mathf.Sin((k * 10f - 0.75f) * 2.0943952f) + 1f;
            }
        }
        return r;
    }

    private void Draw()
    {
        dirty = false;
        GillaBoardLook.Clear(px, W, H);
        if (1f <= awake)
        {
            DrawUi();
        }
        else
        {
            if (title != null)
            {
                ScreenFont.Style style = new ScreenFont.Style();
                style.scale = 3;
                style.slant = 0f;
                style.colours = new Color32[] { Dark, Dark };
                title.DrawCentred(px, W, H, 256, 116, "GILLA-LAB", style);
            }
            if (0f < awake)
            {
                int top = Mathf.RoundToInt(Mathf.SmoothStep(0f, 1f, awake) * 256f);
                DrawUi();
                for (int y = top; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        px[(255 - y) * W + x] = (y & 1) == 0 ? Black : Scan;
                    }
                }
                FillRect(new RectInt(0, Mathf.Min(top, 254), W, 2), Line);
            }
        }
        if (0f <= reboot)
        {
            Glitch(reboot);
        }
        tex.SetPixels32(px);
        tex.Apply(true);
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        screen.GetPropertyBlock(block);
        block.SetTexture(BaseMap, tex);
        screen.SetPropertyBlock(block);
    }
}
