using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;
using System;

public class JointStatePublisher : MonoBehaviour {
    [SerializeField] GameObject panda;
    private string topicName = "/joint_states";
    private ROSConnection ros;
    private List<ArticulationBody> articulationChain;
    private ulong seq;

    private static readonly string[] linkNames = {
            "world/panda_link0/panda_link1",
            "/panda_link2",
            "/panda_link3",
            "/panda_link4",
            "/panda_link5",
            "/panda_link6",
            "/panda_link7"
    };

    private static readonly string[] jointNames = {
            "panda_joint1",
            "panda_joint2",
            "panda_joint3",
            "panda_joint4",
            "panda_joint5",
            "panda_joint6",
            "panda_joint7",
            "panda_finger_joint1",
            "panda_finger_joint2"
    };

    void Start() {
        seq = 0;
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<JointStateMsg>(topicName);

        articulationChain = new List<ArticulationBody>();

        // Get Revolute Joint
        var linkName = string.Empty;
        for (var i = 0; i < linkNames.Length; ++i) {
            // build link path
            linkName += linkNames[i];
            // gets Joints (Joint 1 - 7, because Joint 0 and Joint 8 are fixed Joints)
            ArticulationBody joint = panda.transform.Find(linkName).GetComponent<ArticulationBody>();
            if (joint != null) {
                articulationChain.Add(joint);
            } else {
                Debug.LogError($"Could not find ArticulationBody at {linkName}");
            }
        }

        // Find left and right fingers
        var leftGripper = linkName + "/panda_link8/panda_hand/panda_leftfinger";
        var rightGripper = linkName + "/panda_link8/panda_hand/panda_rightfinger";

        ArticulationBody leftFingerJoint = panda.transform.Find(leftGripper).GetComponent<ArticulationBody>();
        ArticulationBody rightFingerJoint = panda.transform.Find(rightGripper).GetComponent<ArticulationBody>();

        if (leftFingerJoint != null) {
            articulationChain.Add(leftFingerJoint);
        } else {
            Debug.LogError($"Could not find ArticulationBody at {leftGripper}");
        }

        if (rightFingerJoint != null) {
            articulationChain.Add(rightFingerJoint);
        } else {
            Debug.LogError($"Could not find ArticulationBody at {rightGripper}");
        }
    }

    void Update() {
        JointStateMsg jointStateMessage = new JointStateMsg();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        uint seconds = (uint)now.ToUnixTimeSeconds();
        uint nanoseconds = (uint)(now.ToUnixTimeMilliseconds() % 1000 * 1000000);

        jointStateMessage.header = new RosMessageTypes.Std.HeaderMsg {
            seq = (uint)seq,
            stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg {
                sec = seconds,
                nanosec = nanoseconds
            },
            frame_id = "world"
        };

        jointStateMessage.name = new string[articulationChain.Count];
        jointStateMessage.position = new double[articulationChain.Count];

        for (int i = 0; i < articulationChain.Count; ++i) {
            jointStateMessage.name[i] = jointNames[i];
            if (articulationChain[i].jointType == ArticulationJointType.RevoluteJoint) {
                jointStateMessage.position[i] = articulationChain[i].jointPosition[0];
            }
            else if (articulationChain[i].jointType == ArticulationJointType.PrismaticJoint) {
                jointStateMessage.position[i] = articulationChain[i].jointPosition[0];
            }
            else {
                Debug.LogError("Tried to send unsupported joint type. Make sure the robot only uses revolute or prismatic joints");
            }
        }
        ++seq;
        ros.Publish(topicName, jointStateMessage);
    }
}