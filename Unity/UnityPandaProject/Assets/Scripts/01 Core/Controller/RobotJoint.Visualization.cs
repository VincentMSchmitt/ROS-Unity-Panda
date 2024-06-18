using System;
using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotJoint: IJointVisualization {
        public void Highlight(Color color) {
            Renderer[] rendererList = GetVisualRenderers();
            foreach (var mesh in rendererList) {
                MaterialExtensions.SetMaterialColor(mesh.material, color);

                // draw joint limits
                RobotController robotController = RobotController.GetInstance;
                JointLimitDrawer.ClearJointLimits(robotController.limitMeshFilter);
                JointLimitDrawer.DrawJointLimits(joint, robotController.limitMeshFilter, robotController.limitMaterial);
            }
        }

        public void ResetHighlight(Color[] colors) {
            if (colors != null) {
                Renderer[] previousRendererList = GetVisualRenderers();
                for (int counter = 0; counter < previousRendererList.Length; ++counter) {
                    MaterialExtensions.SetMaterialColor(previousRendererList[counter].material, colors[counter]);
                }
            }
        }

        public Color[] StoreJointColors() {
            Renderer[] materialLists = GetVisualRenderers();
            Color[] previousColors = new Color[materialLists.Length];
            for (int counter = 0; counter < materialLists.Length; ++counter) {
                previousColors[counter] = MaterialExtensions.GetMaterialColor(materialLists[counter]);
            }
            return previousColors;
        }

        private Renderer[] GetVisualRenderers() {
            try {
                return joint.transform.Find("Visuals")?.GetComponentsInChildren<Renderer>();
            }
            catch (Exception ex) {
                Debug.LogAssertion("An error occurred while trying to get renderers of an 'Visuals' GameObject:\n" + ex);
                return null;
            }
        }
    }
}