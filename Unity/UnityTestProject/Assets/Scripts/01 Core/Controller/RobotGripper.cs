using System;
using System.Collections;
using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotGripper : IMoveCommand, IGripperCommand, IJointVisualization {
        private RobotJoint finger1 { get; set; }
        private RobotJoint finger2 { get; set; }

        public RobotGripper(RobotJoint finger1, RobotJoint finger2) {
            if (finger1.joint.jointType != ArticulationJointType.PrismaticJoint || 
                finger2.joint.jointType != ArticulationJointType.PrismaticJoint) {
                    throw new ArgumentException("Parameter is not excepted joint type", nameof(ArticulationJointType.PrismaticJoint));
            }
            this.finger1 = finger1;
            this.finger2 = finger2;
        }

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
            xDrive1.targetVelocity *= speed;
            xDrive2.targetVelocity *= speed;
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

        public void MoveGripperOpen() {
            Move(MoveDirection.Open);
        }

        public void MoveGripperClose() {
            Move(MoveDirection.Close);
        }

        public void SetDriveType(ArticulationDriveType type) {
            ArticulationDrive xDrive1 = finger1.joint.xDrive;
            ArticulationDrive xDrive2 = finger1.joint.xDrive;
            xDrive1.driveType = type;
            xDrive2.driveType = type;
            finger1.joint.xDrive = xDrive1;
            finger1.joint.xDrive = xDrive2;
        }

        public void Highlight(Color color) {
            finger1.Highlight(color);
            finger2.Highlight(color);
        }

        public void ResetHighlight(Color[] colors) {
            finger1.ResetHighlight(colors);
            finger2.ResetHighlight(colors);
        }

        public Color[] StoreJointColors() {
            // assume, that left and right finger have same color
            return finger1.StoreJointColors();
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

        private void Move(MoveDirection direction) {
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