using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.FrankaPandaMoveit;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using Panda.Core.Controller;

namespace Panda.Follower {
    public class FollowPlanner : MonoBehaviour {
        public static bool followToggle;
        public string rosServiceName = "franka_panda_follower";
        [SerializeField] GameObject target;
        public float speed = 1f; // percentage of max speed
        public float followDistance = 0.25f;
        public float positionThreshold = 0.11f;
        public float checkInterval = 0.01f;
        private Vector3 lastTargetPosition;
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private ROSConnection ros;
        private bool isMoving = false;
        private float timeAtLastMovement;

        public static void SetToggle(bool value) {
            followToggle = value;
        }

        void Start() {
            // Create ROS connection singleton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<FollowerServiceRequest, FollowerServiceResponse>(rosServiceName);

            // make sure, that speed is valid
            speed = Mathf.Clamp01(speed);

            lastTargetPosition = target.transform.position;
            timeAtLastMovement = Time.time;
        }

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

        public void SendPlanRequest() {
            // TODO: use factory
            var request = new FollowerServiceRequest();
            request.joints_input = CurrentJointState();

            // combine the rotations (y from the target - x, z from the m_PickOrientation)
            Quaternion combinedRotation = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z);

            request.target_pose = new PoseMsg {
                position = (target.transform.position + Vector3.up * followDistance).To<FLU>(),
                orientation = combinedRotation.To<FLU>()
            };

            // send request and evaluate response
            ros.SendServiceMessage<FollowerServiceResponse>(rosServiceName, request, TrajectoryResponse);
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

        void TrajectoryResponse(FollowerServiceResponse response) {
            if (response.trajectories.Length > 0) {
                StartCoroutine(ExecuteTrajectories(response));
            }
            else {
                Debug.LogError("No trajectory returned from FollowerService.");
            }
        }

        private IEnumerator ExecuteTrajectories(FollowerServiceResponse response) {
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