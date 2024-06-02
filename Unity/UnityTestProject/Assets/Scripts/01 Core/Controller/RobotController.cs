using System.Collections.Generic;
using UnityEngine;

namespace Panda.Core.Controller {
    public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };
    public enum ControlType { PositionControl, Ros };
    public class RobotController : MonoBehaviour {
        public Color selectionColor;
        private ArticulationBody[] articulationChain;
        private List<RobotJoint> joints;
        private int selectedJointIndex;
        private RobotJoint selectedJoint {
            get { return joints[selectedJointIndex]; }
        }
        private ISelectionObserver selectionObserver;
        private static RobotController _singleton;

        public static RobotController GetInstance() {
            if (_singleton == null) {
                _singleton = new GameObject("RobotController").AddComponent<RobotController>();
            }
            return _singleton;
        }

        // Initialization and configuration
        void Start() {
            selectedJointIndex = -1;
            joints = new();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            for (int i = 0; i < articulationChain.Length; ++i) {
                // articulationChain[i].gameObject.AddComponent<RobotJoint>();

                if (articulationChain[i].jointType != ArticulationJointType.FixedJoint) {
                    RobotJoint joint = new(articulationChain[i]);
                    joints.Add(joint);
                }
            }
            selectionObserver = new SelectionObserver(selectionColor);
        }

        // called every frame
        void Update() {
            selectionObserver.SetSelectionColor(selectionColor);
            int oldJointIndex = selectedJointIndex;
            switch (true) {
                case bool _ when Input.GetKeyDown(KeyCode.RightArrow):
                    IncreaseIndex();
                    break;
                case bool _ when Input.GetKeyDown(KeyCode.LeftArrow):
                    DecreaseIndex();
                    break;
            }
            if (oldJointIndex != selectedJointIndex) {
                selectionObserver.OnJointSelected(selectedJoint);
            }
        }

        private void IncreaseIndex() {
            // keep index safely within the limits of the array
            selectedJointIndex = (++selectedJointIndex + joints.Count) % joints.Count;
        }

        private void DecreaseIndex() {
            if (selectedJointIndex < 0) {
                selectedJointIndex = joints.Count - 1;
            } else {
                selectedJointIndex = (--selectedJointIndex + joints.Count) % joints.Count;
            }
        }
    }
}