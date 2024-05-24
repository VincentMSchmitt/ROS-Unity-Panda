# ROS-Unity-Panda
This repository contains all files to simulate the Franka Emika Panda robotic arm-manipulator in Unity. It can be controlled using MoveIt and ROS-Noetic. This is being achived with the help of the ROS-TCP-Connector ROS-package from Unity-Robotics-Hub. It aims to recreate the *pick_and_place* tutorial from the Unity-Robotics-Hub as a starting point and adds functionality on that basis.

### Table of Contents
1. [Credits](#credits)
2. [Launching](#example2)
3. [TODO](#third-example)

### Credits
This repository consists of the following ROS-packages:
* **franka_panda_description** (taken from [franka_panda_description](https://github.com/justagist/franka_panda_description "justagist's GitHub"), modified for my needs)
* **franka_panda_moveit** (created with the Setup Assistant [Moveit Setup Assistant](https://github.com/moveit/moveit/tree/master/moveit_setup_assistant "Setup Assistant GitHub") from the franka_panda_description package, heavily modified)
* **moveit_msgs** (taken from [MoveitMsg](https://github.com/moveit/moveit_msgs "MoveitMsg GitHub"))
* **ros_tcp_endpoint** (taken from [Unity-Robotics-Hub](https://github.com/Unity-Technologies/ROS-TCP-Endpoint "Unity-Robotics-Hub"))

> It also includes the Unity-Project used for experimenting with these packages. These are build from scratch, but for orientation 
[Pick and Place Demo](https://github.com/Unity-Technologies/Unity-Robotics-Hub/tree/main/tutorials/pick_and_place "Unity-Robotics-Hub") was used.

### Launching
To launch a demo project, following steps are requiered:
1. Run the Unity project **UnityPandaProject**
2. Navigte to `/ws_panda/pick_and_place_ROS/`
2. Run the demo launchfile:
    ```
    $ source ./devel/setup.zsh #or setup.bash if you don't use zsh
    $ roslaunch franka_panda_moveit panda.launch
    ```
3. Start the Unity-Scene **PandaPart03**
4. Press the **publish** button

> The robot should move to the **Target**, pick it up and drop it at the **TargetPlacement** location. Make sure all the GameObjects are assined in the used scripts (drag and drop the GameObject in the corresponding box).

### TODO:
* fix a bug where MoveIt ignores the floor and plans its trajectory through it
* attach the target to the gripper to prevent the robot throwing the target (this is not a final fix)
* ~~fix the grabbing offset~~
* ~~add visulization for the TCP path~~
* ~~add the collision of the cube to MoveIt~~