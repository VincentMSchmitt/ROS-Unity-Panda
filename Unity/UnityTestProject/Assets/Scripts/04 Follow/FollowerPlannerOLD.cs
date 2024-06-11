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
using UnityEngine.UI;  // for UI components

namespace Panda.Follower {
    public class FollowPlannerOLD : MonoBehaviour {
        public static readonly string[] LinkNames = {
            "world/panda_link0/panda_link1",
            "/panda_link2",
            "/panda_link3",
            "/panda_link4",
            "/panda_link5",
            "/panda_link6",
            "/panda_link7"};

        [Tooltip("The ROS servicename, which will be subscribed to")]
        [SerializeField] string rosServiceName = "franka_panda_follower";

        [Tooltip("The GameObject of the Franka Emika Panda")]
        [SerializeField] GameObject panda;

        [Tooltip("The GameObject of the target")]
        [SerializeField] GameObject target;

        [Tooltip("Selection of the correct TCP is important")]
        [SerializeField] GameObject pandaTCP;

        [Tooltip("Toggle to start/stop following the target")]
        [SerializeField] Toggle followToggle;

        [Tooltip("How fast the robot will wait after moving all joints in the Simulation")]
        [SerializeField] float jointAssignmentWait = 0.15f;

        [Tooltip("How long the robot will wait until he moves to the next position")]
        [SerializeField] float poseAssignmentWait = 0.5f;

        [Tooltip("After what time the robot will replan, if the target didn't reach the goal")]
        [SerializeField] float timeout = 2.5f;

        [Tooltip("How long the follower will timeout before planing again")]
        [SerializeField] float followerTimeout = 0.1f;

        [Tooltip("How close the robot will move to the target")]
        [SerializeField] float followDistance = 0.5f;

        [Tooltip("Tolerance for detecting target position changes")]
        [SerializeField] float positionTolerance = 0.01f;

        // Currently used for the visualization
        //TODO: move functionaliy to SplineDrawer after it is fixed
        [Tooltip("Enable or disable trajectory visualization")]
        [SerializeField] bool visualizeTrajectory = true;

        [Tooltip("Color of the trajectory spline")]
        [SerializeField] Color splineColor = Color.green;

        [Tooltip("Width of the trajectory spline")]
        [SerializeField] float lineWidth = 0.01f;

        private Vector3 lastTargetPosition;
        private bool targetPositionChanged = false;
        private ArticulationBody[] jointArticulationBodies;
        private ROSConnection ros;    
        private LineRenderer lineRenderer;
        private Coroutine followCoroutine;
        // z - Value assures that the gripper is always positioned above the m_Target cube before grasping.
        // y - Value is used to place the target facing the camera
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);

        /// <summary>
        /// Called in the first frame of the game. Initializes the FollowPlanner by setting up the ROS connection and
        /// getting the necessary components.
        /// </summary>
        void Start() {
            // Get ROS connection static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<FollowerServiceRequest, FollowerServiceResponse>(rosServiceName);

            // get the position of the target
            lastTargetPosition = target.transform.position;

            // Get Revolute Joints
            jointArticulationBodies = new ArticulationBody[LinkNames.Length];
            var linkName = string.Empty;
            for (var i = 0; i < LinkNames.Length; ++i) {
                // build link path
                linkName += LinkNames[i];
                // gets Joints (Joint 1 - 7, because Joint 0 and Joint 8 are fixed Joints)
                jointArticulationBodies[i] = panda.transform.Find(linkName).GetComponent<ArticulationBody>();
                // Throw Assertion when no Joints are found
                Assert.IsNotNull(jointArticulationBodies[i]);
            }

            // initialize line renderer
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.positionCount = 0;

            // subscribe to the toggles value change event
            Assert.IsNotNull(followToggle, "FollowToggle is not assigned in the inspector.");
            followToggle.onValueChanged.AddListener(OnFollowToggleChanged);
        }

        /// <summary>
        /// Checks if the target position has changed based on a specified tolerance.
        /// </summary>
        void CheckTargetPosition() {
            if (Vector3.Distance(lastTargetPosition, target.transform.position) > positionTolerance) {
                targetPositionChanged = true;
            }
            else {
                targetPositionChanged = false;
            }
        }

        /// <summary>
        /// Handles changes to the follow toggle's value.
        /// </summary>
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

        /// <summary>
        /// Coroutine that continuously checks the target position and publishes joint states.
        /// </summary>
        /// <returns>An enumerator for coroutine handling.</returns>
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

        /// <summary>
        /// Publishes the current joint states and target pose to the ROS service.
        /// </summary>
        public void PublishJoints() {
            var request = new FollowerServiceRequest();
            request.joints_input = CurrentJointConfig();

            // combine the rotations (y from the target - x, z from the m_PickOrientation)
            Quaternion combinedRotation = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z);

            request.target_pose = new PoseMsg {
                position = (target.transform.position + Vector3.up * followDistance).To<FLU>(),
                orientation = combinedRotation.To<FLU>()
            };

            ros.SendServiceMessage<FollowerServiceResponse>(rosServiceName, request, TrajectoryResponse);
        }

        /// <summary>
        /// Gets the current joint configuration of the robot.
        /// </summary>
        /// <returns>A message containing the current joint positions.</returns>
        PandaMoveitJointsMsg CurrentJointConfig() {
            var joints = new PandaMoveitJointsMsg();
            for (var i = 0; i < LinkNames.Length; ++i) {
                joints.joints[i] = jointArticulationBodies[i].jointPosition[0];
            }
            return joints;
        }

        /// <summary>
        /// Handles the response from the ROS service containing the planned trajectories.
        /// </summary>
        /// <param name="response">The response from the ROS service.</param>
        void TrajectoryResponse(FollowerServiceResponse response) {
            if (response.trajectories.Length > 0) {
                StartCoroutine(ExecuteTrajectories(response));
            }
            else {
                Debug.LogError("No trajectory returned from FollowerService.");
            }
        }

        /// <summary>
        /// Executes the trajectories received from the ROS service response.
        /// </summary>
        /// <param name="response">The response from the ROS service containing the trajectories.</param>
        /// <returns>An enumerator for coroutine handling.</returns>
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
                        for (var joint = 0; joint < jointArticulationBodies.Length; ++joint) {
                            var joint1XDrive = jointArticulationBodies[joint].xDrive;
                            joint1XDrive.target = result[joint];
                            jointArticulationBodies[joint].xDrive = joint1XDrive;
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

        /// <summary>
        /// Waits until the robot is positioned over the target.
        /// </summary>
        /// <returns>An enumerator for coroutine handling.</returns>
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

        /// <summary>
        /// Draws the trajectory of the robot based on the given trajectory points.
        /// </summary>
        /// <param name="trajectoryPoints">A list of points representing the trajectory.</param>
        void DrawTrajectory(List<Vector3> trajectoryPoints) {
            lineRenderer.positionCount = trajectoryPoints.Count;
            lineRenderer.SetPositions(trajectoryPoints.ToArray());
            lineRenderer.startColor = splineColor;
            lineRenderer.endColor = splineColor;
        }
    }
}