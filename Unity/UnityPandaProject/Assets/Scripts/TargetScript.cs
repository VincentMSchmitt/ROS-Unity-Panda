using UnityEngine;

public class TargetTrigger : MonoBehaviour {
    private AttachOnTouch attachOnTouch;

    private void Start() {
        // Find the AttachOnTouch component in the scene (or you can assign it manually)
        attachOnTouch = FindObjectOfType<AttachOnTouch>();
    }

    private void OnTriggerEnter(Collider other) {
        attachOnTouch.OnFingerTriggerEnter(gameObject, other);
    }

    private void OnTriggerExit(Collider other) {
        attachOnTouch.OnFingerTriggerExit(gameObject, other);
    }
}