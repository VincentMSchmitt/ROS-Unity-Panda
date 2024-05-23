using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class JointTarget {
    public string jointName;
    public float targetPosition;
}

public class Startup : MonoBehaviour {
    // Define target positions for each joint
    [SerializeField] public List<JointTarget> jointTargets = new List<JointTarget> {
        new JointTarget { jointName = "panda_link1", targetPosition = 0f },
        new JointTarget { jointName = "panda_link2", targetPosition = 0f },
        new JointTarget { jointName = "panda_link3", targetPosition = 0f },
        new JointTarget { jointName = "panda_link4", targetPosition = 0f },
        new JointTarget { jointName = "panda_link5", targetPosition = -90f },
        new JointTarget { jointName = "panda_link6", targetPosition = 0f },
        new JointTarget { jointName = "panda_link7", targetPosition = 90f }
    };

    public float tolerance = 0.01f; // Tolerance for joint position comparison
    public float checkInterval = 0.1f; // Interval in seconds between position checks

    void Start() {
        ArticulationBody[] articulationChain = this.GetComponentsInChildren<ArticulationBody>();
        StartCoroutine(MoveJointsToTarget(articulationChain, jointTargets));
    }

    IEnumerator MoveJointsToTarget(ArticulationBody[] articulationChain, List<JointTarget> jointTargets) {
        // Iterate only from the second element up to the length of the targetPositions array
        for (int i = 1; i <= jointTargets.Count; ++i) {
            var currentDrive = articulationChain[i].xDrive;
            currentDrive.target = jointTargets[i-1].targetPosition; // Use i-1 to correctly index targetPositions
            articulationChain[i].xDrive = currentDrive;
        }

        yield return StartCoroutine(WaitUntilJointsReachTarget(articulationChain, jointTargets));
    }

    IEnumerator WaitUntilJointsReachTarget(ArticulationBody[] articulationChain, List<JointTarget> jointTargets) {
        bool allJointsAtTarget = false;

        while (!allJointsAtTarget) {
            allJointsAtTarget = true;
            // Iterate only from the second element up to the length of the targetPositions array
            for (int i = 1; i <= jointTargets.Count; ++i) {
                if (articulationChain[i].jointPosition.dofCount > 0) {
                    float currentPosition = articulationChain[i].jointPosition[0];
                    if (Mathf.Abs(currentPosition - jointTargets[i-1].targetPosition) > tolerance) { // Use i-1 to correctly index targetPositions
                        allJointsAtTarget = false;
                        break;
                    }
                }
            }

            if (!allJointsAtTarget) {
                yield return new WaitForSeconds(checkInterval);
            }
        }

        Debug.Log("All specified joints have reached the target positions.");
    }
}