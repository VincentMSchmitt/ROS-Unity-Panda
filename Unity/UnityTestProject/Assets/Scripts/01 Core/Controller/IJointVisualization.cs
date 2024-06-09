using UnityEngine;

namespace Panda.Core.Controller {
    public interface IJointVisualization {
        public void Highlight(Color color);
        public void ResetHighlight(Color[] colors);
        public Color[] StoreJointColors();
    }

    public interface ICombinedInterface : IJointVisualization, IMoveCommand {
    }
}
