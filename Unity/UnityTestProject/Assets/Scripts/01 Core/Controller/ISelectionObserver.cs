using UnityEngine;
using System.Collections.Generic;

namespace Panda.Core.Controller {
    public interface ISelectionObserver {
        void SetSelectionColor(Color color);
        void ResetHighlight();
        List<ICombinedInterface> GetJoints();
        ICombinedInterface GetSelection();
        bool HasSelection();
        void ResetSelection();
        public void Next();
        public void Previous();
        void Add(ICombinedInterface robotJoint);
    }
}