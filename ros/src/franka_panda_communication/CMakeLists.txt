cmake_minimum_required(VERSION 3.0.2)
project(franka_panda_communication)

find_package(catkin REQUIRED COMPONENTS
  rospy
  message_generation
  std_msgs
  geometry_msgs
  moveit_msgs
)

add_message_files(DIRECTORY msg)

add_service_files(DIRECTORY srv)

generate_messages(
  DEPENDENCIES
  std_msgs
  geometry_msgs
  moveit_msgs
)