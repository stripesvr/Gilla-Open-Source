// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public partial class ModMenu : MonoBehaviour
{
    private void ShowPage()
    {
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
                r.knobT = (r.item.on != null && r.item.on()) ? 1f : 0f;
                r.track.gameObject.SetActive(r.item.on != null && kind == Kind.Owner);
                r.arrow.gameObject.SetActive(r.item.on == null && r.item.value == null && kind == Kind.Owner);
                r.value.gameObject.SetActive(r.item.value != null && kind == Kind.Owner);
                r.pressT = 0f;
                r.hoverT = 0f;
            }
        }
        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == page ? Accent : DotOff;
        }
    }

    private void Update()
    {
        if (root == null)
        {
            return;
        }
        if (leaving)
        {
            return;
        }
        if (extra && host == null)
        {
            leaving = true;
            if (!extra)
            {
                AllOff();
                NormalSize();
            }
            else
            {
                Hide();
                if (kind == Kind.Punch)
                {
                    MonsterPlayer.MenuPunches = false;
                }
                else if (!OwnersSide && host != null)
                {
                    if (kind == Kind.OP)
                    {
                        host.OpModsOff();
                    }
                    else if (kind == Kind.Size)
                    {
                        NormalSize();
                    }
                }
            }
            Destroy(this);
            return;
        }
        bool offPath = false;
        if (OwnersSide)
        {
            if (wasOff)
            {
                wasOff = false;
            }
        }
        else
        {
            bool off = ModMenuAccess.OffHere(kind);
            if (wasOff == off)
            {
                if (off)
                {
                    offPath = true;
                }
            }
            else
            {
                wasOff = off;
                if (off)
                {
                    if (!extra)
                    {
                        AllOff();
                    }
                    else
                    {
                        Hide();
                    }
                    offPath = true;
                }
            }
        }
        if (offPath)
        {
            if (extra)
            {
                return;
            }
            if (MonsterPlayer.Local != null && !Mathf.Approximately(MonsterPlayer.ApeSize, 1f))
            {
                MonsterPlayer.Local.SetApeSize(1f, false);
            }
            return;
        }
        bool want = stand != null ? Near() : MenuButton();
        if (want)
        {
            if (!shown)
            {
                shown = true;
                closing = false;
                root.gameObject.SetActive(true);
                anim = 0f;
                ShowPage();
                Place3D();
                lastTouched = Touching();
            }
            anim = Mathf.MoveTowards(anim, 1f, Time.unscaledDeltaTime / 0.18f);
            Presses();
            if (swapping)
            {
                SwapBoard();
            }
        }
        else if (shown)
        {
            if (!closing)
            {
                closing = true;
                closeAt = Time.unscaledTime;
            }
            anim = Mathf.MoveTowards(anim, 0f, Time.unscaledDeltaTime / 0.1f);
            if (anim <= 0.02f || Time.unscaledTime - closeAt > 0.3f)
            {
                shown = false;
                closing = false;
                root.gameObject.SetActive(false);
            }
        }
        if (extra)
        {
            return;
        }
        HideToy(shown && stand == null);
        float dt = Time.deltaTime;
        if (MonsterBrain.Frozen)
        {
            FreezeAnimators();
        }
        ApplyGravity();
        if (body == null)
        {
            return;
        }
        MonsterPlayer local = MonsterPlayer.Local;
        bool busy = KillCam.Local != null && KillCam.Local.Busy;
        bool free;
        if (MonsterPlayer.IsMonster && (kind == Kind.Owner || OwnersSide))
        {
            free = Free;
            if (!busy && !free)
            {
                free = !body.isKinematic;
                busy = false;
            }
        }
        else
        {
            free = Free;
            busy = true;
        }
        Transform head = Head();
        Vector3 fwd = head.forward * flySpeed * (fastFly ? 2.5f : 1f);
        bool flyBtn = (fly || fastFly || noclip) && FlyButton();
        if (!busy && local != null && local.OnLegs)
        {
            SetFloating(false);
            if (!flyBtn)
            {
                if (!noclip && !hover)
                {
                    return;
                }
                fwd = Vector3.zero;
            }
            const System.Reflection.BindingFlags bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            typeof(MonsterPlayer).GetField("floatBy", bf)?.SetValue(local, fwd); /* ? */
            typeof(MonsterPlayer).GetField("floatThrough", bf)?.SetValue(local, noclip); /* ? */
            typeof(MonsterPlayer).GetField("floatFrame", bf)?.SetValue(local, Time.frameCount); /* ? */
            return;
        }
        if (free && flyBtn)
        {
            SetFloating(true);
            body.linearVelocity = fwd;
            return;
        }
        bool hold = (noclip || hover) && free;
        SetFloating(hold);
        if (hold)
        {
            body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, Vector3.zero, dt * 25f);
            return;
        }
        if (gravityNow != 3 || !free)
        {
            return;
        }
        if (Physics.Raycast(Head().position, Vector3.up /* ? */, 40f, gt.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
        {
            return;
        }
        body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, Vector3.up /* ? */, dt * 25f);
    }

    private void HideToy(bool hide)
    {
        CosmeticToy toy = null;
        if (hide && (int)GillaToys.Held == 0)
        {
            toy = GillaToys.Toy;
        }
        if (hiddenToy != null)
        {
            bool keep = toy != null && hiddenToy.Length != 0 && hiddenToy[0] != null
                && hiddenToy[0].transform.IsChildOf(toy.transform);
            if (!keep)
            {
                for (int i = 0; i < hiddenToy.Length; i++)
                {
                    Renderer r = hiddenToy[i];
                    if (r != null)
                    {
                        r.forceRenderingOff = false;
                    }
                }
                hiddenToy = null;
            }
        }
        if (toy == null || hiddenToy != null)
        {
            return;
        }
        hiddenToy = toy.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < hiddenToy.Length; i++)
        {
            hiddenToy[i].forceRenderingOff = true;
        }
    }

    private void LateUpdate()
    {
        if (shown)
        {
            Place3D();
            Animate();
        }
        if (cursor != null)
        {
            bool on = shown && !closing;
            if (on != cursor.gameObject.activeSelf)
            {
                cursor.gameObject.SetActive(on);
            }
            if (on)
            {
                cursor.position = Fingertip();
                float s = stand != null ? 1f : RigScale;
                cursor.localScale = Vector3.one * 0.012f * s;
            }
        }
        if (monsterEsp || playerEsp)
        {
            UpdateEsp();
        }
        if (tracers)
        {
            UpdateTracers();
        }
        if (!platforms)
        {
            return;
        }
        if (gt == null)
        {
            return;
        }
        bool leftDown = Grip(XRNode.LeftHand) > 0.6f && !gt.leftHandDisabled;
        Hold(ref platL, leftDown, LeftHand());
        bool rightDown = Grip(XRNode.RightHand) > 0.6f && !gt.rightHandDisabled;
        Vector3 rightPos = RightHand();
        if (!rightDown)
        {
            if (platR != null)
            {
                Destroy(platR.gameObject);
            }
            platR = null;
            return;
        }
        if (platR == null)
        {
            platR = Slab("ModMenu Platform", rightPos - Vector3.up * 0.06f, new Vector3(platformSize, 0.02f, platformSize)).transform;
        }
    }

    private bool Near()
    {
        if (!stand.gameObject.activeInHierarchy)
        {
            return false;
        }
        return (Head().position - stand.position).sqrMagnitude < standReach * standReach;
    }

    private static bool MenuButton()
    {
        bool pressed = false;
        if (GillaDynamite.InHand(0))
        {
            return false;
        }
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out pressed) && pressed)
        {
            return true;
        }
        if (Application.isEditor)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.mKey.isPressed;
        }
        return false;
    }

    private static bool FlyButton()
    {
        float trigger = 0f;
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out trigger) && trigger > 0.5f)
        {
            return true;
        }
        if (Application.isEditor)
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.fKey.isPressed;
        }
        return false;
    }

    private static float Grip(XRNode node)
    {
        float grip = 0f;
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            if (!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out grip))
            {
                grip = 0f;
            }
            return grip;
        }
        return 0f;
    }

    private Transform Head()
    {
        if (KillCam.Local != null && KillCam.Local.head != null)
        {
            return KillCam.Local.head;
        }
        return gt.headCollider.transform;
    }

    private Vector3 LeftHand()
    {
        Transform t;
        if (!gt.leftHandDisabled && gt.leftHandFollower != null)
        {
            t = gt.leftHandFollower;
        }
        else
        {
            t = gt.leftHandTransform;
        }
        return t.position;
    }

    private Transform PalmHand()
    {
        if (NetworkBootstrap.Instance != null && NetworkBootstrap.Instance.leftHand != null)
        {
            return NetworkBootstrap.Instance.leftHand;
        }
        return gt.leftHandTransform;
    }

    private Vector3 RightHand()
    {
        Transform t;
        if (!gt.rightHandDisabled && gt.rightHandFollower != null)
        {
            t = gt.rightHandFollower;
        }
        else
        {
            t = gt.rightHandTransform;
        }
        return t.position;
    }

    private float RigScale
    {
        get
        {
            if (gt != null)
            {
                return Mathf.Clamp(gt.transform.lossyScale.x, 0.05f, 20f);
            }
            return 1f;
        }
    }

    private void Place3D()
    {
        if (stand != null)
        {
            root.SetPositionAndRotation(stand.position, stand.rotation);
            float e0 = closing ? anim * anim : EaseOutBack(anim);
            root.localScale = Vector3.one * (standWidth / W) * Mathf.Max(e0, 0.0001f);
            return;
        }
        if (extra && host != null)
        {
            place = host.place;
            outM = host.outM;
            turnDeg = host.turnDeg;
            sizeM = host.sizeM;
            upM = host.upM;
        }
        Quaternion handRot = gt.leftHandTransform.rotation;
        Vector3 pos = LeftHand();
        Quaternion turn = Quaternion.Euler(0f, 0f, turnDeg);
        float rig = RigScale;
        float scale = rig * sizeM * (opLook ? 1.15f : 1f) / Mathf.Max(W, H);
        Quaternion rot;
        switch (place)
        {
            case Place.Held:
            {
                Transform palm = PalmHand();
                Vector3 right = palm.right;
                Vector3 up = palm.rotation * CosmeticToy.HeadInHand;
                Vector3 fingers = palm.rotation * CosmeticToy.FingersInHand;
                Vector3 palmPos = palm.position;
                Quaternion look = Quaternion.LookRotation(-right, up);
                pos = palmPos + right * (rig * (outM + HeldOut - 0.05f))
                    + up * (rig * (upM - 0.03f) + scale * H * 0.25f)
                    + fingers * (rig * HeldAlong);
                rot = look * turn;
                break;
            }
            case Place.Palm:
                pos += rig * (handRot * new Vector3(outM, 0f, upM));
                rot = handRot * Quaternion.LookRotation(Vector3.left, Vector3.forward) * turn;
                break;
            case Place.PalmFlipped:
                pos += rig * (handRot * new Vector3(outM, 0f, upM));
                rot = handRot * Quaternion.LookRotation(Vector3.left, Vector3.forward) * Quaternion.Euler(0f, 0f, 180f) * turn;
                break;
            case Place.BackOfHand:
                pos += rig * (handRot * new Vector3(-outM, 0f, upM));
                rot = handRot * Quaternion.LookRotation(Vector3.right, Vector3.forward) * turn;
                break;
            default:
            {
                Transform head = Head();
                Vector3 d = pos - head.position;
                if (d.sqrMagnitude < 0.0001f)
                {
                    d = head.forward;
                }
                Quaternion look = Quaternion.LookRotation(d, Vector3.up);
                pos += rig * (look * new Vector3(0.14f, upM + 0.08f, 0f));
                Vector3 d2 = pos - head.position;
                if (d2.sqrMagnitude <= 0.0001f)
                {
                    d2 = head.forward;
                }
                rot = Quaternion.LookRotation(d2, Vector3.up) * turn;
                break;
            }
        }
        root.SetPositionAndRotation(pos, rot);
        float e = closing ? anim * anim : EaseOutBack(anim);
        root.localScale = Vector3.one * scale * Mathf.Max(e, 0.0001f);
        if (extra && host != null)
        {
            root.position = root.position + (root.rotation * Vector3.right) * RowOffset(rig);
        }
    }

    private float RowOffset(float rig)
    {
        int mine = RowSpot((int)kind);
        int hostSpot = RowSpot((int)host.kind);
        float off = rig * 0.015f + (host.WidthM() + WidthM()) * 0.5f;
        host.GetComponents(rowBoards);
        foreach (ModMenu b in rowBoards)
        {
            if (b == this)
            {
                continue;
            }
            if (!b.extra || b.host != host || b.leaving)
            {
                continue;
            }
            int s = RowSpot((int)b.kind);
            if (hostSpot < mine)
            {
                if (s <= hostSpot || mine <= s)
                {
                    continue;
                }
            }
            else if (hostSpot <= s || s <= mine)
            {
                continue;
            }
            off += rig * 0.015f + b.WidthM();
        }
        rowBoards.Clear();
        if (mine <= hostSpot)
        {
            off = -off;
        }
        return off;
    }

    private static int RowSpot(int k)
    {
        switch (k)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 2;
            case 3: return 2;
            case 4: return 2; /* ? */
            default: return 2;
        }
    }

    private float WidthM()
    {
        return RigScale * W * sizeM * (opLook ? 1.15f : 1f) / Mathf.Max(W, H);
    }

    private static float EaseOutBack(float x)
    {
        float t = x - 1f;
        return t * t * 1.70158f + Mathf.Pow(t, 3f) * 2.70158f + 1f;
    }

    private void Animate()
    {
        float dt = Time.unscaledDeltaTime;
        if (extra && host != null && toastUntil < host.toastUntil)
        {
            toast = host.toast;
            toastUntil = host.toastUntil;
        }
        int dir = slideDir;
        slide = Mathf.MoveTowards(slide, 0f, dt / 0.16f);
        Color op = OpColourNow();
        float pulse = Mathf.Sin(Time.timeSinceLevelLoad * 2f * Mathf.PI / 1.6f);
        if (titleRim != null)
        {
            titleRim.effectColor = new Color(op.r, op.g, op.b, 1f);
        }
        if (frameDots != null)
        {
            frameDots.color = new Color(op.r, op.g, op.b, 1f);
        }
        if (wash != null)
        {
            wash.color = new Color(op.r, op.g, op.b, (pulse * 0.5f + 0.5f) * 0.07f + 0.03f);
        }
        float knobStep = dt / 0.12f;
        foreach (Row r in rows)
        {
            if (r.item == null)
            {
                continue;
            }
            float target = (r.item.on != null && r.item.on()) ? 1f : 0f;
            r.knobT = Mathf.MoveTowards(r.knobT, target, knobStep);
            float k = r.knobT * r.knobT * (3f - 2f * r.knobT);
            float c = Mathf.Clamp01(k);
            r.knob.rectTransform.anchoredPosition = new Vector2(c * 42f - 21f, 0f);
            r.track.color = Color.Lerp(SimpleOff, SimpleOn, c);
            r.knob.color = Color.Lerp(KnobOff, Color.white, c);
            if (r.box != null)
            {
                for (int i = 0; i < r.box.Length; i++)
                {
                    Color from;
                    Color to;
                    if (!opLook)
                    {
                        from = SimpleBox;
                        to = SimpleBoxOn;
                    }
                    else
                    {
                        from = Color.Lerp(SimpleBox, new Color(op.r, op.g, op.b, 1f), 0.85f);
                        to = Color.white;
                    }
                    r.box[i].color = Color.Lerp(from, to, c);
                }
            }
            Step(r, Color.Lerp(r.offCol, r.onCol, c), dt);
            r.rt.anchoredPosition = new Vector2(r.basePos.x + slide * slide * dir * 120f, r.basePos.y + 0f);
            r.glow.rectTransform.anchoredPosition = r.rt.anchoredPosition;
            r.glow.color = new Color(Accent.r, Accent.g, Accent.b, r.pressT * 0.35f);
            if (r.item.value != null)
            {
                r.value.text = r.item.value();
            }
        }
        if (prev != null)
        {
            Step(prev, SimpleOff, dt);
        }
        if (next != null)
        {
            Step(next, SimpleOff, dt);
        }
        if (swapKey != null)
        {
            Step(swapKey, SimpleOff, dt);
        }
        float left = toastUntil - Time.unscaledTime;
        if (left > 0f)
        {
            footText.text = toast;
            footText.color = new Color(AccentHot.r, AccentHot.g, AccentHot.b, Mathf.Clamp01(left / 0.4f));
            return;
        }
        string foot;
        if (stand != null)
        {
            foot = "POKE A ROW  ·  TRY IT HERE";
        }
        else if (kind != Kind.Owner)
        {
            if (kind == Kind.Punch)
            {
                foot = "make a fist with the grip, and swing";
            }
            else if (!opLook)
            {
                foot = "hold Y, poke a size";
            }
            else
            {
                foot = "right trigger to fly, a grip for a platform";
            }
        }
        else
        {
            float sdt = Mathf.Max(0.001f, Time.smoothDeltaTime);
            int fps = Mathf.RoundToInt(1f / sdt);
            foot = "HOLD Y  ·  POKE A ROW  ·  " + fps.ToString() + " FPS";
        }
        footText.text = foot;
        footText.color = TextDim;
    }

    private static void Step(Hit h, Color baseCol, float dt)
    {
        h.pressT = Mathf.MoveTowards(h.pressT, 0f, dt / 0.2f);
        h.hoverT = Mathf.MoveTowards(h.hoverT, h.hovered ? 1f : 0f, dt / 0.08f);
        float hv = Mathf.Clamp01(h.hoverT);
        float pr = Mathf.Clamp01(h.pressT * 0.55f);
        Color hot = new Color(baseCol.r * 1.7f + 0.05f, baseCol.g * 1.7f + 0.05f, baseCol.b * 1.7f + 0.05f, baseCol.a * 1.7f + 0f);
        Color c = Color.Lerp(baseCol, hot, hv);
        h.bg.color = Color.Lerp(c, Color.white, pr);
        float s = h.hoverT * 0.035f + 1f + h.pressT * 0.07f;
        h.rt.localScale = new Vector3(s, s, 1f);
    }

    private Vector3 Fingertip()
    {
        if (apeTip != null && apeTip.gameObject.activeInHierarchy && !gt.rightHandDisabled)
        {
            return apeTip.position;
        }
        Transform hand = gt.rightHandTransform;
        Vector3 pos = RightHand();
        Quaternion aim = hand.rotation * AimFromGrip;
        Vector3 dir = aim * Vector3.forward;
        return pos + dir * fingerLength * RigScale;
    }
}
