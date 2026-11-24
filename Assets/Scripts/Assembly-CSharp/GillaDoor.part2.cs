// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class GillaDoor : MonoBehaviour
{
    public bool Blow(Vector3 away)
    {
        if (keycardOnly || modeMap || Blown || 7 < index)
        {
            return false;
        }
        if (IsOpen && open == (IsOpen ? 1f : 0f))
        {
            return false;
        }
        blowAway = away;
        Mask = (1 << index) | (1 << (index + BlownShift)) | Mask;
        Mask = Mask & ~(1 << (index ^ PriedShift));
        Changed();
        return true;
    }

    private static void Changed()
    {
        NetworkApe local = NetworkApe.Local;
        Version = Version + 1;
        By = local != null ? ApeOwner(local) : 0;
    }

    // NetworkApe.owner is not accessible from here; read it by reflection.
    private static int ApeOwner(NetworkApe ape) /* ? */
    {
        object v = ReadMember(ape, "owner");
        return v is int i ? i : 0;
    }

    private static object ReadMember(object target, string name) /* ? */
    {
        if (target == null)
        {
            return null;
        }
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();
        while (type != null)
        {
            FieldInfo f = type.GetField(name, flags);
            if (f != null)
            {
                return f.GetValue(target);
            }
            PropertyInfo p = type.GetProperty(name, flags);
            if (p != null)
            {
                return p.GetValue(target);
            }
            type = type.BaseType;
        }
        return null;
    }

    private bool MonsterInDoorway()
    {
        Vector3 foot = Foot;
        Vector3 through = Through;
        Vector3 up = Vector3.up;
        Vector3 side = Vector3.Cross(up, through);
        foreach (MonsterBrain monster in MonsterBrain.All)
        {
            if (monster == null)
            {
                continue;
            }
            if (ReadMember(monster, "door") as GillaDoor == this) /* ? */
            {
                return true;
            }
            Vector3 d = monster.transform.position - foot;
            if (Mathf.Abs(Vector3.Dot(d, Through)) < Thickness * 0.5f + 0.9f
                && Mathf.Abs(Vector3.Dot(d, side)) < Width * 0.5f + 0.3f
                && Mathf.Abs(d.y) < 2f)
            {
                return true;
            }
        }
        return false;
    }

    private static float PryCurve(float t)
    {
        return Spline.Eval(t, PryT, PryH);
    }

    public static void Offer(int mask, int version, int by)
    {
        if (Version <= version)
        {
            if (Version == version && by <= By)
            {
                return;
            }
            int oldVersion = Version;
            Mask = mask;
            Version = version;
            By = by;
            if (oldVersion == 0)
            {
                foreach (GillaDoor door in doors)
                {
                    door.Snap();
                }
            }
        }
    }

    public static void NewRoom()
    {
        Mask = 0;
        Version = 0;
        By = -1;
        NewModeRoom();
        foreach (GillaDoor door in doors)
        {
            if (!door.modeMap)
            {
                door.Snap();
            }
            else
            {
                door.NewRound();
            }
        }
    }

    private void Snap()
    {
        if (Blown)
        {
            blownShown = true;
            blownAt = Time.time;
            ShowSlab(false);
        }
        else if (blownShown)
        {
            BackIn();
        }
        bool isOpen = IsOpen;
        moving = false;
        prying = false;
        open = isOpen ? 1f : 0f;
        Place();
    }

    private void Update()
    {
        if (modeMap)
        {
            FollowRound();
        }
        if (blownShown != Blown)
        {
            if (Blown)
            {
                BlowOff();
            }
            else
            {
                BackIn();
            }
        }
        if (Blown)
        {
            if (modeMap)
            {
                return;
            }
            float stagger = 0f;
            if (NetworkApe.Local != null)
            {
                stagger = (ApeOwner(NetworkApe.Local) % 4) * 0.4f;
            }
            if (Time.time - blownAt <= stagger + blownResetSeconds)
            {
                return;
            }
            if (MonsterInDoorway())
            {
                return;
            }
            Mask = Mask & ~((1 << (index + BlownShift)) | (1 << index));
            NetworkApe local = NetworkApe.Local;
            Version = Version + 1;
            By = local != null ? ApeOwner(local) : 0;
            return;
        }
        float target = IsOpen ? 1f : 0f;
        float current = open;
        if (current == target)
        {
            if (shakeFrom < 0f)
            {
                return;
            }
            if (0.35f < Time.time - shakeFrom)
            {
                shakeFrom = -1f;
            }
            Place();
            return;
        }
        bool skipSound = false;
        if (!moving)
        {
            moving = true;
            if (target <= current || current != 0f)
            {
                prying = false;
            }
            else
            {
                prying = Pried;
                if (prying)
                {
                    PlayPry();
                    skipSound = true;
                }
            }
            if (!skipSound)
            {
                Sfx.Play(moveSounds, panel.position, 1f, 0.95f, 1.05f);
            }
        }
        float heightBefore = Height();
        float from = open;
        float dt = Time.deltaTime;
        float duration = prying ? PrySeconds : seconds;
        if (duration <= 0.05f)
        {
            duration = 0.05f;
        }
        float step = dt / duration;
        float next = from + (target - from >= 0f ? step : -step);
        if (Mathf.Abs(target - from) <= step)
        {
            next = target;
        }
        open = next;
        Place();
        if (target == 0f)
        {
            CrushUnder();
        }
        float r = rise;
        float heightAfter = Height();
        if (prying)
        {
            PrySparks(r * (heightAfter - heightBefore), Time.deltaTime, open == target);
        }
        if (open == target)
        {
            if (!prying)
            {
                Sfx.Play(stopSounds, panel.position, 1f, 0.95f, 1.05f);
            }
            moving = false;
            prying = false;
        }
    }

    private void CrushUnder()
    {
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        if (boot != null && boot.Joining)
        {
            return;
        }
        KillCam cam = KillCam.Local;
        if (cam != null && cam.head != null && !cam.Busy && !MonsterPlayer.IsMonster)
        {
            Vector3 away;
            if (Under(cam.head.position, MonsterPlayer.ApeSize, out away))
            {
                NetworkApe.Crushed(cam.head.position, away);
                GillaBlast.CrushYou(cam.head.position + Vector3.up * 0.3f, away + Vector3.up * 0.5f);
            }
            else
            {
                CrushHands();   // Redux: a hand under a closing door loses its arm (GillaDoor.Arms.cs)
            }
        }
        GillaDummy[] dummies = GillaDummy.All.ToArray();
        for (int i = 0; i < dummies.Length; i++)
        {
            GillaDummy dummy = dummies[i];
            if (dummy == null)
            {
                continue;
            }
            // GillaDummy.mode / Mode / blown are not accessible from here; read them by reflection.
            object modeValue = ReadMember(dummy, "mode"); /* ? */
            string modeName = modeValue != null ? modeValue.ToString() : null;
            object blownValue = ReadMember(dummy, "blown"); /* ? */
            bool dummyBlown = blownValue is bool b && b;
            if (modeName != "Gone" && modeName == "Standing" && !dummyBlown && dummy.Mine)
            {
                if (dummy.tearing != null && dummy.head != null)
                {
                    Vector3 away;
                    if (Under(dummy.head.position, 1f, out away))
                    {
                        NetworkApe.Crushed(dummy.head.position, away);
                        GillaBlast.CrushDummy(dummy, dummy.head.position + Vector3.up * 0.3f, away + Vector3.up * 0.5f);
                    }
                }
            }
        }
    }

    public static bool OutOfSlabs(Transform thing, Bounds bounds, Rigidbody rb)
    {
        Vector3 away;
        return OutOfSlabs(thing, bounds, rb, out away);
    }

    public static bool OutOfSlabs(Transform thing, Bounds bounds, Rigidbody rb, out Vector3 away)
    {
        away = Vector3.zero;
        foreach (GillaDoor door in doors)
        {
            if (door == null || door.Blown)
            {
                continue;
            }
            Vector3 center = bounds.center;
            Vector3 foot = door.Foot;
            Vector3 up = Vector3.up;
            Vector3 through = door.Through;
            float lift = 0f;
            if (door.IsOpen)
            {
                lift = door.rise * door.Height();
            }
            float bottom = door.Foot.y + lift;
            if (bounds.center.y + bounds.extents.y < bottom || bottom + door.tall < bounds.center.y - bounds.extents.y)
            {
                continue;
            }
            Vector3 d = center - foot;
            Vector3 ext = bounds.extents;
            float along = Vector3.Dot(d, through);
            float reach = door.Thickness * 0.5f + Mathf.Abs(ext.x * through.x) + Mathf.Abs(ext.z * through.z);
            if (reach <= Mathf.Abs(along))
            {
                continue;
            }
            Vector3 side = Vector3.Cross(up, through);
            if (Mathf.Abs(ext.x * side.x) + Mathf.Abs(side.z * ext.z) + door.Width * 0.5f <= Mathf.Abs(Vector3.Dot(d, side)))
            {
                continue;
            }
            away = through * (along >= 0f ? 1f : -1f);
            thing.position = thing.position + (away * (reach + 0.02f) - through * along);
            if (rb != null)
            {
                rb.position = thing.position;
            }
            return true;
        }
        return false;
    }

    private bool Under(Vector3 head, float size, out Vector3 away)
    {
        Vector3 d = head - Foot;
        Vector3 through = Through;
        float along = Vector3.Dot(d, through);
        away = along >= 0f ? through : -through;
        if (Mathf.Abs(along) < Thickness * 0.5f + 0.08f)
        {
            if (Width * 0.5f <= Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, through))))
            {
                return false;
            }
            if (d.y <= -0.3f)
            {
                return false;
            }
            if (rise * Height() <= size * 0.15f + d.y)
            {
                return d.y < rise * Height() + 0.6f;
            }
        }
        return false;
    }

    private float Height()
    {
        float t = open;
        if (prying)
        {
            return PryCurve(t * PrySeconds);
        }
        return Mathf.SmoothStep(0f, 1f, t);
    }

    private void Place()
    {
        Transform p = panel;
        Vector3 basePos = shut;
        float r = rise;
        float h = Height();
        Vector3 knock = Knock();
        p.localPosition = knock + basePos + Vector3.up * (r * h);
        if (!modeMap)
        {
            return;
        }
        panel.localRotation = KnockTurn() * shutTurn;
    }

    private void Seam(float c, float up, float kick, float lift, ref ParticleSystem.EmitParams ep)
    {
        Vector3 side = Vector3.Cross(Vector3.up, Through);
        float sign = Random.value < 0.5f ? 1f : -1f;
        Vector3 foot = Foot;
        float along = (seam - 0.01f) * c;
        ep.position = foot + side * along + Vector3.up * up + Through * (sign * (Thickness * 0.5f + 0.03f));
        Vector3 through = Through;
        float outSpeed = Random.Range(0.8f, 2.2f);
        float sideSpeed = Random.Range(0.2f, 1.2f);
        float upSpeed = lift <= 0f ? Random.Range(-1.2f, 0.3f) : Random.Range(-0.3f, 1.5f);
        ep.velocity = (through * (sign * outSpeed) + side * (-(c * sideSpeed)) + Vector3.up * upSpeed) * kick;
    }

    private void PrySparks(float moved, float dt, bool atTop)
    {
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
        if (prySparks == null)
        {
            prySparks = MakeSparks("PrySparks", 450, new Color(1f, 0.85f, 0.12f, 1f), new Color(1f, 0.68f, 0.03f, 1f), new Color(0.6f, 0.26f, 0.01f, 1f));
        }
        if (prySparks == null)
        {
            return;
        }
        float due = pryDue + (prySparksPerSecond + (Mathf.Abs(moved) / dt) * prySparksPerSpeed) * dt;
        int count = (int)due;
        int left = count > 39 ? 40 : count;
        pryDue = due - count;
        float kick = Mathf.Clamp((Mathf.Abs(moved) / dt) * 1.2f + 1f, 1f, 4.5f);
        ep = new ParticleSystem.EmitParams();
        if (0 < count)
        {
            do
            {
                float h = rise * Height();
                float t2 = top;
                if (t2 <= h)
                {
                    h = t2;
                }
                float t = Mathf.Clamp01(Random.value * Random.value);
                float c = Random.value < 0.5f ? 1f : -1f;
                Seam(c, h + (t2 - h) * t, kick, 1f, ref ep);
                ep.startLifetime = Random.Range(0.4f, 1.1f);
                ep.startSize = Random.Range(0.018f, 0.036f);
                prySparks.Emit(ep, 1);
                left--;
            } while (left != 0);
        }
        if (atTop)
        {
            for (int i = 40; i != 0; i--)
            {
                float v = Random.value;
                float t2 = top;
                float drop = Random.Range(0f, 0.2f);
                float k = Random.Range(1f, 2.2f);
                Seam(v < 0.5f ? 1f : -1f, t2 - drop, k, -1f, ref ep);
                ep.startLifetime = Random.Range(0.5f, 1.2f);
                ep.startSize = Random.Range(0.02f, 0.04f);
                prySparks.Emit(ep, 1);
            }
        }
    }

    private void BlowOff()
    {
        blownShown = true;
        blownAt = Time.time;
        if (open < 1f)
        {
            Vector3 away = blowAway;
            if (away.sqrMagnitude <= 0.01f)
            {
                away = AwayFromMonster();
            }
            Fling(away);
            PlayBlow();
            BlastSparks(away);
        }
        toldAtRest = false;
        stillSince = 0f;
        blowAway = Vector3.zero;
        ShowSlab(false);
        moving = false;
        prying = false;
        open = 1f;
        Place();
    }

    private void BackIn()
    {
        blownShown = false;
        heldBy = -1;
        if (flying != null)
        {
            Object.Destroy(flying);
        }
        ShowSlab(true);
    }

    private void ShowSlab(bool on)
    {
        if (slabRenderers == null)
        {
            slabRenderers = System.Array.FindAll(panel.GetComponentsInChildren<Renderer>(true), r => ShowSlab_b__168_0(r));
            slabColliders = System.Array.FindAll(panel.GetComponentsInChildren<Collider>(true), c => ShowSlab_b__168_1(c));
        }
        for (int i = 0; i < slabRenderers.Length; i++)
        {
            Renderer r = slabRenderers[i];
            if (r != null)
            {
                r.enabled = on;
            }
        }
        for (int i = 0; i < slabColliders.Length; i++)
        {
            Collider c = slabColliders[i];
            if (c != null)
            {
                c.enabled = on;
            }
        }
    }

    private Vector3 AwayFromMonster()
    {
        Vector3 foot = Foot;
        foreach (MonsterBrain monster in MonsterBrain.All)
        {
            if (monster == null)
            {
                continue;
            }
            if ((monster.transform.position - foot).sqrMagnitude < 16f)
            {
                Vector3 d = monster.transform.position - foot;
                return Vector3.Dot(d, Through) >= 0f ? -Through : Through;
            }
        }
        foreach (NetworkApe ape in NetworkApe.All)
        {
            if (ape == null || !ape.IsMonster || ape.head /* ? */ == null)
            {
                continue;
            }
            if ((ape.head.position - foot).sqrMagnitude < 36f)
            {
                Vector3 d = ape.head.position - foot;
                return Vector3.Dot(d, Through) >= 0f ? -Through : Through;
            }
        }
        return Through;
    }

    private void Fling(Vector3 away)
    {
        Rigidbody rb = MakeLoose();
        Vector3 flat = new Vector3(away.x, 0f, away.z);
        Vector3 dir;
        if (flat.sqrMagnitude <= 0.0001f)
        {
            dir = Through;
        }
        else
        {
            dir = flat.normalized;
        }
        float speed = 5.5f;
        if (!modeMap)
        {
            speed = Random.Range(8f, 11f);
        }
        float upSpeed = 2.5f;
        if (!modeMap)
        {
            upSpeed = Random.Range(3f, 4.5f);
        }
        rb.linearVelocity = dir * speed + Vector3.up * upSpeed;
        rb.angularVelocity = modeMap ? Vector3.zero : Random.insideUnitSphere * Random.Range(4f, 7f);
    }
}
