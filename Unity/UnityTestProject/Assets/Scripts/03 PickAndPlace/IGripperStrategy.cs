using System.Collections.Generic;
using Panda.Core.Controller;
using UnityEngine;

namespace Panda.PickAndPlace {
    public interface IGripperStrategy {
        // TODO: remove
        void Execute(ArticulationBody leftGripper, ArticulationBody rightGripper);
        void Execute(List<IMoveCommand> gripperJoints);
    }
}
