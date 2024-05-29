using UnityEngine;

namespace Panda.Utility {
    /// <summary>
    /// Class used to attach a target object to the robot's hand when touched by both fingers. This ensures that the
    /// target moves correctly with the robot.To achieve this, the target is set to kinematic while transporting,
    /// disabling the physics of the object. Note that this is a workaround to temporarily fix abug with the articulated
    /// bodies.
    /// </summary>
    public class AttachOnTouch : MonoBehaviour {
        [SerializeField] GameObject pandaRightFinger;
        [SerializeField] GameObject pandaLeftFinger;
        [SerializeField] GameObject pandaHand;

        private bool rightFingerTouching = false;
        private bool leftFingerTouching = false;
        private bool isTransporting = false;
        private bool isReleased = true;
        private GameObject targetObject = null;
        private Rigidbody targetRigidbody = null;

        // Singleton instance
        public static AttachOnTouch Instance { get; private set; }

        /// <summary>
        /// Ensures only one instance of this class exists. If an instance exists and it is not this instance, the
        /// current game object is destroyed to enforce the singleton property. If no instance exists, this instance is
        /// assigned to the static Instance property.
        /// </summary>
        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
            } else {
                Instance = this;
            }
        }

        /// <summary>
        /// Called when a finger collider enters the trigger zone. The call happens in "TargetScript". 
        /// </summary>
        /// <param name="other">The other collider involved in this collision.</param>
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

        /// <summary>
        /// Called when a finger collider exits the trigger zone. Currently not called anywere.
        /// </summary>
        /// <param name="other">The other collider involved in this collision.</param>
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
                isTransporting = true; // Start transporting
                isReleased = false; // set the release flag
                //Debug.Log("Target attached to panda hand.");
            }
        }

        private void DetachTargetFromHand() {
            if (targetObject != null) {
                if (targetRigidbody != null) {
                    targetRigidbody.isKinematic = false; // Enable physics
                }

                targetObject.transform.SetParent(null);
                targetObject = null;
                targetRigidbody = null;
                isTransporting = false; // Stop transporting
                //Debug.Log("Target detached from panda hand.");
            }
        }

        public static void OnReachDestination() {
            if (Instance != null && Instance.isTransporting) {
                Instance.DetachTargetFromHand();
            }
        }

        /// <summary>
        /// Resets the transporting and release flags when a new destination is set.
        /// </summary>
        public static void newDestination() {
            Instance.isTransporting = false;
            Instance.isReleased = true;
        }
    }
}