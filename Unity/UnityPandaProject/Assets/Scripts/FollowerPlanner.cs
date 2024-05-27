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
using UnityEngine.UI;  // Für die UI-Komponenten

public class FollowPlanner : MonoBehaviour {
    // Linknames of the used robot (only up to the point where the tool is attached)
    public static readonly string[] linkNames = {
        "world/panda_link0/panda_link1",
        "/panda_link2",
        "/panda_link3",
        "/panda_link4",
        "/panda_link5",
        "/panda_link6",
        "/panda_link7"};

    // ROS related
    [Tooltip("The ROS servicename, which will be subscribed to")]
    [SerializeField] string rosServiceName = "franka_panda_follower";
    
    // GameObjects
    [Tooltip("The GameObject of the Franka Emika Panda")]
    [SerializeField] GameObject panda;

    [Tooltip("The GameObject of the target")]
    [SerializeField] GameObject target;

    [Tooltip("Selection of the correct TCP is important")]
    [SerializeField] GameObject pandaTCP;

    [Tooltip("Toggle to start/stop following the target")]
    [SerializeField] Toggle followToggle;

    // Joint Moving
    [Tooltip("How fast the robot will wait after moving all joints in the Simulation")]
    [SerializeField] float jointAssignmentWait = 0.15f;

    [Tooltip("How long the robot will wait until he moves to the next position")]
    [SerializeField] float poseAssignmentWait = 0.5f;

    [Tooltip("After what time the robot will replan, if the target didn't reach the goal")]
    [SerializeField] float timeout = 2.5f;

    // Follower related
    [Tooltip("How long the follower will timeout before planing again")]
    [SerializeField] float followerTimeout = 0.1f;

    [Tooltip("How close the robot will move to the target")]
    [SerializeField] float followDistance = 0.5f;

    [Tooltip("Tolerance for detecting target position changes")]
    [SerializeField] float positionTolerance = 0.01f;

    // Visualizer
    [Tooltip("Enable or disable trajectory visualization")]
    [SerializeField] bool visualizeTrajectory = true;

    [Tooltip("Color of the trajectory spline")]
    [SerializeField] Color splineColor = Color.green;

    [Tooltip("Width of the trajectory spline")]
    [SerializeField] float lineWidth = 0.01f;

    // Other global variables
    // z - Value assures that the gripper is always positioned above the m_Target cube before grasping.
    // y - Value is used to place the target facing the camera
    readonly Quaternion m_PickOrientation = Quaternion.Euler(0, 45, 180);
    private Vector3 lastTargetPosition;
    private bool targetPositionChanged = false;
    ArticulationBody[] m_JointArticulationBodies;
    ROSConnection m_Ros;    
    LineRenderer lineRenderer;
    Coroutine followCoroutine;

    void Start() {
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterRosService<FollowerServiceRequest, FollowerServiceResponse>(rosServiceName);

        // get the position of the target
        lastTargetPosition = target.transform.position;

        // Get Revolute Joints
        m_JointArticulationBodies = new ArticulationBody[linkNames.Length];
        var linkName = string.Empty;
        for (var i = 0; i < linkNames.Length; ++i) {
            // build link path
            linkName += linkNames[i];
            // gets Joints (Joint 1 - 7, because Joint 0 and Joint 8 are fixed Joints)
            m_JointArticulationBodies[i] = panda.transform.Find(linkName).GetComponent<ArticulationBody>();
            // Throw Assertion when no Joints are found
            Assert.IsNotNull(m_JointArticulationBodies[i]);
        }

        // Initialize Line Renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 0;

        // Subscribe to the Toggle's value change event
        Assert.IsNotNull(followToggle, "FollowToggle is not assigned in the inspector.");
        followToggle.onValueChanged.AddListener(OnFollowToggleChanged);
    }

    void CheckTargetPosition() {
    if (Vector3.Distance(lastTargetPosition, target.transform.position) > positionTolerance) {
        targetPositionChanged = true;
    }
    else {
        targetPositionChanged = false;
    }
}

    void OnFollowToggleChanged(bool isOn) {
        if (isOn) {
            // Start the coroutine to repeatedly call PublishJoints
            Debug.Log("Start Following!");
            followCoroutine = StartCoroutine(FollowRoutine());
        } 
        else {
            // Stop the coroutine
            if (followCoroutine != null) {
                Debug.Log("Stop Following!");
                StopCoroutine(followCoroutine);
            }
        }
    }

    IEnumerator FollowRoutine() {
        while (true) {
            CheckTargetPosition();
            if (targetPositionChanged) {
                PublishJoints();
                lastTargetPosition = target.transform.position;
            }
        yield return new WaitForSeconds(followerTimeout);
        }
    }

    PandaMoveitJointsMsg CurrentJointConfig() {
        var joints = new PandaMoveitJointsMsg();
        for (var i = 0; i < linkNames.Length; ++i) {
            joints.joints[i] = m_JointArticulationBodies[i].jointPosition[0];
        }
        return joints;
    }

    public void PublishJoints() {
        var request = new FollowerServiceRequest();
        request.joints_input = CurrentJointConfig();

        // combine the rotations (y from the target - x, z from the m_PickOrientation)
        Quaternion combinedRotation = Quaternion.Euler(m_PickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, m_PickOrientation.eulerAngles.z);

        request.target_pose = new PoseMsg {
            position = (target.transform.position + Vector3.up * followDistance).To<FLU>(),
            orientation = combinedRotation.To<FLU>()
        };

        m_Ros.SendServiceMessage<FollowerServiceResponse>(rosServiceName, request, TrajectoryResponse);
    }

    void TrajectoryResponse(FollowerServiceResponse response) {
        if (response.trajectories.Length > 0) {
            StartCoroutine(ExecuteTrajectories(response));
        }
        else {
            Debug.LogError("No trajectory returned from FollowerService.");
        }
    }

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
                    trajectoryPoints.Add(pandaTCP.transform.position);

                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(jointAssignmentWait);
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
            Vector3 pandaPosition = pandaTCP.transform.position;
            Vector3 targetPosition = target.transform.position;
            
            // Check if the robot is close to target
            float distanceToTarget = Vector3.Distance(pandaPosition, targetPosition);
            if (distanceToTarget <= followDistance) {
                yield return new WaitForSeconds(poseAssignmentWait);
                yield break;
            }

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= timeout) {
                Debug.LogWarning("Timeout reached while waiting for positioning over target. Replanning.");
                PublishJoints();
                yield return new WaitForSeconds(poseAssignmentWait);
                yield break;
            }
            yield return null;
        }
    }

    void DrawTrajectory(List<Vector3> trajectoryPoints) {
        lineRenderer.positionCount = trajectoryPoints.Count;
        lineRenderer.SetPositions(trajectoryPoints.ToArray());
        lineRenderer.startColor = splineColor;
        lineRenderer.endColor = splineColor;
    }
}