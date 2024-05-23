using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

public class ObjectInfoPublisher : MonoBehaviour {
    // Name des ROS-Themas
    [SerializeField] private string rosTopicName = "object_info";

    // ROS Connector
    private ROSConnection ros;

    void Start() {
        // ROS Connector initialisieren
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ObjectInfoMsg>(rosTopicName);
    }

    public void PublishObjectInfo(GameObject go) {
        var objectInfo = GetObjectInfo(go);

        if (objectInfo.name != null) {
            // Erstelle eine neue ObjectInfoMsg
            ObjectInfoMsg msg = new ObjectInfoMsg {
                name = objectInfo.name,
                position = objectInfo.position.To<FLU>(),
                rotation = objectInfo.rotation.To<FLU>(),
                size = new Vector3Msg(objectInfo.size.x, objectInfo.size.y, objectInfo.size.z)
            };

            // Nachricht veröffentlichen
            ros.Publish(rosTopicName, msg);
        }
    }

    public (string name, PointMsg position, QuaternionMsg rotation, Vector3 size) GetObjectInfo(GameObject go) {
        // Hole das Renderer-Component des GameObjects
        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null) {
            // Name des GameObjects
            string name = go.name;

            // Position in Unity-Koordinaten
            Vector3 unityPosition = go.transform.position;
            // Rotation in Unity-Koordinaten
            Quaternion unityRotation = go.transform.rotation;
            // Größe des GameObjects
            Vector3 size = renderer.bounds.size;

            // Konvertiere Unity-Koordinaten in ROS-Koordinaten (FLU)
            PointMsg rosPosition = unityPosition.To<FLU>();
            QuaternionMsg rosRotation = unityRotation.To<FLU>();

            return (name, rosPosition, rosRotation, size);
        }

        return (null, new PointMsg(), new QuaternionMsg(), Vector3.zero);
    }
}