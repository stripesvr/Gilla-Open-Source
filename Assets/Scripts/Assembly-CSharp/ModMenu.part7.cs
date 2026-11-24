// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public partial class ModMenu : MonoBehaviour
{
    private bool Spot(GameObject prefab, out Vector3 feet, out Quaternion facing)
    {
        feet = Vector3.zero;
        facing = Quaternion.identity;
        CharacterController cc = prefab.GetComponent<CharacterController>();
        float height = 2f;
        if (cc != null)
        {
            height = cc.height;
        }
        float radius = cc != null ? cc.radius : 0.3f;
        Transform head = Head();
        Vector3 start = head.position;
        Vector3 eye = start;
        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.0001f)
        {
            fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }
        fwd = fwd.normalized;
        float low = radius + 0.05f;
        float high = Mathf.Max(low, height - radius);
        for (int i = 0; i < 80; i++)
        {
            float spread = i > 39 ? 180f : 35f;
            float angle = UnityEngine.Random.Range(-spread, spread);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * fwd;
            float dist = UnityEngine.Random.Range(spawnMin, spawnMax);
            Vector3 end = start + dir * dist;
            if (Physics.Linecast(start, end, SpawnMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }
            RaycastHit hit;
            if (!Physics.Raycast(end, Vector3.down, out hit, 4f, SpawnMask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.75f)
            {
                continue;
            }
            Vector3 p = hit.point;
            eye.y = p.y + 0.4f;
            if (Physics.Linecast(eye, p + Vector3.up * 0.4f, SpawnMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }
            if (Physics.CheckCapsule(p + Vector3.up * low, p + Vector3.up * high, radius, SpawnMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }
            Vector3 flat = new Vector3(eye.x - p.x, 0f, eye.z - p.z);
            feet = p + Vector3.up * 0.02f;
            if (flat.sqrMagnitude <= 0.0001f)
            {
                facing = Quaternion.identity;
            }
            else
            {
                facing = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }
            return true;
        }
        return false;
    }

    private void BecomeMonster(int index, string name)
    {
        MonsterPlayer local = MonsterPlayer.Local;
        string msg;
        if (local == null)
        {
            msg = "CAN'T HERE";
        }
        else if (KillCam.Local != null && KillCam.Local.Busy)
        {
            msg = "NOT WHILE YOU'RE CAUGHT";
        }
        else if (index != 0)
        {
            hover = false;
            SetNoclip(false);
            SetLongArms(false);
            SetPlatforms(false);
            local.Become(index);
            msg = "YOU'RE " + name;
        }
        else
        {
            local.Become(0);
            msg = "YOU'RE THE APE AGAIN";
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void Give(int coins, int bolts, int xp)
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
            if (coins >= 1)
            {
                msg = "+" + coins.ToString("N0") + " COINS";
            }
            else if (bolts >= 1)
            {
                msg = "+" + bolts.ToString("N0") + " BOLTS";
            }
            else
            {
                msg = "+" + xp.ToString("N0") + " XP";
            }
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void HealArms()
    {
        NetworkBootstrap net = NetworkBootstrap.Instance;
        string msg = "CAN'T HERE";
        if (net != null && net.tearing != null)
        {
            if (KillCam.Local != null && KillCam.Local.Busy)
            {
                msg = "NOT WHILE YOU'RE CAUGHT";
            }
            else
            {
                net.tearing.ResetAll();
                gt.InitializeValues();
                msg = "ARMS BACK";
            }
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private static NetworkBootstrap Net
    {
        get { return NetworkBootstrap.Instance; }
    }

    private void Join(NetworkBootstrap.Lobby? kind, string code)
    {
        string msg;
        if (Net == null)
        {
            msg = "OFFLINE";
        }
        else
        {
            if (!kind.HasValue)
            {
                Net.JoinRoom(code);
            }
            else
            {
                Net.JoinLobby(kind.Value, code);
            }
            msg = "JOINING...";
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void Rejoin()
    {
        if (Net == null)
        {
            toast = "OFFLINE";
            toastUntil = Time.unscaledTime + 2.2f;
            return;
        }
        string label = Net.RoomLabel ?? "";
        int dash = label.IndexOf('-');
        if (dash >= 0)
        {
            label = label.Substring(dash + 1);
        }
        string code = label;
        for (int i = 0; i < label.Length; i++)
        {
            if (!char.IsDigit(label[i]))
            {
                code = "";
                break;
            }
        }
        Join(new NetworkBootstrap.Lobby?(Net.Kind), code);
    }

    private void OpenAllDoors()
    {
        int opened = 0;
        foreach (GillaDoor door in new List<GillaDoor>(Object.FindObjectsByType<GillaDoor>(FindObjectsSortMode.None)) /* ? */)
        {
            if (door != null && !door.IsOpen && !door.Busy)
            {
                door.Press();
                opened++;
            }
        }
        string msg = "NO SHUT DOORS";
        if (opened > 0)
        {
            msg = string.Format("OPENED {0} DOORS", opened);
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void SetFullbright(bool on)
    {
        if (fullbright == on)
        {
            return;
        }
        fullbright = on;
        if (on)
        {
            savedAmbientMode = RenderSettings.ambientMode;
            savedSky = RenderSettings.ambientSkyColor;
            savedEquator = RenderSettings.ambientEquatorColor;
            savedGround = RenderSettings.ambientGroundColor;
            savedFog = RenderSettings.fog;
            savedSun = RenderSettings.sun;
            stilled.Clear();
            GillaAtmosphere[] atmospheres = Object.FindObjectsByType<GillaAtmosphere>(FindObjectsSortMode.None);
            for (int i = 0; i < atmospheres.Length; i++)
            {
                GillaAtmosphere a = atmospheres[i];
                if (a.enabled)
                {
                    a.enabled = false;
                    stilled.Add(a);
                }
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = fullbrightAmbient;
            RenderSettings.fog = false;
            GameObject go = new GameObject("ModMenu Fullbright");
            lamp = go.AddComponent<Light>();
            lamp.type = LightType.Directional;
            lamp.color = new Color(1f, 1f, 1f, 1f);
            lamp.intensity = 1f;
            lamp.shadows = LightShadows.None;
            lamp.transform.SetParent(Head(), false);
            RenderSettings.sun = lamp;
        }
        else
        {
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientSkyColor = savedSky;
            RenderSettings.ambientEquatorColor = savedEquator;
            RenderSettings.ambientGroundColor = savedGround;
            RenderSettings.fog = savedFog;
            RenderSettings.sun = savedSun;
            if (lamp != null)
            {
                Object.Destroy(lamp.gameObject);
            }
            lamp = null;
            foreach (GillaAtmosphere a in stilled)
            {
                if (a != null)
                {
                    a.enabled = true;
                }
            }
            stilled.Clear();
        }
    }

    private Material EspMat()
    {
        if (espMat == null)
        {
            Material m = new Material(font.material);
            m.name = "ModMenu ESP";
            m.mainTexture = Texture2D.whiteTexture;
            m.renderQueue = 4000;
            espMat = m;
        }
        return espMat;
    }

    private void UpdateEsp()
    {
        seen.Clear();
        Transform head = Head();
        if (monsterEsp)
        {
            foreach (MonsterBrain brain in MonsterBrain.All)
            {
                if (brain == null || !brain.isActiveAndEnabled)
                {
                    continue;
                }
                Marker marker;
                if (!markers.TryGetValue(brain, out marker))
                {
                    marker = MakeMarker(brain.name, monsterColour, brain.GetComponent<CharacterController>());
                    markers[brain] = marker;
                }
                Bounds b;
                if (marker.cc != null && marker.cc.enabled)
                {
                    b = marker.cc.bounds;
                }
                else
                {
                    b = new Bounds(brain.transform.position + Vector3.up, new Vector3(0.8f, 2f, 0.8f));
                }
                string label = brain.name.Replace("(Clone)", "").Trim();
                DrawMarker(marker, b, head, label);
                seen.Add(brain);
            }
        }
        if (playerEsp)
        {
            foreach (NetworkApe ape in NetworkApe.All)
            {
                if (ape == null || ape.IsLocal || ape.head /* ? */ == null)
                {
                    continue;
                }
                Marker marker;
                if (!markers.TryGetValue(ape, out marker))
                {
                    marker = MakeMarker("player", playerColour, null);
                    markers[ape] = marker;
                }
                Vector3 pos = ape.head /* ? */.position;
                string label = string.IsNullOrEmpty(ape.PlayerName) ? "PLAYER" : ape.PlayerName;
                Bounds b = new Bounds(pos + Vector3.up * -0.55f, new Vector3(0.7f, 1.4f, 0.7f));
                DrawMarker(marker, b, head, label);
                seen.Add(ape);
            }
        }
        gone.Clear();
        foreach (KeyValuePair<Object, Marker> pair in markers)
        {
            if (pair.Key == null || !seen.Contains(pair.Key))
            {
                gone.Add(pair.Key);
            }
        }
        foreach (Object key in gone)
        {
            DestroyMarker(markers[key]);
            markers.Remove(key);
        }
    }

    private void DrawMarker(Marker m, Bounds box, Transform head, string name)
    {
        Vector3 headPos = head.position;
        Vector3 c = box.center;
        Vector3 e = box.extents;
        float x0 = c.x - e.x, x1 = c.x + e.x;
        float y0 = c.y - e.y, y1 = c.y + e.y;
        float z0 = c.z - e.z, z1 = c.z + e.z;
        corners[0] = new Vector3(x0, y0, z0);
        corners[1] = new Vector3(x1, y0, z0);
        corners[2] = new Vector3(x1, y0, z1);
        corners[3] = new Vector3(x0, y0, z1);
        corners[4] = new Vector3(x0, y0, z0);
        corners[5] = new Vector3(x0, y1, z0);
        corners[6] = new Vector3(x1, y1, z0);
        corners[7] = new Vector3(x1, y0, z0);
        corners[8] = new Vector3(x1, y1, z0);
        corners[9] = new Vector3(x1, y1, z1);
        corners[10] = new Vector3(x1, y0, z1);
        corners[11] = new Vector3(x1, y1, z1);
        corners[12] = new Vector3(x0, y1, z1);
        corners[13] = new Vector3(x0, y0, z1);
        corners[14] = new Vector3(x0, y1, z1);
        corners[15] = new Vector3(x0, y1, z0);
        float dist = Vector3.Distance(headPos, box.center);
        m.box.SetPositions(corners);
        m.box.widthMultiplier = Mathf.Clamp(dist * 0.004f, 0.008f, 0.08f);
        Vector3 labelPos = new Vector3(box.center.x, dist * 0.01f + y1 + 0.15f, box.center.z);
        Transform lt = m.label.transform;
        Quaternion rot = Quaternion.LookRotation(labelPos - head.position, Vector3.up);
        lt.SetPositionAndRotation(labelPos, rot);
        TextMesh label = m.label;
        float size = Mathf.Clamp(dist * 0.012f, 0.03f, 0.5f);
        label.characterSize = size * 10f / label.fontSize;
        label.text = name + "  " + Mathf.RoundToInt(dist).ToString() + "m";
    }

    private Marker MakeMarker(string name, Color colour, CharacterController cc)
    {
        Marker marker = new Marker();
        marker.cc = cc;
        GameObject go = new GameObject("ModMenu ESP " + name);
        marker.box = go.AddComponent<LineRenderer>();
        marker.box.sharedMaterial = EspMat();
        marker.box.useWorldSpace = true;
        marker.box.positionCount = corners.Length;
        marker.box.endColor = colour;
        marker.box.startColor = colour;
        marker.box.numCornerVertices = 0;
        marker.box.shadowCastingMode = ShadowCastingMode.Off;
        marker.box.receiveShadows = false;
        GameObject labelGo = new GameObject("Label");
        marker.label = labelGo.AddComponent<TextMesh>();
        marker.label.transform.SetParent(go.transform, false);
        marker.label.font = font;
        MeshRenderer mr = marker.label.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        marker.label.fontSize = 64;
        marker.label.anchor = TextAnchor.LowerCenter;
        marker.label.alignment = TextAlignment.Center;
        marker.label.color = colour;
        return marker;
    }

    private static void DestroyMarker(Marker m)
    {
        if (m != null && m.box != null)
        {
            Object.Destroy(m.box.gameObject);
        }
    }

    private void ClearMarkers(bool monsters)
    {
        gone.Clear();
        foreach (KeyValuePair<Object, Marker> pair in markers)
        {
            Object key = pair.Key;
            if (key == null || (key is MonsterBrain) == monsters)
            {
                gone.Add(key);
            }
        }
        foreach (Object key in gone)
        {
            DestroyMarker(markers[key]);
            markers.Remove(key);
        }
    }

    private void UpdateTracers()
    {
        seen.Clear();
        Vector3 from = RightHand();
        foreach (MonsterBrain brain in MonsterBrain.All)
        {
            if (brain != null && brain.isActiveAndEnabled)
            {
                Vector3 to = brain.transform.position + Vector3.up;
                Line(brain, from, to, monsterColour);
            }
        }
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape != null && !ape.IsLocal && ape.head /* ? */ != null)
            {
                Line(ape, from, ape.head /* ? */.position, playerColour);
            }
        }
        gone.Clear();
        foreach (KeyValuePair<Object, LineRenderer> pair in lines)
        {
            if (pair.Key == null || !seen.Contains(pair.Key))
            {
                gone.Add(pair.Key);
            }
        }
        foreach (Object key in gone)
        {
            if (lines[key] != null)
            {
                Object.Destroy(lines[key].gameObject);
            }
            lines.Remove(key);
        }
    }
}
