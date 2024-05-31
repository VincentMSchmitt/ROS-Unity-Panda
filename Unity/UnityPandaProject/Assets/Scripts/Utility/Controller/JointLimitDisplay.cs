using UnityEngine;

namespace Panda.Utility.Controller {
    public class JointLimitDisplay : MonoBehaviour {
        private static int lineCount = 100;
        private static float radius = 0.1f;
        private static float width = 0.01f;

        public static void DrawJointLimits(int selectedIndex, ArticulationBody[] articulationChain, LineRenderer lineRenderer, Color lineColor) {
            // dont draw anything for Prismatic joints
            ArticulationBody articulationBody = articulationChain[selectedIndex];
            if (articulationBody.jointType == ArticulationJointType.PrismaticJoint) {
                return;
            }
            
            float lowerLimit = articulationChain[selectedIndex].xDrive.lowerLimit;
            float upperLimit = articulationChain[selectedIndex].xDrive.upperLimit;

            lineRenderer.positionCount = lineCount;
            lineRenderer.startWidth = width;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineRenderer.startColor;
            lineRenderer.loop = true;

            // Search recursively for the first child GameObject named "Connector"
            GameObject gameObject = articulationChain[selectedIndex].gameObject;
            Transform connectorTransform = gameObject.transform.Find("Connector");
            if (connectorTransform == null) {
                Debug.LogError("No child named 'Connector' found.");
                return;
            }

            // get the position of the link
            Vector3 currentPosition = connectorTransform.position;

            // Set the line renderer positions
            Vector3[] linePositions = new Vector3[lineCount];
            Quaternion jointRotation = articulationChain[selectedIndex].transform.rotation;
            float theta = 2f * Mathf.PI / lineCount;
            float angle = 0;
            for (int i = 0; i < lineCount; ++i) {
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                // Calculate the position around the circle and apply the joint rotation
                Vector3 localPosition = new Vector3(x, 0, z);
                Vector3 rotatedPosition = jointRotation * localPosition;

                linePositions[i] = currentPosition + rotatedPosition;
                angle += theta;
            }
            lineRenderer.SetPositions(linePositions);
        }

        public static void ClearJointLimits(LineRenderer lineRenderer) {
            lineRenderer.positionCount = 0;
        }
    }
}