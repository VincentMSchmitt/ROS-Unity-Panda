using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotJoint: IMoveCommand {
        public ArticulationBody joint { get; private set; }
        private bool isSelected;

        public RobotJoint(ArticulationBody joint) {
            this.joint = joint;
        }

        public void SetSelected(bool selected) {
            isSelected = selected;
        }

        public void MoveClockwise(float amount) {
            // Logic for clockwise movement of the joint
        }

        public void MoveCounterClockwise(float amount) {
            // Logic for moving the joint counterclockwise
        }
    }
}