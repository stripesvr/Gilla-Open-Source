// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using GorillaLocomotion;

public partial class KillCam : MonoBehaviour
{
    public KillCam()
    {
        fadeSeconds = 0.45f;
        blackSeconds = 0.6f;
        follow = 18f;
        holdAfterBite = 0.9f;
        maxSeconds = 5f;
        faceTurnSeconds = 0.4f;
        thrownSeconds = 3f;
        handsBackOver = 0.2f;
        thrownSpeed = 4f;
        slamSpeed = 7f;
        heldEyesOver = 0.36f;
        launchGrace = 0.15f;
        heldEase = 0.2f;
        punchesToKill = 3;
        downTip = 75f;
        downFall = 0.55f;
        downSeconds = 3.5f;
        shoveDownSeconds = 1.5f;
        downRise = 0.45f;
        biteAt = -1f;
        backAt = -100f;
        hiddenBody = new List<Renderer>();
    }

    private Vector3 Unblocked(Vector3 from, Vector3 target)
    {
        Vector3 delta = target - from;
        float dist = delta.magnitude;
        if (0.0001f <= dist)
        {
            Vector3 dir = delta / dist;
            RaycastHit[] hits = Physics.RaycastAll(from, dir, dist + 0.15f, -1797, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance) /* ? */);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider.transform.IsChildOf(transform))
                    continue;
                if (Killer != null && hit.collider.transform.IsChildOf(Killer.transform))
                    continue;
                float d = Mathf.Max(hit.distance - 0.15f, 0f);
                target = from + dir * d;
                break;
            }
        }
        return target;
    }

    private void Heal()
    {
        foreach (Renderer r in hiddenBody)
        {
            if (r != null)
                r.enabled = true;
        }
        hiddenBody.Clear();
        Hits = 0;
        bool handsOk;
        if (locomotion != null)
            handsOk = !locomotion.leftHandDisabled && !locomotion.rightHandDisabled;
        else
            handsOk = true;
        GillaWounds.FinishAll();
        if (dismember != null)
            dismember.ResetAll();
        if (!handsOk)
        {
            if (locomotion != null && locomotion.enabled)
                locomotion.InitializeValues();
        }
    }

    private void HideBody()
    {
        if (dismember == null) return;
        Dismemberment[] copies = dismember.copies;
        if (copies == null) return;
        for (int i = 0; i < copies.Length; i++)
        {
            Dismemberment copy = copies[i];
            if (copy == null) continue;
            Renderer[] renderers = copy.GetComponentsInChildren<Renderer>();
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer r = renderers[j];
                if (r.enabled)
                {
                    r.enabled = false;
                    hiddenBody.Add(r);
                }
            }
        }
    }

    private void Respawn()
    {
        backAt = Time.time;
        Heal();
        Release(spawnPos, spawnRot, Vector3.zero);
        Transform where = onceAt != null ? onceAt : Back();
        onceAt = null;
        Home(where);
        SetFade(0f);
        Died?.Invoke();
        FellInPit = false;
    }

    public void SendHome()
    {
        Transform back = Back();
        Vector3 pos;
        Vector3 fwd;
        if (back != null)
        {
            pos = back.position;
            fwd = back.forward;
        }
        else
        {
            pos = spawnPos;
            fwd = spawnRot * Vector3.forward;
        }
        Player player = Player.Instance;
        if (player == null) return;
        if (player.headCollider == null) return;
        GillaPlaces.Prepare(pos);
        GillaBackroomsSwitch.Prepare(pos);
        Transform rig = player.transform;
        Transform headT = player.headCollider.transform;
        RaycastHit hit;
        float eyes = 1.5f;
        if (Physics.Raycast(headT.position, Vector3.down, out hit, 3f, -7941, QueryTriggerInteraction.Ignore))
            eyes = headT.position.y - hit.point.y;
        Vector3 headFlat = Vector3.ProjectOnPlane(headT.forward, Vector3.up);
        Vector3 wantFlat = Vector3.ProjectOnPlane(fwd, Vector3.up);
        if (0.0001f < headFlat.sqrMagnitude && 0.0001f < wantFlat.sqrMagnitude)
            player.Turn(Vector3.SignedAngle(headFlat, wantFlat, Vector3.up));
        rig.position = rig.position + ((pos + eyes * Vector3.up) - headT.position);
        if (!rig.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            player.InitializeValues();
            return;
        }
        rb.position = rig.position; /* ? */
        player.InitializeValues(); /* ? */
    }

    private Transform Back()
    {
        if (respawnAt != null)
        {
            if (!respawnAt.gameObject.activeInHierarchy)
                respawnAt = null;
        }
        if (respawnAt != null)
            return respawnAt;
        return ModeLobby();
    }

    private static Transform ModeLobby()
    {
        string name = GillaPlaces.CurrentName ?? "";
        NetworkBootstrap.Lobby kind = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Kind : NetworkBootstrap.Lobby.Regular;
        bool monster;
        if (name.StartsWith("BE THE MONSTER"))
            monster = true;
        else if (name.Length == 0)
            monster = kind == NetworkBootstrap.Lobby.Monster;
        else
            monster = false;
        bool notSplash;
        if (name.StartsWith("DON'T SPLASH"))
            notSplash = false;
        else
            notSplash = name.Length != 0 || kind != NetworkBootstrap.Lobby.Splash;
        if (monster)
        {
            if (GillaMapVote.Instance != null && GillaMapVote.Instance.lobby != null)
                return GillaMapVote.Instance.lobby;
        }
        if (notSplash) return null;
        if (GillaSplashLobby.Current == null) return null;
        if (GillaSplashLobby.Current.lobby == null) return null;
        return GillaSplashLobby.Current.lobby;
    }

    private void Home(Transform back)
    {
        Vector3 pos;
        Vector3 fwd;
        if (back != null)
        {
            pos = back.position;
            fwd = back.forward;
        }
        else
        {
            pos = spawnPos;
            fwd = spawnRot * Vector3.forward;
        }
        Player player = Player.Instance;
        if (player == null) return;
        if (player.headCollider == null) return;
        GillaPlaces.Prepare(pos);
        GillaBackroomsSwitch.Prepare(pos);
        Transform rig = player.transform;
        Transform headT = player.headCollider.transform;
        RaycastHit hit;
        float eyes = 1.5f;
        if (Physics.Raycast(headT.position, Vector3.down, out hit, 3f, -7941, QueryTriggerInteraction.Ignore))
            eyes = headT.position.y - hit.point.y;
        Vector3 headFlat = Vector3.ProjectOnPlane(headT.forward, Vector3.up);
        Vector3 wantFlat = Vector3.ProjectOnPlane(fwd, Vector3.up);
        if (0.0001f < headFlat.sqrMagnitude && 0.0001f < wantFlat.sqrMagnitude)
            player.Turn(Vector3.SignedAngle(headFlat, wantFlat, Vector3.up));
        rig.position = rig.position + ((pos + eyes * Vector3.up) - headT.position);
        if (!rig.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            player.InitializeValues();
            return;
        }
        rb.position = rig.position; /* ? */
        player.InitializeValues(); /* ? */
    }

    private void Release(Vector3 pos, Quaternion rot, Vector3 velocity)
    {
        flying = 0f;
        Knocked = false;
        tipped = 0f;
        BlownApart = false;
        DeathVelocity = Vector3.zero;
        transform.SetPositionAndRotation(pos, rot);
        if (body != null)
        {
            body.isKinematic = false;
            body.position = pos;
            body.rotation = rot;
            body.linearVelocity = velocity;
            body.angularVelocity = Vector3.zero;
            restoreInterp = true;
        }
        Physics.SyncTransforms();
        if (locomotion != null && !MonsterPlayer.IsMonster)
        {
            locomotion.InitializeValues();
            locomotion.enabled = true;
        }
        Busy = false;
        dying = false;
        Killer = null;
        heldFace = null;
        HeldBy = null;
    }

    private void BuildFade()
    {
        if (head == null) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "KillFade";
        UnityEngine.Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(head, false);
        go.transform.localScale = Vector3.one * 0.3f;
        go.layer = head.gameObject.layer;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        fadeMat = new Material(shader);
        fadeMat.SetFloat("_Surface", 1f);
        fadeMat.SetFloat("_Blend", 0f);
        fadeMat.SetFloat("_Cull", 0f);
        fadeMat.SetFloat("_ZTest", 8f);
        fadeMat.SetInt("_SrcBlend", 5);
        fadeMat.SetInt("_DstBlend", 10);
        fadeMat.SetInt("_ZWrite", 0);
        fadeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        fadeMat.renderQueue = 5000;
        fade = go.GetComponent<Renderer>();
        fade.sharedMaterial = fadeMat;
        fade.shadowCastingMode = ShadowCastingMode.Off;
        SetFade(0f);
    }

    private void SetFade(float a)
    {
        if (fade == null) return;
        fade.enabled = 0.001f < a;
        fadeMat.SetColor("_BaseColor", new Color(0f, 0f, 0f, a));
    }

    private static void Buzz(float amp, float seconds)
    {
        XRNode[] nodes = new XRNode[] { XRNode.LeftHand, XRNode.RightHand };
        for (int i = 0; i < nodes.Length; i++)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(nodes[i]);
            if (device.isValid)
                device.SendHapticImpulse(0u, amp, seconds);
        }
    }
}
