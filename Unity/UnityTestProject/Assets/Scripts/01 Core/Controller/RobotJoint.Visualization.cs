using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotJoint: IJointVisualization {
        public void Highlight(Color color) {
            Renderer[] rendererList = joint.transform.GetChild(1).GetComponentsInChildren<Renderer>();
            foreach (var mesh in rendererList) {
                MaterialExtensions.SetMaterialColor(mesh.material, color);
            }
        }

        public void ResetHighlight(Color[] colors) {
            if (colors != null) {
                Renderer[] previousRendererList = joint.transform.GetChild(1).GetComponentsInChildren<Renderer>();
                for (int counter = 0; counter < previousRendererList.Length; ++counter) {
                    MaterialExtensions.SetMaterialColor(previousRendererList[counter].material, colors[counter]);
                }
            }
        }

        public Color[] StoreJointColors() {
            Renderer[] materialLists = joint.transform.GetChild(1).GetComponentsInChildren<Renderer>();
            Color[] previousColors = new Color[materialLists.Length];
            for (int counter = 0; counter < materialLists.Length; ++counter) {
                previousColors[counter] = MaterialExtensions.GetMaterialColor(materialLists[counter]);
            }
            return previousColors;
        }
    }
}