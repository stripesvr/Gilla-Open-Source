// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.XR;

public partial class GillaPickaxe : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Out(NetworkBool yanked)
    {
        Out(yanked, true);
    }

    private void Out(bool yanked, bool seen)
    {
        if (into == Into.Nothing)
        {
            return;
        }
        if (seen && on != null)
        {
            Vector3 p = on.TransformPoint(onLocal);
            Vector3 dir = transform.rotation * Along(end);
            float volume = 1f;
            if (!yanked)
            {
                volume = Mathf.Clamp01(depth / 0.1f) * 0.39999998f + 0.5f;
            }
            Sfx.Play(pullSounds, p, volume, 0.94f, 1.06f);
            if (harmed)
            {
                BloodAt(p, -dir, yanked ? 2.4f : 1.6f, yanked ? 34 : 16, yanked ? 3 : 2, yanked);
            }
            Buzz(yanked ? 0.9f : 0.5f, 0.1f);
            if (harmed)
            {
                Drips();
            }
        }
        into = Into.Nothing;
        item.Stuck = false;
        on = null;
        pulls = null;
        ape = null;
        copy = null;
        dummy = null;
        monster = null;
        if (Live && Object != null && Object.HasStateAuthority /* ? */)
        {
            Pulling = false;
            Depth = 0f;
        }
        item.Pinned = false;
        if (item.Hand >= 0 && item.Mine)
        {
            comeFrom = transform.position;
            comeTurn = transform.rotation;
            comeFor = 0.12f;
        }
        else if (item.Hand < 0)
        {
            Collider[] colliders = item.Colliders;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            if (item.Mine)
            {
                item.Body.isKinematic = false;
                item.Body.interpolation = RigidbodyInterpolation.Interpolate;
                item.Body.WakeUp();
            }
        }
        againAt = Time.time + 0.4f;
        Remember();
    }

    private void Drips()
    {
        Material mat = Spray;
        if (mat == null)
        {
            return;
        }
        GameObject go = new GameObject("PickDrip");
        Transform t = go.transform;
        t.SetParent(transform, false);
        t.localPosition = end != 0 ? edgeTip : spikeTip;
        UnityEngine.Object.Destroy(t.gameObject, 4f);
        Blood.Spray(t, Vector3.down, mat, 1.4f, 3, 3, 0.3f);
    }

    private void CameFrom(float dt)
    {
        float t = 1f - comeFor / 0.12f;
        Transform tr = transform;
        Vector3 pos = transform.position;
        Quaternion rot = Quaternion.Slerp(comeTurn, transform.rotation, t);
        tr.SetPositionAndRotation(Vector3.Lerp(comeFrom, pos, t), rot);
        comeFor -= dt;
    }

    private void WetsTo(int e, float deep)
    {
        if (wetTo[e] < deep + 0.012f)
        {
            wetTo[e] = deep + 0.012f;
            wetAt = Time.time;
            wetChanged = true;
        }
    }

    private void Wets()
    {
        if (wet == null || wetAt < 0f || (!wetChanged && Time.time - wetSaid < 0.5f))
        {
            return;
        }
        wetChanged = false;
        wetSaid = Time.time;
        if (props == null)
        {
            props = new MaterialPropertyBlock();
            GillaStains.LayNoise();
        }
        wet.enabled = true;
        props.SetVector(TipId, new Vector4(tipOnMesh.x, tipOnMesh.y, tipOnMesh.z, wetTo[0] * perMesh));
        props.SetVector(EdgeId, new Vector4(edgeOnMesh.x, edgeOnMesh.y, edgeOnMesh.z, wetTo[1] * perMesh));
        props.SetFloat(DryId, Mathf.Clamp01((Time.time - wetAt) / 45f));
        wet.SetPropertyBlock(props);
    }

    private void Buzz(float strength, float seconds)
    {
        XRNode node;
        int hand = item.Hand;
        if (hand == 0)
        {
            node = XRNode.LeftHand;
        }
        else
        {
            if (hand != 1)
            {
                return;
            }
            node = XRNode.RightHand;
        }
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        device.SendHapticImpulse(0, strength, seconds);
    }

    public GillaPickaxe()
    {
        balance = new Vector3(0f, 0.5f, 0f);
        hardSpeed = 6f;
        knockSpeed = 2f;
        inLine = 0.6f;
        pushSpeed = 0.7f;
        stabSpeed = 2.2f;
        glassSpeed = 2.5f;
        firstIn = new Vector2(0.012f, 0.13f);
        skullStops = 0.09f;
        holeRadius = 0.016f;
        chiselIn = 0.45f;
        countsAt = 0.03f;
        reach = 0.025f;
        pullRate = 0.75f;
        leverMost = 28f;
        chiselRadius = 0.022f;
        pushRate = 0.3f;
        monsterHeadHarm = 160f;
        hiddenSince = -1f;
        bodyYanksAt = 0.55f;
        monsterHarm = 45f;
        bodyLeverMost = 45f;
        yanksAt = 0.24f;
        was = new Vector3[2];
        swung = new Vector3[2];
        swungAt = new float[2];
        wetTo = new float[2];
        heldWas = -1;
        wetAt = -1f;
        wetSaid = -100f;
        perMesh = 1f;
        scraping = new bool[2];
        scrapedFrom = new Skin[2];
    }

    static GillaPickaxe()
    {
        TipId = Shader.PropertyToID("_Tip");
        EdgeId = Shader.PropertyToID("_Edge");
        DryId = Shader.PropertyToID("_Dry");
        found = new List<Collider>();
    }
}
