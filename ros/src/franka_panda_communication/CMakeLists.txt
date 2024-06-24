cmake_minimum_required(VERSION 3.5)
project(franka_panda_communication)

if(POLICY CMP0148)
  cmake_policy(SET CMP0148 OLD)
endif()

find_package(catkin REQUIRED COMPONENTS
  rospy
  message_generation
  std_msgs
  geometry_msgs
  moveit_msgs
)

add_message_files(DIRECTORY msg)

add_service_files(DIRECTORY srv)

add_action_files(DIRECTORY actions)

generate_messages(
  DEPENDENCIES
  std_msgs
  geometry_msgs
  moveit_msgs
)

catkin_package(
  CATKIN_DEPENDS message_runtime rospy std_msgs geometry_msgs moveit_msgs
)

# Install files in the share directory
foreach(dir meshes robots launch config)
   install(DIRECTORY ${dir}/
      DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION}/${dir}
      PATTERN ".svn" EXCLUDE
   )
endforeach(dir)