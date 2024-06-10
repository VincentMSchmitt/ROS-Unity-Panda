using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Panda.Core {
    [System.Serializable] public class JointTarget {
        public string jointName;
        public float targetPosition;
    }

    /// <summary>
    /// Moves the robot to a given start position.
    /// This script is executed first to ensure initial setup.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Startup : MonoBehaviour {
        [SerializeField] List<JointTarget> jointTargets = new List<JointTarget> {
            new JointTarget { jointName = "panda_link1", targetPosition = 0   },
            new JointTarget { jointName = "panda_link2", targetPosition = 0   },
            new JointTarget { jointName = "panda_link3", targetPosition = 0   },
            new JointTarget { jointName = "panda_link4", targetPosition = -90f},
            new JointTarget { jointName = "panda_link5", targetPosition = 0   },
            new JointTarget { jointName = "panda_link6", targetPosition = 90f },
            new JointTarget { jointName = "panda_link7", targetPosition = 45f }
        };
        private readonly float tolerance = 0.01f;
        private readonly float checkInterval = 0.1f;

        void Start() {
            ArticulationBody[] articulationChain = this.GetComponentsInChildren<ArticulationBody>();
            StartCoroutine(MoveJointsToTarget(articulationChain, jointTargets));
        }

        /// <param name="articulationChain">Array of articulation bodies representing the robot joints.</param>
        /// <param name="jointTargets">List of target positions for the joints.</param>
        IEnumerator MoveJointsToTarget(ArticulationBody[] articulationChain, List<JointTarget> jointTargets) {
            for (int i = 1; i <= jointTargets.Count; ++i) {
                var currentDrive = articulationChain[i].xDrive;
                currentDrive.target = jointTargets[i - 1].targetPosition;
                articulationChain[i].xDrive = currentDrive;
            }
            yield return StartCoroutine(WaitUntilJointsReachTarget(articulationChain, jointTargets));
        }

        /// <param name="articulationChain">Array of articulation bodies representing the robot joints.</param>
        /// <param name="jointTargets">List of target positions for the joints.</param>
        IEnumerator WaitUntilJointsReachTarget(ArticulationBody[] articulationChain, List<JointTarget> jointTargets) {
            bool allJointsAtTarget = false;
            while (!allJointsAtTarget) {
                allJointsAtTarget = true;
                for (int i = 1; i <= jointTargets.Count; ++i) {
                    // Check if the current joint position is within the tolerance of the target position
                    if (articulationChain[i].jointPosition.dofCount > 0) {
                        float currentPosition = articulationChain[i].jointPosition[0];
                        if (Mathf.Abs(currentPosition - jointTargets[i - 1].targetPosition) > tolerance) {
                            allJointsAtTarget = false;
                            break;
                        }
                    }
                }
                if (!allJointsAtTarget) {
                    yield return new WaitForSeconds(checkInterval);
                }
            }
            // Debug.Log("All specified joints have reached the target positions.");
            EventManager.Instance.TriggerStartupComplete();
        }
    }
}