// using RosMessageTypes.FrankaPandaMoveit;
// using RosMessageTypes.Geometry;
// using System.Collections;
// using System.Linq;
// using Unity.Robotics.ROSTCPConnector;
// using Unity.Robotics.ROSTCPConnector.ROSGeometry;
// using UnityEngine;
// using UnityEngine.Assertions;

// using Panda.Core;
// using Panda.Core.Ros;

// namespace Panda.PickAndPlace {
//     enum Poses { PreGrasp, Grasp, PickUp, PrePlace, Place }
//     public class TrajectoryPlanner : MonoBehaviour {
//         private static readonly string[] linkNames = {
//             "world/panda_link0/panda_link1",
//             "/panda_link2",
//             "/panda_link3",
//             "/panda_link4",
//             "/panda_link5",
//             "/panda_link6",
//             "/panda_link7"
//         };
//         public string rosServiceName = "franka_panda_moveit";
//         [SerializeField] GameObject panda;
//         [SerializeField] GameObject target;
//         [SerializeField] GameObject targetPlacement;
//         [SerializeField] GameObject pandaTCP;
//         public float jointAssignmentWait = 0.15f;
//         public float poseAssignmentWait = 0.5f;
//         public float tolerance = 0.01f;
//         public float upwardsOffset = 0.2f;
//         public float timeout = 2.5f;
//         public bool attachOnTouch = false;

//         private readonly Quaternion pickOrientation = Quaternion.Euler(0, 45, 180);
//         private Vector3 pickPoseOffset => Vector3.up * upwardsOffset;
//         private const float gripperOffset = 0.105f;
//         private ArticulationBody[] jointArticulationBodies;
//         private ArticulationBody leftGripper;
//         private ArticulationBody rightGripper;
//         private ROSConnection ros;
//         private IGripperStrategy gripperStrategy;

//         public void PublishJoints() {
//             var request = TrajectoryRequestFactory.CreatePickAndPlaceRequest(
//                 CurrentJointState(),
//                 new PoseMsg {
//                     position = (target.transform.position + pickPoseOffset).To<FLU>(),
//                     orientation = Quaternion.Euler(pickOrientation.eulerAngles.x, target.transform.rotation.eulerAngles.y + 45, pickOrientation.eulerAngles.z).To<FLU>()
//                 },
//                 new PoseMsg {
//                     position = (targetPlacement.transform.position + pickPoseOffset).To<FLU>(),
//                     orientation = pickOrientation.To<FLU>()
//                 },
//                 pickPoseOffset.y - gripperOffset
//             );

//             ros.SendServiceMessage<MoverServiceResponse>(rosServiceName, request, TrajectoryResponseHandler);
//         }

//         private void Start() {
//             // Create ROS connection singelton static instance
//             ros = ROSConnection.GetOrCreateInstance();
//             ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(rosServiceName);

//             // get all articulation bodies in "arm" group
//             jointArticulationBodies = new ArticulationBody[linkNames.Length];
//             var linkName = string.Empty;
//             for (var i = 0; i < linkNames.Length; ++i) {
//                 linkName += linkNames[i];
//                 jointArticulationBodies[i] = panda.transform.Find(linkName).GetComponent<ArticulationBody>();
//                 Assert.IsNotNull(jointArticulationBodies[i]);
//             }

//             // get all articulation bodies in "hand" group
//             var leftGripperString = linkName + "/panda_link8/panda_hand/panda_leftfinger";
//             var rightGripperString = linkName + "/panda_link8/panda_hand/panda_rightfinger";

//             leftGripper = panda.transform.Find(leftGripperString).GetComponent<ArticulationBody>();
//             rightGripper = panda.transform.Find(rightGripperString).GetComponent<ArticulationBody>();
//         }

//         private PandaMoveitJointsMsg CurrentJointState() {
//             var joints = new PandaMoveitJointsMsg();
//             for (var i = 0; i < linkNames.Length; ++i) {
//                 joints.joints[i] = jointArticulationBodies[i].jointPosition[0];
//             }
//             return joints;
//         }

//         private void TrajectoryResponseHandler(MoverServiceResponse response) {
//             StartCoroutine(TrajectoryResponse(response));
//         }

//         private IEnumerator TrajectoryResponse(MoverServiceResponse response) {
//             if (response.trajectories.Length > 0) {
//                 yield return StartCoroutine(ExecuteTrajectories(response));
//             }
//             else {
//                 Debug.LogError("No trajectory returned from MoverService.");
//             }
//         }

//         private void SetGripperStrategy(IGripperStrategy strategy) {
//             gripperStrategy = strategy;
//         }

//         private IEnumerator ExecuteTrajectories(MoverServiceResponse response) {
//             if (attachOnTouch) {
//                 AttachOnTouch.NewDestination();
//             }

//             if (response.trajectories != null) {
//                 for (var poseIndex = 0; poseIndex < response.trajectories.Length; ++poseIndex) {
//                     foreach (var t in response.trajectories[poseIndex].joint_trajectory.points) {
//                         var jointPositions = t.positions;
//                         var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

//                         // TODO: clean up interface to prevent this
//                         var moveCommand = new MoveCommand(jointArticulationBodies, result) as ICommand;
//                         moveCommand.Execute();

//                         yield return new WaitForSeconds(jointAssignmentWait);
//                     }

//                     yield return new WaitForSeconds(poseAssignmentWait);
                    
//                     switch (poseIndex) {
//                         case (int)Poses.PreGrasp:
//                             SetGripperStrategy(new OpenGripperStrategy());
//                             gripperStrategy.Execute(leftGripper, rightGripper);
//                             break;
//                         case (int)Poses.Grasp:
//                             yield return new WaitForSeconds(0.5f * poseAssignmentWait);
//                             SetGripperStrategy(new CloseGripperStrategy());
//                             gripperStrategy.Execute(leftGripper, rightGripper);
//                             //yield return new WaitForSeconds(0.5f * poseAssignmentWait);
//                             break;
//                         case (int)Poses.PickUp:
//                             break;
//                         case (int)Poses.PrePlace:
//                             yield return StartCoroutine(WaitUntilPositionedOverTarget());
//                             break;
//                         case (int)Poses.Place:
//                             SetGripperStrategy(new OpenGripperStrategy());
//                             gripperStrategy.Execute(leftGripper, rightGripper);
//                             if (attachOnTouch) {
//                                 AttachOnTouch.OnReachDestination();
//                             }
//                             break;
//                         default:
//                             break;
//                     }
//                 }
//             }
//         }

//         private IEnumerator WaitUntilPositionedOverTarget() {
//             float elapsedTime = 0.0f;
//             while (true) {
//                 Vector3 pandaPosition = pandaTCP.transform.position;
//                 Vector3 targetPosition = target.transform.position;
//                 Vector3 targetPlacementPosition = targetPlacement.transform.position;

//                 if (Mathf.Abs(targetPosition.x - targetPlacementPosition.x) < tolerance && Mathf.Abs(pandaPosition.z - targetPosition.z) < tolerance) {
//                     yield return new WaitForSeconds(poseAssignmentWait);
//                     yield break;
//                 }

//                 if (Mathf.Abs(pandaPosition.x - targetPosition.x) < tolerance && Mathf.Abs(pandaPosition.z - targetPosition.z) < tolerance) {
//                     yield return new WaitForSeconds(poseAssignmentWait);
//                     yield break;
//                 }

//                 elapsedTime += Time.deltaTime;
//                 if (elapsedTime >= timeout) {
//                     Debug.LogWarning("Timeout reached while waiting for positioning over target. Replanning.");
//                     // TODO: stop execution of previous PublishJoints
//                     PublishJoints(); // Send a new planning request
//                     yield return new WaitForSeconds(poseAssignmentWait);
//                     yield break;
//                 }
//                 yield return null;
//             }
//         }
//     }
// }