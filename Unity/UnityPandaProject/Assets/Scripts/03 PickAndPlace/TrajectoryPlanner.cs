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

namespace Panda.PickAndPlace {
    enum Poses { PreGrasp, Grasp, PickUp, PrePlace, Place }
    /// <summary>
    /// Plans and executes an pick and place task, based on the specified target and targetPlacemtn using ROS and Unity
    /// components.
    /// </summary>
    public class TrajectoryPlanner : MonoBehaviour {
        [Tooltip("The ROS servicename, which will be subscribed to.")] public string rosServiceName = "franka_panda_moveit";
        [Tooltip("The GameObject of the target.")] [SerializeField] GameObject target;
        [Tooltip("The GameObject of the goal")] [SerializeField] GameObject targetPlacement;
        [Tooltip("Percentage of the max speed.")] public float speed = 0.1f;
        [Tooltip("How long the robot will wait until he moves to the next position.")] public float poseAssignmentWait = 1f;
        [Tooltip("How high the robot will plan above the target/goal (in meters) to avoid collisions.")] public float upwardsOffset = 0.2f;
        private const float gripperOffset = 0.105f; // panda specific
        private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private Vector3 pickPoseOffset => Vector3.up * upwardsOffset;
        private ROSConnection ros;

        /// <summary>
        /// Called in the first frame of the game. Initializes the pick and place by setting up the ROS connection and
        /// getting the necessary components.
        /// </summary>
        void Start() {
            // make sure, that the speed percentage is valid
            speed = Mathf.Clamp01(speed);

            // Create ROS connection singelton static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(rosServiceName);
        }

        /// <summary>
        ///     Create a new MoverServiceRequest with the current values of the robot's joint angles, the target cubes
        ///     current position and rotation, and the targetPlacement position and rotation. Call the MoverService
        ///     using the ROSConnection and if a trajectory is successfully planned, execute the trajectories in a
        ///     coroutine. Considers the roation of the target and which side of the target is easier to grab.
        /// </summary>
        public void SendPlanRequest() {
            //---  init request ---
            var request = new MoverServiceRequest();
            request.joints_input = CurrentJointState();
            
            // --- make sure the orientation of the gripper is always correct while gripping ---
            var objectInfo = GetObjectInfo(target);
            // Check if either dimension is okay:
            if (objectInfo.size.x > 0.075f && objectInfo.size.z > 0.075f) {
                Debug.LogError("Target is too big for the grippers.");
            }
            // Check which dimension is smaller
            Quaternion additionalRotation = Quaternion.Euler(0, 0, 0);
            // x is too big and z is okay
            if (objectInfo.size.x > 0.075f && objectInfo.size.z <= 0.075f) {
                // rotate 90 deg around y
                additionalRotation = Quaternion.Euler(0, 90, 0);
                Debug.Log("Target is too large in the x dimension, rotating to grip along the z dimension.");
            }
            // z is too big and x is okay
            else if (objectInfo.size.z > 0.075f && objectInfo.size.x <= 0.075f) {
                // dont change anything
                Debug.Log("Target is too large in the z dimension, rotating to grip along the x dimension.");
            }
            else {
                Debug.Log("Both Dimensions are fine. Using smaller dimension.");
                // if x dimension is bigger, if z is bigger dont do anything
                if (objectInfo.size.x >= objectInfo.size.z) {
                    // rotate 90 deg around y
                    additionalRotation = Quaternion.Euler(0, 90, 0);
                }
            }  

            // --- combine the rotations ---
            // y from the target - x, z from the m_PickOrientation --> Robot always places in the same Orientation
            // check https://quaternions.online for visulization of these Quaternions
            Quaternion combinedRotation
                = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z) * additionalRotation;

            // --- build the massage ---
            // TARGET
            request.target = new ObjectInfoMsg {
                name = objectInfo.name,
                position = objectInfo.position,
                rotation = objectInfo.rotation,
                size = new Vector3Msg(objectInfo.size.x, objectInfo.size.y, objectInfo.size.z)
            };
            // PICK POSE
            request.pick_pose = new PoseMsg {
                position = (target.transform.position + pickPoseOffset).To<FLU>(),
                orientation = combinedRotation.To<FLU>() // use combined rotation
            };
            // PLACE POSE
            request.place_pose = new PoseMsg {
                position = (targetPlacement.transform.position + pickPoseOffset).To<FLU>(),
                orientation = pickOrientation.To<FLU>() // use predefined rotation to asure target faces the camera
            };
            // OFFSET
            // send selected offset in y dimension (up) with the request
            request.offset = new PandaMoveitOffsetMsg(pickPoseOffset.y - gripperOffset);

            // --- send request and evaluate response ---
            ros.SendServiceMessage<MoverServiceResponse>(rosServiceName, request, TrajectoryResponseHandler);
        }

        /// <summary>
        /// Retrieves the information of a single object, including its position, rotation, and size.
        /// </summary>
        /// <param name="go">The game object to retrieve information from.</param>
        /// <returns>A tuple containing the name, position, rotation, and size of the object.</returns>
        private (string name, PointMsg position, QuaternionMsg rotation, Vector3 size) GetObjectInfo(GameObject go) {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) {
                string name = go.name;
                Vector3 unityPosition = go.transform.position;
                Quaternion unityRotation = go.transform.rotation;

                // Renderer.bounds returns the Axis-Aligned Bounding Box (AABB) - Boundaries in global space that take into
                // account the current position, rotation and scaling of the GameObject
                // Renderer.localBounds returns the Oriented Bounding Box (OBB) - Local limits in the GameObject's own space,
                // independent of global position, rotation and scaling
                // since the localBounds are independant of the scale, we need to apply the scale manually
                Vector3 size = Vector3.Scale(target.GetComponent<Renderer>().localBounds.size, target.transform.localScale);

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

        /// <summary>
        ///     Get the current values of the robot's joint angles.
        /// </summary>
        /// <returns>PandaMoveitJointsMsg</returns>
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

        /// <summary>
        /// Check if the returned response is valid, then use ExecuteTrajectories as coroutine to start moving.
        /// </summary>
        /// <param name="response"> MoverServiceResponse received from franka_panda_moveit service running in ROS</param>
        private IEnumerator TrajectoryResponse(MoverServiceResponse response) {
            if (response.trajectories.Length > 0) {
                yield return StartCoroutine(ExecuteTrajectories(response));
            }
            else {
                Debug.LogError("No trajectory returned from MoverService.");
            }
        }

        private IEnumerator WaitForAllCoroutines(List<Coroutine> coroutines) {
            foreach (var coroutine in coroutines) {
                yield return coroutine;
            }
        }

        /// <summary>
        ///     Execute the returned trajectories from the MoverService. The expectation is that the MoverService will 
        ///     return five trajectory plans, PreGrasp, Grasp, PickUp, PrePlace and Place, where each plan is an array of
        ///     robot poses. A robot pose is the joint angle values of the seven robot joints. Executing a single
        ///     trajectory will iterate through every robot pose in the array while updating the joint values on the robot.
        /// </summary>
        /// <param name="response"> MoverServiceResponse received from franka_panda_moveit service running in ROS</param>
        /// <returns></returns>
        private IEnumerator ExecuteTrajectories(MoverServiceResponse response) {
            List<IMoveCommand> joints = RobotController.GetInstance.GetRevoluteJoints();
            IMoveCommand gripper = RobotController.GetInstance.GetGripper();
            if (response.trajectories != null) {
                // for every pose (pre pick, pickup, etc.)
                for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                    // for every trajectory in that pose
                    foreach (var point in response.trajectories[poseIndex].joint_trajectory.points) {
                        // couroutines for joints movements
                        List<Coroutine> jointCoroutines = new List<Coroutine>();
                        // convert radiant to degree
                        var jointPositions = point.positions;
                        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
                        // for every joint
                        for (int i = 0; i < jointPositions.Length; ++i) {
                            jointCoroutines.Add(StartCoroutine(joints[i].MoveToTarget(result[i], speed)));
                        }
                        // wait, until every joint is where it is supposed to be
                        yield return StartCoroutine(WaitForAllCoroutines(jointCoroutines));
                    }

                    // wait before executing next pose
                    yield return new WaitForSeconds(poseAssignmentWait);

                    // handle event, based on the pose
                    switch (poseIndex) {
                        case (int)Poses.PreGrasp:
                            // Open the gripper at the start
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
                            break;
                    }
                }
            }
        }
    }
}