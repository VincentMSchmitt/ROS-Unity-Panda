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

        // INPUT HELPERS
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

