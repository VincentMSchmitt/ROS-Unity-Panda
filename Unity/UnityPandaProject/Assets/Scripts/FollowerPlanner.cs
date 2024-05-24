using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.FrankaPandaMoveit;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using UnityEngine.Assertions;

public class DistanceTrajectoryPlanner : MonoBehaviour {
    public static readonly string[] LinkNames = {
        "world/panda_link0/panda_link1",
        "/panda_link2",
        "/panda_link3",
        "/panda_link4",
        "/panda_link5",
        "/panda_link6",
        "/panda_link7"};

    [SerializeField] string m_RosServiceName = "franka_panda_moveit";
    [SerializeField] GameObject m_FrankaPanda;
    [SerializeField] GameObject m_Target;
    [SerializeField] GameObject m_PandaTCP;
    [SerializeField] float JointAssignmentWait = 0.15f;
    [SerializeField] float PoseAssignmentWait = 0.5f;
    [SerializeField] float tolerance = 0.01f;
    [SerializeField] float timeout = 2.5f;
    [SerializeField] float desiredDistance = 0.1f;

    ROSConnection m_Ros;
    ArticulationBody[] m_JointArticulationBodies;
    LineRenderer lineRenderer;

    void Start() {
        m_Ros = ROSConnection.GetOrCreateInstance();

        // TODO: new service for only following
        m_Ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(m_RosServiceName);

        m_JointArticulationBodies = new ArticulationBody[LinkNames.Length];
        var linkName = string.Empty;
        for (var i = 0; i < LinkNames.Length; ++i) {
            linkName += LinkNames[i];
            m_JointArticulationBodies[i] = m_FrankaPanda.transform.Find(linkName).GetComponent<ArticulationBody>();
            Assert.IsNotNull(m_JointArticulationBodies[i]);
        }

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.positionCount = 0;
    }

    PandaMoveitJointsMsg CurrentJointConfig() {
        var joints = new PandaMoveitJointsMsg();
        for (var i = 0; i < LinkNames.Length; ++i) {
            joints.joints[i] = m_JointArticulationBodies[i].jointPosition[0];
        }
        return joints;
    }

    public void PublishJoints() {
        var request = new MoverServiceRequest();
        request.joints_input = CurrentJointConfig();

        Quaternion combinedRotation = Quaternion.Euler(m_Target.transform.rotation.eulerAngles.x, m_Target.transform.rotation.eulerAngles.y + 45, 180);

        request.pick_pose = new PoseMsg {
            position = (m_Target.transform.position + Vector3.up * 0.15f).To<FLU>(),
            orientation = combinedRotation.To<FLU>()
        };

        m_Ros.SendServiceMessage<MoverServiceResponse>(m_RosServiceName, request, TrajectoryResponse);
    }

    void TrajectoryResponse(MoverServiceResponse response) {
        if (response.trajectories.Length > 0) {
            StartCoroutine(ExecuteTrajectories(response));
        }
        else {
            Debug.LogError("No trajectory returned from MoverService.");
        }
    }

    IEnumerator ExecuteTrajectories(MoverServiceResponse response) {
        lineRenderer.positionCount = 0;

        if (response.trajectories != null) {
            for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
                foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
                    var jointPositions = t.positions;
                    var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
                    for (var joint = 0; joint < m_JointArticulationBodies.Length; ++joint) {
                        var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                    }
                    yield return new WaitForSeconds(JointAssignmentWait);
                }

                yield return StartCoroutine(WaitUntilPositionedOverTarget());
            }
        }
    }

    IEnumerator WaitUntilPositionedOverTarget() {
        float elapsedTime = 0.0f;

        while (true) {
            Vector3 pandaPosition = m_PandaTCP.transform.position;
            Vector3 targetPosition = m_Target.transform.position;

            float distanceToTarget = Vector3.Distance(pandaPosition, targetPosition);

            if (distanceToTarget <= desiredDistance) {
                yield return new WaitForSeconds(PoseAssignmentWait);
                yield break;
            }

            elapsedTime += Time.deltaTime;

            if (elapsedTime >= timeout) {
                Debug.LogWarning("Timeout reached while waiting for positioning over target. Replanning.");
                PublishJoints();
                yield return new WaitForSeconds(PoseAssignmentWait);
                yield break;
            }
            yield return null;
        }
    }
}