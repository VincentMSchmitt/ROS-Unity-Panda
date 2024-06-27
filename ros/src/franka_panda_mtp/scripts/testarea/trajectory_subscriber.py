#!/usr/bin/env python3
# Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
# You may use, distribute and modify this code under the
# terms of the Educational Community License (ECL), Version 2.0.
# 
# You should have received a copy of the ECL license with
# this file. If not, please write to: schmittv@hs-pforzheim.de,
# or visit: https://opensource.org/licenses/ECL-2.0
"""
    Subscribes to SourceDestination topic.
    Uses MoveIt to compute a trajectory from the target to the destination.
    Trajectory is then published to PickAndPlaceTrajectory topic.
"""
import rospy

from franka_panda_communication.msg import PandaMoveitJoints

def callback(data):
    rospy.loginfo("On: " + rospy.get_caller_id() + " I heard:\n%s", data)

def listener():
    rospy.init_node('Trajectory_Subscriber', anonymous=True)
    rospy.Subscriber("/panda_joints", PandaMoveitJoints, callback)
    rospy.spin()

if __name__ == '__main__':
    listener()