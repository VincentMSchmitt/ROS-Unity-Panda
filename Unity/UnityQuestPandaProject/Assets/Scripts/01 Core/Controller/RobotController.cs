/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Panda.Core.Controller {
    public enum ControlType { PositionControl, Moveit };
    /// <summary>
    /// The controller of the panda robot. Handels the joints (articulation bodys), selection, meshes, xDrives and
    /// everything else related to the robot.
    /// </summary>
    public class RobotController : MonoBehaviour {
        [Tooltip("The current type of control (PositionControl or Moveit).")] public ControlType controlType = ControlType.PositionControl;
        [Tooltip("The speed for moving the joints.")] public float speed = 50f;
        [Tooltip("The color for a selected joint.")] public Color selectionColor = Color.red;
        [Tooltip("The material for the limit visulization (experimental).")] public Material limitMaterial;
        [HideInInspector] public MeshFilter limitMeshFilter;
        [HideInInspector] public ISelectionObserver selectionObserver { get; private set; }
        [HideInInspector] public ArticulationBody[] articulationChain {get; private set; }
        [HideInInspector] public List<ArticulationBody> articulationList {
            get {
                List<ArticulationBody> articulationListTemp = articulationChain?.OfType<ArticulationBody>().ToList();
                return articulationListTemp.Where( x => ( (x.jointType == ArticulationJointType.RevoluteJoint)
                                                       || (x.jointType == ArticulationJointType.PrismaticJoint) )
                                                 ).ToList<ArticulationBody>();
            }
        }
        public static RobotController GetInstance { // Singelton
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
        private static RobotController instance;

        RobotController() {
            selectionObserver = new SelectionObserver(selectionColor);
        }

        /// <summary>
        /// Called in the first frame of the game. Initializes the controller.
        /// </summary>
        private void Start() {
            articulationChain = GetComponentsInChildren<ArticulationBody>();
            // add the revolute joints --> assume gripper are the last 2 elements
            int i = 0;
            for (;i < articulationChain.Length - 2; ++i) {
                if (articulationChain[i].jointType != ArticulationJointType.FixedJoint) {
                    RobotJoint joint = new(articulationChain[i]);
                    selectionObserver.Add(joint);
                }
            }
            // add gripper
            RobotJoint finger1 = new(articulationChain[i]);
            RobotJoint finger2 = new(articulationChain[i+1]);
            RobotGripper gripper = new(finger1, finger2);
            selectionObserver.Add(gripper);

            // init mesh filter for joint limit functionality
            if (limitMeshFilter == null) {
                GameObject highlightObject = new GameObject("HighlightMesh");
                highlightObject.transform.SetParent(gameObject.transform);
                limitMeshFilter = highlightObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = highlightObject.AddComponent<MeshRenderer>();
                if (limitMaterial == null) {
                    Material newMaterial = new Material(Shader.Find("Standard-DoubleSided"));
                    meshRenderer.material = newMaterial;
                } else {
                    meshRenderer.material = limitMaterial;
                }
            }
        }

        /// <summary>
        /// Called every frame of the game. Gets inputs for the selection.
        /// </summary>
        private void Update() {
            // check current controlType
            UpadateControlType(controlType);
            if (controlType == ControlType.PositionControl) {
                // update color dynamicly while in play mode
                selectionObserver.SetSelectionColor(selectionColor);
                // Select the joints
                switch (true) {
                    case bool _ when Input.GetKeyDown(KeyCode.RightArrow):
                        selectionObserver.Next();
                        break;
                    case bool _ when Input.GetKeyDown(KeyCode.LeftArrow):
                        selectionObserver.Previous();
                        break;
                }
            }
        }

        /// <summary>
        /// Called every physics update of the game. Recommended for using with articulation body manipulation. Gets
        //  inputs movement of the selected joint(s).
        /// </summary>
        private void FixedUpdate() {
            // at start, dont do antyhing
            if (controlType != ControlType.PositionControl && selectionObserver.HasSelection()) {
                return;
            }
            // Move the robot
            var selectedJoint = selectionObserver.GetSelection();
            if (selectedJoint == null) {
                return;
            }
            switch (true) {
                case bool _ when Input.GetKey(KeyCode.UpArrow):
                        if (selectedJoint.JointType() == ArticulationJointType.RevoluteJoint) {
                            selectedJoint.Move(MoveDirection.Clockwise);
                        }
                        // assume the only joints in this robot that are prismatic is the gripper
                        else if (selectedJoint.JointType() == ArticulationJointType.PrismaticJoint) {
                            selectedJoint.Move(MoveDirection.Open);
                        }
                        else {
                            Debug.LogError("Tried to controll unsupported jointtype: " + selectedJoint.JointType());
                        }
                    break;
                case bool _ when Input.GetKey(KeyCode.DownArrow):
                        if (selectedJoint.JointType() == ArticulationJointType.RevoluteJoint) {
                            selectedJoint.Move(MoveDirection.CounterClockwise);
                        }
                        // assume the only joints in this robot that are prismatic is the gripper
                        else if (selectedJoint.JointType() == ArticulationJointType.PrismaticJoint) {
                            selectedJoint.Move(MoveDirection.Close);
                        }
                        else {
                            Debug.LogError("Tried to controll unsupported jointtype: " + selectedJoint.JointType());
                        }
                    break;
            }
        }

        /// <summary>
        /// Gets all the revolute joint targets (xDrive.target).
        /// </summary>
        /// <returns>The joint targets of all revolute joints in the robot as list of floats.</returns>
        public List<float> GetRevoluteJointTargets() {
            List<float> jointStates = new();
            foreach (var joint in selectionObserver.GetJoints()) {
                if (joint.JointType() == ArticulationJointType.RevoluteJoint) {
                    jointStates.Add(joint.GetTarget());
                }
            }
            return jointStates;
        }

        /// <summary>
        /// Gets all the revolute joints.
        /// </summary>
        /// <returns>The joints of all revolute joints in the robot as list of IMoveCommand.</returns>
        public List<IMoveCommand> GetRevoluteJoints() {
            List<IMoveCommand> joints = new();
            foreach (var joint in selectionObserver.GetJoints()) {
                if (joint.JointType() == ArticulationJointType.RevoluteJoint) {
                    joints.Add(joint);
                }
            }
            return joints;
        }

        /// <summary>
        /// Gets all the revolute joints.
        /// </summary>
        /// <returns>The joints of all revolute joints in the robot as list of IMoveCommand.</returns>
        public List<IMoveCommand> GetHandJoints() {
            List<IMoveCommand> joints = new();
            foreach (var joint in selectionObserver.GetJoints()) {
                if (joint.JointType() == ArticulationJointType.PrismaticJoint) {
                    joints.Add(joint);
                }
            }
            return joints;
        }

        /// <summary>
        /// Gets all the revolute joints.
        /// </summary>
        /// <returns>The joints of the gripper of the robot as IMoveCommand.</returns>
        public IMoveCommand GetGripper() {
            foreach (var joint in selectionObserver.GetJoints()) {
                if (joint.GetType() == typeof(RobotGripper)) {
                    return joint;
                }
            }
            return null;
        }

        public void SetControlTypeMoveit() {
            GetInstance.controlType = ControlType.Moveit;
        }

        public void SetControlTypePositionControl() {
            GetInstance.controlType = ControlType.PositionControl;
        }

        /// <summary>
        /// Gets all the selectable joints and sets their drive-type and force acording to the selected control type
        /// (PositionControl or Moveit)
        /// </summary>
        /// <param name="type">The control type that the force and drive-type should be set for.</param>
        private void UpadateControlType(ControlType type) {
            switch (type) {
                case ControlType.PositionControl:
                    foreach (IMoveCommand joint in selectionObserver.GetJoints()) {
                        joint.SetDriveType(ArticulationDriveType.Target);
                        joint.SetToMaxForce();
                    }
                    return;
                case ControlType.Moveit:
                    foreach (IMoveCommand joint in selectionObserver.GetJoints()) {
                        joint.SetDriveType(ArticulationDriveType.Force);
                        //joint.ResetForce();
                    }
                    selectionObserver.ResetHighlight();
                    selectionObserver.ResetSelection();
                    return;
                default:
                    Debug.LogAssertion("Unsupported ControlType selected.");
                    return;
            }
        }
    }
}