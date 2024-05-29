using UnityEngine;

namespace Panda.Utility {
    [RequireComponent(typeof(AttachOnTouch))]

    /// <summary>
    /// Handles triggering events when the target is touched by the robot's fingers. This script must be attached to
    /// the target object.
    /// </summary>
    public class TargetTrigger : MonoBehaviour {
        // Determines whether the trigger functionality is enabled
        [Tooltip("Determines whether the trigger functionality is enabled.")]
        [SerializeField] private bool isEnabled = true;
        private AttachOnTouch attachOnTouch;

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first
        /// time. Initializes the script by finding the AttachOnTouch component in the scene.
        /// </summary>
        private void Start() {
            // Find the AttachOnTouch component in the scene (or assign it manually)
            attachOnTouch = FindObjectOfType<AttachOnTouch>();

            // Check if the AttachOnTouch component was found
            if (attachOnTouch == null) {
                Debug.LogAssertion("AttachOnTouch component not found in the scene.");
            }
        }

        /// <summary>
        /// Called when another collider enters the trigger collider attached to this object. This will call the
        /// function to attach the target to the robot.
        /// </summary>
        /// <param name="other">The other collider involved in this collision.</param>
        private void OnTriggerEnter(Collider other) {
            // Only process the trigger if enabled and attachOnTouch is found
            if (isEnabled) {
                attachOnTouch.OnFingerTriggerEnter(gameObject, other);
            }
        }
    }
}