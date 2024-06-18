using UnityEngine;

namespace Panda.Core.Controller {
    public partial class RobotGripper : IJointVisualization {
        public void Highlight(Color color) {
            finger1.Highlight(color);
            finger2.Highlight(color);
        }

        public void ResetHighlight(Color[] colors) {
            finger1.ResetHighlight(colors);
            finger2.ResetHighlight(colors);
        }

        public Color[] StoreJointColors() {
            // assume, that left and right finger have same color
            return finger1.StoreJointColors();
        }
    }
}