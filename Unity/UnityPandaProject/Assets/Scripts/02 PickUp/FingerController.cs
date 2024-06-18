using UnityEngine;

namespace Panda.PickUp {
    public class FingerController : MonoBehaviour {
        private ArticulationBody articulation;
        public float maxPositionX;
        public float minPositionX;

        void Start() {
            articulation = GetComponent<ArticulationBody>();
            maxPositionX = Mathf.Max(articulation.xDrive.upperLimit, articulation.xDrive.lowerLimit);
            minPositionX = Mathf.Min(articulation.xDrive.upperLimit, articulation.xDrive.lowerLimit);
        }

        public void Open() {
            float num = (float)GripState.Opening * Time.fixedDeltaTime * HorizontalController.speed;
            float gripTarget = CalculateTarget(articulation.xDrive.target, num, articulation.linearLockX, articulation.xDrive.upperLimit, articulation.xDrive.lowerLimit);
            UpdateGrip(gripTarget);
        }

        public void Close() {
            float num = (float)GripState.Closing * Time.fixedDeltaTime * HorizontalController.speed;
            float gripTarget = CalculateTarget(articulation.xDrive.target, num, articulation.linearLockX, articulation.xDrive.upperLimit, articulation.xDrive.lowerLimit);
            UpdateGrip(gripTarget);
        }

        public float CurrentGrip() {
            float grip = Mathf.InverseLerp(minPositionX, maxPositionX, transform.localPosition.x);
            return grip;
        }

        public void UpdateGrip(float gripTarget) {
            var drive = articulation.xDrive;
            drive.target = gripTarget;
            articulation.xDrive = drive;
        }

        private float CalculateTarget(float currentTarget, float num, ArticulationDofLock dofLock, float upperLimit, float lowerLimit) {
            // if the motion is limited
            if (dofLock == ArticulationDofLock.LimitedMotion) {
                // check for upper limit
                if (currentTarget + num > upperLimit) {
                    return upperLimit;
                }
                // check for lower limit
                else if (currentTarget + num < lowerLimit) {
                    return lowerLimit;
                }
                else {
                    return currentTarget + num;
                }
            }
            // if the motion is not limited
            else {
                return currentTarget + num;
            }
        }
    }
}