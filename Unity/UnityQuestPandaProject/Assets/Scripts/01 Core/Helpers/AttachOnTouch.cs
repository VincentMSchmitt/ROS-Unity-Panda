/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using UnityEngine;

namespace Panda.Core {
    /// <summary>
    /// Class used to attach a target object to the robot's hand when touched by both fingers. This ensures that the
    /// target moves correctly with the robot.To achieve this, the target is set to kinematic while transporting,
    /// disabling the physics of the object. Note that this is purely used for the visual representation of the cube.
    /// Currently not used anywere.
    /// </summary>
    public class AttachOnTouch : MonoBehaviour {
        enum PandaFinger { Right, Left, None }
        [Tooltip("The GameObject of the right finger.")] [SerializeField] GameObject pandaRightFinger;
        [Tooltip("The GameObject of the left finger.")] [SerializeField] GameObject pandaLeftFinger;
        [Tooltip("The GameObject of the hand --> this is where the target will be attached to.")] [SerializeField] GameObject pandaHand;
        private bool rightFingerTouching = false;
        private bool leftFingerTouching = false;
        private bool isTransporting = false;
        private bool isReleased = true;
        private GameObject targetObject = null;
        private Rigidbody targetRigidbody = null;
        public static AttachOnTouch GetInstance { get; private set; } // Singleton instance
        
        public static void OnReachDestination() {
            if (GetInstance != null && GetInstance.isTransporting) {
                GetInstance.DetachTargetFromHand();
            }
        }

        public static void NewDestination() {
            GetInstance.isTransporting = false;
            GetInstance.isReleased = true;
        }

        /// <summary>
        /// Ensures only one instance of this class exists. If an instance exists and it is not this instance, the
        /// current game object is destroyed to enforce the singleton property. If no instance exists, this instance is
        /// assigned to the static Instance property.
        /// </summary>
        private void Awake() {
            if (GetInstance != null && GetInstance != this) {
                Destroy(this.gameObject);
            } else {
                GetInstance = this;
            }
        }

        private PandaFinger GetPandaFinger(GameObject obj) {
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
                AttachTargetToHand(GetInstance.gameObject);
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
                isTransporting = true;  // Start transporting
                isReleased = false;     // set the release flag
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
    }
}