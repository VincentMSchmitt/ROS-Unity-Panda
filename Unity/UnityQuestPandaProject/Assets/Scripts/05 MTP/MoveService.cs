using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

using Panda.Core.Controller;
using RosMessageTypes.FrankaPandaCommunication;


namespace Panda.MTP {
    enum MoveServiceType { Arm = 1, Hand = 2 }
    public class MoveService : MonoBehaviour {
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

        private MoveServiceResponse ServiceRequestHandler(MoveServiceRequest request) {
            if (request.trajectory != null) {
                Debug.Log("Move trajectory request received from " + rosServiceName + ".");
                StartCoroutine(ExecuteTrajectories(request));
            }
            else {
                Debug.LogError("No trajectory returned from MoveService.");
            }

            // Create and return the response
            MoveServiceResponse response = new MoveServiceResponse();
            response.success = true;
            return response;
        }

        private IEnumerator WaitForAllCoroutines(List<Coroutine> coroutines) {
            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
        }

        private IEnumerator ExecuteTrajectories(MoveServiceRequest request) {
            RobotController robotController = RobotController.GetInstance;

            if (request.trajectory != null) {
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
        }
    }
}