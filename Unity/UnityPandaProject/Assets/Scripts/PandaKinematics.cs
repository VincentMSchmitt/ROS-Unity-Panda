using UnityEngine;
using System.Collections.Generic;

[System.Serializable] public class DHParameter {
    public string jointName;
    public float alpha;
    public float a;
    public float d;
    public float theta;
}

public class PandaKinematics {
    // Denavit-Hartenberg parameters for the joints of the Panda robot
    private static readonly List<DHParameter> DHParameters;

    static PandaKinematics() {
        DHParameters = new List<DHParameter> {
            new DHParameter { jointName = "panda_joint1", alpha = 0,               a = 0,       d = 0.333f,  theta = 0 }, // Joint 1
            new DHParameter { jointName = "panda_joint2", alpha = -Mathf.PI / 2,   a = 0,       d = 0,       theta = 0 }, // Joint 2
            new DHParameter { jointName = "panda_joint3", alpha =  Mathf.PI / 2,   a = 0,       d = 0.316f,  theta = 0 }, // Joint 3
            new DHParameter { jointName = "panda_joint4", alpha =  Mathf.PI / 2,   a = 0.0825f, d = 0,       theta = 0 }, // Joint 4
            new DHParameter { jointName = "panda_joint5", alpha = -Mathf.PI / 2,   a = -0.0825f,d = 0.384f,  theta = 0 }, // Joint 5
            new DHParameter { jointName = "panda_joint6", alpha =  Mathf.PI / 2,   a = 0,       d = 0,       theta = 0 }, // Joint 6
            new DHParameter { jointName = "panda_joint7", alpha =  Mathf.PI / 2,   a = 0.088f,  d = 0,       theta = 0 }  // Joint 7
        };
    }

    /// <summary>
    /// Calculates the entire transformation from the base frame of the robot to the end effector
    /// </summary>
    /// <param name="jointAngles"></param>
    /// <returns>Matrix4x4</returns>
    public static Matrix4x4 CalculateEndEffectorTransformation(float[] jointAngles) {
        // checks whether the number of joint angles matches the number of DH parameters
        if (jointAngles.Length != DHParameters.Count) {
            Debug.LogError("Invalid number of joint angles.");
            return Matrix4x4.identity;
        }

        // is initialized as a unit matrix
        Matrix4x4 endEffectorTransformation = Matrix4x4.identity;

        // The transformation matrix is calculated for each joint and multiplied to create the overall transformation
        for (int i = 0; i < jointAngles.Length; ++i) {
            Matrix4x4 jointTransformation = CalculateJointTransformation(i, jointAngles[i]);
            endEffectorTransformation *= jointTransformation;
            //Debug.Log($"Joint {i + 1} Transformation Matrix:\n{MatrixToString(jointTransformation)}");
        }

        // Adding the fixed transformation (Rotation around the z-axis by -45 degrees) for the panda_hand_joint
        Matrix4x4 pandaHandJointTransformation = Matrix4x4.identity;
        pandaHandJointTransformation.SetTRS(new Vector3(0, 0, 0), Quaternion.Euler(0, 0, -45), Vector3.one);
        endEffectorTransformation *= pandaHandJointTransformation;

        return endEffectorTransformation;
    }

    /// <summary>
    /// Function to calculate joint transformation using DH parameters
    /// </summary>
    /// <param name="jointIndex"></param>
    /// <param name="jointAngle"></param>
    /// <returns></returns>
    public static Matrix4x4 CalculateJointTransformation(int jointIndex, float jointAngle) {
        if (jointIndex < 0 || jointIndex >= DHParameters.Count) {
            Debug.LogError("Invalid joint index.");
            return Matrix4x4.identity;
        }

        // Extract the Denavit-Hartenberg parameters for the corresponding joint
        DHParameter dh = DHParameters[jointIndex];
        float alpha = dh.alpha;
        float a = dh.a;
        float d = dh.d;
        float theta = dh.theta + jointAngle;

        // Calculate the transformation using the Denavit-Hartenberg parameters
        // Each row of the matrix is set according to DH conventions:
        //  * First row:  Sets the values for the rotation around the Z-axis and the shift along the X-axis.
        //  * Second row: Sets the values for the rotation around the Z-axis and the shift along the Y-axis.
        //  * Third line: Sets the values for the displacement along the Z-axis.
        //  * Fourth row: Sets the homogeneity condition of the matrix.

        Matrix4x4 transformationMatrix = Matrix4x4.identity;
        transformationMatrix.SetRow(0, new Vector4(Mathf.Cos(theta), -Mathf.Sin(theta) * Mathf.Cos(alpha), Mathf.Sin(theta) * Mathf.Sin(alpha), a * Mathf.Cos(theta)));
        transformationMatrix.SetRow(1, new Vector4(Mathf.Sin(theta), Mathf.Cos(theta) * Mathf.Cos(alpha), -Mathf.Cos(theta) * Mathf.Sin(alpha), a * Mathf.Sin(theta)));
        transformationMatrix.SetRow(2, new Vector4(0, Mathf.Sin(alpha), Mathf.Cos(alpha), d));
        transformationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        return transformationMatrix;
    }

    private static string MatrixToString(Matrix4x4 matrix) {
        return  $"{matrix[0, 0]} {matrix[0, 1]} {matrix[0, 2]} {matrix[0, 3]}\n" +
                $"{matrix[1, 0]} {matrix[1, 1]} {matrix[1, 2]} {matrix[1, 3]}\n" +
                $"{matrix[2, 0]} {matrix[2, 1]} {matrix[2, 2]} {matrix[2, 3]}\n" +
                $"{matrix[3, 0]} {matrix[3, 1]} {matrix[3, 2]} {matrix[3, 3]}";
    }
}