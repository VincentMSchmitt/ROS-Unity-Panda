using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using RosMessageTypes.FrankaPandaMoveit;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

using static GameObjectFilter;

public class ObjectInfoPublisher : MonoBehaviour {
    [SerializeField] private bool sendCollisions = true;
    [SerializeField] private string rosTopicName = "object_info";
    [SerializeField] private float publishInterval = 1.0f;
    private ROSConnection ros;
    private List<GameObject> trackedGameObjects;
    private float timeSinceLastPublish;

    void Start() {
        // ROS Connector init
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ObjectInfoMsg>(rosTopicName);
        timeSinceLastPublish = 0.0f;
        print("Ready to avioid collisions.");
    }

    void Update() {
        if (sendCollisions) {
            timeSinceLastPublish += Time.deltaTime;

            if (timeSinceLastPublish >= publishInterval) {
                trackedGameObjects = GameObjectFilter.GetAllGameObjectsWithTag("track");
                // send each box on their own
                // TODO: pack them into one Message
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
            ObjectInfoMsg msg = new ObjectInfoMsg {
                name = objectInfo.name,
                position = objectInfo.position,
                rotation = objectInfo.rotation,
                size = new Vector3Msg(objectInfo.size.x, objectInfo.size.y, objectInfo.size.z)
            };
            // publish msg
            ros.Publish(rosTopicName, msg);
        }
    }

    public (string name, PointMsg position, QuaternionMsg rotation, Vector3 size) GetObjectInfo(GameObject go) {
        // get renderer of the GameObject
        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null) {
            string name = go.name;
            // Unity coordinates
            Vector3 unityPosition = go.transform.position;
            Quaternion unityRotation = go.transform.rotation;
            Vector3 size = renderer.bounds.size;

            // convert Unity coordinates into ROS coordinates (FLU)
            // TODO: This does not work as intended for some reason. For now the objects will be rotated
            // on the ROS side by 90 Deg
            PointMsg rosPosition = unityPosition.To<FLU>();
            QuaternionMsg rosRotation = unityRotation.To<FLU>();

            return (name, rosPosition, rosRotation, size);
        }
        // send empty message if no object renderer is found
        return (null, new PointMsg(), new QuaternionMsg(), Vector3.zero);
    }
}