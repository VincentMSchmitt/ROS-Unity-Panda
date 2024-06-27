/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using UnityEngine;

namespace Panda.PickUp {
    public enum HandState { Fixed = 0, MovingUp = 1, MovingDown = -1 };
    public class VerticalController : MonoBehaviour {
        public HandState moveState = HandState.Fixed;
        public float speed = 0.05f;

        private void FixedUpdate() {
            if (moveState != HandState.Fixed) {
                ArticulationBody articulationBody = GetComponent<ArticulationBody>();

                //get jointPosition along y axis
                float xDrivePostion = articulationBody.jointPosition[0];

                //increment this y position
                float targetPosition = xDrivePostion + -(float)moveState * Time.fixedDeltaTime * speed;

                //set joint Drive to new position
                var drive = articulationBody.xDrive;
                drive.target = targetPosition;
                articulationBody.xDrive = drive;
            }
        }
    }
}