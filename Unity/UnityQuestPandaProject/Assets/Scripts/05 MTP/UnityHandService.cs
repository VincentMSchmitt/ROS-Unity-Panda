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

        private IEnumerator ExecuteTrajectories(HandServiceRequest request, System.Action onComplete) {
            RobotController robotController = RobotController.GetInstance;

            List<IMoveCommand> handJoints = robotController.GetHandJoints();
            Debug.Log("number of handJoints: " + handJoints.Count);
            Debug.Log("number of targets: " + request.gripper_targets.ToList<double>().Count);

            foreach (float point in request.gripper_targets) {
                yield return StartCoroutine(handJoints[0].MoveToTarget(point, speed));
            }
            onComplete?.Invoke();
        }
    }
}