/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using System.Collections;
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
            // set to foce control
            robotController.SetControlTypeMoveit();
            IMoveCommand gripper = robotController.GetGripper();
            foreach (float point in request.gripper_targets) {
                // assume gripper targets are identical, so only first one is used here since both grippers are
                // operated together 
                yield return StartCoroutine(gripper.MoveToTarget(point, speed));
            }
            onComplete?.Invoke();
        }
    }
}