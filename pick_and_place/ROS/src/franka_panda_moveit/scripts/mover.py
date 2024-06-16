#!/usr/bin/env python

from __future__ import print_function

import sys
import copy
import math
import rospy
import tf.transformations as tf
import moveit_commander
import moveit_msgs.msg
from moveit_msgs.msg import Constraints, JointConstraint, PositionConstraint, OrientationConstraint, BoundingVolume
from sensor_msgs.msg import JointState
from moveit_msgs.msg import RobotState
from moveit_commander import PlanningSceneInterface
import geometry_msgs.msg
from geometry_msgs.msg import PoseStamped
from geometry_msgs.msg import Quaternion, Pose
from std_msgs.msg import String
from moveit_commander.conversions import pose_to_list

from franka_panda_moveit.srv import MoverService, MoverServiceRequest, MoverServiceResponse

joint_names = ['panda_joint1', 'panda_joint2', 'panda_joint3', 'panda_joint4', 'panda_joint5', 'panda_joint6', 'panda_joint7']

class Target:
    def __init__(self, name, pose, size):
        self.name = name
        self.pose = pose
        self.size = size

"""
    Creates a pick and place plan using the four states below.
    
    1. Pre Grasp - position gripper directly above target object
    2. Grasp - lower gripper so that fingers are on either side of object
    3. Pick Up - raise gripper back to the pre grasp position
    4. Pre Place - position gripper directly placement position
    5. Place - lower gripper to desired placement position

    Gripper behaviour is handled outside of this trajectory planning.
        - Gripper close occurs after 'grasp' position has been achieved
        - Gripper open occurs after 'place' position has been achieved

    https://github.com/ros-planning/moveit/blob/master/moveit_commander/src/moveit_commander/move_group.py
"""
def plan_pick_and_place(req):
    # for reference see:
    # https://moveit.github.io/moveit_tutorials/doc/move_group_python_interface/move_group_python_interface_tutorial.html
    response = MoverServiceResponse()

    group_name = "panda_arm"
    move_group = moveit_commander.MoveGroupCommander(group_name)
    scene = PlanningSceneInterface()
    current_robot_joint_configuration = req.joints_input.joints

    # plan the trajectory of the poses, if no points in the pose, return empty
    # Pre grasp - position gripper directly above target object -----------------------------------
    pre_grasp_pose = plan_trajectory(move_group, req.pick_pose, current_robot_joint_configuration)
    if not pre_grasp_pose.joint_trajectory.points:
        rospy.logwarn("Pre grasp pose planning failed.")
        return response # empty
    previous_ending_joint_angles = pre_grasp_pose.joint_trajectory.points[-1].positions

    # Grasp - lower gripper so that fingers are on either side of object --------------------------
    pick_pose = copy.deepcopy(req.pick_pose)
    pick_pose.position.z -= req.offset.offset # value gets send from Unity
    grasp_pose = plan_trajectory(move_group, pick_pose, previous_ending_joint_angles)
    if not grasp_pose.joint_trajectory.points:
        rospy.logwarn("Grasp pose planning failed.")
        return response # empty
    previous_ending_joint_angles = grasp_pose.joint_trajectory.points[-1].positions
    
    # spawn and attach the target to the world ----------------------------------------------------
    target = convert_target(req.target)
    scene.add_box(target.name, target.pose, target.size)
    eef_link = move_group.get_end_effector_link()
    robot_commander = moveit_commander.RobotCommander()
    touch_links = robot_commander.get_link_names(group_name)
    scene.attach_box(eef_link, target.name, target.pose, target.size, touch_links)
    
    # Pick Up - raise gripper back to the pre grasp position --------------------------------------
    pick_up_pose = plan_trajectory(move_group, req.pick_pose, previous_ending_joint_angles)
    if not pick_up_pose.joint_trajectory.points:
        rospy.logwarn("Pick up pose planning failed.")
        return response # empty
    previous_ending_joint_angles = pick_up_pose.joint_trajectory.points[-1].positions

    # Pre place - move gripper above desired placement position -----------------------------------
    pre_place_pose = plan_trajectory(move_group, req.place_pose, previous_ending_joint_angles)
    if not pre_place_pose.joint_trajectory.points:
        rospy.logwarn("Pre place pose planning failed.")
        return response # empty
    previous_ending_joint_angles = pre_place_pose.joint_trajectory.points[-1].positions

    # Place - move gripper to desired placement position ------------------------------------------
    place_pose = copy.deepcopy(req.place_pose)
    place_pose.position.z -= req.offset.offset - 0.015 # place 1,5 cm above ground
    place_pose = plan_trajectory(move_group, place_pose, previous_ending_joint_angles)
    if not place_pose.joint_trajectory.points:
        rospy.logwarn("Place pose planning failed.")
        return response # empty
    
    # If trajectory planning worked for all pick and place stages, add plan to response -----------
    response.trajectories.append(pre_grasp_pose)
    response.trajectories.append(grasp_pose)
    response.trajectories.append(pick_up_pose)
    response.trajectories.append(pre_place_pose)
    response.trajectories.append(place_pose)

    # It is adviced to clear the targets after planning the poses ---------------------------------
    move_group.clear_pose_targets()

    # Detach the target from the robot and remove it ----------------------------------------------
    if target.name in scene.get_attached_objects():
        scene.remove_attached_object(eef_link, target.name)
    if target.name in scene.get_known_object_names():
        scene.remove_world_object(target.name)
    
    return response

"""
    Given the start angles of the robot, plan a trajectory that ends at the destination pose.
"""
def plan_trajectory(move_group, destination_pose, start_joint_angles):
    # save current states of the joints defined above
    current_joint_state = JointState()
    current_joint_state.name = joint_names
    current_joint_state.position = start_joint_angles

    # set the start state with the current joint values
    moveit_robot_state = RobotState()
    moveit_robot_state.joint_state = current_joint_state
    move_group.set_start_state(moveit_robot_state)

    # set the goal state and plan the trajectory
    move_group.set_pose_target(destination_pose)
    plan = move_group.plan()

    # if planning fails, raise exeption
    if not plan:
        exception_str = """
            Trajectory could not be planned for a destination of {} with starting joint angles {}.
            Please make sure target and destination are reachable by the robot.
        """.format(destination_pose, destination_pose)
        raise Exception(exception_str)

    return plan[1]

"""
    Converts the Unity values of the target into ROS values so the target can be spawned in RViz
"""
def convert_target(target):
    object_name = target.name
    # Create a PoseStamped message for the object
    object_pose = PoseStamped()
    object_pose.header.frame_id = "world"
    object_pose.pose.position = target.position

    # Create a quaternion for a 90-degree rotation around the x-axis
    q = tf.quaternion_from_euler(1.5708, 0, 0)  # 1.5708 radians = 90 degrees
    object_pose.pose.orientation.x = q[0]
    object_pose.pose.orientation.y = q[1]
    object_pose.pose.orientation.z = q[2]
    object_pose.pose.orientation.w = q[3]

    size = (target.size.x, target.size.y, target.size.z)

    return Target(object_name, object_pose, size)

def moveit_server():
    moveit_commander.roscpp_initialize(sys.argv)
    rospy.init_node('franka_panda_moveit_server')

    s = rospy.Service('franka_panda_moveit', MoverService, plan_pick_and_place)
    print("Ready to plan")
    rospy.spin()

if __name__ == "__main__":
    moveit_server()