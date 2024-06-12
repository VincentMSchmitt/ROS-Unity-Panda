#include <ros/ros.h>
#include <moveit/move_group_interface/move_group_interface.h>
#include <moveit/planning_scene_interface/planning_scene_interface.h>

#include <geometry_msgs/Pose.h>
#include <moveit_msgs/DisplayRobotState.h>
#include <moveit_msgs/DisplayTrajectory.h>
#include <moveit_msgs/AttachedCollisionObject.h>
#include <moveit_msgs/CollisionObject.h>
#include <moveit_msgs/Constraints.h>
#include <moveit_msgs/OrientationConstraint.h>

#include <moveit_visual_tools/moveit_visual_tools.h>

// The circle constant tau = 2*pi. One tau is one rotation in radians.
const double tau = 2 * M_PI;

// planning groups
static const std::string PLANNING_GROUP_ARM = "panda_arm";
static const std::string PLANNING_GROUP_HAND = "panda_hand";

int main(int argc, char** argv) {
    // ----------------------------------------------------------------------------------------------------------------
    // Setup
    // ^^^^^
    ros::init(argc, argv, "test_example");
    ros::NodeHandle node_handle;

    // ROS spinning must be running for the MoveGroupInterface to get information about the robot's state
    ros::AsyncSpinner spinner(1);
    spinner.start();

    // setup the planning groups
    moveit::planning_interface::MoveGroupInterface move_group_interface_arm(PLANNING_GROUP_ARM);

    // FOr adding and removing collision objects to the "virtual world" scene
    moveit::planning_interface::PlanningSceneInterface planning_scene_interface;

    // Raw pointers are frequently used to refer to the planning group for improved performance.
    const moveit::core::JointModelGroup* joint_model_group_arm =
        move_group_interface_arm.getCurrentState()->getJointModelGroup(PLANNING_GROUP_ARM);

    // Set workspace boundaries (x_min, y_min, z_min, x_max, y_max, z_max)
    move_group_interface_arm.setWorkspace(-1.0, -1.0, 0.01, 1.0,  1.0, 2.0);

    // ----------------------------------------------------------------------------------------------------------------
    // Visualization
    // ^^^^^^^^^^^^^
    moveit_visual_tools::MoveItVisualTools visual_tools("panda_link0"); // pass in the base frame
    visual_tools.deleteAllMarkers();

    // Remote control is an introspection tool that allows users to step through a high level script via buttons and
    // keyboard shortcuts in RViz
    visual_tools.loadRemoteControl();

    // Batch publishing is used to reduce the number of messages being sent to RViz for large visualizations
    visual_tools.trigger();

    // ----------------------------------------------------------------------------------------------------------------
    // Start the demo
    // ^^^^^^^^^^^^^^^^^^^^^^^^^
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window to start the demo");

    // .. _move_group_interface-planning-to-pose-goal:
    //
    // Planning to a Pose goal
    // ^^^^^^^^^^^^^^^^^^^^^^^
    // We can plan a motion for this group to a desired pose for the
    // end-effector.
    geometry_msgs::Pose target_pose1;
    target_pose1.position.x = 0.28;
    target_pose1.position.y = -0.2;
    target_pose1.position.z = 0.5;
    target_pose1.orientation.y = 1.0;
    move_group_interface_arm.setPoseTarget(target_pose1);

    moveit::planning_interface::MoveGroupInterface::Plan plan;
    bool success = (move_group_interface_arm.plan(plan) == moveit::core::MoveItErrorCode::SUCCESS);
    ROS_INFO_NAMED("test_example", "Visualizing plan 1 (pose goal) %s", success ? "SUCCESS" : "FAILED");
    visual_tools.publishTrajectoryLine(plan.trajectory_, joint_model_group_arm);
    visual_tools.trigger();
    move_group_interface_arm.clearPoseTargets();
    visual_tools.prompt("Press 'next' to execute the path");
    move_group_interface_arm.execute(plan);
    visual_tools.prompt("Press 'next' to plan the next path (no obstacles)");

    // ----------------------------------------------------------------------------------------------------------------
    // Simple plan
    // plan a simple goal with no objects in the way
    visual_tools.deleteAllMarkers();
    move_group_interface_arm.setStartState(*move_group_interface_arm.getCurrentState());
    geometry_msgs::Pose another_pose;
    another_pose.position.x = 0.7;
    another_pose.position.y = 0.0;
    another_pose.position.z = 0.59;
    another_pose.orientation.y = 1.0;
    move_group_interface_arm.setPoseTarget(another_pose);

    success = (move_group_interface_arm.plan(plan) == moveit::core::MoveItErrorCode::SUCCESS);
    ROS_INFO_NAMED("test_example", "Visualizing plan 2 (with no obstacles) %s", success ? "SUCCESS" : "FAILED");

    visual_tools.publishTrajectoryLine(plan.trajectory_, joint_model_group_arm);
    visual_tools.trigger();
    visual_tools.prompt("Press 'next' to add a box to the scene");

    // ----------------------------------------------------------------------------------------------------------------
    // Add a box to the scene
    // ^^^^^^^^^^^^^^^^^^^^^^
    // Define a collision object ROS message for the robot to avoid.
    moveit_msgs::CollisionObject collision_object;
    collision_object.header.frame_id = move_group_interface_arm.getPlanningFrame();

    // The id of the object is used to identify it.
    collision_object.id = "box1";

    // Define a box to add to the world.
    shape_msgs::SolidPrimitive primitive;
    primitive.type = primitive.BOX;
    primitive.dimensions.resize(3);
    primitive.dimensions[primitive.BOX_X] = 0.1;
    primitive.dimensions[primitive.BOX_Y] = 1.5;
    primitive.dimensions[primitive.BOX_Z] = 0.5;

    // Define a pose for the box (specified relative to frame_id)
    geometry_msgs::Pose box_pose;
    box_pose.orientation.w = 1.0;
    box_pose.position.x = 0.5;
    box_pose.position.y = 0.0;
    box_pose.position.z = 0.25;

    collision_object.primitives.push_back(primitive);
    collision_object.primitive_poses.push_back(box_pose);
    collision_object.operation = collision_object.ADD;

    std::vector<moveit_msgs::CollisionObject> collision_objects;
    collision_objects.push_back(collision_object);

    // Now, let's add the collision object into the world (using a vector that could contain additional objects)
    ROS_INFO_NAMED("test_example", "Add an object into the world");
    planning_scene_interface.addCollisionObjects(collision_objects);
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window, once the collision object appears in RViz to plan the next path (obstacles)");

    // ----------------------------------------------------------------------------------------------------------------
    // plan with collision object
    // ^^^^^^^^^^^^^^^^^^^^^^^^^^
    // Plan a trajectory which will avoid the obstacle
    success = (move_group_interface_arm.plan(plan) == moveit::core::MoveItErrorCode::SUCCESS);
    ROS_INFO_NAMED("test_example", "Visualizing plan 3 (pose goal move around cuboid) %s", success ? "SUCCESS" : "FAILED");
    visual_tools.publishTrajectoryLine(plan.trajectory_, joint_model_group_arm);
    visual_tools.trigger();
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window once the plan is complete");

    // ----------------------------------------------------------------------------------------------------------------
    // Program End
    // ^^^^^^^^^^^
    visual_tools.deleteAllMarkers();
    ros::shutdown();
    return 0;
}