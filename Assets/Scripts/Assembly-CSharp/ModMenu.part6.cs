// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Rendering;

public partial class ModMenu : MonoBehaviour
{
    private void SpawnItem(GameObject thing, string says)
    {
        if (thing == null)
        {
            toast = "NOT IN THIS BUILD";
            toastUntil = Time.unscaledTime + 2.2f;
            return;
        }
        if (GillaItem.All.Count >= ItemsAtMost)
        {
            toast = "THAT'S PLENTY";
            toastUntil = Time.unscaledTime + 2.2f;
            return;
        }

        Transform head = Head();
        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }
        Vector3 fwd = flat.normalized;
        Vector3 jitter = Vector3.ProjectOnPlane(UnityEngine.Random.insideUnitSphere, Vector3.up);
        Vector3 pos = head.position + fwd * 0.45f + jitter * 0.22f + Vector3.down * 0.3f;
        Vector3 toPos = pos - head.position;
        float dist = toPos.magnitude;
        RaycastHit hit;
        if (Physics.SphereCast(head.position, 0.06f, toPos.normalized, out hit, dist, SpawnMask, QueryTriggerInteraction.Ignore))
        {
            pos = hit.point + hit.normal * 0.08f;
        }
        Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(fwd, Vector3.up), Vector3.up);

        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = boot != null ? boot.Runner /* ? */ : null;
        if (runner != null && runner.IsRunning)
        {
            if (boot.Joining)
            {
                toast = "STILL JOINING - TRY AGAIN";
                toastUntil = Time.unscaledTime + 2.2f;
                return;
            }
            NetworkObject netObj;
            if (!thing.TryGetComponent<NetworkObject>(out netObj))
            {
                Debug.LogError("[ModMenu] " + thing.name + " has no NetworkObject");
                return;
            }
            NetworkObject spawned = runner.Spawn(netObj, new Vector3?(pos), new Quaternion?(rotation), null, null, (NetworkSpawnFlags)4);
            if (spawned == null)
            {
                Debug.LogError("[ModMenu] the network didn't take " + thing.name);
                return;
            }
        }
        else
        {
            GameObject made = Instantiate<GameObject>(thing, pos, rotation);
            made.name = thing.name;
        }
        toast = says;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void SpawnDummy()
    {
        Transform head = Head();
        GillaDummy dummy = PutDummy(head, transform.forward, Toast);
        if (dummy != null)
        {
            toast = "DUMMY SPAWNED";
            toastUntil = Time.unscaledTime + 2.2f;
        }
    }

    public static GillaDummy PutDummy(Transform head, Vector3 otherwise, Action<string> say)
    {
        GillaSpawnables spawnables = GillaSpawnables.Get();
        GameObject prefab = spawnables != null ? spawnables.dummy : null;
        if (prefab == null)
        {
            if (say != null) say("NO DUMMY IN THIS BUILD");
            return null;
        }

        Vector3 flat = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = Vector3.ProjectOnPlane(otherwise, Vector3.up);
        }
        Vector3 fwd = flat.normalized;
        RaycastHit wallHit;
        bool blocked = Physics.SphereCast(head.position, 0.25f, fwd, out wallHit, 2.2f, SpawnMask, QueryTriggerInteraction.Ignore);
        float along = Mathf.Max(0.6f, wallHit.distance - 0.4f);
        if (!blocked) along = 1.8f;

        RaycastHit floor;
        if (!Physics.Raycast(head.position + fwd * along, Vector3.down, out floor, 6f, SpawnMask, QueryTriggerInteraction.Ignore))
        {
            if (say != null) say("NO FLOOR THERE");
            return null;
        }
        Quaternion rotation = Quaternion.LookRotation(-fwd, Vector3.up);

        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = boot != null ? boot.Runner /* ? */ : null;
        if (runner != null && runner.IsRunning)
        {
            if (boot.Joining)
            {
                if (say != null) say("STILL JOINING - TRY AGAIN");
                return null;
            }
            NetworkObject netObj;
            if (!prefab.TryGetComponent<NetworkObject>(out netObj))
            {
                Debug.LogError("[ModMenu] " + prefab.name + " has no NetworkObject");
                return null;
            }
            NetworkObject spawned = runner.Spawn(netObj, new Vector3?(floor.point), new Quaternion?(rotation), null, null, (NetworkSpawnFlags)4);
            if (spawned != null)
            {
                return spawned.GetComponent<GillaDummy>();
            }
            Debug.LogError("[ModMenu] the network didn't take " + prefab.name);
            return null;
        }
        GameObject made = Instantiate<GameObject>(prefab, floor.point, rotation);
        made.name = prefab.name;
        return made.GetComponent<GillaDummy>();
    }

    private void ClearDummies()
    {
        GillaDummy[] all = GillaDummy.All.ToArray();
        int cleared = 0;
        for (int i = 0; i < all.Length; i++)
        {
            GillaDummy dummy = all[i];
            if (dummy == null) continue;
            if (!dummy.Mine) continue;
            if (dummy.Object != null && dummy.Object.Runner && dummy.Object.Runner.Exists(dummy.Object) /* ? */)
            {
                dummy.Runner.Despawn(dummy.Object);
            }
            else
            {
                Destroy(dummy.gameObject);
            }
            cleared++;
        }
        string msg = "NO DUMMIES OF YOURS";
        if (cleared > 0)
        {
            msg = "CLEARED " + cleared.ToString();
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private static Material ParticleMat()
    {
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline != null)
        {
            return pipeline.defaultParticleMaterial;
        }
        return null;
    }

    public static void Burst(Vector3 at, int count, float speed, float size, float life, float gravity)
    {
        Material mat = ParticleMat();
        if (mat == null) return;

        GameObject go = new GameObject("ModMenu Burst");
        go.transform.position = at;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.2831855f);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count;
        main.stopAction = ParticleSystemStopAction.Destroy;

        Gradient rainbow = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0.25f, 0.2f, 1f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.75f, 0.1f, 1f), 0.25f);
        colorKeys[2] = new GradientColorKey(new Color(0.3f, 1f, 0.4f, 1f), 0.5f);
        colorKeys[3] = new GradientColorKey(new Color(0.25f, 0.6f, 1f, 1f), 0.75f);
        colorKeys[4] = new GradientColorKey(new Color(0.95f, 0.3f, 1f, 1f), 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        rainbow.SetKeys(colorKeys, alphaKeys);
        ParticleSystem.MinMaxGradient startColor = new ParticleSystem.MinMaxGradient(rainbow);
        startColor.mode = ParticleSystemGradientMode.RandomColor;
        main.startColor = startColor;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        GradientColorKey[] fadeColors = new GradientColorKey[2];
        fadeColors[0] = new GradientColorKey(new Color(1f, 1f, 1f, 1f), 0f);
        fadeColors[1] = new GradientColorKey(new Color(1f, 1f, 1f, 1f), 1f);
        GradientAlphaKey[] fadeAlphas = new GradientAlphaKey[3];
        fadeAlphas[0] = new GradientAlphaKey(1f, 0f);
        fadeAlphas[1] = new GradientAlphaKey(1f, 0.7f);
        fadeAlphas[2] = new GradientAlphaKey(0f, 1f);
        fade.SetKeys(fadeColors, fadeAlphas);
        col.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0f, (short)count);
        emission.SetBursts(bursts);

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        ps.Play();
    }

    public static GameObject Emitter(Vector3 at, float rate, float size, float life)
    {
        Material mat = ParticleMat();
        if (mat == null) return null;

        GameObject go = new GameObject("ModMenu Sparks");
        go.transform.position = at;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f, 1f), new Color(1f, 0.45f, 0.1f, 1f));
        main.gravityModifier = 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        ps.Play();
        return go;
    }

    private void SetFreeze(bool on)
    {
        MonsterBrain.Frozen = on;
        if (on)
        {
            FreezeAnimators();
            return;
        }
        foreach (KeyValuePair<Animator, float> pair in frozenAnims)
        {
            if (pair.Key != null)
            {
                pair.Key.speed = pair.Value;
            }
        }
        frozenAnims.Clear();
    }

    private void FreezeAnimators()
    {
        foreach (MonsterBrain brain in MonsterBrain.All)
        {
            if (brain == null) continue;
            if (brain.Dead /* ? */) continue;
            if (brain.animator /* ? */ == null) continue;
            if (brain.animator.speed == 0f) continue;
            Animator anim = brain.animator;
            frozenAnims[anim] = anim.speed;
            brain.animator.speed = 0f;
        }
    }

    private void KillAllMonsters()
    {
        MonsterBrain[] brains = UnityEngine.Object.FindObjectsByType<MonsterBrain>(FindObjectsSortMode.None);
        int killed = 0;
        int notMine = 0;
        for (int i = 0; i < brains.Length; i++)
        {
            MonsterBrain brain = brains[i];
            KillCam cam = KillCam.Local;
            if (cam != null && cam.Busy && cam.Killer == brain)
            {
                continue;
            }
            MonsterNet net = brain.net /* ? */;
            if (net != null && net.Live)
            {
                if (!net.Object.HasStateAuthority)
                {
                    notMine++;
                    continue;
                }
                net.Runner.Despawn(net.Object);
            }
            else
            {
                Destroy(brain.gameObject);
            }
            killed++;
        }
        string log = string.Format("[ModMenu] killed {0} monsters", killed);
        string extraLog = "";
        if (notMine > 0)
        {
            extraLog = string.Format("; {0} are the room's master client's, not yours to despawn", notMine);
        }
        Debug.Log(log + extraLog);
        string msg;
        if (notMine < 1)
        {
            msg = string.Format("KILLED {0}", killed);
        }
        else
        {
            msg = string.Format("KILLED {0} - {1} AREN'T YOURS", killed, notMine);
        }
        toast = msg;
        toastUntil = Time.unscaledTime + 2.2f;
    }

    private void SpawnMonster(MonsterCatalog.Entry e)
    {
        Vector3 pos;
        Quaternion rot;
        if (!Spot(e.prefab, out pos, out rot))
        {
            toast = "NO ROOM FOR IT HERE";
            toastUntil = Time.unscaledTime + 2.2f;
            return;
        }

        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = boot != null ? boot.Runner /* ? */ : null;
        if (runner != null && runner.IsRunning)
        {
            if (boot.Joining)
            {
                toast = "STILL JOINING - TRY AGAIN";
                toastUntil = Time.unscaledTime + 2.2f;
                return;
            }
            NetworkObject netObj;
            if (e.prefab.TryGetComponent<NetworkObject>(out netObj))
            {
                bool master = runner.IsSharedModeMasterClient;
                NetworkSpawnFlags flags = master ? (NetworkSpawnFlags)2 : (NetworkSpawnFlags)4;
                NetworkObject spawned = runner.Spawn(netObj, new Vector3?(pos), new Quaternion?(rot), null, null, flags);
                if (spawned == null)
                {
                    Debug.LogError("[ModMenu] the network didn't take " + e.prefab.name);
                    return;
                }
                Toast(Label(e) + " SPAWNED");
                return;
            }
            Debug.LogError("[ModMenu] " + e.prefab.name + " has no NetworkObject");
            return;
        }

        GameObject made = Instantiate<GameObject>(e.prefab, pos, rot);
        made.name = e.prefab.name;
        toast = Label(e) + " SPAWNED";
        toastUntil = Time.unscaledTime + 2.2f;
    }
}
