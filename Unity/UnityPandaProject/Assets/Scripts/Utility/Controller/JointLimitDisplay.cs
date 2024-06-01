using UnityEngine;

namespace Panda.Utility.Controller {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class JointLimitDisplay : MonoBehaviour {
        private static int lineCount = 100;
        private static float radius = 0.5f;
        private static float lineWidth = 0.01f;

        private static GameObject lineObject;
        private static MeshFilter staticMeshFilter;

        public static void DrawJointLimits(int selectedIndex, ArticulationBody[] articulationChain, MeshFilter meshFilter, Material material) {
            // don't draw anything for Prismatic joints
            ArticulationBody articulationBody = articulationChain[selectedIndex];
            if (articulationBody.jointType == ArticulationJointType.PrismaticJoint) {
                return;
            }

            // Convert limits to radians
            float lowerLimit = articulationChain[selectedIndex].xDrive.lowerLimit * Mathf.PI / 180;
            float upperLimit = articulationChain[selectedIndex].xDrive.upperLimit * Mathf.PI / 180;

            // Search recursively for the first child GameObject named "Connector"
            GameObject gameObject = articulationChain[selectedIndex].gameObject;
            Transform connectorTransform = gameObject.transform.Find("Connector");
            if (connectorTransform == null) {
                Debug.LogError("No child named 'Connector' found.");
                return;
            }

            // get the position of the link
            Vector3 currentPosition = connectorTransform.position;

            // Set up the mesh vertices and triangles
            Vector3[] vertices = new Vector3[lineCount + 1];    // +1 for the center point
            int[] triangles = new int[(lineCount - 1) * 3];     // (lineCount - 1) segments each having 3 vertices

            // Add the center point
            vertices[0] = currentPosition;

            Quaternion jointRotation = articulationChain[selectedIndex].transform.rotation;

            float angle = lowerLimit;
            float delta = (upperLimit - lowerLimit) / (lineCount - 1);

            for (int i = 0; i < lineCount; ++i) {
                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                // Calculate the position around the circle and apply the joint rotation
                Vector3 localPosition = new Vector3(x, 0, z);
                Vector3 rotatedPosition = jointRotation * localPosition;

                vertices[i + 1] = currentPosition + rotatedPosition;

                // Create triangles
                if (i < lineCount - 1) {
                    triangles[i * 3] = 0;           // each triangular area begins with the central vertex
                    triangles[i * 3 + 1] = i + 1;   // current outer point on the circle
                    triangles[i * 3 + 2] = i + 2;   // next outer point on the circle
                }
                angle += delta;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            // Set the color of the mesh
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            meshRenderer.material = material;

            // Draw initial position line
            UpdateJointPositionLine(selectedIndex, articulationChain, currentPosition, jointRotation, material);
        }

        public static void UpdateJointPositionLine(int selectedIndex, ArticulationBody[] articulationChain, Vector3 currentPosition, Quaternion jointRotation, Material material) {
            if (lineObject == null) {
                lineObject = new GameObject("CurrentPositionLine");
                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.material = material;
                lineRenderer.material.color = Color.blue;
                lineRenderer.positionCount = 2;
            }

            LineRenderer lr = lineObject.GetComponent<LineRenderer>();

            // handeling the start position (joint0)
            if (articulationChain[selectedIndex].jointPosition.dofCount == 0) {
                return;
            }

            float currentPositionAngle = articulationChain[selectedIndex].jointPosition[0] * Mathf.PI / 180;
            float x = radius * Mathf.Cos(currentPositionAngle);
            float z = radius * Mathf.Sin(currentPositionAngle);

            Vector3 localPosition = new Vector3(x, 0, z);
            Vector3 rotatedPosition = jointRotation * localPosition;

            lr.SetPosition(0, currentPosition);
            lr.SetPosition(1, currentPosition + rotatedPosition);
        }

        public static void ClearJointLimits(MeshFilter meshFilter) {
            meshFilter.mesh = null;

            if (lineObject != null) {
                GameObject.Destroy(lineObject);
                lineObject = null;
            }
        }

        public static void SetMeshFilter(MeshFilter meshFilter) {
            staticMeshFilter = meshFilter;
        }

        public static MeshFilter GetMeshFilter() {
            return staticMeshFilter;
        }
    }
}