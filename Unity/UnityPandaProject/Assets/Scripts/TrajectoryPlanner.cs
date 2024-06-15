using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;
using System.Collections;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using UnityEngine.Assertions;

using Panda.Utility;

namespace Panda {
    public class TrajectoryPlanner : MonoBehaviour {
        // Linknames of the used robot (only up to the point where the tool is attached)
        public static readonly string[] LinkNames = {
            "world/panda_link0/panda_link1",
            "/panda_link2",
            "/panda_link3",
            "/panda_link4",
            "/panda_link5",
            "/panda_link6",
            "/panda_link7"
        };

        // Poses used by MoveIt
        enum Poses {
            PreGrasp,
            Grasp,
            PickUp,
            PrePlace,
            Place
        }

        [Tooltip("The ROS servicename, which will be subscribed to")]
        [SerializeField] string rosServiceName = "franka_panda_moveit";

        [Tooltip("The GameObject of the Franka Emika Panda")]
        [SerializeField] GameObject panda;

        [Tooltip("The GameObject of the target")]
        [SerializeField] GameObject target;

        [Tooltip("The GameObject of the goal")]
        [SerializeField] GameObject targetPlacement;

        [Tooltip("Selection of the correct TCP is important")]
        [SerializeField] GameObject pandaTCP;

        [Tooltip("How fast the robot will wait after moving all joints in the Simulation")]
        [SerializeField] float jointAssignmentWait = 0.15f;
        
        [Tooltip("How long the robot will wait until he moves to the next position")]
        [SerializeField] float poseAssignmentWait = 0.5f;
        
        [Tooltip("How close the robot will move to the target")]
        [SerializeField] float tolerance = 0.01f;

        [Tooltip("How high the robot will plan above the target/goal (in meters) to avoid collisions.")]
        [SerializeField] float upwardsOffset = 0.2f;

        [Tooltip("After what time the robot will replan, if the target didn't reach the goal")]
        [SerializeField] float timeout = 2.5f;

        [Tooltip("Enable or disable trajectory visualization")]
        [SerializeField] bool visualizeTrajectory = true;

        [Tooltip("Enable or disable attach on touch")]
        [SerializeField] bool attachOnTouch = false;

        // Event delegate type for trajectory received event
        public delegate void TrajectoryReceivedEventHandler(MoverServiceResponse response);
        // Event to be raised when trajectory is received
        public event TrajectoryReceivedEventHandler OnTrajectoryReceived;


        // Other members 
        // z - Value assures that the gripper is always positioned above the target cube before grasping
        // y - Value used to define place position
        readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
        private Vector3 pickPoseOffset = Vector3.up;
        private const float gripperOffset = 0.105f;
        private ArticulationBody[] jointArticulationBodies;
        private ArticulationBody leftGripper;
        private ArticulationBody rightGripper;
        private ROSConnection ros;

        /// <summary>
        ///     Find all robot joints in Awake() and add them to the jointArticulationBodies array.
        ///     Find left and right finger joints and assign them to their respective articulation body objects.
        /// </summary>
        void Start() {
            // Get ROS connection static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(rosServiceName);

            // set the pickPoseOfffset
            pickPoseOffset *= upwardsOffset;

            // Get Revolute Joints
            jointArticulationBodies = new ArticulationBody[LinkNames.Length];
            var linkName = string.Empty;
            for (var i = 0; i < LinkNames.Length; ++i) {
                // build link path
                linkName += LinkNames[i];
                // gets Joints (Joint 1 - 7, because Joint 0 and Joint 8 are fixed Joints)
                jointArticulationBodies[i] = panda.transform.Find(linkName).GetComponent<ArticulationBody>();
                // Throw Assertion when no Joints are found
                Assert.IsNotNull(jointArticulationBodies[i]);
            }

            // Find left and right fingers
            var leftGripper = linkName + "/panda_link8/panda_hand/panda_leftfinger";
            var rightGripper = linkName + "/panda_link8/panda_hand/panda_rightfinger";
            
            this.leftGripper = panda.transform.Find(leftGripper).GetComponent<ArticulationBody>();
            this.rightGripper = panda.transform.Find(rightGripper).GetComponent<ArticulationBody>();
        }

        /// <summary>
        ///     Get the current values of the robot's joint angles.
        /// </summary>
        /// <returns>PandaMoveitJointsMsg</returns>
        PandaMoveitJointsMsg CurrentJointConfig() {
            var joints = new PandaMoveitJointsMsg();

            for (var i = 0; i < LinkNames.Length; ++i) {
                joints.joints[i] = jointArticulationBodies[i].jointPosition[0];
            }
            return joints;
        }

        /// <summary>
        ///     Create a new MoverServiceRequest with the current values of the robot's joint angles,
        ///     the target cube's current position and rotation, and the targetPlacement position and rotation.
        ///     Call the MoverService using the ROSConnection and if a trajectory is successfully planned,
        ///     execute the trajectories in a coroutine.
        ///     Considers the roation of the target and which side of the target is easier to grab.
        /// </summary>
        public void PublishJoints() {
            // init request
            var request = new MoverServiceRequest();
            request.joints_input = CurrentJointConfig();

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

        /// <summary>
        /// Check if the returned response is valid, then use ExecuteTrajectories as coroutine to start moving.
        /// </summary>
        /// <param name="response"> MoverServiceResponse received from franka_panda_moveit mover service running in ROS</param>
        IEnumerator TrajectoryResponse(MoverServiceResponse response) {
            if (response.trajectories.Length > 0) {
                Debug.Log("Trajectory returned.");
                // Raise the trajectory received event
                OnTrajectoryReceived?.Invoke(response);

                // draw trajectories
                if (visualizeTrajectory) {
                    if (response != null) {
                        SplineDrawer.DrawTrajectories(response);
                    }
                }

                // execute trajectories
                yield return StartCoroutine(ExecuteTrajectories(response));
            }
            else {
                Debug.LogError("No trajectory returned from MoverService.");
            }
        }
        
        void TrajectoryResponseHandler(MoverServiceResponse response) {
            StartCoroutine(TrajectoryResponse(response));
        }

        /// <summary>
        ///     Execute the returned trajectories from the MoverService.
        ///     The expectation is that the MoverService will return four trajectory plans, PreGrasp, Grasp, PickUp, and
        ///     Place, where each plan is an array of robot poses. A robot pose is the joint angle values of the six robot
        ///     joints. Executing a single trajectory will iterate through every robot pose in the array while updating the
        ///     joint values on the robot.
        /// </summary>
        /// <param name="response"> MoverServiceResponse received from franka_panda_moveit mover service running in ROS</param>
        /// <returns></returns>
        IEnumerator ExecuteTrajectories(MoverServiceResponse response) {
            // send "new destination" to the attach script
            if (attachOnTouch) {
                AttachOnTouch.newDestination();
            }

            // Process the MoveIt response
            if (response.trajectories != null) {
                // For every trajectory plan returned
                for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                    // For every robot pose in trajectory plan
                    foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
                        var jointPositions = t.positions;
                        
                        // convert radiant to degree
                        var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

                        // Set the joint values for every joint
                        for (var joint = 0; joint < jointArticulationBodies.Length; ++joint) {
                            var joint1XDrive = jointArticulationBodies[joint].xDrive;
                            joint1XDrive.target = result[joint];
                            jointArticulationBodies[joint].xDrive = joint1XDrive;
                        }

                        // Wait for robot to achieve pose for all joint assignments
                        yield return new WaitForSeconds(jointAssignmentWait);
                    }

                    // Wait until the TCP is directly above m_Target
                    yield return StartCoroutine(WaitUntilPositionedOverTarget());

                    // wait before executing next pose
                    yield return new WaitForSeconds(poseAssignmentWait);

                    // handle event based on the pose
                    switch (poseIndex) {
                        case (int)Poses.PreGrasp:
                            // Open the gripper if completed executing the trajectory
                            OpenGripper();
                            break;
                        case (int)Poses.Grasp:
                            // Close the gripper if completed executing the trajectory
                            CloseGripper();
                            break;
                        case (int)Poses.PickUp:
                            // Add any specific actions for PickUp pose if needed
                            break;
                        case (int)Poses.PrePlace:
                            // Add any specific actions for PrePlace pose if needed
                            break;
                        case (int)Poses.Place:
                            // Open the gripper if completed executing the trajectory for the Place pose
                            OpenGripper();
                            // send "destination reached" to the attach script
                            if (attachOnTouch) {
                                AttachOnTouch.OnReachDestination();
                            }
                            break;
                        default:
                            // Optional: handle unexpected poseIndex values if necessary
                            break;
                    }      
                }
            }
        }

        /// <summary>
        ///     Waits until the gripper is exactly over the target. Continuously checks after each trajectory plan, if
        ///     the target is still close to the TCP. If so, break. If the target is not close, send a replan request.
        ///     If the target is near the goal, break.
        /// </summary>
        IEnumerator WaitUntilPositionedOverTarget() {
            float elapsedTime = 0.0f;

            while (true) {
                Vector3 pandaPosition = pandaTCP.transform.position;
                Vector3 targetPosition = target.transform.position;
                Vector3 targetPlacementPosition = targetPlacement.transform.position;

                // Check if the target is already at the goal, if so, break
                if (Mathf.Abs(targetPosition.x - targetPlacementPosition.x) < tolerance) {
                    if (Mathf.Abs(pandaPosition.z - targetPosition.z) < tolerance) {
                        // Exit the loop if the condition is met
                        yield return new WaitForSeconds(poseAssignmentWait);
                        yield break;
                    }
                }

                // Check if the TCP is directly above the target (using the defined tolerance), if so, break
                if (Mathf.Abs(pandaPosition.x - targetPosition.x) < tolerance) {
                    if (Mathf.Abs(pandaPosition.z - targetPosition.z) < tolerance) {
                        // Exit the loop if the condition is met
                        yield return new WaitForSeconds(poseAssignmentWait);
                        yield break;
                    }
                }

                // Increment elapsed time
                elapsedTime += Time.deltaTime;

                // Check if timeout is reached
                if (elapsedTime >= timeout) {
                    Debug.LogWarning("Timeout reached while waiting for positioning over target. Replanning.");

                    // TODO: stop execution of previous PublishJoints
                    //PublishJoints(); // Send a new planning request
                    //yield return new WaitForSeconds(poseAssignmentWait);
                    yield break;
                }

                // Wait for the next frame
                yield return null;
            }
        }

        /// <summary>
        ///     Close the gripper
        /// </summary>
        void CloseGripper() {
            var leftDrive = leftGripper.xDrive;
            var rightDrive = rightGripper.xDrive;

            leftDrive.target =  0f;
            rightDrive.target = 0f;

            leftGripper.xDrive = leftDrive;
            rightGripper.xDrive = rightDrive;
        }

        /// <summary>
        ///     Open the gripper
        /// </summary>
        void OpenGripper() {
            var leftDrive = leftGripper.xDrive;
            var rightDrive = rightGripper.xDrive;

            leftDrive.target = 0.04f;
            rightDrive.target = 0.04f;

            leftGripper.xDrive = leftDrive;
            rightGripper.xDrive = rightDrive;
        }
    }
}