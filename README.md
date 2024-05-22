# ROS-Unity-panda

This repository contains all files to simulate the Franka Emika Panda robotic arm-manipulator in Unity. It can be controlled using MoveIt1 and ROS1-noetic. This is being achived with the help of the ROS-TCP-Connector ROS-package from Unity-Robotics-Hub.

This repository consists of the following ROS-packages:
* franka_panda_description
* franka_panda_moveit
* moveit_msgs
* ros_tcp_endpoint

> It also includes the Unity-Project used for experimenting with these packages.

## Launching
To launch a demo project, following steps are requiered:
1. Run the unity-project **UnityPandaProject**
2. Run the demo launchfile:
    ```
    $ source ./devel/setup.zsh
    $ roslaunch franka_panda_moveit panda.launch
    ```
3. Start the Unity-Scene **PandaPart03**
4. Press the **publish** button

> The robot should move to the **Target**, pick it up and drop it at the **TargetPlacement** location. Make sure all the GameObjects are assined in the used scripts (drag and drop the GameObject in the corresponding box).