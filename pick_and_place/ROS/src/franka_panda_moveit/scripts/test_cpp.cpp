#include <ros/ros.h>
#include <moveit/move_group_interface/move_group_interface.h>
#include <geometry_msgs/Pose.h>

#include <moveit/move_group_interface/move_group_interface.h>
#include <moveit/planning_scene_interface/planning_scene_interface.h>

#include <moveit_msgs/DisplayRobotState.h>
#include <moveit_msgs/DisplayTrajectory.h>

#include <moveit_msgs/AttachedCollisionObject.h>
#include <moveit_msgs/CollisionObject.h>

#include <moveit_visual_tools/moveit_visual_tools.h>

int main(int argc, char** argv) {
    ros::init(argc, argv, "test_example");
    ros::NodeHandle node_handleh;

    moveit::planning_interface::MoveGroupInterface move_group("panda_arm");

    // Set workspace boundaries (x_min, y_min, z_min, x_max, y_max, z_max)
    move_group.setWorkspace(-1.0, -1.0, 0.1,
                             1.0,  1.0, 2.0);

    ros::spin();
    return 0;
}