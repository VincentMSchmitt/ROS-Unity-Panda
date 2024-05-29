using System.Collections.Generic;
using UnityEngine;

namespace Panda.Utility {
    /// <summary>
    /// Class for drawing bounding boxes around GameObjects tagged as "track".
    /// </summary>
    public class BoundingBoxDrawer : MonoBehaviour {
        [Tooltip("Toggle if the bounding boxes should be drawn.")]
        [SerializeField] public bool drawBoundingBox = true;

        [Tooltip("The color, that the bounding box will have.")]
        [SerializeField] public Color boundingBoxColor = Color.green;
        private List<GameObject> trackedGameObjects;

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first
        /// time. Initialize a list to hold all the found GameObjects.
        /// </summary>
        void Start() {
            trackedGameObjects = new List<GameObject>();
        }

        /// <summary>
        /// Updates bounding boxes every frame.
        /// TODO: only update if the object is moved or changed its location.
        /// </summary>
        void Update() {
            if (drawBoundingBox) {
                // find all GameObjects with tag "track"
                trackedGameObjects = GameObjectFilter.GetAllGameObjectsWithTag("track");

                // draw bounding-boxes for found GameObjects
                foreach (GameObject go in trackedGameObjects) {
                    DrawBoundingBox(go);
                }
            }
        }

        void DrawBoundingBox(GameObject go) {
            // if renderer of GameObject is found
            if (go.TryGetComponent<Renderer>(out var renderer)) {
                // calculate die corners of the bounding box
                Vector3 center = renderer.bounds.center;
                Vector3 extents = renderer.bounds.extents;

                Vector3 v3FrontTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);
                Vector3 v3FrontTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);
                Vector3 v3FrontBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z);
                Vector3 v3FrontBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);

                Vector3 v3BackTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);
                Vector3 v3BackTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z);
                Vector3 v3BackBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);
                Vector3 v3BackBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);

                // draw
                Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, boundingBoxColor);
                Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, boundingBoxColor);
                Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, boundingBoxColor);
                Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, boundingBoxColor);

                Debug.DrawLine(v3BackTopLeft, v3BackTopRight, boundingBoxColor);
                Debug.DrawLine(v3BackTopRight, v3BackBottomRight, boundingBoxColor);
                Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, boundingBoxColor);
                Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, boundingBoxColor);

                Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, boundingBoxColor);
                Debug.DrawLine(v3FrontTopRight, v3BackTopRight, boundingBoxColor);
                Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, boundingBoxColor);
                Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, boundingBoxColor);
            }
        }
    }

    /// <summary>
    /// Class for filtering GameObject.
    /// </summary>
    public static class GameObjectFilter {
        /// <summary>
        /// Filters a given list of GameObject and returns all GameObjects with a given tag.
        /// </summary>
        /// <param name="tag">The tag which the function should look for.</param>
        /// <returns>A list of the found GameObjects</returns>
        public static List<GameObject> GetAllGameObjectsWithTag(string tag) {
            // Create a list to store the filtered GameObjects
            List<GameObject> filteredGameObjects = new List<GameObject>();

            // Find all GameObjects with the specified tag
            GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag(tag);

            // Add the found GameObjects to the list
            filteredGameObjects.AddRange(gameObjectsWithTag);

            return filteredGameObjects;
        }
    }
}