// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Fusion;

public partial class GillaSplashDirector : MonoBehaviour
{
    static GillaSplashDirector()
    {
        SunkId = Shader.PropertyToID("_GillaSunk");
    }

    public GillaSplashDirector()
    {
        pools = new GillaSplashPool[0];
        kinds = new GameObject[0];
        decks = new Material[0];
        sides = new Material[0];
        drag = 1.6f;
        noiseEvery = 0.25f;
        local = new List<GameObject>();
        wasIn = new bool[4];
        lastAt = new Vector3[4];
        nextWake = new float[4];
        others = new Dictionary<Transform, (Vector3 at, float next)>();
        asked = new float[128];
        sunk = new float[128];
        standing = new List<GameObject>();
        standingKinds = new List<int>();
        sinkers = new List<GillaSinkPlatform>();
        hallGroups = new Transform[0];
        hallsOut = int.MinValue;
        limbs = new Dictionary<GillaSplashMonster, Transform[]>();
        wading = new Dictionary<Transform, (Vector3 at, float next)>();
    }

    private void Sinking()
    {
        GillaSplashGame game = GillaSplashGame.Current;
        bool off;
        bool phaseTwo;
        if (game != null && game.Live && game.Sinking)
        {
            phaseTwo = game.Phase == 2;
            off = false;
        }
        else
        {
            phaseTwo = false;
            off = true;
        }

        bool dirty = false;
        bool moved = false;
        foreach (GillaSinkPlatform p in sinkers)
        {
            if (p == null) continue;
            float t;
            int round;
            if (off)
            {
                t = -1f;
                round = 2;
            }
            else
            {
                t = -1f;
                if (p.index /* ? */ < 128)
                {
                    if (0f < game.SinkAt[p.index])
                    {
                        t = game.Now - game.SinkAt[p.index];
                    }
                }
                round = game.Round;
            }
            moved |= p.Show(t, round);
            if (p.index < sunk.Length)
            {
                int r = off ? 2 : game.Round;
                float v = 0f;
                if (0f <= t)
                {
                    v = Mathf.Clamp01((GillaSinkPlatform.At(t, 0, r).Item1 - 0.2f) / 0.15f);
                }
                if (!Mathf.Approximately(sunk[p.index], v))
                {
                    dirty = true;
                    sunk[p.index] = v;
                }
            }
        }
        if (moved)
        {
            Physics.SyncTransforms();
        }
        if (dirty)
        {
            Shader.SetGlobalFloatArray(SunkId, sunk);
        }
        if (!phaseTwo) return;

        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance /* ? */;
        if (player == null) return;
        if (player.bodyCollider == null) return;
        if (KillCam.Local != null && KillCam.Local.Busy) return;

        for (int i = 3; i != 0; i--)
        {
            Vector3 origin;
            float dist;
            if (i == 1)
            {
                CapsuleCollider body = player.bodyCollider;
                origin = body.transform.TransformPoint(body.center);
                dist = body.height * 0.5f * body.transform.lossyScale.y + 0.35f;
            }
            else
            {
                if (!player.IsHandTouching(i == 3)) continue;
                bool disabled;
                Transform follower;
                if (i == 3)
                {
                    disabled = player.leftHandDisabled;
                    follower = player.leftHandFollower;
                }
                else
                {
                    disabled = player.rightHandDisabled;
                    follower = player.rightHandFollower;
                }
                if (disabled) continue;
                origin = follower.position + Vector3.up * 0.05f;
                dist = 0.2f;
            }

            RaycastHit hit;
            if (Physics.Raycast(origin, Vector3.down, out hit, dist, ~0x1700, QueryTriggerInteraction.Ignore))
            {
                GillaSinkPlatform platform = hit.collider.GetComponentInParent<GillaSinkPlatform>();
                if (platform != null && platform.index < 128)
                {
                    if (game.SinkAt[platform.index] <= 0f)
                    {
                        if (asked[platform.index] <= Time.time)
                        {
                            asked[platform.index] = Time.time + 0.5f;
                            game.RPC_Stood(platform.index);
                        }
                    }
                }
            }
        }
    }

    private void Spawning()
    {
        if (monsterPrefab == null) return;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        NetworkRunner runner = null;
        if (boot != null)
        {
            runner = boot.Runner /* ? */;
        }
        if (runner != null && runner.IsRunning)
        {
            offlineFor = 0f;
            ClearLocal();
            if (boot.Joining) return;
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 2f;
            if (!runner.IsSharedModeMasterClient) return;
            if (!boot.Settled) return;
            if (boot.Kind != NetworkBootstrap.Lobby.Splash) return;
            MakeMissing(runner);
            return;
        }
        if (boot != null && boot.Joining)
        {
            offlineFor = 0f;
            return;
        }
        offlineFor += Time.deltaTime;
        if (local.Count < 1)
        {
            float wait = boot != null ? 2f : 0f;
            if (wait <= offlineFor)
            {
                if (built == 0)
                {
                    built = -1;
                    Put(GillaSplashLayout.Make(Halls, 0, 1, new System.Random()));
                }
                for (int i = 0; i < pools.Length; i++)
                {
                    GillaSplashPool pool = pools[i];
                    if (pool == null) continue;
                    if (pool.monsterStart == null) continue;
                    GameObject go = Instantiate(monsterPrefab, pool.monsterStart.position, pool.monsterStart.rotation);
                    go.GetComponent<GillaSplashMonster>().PoolIndex = pool.index;
                    local.Add(go);
                }
            }
        }
    }

    private void ClearLocal()
    {
        foreach (GameObject go in local)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }
        local.Clear();
    }

    private void MakeMissing(NetworkRunner runner)
    {
        HashSet<int> have = new HashSet<int>();
        foreach (GillaSplashMonster m in GillaSplashMonster.All)
        {
            if (m.Live)
            {
                have.Add(m.PoolIndex);
            }
        }
        NetworkObject prefab;
        if (monsterPrefab.TryGetComponent<NetworkObject>(out prefab))
        {
            int count = 0;
            for (int i = 0; i < pools.Length; i++)
            {
                GillaSplashPool pool = pools[i];
                if (pool == null) continue;
                if (pool.monsterStart == null) continue;
                if (have.Contains(pool.index)) continue;
                byte index = (byte)pool.index;
                NetworkObject spawned = runner.Spawn(prefab, pool.monsterStart.position, pool.monsterStart.rotation, null,
                    (r, o) => MakeMissing_b__69_0(index, r, o), (NetworkSpawnFlags)2);
                if (spawned != null)
                {
                    count++;
                }
            }
            if (0 < count)
            {
                Debug.Log(string.Format("[GillaSplashDirector] spawned {0} water monsters on the network in '{1}', as its master client", count, runner.SessionInfo.Name));
            }
        }
        else
        {
            Debug.LogError("[GillaSplashDirector] the monster prefab has no NetworkObject");
        }
    }

    public static GillaSplashMonster MonsterIn(int index)
    {
        foreach (GillaSplashMonster m in GillaSplashMonster.All)
        {
            if (m == null) continue;
            if (m.PoolIndex == index && m.Live)
            {
                return m;
            }
        }
        return null;
    }

    private void You(float dt)
    {
        GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance /* ? */;
        if (player == null) return;
        if (player.headCollider == null) return;
        if (player.bodyCollider == null) return;
        bool busy = KillCam.Local != null ? KillCam.Local.Busy : false;

        CapsuleCollider body = player.bodyCollider;
        Vector3 headPos = player.headCollider.transform.position;
        Vector3 center = body.transform.TransformPoint(body.center);
        Vector3 up = body.transform.up;
        float half = body.height * 0.5f * body.transform.lossyScale.y;
        Vector3 leftPos = player.leftHandFollower.position;
        Vector3 rightPos = player.rightHandFollower.position;

        Vector3[] pts = new Vector3[4];
        pts[0] = center - up * half;
        pts[1] = leftPos;
        pts[2] = rightPos;
        pts[3] = headPos;

        Vector3 noiseAt = pts[0];
        GillaSplashPool youPool = null;
        bool bodyIn = false;
        bool headIn = false;
        bool any = false;

        for (int i = 0; i < 4; i++)
        {
            Vector3 p = pts[i];
            Vector3 vel;
            if (dt <= 0f)
            {
                vel = Vector3.zero;
            }
            else
            {
                vel = (p - lastAt[i]) / dt;
            }
            lastAt[i] = p;

            GillaSplashPool pool;
            if (i == 2 && player.rightHandDisabled)
            {
                pool = null;
            }
            else if (i == 1 && player.leftHandDisabled)
            {
                pool = null;
            }
            else
            {
                pool = GillaSplashPool.At(p);
            }

            if (pool != null && pool.In(p, i != 3 ? 0.02f : 0f))
            {
                bool skip = busy || wasIn[i];
                if (i != 3 && !skip)
                {
                    float s = Mathf.Clamp01(vel.y * -0.25f);
                    float strength = i == 0 ? s * 0.45f + 0.55f : s * 0.38f + 0.22f;
                    Heard(pool, p, strength, true);
                }
                wasIn[i] = true;
                if (!(busy || i == 3))
                {
                    Wake(p, p - vel * dt, dt, ref nextWake[i]);
                }
                bodyIn |= i == 0;
                headIn |= i == 3;
                if (i == 0 || youPool == null)
                {
                    noiseAt = p;
                    youPool = pool;
                }
                any = true;
            }
            else
            {
                wasIn[i] = false;
            }
        }

        if (!busy && any)
        {
            if (nextNoise <= Time.time)
            {
                float d = Vector3.Distance(Flat(noiseAt), Flat(lastNoise));
                if (0.25f < d || nextNoise + 0.75f <= Time.time)
                {
                    float s = Mathf.Clamp(d / (noiseEvery * 5f), 0f, 0.45f);
                    Heard(youPool, noiseAt, s, false);
                    lastNoise = noiseAt;
                    nextNoise = Time.time + noiseEvery;
                }
            }
            if (bodyIn)
            {
                player.Slow(Mathf.Exp(-(drag * dt)));
            }
            GillaSplashGame game = GillaSplashGame.Current;
            if (!MonsterBrain.IgnoreLocal)
            {
                if (game == null || !game.Live || game.InRace(game.Runner.LocalPlayer))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (!wasIn[i]) continue;
                        GillaSplashPool pool = GillaSplashPool.At(pts[i]);
                        GillaSplashMonster monster = null;
                        if (pool != null)
                        {
                            monster = MonsterIn(pool.index);
                        }
                        if (monster != null && monster.Hunting)
                        {
                            Vector3 mouth = monster.Mouth;
                            float dx = mouth.x - pts[i].x;
                            float dz = mouth.z - pts[i].z;
                            if (Mathf.Sqrt(dx * dx + dz * dz) < monster.reach)
                            {
                                monster.Catch();
                                break;
                            }
                        }
                    }
                }
            }
        }

        Underwater(headIn && !busy, player.headCollider.transform);
    }

    private void Heard(GillaSplashPool pool, Vector3 at, float strength, bool entering)
    {
        GillaSplashMonster monster = MonsterIn(pool.index);
        if (monster == null)
        {
            Show(at, strength, entering);
            return;
        }
        monster.Splash(at, strength, entering);
    }

    public static void Show(Vector3 at, float strength, bool entering)
    {
        GillaSplashPool pool = GillaSplashPool.At(at);
        if (pool != null)
        {
            at.y = pool.Surface;
        }
        if (0.05f < strength)
        {
            GillaSplashPool.Ripple(at, strength);
        }
        if (entering)
        {
            GillaVoxelSplash.Emit(at, strength);
        }
        GillaSplashDirector d = Instance;
        if (d == null) return;
        AudioClip[] clips;
        float volume;
        float pitchMax;
        if (entering)
        {
            clips = 0.5f <= strength ? d.splashSounds : d.dipSounds;
            volume = Mathf.Clamp01(strength) * 0.55f + 0.45f;
            pitchMax = 1.12f;
        }
        else
        {
            if (strength <= 0.12f) return;
            clips = d.wadeSounds;
            pitchMax = 1.1f;
            volume = strength * 0.8f + 0.3f;
        }
        Sfx.Play(clips, at, volume, 0.9f, pitchMax);
    }

    private void Underwater(bool under, Transform head)
    {
        if (murk == null && under)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Underwater";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(head, false);
            go.transform.localScale = Vector3.one * 0.32f;
            go.layer = head.gameObject.layer;
            murkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            murkMat.SetFloat("_Surface", 1f);
            murkMat.SetFloat("_Cull", 0f);
            murkMat.SetFloat("_ZTest", 8f);
            murkMat.SetInt("_SrcBlend", 5);
            murkMat.SetInt("_DstBlend", 10);
            murkMat.SetInt("_ZWrite", 0);
            murkMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            murkMat.renderQueue = 4990;
            murk = go.GetComponent<Renderer>();
            murk.sharedMaterial = murkMat;
            murk.shadowCastingMode = ShadowCastingMode.Off;
        }
        if (murk != null)
        {
            murk.enabled = under;
            GillaSplashPool pool = null;
            if (under)
            {
                pool = GillaSplashPool.At(head.position);
            }
            if (pool != null)
            {
                murkMat.SetColor("_BaseColor", pool.murk);
            }
        }
        if (muffle == null && under)
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (listener != null)
            {
                if (!listener.TryGetComponent<AudioLowPassFilter>(out muffle))
                {
                    muffle = listener.gameObject.AddComponent<AudioLowPassFilter>();
                }
            }
        }
        if (muffle != null)
        {
            muffle.cutoffFrequency = under ? 900f : 22000f;
        }
    }

    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}
