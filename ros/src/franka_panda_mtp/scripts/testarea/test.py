#!/usr/bin/env python3
# Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
# You may use, distribute and modify this code under the
# terms of the Educational Community License (ECL), Version 2.0.
# 
# You should have received a copy of the ECL license with
# this file. If not, please write to: schmittv@hs-pforzheim.de,
# or visit: https://opensource.org/licenses/ECL-2.0

import sys
import copy
import rospy
import moveit_commander
# visulization
from visualization_msgs.msg import Marker
from geometry_msgs.msg import Point
# predefined positions
import positions

def main():
    moveit_commander.roscpp_initialize(sys.argv)
    rospy.init_node('moveit_test')
    
    robot = moveit_commander.RobotCommander()
    move_group_arm = moveit_commander.MoveGroupCommander("panda_arm")
    move_group_hand = moveit_commander.MoveGroupCommander("panda_hand")
    
    marker_pub = rospy.Publisher('visualization_marker', Marker, queue_size=10)
    rospy.sleep(2)  # wait for rviz
    
    # Function to initialise marker for visulization ----------------------------------------------
    def initialize_marker():
        marker = Marker()
        marker.header.frame_id = "world"
        marker.type = marker.LINE_STRIP
        marker.action = marker.ADD
        marker.scale.x = 0.1
        marker.color.a = 0.5
        marker.color.r = 0.0
        marker.color.g = 1.0
        marker.color.b = 0.0
        marker.lifetime = rospy.Duration(0)  # The marker remains in place
        return marker

    # Function to add a single point to the marker  ------------------------------------------------
    def add_point_to_marker(pose, marker):
        p = Point()
        p.x = pose.position.x
        p.y = pose.position.y
        p.z = pose.position.z
        marker.points.append(p)
        marker_pub.publish(marker)
    
    # Function to add the complete trajectory to the marker  --------------------------------------
    def add_trajectory_to_marker(trajectory, marker):
        for point in trajectory.joint_trajectory.points:
            pose = move_group_arm.get_current_pose().pose
            print("TCP Pose at this point:", pose)
            add_point_to_marker(pose, marker)
    
    # Function to move gripper  -------------------------------------------------------------------
    def set_gripper_percentage(opening_percentage):
        opening_percentage = max(0.0, min(100.0, opening_percentage))  # Clamp to [0, 100]
        max_opening = 0.08  # Maximum opening width in meters (100% open)
        opening_width = (opening_percentage / 100.0) * max_opening
        joint_goal = move_group_hand.get_current_joint_values()
        joint_goal[0] = opening_width / 2.0  # Sets the value for panda_finger_joint1
        joint_goal[1] = opening_width / 2.0  # Sets the value for panda_finger_joint2
        move_group_hand.go(joint_goal, wait=True)
        move_group_hand.stop()
        
    # Setup moveit --------------------------------------------------------------------------------
    marker = initialize_marker()
    
    # arm
    move_group_arm.set_planning_time(10)
    move_group_arm.set_num_planning_attempts(50)
    move_group_arm.set_max_velocity_scaling_factor(1)
    move_group_arm.set_max_acceleration_scaling_factor(1)
    move_group_arm.set_planner_id("RRTConnect")
    # hand
    move_group_hand.set_planning_time(10)
    move_group_hand.set_num_planning_attempts(50)
    move_group_hand.set_max_velocity_scaling_factor(1)
    move_group_hand.set_max_acceleration_scaling_factor(1)
    
    # plan and exectue  ---------------------------------------------------------------------------
    # remove all leftover markers from the scene
    marker.DELETEALL
    marker_pub.publish(marker)
    
    # Move to pose1
    move_group_arm.set_pose_target(positions.pose1)
    plan = move_group_arm.plan()
    if plan[0]:  # Check if planning was successful
        plan1 = plan[1]
        move_group_arm.execute(plan1, wait=True)
        move_group_arm.stop()
        move_group_arm.clear_pose_targets()
        add_trajectory_to_marker(plan1, marker)
    else:
        rospy.logerr("Planning to pose1 failed")

    # Move to pose2
    # Plan Cartesian Path from pose1 to pose2 along z-axis
    pose2 = positions.pose2
    waypoints = []
    waypoints.append(move_group_arm.get_current_pose().pose)
    plan2 = move_group_arm.get_current_pose().pose
    plan2.position.z = pose2.position.z  # Only change the z-axis
    waypoints.append(copy.deepcopy(plan2))

    # Compute Cartesian path
    (plan, fraction) = move_group_arm.compute_cartesian_path(
                                waypoints,   # waypoints to follow
                                0.01,        # eef_step
                                0.0)         # jump_threshold

    # Check if a sufficient fraction of the path was planned
    if fraction == 1:
        success = move_group_arm.execute(plan, wait=True)
        if success:
            move_group_arm.stop()
            move_group_arm.clear_pose_targets()
            set_gripper_percentage(25) # 25% open
            #add_trajectory_to_marker(plan, marker)
    else:
        rospy.logerr("Planning to pose3 failed")
            
    # Move to pose3
    move_group_arm.set_pose_target(positions.pose3)
    plan = move_group_arm.plan()
    if plan[0]:  # Check if planning was successful
        plan3 = plan[1]
        move_group_arm.execute(plan3, wait=True)
        move_group_arm.stop()
        move_group_arm.clear_pose_targets()
        #add_trajectory_to_marker(plan, marker)
    else:
        rospy.logerr("Planning to pose3 failed")
    
    # Move to pose4
    # Plan Cartesian Path from pose3 to pose4 along z-axis
    pose4 = positions.pose4
    waypoints = []
    waypoints.append(move_group_arm.get_current_pose().pose)
    plan4 = move_group_arm.get_current_pose().pose
    plan4.position.z = pose4.position.z  # Only change the z-axis
    waypoints.append(copy.deepcopy(plan4))

    # Compute Cartesian path
    (plan, fraction) = move_group_arm.compute_cartesian_path(
                                waypoints,   # waypoints to follow
                                0.01,        # eef_step
                                0.0)         # jump_threshold

    # Check if a sufficient fraction of the path was planned
    if fraction == 1:
        success = move_group_arm.execute(plan, wait=True)
        if success:
            move_group_arm.stop()
            move_group_arm.clear_pose_targets()
            set_gripper_percentage(100) # 100% open
            #add_trajectory_to_marker(plan, marker)
    else:
        rospy.logerr("Planning to pose4 failed")
    
    # Move to home
    move_group_arm.set_named_target("home")
    plan = move_group_arm.plan()
    if plan[0]:  # Check if planning was successful
        plan_home = plan[1]
        move_group_arm.execute(plan_home, wait=True)
        move_group_arm.stop()
        move_group_arm.clear_pose_targets()
        #add_trajectory_to_marker(plan, marker)
    else:
        rospy.logerr("Planning to home position failed")

    print("All movements completed")

    # end program  --------------------------------------------------------------------------------
    moveit_commander.roscpp_shutdown()
    
if __name__ == "__main__":
    main()