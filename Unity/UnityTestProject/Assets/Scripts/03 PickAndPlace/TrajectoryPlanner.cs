using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;
using System.Collections;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using System;
using System.Collections.Generic;

using Panda.Core.Controller;
using Panda.Core;

namespace Panda.PickAndPlace {
    enum Poses { PreGrasp, Grasp, PickUp, PrePlace, Place }
    public class TrajectoryPlanner : MonoBehaviour {
        public string rosServiceName = "franka_panda_moveit";
        [SerializeField] GameObject target;
        [SerializeField] GameObject targetPlacement;
        public float speed = 0.1f; // percentage of max speed
        public float poseAssignmentWait = 1f;
        public float upwardsOffset = 0.2f;
        private const float gripperOffset = 0.105f; // panda specific
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private Vector3 pickPoseOffset => Vector3.up * upwardsOffset;
        private ROSConnection ros;

        private void Start() {
            // make sure, that speed is valid
            speed = Mathf.Clamp01(speed);

            // Create ROS connection singelton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(rosServiceName);
        }

        public void SendPlanRequest() {
            // init request
            var request = new MoverServiceRequest();
            request.joints_input = CurrentJointState();

            // first make sure the orientation of the gripper is always correct while gripping
            // Renderer.bounds returns the Axis-Aligned Bounding Box (AABB) - Boundaries in global space that take into
            // account the current position, rotation and scaling of the GameObject
            // Renderer.localBounds returns the Oriented Bounding Box (OBB) - Local limits in the GameObject's own space,
            // independent of global position, rotation and scaling

            // since the localBounds are independant of the scale, we need to apply the scale manually
            Vector3 scaledLocalSize = Vector3.Scale(target.GetComponent<Renderer>().localBounds.size, target.transform.localScale);

            // Check if either dimension is okay:
            if (scaledLocalSize.x > 0.075f && scaledLocalSize.z > 0.075f) {
                Debug.LogError("Target is too big for the grippers.");
            }

            // Check which dimension is smaller
            Quaternion additionalRotation = Quaternion.Euler(0, 0, 0);
            // x is too big and z is okay
            if (scaledLocalSize.x > 0.075f && scaledLocalSize.z <= 0.075f) {
                // rotate 90 deg around y
                additionalRotation = Quaternion.Euler(0, 90, 0);
                Debug.Log("Target is too large in the x dimension, rotating to grip along the z dimension.");
            }
            // z is too big and x is okay
            else if (scaledLocalSize.z > 0.075f && scaledLocalSize.x <= 0.075f) {
                // dont change anything
                Debug.Log("Target is too large in the z dimension, rotating to grip along the x dimension.");
            }
            else {
                Debug.Log("Both Dimensions are fine. Using smaller dimension.");
                // if x dimension is bigger, if z is bigger dont do anything
                if (scaledLocalSize.x >= scaledLocalSize.z) {
                    // rotate 90 deg around y
                    additionalRotation = Quaternion.Euler(0, 90, 0);
                }
            }  

            // combine the rotations (y from the target - x, z from the m_PickOrientation)
            // check https://quaternions.online for explanation regarding Quaternions
            Quaternion combinedRotation = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z) * additionalRotation;

            // build the meassge:
            List<GameObject> targets = new();
            targets = GameObjectFilter.GetAllGameObjectsWithTag("target");
            if (targets.Count != 1) {
                Debug.LogError("It is not allowed to have multiple targets. Please make sure there is only one at a time");
                return;
            }

            var objectInfo = GetObjectInfo(targets[0]);
            request.target = new ObjectInfoMsg {
                name = objectInfo.name,
                position = objectInfo.position,
                rotation = objectInfo.rotation,
                size = new Vector3Msg(objectInfo.size.x, objectInfo.size.y, objectInfo.size.z)
            };

            // define the poses for the pick and place
            // pick pose
            request.pick_pose = new PoseMsg {
                position = (target.transform.position + pickPoseOffset).To<FLU>(),
                orientation = combinedRotation.To<FLU>() // use combined rotation
            };

            // place pose
            request.place_pose = new PoseMsg {
                position = (targetPlacement.transform.position + pickPoseOffset).To<FLU>(),
                orientation = pickOrientation.To<FLU>() // use predefined rotation to asure target faces the camera
            };

            // send selected offset in y dimension (up) with the request
            request.offset = new PandaMoveitOffsetMsg(pickPoseOffset.y - gripperOffset);

            // send request and evaluate response
            ros.SendServiceMessage<MoverServiceResponse>(rosServiceName, request, TrajectoryResponseHandler);
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
            List<IMoveCommand> joints = RobotController.GetInstance.GetRevoluteJoints();
            IMoveCommand gripper = RobotController.GetInstance.GetGripper();
            if (response.trajectories != null) {
                for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                    foreach (var point in response.trajectories[poseIndex].joint_trajectory.points) {
                        var jointPositions = point.positions;
                        
                        // convert radiant to degree
                        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                        // couroutines for joints movements
                        List<Coroutine> jointCoroutines = new List<Coroutine>();

                        for (int i = 0; i < jointPositions.Length; ++i) {
                            jointCoroutines.Add(StartCoroutine(joints[i].MoveToTarget(result[i], speed)));
                        }
                        // wait, until every joint is where it is supposed to be
                        yield return StartCoroutine(WaitForAllCoroutines(jointCoroutines));
                    }

                    // wait before executing next pose
                    yield return new WaitForSeconds(poseAssignmentWait);

                    // handle event based on the pose
                    switch (poseIndex) {
                        case (int)Poses.PreGrasp:
                            StartCoroutine(gripper.MoveToTarget(GripperTarget.Open, speed));
                            break;
                        case (int)Poses.Grasp:
                            // Close the gripper if completed executing the trajectory
                            StartCoroutine(gripper.MoveToTarget(GripperTarget.Close, speed));
                            break;
                        case (int)Poses.Place:
                            // Open the gripper if completed executing the trajectory for the Place pose
                            StartCoroutine(gripper.MoveToTarget(GripperTarget.Open, speed));
                            break;
                        default:
                            Debug.Log("Unexpected pose index found.");
                            break;
                    }
                }
            }
        }

        private IEnumerator WaitForAllCoroutines(List<Coroutine> coroutines) {
            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
        }

        /// <summary>
        /// Retrieves the information of a single object, including its position, rotation, and size.
        /// </summary>
        /// <param name="go">The game object to retrieve information from.</param>
        /// <returns>A tuple containing the name, position, rotation, and size of the object.</returns>
        public (string name, PointMsg position, QuaternionMsg rotation, Vector3 size) GetObjectInfo(GameObject go) {
            // get renderer of the GameObject
            Renderer renderer = go.GetComponent<Renderer>();

            if (renderer != null) {
                string name = go.name;
                // Unity coordinates
                Vector3 unityPosition = go.transform.position;
                Quaternion unityRotation = go.transform.rotation;
                Vector3 size = renderer.bounds.size;

                // convert Unity coordinates into ROS coordinates (FLU)
                // TODO: This does not work as intended for some reason. For now the objects will be rotated by 90 Deg
                // on the ROS side
                PointMsg rosPosition = unityPosition.To<FLU>();
                QuaternionMsg rosRotation = unityRotation.To<FLU>();

                return (name, rosPosition, rosRotation, size);
            }
            // send empty message if no object renderer is found
            return (null, new PointMsg(), new QuaternionMsg(), Vector3.zero);
        }
    }
}