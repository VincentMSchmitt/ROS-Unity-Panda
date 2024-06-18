using System.Collections;
using UnityEngine;

namespace Panda.Core.Controller {
    public enum MoveDirection { Clockwise = 1, CounterClockwise = -1, Open = 1, Close = -1, Up = 1, Down = -1 };
    public interface IMoveCommand {
        void SetDriveType (ArticulationDriveType type);
        void SetToMaxForce();
        void ResetForce();
        float GetTarget();
        void Move(MoveDirection direction);
        IEnumerator MoveToTarget(float target, float speed);
        ArticulationJointType JointType();
    }
}