// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class BecomeMonsterButton : MonoBehaviour
{
	[Tooltip("The monster (MonsterCatalog, 1-based); 0 = back to the ape.")]
	public int monster;
	public Transform[] hands;
	public float radius = 0.13f;
	private float cooldown;
	private Vector3 baseScale;

	private void Start()
	{
		baseScale = transform.localScale;
	}

	private void Update()
	{
		cooldown -= Time.deltaTime;
		transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 1f - Mathf.Exp(Time.deltaTime * -10f));
		if (cooldown <= 0f && hands != null)
		{
			if (MonsterPlayer.Local == null)
			{
				return;
			}
			for (int i = 0; i < hands.Length; i++)
			{
				Transform hand = hands[i];
				if (hand == null)
				{
					continue;
				}
				if ((hand.position - transform.position).sqrMagnitude <= radius * radius)
				{
					cooldown = 1f;
					transform.localScale = baseScale * 0.8f;
					MonsterPlayer.Local.Become(monster);
					return;
				}
			}
		}
	}

	public void Click()
	{
		if (cooldown <= 0f)
		{
			if (MonsterPlayer.Local != null)
			{
				Press();
				return;
			}
		}
	}

	private void Press()
	{
		cooldown = 1f;
		transform.localScale = baseScale * 0.8f;
		MonsterPlayer.Local.Become(monster);
	}
}
