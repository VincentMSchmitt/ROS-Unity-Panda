using RosMessageTypes.FrankaPandaMoveit;
using RosMessageTypes.Geometry;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

using Panda.Utility;

namespace Panda.Ros {
    public class ObjectInfoPublisher : MonoBehaviour {
        [Tooltip("If true, the object information will be sent for collision avoidance.")]
        [SerializeField] public bool sendCollisions = true;

        [Tooltip("The name of the ROS topic to publish the object information to.")]
        [SerializeField] public string rosTopicName = "object_info";

        [Tooltip("The interval (in seconds) at which to publish the object information.")]
        [SerializeField] public float publishInterval = 1.0f;

        private ROSConnection ros;
        private List<GameObject> trackedGameObjects;
        private float timeSinceLastPublish;

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update
        /// methods are called the first time. Initialize the ROS connection and set up
        /// the publisher.
        /// </summary>
        void Start() {
            // ROS Connector init
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<ObjectInfoMsg>(rosTopicName);
            timeSinceLastPublish = 0.0f;
            print("Ready to avioid collisions.");
        }

        /// <summary>
        /// Publishes the object information at the specified interval.
        /// </summary>
        void Update() {
            if (sendCollisions) {
                timeSinceLastPublish += Time.deltaTime;

                if (timeSinceLastPublish >= publishInterval) {
                    // Find all game objects with the "track" tag
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

        /// <summary>
        /// Publishes the information of a single object to ROS.
        /// </summary>
        /// <param name="go">The game object to publish information about.</param>
        public void PublishObjectInfo(GameObject go) {
            var objectInfo = GetObjectInfo(go);

            if (objectInfo.name != null) {
                ObjectInfoMsg msg = new ObjectInfoMsg {
                    name = objectInfo.name,
                    position = objectInfo.position,
                    rotation = objectInfo.rotation,
                    size = new Vector3Msg(objectInfo.size.x, objectInfo.size.y, objectInfo.size.z)
                };
                // publish the message
                ros.Publish(rosTopicName, msg);
            }
        }

        /// <summary>
        /// Retrieves the information of a single object, including its position, rotation, and size.
        /// </summary>
        /// <param name="go">The game object to retrieve information from.</param>
        /// <returns>A tuple containing the name, position, rotation, and size of the object.</returns>
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
                // TODO: This does not work as intended for some reason. For now
                // the objects will be rotated by 90 Deg on the ROS side
                PointMsg rosPosition = unityPosition.To<FLU>();
                QuaternionMsg rosRotation = unityRotation.To<FLU>();

                return (name, rosPosition, rosRotation, size);
            }
            // send empty message if no object renderer is found
            return (null, new PointMsg(), new QuaternionMsg(), Vector3.zero);
        }
    }
}