#!/usr/bin/env python3

import rospy
import tf.transformations as tf
from moveit_commander import PlanningSceneInterface
from geometry_msgs.msg import PoseStamped

from franka_panda_communication.msg import ObjectInfo

class ObjectManager:
    def __init__(self):
        rospy.init_node('object_info_listener', anonymous=True)

        self.scene = PlanningSceneInterface()
        self.object_dict = {}  # Dictionary to store object names and their information

        rospy.Subscriber('object_info', ObjectInfo, self.object_info_callback)

        rospy.sleep(2)  # Allow some time for RViz to initialize

    def object_info_callback(self, data):
        object_name = data.name

        # Check if the object is already in the dictionary and if its position has changed
        if object_name in self.object_dict:
            old_data = self.object_dict[object_name]
            if (data.position.x == old_data.position.x and
                data.position.y == old_data.position.y and
                data.position.z == old_data.position.z):
                # The position has not changed, do not update the object
                return
            else:
                # Remove the existing object with the same name if position has changed
                rospy.loginfo("Object " + object_name + " position has been updated.")
                self.remove_object(object_name)
        
        # Add the new object
        self.add_object(data)

    def add_object(self, data, timeout=4):
        scene = self.scene

        # Create a PoseStamped message for the object
        object_pose = PoseStamped()
        object_pose.header.frame_id = "world"
        object_pose.pose.position = data.position

        # Create a quaternion for a 90-degree rotation around the x-axis
        q = tf.quaternion_from_euler(1.5708, 0, 0)  # 1.5708 radians = 90 degrees
        object_pose.pose.orientation.x = q[0]
        object_pose.pose.orientation.y = q[1]
        object_pose.pose.orientation.z = q[2]
        object_pose.pose.orientation.w = q[3]

        # Add the object to the planning scene
        scene.add_box(data.name, object_pose, size=(data.size.x, data.size.y, data.size.z))

        self.object_dict[data.name] = data
        return self.wait_for_state_update(object_name=data.name, object_is_known=True, timeout=timeout)

    def remove_object(self, object_name, timeout=4):
        scene = self.scene

        scene.remove_world_object(object_name)

        if object_name in self.object_dict:
            del self.object_dict[object_name]

        return self.wait_for_state_update(object_name=object_name, object_is_known=False, timeout=timeout)

    def wait_for_state_update(self, object_name, object_is_known=False, timeout=4):
        start = rospy.get_time()
        seconds = rospy.get_time()
        while (seconds - start < timeout) and not rospy.is_shutdown():
            # Test if the object is in the scene
            is_known = object_name in self.scene.get_known_object_names()

            if object_is_known == is_known:
                return True

            rospy.sleep(0.1)
            seconds = rospy.get_time()
        return False

    def __del__(self):
        rospy.loginfo("Collision detection logging off.")

if __name__ == '__main__':
    try:
        object_manager = ObjectManager()
        rospy.spin()
    except rospy.ROSInterruptException:
        pass