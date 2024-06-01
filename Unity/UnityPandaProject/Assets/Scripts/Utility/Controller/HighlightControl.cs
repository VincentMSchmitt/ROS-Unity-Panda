using Unity.Robotics;
using UnityEditor;
using UnityEngine;

namespace Panda.Utility.Controller {
    [RequireComponent(typeof(JointLimitDisplay))]
    public class HighlightControl {

        public Color color;
        private int storedColorIndex;
        private ArticulationBody[] articulationChain;
        private Color[] prevColor; // Stores original colors of the part being highlighted

        public HighlightControl(ArticulationBody[] articulationChain, Color color) {
            this.articulationChain = articulationChain;
            this.color = color;
        }

        /// <summary>
        /// Resets original color of the part being highlighted
        /// </summary>
        /// <param name="index">Index of the part in the Articulation chain</param>
        public void ResetJointColors() {
            Renderer[] previousRendererList = articulationChain[storedColorIndex].transform.GetChild(1).GetComponentsInChildren<Renderer>();
            for (int counter = 0; counter < previousRendererList.Length; ++counter) {
                MaterialExtensions.SetMaterialColor(previousRendererList[counter].material, prevColor[counter]);
            }
        }

        /// <summary>
        /// Stores original color of the part being highlighted
        /// </summary>
        /// <param name="index">Index of the part in the Articulation chain</param>
        public void StoreJointColors(int index) {
            Renderer[] materialLists = articulationChain[index].transform.GetChild(1).GetComponentsInChildren<Renderer>();
            prevColor = new Color[materialLists.Length];
            for (int counter = 0; counter < materialLists.Length; ++counter) {
                prevColor[counter] = MaterialExtensions.GetMaterialColor(materialLists[counter]);
            }
            storedColorIndex = index;
        }

        /// <summary>
        /// Highlights the color of the robot by changing the color of the part to a color set by the user in the inspector window
        /// </summary>
        /// <param name="selectedIndex">Index of the link selected in the Articulation Chain</param>
        public void Highlight(int selectedIndex, MeshFilter meshFilter, Material material) {
            if (selectedIndex >= articulationChain.Length) {
                return;
            }

            ResetJointColors();
            if (selectedIndex != -1) {
               StoreJointColors(selectedIndex);
            }

            // set the color of the selected join meshes to the highlight color
            Renderer[] rendererList = articulationChain[selectedIndex].transform.GetChild(1).GetComponentsInChildren<Renderer>();
            foreach (var mesh in rendererList) {
                MaterialExtensions.SetMaterialColor(mesh.material, color);
            }

            // clear old joint limits and draw new joint limits
            JointLimitDisplay.ClearJointLimits(meshFilter);
            JointLimitDisplay.DrawJointLimits(selectedIndex, articulationChain, meshFilter, material);
        }
    }
}

public static class MaterialExtensions {
    public static Color GetMaterialColor(Renderer renderer) {
        return renderer.material.color;
    }

    public static void SetMaterialColor(Material material, Color color) {
        material.color = color;
    }
}