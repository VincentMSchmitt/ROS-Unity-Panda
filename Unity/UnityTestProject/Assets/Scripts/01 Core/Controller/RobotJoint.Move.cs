using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotJoint: IMoveCommand, IJointCommand {
        public ArticulationBody joint { get; private set; }
        private float savedForce;

        public RobotJoint(ArticulationBody joint) {
            this.joint = joint;
            StoreForce();
        }

        public void MoveClockwise() {
            Move(MoveDirection.Clockwise);
        }

        public void MoveCounterClockwise() {
            Move(MoveDirection.CounterClockwise);
        }

        public void MoveUp() {
            Move(MoveDirection.Up);
        }

        public void MoveDown() {
            Move(MoveDirection.Down);
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
        
        private void Move(MoveDirection direction) {
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
                    Debug.LogAssertion("An error has ourrured while trying to the joint: " + joint.name);
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