import geometry_msgs.msg
from tf.transformations import quaternion_from_euler

# Convert Euler angles to quaternion for a downward orientation
down_orientation_quaternion = quaternion_from_euler(0, 3.14159, 0)  # Roll, pitch, yaw

# pose 1
pose1 = geometry_msgs.msg.Pose()
pose1.orientation.x = down_orientation_quaternion[0]
pose1.orientation.y = down_orientation_quaternion[1]
pose1.orientation.z = down_orientation_quaternion[2]
pose1.orientation.w = down_orientation_quaternion[3]
pose1.position.x = 0.5
pose1.position.y = 0.5
pose1.position.z = 0.25

# pose 2
pose2 = geometry_msgs.msg.Pose()
pose2.orientation.x = down_orientation_quaternion[0]
pose2.orientation.y = down_orientation_quaternion[1]
pose2.orientation.z = down_orientation_quaternion[2]
pose2.orientation.w = down_orientation_quaternion[3]
pose2.position.x = 0.5
pose2.position.y = 0.5
pose2.position.z = 0.1

# pose 3
pose3 = geometry_msgs.msg.Pose()
pose3.orientation.x = down_orientation_quaternion[0]
pose3.orientation.y = down_orientation_quaternion[1]
pose3.orientation.z = down_orientation_quaternion[2]
pose3.orientation.w = down_orientation_quaternion[3]
pose3.position.x = 0.5
pose3.position.y = -0.5
pose3.position.z = 0.25

# pose 4
pose4 = geometry_msgs.msg.Pose()
pose4.orientation.x = down_orientation_quaternion[0]
pose4.orientation.y = down_orientation_quaternion[1]
pose4.orientation.z = down_orientation_quaternion[2]
pose4.orientation.w = down_orientation_quaternion[3]
pose4.position.x = 0.5
pose4.position.y = -0.5
pose4.position.z = 0.1