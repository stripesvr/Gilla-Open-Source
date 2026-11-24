// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Reflection;
using UnityEngine;
using Fusion;

public class GillaItemNet : NetworkBehaviour, IStateAuthorityChanged, IPublicFacingInterface
{
    public GillaItem item;
    private const float GoneFor = 2.5f;
    private int seenEats;
    private float nextClaim;
    private float knockCool;
    private Vector3 lastAt;
    private Vector3 lastVelocity;

    [Networked]
    private PlayerRef Holder { get; set; }

    [Networked]
    private PlayerRef LastHolder { get; set; }

    [Networked]
    private NetworkBool Gone { get; set; }

    [Networked]
    private float GoneAt { get; set; }

    [Networked]
    private float Stirred { get; set; }

    [Networked]
    private int Eats { get; set; }

    [Networked]
    private byte EatenBy { get; set; }

    [Networked]
    private PlayerRef PaidTo { get; set; }

    public bool Live
    {
        get
        {
            if (Object == null)
            {
                return false;
            }
            if (!Object.Runner)
            {
                return false;
            }
            return Object.Runner.Exists(Object);
        }
    }

    public bool HeldByOther
    {
        get
        {
            if (Live && Holder != PlayerRef.None)
            {
                return Holder != Runner.LocalPlayer;
            }
            return false;
        }
    }

    public bool PaysMe
    {
        get
        {
            if (Live)
            {
                return PaidTo == Runner.LocalPlayer;
            }
            return true;
        }
    }

    public override void Spawned()
    {
        seenEats = Eats;
        lastAt = transform.position;
        if (Object == null || !Object.HasStateAuthority)
        {
            if (Gone)
            {
                item.ShowGone();
            }
        }
    }

    public void TakeOver()
    {
        if (!Live)
        {
            return;
        }
        if (Object.HasStateAuthority)
        {
            return;
        }
        Object.RequestStateAuthority();
    }

    public void Swallowed(int recycler)
    {
        if (Live && Object != null && Object.HasStateAuthority)
        {
            EatenBy = (byte)(recycler + 1);
            PaidTo = LastHolder == PlayerRef.None ? Runner.LocalPlayer : LastHolder;
            Eats = Eats + 1;
            seenEats = Eats;
        }
    }

    public void StateAuthorityChanged()
    {
        bool auth = Object != null && Object.HasStateAuthority;
        item.AuthorityChanged(auth);
    }

    public void RemoveFromRoom()
    {
        if (!Live)
        {
            return;
        }
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
            return;
        }
        Object.RequestStateAuthority();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            return;
        }
        PlayerRef local = Runner.LocalPlayer;
        if (item.Hand < 0)
        {
            Holder = PlayerRef.None;
        }
        else
        {
            Holder = local;
            LastHolder = local;
        }
        float t = Runner.SimulationTime;
        if (item.Gone && !Gone)
        {
            Gone = true;
            GoneAt = t;
        }
        if (!item.Still || Stirred <= 0f)
        {
            Stirred = t;
        }
        GillaItemDirector director = GillaItemDirector.Instance;
        if (!Gone || t - GoneAt < GoneFor)
        {
            if (Gone || !item.Lost)
            {
                if (Gone)
                {
                    return;
                }
                if (director == null)
                {
                    return;
                }
                if (director.Stays(item))
                {
                    return;
                }
                if (t - Stirred <= director.lasts)
                {
                    return;
                }
                if ((float)typeof(GillaItemDirector).GetMethod("Nearest", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { transform.position }) < 8f) /* ? */
                {
                    return;
                }
            }
        }
        Runner.Despawn(Object);
    }

    public override void Render()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            return;
        }
        if (Gone && !item.Gone)
        {
            item.ShowGone();
        }
        Knock();
        if (Eats == seenEats)
        {
            return;
        }
        seenEats = Eats;
        item.ShowSwallowed();
        GillaRecycler recycler = GillaRecycler.Number(EatenBy - 1);
        if (recycler != null)
        {
            recycler.Watch(item);
        }
    }

    private void Knock()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            return;
        }
        Vector3 pos = transform.position;
        knockCool -= dt;
        Vector3 velocity = (pos - lastAt) / dt;
        if (Holder == PlayerRef.None && !Gone)
        {
            if (!item.Swallowed && knockCool <= 0f)
            {
                if (lastVelocity.magnitude > 1.2f)
                {
                    float change = (velocity - lastVelocity).magnitude;
                    float angle = Vector3.Angle(lastVelocity, velocity) /* ? */;
                    if (change > 2f && angle < 30f)
                    {
                        GillaItem target = item;
                        knockCool = 0.15f;
                        target.Knock(transform.position, change);
                    }
                }
            }
        }
        lastAt = transform.position;
        lastVelocity = velocity;
    }

    private void Update()
    {
        if (!Live)
        {
            return;
        }
        if (Object.HasStateAuthority)
        {
            return;
        }
        if (Object.StateAuthority != PlayerRef.None)
        {
            return;
        }
        if (!Runner.IsSharedModeMasterClient)
        {
            return;
        }
        if (Time.unscaledTime < nextClaim)
        {
            return;
        }
        nextClaim = Time.unscaledTime + 1f;
        Object.RequestStateAuthority();
    }
}
