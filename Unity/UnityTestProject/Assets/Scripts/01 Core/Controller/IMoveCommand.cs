using System.Collections;
using UnityEngine;

namespace Panda.Core.Controller {
    public interface IMoveCommand {
        void SetDriveType (ArticulationDriveType type);
        void SetToMaxForce();
        void ResetForce();
        float GetTarget();
        IEnumerator MoveToTarget(float target, float speed);
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