// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

public class GillaQuestTracker : MonoBehaviour
{
	private Transform head;
	private Vector3 last;
	private float metres;
	private float lit;
	private int roundSeen = -1;
	private int raceSeen = -1;

	[RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType)0)]
	private static void Boot()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
		SceneManager.sceneLoaded += OnSceneLoaded;
		Attach();
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance /* ? */ != null
			? GorillaLocomotion.Player.Instance /* ? */
			: UnityEngine.Object.FindAnyObjectByType<GorillaLocomotion.Player>();
		if (player == null)
		{
			return;
		}
		if (player.GetComponent<GillaQuestTracker>() != null)
		{
			return;
		}
		player.gameObject.AddComponent<GillaQuestTracker>();
	}

	private static void Attach()
	{
		GorillaLocomotion.Player player = GorillaLocomotion.Player.Instance /* ? */ != null
			? GorillaLocomotion.Player.Instance /* ? */
			: UnityEngine.Object.FindAnyObjectByType<GorillaLocomotion.Player>();
		if (player == null)
		{
			return;
		}
		if (player.GetComponent<GillaQuestTracker>() != null)
		{
			return;
		}
		player.gameObject.AddComponent<GillaQuestTracker>();
	}

	private void OnEnable()
	{

		KillCam.Died += OnDied;
	}

	private void OnDisable()
	{
		KillCam.Died -= OnDied;
	}

	private void OnDied()
	{
		if (KillCam.Local != null && KillCam.Local.FellInPit)
		{
			return;
		}
		GillaQuests.Report(GillaQuests.Stat.Caught, 1);
	}

	private void Start()
	{
		Component source = this;
		GorillaLocomotion.Player player = GetComponent<GorillaLocomotion.Player>();
		if (player != null)
		{
			if (player.headCollider /* ? */ != null)
			{
				source = player.headCollider /* ? */;
			}
		}
		head = source.transform;
		last = head.position;
	}

	private void Update()
	{
		float dt = Time.deltaTime;
		Vector3 pos = head.position;
		Vector3 delta = pos - last;
		last = pos;
		float dist = delta.magnitude;
		if (dist < 3f)
		{
			if (!(KillCam.Local != null && KillCam.Local.Busy))
			{
				metres = dist + metres;
			}
		}
		float m = metres;
		if (1f <= m)
		{
			metres = m - (int)m;
			GillaQuests.Report(GillaQuests.Stat.Metres, (int)m);
		}
		float l = lit;
		if (GillaHeadlamp.Lit)
		{
			l = dt + l;
			lit = l;
		}
		if (1f <= l)
		{
			lit = l - (int)l;
			GillaQuests.Report(GillaQuests.Stat.FlashlightSeconds, (int)l);
		}
		GillaModeRound round = GillaModeRound.Current;
		if (round != null)
		{
			if (round.Live && (int)round.Phase == 1 && round.Round != roundSeen)
			{
				if (round.Runner != null)
				{
					NetworkDictionary<PlayerRef, byte> players = round.Players;
					if (players.ContainsKey(round.Runner.LocalPlayer))
					{
						roundSeen = round.Round;
						GillaQuests.Report(GillaQuests.Stat.MonsterRound, 1);
					}
				}
			}
		}
		GillaSplashGame splash = GillaSplashGame.Current;
		if (splash != null)
		{
			if (splash.Live && (int)splash.Phase == 2 && splash.Starts != raceSeen)
			{
				if (splash.Runner != null)
				{
					NetworkDictionary<PlayerRef, byte> players = splash.Players;
					if (players.ContainsKey(splash.Runner.LocalPlayer))
					{
						raceSeen = splash.Starts;
						GillaQuests.Report(GillaQuests.Stat.SplashRace, 1);
					}
				}
			}
		}
	}

	private void OnApplicationPause(bool paused)
	{
		if (paused)
		{
			GillaQuests.Flush();
			return;
		}
	}

	private void OnApplicationQuit()
	{
		GillaQuests.Flush();
	}
}
