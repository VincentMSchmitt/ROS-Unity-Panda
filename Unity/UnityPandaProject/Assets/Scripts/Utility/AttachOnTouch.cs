using UnityEngine;

namespace Panda.Utility {
    /// <summary>
    /// Class used to attach a target object to the robot's hand when touched by both fingers. This ensures that the
    /// target moves correctly with the robot.To achieve this, the target is set to kinematic while transporting,
    /// disabling the physics of the object. Note that this is a workaround to temporarily fix abug with the articulated
    /// bodies.
    /// </summary>
    public class AttachOnTouch : MonoBehaviour {

        enum PandaFinger {
            Right,
            Left,
            None
        }

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
        /// Called when another collider enters the trigger collider attached to this object. This attach the target to
        /// the robot.
        /// </summary>
        /// <param name="other">The other collider involved in this collision.</param>
        private void OnTriggerEnter(Collider other) {
            switch (GetPandaFinger(other.gameObject)) {
                case PandaFinger.Right:
                    rightFingerTouching = true;
                    break;
                case PandaFinger.Left:
                    leftFingerTouching = true;
                    break;
                case PandaFinger.None:
                    break;
            }
            // If both fingers are touching
            if (rightFingerTouching && leftFingerTouching) {
                AttachTargetToHand(Instance.gameObject);
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
                // Reset flags
                rightFingerTouching = false;
                leftFingerTouching = false;
                isTransporting = false; // Stop transporting

                targetObject.transform.SetParent(null);
                targetObject = null;
                targetRigidbody = null;
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


        PandaFinger GetPandaFinger(GameObject obj) {
        if (obj == pandaRightFinger) {
            return PandaFinger.Right;
        }
        else if (obj == pandaLeftFinger) {
            return PandaFinger.Left;
        }
        else {
            return PandaFinger.None;
        }
    }
    }
}