using UnityEngine;
using System.Collections.Generic;

public class DirectKinematics {
    static readonly float M_PI = Mathf.PI;

    public static Vector3 GetTCPPosition(Matrix4x4 matrix) {
        // position is saved in column 3 of the transformation matrix
        Vector3 position = matrix.GetColumn(3);
        return position;
    }

    public static Matrix4x4 ForwardKinematics(float[] jointAngles) {
        float[,] dhParameters = DHParams(jointAngles);

        Matrix4x4 T_01  = TFMatrix(0, dhParameters);
        Matrix4x4 T_12  = TFMatrix(1, dhParameters);
        Matrix4x4 T_23  = TFMatrix(2, dhParameters);
        Matrix4x4 T_34  = TFMatrix(3, dhParameters);
        Matrix4x4 T_45  = TFMatrix(4, dhParameters);
        Matrix4x4 T_56  = TFMatrix(5, dhParameters);
        Matrix4x4 T_67  = TFMatrix(6, dhParameters);
        Matrix4x4 T_7F  = TFMatrix(7, dhParameters);
        Matrix4x4 T_FEE = TFMatrix(8, dhParameters);

        // TODO: check if this kinematics chain is calculated correctly
        // calculate kinematic chain for the EE transformation matrix
        Matrix4x4 T_EE = T_01 * T_12 * T_23 * T_34 * T_45 * T_56 * T_67 * T_7F * T_FEE;

        return T_EE;
    }

    static float[,] DHParams(float[] jointAngles) {
        // DH-Parameter: https://www.researchgate.net/publication/357238256_Analytical_Inverse_Kinematics_for_Franka_Emika_Panda_-_a_Geometrical_Solver_for_7-DOF_Manipulators_with_Unconventional_Design
        // DH-Parameters: alpha = link twist, a = link length, d = offset, theta = joint angle
        float[,] dh = new float[,] {
        //   alpha(rad)    a(m)     d(m)     theta(rad)
            {        0,        0,  0.333f, jointAngles[0]}, // joint 1
            {-M_PI / 2,        0,       0, jointAngles[1]}, // joint 2
            { M_PI / 2,        0,  0.316f, jointAngles[2]}, // joint 3
            { M_PI / 2,  0.0825f,       0, jointAngles[3]}, // joint 4
            {-M_PI / 2, -0.0825f,  0.384f, jointAngles[4]}, // joint 5
            { M_PI / 2,        0,       0, jointAngles[5]}, // joint 6
            { M_PI / 2,   0.088f,       0, jointAngles[6]}, // joint 7
            {        0,        0,  0.107f,              0}, // flanch (F)
            {        0,        0, 0.1034f,       M_PI / 4}  // end effector (EE)
        };
        return dh;
    }

    static Matrix4x4 TFMatrix(int i, float[,] dh) {
        // Define transformation matrix based on DH parameters
        float alpha = dh[i, 0];
        float a     = dh[i, 1];
        float d     = dh[i, 2];
        float theta = dh[i, 3];

        // standard DH-matrix for a 3D pose with the parameters: alpha, a, d, theta
        // Each row of the matrix is set according to DH conventions:
        //  * First row:  Sets the values for the rotation around the Z-axis and the shift along the X-axis.
        //  * Second row: Sets the values for the rotation around the Z-axis and the shift along the Y-axis.
        //  * Third line: Sets the values for the displacement along the Z-axis.
        //  * Fourth row: Sets the homogeneity condition of the matrix.
        Matrix4x4 TF = Matrix4x4.identity;
        TF.SetRow(0, new Vector4(Mathf.Cos(theta),  -Mathf.Sin(theta) * Mathf.Cos(alpha),  Mathf.Sin(theta) * Mathf.Sin(alpha), a * Mathf.Cos(theta)));
        TF.SetRow(1, new Vector4(Mathf.Sin(theta),   Mathf.Cos(theta) * Mathf.Cos(alpha), -Mathf.Cos(theta) * Mathf.Sin(alpha), a * Mathf.Sin(theta)));
        TF.SetRow(2, new Vector4(               0,                      Mathf.Sin(alpha),                     Mathf.Cos(alpha),                    d));
        TF.SetRow(3, new Vector4(               0,                                     0,                                    0,                    1));

        return TF;
    }
}