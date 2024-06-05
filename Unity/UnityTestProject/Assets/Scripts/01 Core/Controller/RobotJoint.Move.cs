using UnityEngine;

namespace Panda.Core.Controller {
    public enum RotationDirection { Clockwise = 1, CounterClockwise = -1, Open = 1, Close = -1 };
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

        public void MoveGripperOpen() {
            Move(RotationDirection.Open);
        }
        public void MoveGripperClose() {
            Move(RotationDirection.Close);
        }

        public void SetDriveType (ArticulationDriveType type) {
            xDrive = joint.xDrive;
            xDrive.driveType = type;
            joint.xDrive = xDrive;
        }
        
        private void Move(RotationDirection direction) {
            // num is the value by which the target of the drive is to be changed in this update. It is based on the
            // direction of movement, the fixed delta time and the speed of the controller
            float num = 0.0f;

            xDrive = joint.xDrive;
            // for differen joint type do different things
            switch (joint.jointType) {
                case ArticulationJointType.FixedJoint:
                    return;
                case ArticulationJointType.RevoluteJoint:
                    num = (float)direction * Time.fixedDeltaTime * RobotController.GetInstance.speed;
                    xDrive.target = CalculateTarget(xDrive.target, num, joint.twistLock, xDrive.upperLimit, xDrive.lowerLimit);
                    break;
                case ArticulationJointType.PrismaticJoint:
                    // these need a much slower speed
                    num = (float)direction * Time.fixedDeltaTime * RobotController.GetInstance.speed/500;
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