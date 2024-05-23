using UnityEngine;
using RosMessageTypes.Geometry;

public static class RosConversions {
    // Konvertiere Unity Vector3 in ROS PointMsg im FLU-Koordinatensystem
    public static PointMsg To<FLU>(this Vector3 vector) {
        return new PointMsg(vector.z, -vector.x, vector.y);
    }

    // Konvertiere Unity Quaternion in ROS QuaternionMsg im FLU-Koordinatensystem
    public static QuaternionMsg To<FLU>(this Quaternion quaternion) {
        return new QuaternionMsg(-quaternion.z, quaternion.x, -quaternion.y, quaternion.w);
    }
}