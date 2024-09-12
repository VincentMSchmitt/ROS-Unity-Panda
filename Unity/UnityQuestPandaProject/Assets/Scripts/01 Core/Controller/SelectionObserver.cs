/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using System.Collections.Generic;
using UnityEngine;

namespace Panda.Core.Controller {
    public class SelectionObserver : ISelectionObserver {
        public List<ICombinedInterface> joints { get; private set; }
        private int selectedJointIndex = -1;
        private Color selectionColor;
        private Color[] previousColor;
        private ICombinedInterface selectedJoint {
            get { 
                if (selectedJointIndex != -1) {
                    return joints[selectedJointIndex];
                } 
                return null;
            }    
        }

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

        public void ResetSelection() {
            selectedJointIndex = -1;
        }

        public void Add(ICombinedInterface robotJoint) {
            joints.Add(robotJoint);
        }
    }
}