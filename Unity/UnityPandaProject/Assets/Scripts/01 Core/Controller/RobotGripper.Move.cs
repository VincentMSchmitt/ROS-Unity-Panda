using System.Collections;
using UnityEngine;

namespace Panda.Core.Controller {
    public static class GripperTarget {
        public const float Open = 0.04f;
        public const float Close = 0.0f;
    }

    public partial class RobotGripper : IMoveCommand {
        public float GetTarget() {
            // assume finger1 and finger2 have identical target
            return finger1.joint.xDrive.target;
        }

        public IEnumerator MoveToTarget(float target, float speed) {
            // assume finger1 and finger2 have identical target
            ArticulationDrive xDrive1 = finger1.joint.xDrive;
            ArticulationDrive xDrive2 = finger2.joint.xDrive;
            xDrive1.target = target;
            xDrive2.target = target;
            xDrive1.targetVelocity = initialVelocity * speed;
            xDrive2.targetVelocity = initialVelocity * speed;
            finger1.joint.xDrive = xDrive1;
            finger2.joint.xDrive = xDrive2;
            while (!IsAtTarget(target)) {
                yield break;
            }
        }
     
        private bool IsAtTarget(float target) {
            if (finger1.joint.xDrive.target != target &&
                finger2.joint.xDrive.target != target) {
                return true;
            }
            else {
                return false;
            }
        }

        public void SetDriveType(ArticulationDriveType type) {
            ArticulationDrive xDrive1 = finger1.joint.xDrive;
            ArticulationDrive xDrive2 = finger1.joint.xDrive;
            xDrive1.driveType = type;
            xDrive2.driveType = type;
            finger1.joint.xDrive = xDrive1;
            finger1.joint.xDrive = xDrive2;
        }

        public ArticulationJointType JointType() {
            // assume, that left and right finger have same jointType
            return finger1.joint.jointType;
        }

        public void SetToMaxForce () {
            finger1.SetToMaxForce();
            finger2.SetToMaxForce();
        }

        public void ResetForce () {
            finger1.ResetForce();
            finger2.ResetForce();
        }

        public void Move(MoveDirection direction) {
            ArticulationDrive xDrive1 = finger1.joint.xDrive;
            ArticulationDrive xDrive2 = finger2.joint.xDrive;
            // num is the value by which the target of the drive is to be changed in this update. It is based on the
            // direction of movement, the fixed delta time and the speed of the controller
            float num = (float)direction * Time.fixedDeltaTime * RobotController.GetInstance.speed / 1500;
            xDrive1.target = CalculateTarget(xDrive1.target, num, finger1.joint.linearLockX, xDrive1.upperLimit, xDrive1.lowerLimit);
            xDrive2.target = CalculateTarget(xDrive2.target, num, finger2.joint.linearLockX, xDrive2.upperLimit, xDrive2.lowerLimit);
            finger1.joint.xDrive = xDrive1;
            finger2.joint.xDrive = xDrive2;
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