using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.FrankaPandaMoveit;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

using static GameObjectFilter;

public class ObjectInfoPublisher : MonoBehaviour {

    // Serielles Feld zur Steuerung, ob die Kollisionen gesendet werden sollen oder nicht
    [SerializeField] private bool sendCollisions = true;

    // Name des ROS-Themas
    [SerializeField] private string rosTopicName = "object_info";

    // Serielles Feld zur Steuerung des Intervalls in Sekunden
    [SerializeField] private float publishInterval = 1.0f;

    // ROS Connector
    private ROSConnection ros;

    private List<GameObject> trackedGameObjects;

    private float timeSinceLastPublish;

    void Start() {
        // ROS Connector initialisieren
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ObjectInfoMsg>(rosTopicName);
        timeSinceLastPublish = 0.0f;
        print("Ready to avioid collisions.")
    }

    void Update() {
        if (sendCollisions) {
            timeSinceLastPublish += Time.deltaTime;

            if (timeSinceLastPublish >= publishInterval) {
                trackedGameObjects = GameObjectFilter.GetAllGameObjectsWithTag("track");
                // senden der einzelnen boxen
                foreach (GameObject go in trackedGameObjects) {
                    PublishObjectInfo(go);
                }
                timeSinceLastPublish = 0.0f;
            }
        }
    }

    public void PublishObjectInfo(GameObject go) {
        var objectInfo = GetObjectInfo(go);

        if (objectInfo.name != null) {
            // Erstelle eine neue ObjectInfoMsg
            ObjectInfoMsg msg = new ObjectInfoMsg {
                name = objectInfo.name,
                position = objectInfo.position,
                rotation = objectInfo.rotation,
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
            // TODO:Check if this works as intended
            PointMsg rosPosition = unityPosition.To<FLU>();
            QuaternionMsg rosRotation = unityRotation.To<FLU>();


            return (name, rosPosition, rosRotation, size);
        }
        return (null, new PointMsg(), new QuaternionMsg(), Vector3.zero);
    }
}