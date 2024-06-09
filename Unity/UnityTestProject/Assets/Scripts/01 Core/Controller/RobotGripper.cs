using System;
using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotGripper : ICombinedInterface {
        private RobotJoint finger1 { get; set; }
        private RobotJoint finger2 { get; set; }
        private float initialVelocity = 0.0f;

        public RobotGripper(RobotJoint finger1, RobotJoint finger2) {
            if (finger1.joint.jointType != ArticulationJointType.PrismaticJoint || 
                finger2.joint.jointType != ArticulationJointType.PrismaticJoint) {
                    throw new ArgumentException("Parameter is not excepted joint type", nameof(ArticulationJointType.PrismaticJoint));
            }
            this.finger1 = finger1;
            this.finger2 = finger2;
            // assuming finger1 and finger2 have the same velocity
            this.initialVelocity = finger1.joint.xDrive.targetVelocity;
        }
    }
}