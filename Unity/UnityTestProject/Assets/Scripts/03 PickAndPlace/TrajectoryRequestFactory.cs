using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;

namespace Panda.PickAndPlace {
    public class TrajectoryRequestFactory {
        public static MoverServiceRequest CreateRequest(PandaMoveitJointsMsg currentJoints, PoseMsg pickPose, PoseMsg placePose, float offset) {
            return new MoverServiceRequest {
                joints_input = currentJoints,
                pick_pose = pickPose,
                place_pose = placePose,
                offset = new PandaMoveitOffsetMsg(offset)
            };
        }
    }
}