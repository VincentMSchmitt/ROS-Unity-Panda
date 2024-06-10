using UnityEngine;
using System.Collections.Generic;

namespace Panda.Core.Controller {
    public interface ISelectionObserver {
        List<ICombinedInterface> GetJoints();
        ICombinedInterface GetSelection();
        void Add(ICombinedInterface robotJoint);
        void SetSelectionColor(Color color);
        void ResetHighlight();
        bool HasSelection();
        void ResetSelection();
        public void Next();
        public void Previous();
    }
}