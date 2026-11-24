// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System.Reflection;
using UnityEngine;
using Fusion;

public class MonsterNet : NetworkBehaviour
{
	public MonsterBrain brain;
	private const int MostHurts = 16;
	private int seenKills;
	private int seenHurts;
	private int quietUpTo;

	private const BindingFlags AnyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	[Networked]
	private float Speed { get; set; }

	[Networked]
	private float Pace { get; set; }

	[Networked]
	private Vector3 Look { get; set; }

	[Networked]
	private float LookWeight { get; set; }

	[Networked]
	private PlayerRef Victim { get; set; }

	[Networked]
	private int Kills { get; set; }

	[Networked]
	private NetworkBool Chasing { get; set; }

	[Networked]
	private NetworkBool Prying { get; set; }

	[Networked]
	private float Climb { get; set; }

	[Networked]
	private NetworkBool Dead { get; set; }

	[Networked]
	private float HealthLeft { get; set; }

	[Networked]
	private float SnareLeft { get; set; }

	[Networked]
	private int HurtsMade { get; set; }

	[Networked]
	[Capacity(16)]
	private NetworkArray<Vector4> Hurts { get; }

	public bool CanSay
	{
		get
		{
			if (IsAuthority)
			{
				return HurtsMade < MostHurts;
			}
			return false;
		}
	}

	public void Say(Vector4 said)
	{
		if (CanSay)
		{
			NetworkArray<Vector4> hurts = Hurts;
			hurts.Set(HurtsMade, said);
			HurtsMade = HurtsMade + 1;
			seenHurts = HurtsMade;
		}
	}

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

	public bool IsAuthority
	{
		get
		{
			if (Live && Object != null)
			{
				return Object.HasStateAuthority;
			}
			return false;
		}
	}

	public override void Spawned()
	{
		seenKills = Kills;
		brain.Puppet = Object == null || !Object.HasStateAuthority;
		if (Object != null && Object.HasStateAuthority)
		{
			if (HurtsMade == 0)
			{
				HealthLeft = brain.Health;
			}
		}
		quietUpTo = HurtsMade;
		if (Dead)
		{
			brain.ShowDeath();
		}
	}

	public void KillStarted(PlayerRef victim)
	{
		if (IsAuthority)
		{
			Victim = victim;
			Kills = Kills + 1;
			seenKills = Kills;
		}
	}

	public override void FixedUpdateNetwork()
	{
		brain.Puppet = Object == null || !Object.HasStateAuthority;
		if (Object == null || !Object.HasStateAuthority)
		{
			return;
		}
		Speed = brain.AnimSpeed;
		Pace = brain.AnimPace;
		Look = brain.LookPoint;
		LookWeight = brain.LookWeight;
		Prying = brain.Prying;
		Climb = brain.ClimbPitch;
		Chasing = brain.Chasing;
		Dead = brain.Dead;
		HealthLeft = brain.Health;
		SnareLeft = brain.SnareLeft;
	}

	private void CatchUp()
	{
		if (brain.Hurt == null)
		{
			return;
		}
		while (seenHurts < HurtsMade)
		{
			MonsterWounds wounds = brain.Hurt;
			NetworkArray<Vector4> hurts = Hurts;
			Vector4 said = hurts[seenHurts];
			typeof(MonsterWounds).GetMethod("Take", AnyMember, null, new[] { typeof(Vector4), typeof(bool) }, null)?.Invoke(wounds, new object[] { said, quietUpTo <= seenHurts }); /* ? */
			seenHurts++;
		}
	}

	public override void Render()
	{
		bool authority = Object != null && Object.HasStateAuthority;
		brain.Puppet = !authority;
		CatchUp();
		if (Object != null && Object.HasStateAuthority)
		{
			return;
		}
		if (Dead)
		{
			brain.ShowDeath();
			return;
		}
		if (HealthLeft > 0f)
		{
			brain.HeardHealth(HealthLeft);
		}
		brain.ShowSnare(SnareLeft);
		brain.ShowPuppet(Speed, Pace, Look, LookWeight);
		if (brain.Prying != (bool)Prying)
		{
			brain.ShowPry(Prying);
		}
		if (brain.ClimbPitch != Climb)
		{
			brain.ShowClimb(Climb);
		}
		if (brain.Chasing != (bool)Chasing)
		{
			brain.ShowChasing(Chasing);
		}
		if (Kills == seenKills)
		{
			return;
		}
		seenKills = Kills;
		brain.PlayKill();
		PlayerRef victim = Victim;
		if (victim == Runner.LocalPlayer)
		{
			if (KillCam.Local != null)
			{
				KillCam cam = KillCam.Local;
				MonsterBrain killer = brain;
				if (!cam.Busy)
				{
					if (cam.head != null)
					{
						typeof(KillCam).GetMethod("Freeze", AnyMember, null, System.Type.EmptyTypes, null)?.Invoke(cam, null); /* ? */
						typeof(KillCam).GetProperty("Killer", AnyMember)?.SetValue(cam, killer); /* ? */
						FieldInfo onceAtField = typeof(KillCam).GetField("onceAt", AnyMember); /* ? */
						if (onceAtField != null)
						{
							onceAtField.SetValue(cam, null);
						}
						else
						{
							typeof(KillCam).GetProperty("onceAt", AnyMember)?.SetValue(cam, null); /* ? */
						}
						typeof(KillCam).GetMethod("Buzz", AnyMember, null, new[] { typeof(float), typeof(float) }, null)?.Invoke(null, new object[] { 0.8f, 0.25f }); /* ? */
					}
				}
			}
		}
	}
}
