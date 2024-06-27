/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

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