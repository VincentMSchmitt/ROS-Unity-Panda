#!/usr/bin/env python3
# Copyright (C) 2024 Gary Hildebrand, Vincent Schmitt - All Rights Reserved
# You may use, distribute and modify this code under the
# terms of the Educational Community License (ECL), Version 2.0.
# 
# You should have received a copy of the ECL license with
# this file. If not, please write to: schmittv@hs-pforzheim.de,
# or visit: https://opensource.org/licenses/ECL-2.0

from datetime import datetime
import sys
import rospy
import moveit_commander
from mtppy.opcua_server_pea import OPCUAServerPEA
from mtppy.mtp_generator import MTPGenerator
from services.moveService import mtpMoveService
from services.handService import mtpHandService
#from services.followerService import FollowerService
#from services.pickAndPlaceService import PickAndPlaceService

def main():
    try:
        ### MTP File Generation
        writer_info_dict = {
                             'WriterName': 'PforzheimUniversity/Engineerium', 'WriterID': 'PforzheimUniversity/Engineerium', 'WriterVendor': 'PforzheimUniversity',
                             'WriterVendorURL': 'www.hs-pforzheim.de',
                             'WriterVersion': '1.0.0', 'WriterRelease': '', 'LastWritingDateTime': str(datetime.now()),
                             'WriterProjectTitle': 'PforzheimUniversity/Engineerium/mtp-unity', 'WriterProjectID': ''
                            }
        export_manifest_path = '../aml/robots_manifest.aml'
        manifest_template_path = '../aml/manifest_template.xml'  
        mtp_generator = MTPGenerator(writer_info_dict, export_manifest_path, manifest_template_path=manifest_template_path)

        ### Defining a virtual PEA (process equipment assembly == modul) for the Franka Emika Robot
        robot = OPCUAServerPEA(mtp_generator=mtp_generator,endpoint='opc.tcp://127.0.0.1:4840/')

        ### Setting up ROS environment
        moveit_commander.roscpp_initialize(sys.argv)
        rospy.init_node('mtp_panda_robot')

        ### Add services
        move_service = mtpMoveService(tag_name="Move-Service", tag_description='')
        hand_service = mtpHandService(tag_name="Hand-Service", tag_description='')

        robot.add_service(move_service)
        robot.add_service(hand_service)

        ### run the server
        robot.run_opcua_server()
        rospy.spin()

    except:
        if robot is not None:
            robot.stop_opcua_server()
            rospy.logerr("There is no active OPCUA-Server PEA for the robot.")
        sys.exit(1)

if __name__ == "__main__":
    main()