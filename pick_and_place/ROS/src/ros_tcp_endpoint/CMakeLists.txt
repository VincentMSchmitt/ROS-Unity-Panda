cmake_minimum_required(VERSION 2.8.3)
project(ros_tcp_endpoint)

# Set CMP0148 to OLD to suppress warning when building
cmake_policy(SET CMP0148 OLD)

find_package(catkin REQUIRED COMPONENTS
  rospy
  std_msgs
)

catkin_python_setup()

catkin_package(CATKIN_DEPENDS message_runtime )
