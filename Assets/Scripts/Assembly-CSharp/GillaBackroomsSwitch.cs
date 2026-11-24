// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class GillaBackroomsSwitch : MonoBehaviour
{
	[Tooltip("What's switched, nearest the hole first.")]
	public GameObject[] parts = new GameObject[0];
	[Tooltip("All of it, and the hole, world: you're down there.")]
	public Bounds below;
	[Tooltip("The locked room and its doorway, world.")]
	public Bounds room;
	[Tooltip("In front of the door, world: they're on from here while it's open.")]
	public Bounds store;
	public GillaDoor door;
	[Tooltip("How many parts a frame.")]
	public int perFrame = 40;
	[Tooltip("Seconds after you've gone before they go off.")]
	public float offAfter = 3f;
	public static bool Held;
	public static bool ForMonster;
	private int on;
	private float wantedAt = float.NegativeInfinity;

	public static GillaBackroomsSwitch Instance { get; private set; }

	public bool AllOn
	{
		get { return parts.Length <= on; }
	}

	public bool AllOff
	{
		get { return on < 1; }
	}

	private void Awake()
	{

		Instance = this;
		on = parts.Length;
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Start()
	{
		if (Wanted(Eyes()))
		{
			wantedAt = Time.time;
			return;
		}
		All(false);
	}

	public static bool SameSide(Vector3 a, Vector3 b)
	{
		if (Instance == null)
		{
			return true;
		}
		bool inA = Instance.below.Contains(a);
		bool inB = Instance.below.Contains(b);
		return inA == inB;
	}

	public static bool OnYourSide(Vector3 at)
	{
		if (Instance == null)
		{
			return true;
		}
		Vector3 eyes = Eyes();
		if ((at - eyes).sqrMagnitude >= 9f)
		{
			bool inAt = Instance.below.Contains(at);
			bool inEyes = Instance.below.Contains(eyes);
			return inAt == inEyes;
		}
		return true;
	}

	public bool Wanted(Vector3 at)
	{
		if (below.Contains(at) || room.Contains(at))
		{
			return true;
		}
		if (door != null && door.IsOpen)
		{
			return store.Contains(at);
		}
		return false;
	}

	public static void Prepare(Vector3 at)
	{
		if (Instance == null)
		{
			return;
		}
		if (!Instance.Wanted(at))
		{
			return;
		}
		Instance.wantedAt = Time.time;
		Instance.All(true);
	}

	private void LateUpdate()
	{
		if (Held || ForMonster || Wanted(Eyes()))
		{
			wantedAt = Time.time;
		}
		if (offAfter <= Time.time - wantedAt)
		{
			if (on > 0 && perFrame > 0)
			{
				int i = 0;
				do
				{
					if (on < 1)
					{
						return;
					}
					on--;
					if (parts[on] != null)
					{
						parts[on].SetActive(false);
					}
					i++;
				} while (i < perFrame);
			}
		}
		else
		{
			if (on < parts.Length && perFrame > 0)
			{
				int i = 0;
				do
				{
					if (parts.Length <= on)
					{
						return;
					}
					if (parts[on] != null)
					{
						parts[on].SetActive(true);
					}
					i++;
					on++;
				} while (i < perFrame);
			}
		}
	}

	private void All(bool state)
	{
		for (int i = 0; i < parts.Length; i++)
		{
			GameObject part = parts[i];
			if (part != null && part.activeSelf != state)
			{
				part.SetActive(state);
			}
		}
		on = state ? parts.Length : 0;
	}

	private static Vector3 Eyes()
	{
		Camera cam = Camera.main;
		if (cam != null)
		{
			return cam.transform.position;
		}
		return Vector3.positiveInfinity;
	}
}
