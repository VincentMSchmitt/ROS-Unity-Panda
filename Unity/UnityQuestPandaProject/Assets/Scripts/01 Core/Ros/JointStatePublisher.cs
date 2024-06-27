/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using System.Collections.Generic;
using System;
using Panda.Core.Controller;

public class JointStatePublisher : MonoBehaviour {
    [SerializeField] GameObject panda;
    private string topicName = "/joint_states";
    private ROSConnection ros;
    private ulong seq;

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

        // get acticulation chain
        List<ArticulationBody> articulationList = RobotController.GetInstance.articulationList;
        if (articulationList == null) {
            Debug.Log("Articulation chain dosent exist.");
            return;
        }

        jointStateMessage.name = new string[articulationList.Count];
        jointStateMessage.position = new double[articulationList.Count];

        for (int i = 0; i < articulationList.Count; ++i) {
            jointStateMessage.name[i] = jointNames[i];
            if (articulationList[i].jointType == ArticulationJointType.RevoluteJoint) {
                jointStateMessage.position[i] = articulationList[i].jointPosition[0];
            }
            else if (articulationList[i].jointType == ArticulationJointType.PrismaticJoint) {
                jointStateMessage.position[i] = articulationList[i].jointPosition[0];
            }
            else {
                Debug.LogError("Tried to send unsupported joint type. Make sure the robot only uses revolute or prismatic joints");
            }
        }
        ++seq;
        ros.Publish(topicName, jointStateMessage);
    }
}