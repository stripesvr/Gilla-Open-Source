// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GillaDoor : MonoBehaviour
{
    static GillaDoor()
    {
        ModeBy = -1;
        modeRound = -1;
        roundSeenAt = -1;
        KnockIn = new float[] { 0f, 0.025f, 0.06f };
        KnockOver = new float[] { 0f, 1.2f, 3f };
        By = -1;
        doors = new List<GillaDoor>();
        PryT = new float[11] /* ? */;
        PryH = new float[11] /* ? */;
    }

    public GillaDoor()
    {
        heldBy = -1;
        shakeFrom = -1f;
        shutTurn = Quaternion.identity;
        rise = 2.25f;
        seconds = 1.3f;
        blownResetSeconds = 15f;
        prySparksPerSecond = 45f;
        prySparksPerSpeed = 160f;
        tall = 2.7f;
        seam = 1.24f;
        toldAtRest = true;
        heldRot = Quaternion.identity;
        top = 2.5f;
        Width = 2.8f;
        Thickness = 0.3f;
        Through = Vector3.forward;
    }

    private Rigidbody MakeLoose()
    {
        if (flying != null)
        {
            Destroy(flying);
        }
        flying = Instantiate(panel.gameObject, panel.position, panel.rotation);
        flying.name = name + "_Blown";
        Transform t = flying.transform;
        t.localScale = panel.lossyScale;
        foreach (Renderer r in flying.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
        }
        foreach (MonoBehaviour mb in flying.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Destroy(mb);
        }
        foreach (AudioSource src in flying.GetComponentsInChildren<AudioSource>(true))
        {
            Destroy(src);
        }
        foreach (Transform child in flying.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = 2;
        }
        foreach (Collider col in flying.GetComponentsInChildren<Collider>(true))
        {
            DestroyImmediate(col);
        }
        Transform ft = flying.transform;
        Vector3 through = Through;
        Vector3 up = Vector3.up;
        float h = Height();
        BoxCollider box = flying.AddComponent<BoxCollider>();
        float bottom = rise * h + 0.02f;
        float height = (top - bottom) - 0.04f;
        if (height <= 0.3f)
        {
            height = 0.3f;
        }
        float d = Vector3.Dot(panel.position - Foot, Through);
        Vector3 c = Foot + Through * d;
        c.y += height * 0.5f + bottom;
        box.center = ft.InverseTransformPoint(c);
        Vector3 a = ft.InverseTransformVector(Vector3.Cross(up, through) * (seam + seam - 0.06f));
        Vector3 b = ft.InverseTransformVector(Vector3.up * height);
        Vector3 e = ft.InverseTransformVector(Through * Thickness);
        box.size = Abs(a) + Abs(b) + Abs(e);
        int mask = LayerMask.GetMask(new string[] { "PlayerRig", "FirstPerson", "MirrorOnly", "Buttons", "Apes" });
        box.excludeLayers = mask;
        var player = GorillaLocomotion.Player.Instance /* ? */;
        if (player != null)
        {
            foreach (Collider pc in player.GetComponentsInChildren<Collider>(true))
            {
                Physics.IgnoreCollision(box, pc);
            }
        }
        Vector3 foot = Foot;
        float halfUp = (top + 0.3f + 0.7f) * 0.5f;
        Quaternion turn = Quaternion.LookRotation(Through, Vector3.up);
        Vector3 center = foot + Vector3.up * halfUp;
        Vector3 halfExtents = new Vector3(seam + 0.8f, (top + 0.4f) * 0.5f, Thickness * 0.5f + 0.5f);
        Collider[] frame = Physics.OverlapBox(center, halfExtents, turn, ~mask, QueryTriggerInteraction.Ignore);
        foreach (Collider other in frame)
        {
            if (other != box)
            {
                Physics.IgnoreCollision(box, other, true);
            }
        }
        if (Application.isPlaying)
        {
            StartCoroutine(Unignore(box, frame, 0.35f));
        }
        Rigidbody body = flying.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = flying.AddComponent<Rigidbody>();
        }
        body.isKinematic = false;
        body.useGravity = true;
        body.excludeLayers = mask;
        body.mass = 150f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return body;
    }

    private static Vector3 Abs(Vector3 v)
    {
        return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    private void PlayBlow()
    {
        AudioClip[] clips = blowSounds;
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        GameObject go = new GameObject("Sfx_" + clip.name);
        go.transform.position = Foot + Vector3.up * 1.2f;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.pitch = Random.Range(0.94f, 1.06f);
        src.spatialBlend = 1f;
        src.minDistance = 6f;
        src.maxDistance = 60f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0f;
        src.Play();
        Destroy(go, clip.length + 0.2f);
    }

    private void BlastSparks(Vector3 away)
    {
        ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
        if (prySparks == null)
        {
            prySparks = MakeSparks("PrySparks", 450,
                new Color(1f, 0.85f, 0.12f, 1f),
                new Color(1f, 0.68f, 0.03f, 1f),
                new Color(0.6f, 0.26f, 0.01f, 1f));
        }
        if (prySparks == null) return;
        ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < 140; i++)
        {
            float r = Random.value;
            float up = Random.Range(0.05f, top);
            float kick = Random.Range(1.5f, 4f);
            float c = 1f;
            if (0.5f <= r)
            {
                c = -1f;
            }
            Seam(c, up, kick, 1f, ref ep);
            ep.velocity = ep.velocity + away * Random.Range(2f, 6f);
            ep.startLifetime = Random.Range(0.5f, 1.4f);
            ep.startSize = Random.Range(0.02f, 0.045f);
            prySparks.Emit(ep, 1);
        }
    }

    private ParticleSystem MakeSparks(string name, int max, Color hot, Color warm, Color cool)
    {
        if (sparkMaterial == null)
        {
            sparkMaterial = Resources.Load<Material>("Gilla_Spark");
        }
        if (sparkMaterial == null)
        {
            return null;
        }
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.gravityModifier = 1.3f;
        main.maxParticles = max;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(hot, 0f);
        colorKeys[1] = new GradientColorKey(warm, 0.35f);
        colorKeys[2] = new GradientColorKey(cool, 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        g.SetKeys(colorKeys, alphaKeys);
        col.color = g;
        ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        ParticleSystem.CollisionModule collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.quality = ParticleSystemCollisionQuality.Low;
        collision.collidesWith = -5893;
        collision.bounce = 0.35f;
        collision.dampen = 0.45f;
        collision.lifetimeLoss = 0.2f;
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Stretch;
        psr.velocityScale = 0.035f;
        psr.lengthScale = 1.2f;
        psr.sharedMaterial = sparkMaterial;
        psr.shadowCastingMode = ShadowCastingMode.Off;
        psr.receiveShadows = false;
        ps.Play();
        return ps;
    }

    private void PlayPry()
    {
        AudioClip[] clips = prySounds;
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;
        GameObject go = new GameObject("Sfx_" + clip.name);
        go.transform.SetParent(panel, false);
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f;
        src.minDistance = 4f;
        src.maxDistance = 40f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0f;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }
}
