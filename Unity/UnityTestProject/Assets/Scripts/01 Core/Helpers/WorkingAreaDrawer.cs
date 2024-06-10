using UnityEngine;

namespace Panda.Core {
    public class WorkingAreaDrawer : MonoBehaviour {
        public static bool toggleWorkingArea = true;
        private const float radius = 0.855f;
        private const float nearEdgeDistance = 0.075f;
        private Color insideColor = Color.green;
        private Color nearEdgeColor = Color.yellow;
        private Color outsideColor = Color.red;

        public static void SetToogle(bool value) {
            toggleWorkingArea = value;
        }

        private void OnDrawGizmos() {
            if (toggleWorkingArea) {
                GameObject robot = GameObject.FindWithTag("robot");
                GameObject target = GameObject.FindWithTag("target");

                if (robot != null) {
                    Vector3 robotPosition = robot.transform.position;

                    if (target != null) {
                        Vector3 targetPosition = target.transform.position;
                        float distance = Vector3.Distance(new Vector3(robotPosition.x, 0, robotPosition.z), new Vector3(targetPosition.x, 0, targetPosition.z));

                        if (distance <= radius) {
                            if (distance >= radius - nearEdgeDistance) {
                                Gizmos.color = nearEdgeColor;
                            } else {
                                Gizmos.color = insideColor;
                            }
                        } else {
                            Gizmos.color = outsideColor;
                        }
                    }
                    DrawCircle(robotPosition, radius);
                }
            }
        }

        private void DrawCircle(Vector3 center, float radius) {
            int segments = 360;
            float angle = 360f / segments;

            Vector3 previousPoint = center + new Vector3(Mathf.Cos(0) * radius, 0, Mathf.Sin(0) * radius);

            for (int i = 1; i <= segments; i++) {
                float rad = Mathf.Deg2Rad * (i * angle);
                Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
                Gizmos.DrawLine(previousPoint, newPoint);
                previousPoint = newPoint;
            }
        }
    }
}