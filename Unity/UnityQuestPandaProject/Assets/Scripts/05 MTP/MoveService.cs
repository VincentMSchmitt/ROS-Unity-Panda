using RosMessageTypes.FrankaPandaCommunication;
using System;
using System.Collections;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace Panda.MTP {
    enum MoveServiceType { Arm = 1, Hand = 2 }
    public class MoveService : MonoBehaviour {
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
            // You can add logic to handle different types of movement here
            if (request.trajectory_type == (int)MoveServiceType.Arm) {
                // TODO: implement movement
                Debug.Log("Moving the arm with trajectory data.");
            } else if (request.trajectory_type == (int)MoveServiceType.Hand) {
                // TODO: implement movement
                Debug.Log("Moving the hand with trajectory data.");
            }

            // Create and return the response
            MoveServiceResponse response = new MoveServiceResponse();
            response.success = true;
            return response;
        }

        private IEnumerator TrajectoryResponse(MoveServiceRequest request) {
            if (request.trajectory != null) {
                throw new NotImplementedException("This method is not implemented yet.");
            }
            else {
                Debug.LogError("No trajectory returned from MoverService.");
                
            }
            yield break;
        }
    }
}