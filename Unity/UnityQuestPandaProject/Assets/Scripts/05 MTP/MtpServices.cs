using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

using Panda.Core.Controller;
using RosMessageTypes.FrankaPandaCommunication;


namespace Panda.MTP {
    enum MoveServiceType { Arm = 1, Hand = 2 }
    public class MtpServices : MonoBehaviour {
        [Tooltip("Percentage of the max speed")] public float speed = 1f;
        private string rosServiceName = "move_service";
        private ROSConnection ros;

        void Start() {
            // Create ROS connection singelton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<MoveServiceRequest, MoveServiceResponse>(rosServiceName);

            // Implement the service
            ros.ImplementService<MoveServiceRequest, MoveServiceResponse>(rosServiceName, ServiceRequestHandler);
        }

        private async Task<MoveServiceResponse> ServiceRequestHandler(MoveServiceRequest request) {
            var response = new MoveServiceResponse();
            if (request.trajectory != null) {
                Debug.Log("Move trajectory request received from " + rosServiceName + ".");
                await ExecuteTrajectoriesAsync(request);
                response.success = true;
            } else {
                Debug.LogError("No trajectory returned from MoveService.");
                response.success = false;
            }
            return response;
        }

        private Task ExecuteTrajectoriesAsync(MoveServiceRequest request) {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(ExecuteTrajectories(request, () => tcs.SetResult(true)));
            return tcs.Task;
        }

        private IEnumerator WaitForAllCoroutines(List<Coroutine> coroutines) {
            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
        }

        private IEnumerator ExecuteTrajectories(MoveServiceRequest request, System.Action onComplete) {
            RobotController robotController = RobotController.GetInstance;
            // cast uint8 into trajectoryType
            MoveServiceType trajectoryType = (MoveServiceType)request.trajectory_type;

            // check which kind of trajectory is sent and act accordingly
            if (trajectoryType == MoveServiceType.Arm) {
                foreach (var point in request.trajectory.joint_trajectory.points) {
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

            if (trajectoryType == MoveServiceType.Hand) {
                foreach (var point in request.trajectory.joint_trajectory.points) {
                    var jointPositions = point.positions;
                    var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                    Debug.LogWarning("Moving a hand joint is not implemented yet.");

                    // // coroutines for joints movements
                    // List<Coroutine> jointCoroutines = new List<Coroutine>();
                    // List<IMoveCommand> handJoints = robotController.GetHandJoints();
                    // for (int i = 0; i < jointPositions.Length; ++i) {
                    //     jointCoroutines.Add(StartCoroutine(handJoints[i].MoveToTarget(result[i], speed)));
                    // }
                    // // wait, until every joint is where he is supposed to be
                    // yield return StartCoroutine(WaitForAllCoroutines(jointCoroutines));
                }
            }

            onComplete?.Invoke();
        }
    }
}