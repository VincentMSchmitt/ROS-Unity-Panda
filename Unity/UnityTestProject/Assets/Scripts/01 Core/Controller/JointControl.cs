using UnityEngine;
using UnityEngine.Assertions;

namespace Panda.Core.Controller {
    public class JointControl : MonoBehaviour {
        [HideInInspector] public RotationDirection direction = RotationDirection.None;
        [HideInInspector] public ControlType controltype;
        [HideInInspector] public ArticulationBody joint;
        private RobotController controller;

        private void Start() {
            controller = (RobotController)GetComponentInParent(typeof(RobotController));
            joint = GetComponent<ArticulationBody>();
            controller.UpdateControlType(this);
        }

        // Is called every fixed framerate frame. FixedUpdate should be used when applying forces, torques, or other
        // physics-related functions - because it will be executed exactly in sync with the physics engine itself
        private void FixedUpdate() {
            // TODO: dont add JointControl to fixed joints, then this is not needed
            if (controltype != ControlType.PositionControl) {
                return;
            }

            // num is the value by which the target of the drive is to be changed in this update. It is based on the
            // direction of movement, the fixed delta time and the speed of the controller
            float num = (float)direction * Time.fixedDeltaTime * controller.speed;

            // TODO: build and joint interface and dirive differen types of joints from that interface
            ArticulationDrive xDrive = joint.xDrive;
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