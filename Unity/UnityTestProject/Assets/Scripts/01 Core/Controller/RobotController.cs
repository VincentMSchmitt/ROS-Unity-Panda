using System.Collections.Generic;
using System.Runtime.Serialization.Configuration;
using UnityEngine;

namespace Panda.Core.Controller {
    public enum ControlType { PositionControl, Moveit };
    public class RobotController : MonoBehaviour {
        public ControlType controlType = ControlType.PositionControl;
        public float speed = 50f;
        public Color selectionColor = Color.red;
        private ArticulationBody[] articulationChain;
        private List<RobotJoint> joints;
        private int selectedJointIndex = -1;
        private ISelectionObserver selectionObserver;
        private static RobotController instance;
        private RobotJoint selectedJoint {
            get => joints[selectedJointIndex];
        }
        public static RobotController GetInstance {
            get {
                if (instance == null) {
                    instance = FindObjectOfType<RobotController>();
                    if (instance == null) {
                        GameObject singletonObject = new();
                        instance = singletonObject.AddComponent<RobotController>();
                        singletonObject.name = typeof(RobotController).ToString() + " (Singleton)";
                        
                        // keep the singleton across scenes
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return instance;
            }
        }

        // Initialization and configuration
        private void Start() {
            joints = new();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            for (int i = 0; i < articulationChain.Length; ++i) {
                if (articulationChain[i].jointType != ArticulationJointType.FixedJoint) {
                    RobotJoint joint = new(articulationChain[i]);
                    joints.Add(joint);
                }
            }
            selectionObserver = new SelectionObserver(selectionColor);
        }

        // called every frame 
        private void Update() {
            // check current controlType
            UpadateControlType(controlType);

            if (controlType == ControlType.PositionControl) {
                // update color dynamicly while in play mode
                selectionObserver.SetSelectionColor(selectionColor);
                // Select the joints
                int oldJointIndex = selectedJointIndex;
                switch (true) {
                    case bool _ when Input.GetKeyDown(KeyCode.RightArrow):
                        IncreaseIndex();
                        break;
                    case bool _ when Input.GetKeyDown(KeyCode.LeftArrow):
                        DecreaseIndex();
                        break;
                }
                // new joint selected
                if (oldJointIndex != selectedJointIndex) {
                    selectionObserver.OnJointSelected(selectedJoint);
                }
            }
        }

        // called every physics update
        private void FixedUpdate() {
            // at start, dont do antyhing
            if (controlType != ControlType.PositionControl || selectedJointIndex < 0) {
                return;
            }
            // Move the robot
            switch (true) {
                case bool _ when Input.GetKey(KeyCode.UpArrow):
                        if (selectedJoint.joint.jointType == ArticulationJointType.RevoluteJoint) {
                            selectedJoint.MoveClockwise();
                        }
                        else if (selectedJoint.joint.jointType == ArticulationJointType.PrismaticJoint) {
                            selectedJoint.MoveGripperOpen();
                        }
                        else {
                            Debug.LogError("Tried to controll unsupported jointtype: " + selectedJoint.joint.jointType);
                        }
                    break;
                case bool _ when Input.GetKey(KeyCode.DownArrow):
                        if (selectedJoint.joint.jointType == ArticulationJointType.RevoluteJoint) {
                            selectedJoint.MoveCounterClockwise();
                        }
                        else if (selectedJoint.joint.jointType == ArticulationJointType.PrismaticJoint) {
                            selectedJoint.MoveGripperClose();
                        }
                        else {
                            Debug.LogError("Tried to controll unsupported jointtype: " + selectedJoint.joint.jointType);
                        }
                    break;
            }
        }

        private void IncreaseIndex() {
            // keep index safely within the limits of the array
            selectedJointIndex = (++selectedJointIndex + joints.Count) % joints.Count;
        }

        private void DecreaseIndex() {
            if (selectedJointIndex < 0) {
                selectedJointIndex = joints.Count - 1;
            }
            else {
                selectedJointIndex = (--selectedJointIndex + joints.Count) % joints.Count;
            }
        }

        private void UpadateControlType(ControlType type) {
            switch (type) {
                case ControlType.PositionControl:
                    foreach (RobotJoint joint in joints) {
                        joint.SetDriveType(ArticulationDriveType.Target);
                    }
                    return;
                case ControlType.Moveit:
                    foreach (RobotJoint joint in joints) {
                        joint.SetDriveType(ArticulationDriveType.Force); 
                    }
                    selectionObserver.ResetHighlight();
                    selectedJointIndex = -1;
                    return;
                default:
                    Debug.LogAssertion("Unsupported ControlType selected.");
                    return;
            }
        }

        public void SetControlTypeMoveit () {
            controlType = ControlType.Moveit;
        }
    }
}