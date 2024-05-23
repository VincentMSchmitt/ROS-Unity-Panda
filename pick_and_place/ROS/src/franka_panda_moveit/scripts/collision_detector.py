#!/usr/bin/env python

from __future__ import print_function

import sys
import copy
import rospy
import moveit_commander
import moveit_msgs.msg
import geometry_msgs.msg
from math import pi, tau, dist, fabs, cos
from std_msgs.msg import String
from moveit_commander.conversions import pose_to_list

def add_box(self, timeout=4):
    box_name = self.box_name
    scene = self.scene

    ## First, we will create a box in the planning scene between the fingers:
    box_pose = geometry_msgs.msg.PoseStamped()
    box_pose.header.frame_id = "panda_hand"
    box_pose.pose.orientation.w = 1.0
    box_pose.pose.position.z = 0.11  # above the panda_hand frame
    box_name = "box"
    scene.add_box(box_name, box_pose, size=(0.075, 0.075, 0.075))

    self.box_name = box_name
    return self.wait_for_state_update(box_is_known=True, timeout=timeout)

def remove_box(self, timeout=4):
    box_name = self.box_name
    scene = self.scene

    scene.remove_world_object(box_name)

    return self.wait_for_state_update(
        box_is_attached=False, box_is_known=False, timeout=timeout
    )


def main():
    print("Test")
    # try:
    #     #TODO: add Implementation

    # except rospy.ROSInterruptException:
    #     return
    # except KeyboardInterrupt:
    #     return


if __name__ == "__main__":
    main()