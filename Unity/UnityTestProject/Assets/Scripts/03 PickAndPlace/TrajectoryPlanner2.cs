using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;
using System.Collections;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

using Panda.Core.Controller;
using System;
using System.Collections.Generic;

namespace Panda.PickAndPlace {
    public class TrajectoryPlanner2 : MonoBehaviour {
        public string rosServiceName = "franka_panda_moveit";
        [SerializeField] GameObject target;
        [SerializeField] GameObject targetPlacement;
        // TODO: use this to set the speed to a % of the top speed available
        public float speed = 0.1f; // percentage of max speed
        public float poseAssignmentWait = 1f;
        public float upwardsOffset = 0.2f;
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private Vector3 pickPoseOffset => Vector3.up * upwardsOffset;
        private const float gripperOffset = 0.105f;
        private ROSConnection ros;

        private void Start() {
            // Create ROS connection singelton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(rosServiceName);
        }

        public void SendPlanRequest() {
            var request = TrajectoryRequestFactory.CreateRequest(
                CurrentJointState(),
                new PoseMsg {
                    position = (target.transform.position + pickPoseOffset).To<FLU>(),
                    orientation = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z).To<FLU>()
                },
                new PoseMsg {
                    position = (targetPlacement.transform.position + pickPoseOffset).To<FLU>(),
                    orientation = pickOrientation.To<FLU>()
                },
                pickPoseOffset.y - gripperOffset
            );
            // send request
            ros.SendServiceMessage<MoverServiceResponse>(rosServiceName, request, TrajectoryResponseHandler);
        }

        private PandaMoveitJointsMsg CurrentJointState() {
            var msg = new PandaMoveitJointsMsg();
            RobotController robotController = RobotController.GetInstance;
            List<float> targets = robotController.GetRevoluteJointTargets();
            // if we have more or less than 7 revolute joints
            if (targets.Count != 7) {
                throw new ArgumentOutOfRangeException("targets",
                    $"The PandaMoveitJointMsg needs exactly 7 joints.Passed joint values: {targets.Count}");
            }
            msg.joints = targets.Select(j => (double)j * Mathf.Deg2Rad).ToArray();
            return msg;
        }

        private void TrajectoryResponseHandler(MoverServiceResponse response) {
            StartCoroutine(TrajectoryResponse(response));
        }

        private IEnumerator TrajectoryResponse(MoverServiceResponse response) {
            if (response.trajectories.Length > 0) {
                yield return StartCoroutine(ExecuteTrajectories(response));
            }
            else {
                Debug.LogError("No trajectory returned from MoverService.");
            }
        }

        private IEnumerator ExecuteTrajectories(MoverServiceResponse response) {
            RobotController robotController = RobotController.GetInstance;
            if (response.trajectories != null) {
                for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                    foreach (var point in response.trajectories[poseIndex].joint_trajectory.points) {
                        var jointPositions = point.positions;
                        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                        // List for couroutines for all joints
                        List<Coroutine> coroutines = new List<Coroutine>();
                        for (int i = 0; i < jointPositions.Length; i++) {
                            coroutines.Add(StartCoroutine(robotController.joints[i].MoveToTarget(result[i], speed)));
                        }

                        // wait, until every joint is where he is supposed to be
                        foreach (var coroutine in coroutines) {
                            yield return coroutine;
                        }
                    }
                    yield return new WaitForSeconds(poseAssignmentWait);
                }
            }
        }
    }
}