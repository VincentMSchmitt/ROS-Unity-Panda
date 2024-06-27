#!/usr/bin/env python3

import rospy
import moveit_commander
import geometry_msgs.msg
from mtppy.service import Service
from mtppy.procedure import Procedure
from mtppy.operation_elements import AnaServParam

import control.positions as positions
from franka_panda_communication.srv import MoveService, MoveServiceRequest

# -------------------------------------------------------------------------------------------------
class MoveControlROSClient:
    def __init__(self):
        # Initialize the ROS node if not already initialized
        if not rospy.get_node_uri():
            rospy.init_node('mtp_panda_robot', anonymous=True)

    def make_service_request(self, _trajectory):
        rospy.wait_for_service('unity_move_service', 5.0)
        try:
            move_service = rospy.ServiceProxy('unity_move_service', MoveService)
            request = MoveServiceRequest(trajectory=_trajectory)
            response = move_service(request)
            #rospy.loginfo("Service call successful: %s", response.success)
            return response.success
        except rospy.ServiceException as e:
            rospy.logerr("Service call failed: %s", e)
            return False

# -------------------------------------------------------------------------------------------------
class MoveControl():
    def __init__(self, robot, group, pose, name):
        self.robot = robot
        self.group = group
        self.pose = pose
        self.name = name
        self.planned_path = None

    def update_pose(self, new_pose):
        self.pose = new_pose

    def reach_pose_via_joints(self):
        ''' Move the robot to the desired pose via joint values.

        Returns:
            bool: True if the robot successfully reaches the pose, False otherwise.
        '''
        self.group.set_joint_value_target(self.pose[2])
        self.planned_path = self.group.plan()
        
        # if the planing was successful, send message to unity and wait for its to complete
        if self.planned_path:
            trajectory = self.planned_path[1]
            client = MoveControlROSClient()
            success = client.make_service_request(trajectory)
            if success:
                #rospy.logerr("Planning the joint goal succeeded.")
                return True
            else:
                rospy.logerr("Planning the joint goal failed.")
                return False
        
    def reach_pose_via_posquat(self):
        ''' Move the robot to the desired pose via Position/Quaternion values.

        Returns:
            bool: True if the robot successfully reaches the pose, False otherwise.
        '''
        posquat = geometry_msgs.msg.Pose()
        posquat.position = geometry_msgs.msg.Point(x=self.pose[0][0],y=self.pose[0][1], z=self.pose[0][2])
        posquat.orientation = geometry_msgs.msg.Quaternion(w=self.pose[1][0],x=self.pose[1][1],y=self.pose[1][2],z=self.pose[1][3])
        self.group.set_pose_target(posquat)
        self.planned_path = self.group.plan()

        # if the planing was successful, send message to unity and wait for its to complete
        if self.planned_path:
            trajectory = self.planned_path[1]
            client = MoveControlROSClient()
            success = client.make_service_request(trajectory)
            if success:
                #rospy.loginfo("Trajectory execution successful.")
                return True
            else:
                rospy.logerr("Trajectory execution failed.")
                return False
        rospy.logerr("Planning the joint goal failed.")
        return False

# -------------------------------------------------------------------------------------------------
class mtpMoveService(Service):
    def __init__(self, tag_name: str, tag_description: str):
        super().__init__(tag_name, tag_description)

        group = moveit_commander.MoveGroupCommander('panda_arm')
        robot = moveit_commander.RobotCommander('robot_description')
        group.set_max_velocity_scaling_factor(0.4)
        group.set_max_acceleration_scaling_factor(0.2)
        group.set_planner_id("RRTConnect")
        group.set_planning_time(30)
        group.set_num_planning_attempts(50)
        
        self.defaultpose = [positions.pos1, positions.rot1, positions.joints1]
        self.movetask = MoveControl(robot=robot,group=group,pose=self.defaultpose,name="target_1")

        ### Procedure using joint values (j1 - j7) to move to a specific pose relative to the robots base
        movejoints_rel2base_procedure = Procedure(procedure_id=1, tag_name="MoveViaJoints",is_self_completing=True)
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint1", tag_description='', v_min=-2.8963, v_max=2.8963))
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint2", tag_description='', v_min=-1.7618, v_max=1.7618))
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint3", tag_description='', v_min=-2.8963, v_max=2.8963))
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint4", tag_description='', v_min=-3.0708, v_max=-0.0688))
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint5", tag_description='', v_min=-2.8963, v_max=2.8963))
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint6", tag_description='', v_min=-0.0165, v_max=3.7515))
        movejoints_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Joint7", tag_description='', v_min=-2.8963, v_max=2.8963))
        
        ### Procedure using Position (x,y,z) and Quaternion(qx,qy,qz,qw) to move to a specific pose relative to the robots base
        moveposquat_rel2base_procedure = Procedure(procedure_id=2, tag_name="MoveViaPosQuat",is_self_completing=True)
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="X-Coordinate",  tag_description='', v_min=-0.845, v_max=0.845))
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Y-Coordinate",  tag_description='', v_min=-0.350, v_max=1.180))
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="Z-Coordinate",  tag_description='', v_min=-0.845, v_max=0.845))
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="QX-Quaternion", tag_description='', v_min=-1000, v_max=1000))
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="QY-Quaternion", tag_description='', v_min=-1000, v_max=1000))
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="QZ-Quaternion", tag_description='', v_min=-1000, v_max=1000))
        moveposquat_rel2base_procedure.add_procedure_parameter(AnaServParam(tag_name="QW-Quaternion", tag_description='', v_min=-1000, v_max=1000))

        ### add procedure to service
        self.add_procedure(movejoints_rel2base_procedure)
        self.add_procedure(moveposquat_rel2base_procedure)
    
    ## IDLE ---------------------------------------------------------------------------------------
    def idle(self):
        """
        Idle state.
        :return:
        """
        if self.procedure_control.get_procedure_cur() != 0:
            print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Idle State!")
    
    ## STARTING -----------------------------------------------------------------------------------
    def starting(self):
        """
        Starting state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Starting State!")

        # handle procedure cur
        if self.procedure_control.get_procedure_cur() == 1:
            self.procedures[1].procedure_parameters['Joint1'].set_v_out()
            self.procedures[1].procedure_parameters['Joint2'].set_v_out()
            self.procedures[1].procedure_parameters['Joint3'].set_v_out()
            self.procedures[1].procedure_parameters['Joint4'].set_v_out()
            self.procedures[1].procedure_parameters['Joint5'].set_v_out()
            self.procedures[1].procedure_parameters['Joint6'].set_v_out()
            self.procedures[1].procedure_parameters['Joint7'].set_v_out()
        elif self.procedure_control.get_procedure_cur() == 2:
            self.procedures[2].procedure_parameters['X-Coordinate'].set_v_out()
            self.procedures[2].procedure_parameters['Y-Coordinate'].set_v_out()
            self.procedures[2].procedure_parameters['Z-Coordinate'].set_v_out()
            self.procedures[2].procedure_parameters['QX-Quaternion'].set_v_out()
            self.procedures[2].procedure_parameters['QY-Quaternion'].set_v_out()
            self.procedures[2].procedure_parameters['QZ-Quaternion'].set_v_out()
            self.procedures[2].procedure_parameters['QW-Quaternion'].set_v_out()
        else:
            print(f"Not a valid Procedure ID: {self.procedure_control.get_procedure_cur()}")

        new_pos = []
        new_rot = []
        new_joints = []
        
        new_pos.append(self.procedures[2].procedure_parameters['X-Coordinate'].get_v_out())
        new_pos.append(self.procedures[2].procedure_parameters['Y-Coordinate'].get_v_out())
        new_pos.append(self.procedures[2].procedure_parameters['Z-Coordinate'].get_v_out())

        new_rot.append(self.procedures[2].procedure_parameters['QW-Quaternion'].get_v_out())
        new_rot.append(self.procedures[2].procedure_parameters['QX-Quaternion'].get_v_out())
        new_rot.append(self.procedures[2].procedure_parameters['QY-Quaternion'].get_v_out())
        new_rot.append(self.procedures[2].procedure_parameters['QZ-Quaternion'].get_v_out())
        
        new_joints.append(self.procedures[1].procedure_parameters['Joint1'].get_v_out())
        new_joints.append(self.procedures[1].procedure_parameters['Joint2'].get_v_out())
        new_joints.append(self.procedures[1].procedure_parameters['Joint3'].get_v_out())
        new_joints.append(self.procedures[1].procedure_parameters['Joint4'].get_v_out())
        new_joints.append(self.procedures[1].procedure_parameters['Joint5'].get_v_out())
        new_joints.append(self.procedures[1].procedure_parameters['Joint6'].get_v_out())
        new_joints.append(self.procedures[1].procedure_parameters['Joint7'].get_v_out())

        new_pose = [new_pos, new_rot, new_joints]

        self.movetask.update_pose(new_pose=new_pose)
        self.state_change()
        return  
    
    ## EXECUTE ------------------------------------------------------------------------------------
    def execute(self):
        """
        Execute state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Execute State!")
        
        # handle procedure cur
        if self.procedure_control.get_procedure_cur() == 1:
            self.movetask.reach_pose_via_joints()
            self.state_change()
        elif self.procedure_control.get_procedure_cur() == 2:
            self.movetask.reach_pose_via_posquat()
            self.state_change()
        else:
            print(f"Not a valid Procedure ID: {self.procedure_control.get_procedure_cur()}")

        return
    
    ## COMPLETEING --------------------------------------------------------------------------------
    def completing(self):
        """
        Completing state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Completing State!")
        self.state_change()
        return
    
    ## COMPLETED ----------------------------------------------------------------------------------
    def completed(self):
        """
        Completed state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Completed State!")
        return
    
    ## PAUSING ------------------------------------------------------------------------------------
    def pausing(self):
        """
        Pausing state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Pausing State!")
        self.state_change()
        return
    
    ## PAUSED -------------------------------------------------------------------------------------
    def paused(self):
        """
        Paused state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Paused State!")
        return
    
    ## RESUMING -----------------------------------------------------------------------------------
    def resuming(self):
        """
        Resuming state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Resuming State!")
        self.state_change()
        return
    
    ## HOLDING ------------------------------------------------------------------------------------
    def holding(self):
        """
        Holding state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Holding State!")
        self.state_change()
        return
    
    ## HELD ---------------------------------------------------------------------------------------
    def held(self):
        """
        Held state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Held State!")
        return
    
    ## UNHOLDING ----------------------------------------------------------------------------------
    def unholding(self):
        """
        Unholding state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Unholding State!")
        self.state_change()
        return
    
    ## STOPPING -----------------------------------------------------------------------------------
    def stopping(self):
        """
        Stopping state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Stopping State!")
        self.state_change()
        return
    
    ## STOPPED ------------------------------------------------------------------------------------
    def stopped(self):
        """
        Stopped state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Stopped State!")
        self.state_change()
        return
    
    ## ABORTING -----------------------------------------------------------------------------------
    def aborting(self):
        """
        Aborting state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Aborting State!")
        self.state_change()
        return
    
    ## ABORTED ------------------------------------------------------------------------------------
    def aborted(self):
        """
        Aborted state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Aborted State!")
        return
    
    ## RESETTING ----------------------------------------------------------------------------------
    def resetting(self):
        """
        Resetting state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Resetting State!")
        self.state_change()
        return