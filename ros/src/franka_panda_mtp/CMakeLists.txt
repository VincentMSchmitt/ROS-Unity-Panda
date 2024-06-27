cmake_minimum_required(VERSION 3.5)
project(franka_panda_mtp)

if(POLICY CMP0148)
  cmake_policy(SET CMP0148 OLD)
endif()

find_package(catkin REQUIRED COMPONENTS
  rospy
  roslaunch
  tf
  moveit_commander
  sensor_msgs
  moveit_msgs
  geometry_msgs
  visualization_msgs
  franka_panda_communication
)

roslaunch_add_file_check(launch)

catkin_package(
  CATKIN_DEPENDS
  rospy
  roslaunch
  tf
  moveit_commander
  sensor_msgs
  moveit_msgs
  geometry_msgs
  visualization_msgs
  franka_panda_communication
)

catkin_install_python(PROGRAMS
  scripts/follower.py
  scripts/mover.py
  scripts/collision_detector.py
  DESTINATION ${CATKIN_PACKAGE_BIN_DESTINATION}
)

install(DIRECTORY launch
  DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION}
  PATTERN "setup_assistant.launch" EXCLUDE
)

include_directories(
  ${catkin_INCLUDE_DIRS}
)