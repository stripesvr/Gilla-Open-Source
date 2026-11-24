// Redux addition (not in the original game): apes can pick up what's left of a body.
// The original only let monsters (and owners with punches on) hold bodies and torn-off parts.
// This runs the same grab code for an ordinary ape: grip near a body or a loose arm, leg or
// head to hold it, let go to drop or throw it. It goes through the same RPCs as a monster's
// grab, so everyone in the room sees it.
using UnityEngine;
using UnityEngine.XR;

public partial class MonsterPlayer
{
    private int apeBodiesFrame = -10;

    private void ApeBodies()
    {
        bool busy = KillCam.Local != null && KillCam.Local.Busy;
        if (busy || held != null || heldDummy != null || HasSecond || deadBody[0] >= 0 || deadBody[1] >= 0 || awaitBody >= 0
            || leftFollower == null || rightFollower == null || leftTarget == null || rightTarget == null || locomotion == null)
        {
            ApeLetGo();
            return;
        }
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        if (!locomotion.enabled)
        {
            // keyboard-and-mouse rig: nothing else is moving the hand followers
            leftFollower.position = RealHand(false);
            rightFollower.position = RealHand(true);
        }
        gripL = !HandTaken(0) && (TestGrips < 0 ? Grip(XRNode.LeftHand) : (TestGrips & 1) != 0);
        gripR = !HandTaken(1) && (TestGrips < 0 ? Grip(XRNode.RightHand) : ((TestGrips >> 1) & 1) != 0);
        // an item within reach wins: that grip is the item's
        if (gripL && !wasGripL && bodyOf[0] < 0 && GillaHands.Reaches(0)) gripL = false;
        if (gripR && !wasGripR && bodyOf[1] < 0 && GillaHands.Reaches(1)) gripR = false;

        Vector3 l = leftFollower.position;
        Vector3 r = rightFollower.position;
        if (Time.frameCount - apeBodiesFrame > 1)
        {
            lastL = l;
            lastR = r;
            throwL = Vector3.zero;
            throwR = Vector3.zero;
        }
        apeBodiesFrame = Time.frameCount;
        float t = 1f - Mathf.Exp(dt * -25f);
        throwL = Vector3.Lerp(throwL, (l - lastL) / dt, t);
        throwR = Vector3.Lerp(throwR, (r - lastR) / dt, t);
        lastL = l;
        lastR = r;

        Bodies(grabRadius * Mathf.Max(transform.localScale.x, 0.4f), true);
        wasGripL = gripL;
        wasGripR = gripR;
    }
}
