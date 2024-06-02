using UnityEngine;

namespace Panda.Core.Controller {
    public interface ISelectionObserver {
        void OnJointSelected(IJointVisualization joint);
        void SetSelectionColor(Color color);
    }
}