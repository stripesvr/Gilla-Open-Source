// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaPickaxe : NetworkBehaviour
{
    private static void MakeSparks()
    {
        Material material = Resources.Load<Material>("Gilla_PickSpark");
        if (material == null)
        {
            material = Resources.Load<Material>("Gilla_Spark");
        }
        if (material == null)
        {
            return;
        }

        sparks = Particles("PickSparks", material, 300);
        ParticleSystem.MainModule main = sparks.main;
        main.gravityModifier = 1.1f;

        ParticleSystem.ColorOverLifetimeModule col = sparks.colorOverLifetime;
        col.enabled = true;
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0.97f, 0.86f, 1f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.82f, 0.42f, 1f), 0.25f);
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.5f, 0.12f, 1f), 0.6f);
        colorKeys[3] = new GradientColorKey(new Color(0.7f, 0.18f, 0.02f, 1f), 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 0.55f);
        alphaKeys[2] = new GradientAlphaKey(0f, 1f);
        gradient.SetKeys(colorKeys, alphaKeys);
        col.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = sparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.35f));

        ParticleSystem.LimitVelocityOverLifetimeModule limit = sparks.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 100f;
        limit.drag = 1.6f;

        ParticleSystem.CollisionModule collision = sparks.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.quality = ParticleSystemCollisionQuality.Low;
        collision.collidesWith = -0x1705;
        collision.bounce = 0.3f;
        collision.dampen = 0.5f;
        collision.lifetimeLoss = 0.35f;

        ParticleSystemRenderer sparkRenderer = sparks.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        sparkRenderer.velocityScale = 0.016f;
        sparkRenderer.lengthScale = 1.5f;

        flash = Particles("PickFlash", material, 16);
        ParticleSystem.ColorOverLifetimeModule flashCol = flash.colorOverLifetime;
        flashCol.enabled = true;
        Gradient flashGradient = new Gradient();
        GradientColorKey[] flashColors = new GradientColorKey[2];
        flashColors[0] = new GradientColorKey(new Color(1f, 0.95f, 0.8f, 1f), 0f);
        flashColors[1] = new GradientColorKey(new Color(1f, 0.6f, 0.2f, 1f), 1f);
        GradientAlphaKey[] flashAlphas = new GradientAlphaKey[2];
        flashAlphas[0] = new GradientAlphaKey(1f, 0f);
        flashAlphas[1] = new GradientAlphaKey(0f, 1f);
        flashGradient.SetKeys(flashColors, flashAlphas);
        flashCol.color = flashGradient;

        ParticleSystem.SizeOverLifetimeModule flashSize = flash.sizeOverLifetime;
        flashSize.enabled = true;
        flashSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.2f));

        ParticleSystemRenderer flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
        flashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static ParticleSystem Particles(string name, Material material, int most)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.maxParticles = most;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private bool Looks(int e, Vector3 from, Vector3 to, Vector3 going, float speed)
    {
        Landed l;
        Landed onBody;
        if (item.Hand > -1)
        {
            if (Physics.Linecast(transform.position, to, -0x1705, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }
        Vector3 d = to - from;
        int steps = (int)(float)(int)(d.magnitude / 0.03f);
        if (steps > 5) steps = 6;
        if (steps < 2) steps = 1;
        int i = 0;
        while (true)
        {
            float t = Mathf.Clamp01((float)(i + 1) / (float)steps);
            if (LandsOn(from + d * t, going, out l))
            {
                break;
            }
            i++;
            if (steps == i)
            {
                if (!OnABody(from, to, out onBody))
                {
                    return false;
                }
                return Lands(e, onBody, to, going, speed);
            }
        }
        return Lands(e, l, to, going, speed);
    }

    private bool LandsOn(Vector3 at, Vector3 going, out Landed l)
    {
        l = default(Landed);
        KillCam cam = KillCam.Local;
        int punchesToKill;
        if (cam != null)
        {
            punchesToKill = cam.punchesToKill;
        }
        else
        {
            punchesToKill = 3;
        }

        foreach (NetworkApe a in NetworkApe.All)
        {
            if (a == null) continue;
            if (a == NetworkApe.Local) continue;
            if (a.IsMonster) continue;
            Dismemberment apeBody = a.GetComponentInChildren<Dismemberment>() /* ? */;
            if (apeBody == null) continue;
            if (a.head /* ? */ == null) continue;
            System.Reflection.FieldInfo punchesField = typeof(NetworkApe).GetField("punches", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public) /* ? */;
            int apePunches = punchesField != null ? System.Convert.ToInt32(punchesField.GetValue(a)) : 0 /* ? */;
            if (punchesToKill <= apePunches) continue;
            if ((a.head /* ? */.position - at).sqrMagnitude > 9f) continue;
            GoreSection s;
            Vector3 p;
            if (GillaWounds.On(apeBody).Hit(at, reach, going, out s, out p))
            {
                l.into = Into.Player;
                l.id = a.PlayerId /* ? */;
                l.thing = default(NetworkId);
                l.section = s;
                l.point = p;
                return true;
            }
        }

        foreach (GillaDummy d in GillaDummy.All)
        {
            if (d == null) continue;
            if (!d.Alive) continue;
            Dismemberment dummyBody = d.GetComponentInChildren<Dismemberment>() /* ? */;
            if (dummyBody == null) continue;
            Transform dummyRoot = d.transform /* ? */;
            if (dummyRoot == null) continue;
            if ((dummyRoot.position - at).sqrMagnitude > 9f) continue;
            GoreSection s;
            Vector3 p;
            if (GillaWounds.On(dummyBody).Hit(at, reach, going, out s, out p))
            {
                int index = GillaDummy.All.IndexOf(d);
                NetworkId thing = default(NetworkId);
                if (d.Object != null && d.Object.IsValid /* ? */)
                {
                    thing = d.Object.Id;
                }
                l.into = Into.Dummy;
                l.id = index;
                l.thing = thing;
                l.section = s;
                l.point = p;
                return true;
            }
        }

        foreach (MonsterBrain m in MonsterBrain.All)
        {
            if (m == null) continue;
            if (!m.isActiveAndEnabled /* ? */) continue;
            GillaWounds monsterWounds = m.GetComponentInChildren<GillaWounds>() /* ? */;
            if (monsterWounds == null) continue;
            if (!monsterWounds.isActiveAndEnabled /* ? */) continue;
            if ((m.Middle - at).sqrMagnitude > 36f) continue;
            GoreSection s;
            Vector3 p;
            if (monsterWounds.Hit(at, reach, going, out s, out p) /* ? */)
            {
                int index = MonsterBrain.All.IndexOf(m);
                NetworkId thing = default(NetworkId);
                if (m.net /* ? */ != null && m.net /* ? */.Live)
                {
                    thing = m.net /* ? */.Object.Id;
                }
                l.into = Into.Monster;
                l.id = index;
                l.thing = thing;
                l.section = s;
                l.point = p;
                return true;
            }
        }
        return false;
    }

    private static bool OnABody(Vector3 from, Vector3 to, out Landed l)
    {
        l = default(Landed);
        Vector3 d = to - from;
        float distance = d.magnitude;
        Vector3 dir = Vector3.zero;
        if (distance > 1e-05f)
        {
            dir = d / distance;
        }
        foreach (GillaCorpse corpse in GillaCorpse.All)
        {
            if (corpse == null) continue;
            if ((corpse.MiddleNow - to).sqrMagnitude > 16f) continue;
            for (int part = 0; part != 4; part++)
            {
                if (part != 0 && !corpse.IsOff(part)) continue;
                Transform t = corpse.PartTransform(part);
                if (t == null) continue;
                t.GetComponentsInChildren<Collider>(found);
                foreach (Collider c in found)
                {
                    if (c == null) continue;
                    if (!c.enabled) continue;
                    Vector3 p;
                    RaycastHit hit;
                    if (c.Raycast(new Ray(from, dir), out hit, distance))
                    {
                        p = hit.point;
                    }
                    else
                    {
                        if ((c.ClosestPoint(to) - to).sqrMagnitude >= 1e-06f) continue;
                        p = to;
                    }
                    l.into = Into.Body;
                    l.id = corpse.Id;
                    l.thing = default(NetworkId);
                    l.section = (GoreSection)SectionOf(c.transform);
                    l.point = p;
                    return true;
                }
            }
        }
        return false;
    }

    private static byte SectionOf(Transform t)
    {
        string name = t.name;
        if (name.IndexOf("head", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return 0;
        }
        if (name.IndexOf("arm", System.StringComparison.OrdinalIgnoreCase) < 0 &&
            name.IndexOf("hand", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return 1;
        }
        if (name.EndsWith("_R") || name.EndsWith("ArmR"))
        {
            return 3;
        }
        return 2;
    }

    private bool Lands(int e, Landed l, Vector3 at, Vector3 going, float speed)
    {
        Vector3 axis = transform.TransformDirection(Along(e));
        float hard = 0f;
        if (pushSpeed != hardSpeed)
        {
            hard = Mathf.Clamp01((speed - pushSpeed) / (hardSpeed - pushSpeed));
        }
        Vector3 dir = going.normalized;
        if (Vector3.Dot(axis, dir) >= inLine)
        {
            if (speed < stabSpeed)
            {
                return false;
            }
            float scale = 1f;
            if (e == 1)
            {
                scale = chiselIn;
            }
            float howFar = Mathf.Lerp(firstIn.x, firstIn.y, Mathf.Clamp01(hard)) * scale;
            if (Live)
            {
                RPC_In((byte)e, (byte)l.into, l.id, l.thing, (byte)l.section, l.point, axis, howFar, hard);
            }
            else
            {
                In(e, (byte)l.into, l.id, l.thing, (byte)l.section, l.point, axis, howFar, hard);
            }
        }
        else
        {
            if (speed < knockSpeed)
            {
                return false;
            }
            againAt = Time.time + 0.3f;
            if (Live)
            {
                RPC_Knock(at, hard);
            }
            else
            {
                Knock(at, hard);
            }
        }
        return true;
    }

    private void Scrapes(int e, Vector3 tip)
    {
        Skin s;
        if (!OnSkin(tip, out s))
        {
            scraping[e] = false;
            return;
        }
        if (s.inside > 0f)
        {
            Transform tr = transform;
            tr.position = tr.position + s.normal * s.inside;
        }
        if (scraping[e] && s.into == scrapedFrom[e].into && scrapedFrom[e].id == s.id)
        {
            GoreSection section = scrapedFrom[e].section;
            if (section == s.section)
            {
                Vector3 was = scrapedFrom[e].world;
                Vector3 fromBody = scrapedFrom[e].body;
                GillaWounds wounds = scrapedFrom[e].wounds;
                GillaCorpse corpse = scrapedFrom[e].corpse;
                if (corpse != null)
                {
                    was = corpse.WorldOn(section, fromBody);
                }
                else if (wounds != null)
                {
                    was = wounds.WorldOn(section, fromBody);
                }
                float moved = (s.world - was).magnitude;
                if (moved < 0.025f)
                {
                    return;
                }
                scrapedFrom[e] = s;
                if (moved > 0.25f)
                {
                    return;
                }
                if (Live)
                {
                    RPC_Scratch((byte)s.into, s.id, s.thing, (byte)s.section, fromBody, s.body);
                }
                else
                {
                    Scratch((byte)s.into, s.id, s.thing, (byte)s.section, fromBody, s.body);
                }
                Buzz(0.22f, 0.04f);
                return;
            }
        }
        scraping[e] = true;
        scrapedFrom[e] = s;
    }
}
