// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GillaItem : MonoBehaviour
{
	[Tooltip("What the recycler's screen calls it.")]
	public string label = "SCRAP";
	[Tooltip("The bolts the recycler pays for it (its name's from when it paid coins).")]
	public int coins = 5;
	[Tooltip("Its knocks on things (louder the harder it hits).")]
	public AudioClip[] knocks;
	[Tooltip("It stays till it's lost, however long it lies untouched, and no recycler takes it (the keycard: GillaItemDirector).")]
	public bool keeps;
	[Tooltip("A recycler takes it (not a stick of dynamite: GillaDynamite).")]
	public bool recycles = true;
	public static readonly List<GillaItem> All = new List<GillaItem>();
	public Action<Vector3, Vector3, float, Collider> Knocked;
	private Renderer[] renderers;
	private float bornY;
	private float stirred;
	private float knockCool;
	private bool throwWaits;
	private Vector3 throwVelocity;
	private Vector3 throwSpin;

	public int Hand { get; private set; } = -1;
	public bool Swallowed { get; private set; }
	public bool Gone { get; private set; }
	public bool Pinned { get; set; }
	public bool ForMonsters { get; set; }
	public bool Stuck { get; set; }
	public Rigidbody Body { get; private set; }
	public Collider[] Colliders { get; private set; }
	public GillaItemNet Net { get; private set; }

	public bool Mine
	{
		get
		{
			if (Net == null)
			{
				return true;
			}
			if (!Net.Live)
			{
				return true;
			}
			var obj = Net.Object;
			if (obj == null)
			{
				return false;
			}
			return obj.HasStateAuthority; /* ? */
		}
	}

	public bool HeldByOther
	{
		get
		{
			if (Net != null)
			{
				GillaItemNet net = Net;
				Fusion.PlayerRef holder = NetHolder(net); /* ? */
				if (net.Live && holder != Fusion.PlayerRef.None)
				{
					return holder != net.Runner.LocalPlayer;
				}
				return false;
			}
			return false;
		}
	}

	// GillaItemNet.Holder is not accessible from here; read it reflectively. /* ? */
	private static Fusion.PlayerRef NetHolder(GillaItemNet net)
	{
		PropertyInfo prop = typeof(GillaItemNet).GetProperty("Holder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (prop != null && net != null && net.Live)
		{
			object value;
			try
			{
				value = prop.GetValue(net);
			}
			catch (TargetInvocationException)
			{
				// Redux: not spawned on the network yet, so nobody holds it
				return Fusion.PlayerRef.None;
			}
			if (value is Fusion.PlayerRef player)
			{
				return player;
			}
		}
		return Fusion.PlayerRef.None;
	}

	// GillaItemDirector.Nearest is not accessible from here; call it reflectively. /* ? */
	private static float DirectorNearest(Vector3 position)
	{
		MethodInfo method = typeof(GillaItemDirector).GetMethod("Nearest", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Vector3) }, null);
		if (method != null)
		{
			object value = method.Invoke(null, new object[] { position });
			if (value != null)
			{
				return Convert.ToSingle(value);
			}
		}
		return float.MaxValue;
	}

	public bool PaysMe
	{
		get
		{
			if (Net == null)
			{
				return true;
			}
			return Net.PaysMe;
		}
	}

	public bool Lost
	{
		get
		{
			if (Hand < 0 && !Swallowed && !Gone)
			{
				return transform.position.y < bornY - 30f;
			}
			return false;
		}
	}

	public bool Still
	{
		get
		{
			if (Hand < 0 && !Swallowed && !Gone && !throwWaits)
			{
				if (Body.isKinematic)
				{
					return true;
				}
				return Body.linearVelocity.sqrMagnitude < 0.01f;
			}
			return false;
		}
	}

	private void Awake()
	{

		Body = GetComponent<Rigidbody>();
		Colliders = GetComponentsInChildren<Collider>();
		renderers = GetComponentsInChildren<Renderer>();
		Net = GetComponent<GillaItemNet>();
		bornY = transform.position.y;
		stirred = Time.time;
		string[] layers = new string[] { "PlayerRig", "FirstPerson", "MirrorOnly", "Buttons", "Apes" };
		int mask = 0;
		for (int i = 0; i < layers.Length; i++)
		{
			int layer = LayerMask.NameToLayer(layers[i]);
			mask |= layer >= 0 ? 1 << layer : 0;
		}
		Body.excludeLayers = mask;
	}

	private void OnEnable()
	{

		All.Add(this);
	}

	private void OnDisable()
	{
		All.Remove(this);
	}

	private void Start()
	{
		GillaHands.IgnoreYou(Colliders);
	}

	private void Update()
	{
		knockCool -= Time.deltaTime;
		if (Net != null && Net.Live)
		{
			Body.isKinematic = !(Mine && !Swallowed && !Gone && !throwWaits && Hand < 0) || Pinned;
			if (Hand < 0)
			{
				return;
			}
			Hold();
			return;
		}
		if (!Still)
		{
			stirred = Time.time;
		}
		if (!Gone)
		{
			GillaItemDirector director = GillaItemDirector.Instance;
			if (!Lost)
			{
				if (director != null)
				{
					if (!director.Stays(this) && director.lasts < Time.time - stirred)
					{
						if (DirectorNearest(transform.position) >= 8f) /* ? */
						{
							Destroy(gameObject);
						}
					}
				}
				return;
			}
		}
		Destroy(gameObject);
	}

	public void Take(int hand)
	{
		Hand = hand;
		throwWaits = false;
		if (Net != null)
		{
			Net.TakeOver();
		}
		Hold();
	}

	private void Hold()
	{
		bool collidersOn;
		if (Colliders.Length == 0 || !(Colliders[0] != null))
		{
			collidersOn = false;
		}
		else
		{
			collidersOn = Colliders[0].enabled;
		}
		if (!collidersOn && Body.isKinematic)
		{
			return;
		}
		Body.isKinematic = true;
		Body.interpolation = RigidbodyInterpolation.None;
		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i].enabled = false;
		}
	}

	public void Release(Vector3 velocity, Vector3 spin)
	{
		Hand = -1;
		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i].enabled = true;
		}
		if (Mine)
		{
			Fly(velocity, spin);
			return;
		}
		throwVelocity = velocity;
		throwWaits = true;
		throwSpin = spin;
	}

	private void Fly(Vector3 velocity, Vector3 spin)
	{
		throwWaits = false;
		Body.isKinematic = false;
		Body.interpolation = RigidbodyInterpolation.Interpolate;
		Body.linearVelocity = velocity;
		Body.angularVelocity = spin;
		Body.WakeUp();
	}

	public void AuthorityChanged(bool mine)
	{
		if (mine)
		{
			if (throwWaits && Hand < 0)
			{
				Fly(throwVelocity, throwSpin);
				return;
			}
		}
		else
		{
			throwWaits = false;
			if (Hand >= 0 && HeldByOther)
			{
				GillaHands.Drop(this);
				return;
			}
		}
	}

	public void Swallow(int recycler)
	{
		if (Hand >= 0)
		{
			GillaHands.Drop(this);
		}
		Swallowed = true;
		Body.isKinematic = true;
		Body.interpolation = RigidbodyInterpolation.None;
		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i].enabled = false;
		}
		if (Net != null)
		{
			Net.Swallowed(recycler);
		}
	}

	public void Recycle()
	{
		Swallowed = false;
		Gone = true;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = false;
		}
	}

	public void ShowSwallowed()
	{
		if (Hand >= 0)
		{
			GillaHands.Drop(this);
		}
		Swallowed = true;
		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i].enabled = false;
		}
	}

	public void ShowGone()
	{
		Swallowed = false;
		Gone = true;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = false;
		}
		for (int i = 0; i < Colliders.Length; i++)
		{
			Colliders[i].enabled = false;
		}
	}

	private void OnCollisionEnter(Collision hit)
	{
		ContactPoint contact = hit.GetContact(0);
		Vector3 rel = hit.relativeVelocity;
		Knock(contact.point, (float)Math.Sqrt(rel.sqrMagnitude));
	}

	public void HeldKnock(Vector3 at, Vector3 faces, float speed, Collider on)
	{
		if (Knocked != null)
		{
			Knocked(at, faces, speed, on);
			return;
		}
		Knock(at, speed);
	}

	public void Knock(Vector3 at, float speed)
	{
		if (speed >= 0.6f && knockCool <= 0f && knocks != null && knocks.Length != 0)
		{
			float f = speed / 5f;
			knockCool = 0.12f;
			float volume = 0.15f;
			if (f >= 0.15f)
			{
				volume = f;
				if (f > 0.8f)
				{
					volume = 0.8f;
				}
			}
			Sfx.Play(knocks, at, volume, 0.92f, 1.12f);
		}
	}
}
