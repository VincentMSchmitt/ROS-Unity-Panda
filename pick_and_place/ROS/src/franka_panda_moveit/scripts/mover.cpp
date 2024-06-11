#include <ros/ros.h>
#include <moveit/move_group_interface/move_group_interface.h>
#include <moveit_msgs/RobotTrajectory.h>
#include <moveit_msgs/Constraints.h>
#include <moveit_msgs/MoveItErrorCodes.h>
#include <geometry_msgs/Pose.h>
#include <std_srvs/Empty.h>
#include <franka_panda_moveit/MoverService.h>

std::vector<std::string> joint_names = {"panda_joint1", "panda_joint2", "panda_joint3", "panda_joint4",
                                        "panda_joint5", "panda_joint6", "panda_joint7"};

moveit::planning_interface::MoveGroupInterface::Plan plan_trajectory(moveit::planning_interface::MoveGroupInterface& move_group,
                                                                        const geometry_msgs::Pose& destination_pose,
                                                                        const boost::array<double,
                                                                        7UL>& start_joint_angles) {
    sensor_msgs::JointState current_joint_state;
    current_joint_state.name = joint_names;
    current_joint_state.position = start_joint_angles;

    moveit_msgs::RobotState moveit_robot_state;
    moveit_robot_state.joint_state = current_joint_state;
    move_group.setStartState(moveit_robot_state);

    move_group.setPoseTarget(destination_pose);
    moveit::planning_interface::MoveGroupInterface::Plan plan;
    bool success = (move_group.plan(plan) == moveit::planning_interface::MoveItErrorCode::SUCCESS);

    if (!success) {
        std::stringstream ss;
        ss << "Trajectory could not be planned for a destination of " << destination_pose;
        ss << "\nPlease make sure target and destination are reachable by the robot." << ss.end;
        throw std::runtime_error(ss.str());
    }

    return plan;
}

bool plan_pick_and_place(franka_panda_moveit::MoverService::Request &reqest,
                        franka_panda_moveit::MoverService::Response &response) {
    moveit::planning_interface::MoveGroupInterface move_group("panda_arm");

    auto current_robot_joint_configuration = reqest.joints_input.joints;

    auto pre_grasp_pose = plan_trajectory(move_group, reqest.pick_pose, current_robot_joint_configuration);
    if (pre_grasp_pose.trajectory_.joint_trajectory.points.empty()) {
        ROS_WARN("Pre grasp pose planning failed.");
        return false;
    }
    auto previous_ending_joint_angles = pre_grasp_pose.trajectory_.joint_trajectory.points.back().positions;

    geometry_msgs::Pose pick_pose = reqest.pick_pose;
    pick_pose.position.z -= reqest.offset.offset;
    auto grasp_pose = plan_trajectory(move_group, pick_pose, previous_ending_joint_angles);
    if (grasp_pose.trajectory_.joint_trajectory.points.empty()) {
        ROS_WARN("Grasp pose planning failed.");
        return false;
    }
    previous_ending_joint_angles = grasp_pose.trajectory_.joint_trajectory.points.back().positions;

    auto pick_up_pose = plan_trajectory(move_group, reqest.pick_pose, previous_ending_joint_angles);
    if (pick_up_pose.trajectory_.joint_trajectory.points.empty()) {
        ROS_WARN("Pick up pose planning failed.");
        return false;
    }
    previous_ending_joint_angles = pick_up_pose.trajectory_.joint_trajectory.points.back().positions;

    auto pre_place_pose = plan_trajectory(move_group, reqest.place_pose, previous_ending_joint_angles);
    if (pre_place_pose.trajectory_.joint_trajectory.points.empty()) {
        ROS_WARN("Pre place pose planning failed.");
        return false;
    }
    previous_ending_joint_angles = pre_place_pose.trajectory_.joint_trajectory.points.back().positions;

    geometry_msgs::Pose place_pose = reqest.place_pose;
    place_pose.position.z -= reqest.offset.offset - 0.015;
    auto place_trajectory = plan_trajectory(move_group, place_pose, previous_ending_joint_angles);
    if (place_trajectory.trajectory_.joint_trajectory.points.empty()) {
        ROS_WARN("Place pose planning failed.");
        return false;
    }

    response.trajectories.push_back(pre_grasp_pose.trajectory_);
    response.trajectories.push_back(grasp_pose.trajectory_);
    response.trajectories.push_back(pick_up_pose.trajectory_);
    response.trajectories.push_back(pre_place_pose.trajectory_);
    response.trajectories.push_back(place_trajectory.trajectory_);

    move_group.clearPoseTargets();
    return true;
}

int main(int argc, char** argv) {
    ros::init(argc, argv, "franka_panda_moveit_server");
    ros::NodeHandle nh;

    ros::ServiceServer service = nh.advertiseService("franka_panda_moveit", plan_pick_and_place);
    ROS_INFO("Ready to plan");
    ros::spin();

    return 0;
}