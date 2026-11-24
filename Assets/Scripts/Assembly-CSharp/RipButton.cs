// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

public class RipButton : MonoBehaviour
{
    public enum Action
    {
        TearLeftArm = 0,
        TearRightArm = 1,
        TearHead = 2,
        ResetAll = 3
    }

    public RipButton.Action action;
    public DismemberGroup group;
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
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 1f - Mathf.Exp(-10f * Time.deltaTime));
        if (cooldown <= 0f)
        {
            if (group == null || hands == null)
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
                    Press();
                    return;
                }
            }
        }
    }

    public void Click()
    {
        if (cooldown <= 0f && group != null)
        {
            Press();
        }
    }

    private void Press()
    {
        cooldown = 1f;
        transform.localScale = baseScale * 0.8f;
        if (action == Action.TearHead)
        {
            group.Tear((ApePart)0);
        }
        else if (action == Action.TearRightArm)
        {
            group.Tear((ApePart)2);
        }
        else if (action == Action.TearLeftArm)
        {
            group.Tear((ApePart)1);
        }
        else
        {
            group.ResetAll();
        }
    }
}
