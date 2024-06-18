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