cmake_minimum_required(VERSION 3.5)
project(franka_panda_description)

if(POLICY CMP0148)
  cmake_policy(SET CMP0148 OLD)
endif()

find_package(catkin REQUIRED)

catkin_package()

foreach(dir meshes robots)
   install(DIRECTORY ${dir}/
      DESTINATION ${CATKIN_PACKAGE_SHARE_DESTINATION}/${dir})
endforeach(dir)
