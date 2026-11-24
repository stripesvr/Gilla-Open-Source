// Reconstructed from the IL2CPP build (decompiled, not the original source).
using System;
using UnityEngine;
using UnityEngine.XR;

public partial class MonsterPlayer : MonoBehaviour
{
    private void BackToApe()
    {
        DropBoth(Vector3.zero);
        LetGoSlab(Vector3.zero);
        for (int i = 0; i < 2; i++)
        {
            if (bodyOf[i] >= 0)
            {
                LetGoBody(i == 1, Vector3.zero);
            }
        }
        ripBody = -1;
        deadBody[0] = -1;
        deadBody[1] = -1;
        if (worn != null)
        {
            Destroy(worn.gameObject);
        }
        worn = null;
        Vector3 floor = FloorUnder(head.position);
        if (walker != null)
        {
            walker.enabled = false;
            Destroy(walker);
            walker = null;
        }
        if (crawling)
        {
            locomotion.minimumRaycastDistance = minCast;
            locomotion.velocityLimit = velLimit;
            locomotion.maxJumpSpeed = maxJump;
            locomotion.maxArmLength = maxArm;
            locomotion.unStickDistance = unstick;
            locomotion.jumpMultiplier = jumpBy;
            locomotion.leftHandOffset = offL;
            locomotion.rightHandOffset = offR;
            crawling = false;
        }
        crawlSize = 1f;
        ApeSize = 1f;
        ScaleAboutHead(1f);
        Freeze(false);
        Settle(floor.y + apeEyeHeight + 0.1f);
        if (locomotion != null)
        {
            locomotion.enabled = locomotionWasOn;
        }
        locomotionWasOn = true;
        for (int i = 0; i < apeBodies.Length; i++)
        {
            GameObject go = apeBodies[i];
            if (go != null)
            {
                go.SetActive(true);
            }
        }
        entry = null;
        Current = 0;
    }

    private void Freeze(bool on)
    {
        if (locomotion != null)
        {
            if (locomotion.headCollider != null)
            {
                locomotion.headCollider.enabled = !on;
            }
            if (locomotion.bodyCollider != null)
            {
                locomotion.bodyCollider.enabled = !on;
            }
        }
        if (body == null)
        {
            return;
        }
        if (on)
        {
            interp = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        else
        {
            body.isKinematic = false;
            body.interpolation = interp;
        }
    }

    public bool SetApeSize(float s, bool needsRoom)
    {
        s = Mathf.Clamp(s, 0.05f, 3f);
        if (locomotion == null)
        {
            return true;
        }
        if (KillCam.Local != null && KillCam.Local.Busy)
        {
            return true;
        }
        if (!Mathf.Approximately(ApeSize, s))
        {
            RigChanging?.Invoke();
            bool ok = Resize(s, needsRoom);
            RigChanged?.Invoke();
            return ok;
        }
        return true;
    }

    private bool Resize(float s, bool needsRoom)
    {
        Vector3 at = Vector3.zero;
        RaycastHit hit = default;
        s = Mathf.Clamp(s, 0.05f, 3f);
        if (!Mathf.Approximately(ApeSize, s))
        {
            float was = ApeSize;
            if (walker == null)
            {
                float k = s / was;
                if (!HeadAtSize(k, out at) && needsRoom)
                {
                    return false;
                }
                locomotion.minimumRaycastDistance *= k;
                locomotion.velocityLimit *= k;
                locomotion.maxJumpSpeed *= k;
                locomotion.maxArmLength *= k;
                locomotion.unStickDistance *= k;
                locomotion.leftHandOffset = locomotion.leftHandOffset * k;
                locomotion.rightHandOffset = locomotion.rightHandOffset * k;
                ApeSize = s;
                if (worn != null)
                {
                    worn.sizeBy = s;
                }
                transform.localScale = Vector3.one * (s * crawlSize);
                Transform t = transform;
                t.position = t.position + (at - head.position);
                if (body != null)
                {
                    body.position = transform.position;
                    body.linearVelocity = Vector3.zero;
                }
                Physics.SyncTransforms();
                locomotion.InitializeValues();
                return true;
            }
            if (needsRoom)
            {
                if (Physics.Raycast(head.position, Vector3.up, out hit, 60f, -5893, QueryTriggerInteraction.Ignore))
                {
                    if (hit.point.y < transform.position.y + s * (standEyes + 0.15f))
                    {
                        return false;
                    }
                }
            }
            ApeSize = s;
            Size(standEyes);
            if (worn != null)
            {
                worn.sizeBy = s;
            }
        }
        return true;
    }

    private bool HeadAtSize(float k, out Vector3 at)
    {
        RaycastHit up = default;
        RaycastHit down = default;
        at = head.position;
        int layerMask = locomotion.locomotionEnabledLayers.value;
        float radius;
        if (locomotion.headCollider != null)
        {
            radius = locomotion.headCollider.radius * locomotion.headCollider.transform.lossyScale.x;
        }
        else
        {
            radius = 0.1f;
        }
        float y;
        float least;
        float startY;
        if (Physics.Raycast(at, Vector3.down, out down, 60f, layerMask, QueryTriggerInteraction.Ignore))
        {
            startY = at.y;
            least = radius * k + down.point.y + 0.01f;
            y = Mathf.Max(down.point.y + (startY - down.point.y) * k, least);
        }
        else
        {
            startY = at.y;
            least = float.NegativeInfinity;
            y = startY;
        }
        bool hitUp = Physics.Raycast(new Vector3(at.x, startY, at.z), Vector3.up, out up, 60f, layerMask, QueryTriggerInteraction.Ignore);
        float most = hitUp ? (up.point.y - radius * k) - 0.01f : float.PositiveInfinity;
        y = Mathf.Min(y, most);
        at.y = Mathf.Max(least, y);
        return least <= most;
    }

    private void ScaleAboutHead(float s)
    {
        Vector3 was = head.position;
        transform.localScale = Vector3.one * s;
        Vector3 now = head.position;
        Transform t = transform;
        Vector3 p = t.position;
        t.position = new Vector3(was.x - now.x + p.x, p.y + 0f, was.z - now.z + p.z);
        if (body != null)
        {
            body.position = transform.position;
        }
        Physics.SyncTransforms();
    }

    private void Settle(float eyeY)
    {
        Transform t = transform;
        Vector3 p = t.position;
        t.position = p + Vector3.up * (eyeY - head.position.y);
        if (body != null)
        {
            body.position = transform.position;
            body.linearVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
        if (locomotion == null)
        {
            return;
        }
        locomotion.InitializeValues();
    }

    private void Update()
    {
        TestEachFrame?.Invoke();
        if (ApeSize < 1f)
        {
            if (Current == 0 || crawling)
            {
                if (body != null && !body.isKinematic && body.useGravity)
                {
                    body.AddForce(Physics.gravity * (ApeSize - 1f), ForceMode.Acceleration);
                }
            }
        }
        if (Current != 0)
        {
            if (entry == null)
            {
                return;
            }
            float dt = Time.deltaTime;
            if (KillCam.Local != null && KillCam.Local.Busy)
            {
                return;
            }
            dt = Mathf.Max(dt, 0.0001f);
            backT = WantsBack ? dt + backT : 0f;
            if (backHold < backT)
            {
                backT = 0f;
                if (Current != 0)
                {
                    if (KillCam.Local != null && KillCam.Local.Busy)
                    {
                        return;
                    }
                    RigChanging?.Invoke();
                    Turn(0);
                    RigChanged?.Invoke();
                }
                return;
            }
            if (walker != null)
            {
                WalkOn(dt);
            }
            if (entry.grabs)
            {
                GillaRip.MakeOneMore();
            }
            Hands(dt);
            return;
        }
        if (slabDoor != null)
        {
            LetGoSlab(Vector3.zero);
        }
        bool notHere = !ApePunchesOffHere;
        if (!(OwnerPunches && notHere))
        {
            if (!(MenuPunches && notHere))
            {
                ApeBodies();   // Redux: an ape can hold bodies and torn-off parts (MonsterPlayer.ApeBodies.cs)
                return;
            }
            ApeLetGo();
            MenuFists();
            return;
        }
        ApePunches();
    }

    public void Snared(float seconds)
    {
        snaredTill = Time.time + seconds;
        snaredAt = transform.position;
    }

    private void LateUpdate()
    {
        if (Current == 0 || snaredTill <= Time.time)
        {
            if (wasSnared)
            {
                if (locomotion != null && locomotion.enabled)
                {
                    locomotion.InitializeValues();
                }
            }
            wasSnared = false;
        }
        else
        {
            wasSnared = true;
            transform.position = snaredAt;
            if (body != null)
            {
                body.position = snaredAt;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                }
            }
            if (struggleBuzz <= Time.time)
            {
                struggleBuzz = Time.time + UnityEngine.Random.Range(0.25f, 0.6f);
                XRNode[] nodes = new XRNode[] { XRNode.LeftHand, XRNode.RightHand };
                for (int i = 0; i < nodes.Length; i++)
                {
                    InputDevice device = InputDevices.GetDeviceAtXRNode(nodes[i]);
                    device.SendHapticImpulse(0u, 0.35f, 0.08f);
                }
            }
        }
    }

    private bool WantsBack
    {
        get
        {
            if (Button(XRNode.LeftHand, CommonUsages.primaryButton))
            {
                if (!Grip(XRNode.LeftHand))
                {
                    if (held == null)
                    {
                        if (heldDummy == null && !HasSecond)
                        {
                            return !GillaMapVote.HuntingApes;
                        }
                    }
                }
            }
            return false;
        }
    }

    public bool OnLegs
    {
        get { return walker != null; }
    }

    public void CloseFingers(bool right, float k)
    {
        if (worn != null)
        {
            worn.CloseFingers(right, k);
        }
    }

    public void Float(Vector3 velocity, bool through)
    {
        floatBy = velocity;
        floatThrough = through;
        floatFrame = Time.frameCount;
    }

    private void WalkOn(float dt)
    {
        wornFor += dt;
        if (!fitted && 0.4f < wornFor)
        {
            if (worn != null)
            {
                float eyes = worn.EyeHeightNow;
                if (ApeSize * 0.3f < eyes)
                {
                    fitted = true;
                    float e = worn.EyeHeightNow / ApeSize;
                    if (0.04f < Mathf.Abs(e - entry.eyeHeight))
                    {
                        standEyes = e;
                        Size(e);
                    }
                }
            }
        }
        Vector2 stick = snaredTill <= Time.time ? Stick(XRNode.LeftHand) : Vector2.zero;
        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
        float amount = Mathf.Min(stick.magnitude, 1f);
        float run = entry.runSpeed;
        float k = Mathf.Clamp01((amount - 0.15f) / 0.65f);
        Vector3 want;
        if (amount <= 0.01f)
        {
            want = Vector3.zero;
        }
        else
        {
            want = (fwd * stick.y + right * stick.x).normalized * (run * ApeSize * faster * k);
        }
        planar = Vector3.MoveTowards(planar, want, dt * 20f);
        int since = Time.frameCount - floatFrame;
        float newVy = 0f;
        if (1 < since)
        {
            newVy = walker.isGrounded ? -1f : vy + dt * -9.8f;
        }
        vy = newVy;
        CharacterController cc = walker;
        Vector3 c = head.localPosition;
        float h = walker.height;
        float skin = walker.skinWidth;
        c.z = head.localPosition.z;
        c.y = h * 0.5f + skin;
        cc.center = c;
        Vector3 extra = since < 2 ? floatBy : Vector3.zero;
        Vector3 move = (planar + Vector3.up * vy + extra) * dt;
        if (since < 2 && floatThrough)
        {
            Transform t = transform;
            t.position = move + t.position;
        }
        else
        {
            walker.Move(move);
        }
        if (body != null)
        {
            body.position = transform.position;
        }
        leftFollower.position = leftTarget.position + leftTarget.rotation * locomotion.leftHandOffset;
        rightFollower.position = rightTarget.position + rightTarget.rotation * locomotion.rightHandOffset;
    }

    private void Hands(float dt)
    {
        gripL = TestGrips < 0 ? Grip(XRNode.LeftHand) : (TestGrips & 1) != 0;
        gripR = TestGrips < 0 ? Grip(XRNode.RightHand) : ((TestGrips >> 1) & 1) != 0;
        if (GillaHands.Holding(0) != null || (gripL && !wasGripL && GillaHands.Reaches(0)))
        {
            gripL = false;
            armLostAt[0] = Time.time;
        }
        if (GillaHands.Holding(1) != null || (gripR && !wasGripR && GillaHands.Reaches(1)))
        {
            gripR = false;
            armLostAt[1] = Time.time;
        }
        FollowHands(dt);
        float radius = grabRadius * Mathf.Max(transform.localScale.x, 0.4f);
        if (!entry.grabs)
        {
            Killing(radius, dt);
        }
        else
        {
            Grabbing(radius);
            ShowPull();
            AwaitTorn();
            AwaitBody();
            TellAtRest();
        }
        wasGripL = gripL;
        wasGripR = gripR;
    }

    private void FollowHands(float dt)
    {
        Vector3 l = leftFollower.position;
        Vector3 r = rightFollower.position;
        velL = (l - lastL) / dt;
        velR = (r - lastR) / dt;
        Vector3 real = RealHand(false);
        Vector3 realR = RealHand(true);
        realVelR = (realR - lastRealR) / dt;
        lastRealR = realR;
        realVelL = (real - lastRealL) / dt;
        lastRealL = real;
        float t = 1f - Mathf.Exp(dt * -25f);
        throwL = Vector3.Lerp(throwL, velL, t);
        throwR = Vector3.Lerp(throwR, velR, t);
        ArmSwing(0, real, dt);
        ArmSwing(1, realR, dt);
        lastL = l;
        punchIn -= dt;
        lastR = r;
    }

    private void ApePunches()
    {
        float dt = Time.deltaTime;
        bool busy = KillCam.Local != null ? KillCam.Local.Busy : false;
        dt = Mathf.Max(dt, 0.0001f);
        int frame = Time.frameCount;
        int last = fistsFrame;
        fistsFrame = Time.frameCount;
        for (int i = 0; i < 2; i++)
        {
            if (1 < frame - last || HandTaken(i))
            {
                armLostAt[i] = Time.time;
            }
        }
        if (!busy)
        {
            gripL = !HandTaken(0) && (TestGrips < 0 ? Grip(XRNode.LeftHand) : (TestGrips & 1) != 0);
            gripR = !HandTaken(1) && (TestGrips < 0 ? Grip(XRNode.RightHand) : ((TestGrips >> 1) & 1) != 0);
        }
        else
        {
            gripL = false;
            gripR = false;
        }
        FollowHands(dt);
        GillaRip.MakeOneMore();
        Grabbing(grabRadius * Mathf.Max(transform.localScale.x, 0.4f));
        ShowPull();
        AwaitTorn();
        AwaitBody();
        TellAtRest();
        wasGripL = gripL;
        wasGripR = gripR;
    }

    private void MenuFists()
    {
        float dt = Time.deltaTime;
        bool free = KillCam.Local != null ? !KillCam.Local.Busy : true;
        int frame = Time.frameCount;
        int last = fistsFrame;
        fistsFrame = Time.frameCount;
        for (int i = 0; i < 2; i++)
        {
            if (1 < frame - last || HandTaken(i))
            {
                armLostAt[i] = Time.time;
            }
        }
        if (free)
        {
            gripL = !HandTaken(0) && (TestGrips < 0 ? Grip(XRNode.LeftHand) : (TestGrips & 1) != 0);
            gripR = !HandTaken(1) && (TestGrips < 0 ? Grip(XRNode.RightHand) : ((TestGrips >> 1) & 1) != 0);
        }
        else
        {
            gripL = false;
            gripR = false;
        }
        FollowHands(dt);
        FreePunching();
        wasGripL = gripL;
        wasGripR = gripR;
    }

    private bool HandTaken(int i)
    {
        if (locomotion != null)
        {
            bool disabled = i == 0 ? locomotion.leftHandDisabled : locomotion.rightHandDisabled;
            if (disabled)
            {
                return true;
            }
        }
        if (GillaHands.Holding(i) != null)
        {
            return true;
        }
        return GillaToys.HeldIn(i);
    }

    private void ApeLetGo()
    {
        if (held == null && heldDummy == null && !HasSecond)
        {
            if (bodyOf[0] < 0 && bodyOf[1] < 0 && ripBody < 0)
            {
                if (deadBody[0] < 0 && deadBody[1] < 0 && awaitBody < 0)
                {
                    return;
                }
            }
        }
        DropBoth(Vector3.zero);
        for (int i = 0; i < 2; i++)
        {
            if (bodyOf[i] >= 0)
            {
                LetGoBody(i == 1, Vector3.zero);
            }
        }
        ripBody = -1;
        deadBody[0] = -1;
        deadBody[1] = -1;
        awaitBody = -1;
        gripL = false;
        gripR = false;
        wasGripL = false;
        wasGripR = false;
    }
}
