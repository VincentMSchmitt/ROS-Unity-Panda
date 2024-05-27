using UnityEngine;

public class AttachOnTouch : MonoBehaviour {
    public GameObject pandaRightFinger;
    public GameObject pandaLeftFinger;
    public GameObject pandaHand;

    private bool rightFingerTouching = false;
    private bool leftFingerTouching = false;
    private GameObject targetObject = null;

    private void Start() {
        if (pandaRightFinger == null) {
            pandaRightFinger = transform.Find("panda_rightfinger").gameObject;
        }

        if (pandaLeftFinger == null) {
            pandaLeftFinger = transform.Find("panda_leftfinger").gameObject;
        }

        if (pandaHand == null) {
            pandaHand = transform.Find("panda_hand").gameObject;
        }
    }

    public void OnFingerTriggerEnter(GameObject target, Collider other) {
        //Debug.Log("OnTriggerEnter called with: " + other.gameObject.name);
        if (other.gameObject == pandaRightFinger) {
            rightFingerTouching = true;
        }
        else if (other.gameObject == pandaLeftFinger) {
            leftFingerTouching = true;
        }

        // attach to panda_hand
        if (rightFingerTouching && leftFingerTouching) {
            AttachTargetToHand(target);
        }
    }

    public void OnFingerTriggerExit(GameObject target, Collider other) {
        //Debug.Log("OnTriggerExit called with: " + other.gameObject.name);
        if (other.gameObject == pandaRightFinger) {
            rightFingerTouching = false;
        }
        else if (other.gameObject == pandaLeftFinger) {
            leftFingerTouching = false;
        }

        // this should be an || but for consistancy this is ignored for now
        if (!rightFingerTouching && !leftFingerTouching) {
            DetachTargetFromHand();
        }
    }

    private void AttachTargetToHand(GameObject target) {
        if (targetObject == null) {
            targetObject = target;
            target.transform.SetParent(pandaHand.transform);
            //Debug.Log("Target attached to panda hand.");
        }
    }

    private void DetachTargetFromHand() {
        if (targetObject != null) {
            targetObject.transform.SetParent(null);
            //Debug.Log("Target detached from panda hand.");
            targetObject = null;
        }
    }
}