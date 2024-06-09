using System.Collections.Generic;
using UnityEngine;

namespace Panda.Core.Controller {
    public class SelectionObserver : ISelectionObserver {
        private int selectedJointIndex = -1;
        private ICombinedInterface selectedJoint {
            get { 
                if (selectedJointIndex != -1) {
                    return joints[selectedJointIndex];
                } 
                return null;
            }    
        }

        private Color selectionColor;
        private Color[] previousColor;

        public List<ICombinedInterface> joints { get; private set; }

        public SelectionObserver(Color selectionColor) {
            this.selectionColor = selectionColor;
            joints = new();
        }

         public void SetSelectionColor(Color color) {
            selectionColor = color;
        }

        public void ResetHighlight() {
            selectedJoint?.ResetHighlight(previousColor);
        }

        public void Next() {
            // keep index safely within the limits of the array
            selectedJoint?.ResetHighlight(previousColor);
            selectedJointIndex = (++selectedJointIndex + joints.Count) % joints.Count;
            previousColor = selectedJoint.StoreJointColors();
            selectedJoint.Highlight(selectionColor);
        }

        public void Previous() {
            selectedJoint?.ResetHighlight(previousColor);
            if (selectedJointIndex < 0) {
                selectedJointIndex = joints.Count - 1;
            }
            else {
                selectedJointIndex = (--selectedJointIndex + joints.Count) % joints.Count;
            }
            previousColor = selectedJoint.StoreJointColors();
            selectedJoint.Highlight(selectionColor);
        }

        public List<ICombinedInterface> GetJoints() {
            return joints;
        }

        public ICombinedInterface GetSelection() {
            return selectedJoint;
        }

        public bool HasSelection() {
            return selectedJointIndex != -1;
        }

        public void ResetSelection()
        {
            selectedJointIndex = -1;
        }

        public void Add(ICombinedInterface robotJoint)
        {
            joints.Add(robotJoint);
        }
    }
}