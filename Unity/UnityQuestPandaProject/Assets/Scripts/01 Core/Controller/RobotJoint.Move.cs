/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using System.Collections;
using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotJoint: IMoveCommand {
        public float GetTarget() {
            return joint.xDrive.target;
        }

        public IEnumerator MoveToTarget(float target, float speed) {
            ArticulationDrive xDrive = joint.xDrive;
            xDrive.target = target;
            xDrive.targetVelocity *= speed;
            joint.xDrive = xDrive;
            while (!IsAtTarget(target)) {
                yield break;
            }
        }
     
        private bool IsAtTarget(float target) {
            if (joint.xDrive.target != target) {
                return true;
            }
            else {
                return false;
            }
        }

        public void SetDriveType (ArticulationDriveType type) {
            ArticulationDrive xDrive = joint.xDrive;
            xDrive.driveType = type;
            joint.xDrive = xDrive;
        }

        public ArticulationJointType JointType() {
            return joint.jointType;
        }

        public void SetToMaxForce() {
            ArticulationDrive xDrive = joint.xDrive;
            xDrive.forceLimit = 3.402823e+38f;
            joint.xDrive = xDrive;
        }

        public void ResetForce () {
            ArticulationDrive xDrive = joint.xDrive;
            xDrive.forceLimit = savedForce;
            joint.xDrive = xDrive;
        }

        public void StoreForce() {
            savedForce = joint.xDrive.forceLimit;
        }
        
        public void Move(MoveDirection direction) {
            // num is the value by which the target of the drive is to be changed in this update. It is based on the
            // direction of movement, the fixed delta time and the speed of the controller
            float num = 0.0f;

            ArticulationDrive xDrive = joint.xDrive;
            // for differen joint type do different things
            switch (joint.jointType) {
                case ArticulationJointType.FixedJoint:
                    return;
                case ArticulationJointType.RevoluteJoint:
                    num = (float)direction * Time.fixedDeltaTime * RobotController.GetInstance.speed;
                    xDrive.target = CalculateTarget(xDrive.target, num, joint.twistLock, xDrive.upperLimit, xDrive.lowerLimit);
                    break;
                case ArticulationJointType.PrismaticJoint:
                    num = (float)direction * Time.fixedDeltaTime * RobotController.GetInstance.speed;
                    xDrive.target = CalculateTarget(xDrive.target, num, joint.linearLockX, xDrive.upperLimit, xDrive.lowerLimit);
                    break;
                default:
                    Debug.LogAssertion("An error has ourrured while trying to move the joint: " + joint.name);
                    return;
            }
            joint.xDrive = xDrive;
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