// Reconstructed from the IL2CPP build (decompiled, not the original source).
using UnityEngine;

namespace GorillaLocomotion
{
    public partial class Player : MonoBehaviour
    {
        public void Turn(float degrees)
        {
            Vector3 pivot = headCollider.transform.position;
            Quaternion rotation = Quaternion.AngleAxis(degrees, transform.up);
            transform.RotateAround(pivot, transform.up, degrees);
            denormalizedVelocityAverage = Quaternion.Euler(0f, degrees, 0f) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0f, degrees, 0f) * velocityHistory[i];
            }
            lastPosition = pivot + rotation * (lastPosition - pivot);
            lastLeftHandPosition = TurnedHand(lastLeftHandPosition, pivot, rotation, leftHandDisabled);
            lastRightHandPosition = TurnedHand(lastRightHandPosition, pivot, rotation, rightHandDisabled);
            lastLeftTurn = rotation * lastLeftTurn;
            lastRightTurn = rotation * lastRightTurn;
        }

        private Vector3 TurnedHand(Vector3 last, Vector3 pivot, Quaternion turn, bool disabled)
        {
            Vector3 result = pivot + turn * (last - pivot);
            Vector3 hit = Vector3.zero;
            if (!disabled && IterativeCollisionSphereCast(last, minimumRaycastDistance, result - last, defaultPrecision, out hit, true))
            {
                result = hit;
            }
            return result;
        }

        private void StoreVelocities()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistorySize;
            Vector3 oldestVelocity = velocityHistory[velocityIndex];
            currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
            velocityHistory[velocityIndex] = currentVelocity;
            lastPosition = transform.position;
        }

        public Player()
        {
            defaultPrecision = 0.995f;
            maxArmLength = 1.5f;
            unStickDistance = 1f;
            minimumRaycastDistance = 0.05f;
            defaultSlideFactor = 0.03f;
            launchedUntil = -1f;
            lastLeftTurn = Quaternion.identity;
            leftWhich = -1;
            rightWhich = -1;
            lastRightTurn = Quaternion.identity;
        }
    }
}
