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
    public class InputController : MonoBehaviour {
        public GameObject pandaHand;

        void Update() {
            // arrow keys or w/s
            float inputVertical = Input.GetAxis("Vertical");
            HandState moveState = MoveStateForInput(inputVertical);
            VerticalController verticalController = pandaHand.GetComponent<VerticalController>();
            verticalController.moveState = moveState;

            // arrow keys or a/d
            float inputHorizontal = Input.GetAxis("Horizontal");
            HorizontalController horizontalController = pandaHand.GetComponent<HorizontalController>();
            horizontalController.gripState = GripStateForInput(inputHorizontal);
        }

        static GripState GripStateForInput(float input) {
            if (input > 0) {
                return GripState.Opening;
            }
            else if (input < 0) {
                return GripState.Closing;
            }
            else {
                return GripState.Fixed;
            }
        }

        HandState MoveStateForInput(float input) {
            if (input > 0) {
                return HandState.MovingUp;
            }
            else if (input < 0) {
                return HandState.MovingDown;
            }
            else {
                return HandState.Fixed;
            }
        }
    }
}

