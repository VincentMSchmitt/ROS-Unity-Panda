# syntax=docker/dockerfile:1
# escape=\

FROM ros:noetic-ros-core

ENV ROS_WORKSPACE=/catkin_ws
ENV DEBIAN_FRONTEND=noninteractive

# Install bootstrap and other needed tools
RUN apt-get update && apt-get install --no-install-recommends -y \
    build-essential \
    python3 \
    python3-rosdep \
    python3-rosinstall \
    python3-vcstools \
    net-tools \
    python3-pip \
    python3-dev \
    nano \
    iputils-ping \
    dos2unix \
    && ln -s /usr/bin/python3 /usr/bin/python \
    && rm -rf /var/lib/apt/lists/*

# Bootstrap rosdep
RUN rosdep init && \
  rosdep update --rosdistro $ROS_DISTRO

# Install ros packages
RUN apt-get update && apt-get install -y --no-install-recommends \
    ros-noetic-ros-base=1.5.0-1* \
    ros-noetic-robot-state-publisher \
    ros-noetic-moveit \
    ros-noetic-rosbridge-suite \
    ros-noetic-joy \
    ros-noetic-ros-control \
    ros-noetic-ros-controllers \
    ros-noetic-tf2-web-republisher \
    && rm -rf /var/lib/apt/lists/*

# Install development tools
RUN pip3 install \
    rospkg \
    jsonpickle

# Copy each directory explicitly to avoid workspace cruft
COPY ./ROS/src/franka_panda_moveit $ROS_WORKSPACE/src/franka_panda_moveit
COPY ./ROS/src/franka_panda_description $ROS_WORKSPACE/src/franka_panda_description
COPY ./ROS/src/moveit_msgs $ROS_WORKSPACE/src/moveit_msgs
COPY ./ROS/src/ros_tcp_endpoint $ROS_WORKSPACE/src/ros_tcp_endpoint

# Copy shell scripts
COPY ./docker/set-up-workspace /setup.sh
COPY docker/startup /

# Compatibility for windows
RUN /bin/bash -c "find $ROS_WORKSPACE -type f -print0 | xargs -0 dos2unix"

# Execute setup script & remove it
RUN dos2unix /startup && dos2unix /setup.sh && chmod +x /setup.sh && /setup.sh && rm /setup.sh

# Making sure the file modes are executable
RUN chmod +x $ROS_WORKSPACE/src/franka_panda_moveit/scripts/*.py

WORKDIR $ROS_WORKSPACE
ENTRYPOINT ["/startup"]