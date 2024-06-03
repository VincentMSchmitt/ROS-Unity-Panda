using UnityEngine;

namespace Panda.Core.Controller {
    public enum RotationDirection { Clockwise = 1, CounterClockwise = -1 };
    public partial class RobotJoint: IMoveCommand {
        public ArticulationBody joint { get; private set; }
        private ArticulationDrive xDrive;

        public RobotJoint(ArticulationBody joint) {
            this.joint = joint;
        }

        public void MoveClockwise() {
            Move(RotationDirection.Clockwise);
        }

        public void MoveCounterClockwise() {
            Move(RotationDirection.CounterClockwise);
        }

        public void SetDriveType (ArticulationDriveType type) {
            xDrive = joint.xDrive;
            xDrive.driveType = type;
            joint.xDrive = xDrive;
        }
        
        private void Move(RotationDirection direction) {
            // num is the value by which the target of the drive is to be changed in this update. It is based on the
            // direction of movement, the fixed delta time and the speed of the controller
            float num = (float)direction * Time.fixedDeltaTime * RobotController.GetInstance.speed;

            xDrive = joint.xDrive;
            // for differen joint type do different things
            switch (joint.jointType) {
                case ArticulationJointType.FixedJoint:
                    return;
                case ArticulationJointType.RevoluteJoint:
                    xDrive.target = CalculateTarget(xDrive.target, num, joint.twistLock, xDrive.upperLimit, xDrive.lowerLimit);
                    break;
                case ArticulationJointType.PrismaticJoint:
                    xDrive.target = CalculateTarget(xDrive.target, num, joint.linearLockX, xDrive.upperLimit, xDrive.lowerLimit);
                    break;
                default:
                    Debug.LogAssertion("Tried to support unsupported type of joint: " + joint.jointType);
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