using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotJoint: ICombinedInterface {
        public ArticulationBody joint { get; private set; }
        private float savedForce;

        public RobotJoint(ArticulationBody joint) {
            this.joint = joint;
            StoreForce();
        }
    }
}