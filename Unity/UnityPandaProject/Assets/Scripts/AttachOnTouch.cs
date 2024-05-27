using UnityEngine;

public class AttachOnTouch : MonoBehaviour {
    public GameObject pandaRightFinger;
    public GameObject pandaLeftFinger;
    public GameObject pandaHand;

    private bool rightFingerTouching = false;
    private bool leftFingerTouching = false;
    private bool isTransporting = false;
    private bool isReleased = true;
    private GameObject targetObject = null;
    private Rigidbody targetRigidbody = null;

    // Singleton instance
    public static AttachOnTouch Instance { get; private set; }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
        } else {
            Instance = this;
        }
    }

    public void OnFingerTriggerEnter(GameObject target, Collider other) {
        if (other.gameObject == pandaRightFinger) {
            rightFingerTouching = true;
        } else if (other.gameObject == pandaLeftFinger) {
            leftFingerTouching = true;
        }

        if (rightFingerTouching && leftFingerTouching) {
            AttachTargetToHand(target);
        }
    }

    public void OnFingerTriggerExit(GameObject target, Collider other) {
        if (!isTransporting) {
            if (other.gameObject == pandaRightFinger) {
                rightFingerTouching = false;
            } else if (other.gameObject == pandaLeftFinger) {
                leftFingerTouching = false;
            }

            if (!rightFingerTouching && !leftFingerTouching) {
                DetachTargetFromHand();
            }
        }
    }

    private void AttachTargetToHand(GameObject target) {
        if (targetObject == null && isReleased) {
            targetObject = target;
            targetRigidbody = target.GetComponent<Rigidbody>();

            if (targetRigidbody != null) {
                targetRigidbody.isKinematic = true; // Disable physics
            }

            target.transform.SetParent(pandaHand.transform);
            //Debug.Log("Target attached to panda hand.");
            isTransporting = true; // Start transporting
            isReleased = false; // set the release flag
        }
    }

    private void DetachTargetFromHand() {
        if (targetObject != null) {
            if (targetRigidbody != null) {
                targetRigidbody.isKinematic = false; // Enable physics
            }

            targetObject.transform.SetParent(null);
            //Debug.Log("Target detached from panda hand.");
            targetObject = null;
            targetRigidbody = null;
            isTransporting = false; // Stop transporting
        }
    }

    // Static method to be called when the robot reaches its destination
    public static void OnReachDestination() {
        if (Instance != null && Instance.isTransporting) {
            Instance.DetachTargetFromHand();
        }
    }

    // Static method to be called when the robot reaches its destination
    public static void newDestination() {
        Instance.isTransporting = false;
        Instance.isReleased = true;
    }
}