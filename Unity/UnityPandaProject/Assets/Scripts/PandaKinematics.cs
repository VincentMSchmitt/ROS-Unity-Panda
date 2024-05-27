using UnityEngine;
using System.Collections.Generic;

[System.Serializable] public class DHParameter {
    public string jointName;
    public float alpha;
    public float a;
    public float d;
    public float theta;
}

public class PandaKinematics : MonoBehaviour {
    // Denavit-Hartenberg-Parameter für die Gelenke des Panda-Roboters
    // https://frankaemika.github.io/docs/control_parameters.html#denavithartenberg-parameters
    // can be extracted from the urdf also
    private static readonly List<DHParameter> DHParameters;

    static PandaKinematics() {
        DHParameters = new List<DHParameter> {
            new DHParameter { jointName = "panda_joint1", alpha = 0,               a = 0,       d = 0.333f,  theta = 0 }, // Joint 1
            new DHParameter { jointName = "panda_joint2", alpha = -Mathf.PI / 2,   a = 0,       d = 0,       theta = 0 }, // Joint 2
            new DHParameter { jointName = "panda_joint3", alpha = Mathf.PI / 2,    a = 0,       d = 0.316f,  theta = 0 }, // Joint 3
            new DHParameter { jointName = "panda_joint4", alpha = Mathf.PI / 2,    a = 0.0825f, d = 0,       theta = 0 }, // Joint 4
            new DHParameter { jointName = "panda_joint5", alpha = -Mathf.PI / 2,   a = -0.0825f,d = 0.384f,  theta = 0 }, // Joint 5
            new DHParameter { jointName = "panda_joint6", alpha = Mathf.PI / 2,    a = 0,       d = 0,       theta = 0 }, // Joint 6
            new DHParameter { jointName = "panda_joint7", alpha = Mathf.PI / 2,    a = 0.088f,  d = 0,       theta = 0 }  // Joint 7
        };
    }

    // Function to calculate joint transformation using DH parameters
    public static Matrix4x4 CalculateJointTransformation(int jointIndex, float jointAngle) {
        if (jointIndex < 0 || jointIndex >= DHParameters.Count) {
            Debug.LogError("Invalid joint index.");
            return Matrix4x4.identity;
        }

        // Extrahiere die Denavit-Hartenberg-Parameter für das entsprechende Gelenk
        DHParameter dh = DHParameters[jointIndex];
        float alpha = dh.alpha;
        float a = dh.a;
        float d = dh.d;
        float theta = dh.theta + jointAngle;

        // Berechne die Transformation mithilfe der Denavit-Hartenberg-Parameter
        Matrix4x4 transformationMatrix = Matrix4x4.identity;
        transformationMatrix.SetRow(0, new Vector4(Mathf.Cos(theta), -Mathf.Sin(theta) * Mathf.Cos(alpha), Mathf.Sin(theta) * Mathf.Sin(alpha), a * Mathf.Cos(theta)));
        transformationMatrix.SetRow(1, new Vector4(Mathf.Sin(theta), Mathf.Cos(theta) * Mathf.Cos(alpha), -Mathf.Cos(theta) * Mathf.Sin(alpha), a * Mathf.Sin(theta)));
        transformationMatrix.SetRow(2, new Vector4(0, Mathf.Sin(alpha), Mathf.Cos(alpha), d));
        transformationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        return transformationMatrix;
    }
}