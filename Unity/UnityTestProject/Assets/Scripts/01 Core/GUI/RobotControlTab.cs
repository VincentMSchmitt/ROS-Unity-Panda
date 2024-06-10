using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Panda.Core.Controller;
using Panda.PickAndPlace;
using Panda.Follower;

namespace Panda.Core.Gui {
    public class RobotControlTab : MonoBehaviour, IHudTab {
        string IHudTab.Label => "Control";
        private RobotController robotController;
        private bool isFollowButtonPressed = false;
        private FollowPlanner followPlanner;

        public void Start() {
            HudPanel.RegisterTab(this, 0);
            robotController = RobotController.GetInstance;
            followPlanner = FindObjectOfType<FollowPlanner>();
        }

        void IHudTab.OnGUI(HudPanel hud) {
            GUILayout.BeginHorizontal();

            // send plan request
            if (GUILayout.Button("Plan")) {
                isFollowButtonPressed = false;
                robotController.SetControlTypeMoveit();
                TrajectoryPlanner trajectoryPlanner = FindObjectOfType<TrajectoryPlanner>();
                if (trajectoryPlanner == null) {
                    throw new System.NullReferenceException("There is no Trajectory Planner in this scene.");
                }
                trajectoryPlanner?.SendPlanRequest();
            }

            // toggle manual control mode
            if (GUILayout.Button("Control")) {
                isFollowButtonPressed = false;
                robotController.SetControlTypePositionControl();
            }

            // toggle follow mode
            GUIStyle followButtonStyle = new GUIStyle(GUI.skin.button);
            if (isFollowButtonPressed) {
                followButtonStyle.normal.textColor = Color.red; // Mark the button when pressed
            }

            if (GUILayout.Button("Follow", followButtonStyle)) {
                isFollowButtonPressed = !isFollowButtonPressed; // Toggle the button state
                robotController.SetControlTypeMoveit();
                FollowPlanner.SetToggle(isFollowButtonPressed);
                if (isFollowButtonPressed) {
                    StartCoroutine(followPlanner.FollowRoutine());
                }
            }

            GUILayout.EndHorizontal();
        }

        public void OnSelected() { }
        public void OnDeselected() { }
    }
}