using UnityEngine;

namespace Panda.PickAndPlace {
    public class MoveCommand : ICommand {
        private ArticulationBody[] jointArticulationBodies;
        private float[] targetPositions;

        public MoveCommand(ArticulationBody[] joints, float[] positions) {
            jointArticulationBodies = joints;
            targetPositions = positions;
        }

        public void Execute() {
            for (int i = 0; i < jointArticulationBodies.Length; i++) {
                var joint1XDrive = jointArticulationBodies[i].xDrive;
                joint1XDrive.target = targetPositions[i];
                jointArticulationBodies[i].xDrive = joint1XDrive;
            }
        }
    }
}