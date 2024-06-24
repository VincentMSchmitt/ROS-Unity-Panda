/*
    This program is a testing ground for experimenting with the moveit c++ interface.
    Everything that is being done here, served as an example for the real program.
*/

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
    moveit::planning_interface::MoveGroupInterface move_group_interface_hand(PLANNING_GROUP_HAND);

    // FOr adding and removing collision objects to the "virtual world" scene
    moveit::planning_interface::PlanningSceneInterface planning_scene_interface;

    // Raw pointers are frequently used to refer to the planning group for improved performance.
    const moveit::core::JointModelGroup* joint_model_group_arm =
        move_group_interface_arm.getCurrentState()->getJointModelGroup(PLANNING_GROUP_ARM);
    const moveit::core::JointModelGroup* joint_model_group_hand =
        move_group_interface_hand.getCurrentState()->getJointModelGroup(PLANNING_GROUP_HAND);

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
    // ^^^^^^^^^^^^^^
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window to start the demo");

    // ----------------------------------------------------------------------------------------------------------------
    // Open Gripper
    // ^^^^^^^^^^^^
    std::vector<double> open_gripper = {0.04, 0.04};
    move_group_interface_hand.setJointValueTarget(open_gripper);
    move_group_interface_hand.setMaxVelocityScalingFactor(1);
    move_group_interface_hand.setMaxAccelerationScalingFactor(1);
    move_group_interface_hand.move();
    
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window to plan a pose");

    // ----------------------------------------------------------------------------------------------------------------
    // Planning to a Pose goal 
    // ^^^^^^^^^^^^^^^^^^^^^^^
    // We can plan a motion for this group to a desired pose for the end-effector.
    geometry_msgs::Pose target_pose1;
    target_pose1.position.x = 0.28;
    target_pose1.position.y = -0.2;
    target_pose1.position.z = 0.5;
    target_pose1.orientation.y = 1.0;
    move_group_interface_arm.setPoseTarget(target_pose1);

    move_group_interface_arm.setMaxVelocityScalingFactor(0.75);
    move_group_interface_arm.setMaxAccelerationScalingFactor(0.75);

    moveit::planning_interface::MoveGroupInterface::Plan plan;
    bool success = (move_group_interface_arm.plan(plan) == moveit::core::MoveItErrorCode::SUCCESS);
    ROS_INFO_NAMED("test_example", "Visualizing plan 1 (pose goal) %s", success ? "SUCCESS" : "FAILED");
    visual_tools.publishTrajectoryLine(plan.trajectory_, joint_model_group_arm);
    visual_tools.trigger();
    move_group_interface_arm.clearPoseTargets();
    visual_tools.prompt("Press 'next' to execute the path");

    // ----------------------------------------------------------------------------------------------------------------
    // Executing to a Pose goal 
    // ^^^^^^^^^^^^^^^^^^^^^^^^
    move_group_interface_arm.execute(plan);
    visual_tools.prompt("Press 'next' to plan the next path (no obstacles)");

    // ----------------------------------------------------------------------------------------------------------------
    // Simple plan
    // ^^^^^^^^^^^
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
    visual_tools.deleteAllMarkers();
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

    // Attaching objects to the robot ---------------------------------------------------------------------------------
    // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //
    // Attach objects to the robot, so that it moves with the robot geometry. This simulates picking up the object for
    //  the purpose of manipulating it. The motion planning should avoid collisions between the two objects as well.
    moveit_msgs::CollisionObject object_to_attach;
    object_to_attach.id = "cylinder1";

    shape_msgs::SolidPrimitive cylinder_primitive;
    cylinder_primitive.type = primitive.CYLINDER;
    cylinder_primitive.dimensions.resize(2);
    cylinder_primitive.dimensions[primitive.CYLINDER_HEIGHT] = 0.20;
    cylinder_primitive.dimensions[primitive.CYLINDER_RADIUS] = 0.04;

    // We define the frame/pose for this cylinder so that it appears in the gripper
    object_to_attach.header.frame_id = move_group_interface_arm.getEndEffectorLink();
    geometry_msgs::Pose grab_pose;
    grab_pose.orientation.w = 1.0;
    grab_pose.position.z = 0.2;

    // First, we add the object to the world (without using a vector)
    object_to_attach.primitives.push_back(cylinder_primitive);
    object_to_attach.primitive_poses.push_back(grab_pose);
    object_to_attach.operation = object_to_attach.ADD;
    planning_scene_interface.applyCollisionObject(object_to_attach);

    // Then, we "attach" the object to the robot at the given link and allow collisions between the object and the listed
    // links. You could also use applyAttachedCollisionObject to attach an object to the robot directly.
    ROS_INFO_NAMED("tutorial", "Attach the object to the robot");
    move_group_interface_arm.attachObject(object_to_attach.id, "panda_hand", { "panda_leftfinger", "panda_rightfinger" });

    visual_tools.trigger();

    /* Wait for MoveGroup to receive and process the attached collision object message */
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window once the new object is attached to the robot");

    // Replan, but now with the object in hand.
    move_group_interface_arm.setStartStateToCurrentState();
    success = (move_group_interface_arm.plan(plan) == moveit::core::MoveItErrorCode::SUCCESS);
    ROS_INFO_NAMED("tutorial", "Visualizing plan 7 (move around cuboid with cylinder) %s", success ? "SUCCESS" : "FAILED");
    visual_tools.publishTrajectoryLine(plan.trajectory_, joint_model_group_arm);
    visual_tools.deleteAllMarkers();
    visual_tools.trigger();
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window once the plan is complete");

    // Detaching and Removing Objects ---------------------------------------------------------------------------------
    // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //
    // Now, let's detach the cylinder from the robot's gripper.
    ROS_INFO_NAMED("tutorial", "Detach the object from the robot");
    move_group_interface_arm.detachObject(object_to_attach.id);
    visual_tools.deleteAllMarkers();
    visual_tools.trigger();

    /* Wait for MoveGroup to receive and process the attached collision object message */
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window once the new object is detached from the robot");

    // Now, let's remove the objects from the world.
    ROS_INFO_NAMED("tutorial", "Remove the objects from the world");
    std::vector<std::string> object_ids;
    object_ids.push_back(collision_object.id);
    object_ids.push_back(object_to_attach.id);
    planning_scene_interface.removeCollisionObjects(object_ids);

    // Show text in RViz of status
    visual_tools.trigger();

    /* Wait for MoveGroup to receive and process the attached collision object message */
    visual_tools.prompt("Press 'next' in the RvizVisualToolsGui window to once the collision object disappears");

    // ----------------------------------------------------------------------------------------------------------------
    // Program End
    // ^^^^^^^^^^^
    visual_tools.deleteAllMarkers();
    visual_tools.trigger();
    ros::shutdown();
    return 0;
}