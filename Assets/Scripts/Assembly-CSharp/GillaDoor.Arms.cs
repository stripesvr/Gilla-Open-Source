// Redux addition (not in the original game): a closing door takes off an arm caught under it.
// Uses the same arm tear as the dynamite (NetworkApe.TearYou), so the arm comes off for everyone
// and you live on without it; only the head under the door kills you, as before.
using UnityEngine;

public partial class GillaDoor
{
    private void CrushHands()
    {
        NetworkApe me = NetworkApe.Local;
        if (me == null)
        {
            return;
        }
        for (int i = 0; i < 2; i++)
        {
            bool right = i == 1;
            ApePart part = right ? ApePart.ArmR : ApePart.ArmL;
            if (me.IsTorn((int)part))
            {
                continue;
            }
            Transform hand = me.HandOf(right);
            if (hand == null)
            {
                continue;
            }
            Vector3 away;
            if (HandUnder(hand.position, MonsterPlayer.ApeSize, out away))
            {
                NetworkApe.TearYou(part);
                Sfx.Play(stopSounds, hand.position, 1f, 0.8f, 0.9f);
            }
        }
    }

    // Like Under, but for a hand: it only has to be inside the door's slot and below the panel's
    // bottom edge as it comes down.
    private bool HandUnder(Vector3 hand, float size, out Vector3 away)
    {
        Vector3 d = hand - Foot;
        Vector3 through = Through;
        float along = Vector3.Dot(d, through);
        away = along >= 0f ? through : -through;
        if (Mathf.Abs(along) >= Thickness * 0.5f + 0.04f)
        {
            return false;
        }
        if (Width * 0.5f <= Mathf.Abs(Vector3.Dot(d, Vector3.Cross(Vector3.up, through))))
        {
            return false;
        }
        float bottom = rise * Height();
        return d.y > -0.05f && d.y < bottom + 0.05f * size && bottom < 0.9f;
    }
}

