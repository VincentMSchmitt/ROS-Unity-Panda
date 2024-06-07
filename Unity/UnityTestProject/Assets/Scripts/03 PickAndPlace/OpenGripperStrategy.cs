using System.Collections.Generic;
using Panda.Core.Controller;
using UnityEngine;

namespace Panda.PickAndPlace {
    public class OpenGripperStrategy : IGripperStrategy {
        public void Execute(ArticulationBody leftGripper, ArticulationBody rightGripper) {
            var leftDrive = leftGripper.xDrive;
            var rightDrive = rightGripper.xDrive;
            leftDrive.target = 0.04f;
            rightDrive.target = 0.04f;
            leftGripper.xDrive = leftDrive;
            rightGripper.xDrive = rightDrive;
        }

        public void Execute(List<IMoveCommand> gripperJoints) {
            throw new System.NotImplementedException();
        }
    }
}