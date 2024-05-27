using UnityEngine;

public class TargetTrigger : MonoBehaviour {

    [SerializeField] private bool isEnabled = true;
    private AttachOnTouch attachOnTouch;

    private void Start() {
        // Find the AttachOnTouch component in the scene (or assign it manually)
        attachOnTouch = FindObjectOfType<AttachOnTouch>();
    }

    private void OnTriggerEnter(Collider other) {
        if (isEnabled) {
            attachOnTouch.OnFingerTriggerEnter(gameObject, other);
        }
    }
}