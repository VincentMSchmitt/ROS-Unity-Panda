using System.Collections.Generic;
using UnityEngine;

namespace Panda.Core.Controller {
    public enum ControlType { PositionControl, Moveit };
    public enum MoveDirection { Clockwise = 1, CounterClockwise = -1, Open = 1, Close = -1, Up = 1, Down = -1 };
    public class RobotController : MonoBehaviour {
        public ControlType controlType = ControlType.PositionControl;
        public float speed = 50f;
        public Color selectionColor = Color.red;
        public List<IMoveCommand> joints { get; private set; }
        private ArticulationBody[] articulationChain;
        private int selectedJointIndex = -1;
        private ISelectionObserver selectionObserver;
        private IMoveCommand selectedJoint {
            get => joints[selectedJointIndex];
        }
        private static RobotController instance;
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

        public List<float> GetRevoluteJointTargets() {
            List<float> jointStates = new();
            foreach (var joint in joints) {
                if (joint.JointType() == ArticulationJointType.RevoluteJoint) {
                    jointStates.Add(joint.GetTarget());
                }
            }
            return jointStates;
        }

        public void SetControlTypeMoveit() {
            GetInstance.controlType = ControlType.Moveit;
        }

        public void SetControlTypePositionControl() {
            GetInstance.controlType = ControlType.PositionControl;
        }

        // TODO: update this to work with interface
        public ArticulationBody[] GetCurrentState() {
            return articulationChain;
        }

        // Initialization and configuration
        private void Start() {
            joints = new();
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            
            // add the revolute joints
            // assume gripper are the last 2 elements
            int i = 0;
            for (;i < articulationChain.Length - 2; ++i) {
                if (articulationChain[i].jointType != ArticulationJointType.FixedJoint) {
                    RobotJoint joint = new(articulationChain[i]);
                    joints.Add(joint);
                }
            }

            // add gripper
            RobotJoint finger1 = new(articulationChain[i]);
            RobotJoint finger2 = new(articulationChain[i+1]);
            RobotGripper gripper = new(finger1, finger2);
            joints.Add(gripper);
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
                    IJointVisualization jointVisualization = selectedJoint as IJointVisualization;
                    if (jointVisualization != null) {
                        selectionObserver.OnJointSelected(jointVisualization);
                    }
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
                        if (selectedJoint.JointType() == ArticulationJointType.RevoluteJoint) {
                            IJointCommand moveCommand = selectedJoint as IJointCommand;
                            if (moveCommand != null) {
                                moveCommand.MoveClockwise();
                            }
                        }
                        // assume the only joints in this robot that are prismatic is the gripper
                        else if (selectedJoint.JointType() == ArticulationJointType.PrismaticJoint) {
                            IGripperCommand gripperCommand = selectedJoint as IGripperCommand;
                            if (gripperCommand != null) {
                                gripperCommand.MoveGripperOpen();
                            }
                        }
                        else {
                            Debug.LogError("Tried to controll unsupported jointtype: " + selectedJoint.JointType());
                        }
                    break;
                case bool _ when Input.GetKey(KeyCode.DownArrow):
                        if (selectedJoint.JointType() == ArticulationJointType.RevoluteJoint) {
                            IJointCommand moveCommand = selectedJoint as IJointCommand;
                            if (moveCommand != null) {
                                moveCommand.MoveCounterClockwise();
                            }
                        }
                        // assume the only joints in this robot that are prismatic is the gripper
                        else if (selectedJoint.JointType() == ArticulationJointType.PrismaticJoint) {
                            IGripperCommand gripperCommand = selectedJoint as IGripperCommand;
                            if (gripperCommand != null) {
                                gripperCommand.MoveGripperClose();
                            }
                        }
                        else {
                            Debug.LogError("Tried to controll unsupported jointtype: " + selectedJoint.JointType());
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
                    foreach (IMoveCommand joint in joints) {
                        joint.SetDriveType(ArticulationDriveType.Target);
                        joint.SetToMaxForce();
                    }
                    return;
                case ControlType.Moveit:
                    foreach (IMoveCommand joint in joints) {
                        joint.SetDriveType(ArticulationDriveType.Force);
                        //joint.ResetForce();
                    }
                    selectionObserver.ResetHighlight();
                    selectedJointIndex = -1;
                    return;
                default:
                    Debug.LogAssertion("Unsupported ControlType selected.");
                    return;
            }
        }
    }
}