// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public partial class GillaFreecam : MonoBehaviour
{
    private void BringPlayer()
    {
        if (cam != null)
        {
            Vector3 floor = GillaTeleport.FloorUnder(cam.transform.position, 100f);
            Vector3 camForward = cam.transform.forward;
            RaycastHit hit = default(RaycastHit);
            Rigidbody body = null;
            GorillaLocomotion.Player p = GorillaLocomotion.Player.Instance;
            if (p == null)
            {
                return;
            }
            if (p.headCollider == null)
            {
                return;
            }
            GillaPlaces.Prepare(floor);
            GillaBackroomsSwitch.Prepare(floor);
            Transform root = p.transform;
            Transform head = p.headCollider.transform;
            float height = 1.5f;
            if (Physics.Raycast(head.position, Vector3.down, out hit, 3f, -7941, QueryTriggerInteraction.Ignore))
            {
                height = head.position.y - hit.point.y;
            }
            Vector3 headFlat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            Vector3 camFlat = Vector3.ProjectOnPlane(camForward, Vector3.up);
            if (headFlat.sqrMagnitude > 0.0001f && camFlat.sqrMagnitude > 0.0001f)
            {
                p.Turn(Vector3.SignedAngle(headFlat, camFlat, Vector3.up) /* ? */);
            }
            root.position = root.position + ((floor + height * Vector3.up) - head.position);
            if (root.TryGetComponent<Rigidbody>(out body))
            {
                body.position = root.position;
                body.linearVelocity = Vector3.zero;
            }
            p.InitializeValues();
        }
    }

    private static string Network()
    {
        MonsterBrain[] brains = UnityEngine.Object.FindObjectsByType<MonsterBrain>(FindObjectsSortMode.None);
        int onNet = 0;
        int mine = 0;
        int local = 0;
        for (int i = 0; i < brains.Length; i++)
        {
            MonsterBrain b = brains[i];
            if (b.net != null && b.net.Live)
            {
                onNet++;
                mine += b.net.IsAuthority ? 1 : 0;
            }
            else
            {
                local++;
            }
        }
        string monsters = string.Format("monsters: {0} on the network ({1} run by you)", onNet, mine);
        string extra = "";
        if (local > 0)
        {
            extra = string.Format(", {0} only yours", local);
        }
        string tail = monsters + extra;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (boot != null)
        {
            runner = boot.GetComponentInChildren<NetworkRunner>() /* ? */;
        }
        if (boot != null && boot.Joining)
        {
            return "network: joining the room...   " + tail;
        }
        if (runner != null && runner.IsRunning)
        {
            SessionInfo info = runner.SessionInfo;
            string room = string.Format("room {0} ({1}), {2} in it, ", boot.RoomLabel, info.Name, info.PlayerCount);
            return room + (runner.IsSharedModeMasterClient ? "you're its master client" : "someone else is its master client") + "   " + tail;
        }
        return "network: offline   " + tail;
    }

    private void OnGUI()
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 13;
            style.wordWrap = false;
        }
        if (!On)
        {
            if (hintUntil <= Time.unscaledTime)
            {
                return;
            }
            int h = Screen.height;
            GUI.Box(new Rect(10f, h - 36, 350f, 26f), "P: freecam    G: an ape to punch    C: the cheese pull", style);
            return;
        }
        string text = string.Concat(new string[]
        {
            "FREECAM    P: back to your player\n",
            string.Format("right mouse + move: look    WASD: fly    Q / E: down / up    Shift: faster    scroll: speed ({0:0.#} m/s)\n", speed),
            "left click: press buttons and keys, break glass, shove loose things    T: bring your player here\nG: stand an ape here    left click on it: punch it    K: make it a ragdoll    Y: rip it in two    R: clear them\nC: a dummy in front of you, and the cheese pull done to it\nunder the pointer: ",
            under,
            "\n",
            net
        });
        Vector2 size = style.CalcSize(new GUIContent(text));
        GUI.Box(new Rect(10f, 10f, size.x + 10f, size.y + 6f), text, style);
    }
}
