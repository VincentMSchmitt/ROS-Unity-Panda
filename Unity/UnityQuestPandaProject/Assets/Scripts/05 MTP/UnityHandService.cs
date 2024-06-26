using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

using Panda.Core.Controller;
using RosMessageTypes.FrankaPandaCommunication;


namespace Panda.MTP {
    public class UnityHandService : MonoBehaviour {
        [Tooltip("Percentage of the max speed")] public float speed = 1f;
        private string rosServiceName = "unity_hand_service";
        private ROSConnection ros;

        void Start() {
            // Create ROS connection singelton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<HandServiceRequest, HandServiceResponse>(rosServiceName);

            // Implement the service
            ros.ImplementService<HandServiceRequest, HandServiceResponse>(rosServiceName, ServiceRequestHandler);
        }

        private async Task<HandServiceResponse> ServiceRequestHandler(HandServiceRequest request) {
            var response = new HandServiceResponse();
            if (request.gripper_targets != null) {
                Debug.Log("Hand trajectory request received from " + rosServiceName + ".");
                await ExecuteTrajectoriesAsync(request);
                response.success = true;
            } else {
                Debug.LogError("No trajectory returned from HandService.");
                response.success = false;
            }
            return response;
        }

        private Task ExecuteTrajectoriesAsync(HandServiceRequest request) {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(ExecuteTrajectories(request, () => tcs.SetResult(true)));
            return tcs.Task;
        }

        private IEnumerator WaitForAllCoroutines(List<Coroutine> coroutines) {
            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
        }

        private IEnumerator ExecuteTrajectories(HandServiceRequest request, System.Action onComplete) {
            RobotController robotController = RobotController.GetInstance;

            foreach (var point in request.gripper_targets) {
                Debug.Log("Current target: " + point);
                yield return 0.1;

                // // coroutines for joints movements
                // List<Coroutine> jointCoroutines = new List<Coroutine>();
                // List<IMoveCommand> revoluteJoints = robotController.GetHandJoints();
                // for (int i = 0; i < jointPositions.Length; ++i) {
                //     jointCoroutines.Add(StartCoroutine(revoluteJoints[i].MoveToTarget(result[i], speed)));
                // }
                // // wait, until every joint is where he is supposed to be
                // yield return StartCoroutine(WaitForAllCoroutines(jointCoroutines));
            }
            onComplete?.Invoke();
        }
    }
}