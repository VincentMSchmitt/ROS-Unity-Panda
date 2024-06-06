using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;
using System.Collections;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

using Panda.Core.Controller;
using System;

namespace Panda.PickAndPlace {
    public class TrajectoryPlanner2 : MonoBehaviour {
        public string rosServiceName = "franka_panda_moveit";
        [SerializeField] GameObject target;
        [SerializeField] GameObject targetPlacement;
        // TODO: use this to set the speed to a % of the top speed available
        public float speed = 0.1f; // percentage of max speed
        public float upwardsOffset = 0.2f;
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private Vector3 pickPoseOffset => Vector3.up * upwardsOffset;
        private const float gripperOffset = 0.105f;
        private ROSConnection ros;
        private ArticulationBody[] articulationChain;

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
            var joints = new PandaMoveitJointsMsg();
            ArticulationBody[] articulationChain = RobotController.GetInstance.GetCurrentState();
            
            for (var i = 0; i < articulationChain.Length; ++i) {
                if (articulationChain[i].jointType == ArticulationJointType.RevoluteJoint) {
                    joints.joints[i] = articulationChain[i].jointPosition[0];
                    Debug.Log("joint" + i + ": " + joints.joints[i]);
                }
            }
            return joints;
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
            if (response.trajectories != null) {
                for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                    foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
                        var jointPositions = t.positions;
                        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                        // TODO: clean up interface to prevent this
                        var moveCommand = new MoveCommand(articulationChain, result) as ICommand;
                        moveCommand.Execute();

                        yield return new WaitForSeconds(0.075f);
                    }

                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }
}