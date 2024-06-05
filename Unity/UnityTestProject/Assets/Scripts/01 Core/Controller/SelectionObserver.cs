using UnityEngine;

namespace Panda.Core.Controller {
    public class SelectionObserver : ISelectionObserver {
        private IJointVisualization selectedJoint;
        private Color selectionColor;
        private Color[] previousColor;

        public SelectionObserver(Color selectionColor) {
            this.selectionColor = selectionColor;
        }

        public void OnJointSelected(IJointVisualization joint) {
            selectedJoint?.ResetHighlight(previousColor);
            previousColor = joint.StoreJointColors();
            joint.Highlight(selectionColor);
            selectedJoint = joint;
        }

        public void SetSelectionColor(Color color) {
            selectionColor = color;
        }

        public void ResetHighlight() {
            selectedJoint?.ResetHighlight(previousColor);
        }
    }
}