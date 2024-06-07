using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;

namespace Panda.Core.Ros {
    public class TrajectoryRequestFactory {
        // pick and place
        public static MoverServiceRequest CreatePickAndPlaceRequest(PandaMoveitJointsMsg currentJoints, PoseMsg pickPose, PoseMsg placePose, float offset) {
            return new MoverServiceRequest {
                joints_input = currentJoints,
                pick_pose = pickPose,
                place_pose = placePose,
                offset = new PandaMoveitOffsetMsg(offset)
            };
        }
        // follow
        public static FollowerServiceRequest CreateFollowerRequest(PandaMoveitJointsMsg currentJoints, PoseMsg targetPose, float offset) {
            return new FollowerServiceRequest {
                joints_input = currentJoints,
                target_pose = targetPose,
                offset = new PandaMoveitOffsetMsg(offset)
            };
        }
    }
}