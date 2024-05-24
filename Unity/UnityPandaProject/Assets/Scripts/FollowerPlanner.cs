using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.FrankaPandaMoveit;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using UnityEngine.Assertions;

public class DistanceTrajectoryPlanner : MonoBehaviour {
    // Linknames of the used robot (only up to the point where the tool is attached) ----------------------------------
    public static readonly string[] LinkNames = {
        "world/panda_link0/panda_link1",
        "/panda_link2",
        "/panda_link3",
        "/panda_link4",
        "/panda_link5",
        "/panda_link6",
        "/panda_link7"};

    // Serialized variables -------------------------------------------------------------------------------------------
    [Tooltip("The ROS servicename, which will be subscribed to")]
    [SerializeField] string m_RosServiceName = "franka_panda_follower";
    
    [Tooltip("The GameObject of the Franka Emika Panda")]
    [SerializeField] GameObject m_FrankaPanda;

    [Tooltip("The GameObject of the target")]
    [SerializeField] GameObject m_Target;

    [Tooltip("Selection of the correct TCP is important")]
    [SerializeField] GameObject m_PandaTCP;

    [Tooltip("How fast the robot will wait after moving all joints in the Simulation")]
    [SerializeField] float JointAssignmentWait = 0.15f;

    [Tooltip("How long the robot will wait until he moves to the next position")]
    [SerializeField] float PoseAssignmentWait = 0.5f;

    [Tooltip("How close the robot will move to the target")]
    [SerializeField] float tolerance = 0.01f;

    [Tooltip("After what time the robot will replan, if the target didn't reach the goal")]
    [SerializeField] float timeout = 2.5f;

    [Tooltip("How close the robot will move to the target")]
    [SerializeField] float FollowDistance = 0.5f;

    [Tooltip("Enable or disable trajectory visualization")]
    [SerializeField] bool visualizeTrajectory = true;

    [Tooltip("Color of the trajectory spline")]
    [SerializeField] Color splineColor = Color.green;

    [Tooltip("Width of the trajectory spline")]
    [SerializeField] float lineWidth = 0.01f;

    // Other global variables -----------------------------------------------------------------------------------------
    // z - Value assures that the gripper is always positioned above the m_Target cube before grasping.
    // y - Value is used to place the target facing the camera
    readonly Quaternion m_PickOrientation = Quaternion.Euler(0, 45, 180);
    ArticulationBody[] m_JointArticulationBodies;
    ROSConnection m_Ros;    
    LineRenderer lineRenderer;

    // Functions ------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Find all robot joints in Awake() and add them to the jointArticulationBodies array.
    ///     Find left and right finger joints and assign them to their respective articulation body objects.
    /// </summary>
    void Start() {
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterRosService<FollowerServiceRequest, FollowerServiceResponse>(m_RosServiceName);

        // Get Revolute Joints
        m_JointArticulationBodies = new ArticulationBody[LinkNames.Length];
        var linkName = string.Empty;
        for (var i = 0; i < LinkNames.Length; ++i) {
            // build link path
            linkName += LinkNames[i];
            // gets Joints (Joint 1 - 7, because Joint 0 and Joint 8 are fixed Joints)
            m_JointArticulationBodies[i] = m_FrankaPanda.transform.Find(linkName).GetComponent<ArticulationBody>();
            // Throw Assertion when no Joints are found
            Assert.IsNotNull(m_JointArticulationBodies[i]);
        }

        // Initialize Line Renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    ///     Get the current values of the robot's joint angles.
    /// </summary>
    /// <returns>PandaMoveitJointsMsg</returns>
    PandaMoveitJointsMsg CurrentJointConfig() {
        var joints = new PandaMoveitJointsMsg();
        for (var i = 0; i < LinkNames.Length; ++i) {
            joints.joints[i] = m_JointArticulationBodies[i].jointPosition[0];
        }
        return joints;
    }

    /// <summary>
    ///     Create a new MoverServiceRequest with the current values of the robot's joint angles,
    ///     the target cube's current position and rotation, and the targetPlacement position and rotation.
    ///     Call the MoverService using the ROSConnection and if a trajectory is successfully planned,
    ///     execute the trajectories in a coroutine.
    /// </summary>
    public void PublishJoints() {
        var request = new FollowerServiceRequest();
        request.joints_input = CurrentJointConfig();

        Quaternion combinedRotation = Quaternion.Euler(m_Target.transform.rotation.eulerAngles.x, m_Target.transform.rotation.eulerAngles.y + 45, 180);

        request.target_pose = new PoseMsg {
            position = (m_Target.transform.position + Vector3.up * FollowDistance).To<FLU>(),
            orientation = combinedRotation.To<FLU>()
        };

        m_Ros.SendServiceMessage<FollowerServiceResponse>(m_RosServiceName, request, TrajectoryResponse);
    }

    void TrajectoryResponse(FollowerServiceResponse response) {
        if (response.trajectories.Length > 0) {
            StartCoroutine(ExecuteTrajectories(response));
        }
        else {
            Debug.LogError("No trajectory returned from FollowerService.");
        }
    }

    /// <summary>
    ///     Execute the returned trajectories from the FollowerService.
    ///     Executing a single trajectory will iterate through every robot pose in the array while updating the joint values on the robot.
    /// </summary>
    /// <param name="response"> FollowerRespone received from franka_panda_follower follower service running in ROS</param>
    /// <returns></returns>
    IEnumerator ExecuteTrajectories(FollowerServiceResponse response) {
        lineRenderer.positionCount = 0;

        if (response.trajectories != null) {
            // Collect all trajectory points
            var trajectoryPoints = new List<Vector3>();

            // For every trajectory plan returned
            for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                // For every robot pose in trajectory plan
                foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
                    var jointPositions = t.positions;
                    var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                    // Set the joint values for every joint
                    for (var joint = 0; joint < m_JointArticulationBodies.Length; ++joint) {
                        var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                    }

                    // Add current TCP position to trajectory points
                    trajectoryPoints.Add(m_PandaTCP.transform.position);

                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(JointAssignmentWait);
                }

                // Wait until the TCP is directly above m_Target
                yield return StartCoroutine(WaitUntilPositionedOverTarget());
            }

            // Draw the trajectory spline
            if (visualizeTrajectory) {
                DrawTrajectory(trajectoryPoints);
            }
        }
    }

    IEnumerator WaitUntilPositionedOverTarget() {
        float elapsedTime = 0.0f;

        while (true) {
            Vector3 pandaPosition = m_PandaTCP.transform.position;
            Vector3 targetPosition = m_Target.transform.position;
            
            // Check if the robot is close to target
            float distanceToTarget = Vector3.Distance(pandaPosition, targetPosition);
            if (distanceToTarget <= FollowDistance) {
                yield return new WaitForSeconds(PoseAssignmentWait);
                yield break;
            }

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= timeout) {
                Debug.LogWarning("Timeout reached while waiting for positioning over target. Replanning.");
                PublishJoints();
                yield return new WaitForSeconds(PoseAssignmentWait);
                yield break;
            }
            yield return null;
        }
    }

    /// <summary>
    ///     Draws the trajectory spline in the scene.
    /// </summary>
    /// <param name="trajectoryPoints">List of trajectory points to draw</param>
    void DrawTrajectory(List<Vector3> trajectoryPoints) {
        lineRenderer.positionCount = trajectoryPoints.Count;
        lineRenderer.SetPositions(trajectoryPoints.ToArray());
        lineRenderer.startColor = splineColor;
        lineRenderer.endColor = splineColor;
    }
}