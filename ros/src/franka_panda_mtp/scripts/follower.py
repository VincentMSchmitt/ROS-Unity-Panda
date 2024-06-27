#!/usr/bin/env python3
# Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
# You may use, distribute and modify this code under the
# terms of the Educational Community License (ECL), Version 2.0.
# 
# You should have received a copy of the ECL license with
# this file. If not, please write to: schmittv@hs-pforzheim.de,
# or visit: https://opensource.org/licenses/ECL-2.0

import rospy
import moveit_commander
from sensor_msgs.msg import JointState
from moveit_msgs.msg import RobotState

from franka_panda_communication.srv import Follower, FollowerResponse

joint_names = ['panda_joint1', 'panda_joint2', 'panda_joint3', 'panda_joint4', 'panda_joint5', 'panda_joint6', 'panda_joint7']
        
def plan_follow(req):
    """
    Creates a follow plan
    """
    response = FollowerResponse()

    group_name = "panda_arm"
    move_group = moveit_commander.MoveGroupCommander(group_name)

    # follow pose ---------------------------------------------------------------------------------
    robot_joint_configuration = req.joints_input.joints
    target_pose = plan_trajectory(move_group, req.target_pose, robot_joint_configuration)
    if not target_pose.joint_trajectory.points:
        rospy.logwarn("Pose planning failed.")
        return response # empty
    plan = move_group.plan()
    
    # If trajectory planning worked for all pick and place stages, add plan to response -----------
    response.trajectories.append(target_pose)
    
    # It is adviced to clear the targets after planning the poses ---------------------------------
    move_group.clear_pose_targets()

    return response

def plan_trajectory(move_group, target_pose, joint_configuration):
    """
    Given the start angles of the robot, plan a trajectory that ends at the destination pose.
    """
    current_joint_state = JointState()
    current_joint_state.name = joint_names
    current_joint_state.position = joint_configuration

    moveit_robot_state = RobotState()
    moveit_robot_state.joint_state = current_joint_state
    move_group.set_start_state(moveit_robot_state)

    move_group.set_pose_target(target_pose)
    plan = move_group.plan()

    if not plan:
        exception_str = """
            Trajectory could not be planned for a destination of {} with starting joint angles {}.
            Please make sure target and destination are reachable by the robot.
        """.format(target_pose, target_pose)
        raise Exception(exception_str)

    return plan[1]

if __name__ == "__main__":
    rospy.init_node('follower_service')
    service = rospy.Service('franka_panda_follower', Follower, plan_follow)
    rospy.loginfo("Service franka_panda_follower ready")
    rospy.spin()