// Desktop controls for testing without a headset.
// The original build had a small editor-only rig here (WASD slid the player around at a fixed
// height). This Redux version replaces it with Roblox-style keyboard and mouse movement.
// It only runs on PC (editor, Windows, Mac, Linux) and only when no headset is active;
// on the Quest it switches itself off and the VR arm-swing locomotion is untouched.
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class DesktopTestRig : MonoBehaviour
{
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;
    public float moveSpeed = 2.5f;     // kept for the scene's saved value; see walkSpeed
    public float lookSpeed = 2.2f;

    // Roblox defaults: WalkSpeed 16 studs/s and JumpHeight 7.2 studs, at roughly 0.28 m per stud.
    private const float WalkSpeed = 4.5f;
    private const float RunSpeed = 7.5f;
    private const float JumpHeight = 2.0f;
    private const float Acceleration = 60f;      // snappy: full speed in under 0.1 s
    private const float AirControl = 0.6f;
    private const float EyeHeight = 1.5f;

    private float yaw;
    private float pitch;
    private bool active;
    private bool shiftLock;
    private Rigidbody body;
    private GorillaLocomotion.Player vrMove;
    private Vector3 wish;
    private bool jumpQueued;
    private bool grounded;
    private static readonly Quaternion GripFromAim = Quaternion.Euler(-60f, 0f, 0f);

    private static bool OnPc
    {
        get
        {
            if (Application.isEditor) return true;
            RuntimePlatform p = Application.platform;
            return p == RuntimePlatform.WindowsPlayer || p == RuntimePlatform.LinuxPlayer || p == RuntimePlatform.OSXPlayer;
        }
    }

    private void Start()
    {
        if (!OnPc)
        {
            enabled = false;
            return;
        }
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (device.isValid)
        {
            enabled = false;   // a headset is in use: leave VR movement alone
            return;
        }
        active = true;
        body = GetComponent<Rigidbody>();
        vrMove = GetComponent<GorillaLocomotion.Player>();
        if (vrMove != null)
        {
            vrMove.enabled = false;   // arm-swing locomotion would fight the keyboard
        }
        if (head != null)
        {
            head.localPosition = new Vector3(0f, EyeHeight, 0f);
            yaw = head.eulerAngles.y;
        }
        if (body != null)
        {
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }
        Debug.Log("[DesktopTestRig] no headset detected - desktop controls on (WASD move, Space jump, Shift run, right mouse or arrow keys look, Left Ctrl mouse lock, Q/E reach).");
    }

    private static bool Key(Key key, KeyCode legacy)
    {
        Keyboard k = Keyboard.current;
        if (k != null) return k[key].isPressed;
        return Input.GetKey(legacy);
    }

    private static bool KeyDown(Key key, KeyCode legacy)
    {
        Keyboard k = Keyboard.current;
        if (k != null) return k[key].wasPressedThisFrame;
        return Input.GetKeyDown(legacy);
    }

    private bool wasHeld;

    private static bool Held
    {
        get
        {
            KillCam cam = KillCam.Local;
            return cam != null && cam.Busy;
        }
    }

    private void Update()
    {
        if (!active || head == null) return;
        if (GillaFreecam.On) { wish = Vector3.zero; return; }
        // While the game has hold of you (grabbed, being killed, dead, respawning) the kill
        // camera owns the head, hands and body: keyboard and mouse stand down until it lets go.
        if (Held) { wish = Vector3.zero; jumpQueued = false; return; }

        // Left Ctrl: mouse lock, like Roblox's shift lock (the mouse turns the camera freely).
        if (KeyDown(UnityEngine.InputSystem.Key.LeftCtrl, KeyCode.LeftControl))
        {
            shiftLock = !shiftLock;
            Cursor.lockState = shiftLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shiftLock;
        }

        // Look: hold right mouse (or mouse lock), or the left/right arrow keys.
        Mouse mouse = Mouse.current;
        Vector2 delta = Vector2.zero;
        bool looking = shiftLock;
        if (mouse != null)
        {
            looking = looking || mouse.rightButton.isPressed;
            delta = mouse.delta.ReadValue();
        }
        else
        {
            looking = looking || Input.GetMouseButton(1);
            delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
        }
        if (looking)
        {
            yaw += delta.x * 0.1f * lookSpeed;
            pitch = Mathf.Clamp(pitch - delta.y * 0.1f * lookSpeed, -85f, 85f);
        }
        float turn = (Key(UnityEngine.InputSystem.Key.RightArrow, KeyCode.RightArrow) ? 1f : 0f) - (Key(UnityEngine.InputSystem.Key.LeftArrow, KeyCode.LeftArrow) ? 1f : 0f);
        yaw += turn * 120f * Time.deltaTime;
        head.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Move: WASD relative to where the camera faces, flat on the ground.
        float v = (Key(UnityEngine.InputSystem.Key.W, KeyCode.W) || Key(UnityEngine.InputSystem.Key.UpArrow, KeyCode.UpArrow) ? 1f : 0f)
                - (Key(UnityEngine.InputSystem.Key.S, KeyCode.S) || Key(UnityEngine.InputSystem.Key.DownArrow, KeyCode.DownArrow) ? 1f : 0f);
        float h = (Key(UnityEngine.InputSystem.Key.D, KeyCode.D) ? 1f : 0f) - (Key(UnityEngine.InputSystem.Key.A, KeyCode.A) ? 1f : 0f);
        // Grips: Z closes the left hand, C the right, left mouse button both. They pick up items
        // and body parts, and are a monster's grab. WASD is also the left stick, which is how a
        // walking monster moves.
        bool click = mouse != null ? mouse.leftButton.isPressed : Input.GetMouseButton(0);
        bool gl = click || Key(UnityEngine.InputSystem.Key.Z, KeyCode.Z);
        bool gr = click || Key(UnityEngine.InputSystem.Key.C, KeyCode.C);
        MonsterPlayer.TestGrips = (gl ? 1 : 0) | (gr ? 2 : 0);
        GillaSticks.DeskOn = true;
        GillaSticks.DeskLeft = Vector2.ClampMagnitude(new Vector2(h, v), 1f);

        Quaternion flat = Quaternion.Euler(0f, yaw, 0f);
        Vector3 dir = Vector3.ClampMagnitude(flat * new Vector3(h, 0f, v), 1f);
        bool run = Key(UnityEngine.InputSystem.Key.LeftShift, KeyCode.LeftShift);
        wish = dir * (run ? RunSpeed : WalkSpeed);
        if (KeyDown(UnityEngine.InputSystem.Key.Space, KeyCode.Space)) jumpQueued = true;

        if (body == null)
        {
            // no physics body to push: slide the transform (the original rig's behaviour)
            transform.position += wish * Time.deltaTime;
        }

        // Hands float in front of the camera; Q and E reach them forward.
        float leftReach = Key(UnityEngine.InputSystem.Key.Q, KeyCode.Q) ? 0.75f : 0.45f;
        float rightReach = Key(UnityEngine.InputSystem.Key.E, KeyCode.E) ? 0.75f : 0.45f;
        if (leftHand != null)
        {
            leftHand.position = head.position + head.rotation * new Vector3(-0.25f, -0.35f, leftReach);
            leftHand.rotation = head.rotation * GripFromAim;
        }
        if (rightHand != null)
        {
            rightHand.position = head.position + head.rotation * new Vector3(0.25f, -0.35f, rightReach);
            rightHand.rotation = head.rotation * GripFromAim;
        }
    }

    private void FixedUpdate()
    {
        if (!active || body == null) return;
        if (Held)
        {
            wasHeld = true;
            return;
        }
        if (wasHeld)
        {
            // just released or respawned: take the body back from the kill camera
            wasHeld = false;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            if (head != null) yaw = head.eulerAngles.y;
            pitch = 0f;
        }
        if (body.isKinematic) return;
        if (vrMove != null && vrMove.enabled) vrMove.enabled = false;
        // The VR script normally keeps the body capsule under the head; do it here instead so
        // the player stands on the ground with feet at the rig's origin.
        if (vrMove != null && vrMove.bodyCollider != null)
        {
            CapsuleCollider capsule = vrMove.bodyCollider;
            capsule.enabled = true;
            capsule.isTrigger = false;
            capsule.direction = 1;
            capsule.center = Vector3.zero;
            capsule.height = 1.5f / Mathf.Max(capsule.transform.lossyScale.y, 0.01f);
            capsule.transform.rotation = Quaternion.identity;
            capsule.transform.position = transform.position + Vector3.up * 0.75f;
        }

        // Grounded when something solid is just under the feet.
        Vector3 origin = transform.position + Vector3.up * 0.6f;
        RaycastHit hit;
        grounded = Physics.SphereCast(origin, 0.2f, Vector3.down, out hit, 0.75f, ~0, QueryTriggerInteraction.Ignore)
                   && !hit.collider.transform.IsChildOf(transform);

        Vector3 vel = body.linearVelocity;
        Vector3 flatVel = new Vector3(vel.x, 0f, vel.z);
        float rate = Acceleration * (grounded ? 1f : AirControl);
        flatVel = Vector3.MoveTowards(flatVel, wish, rate * Time.fixedDeltaTime);
        vel.x = flatVel.x;
        vel.z = flatVel.z;
        if (jumpQueued && grounded)
        {
            vel.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * JumpHeight);
        }
        jumpQueued = false;
        body.linearVelocity = vel;
    }

    private void OnDisable()
    {
        if (shiftLock)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
