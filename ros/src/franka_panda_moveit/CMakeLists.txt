cmake_minimum_required(VERSION 3.5)
project(franka_panda_moveit)

if(POLICY CMP0148)
  cmake_policy(SET CMP0148 OLD)
endif()

find_package(catkin REQUIRED)

catkin_package()

install(DIRECTORY launch DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION}
  PATTERN "setup_assistant.launch" EXCLUDE)
install(DIRECTORY config DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION})