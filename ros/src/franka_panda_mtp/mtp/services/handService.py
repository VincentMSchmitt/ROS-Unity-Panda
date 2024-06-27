#!/usr/bin/env python3
# Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
# You may use, distribute and modify this code under the
# terms of the Educational Community License (ECL), Version 2.0.
# 
# You should have received a copy of the ECL license with
# this file. If not, please write to: schmittv@hs-pforzheim.de,
# or visit: https://opensource.org/licenses/ECL-2.0

import rospy
import moveit_commander
from mtppy.service import Service
from mtppy.procedure import Procedure
from mtppy.operation_elements import AnaServParam

from franka_panda_communication.srv import HandService, HandServiceRequest

# -------------------------------------------------------------------------------------------------
class HandControlROSClient:
    def __init__(self):
        # Initialize the ROS node if not already initialized
        if not rospy.get_node_uri():
            rospy.init_node('mtp_panda_robot', anonymous=True)

    def make_service_request(self, _gripper_targets):
        rospy.wait_for_service('unity_hand_service', 5.0)
        try:
            move_service = rospy.ServiceProxy('unity_hand_service', HandService)
            request = HandServiceRequest(gripper_targets=_gripper_targets)
            response = move_service(request)
            #rospy.loginfo("Service call successful: %s", response.success)
            return response.success
        except rospy.ServiceException as e:
            rospy.logerr("Service call failed: %s", e)
            return False

# -------------------------------------------------------------------------------------------------
class HandControl():
    def __init__(self, robot, group, name):        
        self.robot = robot
        self.group = group
        self.name = name
    
    def update_pose(self, new_pose):
        self.pose = new_pose

    def _move_gripper(self) -> bool:
        self.group.set_joint_value_target(self.pose)
        plan_success, self.planned_path, planning_time, error_code = self.group.plan()

        # if the planning was successful, send message to unity and wait for its to complete
        if plan_success:
            gripper_targets = [point.positions for point in self.planned_path.joint_trajectory.points]
            gripper_targets = [item for sublist in gripper_targets for item in sublist]  # Flatten the list of lists
            client = HandControlROSClient()
            success = client.make_service_request(gripper_targets)
            if success:
                #rospy.logerr("Planning the joint goal succeeded.")
                return True
            else:
                rospy.logerr("Planning the joint goal failed.")
                return False

    def grasp(self) -> bool:
        return self._move_gripper()

    def open(self) -> bool:
        return self._move_gripper()

# -------------------------------------------------------------------------------------------------
class mtpHandService(Service):
    def __init__(self, tag_name: str, tag_description: str):
        super().__init__(tag_name, tag_description)
        
        group = moveit_commander.MoveGroupCommander('panda_hand')
        robot = moveit_commander.RobotCommander('robot_description')
        group.set_max_velocity_scaling_factor(0.4)
        group.set_max_acceleration_scaling_factor(0.2)
        group.set_planner_id("RRTConnect")
        group.set_planning_time(30)
        group.set_num_planning_attempts(45)
        
        self.movetask = HandControl(robot=robot,group=group,name="target_1")

        ## Procedure Definition
        openProcedure = Procedure(procedure_id=1, tag_name="OpenGripper", tag_description='', is_self_completing=True)
        closeProcedure = Procedure(procedure_id=2, tag_name="CloseGripper", tag_description='', is_self_completing=True)
        ## Procedure Parameters
        openProcedure.add_procedure_parameter(AnaServParam(tag_name='OpeningWidth', tag_description='', v_min=0.0, v_max=0.08, v_unit=1010))
        closeProcedure.add_procedure_parameter(AnaServParam(tag_name='ClosingWidth', tag_description='', v_min=0.0, v_max=0.08, v_unit=1010))

        ## Add Procedures to Service
        self.add_procedure(openProcedure)
        self.add_procedure(closeProcedure)


    def idle(self):
        """
        Idle state.
        :return:
        """
        if self.procedure_control.get_procedure_cur() != 0:
            print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Idle State!")
        
    def starting(self):
        """
        Starting state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Starting State!")
        if self.procedure_control.get_procedure_cur() == 1:
            anaserv = self.procedures[1].procedure_parameters['OpeningWidth']
            anaserv.set_v_out()
        elif self.procedure_control.get_procedure_cur() == 2:
            anaserv = self.procedures[2].procedure_parameters['ClosingWidth']
            anaserv.set_v_out()

        new_pose = []
        if self.procedure_control.get_procedure_cur() == 1:
            width = self.procedures[1].procedure_parameters['OpeningWidth'].get_v_out()
            new_pose.append((width)/2)
            new_pose.append((width)/2)
        elif self.procedure_control.get_procedure_cur() == 2:
            width = self.procedures[2].procedure_parameters['ClosingWidth'].get_v_out()
            new_pose.append((width)/2)
            new_pose.append((width)/2)
        else:
            print("no valid Procedure ID")

        self.movetask.update_pose(new_pose=new_pose)
        self.state_change()
        return  
    
    def execute(self):
        """
        Execute state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Execute State!")
        if self.procedure_control.get_procedure_cur() == 1:
            self.movetask.open()
            self.state_change()
        elif self.procedure_control.get_procedure_cur() == 2:
            self.movetask.grasp()
            self.state_change()
        else:
            print("no valid Procedure ID")
        #self.state_change()
        return
        
    def completing(self):
        """
        Completing state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Completing State!")
        self.state_change()
        return
        
    def completed(self):
        """
        Completed state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Completed State!")
        return
        
    def pausing(self):
        """
        Pausing state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Pausing State!")
        self.state_change()
        return
          
    def paused(self):
        """
        Paused state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Paused State!")
        return
          
    def resuming(self):
        """
        Resuming state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Resuming State!")
        self.state_change()
        return
        
    def holding(self):
        """
        Holding state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Holding State!")
        self.state_change()
        return
        
    def held(self):
        """
        Held state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Held State!")
        return
        
    def unholding(self):
        """
        Unholding state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Unholding State!")
        self.state_change()
        return
        
    def stopping(self):
        """
        Stopping state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Stopping State!")
        self.state_change()
        return
        
    def stopped(self):
        """
        Stopped state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Stopped State!")
        self.state_change()
        return
        
    def aborting(self):
        """
        Aborting state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Aborting State!")
        self.state_change()
        return
        
    def aborted(self):
        """
        Aborted state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Aborted State!")
        return
        
    def resetting(self):
        """
        Resetting state.
        :return:
        """
        print(f"Service: {self.tag_name} with Procedure: {self.procedures[self.procedure_control.get_procedure_cur()].tag_name} in Resetting State!")
        self.state_change()
        return