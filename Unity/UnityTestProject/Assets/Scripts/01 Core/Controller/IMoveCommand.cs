using UnityEngine;

namespace Panda.Core.Controller {
    public interface IMoveCommand {
        void SetDriveType (ArticulationDriveType type);
        void SetToMaxForce();
        void ResetForce();
        ArticulationJointType JointType(); 
    }

    public interface IJointCommand {
        void MoveClockwise();
        void MoveCounterClockwise();
    }

    public interface IGripperCommand {
        void MoveGripperOpen();
        void MoveGripperClose();
    }
}