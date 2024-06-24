cmake_minimum_required(VERSION 3.1.3)
project(franka_panda_moveit)

find_package(catkin REQUIRED COMPONENTS
  # python
  rospy
  std_msgs
  geometry_msgs
  ros_tcp_endpoint
  message_generation
  moveit_msgs
  sensor_msgs
  controller_manager
  roslaunch
  trajectory_msgs

  # cpp
  roscpp
  moveit_core
  moveit_ros_planning_interface
  geometry_msgs
  std_srvs
)

roslaunch_add_file_check(launch)

add_message_files(DIRECTORY msg)

add_service_files(DIRECTORY srv)

generate_messages(
  DEPENDENCIES
  geometry_msgs
  std_msgs
  moveit_msgs
  sensor_msgs
)

catkin_package(
  CATKIN_DEPENDS
  roscpp
  moveit_core
  moveit_ros_planning_interface
  geometry_msgs
  std_srvs
)

include_directories(
  ${catkin_INCLUDE_DIRS}
)

add_executable(${PROJECT_NAME}_node scripts/testarea/mover.cpp)
target_link_libraries(${PROJECT_NAME}_node
  ${catkin_LIBRARIES}
  moveit_visual_tools
  rviz_visual_tools
)

add_executable(test_node scripts/testarea/test_cpp.cpp)
target_link_libraries(test_node
  ${catkin_LIBRARIES}
  moveit_visual_tools
  rviz_visual_tools
)

catkin_install_python(PROGRAMS
  scripts/mover.py
  scripts/follower.py
  scripts/collision_detector.py
  scripts/testarea/trajectory_subscriber.py
  scripts/testarea/test.py
  DESTINATION ${CATKIN_PACKAGE_BIN_DESTINATION}
)

install(DIRECTORY launch DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION}
  PATTERN "setup_assistant.launch" EXCLUDE)
install(DIRECTORY config DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION})