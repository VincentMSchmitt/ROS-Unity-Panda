/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

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