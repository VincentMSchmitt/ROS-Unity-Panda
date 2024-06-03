using UnityEngine;

namespace Panda.PickUp {
    public class FingerController : MonoBehaviour {
        private ArticulationBody articulation;

        void Start() {
            articulation = GetComponent<ArticulationBody>();            
        }

        // CONTROL
        public void UpdateGrip(float goal) {
            var drive = articulation.xDrive;
            drive.target = goal;
            articulation.xDrive = drive;
        }

        public void Open() {
            float goal = Mathf.Max(articulation.xDrive.lowerLimit, articulation.xDrive.upperLimit);
            UpdateGrip(goal);
        }

        public void Close() {
            float goal = Mathf.Min(articulation.xDrive.lowerLimit, articulation.xDrive.upperLimit);
            UpdateGrip(goal);
        }
    }
}