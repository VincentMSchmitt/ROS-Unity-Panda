/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.FrankaPandaCommunication;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using Panda.Core.Controller;

namespace Panda.Follower {
    /// <summary>
    /// Manages the following of a target using ROS and Unity components.
    /// </summary>
    public class FollowPlanner : MonoBehaviour {
        [Tooltip("Enable/Disable following")] public static bool followToggle;
        [Tooltip("The ROS servicename, which will be subscribed to")] public string rosServiceName = "franka_panda_follower";
        [Tooltip("The GameObject of the target")] [SerializeField] GameObject target;
        [Tooltip("Percentage of the max speed")] public float speed = 1f;
        [Tooltip("How high the robot will plan above the target (in meters) to avoid collisions")] public float followDistance = 0.25f;
        [Tooltip("Tolerance for detecting target position changes")] public float positionThreshold = 0.11f;
        [Tooltip("Interval in which the follower will check for a moved target")] public float checkInterval = 0.01f;
        private Vector3 lastTargetPosition;
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private ROSConnection ros;
        private bool isMoving = false;
        private float timeAtLastMovement;

        public static void SetToggle(bool value) {
            followToggle = value;
        }

        /// <summary>
        /// Called in the first frame of the game. Initializes the FollowPlanner by setting up the ROS connection and
        /// getting the necessary components.
        /// </summary>
        void Start() {
            // Create ROS connection singleton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<FollowerRequest, FollowerResponse>(rosServiceName);

            // make sure, that speed is valid
            speed = Mathf.Clamp01(speed);

            lastTargetPosition = target.transform.position;
            timeAtLastMovement = Time.time;
        }

        /// <summary>
        /// Coroutine that continuously checks the target position and publishes joint states.
        /// </summary>
        /// <returns>An enumerator for coroutine handling.</returns>
        public IEnumerator FollowRoutine() {
            // first plan request
            SendPlanRequest();
            while (followToggle) {
                if (!isMoving && HasTargetMoved()) {
                    SendPlanRequest();
                }
                yield return new WaitForSeconds(checkInterval); // Check target position every 0.1 seconds
            }
            yield break;
        }

        /// <summary>
        /// Checks if the target position has changed based on a specified tolerance.
        /// </summary>
        bool HasTargetMoved() {
            float distance = Vector3.Distance(lastTargetPosition, target.transform.position);
            if (distance > positionThreshold) {
                if (Time.time - timeAtLastMovement > 0.5f) {
                    lastTargetPosition = target.transform.position;
                    timeAtLastMovement = Time.time; // reset the timer
                    return true;
                }
            } else {
                timeAtLastMovement = Time.time; // reset the timer if target hasn't moved
            }
            return false;
        }

        /// <summary>
        /// Publishes the current joint states and target pose to the ROS service and waits for an response.
        /// </summary>
        public void SendPlanRequest() {
            var request = new FollowerRequest();
            request.joints_input = CurrentJointState();

            // combine the rotations (y from the target - x, z from the m_PickOrientation)
            Quaternion combinedRotation = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z);

            request.target_pose = new PoseMsg {
                position = (target.transform.position + Vector3.up * followDistance).To<FLU>(),
                orientation = combinedRotation.To<FLU>()
            };

            // send request and evaluate response
            ros.SendServiceMessage<FollowerResponse>(rosServiceName, request, TrajectoryResponse);
        }

        private PandaMoveitJointsMsg CurrentJointState() {
            var msg = new PandaMoveitJointsMsg();
            RobotController robotController = RobotController.GetInstance;
            List<float> targets = robotController.GetRevoluteJointTargets();
            // if we have more or less than 7 revolute joints
            if (targets.Count != 7) {
                throw new ArgumentOutOfRangeException("targets",
                    $"The PandaMoveitJointMsg needs exactly 7 joints. Passed joint values: {targets.Count}");
            }
            msg.joints = targets.Select(j => (double)j * Mathf.Deg2Rad).ToArray();
            return msg;
        }

        /// <summary>
        /// Handles the response from the ROS service containing the planned trajectories.
        /// </summary>
        /// <param name="response">The response from the ROS service.</param>
        void TrajectoryResponse(FollowerResponse response) {
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
        private IEnumerator ExecuteTrajectories(FollowerResponse response) {
            isMoving = true;
            RobotController robotController = RobotController.GetInstance;
            if (response.trajectories != null) {
                for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                    foreach (var point in response.trajectories[poseIndex].joint_trajectory.points) {
                        var jointPositions = point.positions;
                        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                        // coroutines for joints movements
                        List<Coroutine> jointCoroutines = new List<Coroutine>();
                        List<IMoveCommand> revoluteJoints = robotController.GetRevoluteJoints();
                        for (int i = 0; i < jointPositions.Length; ++i) {
                            jointCoroutines.Add(StartCoroutine(revoluteJoints[i].MoveToTarget(result[i], speed)));
                        }
                        // wait, until every joint is where he is supposed to be
                        yield return StartCoroutine(WaitForAllCoroutines(jointCoroutines));
                    }
                }
            }
            isMoving = false;
        }

        private IEnumerator WaitForAllCoroutines(List<Coroutine> coroutines) {
            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
        }
    }
}