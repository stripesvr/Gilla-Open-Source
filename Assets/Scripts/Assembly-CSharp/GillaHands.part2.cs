// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using GorillaLocomotion;

public partial class GillaHands : MonoBehaviour
{
    public GillaHands()
    {
        reach = 0.1f;
        throwBoost = 1.25f;
        holding = new GillaItem[3];
        heldAt = new Vector3[3];
        settling = new Vector3[3];
        heldTurn = new Quaternion[3];
        shapes = new HeldShape[2];
        knocking = new bool[2];
        was = new bool[2];
        testHeld = new bool[2];
        trailAt = new Vector3[3, 5];
        trailTurn = new Quaternion[3, 5];
        trailTime = new float[5];
        rig = new Collider[0];
        posers = new HandPoser[0];
    }

    private GillaItem Nearest(Vector3 at)
    {
        float best = reach;
        float scale = 1f;
        if (MonsterPlayer.IsMonster)
        {
            scale = transform.lossyScale.x;
            if (scale <= 1f)
            {
                scale = 1f;
            }
        }
        best *= scale;
        GillaItem found = null;
        foreach (GillaItem item in GillaItem.All)
        {
            if (!item.Swallowed && !item.Gone && !item.HeldByOther && Allowed(item))
            {
                Collider[] colliders = item.Colliders;
                float d = float.MaxValue;
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider c = colliders[i];
                    if (c == null)
                    {
                        continue;
                    }
                    bool enabled = c.enabled;
                    c.enabled = true;
                    Vector3 p = c.ClosestPoint(at);
                    float dist = (p - at).magnitude;
                    if (dist <= d)
                    {
                        d = dist;
                    }
                    c.enabled = enabled;
                }
                if (d <= best)
                {
                    best = d;
                    found = item;
                }
            }
        }
        return found;
    }

    private static Vector3 NearestOn(GillaItem item, Vector3 at)
    {
        Vector3 result = item.transform.position;
        Collider[] colliders = item.Colliders;
        float best = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null)
            {
                continue;
            }
            bool enabled = c.enabled;
            c.enabled = true;
            Vector3 p = c.ClosestPoint(at);
            c.enabled = enabled;
            float sq = (p - at).sqrMagnitude;
            if (sq < best)
            {
                result = p;
                best = sq;
            }
        }
        return result;
    }

    private void Take(int h, GillaItem item)
    {
        for (int i = 0; i < 3; i++)
        {
            if (holding[i] == item)
            {
                LetGo(i, false);
            }
        }
        Vector3 point = Point(h);
        Vector3 from = item.transform.position;
        Vector3 grabAt = point;
        if (h != 2)
        {
            Vector3 near = NearestOn(item, point);
            grabAt = point + from - near;
        }
        else
        {
            // Redux: the mouse holds an item by its middle, not its pivot (the jukebox's pivot
            // is at the floor, a metre under the radio)
            Vector3 middle = from;
            bool any = false;
            Bounds b = new Bounds(from, Vector3.zero);
            foreach (Collider c in item.Colliders)
            {
                if (c == null) continue;
                bool was = c.enabled;
                c.enabled = true;
                if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
                c.enabled = was;
            }
            if (any) middle = b.center;
            grabAt = point + from - middle;
        }
        holding[h] = item;
        item.Take(h);
        Quaternion turn = Turn(h);
        heldAt[h] = Quaternion.Inverse(turn) * (grabAt - point);
        settling[h] = from - grabAt;
        heldTurn[h] = Quaternion.Inverse(turn) * item.transform.rotation;
        if (h < 2)
        {
            shapes[h] = ShapeOf(item, Quaternion.Inverse(turn), heldAt[h], from);
            knocking[h] = true;
        }
        Fingers(h, 1f);
        Sfx.Play(pickUps, item.transform.position, 0.55f, 0.94f, 1.08f);
        if (h < 2)
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(h != 0 ? XRNode.RightHand : XRNode.LeftHand);
            device.SendHapticImpulse(0, 0.45f, 0.05f);
        }
    }

    private void LetGo(int h, bool thrown)
    {
        GillaItem item = holding[h];
        holding[h] = null;
        if (h < 2)
        {
            shapes[h] = null;
            if (gt != null)
            {
                if (h == 0)
                {
                    gt.leftHeld = null;
                }
                else
                {
                    gt.rightHeld = null;
                }
            }
        }
        Fingers(h, 0f);
        if (item == null)
        {
            return;
        }
        Vector3 velocity = Vector3.zero;
        Vector3 spin = Vector3.zero;
        if (thrown)
        {
            Throw(h, out velocity, out spin);
        }
        bool hasGt = gt != null;
        if (h < 2 && hasGt)
        {
            Vector3 point = Point(h);
            RaycastHit hit;
            if (Physics.Linecast(point, item.transform.position, out hit, gt.locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
            {
                item.transform.position = hit.point + hit.normal * 0.05f;
            }
        }
        Sfx.Play(putDowns, item.transform.position, 0.5f, 0.94f, 1.08f);
        item.Release(velocity, Vector3.ClampMagnitude(spin, 25f));
    }

    private void Throw(int h, out Vector3 velocity, out Vector3 spin)
    {
        spin = Vector3.zero;
        velocity = Vector3.zero;
        if (frames < 6)
        {
            return;
        }
        int next = (head + 1) % 5;
        float dt = trailTime[head] - trailTime[next];
        if (dt <= 0.001f)
        {
            dt = 0.001f;
        }
        velocity = (trailAt[h, head] - trailAt[h, next]) / dt * throwBoost;
        Quaternion delta = trailTurn[h, head] * Quaternion.Inverse(trailTurn[h, next]);
        Vector3 axis;
        float angle;
        delta.ToAngleAxis(out angle, out axis);
        if (angle > 180f)
        {
            angle -= 360f;
        }
        if (!float.IsNaN(axis.x) && !float.IsInfinity(axis.x))
        {
            spin = axis * (angle * Mathf.Deg2Rad / dt);
        }
        if (h != 2)
        {
            return;
        }
        Vector3 boost;
        if (Camera.main != null)
        {
            boost = Camera.main.transform.forward * 5f + Vector3.up * 1.2f;
        }
        else
        {
            boost = Vector3.zero;
        }
        velocity += boost;
    }

    private void Fingers(int h, float k)
    {
        if (h == 2)
        {
            return;
        }
        XRNode node = h != 0 ? XRNode.RightHand : XRNode.LeftHand;
        for (int i = 0; i < posers.Length; i++)
        {
            HandPoser poser = posers[i];
            if (poser != null && poser.node == node)
            {
                poser.gripOverride = k;
            }
        }
        if (MonsterPlayer.Local != null)
        {
            MonsterPlayer.Local.CloseFingers(h == 1, k);
        }
    }

    private void MouseHands(bool free)
    {
        if (!XRSettings.isDeviceActive && UnityEngine.InputSystem.Mouse.current != null && Camera.main != null && !GillaFreecam.On)
        {
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            bool ok = free ? Allowed(holding[2]) : false;
            if (ok && holding[2] == null && mouse.leftButton.wasPressedThisFrame)
            {
                Camera cam = Camera.main;
                Vector2 pos = mouse.position.ReadValue();
                Ray ray = cam.ScreenPointToRay(new Vector3(pos.x, pos.y, 0f));
                GillaItem best = null;
                float dist = 4f;
                foreach (GillaItem item in GillaItem.All)
                {
                    if (!item.Gone && !item.Swallowed && item.Hand < 0 && !item.HeldByOther && Allowed(item))
                    {
                        Collider[] colliders = item.Colliders;
                        for (int i = 0; i < colliders.Length; i++)
                        {
                            Collider c = colliders[i];
                            if (c != null && c.enabled)
                            {
                                RaycastHit hit;
                                if (c.Raycast(ray, out hit, dist))
                                {
                                    best = item;
                                    dist = hit.distance;
                                }
                            }
                        }
                    }
                }
                if (best != null)
                {
                    Take(2, best);
                }
                return;
            }
            if (holding[2] == null)
            {
                return;
            }
            if (mouse.leftButton.isPressed && ok)
            {
                return;
            }
            LetGo(2, ok);
            return;
        }
        if (holding[2] != null)
        {
            LetGo(2, false);
        }
    }

    private static bool Grip(XRNode node, bool holding)
    {
        if (MonsterPlayer.TestGrips >= 0)
        {
            // Redux: grips from the keyboard rig (or a test)
            return (MonsterPlayer.TestGrips & (node == XRNode.LeftHand ? 1 : 2)) != 0;
        }
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        bool result = false;
        if (device.isValid)
        {
            float value;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out value))
            {
                float threshold = holding ? 0.4f : 0.7f;
                result = threshold < value;
            }
            else
            {
                result = false;
            }
        }
        return result;
    }
}
