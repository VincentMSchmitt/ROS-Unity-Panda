using System;
using UnityEngine;

namespace Panda.Calculate {
    /// <summary>
    /// Class for performing direct kinematics calculations for the Franka Emika Panda nonstandard robotic arm (7DOF).
    /// </summary>
    public class PandaKinematics {
        static readonly float M_PI = Mathf.PI;

        /// <summary>
        /// Extracts the TCP (Tool Center Point) position from a transformation matrix.
        /// </summary>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The TCP position as a Vector3.</returns>
        public static Vector3 GetTCPPosition(float[] jointAngles) {
            Matrix4x4 transformationMatrix = ForwardKinematics(jointAngles);
            // position is saved in column 3 of the transformation matrix
            Vector3 position = transformationMatrix.GetColumn(3);
            return position;
        }

        /// <summary>
        /// Extracts the TCP (Tool Center Point) rotation from a transformation matrix.
        /// </summary>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The TCP rotation as a Quaternion.</returns>
        public static Quaternion GetTCPRotation(float[] jointAngles) {
            Matrix4x4 transformationMatrix = ForwardKinematics(jointAngles);
            // rotation is saved in the upper-left 3x3 submatrix of the transformation matrix
            Quaternion rotation = Quaternion.LookRotation(transformationMatrix.GetColumn(2), transformationMatrix.GetColumn(1));
            return rotation;
        }

        /// <summary>
        /// Computes the forward kinematics for the given joint angles.
        /// </summary>
        /// <param name="jointAngles">Array of joint angles.</param>
        /// <returns>The end effector transformation matrix.</returns>
        private static Matrix4x4 ForwardKinematics(float[] jointAngles) {
            float[,] dhParameters = DHParams(jointAngles);

            // calculate kinematic chain for the EE transformation matrix
            Matrix4x4 transformationMatrix = Matrix4x4.identity;
            for (int i = 0; i < dhParameters.GetLength(0); ++i) {
                transformationMatrix *= TFMatrix(i, dhParameters);
            }
            return transformationMatrix;
        }

        /// <summary>
        /// Generates the DH parameters for the given joint angles.
        /// </summary>
        /// <param name="jointAngles">Array of joint angles.</param>
        /// <returns>Matrix of the DH parameters.</returns>
        private static float[,] DHParams(float[] jointAngles) {
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

        /// <summary>
        /// Computes the transformation matrix for a given joint based on its DH parameters.
        /// </summary>
        /// <param name="i">Index of the joint.</param>
        /// <param name="dh">Array of DH parameters.</param>
        /// <returns>The transformation matrix for the specified joint.</returns>
        private static Matrix4x4 TFMatrix(int i, float[,] dh) {
            // Check if the input parameters are valid
            if (dh == null || i < 0 || i >= dh.GetLength(0)) {
                throw new ArgumentException("Invalid input parameters");
            }

            // Define transformation matrix based on DH parameters
            float alpha = dh[i, 0];
            float a = dh[i, 1];
            float d = dh[i, 2];
            float theta = dh[i, 3];

            // standard DH-matrix for a 3D pose with the parameters: alpha, a, d, theta
            Matrix4x4 TF = Matrix4x4.identity;
            TF.SetRow(0, new Vector4(Mathf.Cos(theta), -Mathf.Sin(theta) * Mathf.Cos(alpha), Mathf.Sin(theta) * Mathf.Sin(alpha), a * Mathf.Cos(theta)));
            TF.SetRow(1, new Vector4(Mathf.Sin(theta), Mathf.Cos(theta) * Mathf.Cos(alpha), -Mathf.Cos(theta) * Mathf.Sin(alpha), a * Mathf.Sin(theta)));
            TF.SetRow(2, new Vector4(0, Mathf.Sin(alpha), Mathf.Cos(alpha), d));
            TF.SetRow(3, new Vector4(0, 0, 0, 1));

            return TF;
        }
    }
}