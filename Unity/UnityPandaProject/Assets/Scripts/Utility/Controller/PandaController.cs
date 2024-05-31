using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Panda.Utility.Controller {
    public enum RotationDirection { None = 0, Positive = 1, Negative = -1 };
    public enum ControlType { PositionControl, Ros };

    public class Controller : MonoBehaviour {
        [InspectorReadOnly(hideInEditMode: true)] public string selectedJoint;
        public ControlType control = ControlType.PositionControl;
        [SerializeField] float stiffness = 10000f;  // TODO: find optimal parameter
        [SerializeField] float damping = 100f;      // TODO: find optimal parameter
        [Tooltip("degree/s")] public float speed = 20;
        [HideInInspector] public int selectedIndex = -1;

        [Tooltip("Color to highlight the currently selected join")]
        public Color highLightColor = new Color(1.0f, 0, 0, 1.0f);

        private static LineRenderer lineRenderer;
        private ArticulationBody[] articulationChain;
        private List<int> selectableJoints = new();
        private int oldIndex;
        private int selectedJointsIndex = -1;
        private HighlightControl highlightControl;

        void Start() {
            articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            for (int i = 0; i < articulationChain.Length; ++i) {
                articulationChain[i].gameObject.AddComponent<JointControl>();
                if (articulationChain[i].jointType != ArticulationJointType.FixedJoint) {
                    selectableJoints.Add(i);
                }
            }
            highlightControl = new(articulationChain, highLightColor);
            oldIndex = selectedIndex;
            highlightControl.StoreJointColors(selectedIndex);
        }

        /// <summary>
        /// Updates the state of the object based on user input.
        /// </summary>
        /// <remarks>
        /// This method is called every frame. It handles user input to change the selected joint and update its direction.
        /// </remarks>
        void Update() {
            highlightControl.color = highLightColor;
            // select joint with left and right arrow keys
            switch (true) {
                case bool _ when Input.GetKeyDown(KeyCode.RightArrow):
                    selectedIndex = NextIndex();
                    Highlight(selectedIndex);
                    break;
                case bool _ when Input.GetKeyDown(KeyCode.LeftArrow):
                    selectedIndex = PreviousIndex();
                    Highlight(selectedIndex);
                    break;
            }
            UpdateDirection(selectedIndex);
        }

        private int NextIndex() {
            // keep index safely within the limits of the array
            selectedJointsIndex = (++selectedJointsIndex + selectableJoints.Count) % selectableJoints.Count;;
            return selectableJoints[selectedJointsIndex];
        }

        private int PreviousIndex() {
            if (selectedJointsIndex == -1) {
                selectedJointsIndex = selectableJoints.Count - 1;
            } 
            else {
                selectedJointsIndex = (--selectedJointsIndex + selectableJoints.Count) % selectableJoints.Count;;
            }
            return selectableJoints[selectedJointsIndex];
        }

        /// <summary>
        /// Sets the direction of movement of the joint on every update
        /// </summary>
        /// <param name="jointIndex">Index of the link selected in the Articulation Chain</param>
        private void UpdateDirection(int jointIndex) {
            if (jointIndex < 0 || jointIndex >= articulationChain.Length) {
                return;
            }
            float moveDirection = Input.GetAxis("Vertical"); // value is from -1 to 1
            
            // get the JointControl component from every joint:
            JointControl current = articulationChain[jointIndex].GetComponent<JointControl>();
            
            // if the index is updated, set rotation direction of previous jointInted to none, update previous index
            if (oldIndex != jointIndex) {
                JointControl previous = articulationChain[oldIndex].GetComponent<JointControl>();
                previous.direction = RotationDirection.None;
                oldIndex = jointIndex;
            }

            if (current.controltype != control) {
                UpdateControlType(current);
            }

            // set the rotation direction
            switch (moveDirection) {
                case > 0:
                    current.direction = RotationDirection.Positive;
                    break;
                case < 0:
                    current.direction = RotationDirection.Negative;
                    break;
                default:
                    current.direction = RotationDirection.None;
                    break;
            }
        }

        /// <summary>
        /// Update the selected joint in the inspector.
        /// </summary>
        /// <param name="selectedIndex">Index of the joint that should be displayed.</param>
        void Highlight(int selectedIndex) {
            if (selectedIndex < 0 || selectedIndex >= articulationChain.Length) {
                return;
            }
            
            if (lineRenderer == null) {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            highlightControl.Highlight(selectedIndex, lineRenderer);
            // TODO: Add enum for joints and display clean name
            selectedJoint = articulationChain[selectedIndex].name;
        }

        public void UpdateControlType(JointControl joint) {
            joint.controltype = control;
            if (control == ControlType.PositionControl) {
                ArticulationDrive drive = joint.joint.xDrive;
                drive.stiffness = stiffness;
                drive.damping = damping;
                joint.joint.xDrive = drive;
            }
        }

        // public void OnGUI() {
        //     GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
        //     centeredStyle.alignment = TextAnchor.UpperCenter;
        //     GUI.Label(new Rect(Screen.width / 2 - 200, 10, 400, 20), "Press left/right arrow keys to select a robot joint.", centeredStyle);
        //     GUI.Label(new Rect(Screen.width / 2 - 200, 30, 400, 20), "Press up/down arrow keys to move " + selectedJoint + ".", centeredStyle);
        // }
    }
}