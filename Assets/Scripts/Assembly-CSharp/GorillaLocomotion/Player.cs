// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

namespace GorillaLocomotion
{
    public partial class Player : MonoBehaviour
    {
        private static Player _instance;
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;
        public Transform leftHandFollower;
        public Transform rightHandFollower;
        public Transform rightHandTransform;
        public Transform leftHandTransform;
        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private Vector3 lastHeadPosition;
        private Rigidbody playerRigidBody;
        public int velocityHistorySize;
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;
        public float velocityLimit;
        public float maxJumpSpeed;
        public float jumpMultiplier;
        public float minimumRaycastDistance = 0.05f;
        public float defaultSlideFactor = 0.03f;
        public float defaultPrecision = 0.995f;
        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 currentVelocity;
        private Vector3 denormalizedVelocityAverage;
        private bool jumpHandIsLeft;
        private Vector3 lastPosition;
        public Vector3 rightHandOffset;
        public Vector3 leftHandOffset;
        public LayerMask locomotionEnabledLayers;
        public bool wasLeftHandTouching;
        public bool wasRightHandTouching;
        public bool disableMovement;
        public bool leftHandDisabled;
        public bool rightHandDisabled;
        private float hitSlip;
        private Vector3 hitNormal;
        private float leftSlip;
        private float rightSlip;
        private Vector3 leftSlipNormal;
        private Vector3 rightSlipNormal;
        private float launchedUntil = -1f;
        public HeldShape leftHeld;
        public HeldShape rightHeld;
        private Quaternion lastLeftTurn = Quaternion.identity;
        private Quaternion lastRightTurn = Quaternion.identity;
        private int leftWhich = -1;
        private int rightWhich = -1;
        private Vector3 hitPoint;
        private Vector3 leftHeldAt;
        private Vector3 rightHeldAt;
        private Vector3 leftHeldFaces;
        private Vector3 rightHeldFaces;
        private Collider hitCollider;
        private Collider leftHeldOn;
        private Collider rightHeldOn;

        public static Player Instance
        {
            get { return _instance; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Object.Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
            InitializeValues();
        }

        public Vector3 HandNow(bool right)
        {
            if (!right)
            {
                return CurrentLeftHandPosition();
            }
            return CurrentRightHandPosition();
        }

        private void FollowerToHand(Transform follower, Vector3 hand)
        {
            RaycastHit hit;
            Vector3 origin = headCollider.transform.position;
            Vector3 delta = hand - origin;
            float distance = delta.magnitude;
            if (distance > 0.0001f)
            {
                Vector3 dir = delta / distance;
                if (Physics.SphereCast(origin, minimumRaycastDistance, dir, out hit, distance, locomotionEnabledLayers.value, QueryTriggerInteraction.Ignore))
                {
                    hand = origin + dir * hit.distance;
                }
            }
            follower.position = hand;
        }

        public void InitializeValues()
        {
            playerRigidBody = GetComponent<Rigidbody>();
            if (leftHandFollower != null && leftHandTransform != null && headCollider != null)
            {
                FollowerToHand(leftHandFollower, CurrentLeftHandPosition());
            }
            if (rightHandFollower != null && rightHandTransform != null && headCollider != null)
            {
                FollowerToHand(rightHandFollower, CurrentRightHandPosition());
            }
            velocityHistory = new Vector3[velocityHistorySize];
            denormalizedVelocityAverage = Vector3.zero;
            wasLeftHandTouching = false;
            wasRightHandTouching = false;
            currentVelocity = Vector3.zero;
            lastLeftHandPosition = leftHandFollower.transform.position;
            lastRightHandPosition = rightHandFollower.transform.position;
            if (leftHandTransform != null)
            {
                lastLeftTurn = leftHandTransform.rotation;
            }
            if (rightHandTransform != null)
            {
                lastRightTurn = rightHandTransform.rotation;
            }
            leftWhich = -1;
            rightWhich = -1;
            lastHeadPosition = headCollider.transform.position;
            velocityIndex = 0;
            lastPosition = transform.position;
            launchedUntil = -1f;
        }

        private Vector3 CurrentLeftHandPosition()
        {
            if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(leftHandTransform, leftHandOffset);
            }
            return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
        }

        private Vector3 CurrentRightHandPosition()
        {
            if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(rightHandTransform, rightHandOffset);
            }
            return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
        }

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private void Update()
        {
            RaycastHit hit;
            Vector3 end;
            int castWhich;

            Vector3 leftMove = Vector3.zero;
            Vector3 rightMove = Vector3.zero;

            bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);
            leftSlip = 0f;
            rightSlip = 0f;
            float now = Time.time;
            bool notLaunched = launchedUntil <= now;
            Quaternion leftTurn = leftHandTransform.rotation;
            Quaternion rightTurn = rightHandTransform.rotation;
            Vector3 gravity = (Vector3.down + Vector3.down) * 9.8f * Time.deltaTime * Time.deltaTime * Mathf.Min(transform.lossyScale.y, 1f);

            bool leftHit = false;
            int leftIdx = -1;
            Vector3 leftNow = CurrentLeftHandPosition();
            if (!leftHandDisabled && notLaunched)
            {
                if (HandCast(leftHeld, lastLeftHandPosition, lastLeftTurn, gravity + (leftNow - lastLeftHandPosition), leftTurn, true, out end, out castWhich))
                {
                    Vector3 m;
                    if (!wasLeftHandTouching)
                    {
                        m = end - CurrentLeftHandPosition();
                        leftIdx = castWhich;
                        if (castWhich >= 0)
                        {
                            leftHeldAt = hitPoint;
                            leftHeldFaces = hitNormal;
                            leftHeldOn = hitCollider;
                        }
                    }
                    else
                    {
                        m = StillOn(leftHeld, leftWhich, lastLeftHandPosition, lastLeftTurn, CurrentLeftHandPosition(), leftTurn);
                        leftIdx = leftWhich;
                    }
                    leftMove = Touch(m, out leftSlip, out leftSlipNormal);
                    leftHit = true;
                }
            }

            bool rightHit = false;
            int rightIdx = -1;
            Vector3 rightNow = CurrentRightHandPosition();
            if (!rightHandDisabled && now >= launchedUntil)
            {
                if (HandCast(rightHeld, lastRightHandPosition, lastRightTurn, gravity + (rightNow - lastRightHandPosition), rightTurn, true, out end, out castWhich))
                {
                    Vector3 m;
                    if (!wasRightHandTouching)
                    {
                        m = end - CurrentRightHandPosition();
                        rightIdx = castWhich;
                        if (castWhich >= 0)
                        {
                            rightHeldAt = hitPoint;
                            rightHeldFaces = hitNormal;
                            rightHeldOn = hitCollider;
                        }
                    }
                    else
                    {
                        m = StillOn(rightHeld, rightWhich, lastRightHandPosition, lastRightTurn, CurrentRightHandPosition(), rightTurn);
                        rightIdx = rightWhich;
                    }
                    rightMove = Touch(m, out rightSlip, out rightSlipNormal);
                    rightHit = true;
                }
            }

            Vector3 rigidBodyMovement;
            if ((leftHit || wasLeftHandTouching) && (rightHit || wasRightHandTouching))
            {
                rigidBodyMovement = (leftMove + rightMove) * 0.5f;
            }
            else
            {
                rigidBodyMovement = leftMove + rightMove;
            }

            if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius, rigidBodyMovement + headCollider.transform.position - lastHeadPosition, defaultPrecision, out end, false))
            {
                rigidBodyMovement = end - lastHeadPosition;
                Vector3 dir = rigidBodyMovement + (headCollider.transform.position - lastHeadPosition);
                Vector3 dir2 = rigidBodyMovement + (headCollider.transform.position - lastHeadPosition);
                if (Physics.Raycast(lastHeadPosition, dir, out hit, dir2.magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
                }
            }

            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position = transform.position + rigidBodyMovement;
            }

            lastHeadPosition = headCollider.transform.position;

            bool leftTouching = leftHit;
            Vector3 leftHand = CurrentLeftHandPosition();
            bool leftDone = false;
            if (!leftHandDisabled)
            {
                bool single = !((leftTouching || wasLeftHandTouching) && (rightHit || wasRightHandTouching));
                if (HandCast(leftHeld, lastLeftHandPosition, lastLeftTurn, leftHand - lastLeftHandPosition, leftTurn, single, out end, out castWhich))
                {
                    lastLeftHandPosition = end;
                    leftSlip = Mathf.Clamp01(hitSlip);
                    leftSlipNormal = hitNormal;
                    leftIdx = castWhich;
                    leftTouching = notLaunched;
                    if (castWhich >= 0)
                    {
                        leftHeldAt = hitPoint;
                        leftHeldFaces = hitNormal;
                        leftHeldOn = hitCollider;
                    }
                    leftDone = true;
                }
            }
            if (!leftDone)
            {
                lastLeftHandPosition = CurrentLeftHandPosition();
            }

            bool rightTouching = notLaunched;
            Vector3 rightHand = CurrentRightHandPosition();
            bool rightDone = false;
            if (!rightHandDisabled)
            {
                bool single = !((leftTouching || wasLeftHandTouching) && (rightHit || wasRightHandTouching));
                if (HandCast(rightHeld, lastRightHandPosition, lastRightTurn, rightHand - lastRightHandPosition, rightTurn, single, out end, out castWhich))
                {
                    lastRightHandPosition = end;
                    rightSlip = Mathf.Clamp01(hitSlip);
                    rightSlipNormal = hitNormal;
                    rightIdx = castWhich;
                    if (castWhich >= 0)
                    {
                        rightHeldAt = hitPoint;
                        rightHeldFaces = hitNormal;
                        rightHeldOn = hitCollider;
                    }
                    rightDone = true;
                }
            }
            if (!rightDone)
            {
                lastRightHandPosition = CurrentRightHandPosition();
                rightTouching = rightHit;
            }

            StoreVelocities();

            bool grip = (leftTouching && leftSlip <= 0f) || (rightTouching && rightSlip <= 0f);
            if (grip)
            {
                if (!disableMovement)
                {
                    if (denormalizedVelocityAverage.magnitude > velocityLimit)
                    {
                        if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                        {
                            playerRigidBody.linearVelocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
                        }
                        else
                        {
                            playerRigidBody.linearVelocity = jumpMultiplier * denormalizedVelocityAverage;
                        }
                    }
                }
            }
            else if (leftTouching || rightTouching)
            {
                if (!disableMovement)
                {
                    if (leftTouching)
                    {
                        PushOff(leftSlipNormal);
                    }
                    if (rightTouching)
                    {
                        PushOff(rightSlipNormal);
                    }
                }
            }

            if (leftTouching)
            {
                if ((CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance)
                {
                    Vector3 headPos = headCollider.transform.position;
                    Vector3 dir = CurrentLeftHandPosition() - headCollider.transform.position;
                    float dist = (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance;
                    if (!Physics.SphereCast(headPos, minimumRaycastDistance * defaultPrecision, dir, out hit, dist, locomotionEnabledLayers.value))
                    {
                        lastLeftHandPosition = CurrentLeftHandPosition();
                        leftTouching = false;
                    }
                }
            }

            if (rightTouching)
            {
                if ((CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance)
                {
                    Vector3 headPos = headCollider.transform.position;
                    Vector3 dir = CurrentRightHandPosition() - headCollider.transform.position;
                    float dist = (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance;
                    if (!Physics.SphereCast(headPos, minimumRaycastDistance * defaultPrecision, dir, out hit, dist, locomotionEnabledLayers.value))
                    {
                        lastRightHandPosition = CurrentRightHandPosition();
                        rightTouching = false;
                    }
                }
            }

            leftHandFollower.position = leftHandDisabled ? headCollider.transform.position : lastLeftHandPosition;
            rightHandFollower.position = rightHandDisabled ? headCollider.transform.position : lastRightHandPosition;

            wasLeftHandTouching = leftTouching;
            leftWhich = leftTouching ? leftIdx : -1;
            wasRightHandTouching = rightTouching;
            rightWhich = rightTouching ? rightIdx : -1;
            lastLeftTurn = leftTurn;
            lastRightTurn = rightTurn;
        }

        private bool HandCast(HeldShape held, Vector3 from, Quaternion fromTurn, Vector3 move, Quaternion toTurn, bool singleHand, out Vector3 end, out int which)
        {
            which = -1;
            bool hit = IterativeCollisionSphereCast(from, minimumRaycastDistance, move, defaultPrecision, out end, singleHand);
            if (held != null && held.at.Length != 0)
            {
                Vector3 to = from + move;
                float best = -1f;
                if (hit)
                {
                    best = (end - to).sqrMagnitude;
                }
                Vector3 savedNormal = hitNormal;
                float savedSlip = hitSlip;
                Vector3 savedPoint = hitPoint;
                Collider savedCollider = hitCollider;
                if (Physics.CheckSphere(from + move * 0.5f, move.magnitude * 0.5f + held.reach + minimumRaycastDistance, locomotionEnabledLayers.value))
                {
                    for (int i = 0; i < held.at.Length; i++)
                    {
                        Vector3 a = from + fromTurn * held.at[i];
                        Vector3 b = to + toTurn * held.at[i];
                        float r = held.radius[i];
                        Vector3 seg = b - a;
                        Vector3 p;
                        if (Physics.CheckSphere((a + b) * 0.5f, seg.magnitude * 0.5f + r + 0.002f, locomotionEnabledLayers.value)
                            && IterativeCollisionSphereCast(a, r, seg, defaultPrecision, out p, singleHand))
                        {
                            Vector3 c = p - toTurn * held.at[i];
                            float d = (c - to).sqrMagnitude;
                            if (best < d)
                            {
                                hit = true;
                                end = c;
                                which = i;
                                savedNormal = hitNormal;
                                savedSlip = hitSlip;
                                savedPoint = hitPoint;
                                savedCollider = hitCollider;
                                best = d;
                            }
                        }
                    }
                    hitNormal = savedNormal;
                    hitSlip = savedSlip;
                    hitPoint = savedPoint;
                    hitCollider = savedCollider;
                    return hit;
                }
            }
            return hit;
        }

        private static Vector3 StillOn(HeldShape held, int which, Vector3 last, Quaternion lastTurn, Vector3 now, Quaternion turn)
        {
            if (held != null && which >= 0 && which < held.at.Length)
            {
                return (last + lastTurn * held.at[which]) - (now + turn * held.at[which]);
            }
            return last - now;
        }

        public bool HeldTouching(bool right, out Vector3 at, out Vector3 faces, out Collider on)
        {
            if (right)
            {
                at = rightHeldAt;
                faces = rightHeldFaces;
            }
            else
            {
                at = leftHeldAt;
                faces = leftHeldFaces;
            }
            on = right ? rightHeldOn : leftHeldOn;
            if (right)
            {
                if (!wasRightHandTouching)
                {
                    return false;
                }
                return rightWhich >= 0;
            }
            if (!wasLeftHandTouching)
            {
                return false;
            }
            return leftWhich >= 0;
        }

        private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 endPosition, bool singleHand)
        {
            RaycastHit hitInfo;
            float r = sphereRadius * precision;
            if (CollisionsSphereCast(startPosition, r, movementVector, precision, out endPosition, out hitInfo))
            {
                Vector3 first = endPosition;
                Surface surface = hitInfo.collider.GetComponent<Surface>();
                float slide;
                if (surface != null)
                {
                    slide = surface.slipPercentage;
                }
                else if (singleHand)
                {
                    slide = 0.001f;
                }
                else
                {
                    slide = defaultSlideFactor;
                }
                hitSlip = surface != null ? surface.slipPercentage : 0f;
                hitNormal = hitInfo.normal;
                hitPoint = hitInfo.point;
                hitCollider = hitInfo.collider;
                Vector3 target = startPosition + movementVector;
                Vector3 slideMove = Vector3.ProjectOnPlane(target - first, hitInfo.normal) * slide;
                if (!CollisionsSphereCast(endPosition, sphereRadius, slideMove, precision * precision, out endPosition, out hitInfo))
                {
                    Vector3 slid = first + slideMove;
                    if (!CollisionsSphereCast(slid, sphereRadius, target - slid, precision * precision * precision, out endPosition, out hitInfo))
                    {
                        endPosition = first;
                        return true;
                    }
                }
                return true;
            }
            if (CollisionsSphereCast(startPosition, r * 0.66f, movementVector.normalized * (r * 0.34f + movementVector.magnitude), precision * 0.66f, out endPosition, out hitInfo))
            {
                endPosition = startPosition;
                Surface surface = hitInfo.collider.GetComponent<Surface>();
                hitSlip = surface != null ? surface.slipPercentage : 0f;
                hitNormal = hitInfo.normal;
                hitPoint = hitInfo.point;
                hitCollider = hitInfo.collider;
                return true;
            }
            endPosition = Vector3.zero;
            return false;
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            RaycastHit inner;
            float distance = movementVector.magnitude;
            float r = sphereRadius * precision;
            if (Physics.SphereCast(startPosition, r, movementVector, out hitInfo, (1f - precision) * sphereRadius + distance, locomotionEnabledLayers.value))
            {
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;
                Vector3 dir = finalPosition - startPosition;
                float extra = (1f - precision * precision) * sphereRadius;
                if (Physics.SphereCast(startPosition, r * precision, dir, out inner, extra + dir.magnitude, locomotionEnabledLayers.value))
                {
                    Vector3 dir2 = finalPosition - startPosition;
                    finalPosition = startPosition + dir2.normalized * Mathf.Max(hitInfo.distance - extra, 0f);
                    hitInfo = inner;
                }
                else
                {
                    Vector3 dir2 = finalPosition - startPosition;
                    if (Physics.Raycast(startPosition, dir2, out inner, r * precision * 0.999f + dir2.magnitude, locomotionEnabledLayers.value))
                    {
                        finalPosition = startPosition;
                        hitInfo = inner;
                        return true;
                    }
                }
                return true;
            }
            if (Physics.Raycast(startPosition, movementVector, out hitInfo, r * 0.999f + distance, locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                return true;
            }
            finalPosition = Vector3.zero;
            return false;
        }

        private Vector3 Touch(Vector3 move, out float slip, out Vector3 normal)
        {
            slip = Mathf.Clamp01(hitSlip);
            normal = hitNormal;
            Rigidbody rb = playerRigidBody;
            if (slip <= 0f)
            {
                rb.linearVelocity = Vector3.zero;
                return move;
            }
            rb.linearVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, normal) * slip;
            float into = Vector3.Dot(move, normal);
            float keep = into;
            if (into <= 0f)
            {
                keep = into * (1f - slip);
            }
            return normal * keep + Vector3.ProjectOnPlane(move, normal) * (1f - slip);
        }

        private void PushOff(Vector3 normal)
        {
            if (Vector3.Dot(normal, denormalizedVelocityAverage) <= velocityLimit)
            {
                return;
            }
            Vector3 velocity = playerRigidBody.linearVelocity;
            playerRigidBody.linearVelocity = velocity /* ? */;
        }

        public bool IsHandTouching(bool forLeftHand)
        {
            return forLeftHand ? wasLeftHandTouching : wasRightHandTouching;
        }

        public void Slow(float keep)
        {
            playerRigidBody.linearVelocity = playerRigidBody.linearVelocity * keep;
            denormalizedVelocityAverage = denormalizedVelocityAverage * keep;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = velocityHistory[i] * keep;
            }
        }

        public void Launch(Vector3 velocity, float seconds)
        {
            InitializeValues();
            playerRigidBody.linearVelocity = velocity;
            launchedUntil = Time.time + seconds;
        }
    }
}
