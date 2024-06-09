using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

using Panda.Core.Controller;
using Panda.PickAndPlace;

namespace Panda.Core.Hud {
    public class RobotControlTab : MonoBehaviour, IHudTab {
        string IHudTab.Label => "Control";
        private RobotController robotController;

        public void Start() {
            HudPanel.RegisterTab(this, 0);
            robotController = RobotController.GetInstance;
        }

        void IHudTab.OnGUI(HudPanel hud) {
            // Adding two new buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Plan")) {
                robotController.SetControlTypeMoveit();
                TrajectoryPlanner trajectoryPlanner = FindObjectOfType<TrajectoryPlanner>();
                if (trajectoryPlanner == null) {
                    throw new System.NullReferenceException("There is no Trajectory Planner in this scene.");
                }
                trajectoryPlanner?.SendPlanRequest();
            }
            if (GUILayout.Button("Control")) {
                robotController.SetControlTypePositionControl();
                
            }
            if (GUILayout.Button("Settings")) {
            }
            GUILayout.EndHorizontal();
        }

        public void OnSelected() { }
        public void OnDeselected() { }
    }
}