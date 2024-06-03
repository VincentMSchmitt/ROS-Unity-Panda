using UnityEngine;

namespace Panda.PickAndPlace {
    public interface IGripperStrategy {
        void Execute(ArticulationBody leftGripper, ArticulationBody rightGripper);
    }
}
