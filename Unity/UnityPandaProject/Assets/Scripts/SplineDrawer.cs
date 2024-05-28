using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RosMessageTypes.FrankaPandaMoveit;

public class SplineDrawer : MonoBehaviour {

    [Tooltip("Color of the trajectory spline")]
    [SerializeField] Color splineColor = Color.green;

    [Tooltip("Width of the trajectory spline")]
    [SerializeField] float lineWidth = 0.01f;
    private static LineRenderer lineRenderer;

    void Start() {
        if (lineRenderer == null) {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Set initial properties
        lineRenderer.startColor = splineColor;
        lineRenderer.endColor = splineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
    }

    /// <summary>
    /// Draw the response as a spline.
    /// </summary>
    /// <param name="response"> MoverServiceResponse received from franka_panda_moveit mover service running in ROS</param>
    public static void DrawTrajectories(MoverServiceResponse response, ArticulationBody[] jointArticulationBodies) {
        // List to store positions of each joint in the final trajectory
        List<Vector3> trajectoryPoints = new List<Vector3>();

        if (response.trajectories != null) {
            // Iterate through each trajectory plan returned
            for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                // Iterate through each robot pose in the trajectory plan
                foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
                    var jointPositions = t.positions;

                    // Convert radians to degrees using lambda function
                    //var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
                    var result = jointPositions.Select(r => (float)r).ToArray();

                    // Calculate the end effector transformation
                    Matrix4x4 finalTransformation = DirectKinematics.ForwardKinematics(result);

                    // Extract position from the final transformation matrix
                    Vector3 tcpPosition = DirectKinematics.GetTCPPosition(finalTransformation);
                    //Debug.Log($"Pose Index: {poseIndex}, Position: {tcpPosition}");

                    // Add position to the trajectory points list
                    trajectoryPoints.Add(tcpPosition);
                }
            }
        }

        // Draw the trajectory using the collected points as a spline
        DrawSpline(trajectoryPoints);
    }

    private static void DrawSpline(List<Vector3> points) {
        if (points == null || points.Count == 0) {
            Debug.LogWarning("No trajectory points to draw.");
            return;
        }

        // Set the positions in the LineRenderer
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        //Debug.Log("Trajectories drawn.");
    }
}