// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using GorillaLocomotion;

public partial class ModMenu : MonoBehaviour
{
    private Hit Touching()
    {
        Vector3 local = root.InverseTransformPoint(Fingertip());
        float s = Mathf.Max(root.localScale.x, 1e-05f);
        float rig = stand != null ? 1f : RigScale;
        float reach = -(pressReach * rig) / s;
        float through = (pressThrough * rig) / s;
        bool inDepth = local.z < through && local.z > reach;
        Hit best = null;
        foreach (Hit h in Pressable())
        {
            h.hovered = false;
            if (Mathf.Abs(local.x - h.basePos.x) >= h.size.x * 0.5f + 4f
                || Mathf.Abs(local.y - h.basePos.y) >= h.size.y * 0.5f + 4f)
            {
                continue;
            }
            if (inDepth)
            {
                best = h;
            }
        }
        if (best != null)
        {
            best.hovered = true;
        }
        return best;
    }

    private void Presses()
    {
        Hit h = Touching();
        if (h != null)
        {
            if (h == lastTouched)
            {
                Repeats(h);
            }
            else if (nextPress <= Time.unscaledTime)
            {
                Press(h, true);
                nextRepeat = Time.unscaledTime + 0.3f;
            }
            else if (h == lastTouched)
            {
                Repeats(h);
            }
        }
        lastTouched = h;
        if (!Application.isEditor)
        {
            return;
        }
        Mouse mouse = Mouse.current;
        Camera cam = Camera.main;
        if (cam != null)
        {
            if (mouse.leftButton.isPressed)
            {
                Hit c = clicked;
                if (c != null)
                {
                    Row row = c as Row;
                    if (row != null && row.item != null && row.item.repeats && nextRepeat <= Time.unscaledTime)
                    {
                        c.pressT = 1f;
                        nextRepeat = Time.unscaledTime + 0.07f;
                        c.act();
                    }
                    return;
                }
                if (!mouse.leftButton.wasPressedThisFrame)
                {
                    return;
                }
                Vector2 mp = mouse.position.ReadValue();
                Ray ray = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
                Plane plane = new Plane(-root.forward, root.position);
                float enter;
                if (!plane.Raycast(ray, out enter))
                {
                    return;
                }
                Vector3 local = root.InverseTransformPoint(ray.origin + ray.direction * enter);
                foreach (Hit p in Pressable())
                {
                    if (Mathf.Abs(local.x - p.basePos.x) < p.size.x * 0.5f
                        && Mathf.Abs(local.y - p.basePos.y) < p.size.y * 0.5f)
                    {
                        Press(p, false);
                        clicked = p;
                        nextRepeat = Time.unscaledTime + 0.3f;
                        return;
                    }
                }
                return;
            }
        }
        clicked = null;
    }

    private void Repeats(Hit h)
    {
        if (h != null)
        {
            Row row = h as Row;
            if (row != null && row.item != null && row.item.repeats && nextRepeat <= Time.unscaledTime)
            {
                h.pressT = 1f;
                nextRepeat = Time.unscaledTime + 0.07f;
                h.act();
            }
        }
    }

    private void Press(Hit h, bool byHand)
    {
        nextPress = Time.unscaledTime + pressCooldown;
        h.pressT = 1f;
        if (click != null)
        {
            sound.PlayOneShot(click, 0.6f);
        }
        if (byHand)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid)
            {
                device.SendHapticImpulse(0, 0.35f, 0.04f);
            }
        }
        h.act();
        Row row = h as Row;
        if (row != null && row.item != null)
        {
            string state = row.item.on == null ? "" : (row.item.on() ? " on" : " off");
            Debug.Log("[ModMenu] " + row.item.label + state);
        }
    }

    private bool Free
    {
        get
        {
            if (body != null && !body.isKinematic && !MonsterPlayer.IsMonster)
            {
                if (KillCam.Local == null)
                {
                    return true;
                }
                return !KillCam.Local.Busy;
            }
            return false;
        }
    }

    private bool Owners
    {
        get
        {
            if (kind != Kind.Owner)
            {
                return OwnersSide;
            }
            return true;
        }
    }

    private static void SetMonsterField(MonsterPlayer monster, string field, object value)
    {
        System.Reflection.FieldInfo f = typeof(MonsterPlayer).GetField(field,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (f != null)
        {
            f.SetValue(monster, value);
        }
    }

    private void Mods(float dt)
    {
        if (MonsterBrain.Frozen)
        {
            FreezeAnimators();
        }
        ApplyGravity();
        if (body == null)
        {
            return;
        }
        MonsterPlayer monster = MonsterPlayer.Local;
        bool skipMonster = KillCam.Local != null && KillCam.Local.Busy;
        bool free;
        if (MonsterPlayer.IsMonster && (kind == Kind.Owner || OwnersSide))
        {
            free = Free;
            if (!skipMonster && !free)
            {
                skipMonster = false;
                free = !body.isKinematic;
            }
        }
        else
        {
            free = Free;
            skipMonster = true;
        }
        Vector3 fwd = Head().forward * flySpeed * (fastFly ? 2.5f : 1f);
        bool flyBtn = (fly || fastFly || noclip) && FlyButton();
        if (!skipMonster && monster != null && monster.OnLegs)
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
            SetMonsterField(monster, "floatBy", fwd); /* ? */
            SetMonsterField(monster, "floatThrough", noclip); /* ? */
            SetMonsterField(monster, "floatFrame", Time.frameCount); /* ? */
            return;
        }
        if (free && flyBtn)
        {
            SetFloating(true);
            body.linearVelocity = fwd;
            return;
        }
        bool hold = noclip || hover;
        SetFloating(hold && free);
        if (hold && free)
        {
            body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, Vector3.zero, dt * 25f);
            return;
        }
        if (gravityNow != 3 || !free)
        {
            return;
        }
        Vector3 pos = Head().position;
        if (Physics.Raycast(pos, Vector3.up, 40f, gt.locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
        {
            return;
        }
        Vector3 vel = body.linearVelocity;
        body.linearVelocity = Vector3.MoveTowards(vel, Vector3.zero /* ? */, dt * 25f);
    }

    private void RigChanging()
    {
        if (gt != null && !extra)
        {
            hadLongArms = longArms;
            hadNoclip = noclip;
            SetNoclip(false);
            SetLongArms(false);
            if (speed || superJumpOn)
            {
                gt.maxJumpSpeed = baseMaxJump;
                gt.jumpMultiplier = baseJump;
            }
        }
    }

    private void RigChanged()
    {
        if (gt != null && !extra)
        {
            baseJump = gt.jumpMultiplier;
            baseMaxJump = gt.maxJumpSpeed;
            ApplyJump();
            SetLongArms(hadLongArms);
            SetNoclip(hadNoclip);
        }
    }

    private void SetFloating(bool on)
    {
        if (floating != on && body != null)
        {
            floating = on;
            if (on)
            {
                savedUseGravity = body.useGravity;
                body.useGravity = false;
            }
            else
            {
                body.useGravity = savedUseGravity;
            }
        }
    }

    private void ApplyJump()
    {
        if (gt != null)
        {
            float m1 = speed ? boostMultiplier : 1f;
            float m2 = superJumpOn ? superJump : 1f;
            float j1 = speed ? boostMaxJump : 1f;
            float j2 = superJumpOn ? superMaxJump : 1f;
            gt.maxJumpSpeed = j1 * j2 * baseMaxJump;
            gt.jumpMultiplier = m1 * m2 * baseJump;
        }
    }

    private void SetLongArms(bool on)
    {
        if (longArms == on || gt == null)
        {
            return;
        }
        longArms = on;
        if (on)
        {
            savedLeftOffset = gt.leftHandOffset;
            savedRightOffset = gt.rightHandOffset;
            savedArmLength = gt.maxArmLength;
            Vector3 add = AimFromGrip * Vector3.forward * longArmReach;
            gt.leftHandOffset = savedLeftOffset + add;
            gt.rightHandOffset = savedRightOffset + add;
            gt.maxArmLength = savedArmLength + longArmStretch;
        }
        else
        {
            gt.leftHandOffset = savedLeftOffset;
            gt.rightHandOffset = savedRightOffset;
            gt.maxArmLength = savedArmLength;
        }
        gt.InitializeValues();
    }

    private void SetNoclip(bool on)
    {
        if (noclip == on)
        {
            return;
        }
        noclip = on;
        if (on)
        {
            savedLayers = gt.locomotionEnabledLayers;
            gt.locomotionEnabledLayers = 0;
            offColliders.Clear();
            Collider[] cols = gt.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++)
            {
                Collider c = cols[i];
                if (c.enabled && !c.isTrigger && !(c is CharacterController))
                {
                    if (body != null && c.attachedRigidbody == body)
                    {
                        c.enabled = false;
                        offColliders.Add(c);
                    }
                }
            }
            if (body == null)
            {
                return;
            }
            if (body.isKinematic)
            {
                return;
            }
            body.linearVelocity = Vector3.zero;
        }
        else
        {
            gt.locomotionEnabledLayers = savedLayers;
            foreach (Collider c in offColliders)
            {
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            offColliders.Clear();
            gt.InitializeValues();
        }
    }

    private void SetGravity(int mode)
    {
        if (gravityMode == mode)
        {
            return;
        }
        gravityMode = mode;
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        int mode = Free ? gravityMode : 0;
        if (mode == gravityNow)
        {
            return;
        }
        if (gravityNow == 0)
        {
            savedGravity = Physics.gravity;
        }
        gravityNow = mode;
        Vector3 g;
        if (mode == 1)
        {
            g = savedGravity * lowGravity;
        }
        else if (mode == 2)
        {
            g = Vector3.zero;
        }
        else if (mode == 3)
        {
            g = -savedGravity;
        }
        else
        {
            g = savedGravity;
        }
        Physics.gravity = g;
    }

    private bool MoveBy(Vector3 delta)
    {
        if (KillCam.Local != null && KillCam.Local.Busy)
        {
            toast = "NOT WHILE YOU'RE CAUGHT";
            toastUntil = Time.unscaledTime + 2.2f;
            return false;
        }
        if (MonsterPlayer.IsMonster)
        {
            toast = "NOT AS A MONSTER";
            toastUntil = Time.unscaledTime + 2.2f;
            return false;
        }
        transform.position = transform.position + delta;
        if (body != null)
        {
            body.position = transform.position;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
        Physics.SyncTransforms();
        gt.InitializeValues();
        return true;
    }

    private void TeleportToSpawn()
    {
        if (KillCam.Local != null && KillCam.Local.Busy)
        {
            toast = "NOT WHILE YOU'RE CAUGHT";
            toastUntil = Time.unscaledTime + 2.2f;
            return;
        }
        transform.SetPositionAndRotation(spawnPos, spawnRot);
        if (body != null)
        {
            body.position = spawnPos;
            body.rotation = spawnRot;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
        Physics.SyncTransforms();
        gt.InitializeValues();
        toast = "BACK AT SPAWN";
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void TeleportForward()
    {
        Transform head = Head();
        Vector3 dir = head.forward;
        RaycastHit hit;
        bool hitSomething = Physics.SphereCast(head.position, 0.25f, dir, out hit, 4f, SpawnMask, QueryTriggerInteraction.Ignore);
        float dist = Mathf.Max(hit.distance - 0.3f, 0f);
        if (!hitSomething)
        {
            dist = 4f;
        }
        if (MoveBy(dir * dist))
        {
            toast = "ZOOM";
            toastUntil = Time.unscaledTime + 2.2f;
        }
    }

    private void SetCheckpoint()
    {
        Vector3 pos = Head().position;
        checkpoint = new Vector3?(pos);
        toast = "CHECKPOINT SAVED";
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void GoToCheckpoint()
    {
        if (!checkpoint.HasValue)
        {
            toast = "SET A CHECKPOINT FIRST";
        }
        else
        {
            Vector3 target = checkpoint.Value;
            if (!MoveBy(target - Head().position))
            {
                return;
            }
            toast = "BACK AT YOUR CHECKPOINT";
        }
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void TeleportToPlayer()
    {
        List<NetworkApe> others = new List<NetworkApe>();
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape != null && !ape.IsLocal && ape.head != null)
            {
                others.Add(ape);
            }
        }
        if (others.Count == 0)
        {
            toast = "NOBODY ELSE HERE";
            toastUntil = Time.unscaledTime + 2.2f;
            return;
        }
        int idx = playerCursor;
        playerCursor = idx + 1;
        NetworkApe target = others[idx % others.Count];
        Vector3 mine = Head().position;
        Vector3 theirs = target.head.position;
        Vector3 away = Vector3.ProjectOnPlane(mine - theirs, Vector3.up);
        Vector3 dir;
        if (away.sqrMagnitude <= 0.0001f)
        {
            dir = -Vector3.ProjectOnPlane(target.head.forward, Vector3.up).normalized;
        }
        else
        {
            dir = away.normalized;
        }
        if (!MoveBy(theirs + dir * 0.9f - mine))
        {
            return;
        }
        string name = string.IsNullOrEmpty(target.PlayerName) ? "THEM" : target.PlayerName.ToUpper();
        toast = "TO " + name;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void SetPlatforms(bool on)
    {
        platforms = on;
        if (on)
        {
            return;
        }
        KillPlatform(ref platL);
        KillPlatform(ref platR);
    }

    private void Platforms()
    {
        if (!platforms)
        {
            return;
        }
        if (gt == null)
        {
            return;
        }
        bool left = Grip(XRNode.LeftHand) > 0.6f && !gt.leftHandDisabled;
        Hold(ref platL, left, LeftHand());
        bool right = Grip(XRNode.RightHand) > 0.6f && !gt.rightHandDisabled;
        Hold(ref platR, right, RightHand());
    }

    private void Hold(ref Transform plat, bool held, Vector3 hand)
    {
        if (!held)
        {
            if (plat != null)
            {
                Destroy(plat.gameObject);
            }
            plat = null;
            return;
        }
        if (plat != null)
        {
            return;
        }
        Vector3 at = hand - Vector3.up * 0.06f;
        Vector3 size = new Vector3(platformSize, 0.02f, platformSize);
        GameObject slab = Slab("ModMenu Platform", at, size);
        plat = slab.transform;
    }

    private static void KillPlatform(ref Transform plat)
    {
        if (plat != null)
        {
            Destroy(plat.gameObject);
        }
        plat = null;
    }

    private void DropPlatform()
    {
        Transform head = Head();
        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = Vector3.forward;
        }
        flat = flat.normalized;
        Vector3 at = flat * 1.4f + head.position - Vector3.up * 0.9f;
        GameObject slab = Slab("ModMenu Dropped Platform", at, new Vector3(1.1f, 0.06f, 1.1f));
        slab.transform.rotation = Quaternion.LookRotation(flat, Vector3.up);
        dropped.Add(slab);
        toast = "PLATFORM";
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void ClearPlatforms()
    {
        foreach (GameObject go in dropped)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        dropped.Clear();
        toast = "PLATFORMS CLEARED";
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private GameObject Slab(string name, Vector3 at, Vector3 size)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = at;
        go.transform.localScale = size;
        LayerMask mask = noclip ? savedLayers : gt.locomotionEnabledLayers;
        for (int layer = 0; layer != 32; layer++)
        {
            if (((mask.value >> layer) & 1) != 0)
            {
                go.layer = layer;
                break;
            }
        }
        Renderer renderer = go.GetComponent<Renderer>();
        if (platformMat == null && unlitMat != null)
        {
            platformMat = new Material(unlitMat);
            platformMat.name = "ModMenu Platform";
            platformMat.SetColor("_BaseColor", Accent);
        }
        if (platformMat != null)
        {
            renderer.sharedMaterial = platformMat;
        }
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    private void Confetti()
    {
        Vector3 at = RightHand() + Vector3.up * 0.1f;
        Burst(at, 160, 4.5f, 0.05f, 2.6f, 0.7f);
        toast = "CONFETTI";
        toastUntil = Time.unscaledTime + 2.2f;
    }
}
