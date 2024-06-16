using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace Panda.Core.Gui {
    /// <summary>
    /// The tab for changings settings regarding the panda robot. This works on top of the Unity-Robotics-Hub
    /// TCP-Connector interface. Make sure, that the TCP-Connector is installed and configured befor adding this to the
    /// scene.
    /// </summary>
    public class RobotSettingsTab : MonoBehaviour, IHudTab {
        string IHudTab.Label => "Settings";
        private bool toggleBoundingBox = true;
        private bool toggleWorkingArea = true;
        private bool toggleJointLimitDisplay = false;

        public void Start() {
            HudPanel.RegisterTab(this, 1);
        }

        void IHudTab.OnGUI(HudPanel hud) {            
            GUILayout.BeginVertical(); // ---------------------------------------------------------
            // draw bounding boxes
            toggleBoundingBox = GUILayout.Toggle(toggleBoundingBox, "Draw bounding boxes");
            if (toggleBoundingBox) {
                BoundingBoxDrawer.SetToogle(true);
            } else {
                BoundingBoxDrawer.SetToogle(false);
            }

            // draw the working area
            toggleWorkingArea = GUILayout.Toggle(toggleWorkingArea, "Draw robot working area");
            if (toggleWorkingArea) {
                WorkingAreaDrawer.SetToogle(true);
            } else {
                WorkingAreaDrawer.SetToogle(false);
            }

            // draw joint limts
            toggleJointLimitDisplay = GUILayout.Toggle(toggleJointLimitDisplay, "Draw joint limits (experimental)");
            if (toggleJointLimitDisplay) {
                JointLimitDrawer.SetToogle(true);
            } else {
                JointLimitDrawer.SetToogle(false);
            }
            GUILayout.EndVertical(); // -----------------------------------------------------------
        }

        public void OnSelected() { }
        public void OnDeselected() { }
    }
}