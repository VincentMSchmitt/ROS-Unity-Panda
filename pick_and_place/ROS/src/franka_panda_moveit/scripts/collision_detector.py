# #!/usr/bin/env python

from __future__ import print_function

import os
import sys
import copy
import rospy
import moveit_commander
import moveit_msgs.msg
import geometry_msgs.msg
from math import pi, tau, dist, fabs, cos
from std_msgs.msg import String
from moveit_commander.conversions import pose_to_list
from franka_panda_moveit.msg import ObjectInfo

class ObjectInfoListener:
    def __init__(self):
        # Initialisiere den Node
        rospy.init_node('object_info_listener', anonymous=True)

        # Erstelle ein Subscriber für das 'object_info' Thema
        rospy.Subscriber('object_info', ObjectInfo, self.callback)

        # Speichere die empfangenen Informationen
        self.objects_info = []

    def callback(self, data):
        rospy.loginfo(f"Received object info: {data}")
        self.objects_info.append(data)

        # Optional: speichere die Informationen in einer Datei
        with open('object_info.txt', 'a') as file:
            file.write(f"Name: {data.name}\n")
            file.write(f"Position: x={data.position.x}, y={data.position.y}, z={data.position.z}\n")
            file.write(f"Rotation: x={data.rotation.x}, y={data.rotation.y}, z={data.rotation.z}, w={data.rotation.w}\n")
            file.write(f"Size: x={data.size.x}, y={data.size.y}, z={data.size.z}\n\n")

    def start_listening(self):
        rospy.spin()


    # TODO: add functionality

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




# ---------------------------------------------------------------------------------------------------------------------

if __name__ == '__main__':
    try:
        listener = ObjectInfoListener()
        listener.start_listening()
    except rospy.ROSInterruptException:
        pass