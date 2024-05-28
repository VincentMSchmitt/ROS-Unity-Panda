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

        // Iterate through each trajectory plan returned
        for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++) {
            // Iterate through each robot pose in the trajectory plan
            foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
                var jointPositions = t.positions;

                // Convert radians to degrees
                var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                // Calculate the end effector transformation
                Matrix4x4 finalTransformation = PandaKinematics.CalculateEndEffectorTransformation(result);

                // Extract position from the final transformation matrix
                Vector3 position = finalTransformation.GetColumn(3);
                //Debug.Log($"Pose Index: {poseIndex}, Position: {position}");

                // Add position to the trajectory points list
                trajectoryPoints.Add(position);
            }
        }

        // Draw the trajectory using the collected points as a spline
        DrawSpline(lineRenderer, trajectoryPoints);
    }

    private static void DrawSpline(LineRenderer lineRenderer, List<Vector3> points) {
        if (points == null || points.Count == 0) {
            Debug.LogWarning("No trajectory points to draw.");
            return;
        }

        // Ensure the LineRenderer has enough points
        lineRenderer.positionCount = points.Count * 10; // Increase the number of points for a smoother spline

        // Generate interpolated points
        List<Vector3> interpolatedPoints = InterpolatePoints(points);

        // Set the positions in the LineRenderer
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());

        Debug.Log("Trajectories drawn as spline!");
    }

    private static List<Vector3> InterpolatePoints(List<Vector3> originalPoints) {
        List<Vector3> interpolatedPoints = new List<Vector3>();

        for (int i = 0; i < originalPoints.Count - 1; ++i) {
            Vector3 p0 = originalPoints[Mathf.Max(i - 1, 0)];
            Vector3 p1 = originalPoints[i];
            Vector3 p2 = originalPoints[i + 1];
            Vector3 p3 = originalPoints[Mathf.Min(i + 2, originalPoints.Count - 1)];

            // Interpolate between p1 and p2
            for (int j = 0; j < 10; j++) {
                float t = j / 10f;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                interpolatedPoints.Add(point);
            }
        }

        // Add the last point
        interpolatedPoints.Add(originalPoints[originalPoints.Count - 1]);

        return interpolatedPoints;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        // Catmull-Rom spline formula (cSpline)
        // https://en.wikipedia.org/wiki/Cubic_Hermite_spline
        float t2 = t * t;
        float t3 = t2 * t;

        float f0 = -0.5f * t3 + t2 - 0.5f * t;
        float f1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float f2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float f3 = 0.5f * t3 - 0.5f * t2;

        return p0 * f0 + p1 * f1 + p2 * f2 + p3 * f3;
    }
}