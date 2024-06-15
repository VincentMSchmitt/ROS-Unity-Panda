using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.UrdfImporter;
using UnityEngine;
using UnityEngine.Assertions;

namespace Panda.Utility {
    /// <summary>
    /// Class for publishing the joint positions of the Franka Emika Panda robot to a ROS topic. This is a modification
    /// of the original source destination publisher from Unity-Robotics-Hub:
    /// https://github.com/Unity-Technologies/Unity-Robotics-Hub/blob/main/tutorials/pick_and_place/Scripts/SourceDestinationPublisher.cs
    /// </summary>
    public class SourceDestinationPublisher : MonoBehaviour {
        /// <summary>
        /// Array containing the names of the links in the kinematic chain.
        /// </summary>
        public static readonly string[] LinkNames = {
            "world/panda_link0/panda_link1",
            "/panda_link2",
            "/panda_link3",
            "/panda_link4",
            "/panda_link5",
            "/panda_link6",
            "/panda_link7"};

        [Tooltip("The topic, that the destination will be published to.")]
        [SerializeField] string topicName = "/panda_joints";

        [Tooltip("The gameobject of the Franka Emika Panda robotic arm manipulator.")]
        [SerializeField] GameObject panda;

        [Tooltip("The gameobject of the target.")]
        [SerializeField] GameObject target;

        [Tooltip("The gameobject of the target placement zone.")]
        [SerializeField] GameObject targetPlacement;

        // z - Value assures that the gripper is always positioned above the target before grasping
        // y - Value used to define place position
        readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 90);
        UrdfJointRevolute[] jointArticulationBodies;
        ROSConnection ros;

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first
        /// time. Initializes the ROS connection and sets up the joint articulation bodies.
        /// </summary>
        void Start() {
            // get ros connection static instance
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<PandaMoveitJointsMsg>(topicName);
            
            // get revolute joints
            jointArticulationBodies = new UrdfJointRevolute[LinkNames.Length];
            var linkName = string.Empty;
            for (var i = 0; i < LinkNames.Length; ++i) {
                // build link path
                linkName += LinkNames[i];
                //Debug.Log("CurrentLinkPath: " + linkName);

                // gets Joints (Joint 1 - 7, because Joint 0 and Joint 8 are fixed Joints)
                jointArticulationBodies[i] = panda.transform.Find(linkName).GetComponent<UrdfJointRevolute>();

                // catch unwanted behaviour
                Assert.IsNotNull(jointArticulationBodies[i]);
            }
        }

        /// <summary>
        /// Publishes the current joint positions and target poses to the ROS topic.
        /// </summary>
        public void Publish() {
            Debug.Log("Sending new PandaMovitJoint-Message...");

            var sourceDestinationMessage = new PandaMoveitJointsMsg {
                joints = new double[LinkNames.Length]
            };

            for (var i = 0; i < LinkNames.Length; ++i) {
                sourceDestinationMessage.joints[i] = jointArticulationBodies[i].GetPosition();
            }

            // Pick Pose
            sourceDestinationMessage.pick_pose = new PoseMsg {
                position = target.transform.position.To<FLU>(),
                orientation = Quaternion.Euler(90, target.transform.eulerAngles.y, 0).To<FLU>()
            };

            // Place Pose
            sourceDestinationMessage.place_pose = new PoseMsg {
                position = targetPlacement.transform.position.To<FLU>(),
                orientation = pickOrientation.To<FLU>()
            };

            // Finally send the message to server_endpoint.py running in ROS
            ros.Publish(topicName, sourceDestinationMessage);
            //Debug.Log("Published Message on " + m_TopicName + ":\n" + sourceDestinationMessage.ToString());
        }
    }
}