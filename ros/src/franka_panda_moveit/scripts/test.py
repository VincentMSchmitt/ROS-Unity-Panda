#!/usr/bin/env python3

import sys
import rospy
import moveit_commander
import geometry_msgs.msg

def main():
    moveit_commander.roscpp_initialize(sys.argv)
    rospy.init_node('moveit_test')
    
    robot = moveit_commander.RobotCommander()
    move_group_arm = moveit_commander.MoveGroupCommander("panda_arm")
    move_group_hand = moveit_commander.MoveGroupCommander("panda_hand")
    
    move_group_arm.set_planning_time(10)
    move_group_arm.set_num_planning_attempts(50)
    move_group_arm.set_max_velocity_scaling_factor(1)
    move_group_arm.set_max_acceleration_scaling_factor(1)
    move_group_arm.set_planner_id("RRTConnect")
    
    move_group_hand.set_planning_time(10)
    move_group_hand.set_num_planning_attempts(50)
    move_group_hand.set_max_velocity_scaling_factor(1)
    move_group_hand.set_max_acceleration_scaling_factor(1)
    
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
    
    # Define the target positions -----------------------------------------------------------------
    pose1 = geometry_msgs.msg.Pose()
    pose1.orientation.w = 1.0
    pose1.position.x = 0.4
    pose1.position.y = 0.1
    pose1.position.z = 0.4

    pose2 = geometry_msgs.msg.Pose()
    pose2.orientation.w = 1.0
    pose2.position.x = 0.4
    pose2.position.y = -0.1
    pose2.position.z = 0.4
    
    # plan and exectue  ---------------------------------------------------------------------------
    # Move to pose1
    move_group_arm.set_pose_target(pose1)
    plan = move_group_arm.plan()
    if plan[0]:  # Check if planning was successful
        plan1 = plan[1]
        move_group_arm.execute(plan1, wait=True)
        move_group_arm.stop()
        move_group_arm.clear_pose_targets()
        
        # Open the gripper
        set_gripper_percentage(80) # 80% open
    else:
        rospy.logerr("Planning to pose1 failed")

    # Move to pose2
    move_group_arm.set_pose_target(pose2)
    plan = move_group_arm.plan()
    if plan[0]:  # Check if planning was successful
        plan2 = plan[1]
        move_group_arm.execute(plan2, wait=True)
        move_group_arm.stop()
        move_group_arm.clear_pose_targets()
        
        # close the gripper
        set_gripper_percentage(0) # 0% open
    else:
        rospy.logerr("Planning to pose2 failed")

    print("All movements completed")

    moveit_commander.roscpp_shutdown()
    
if __name__ == "__main__":
    main()