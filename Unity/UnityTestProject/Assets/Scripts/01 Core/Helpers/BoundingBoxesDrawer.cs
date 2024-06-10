using System.Collections.Generic;
using UnityEngine;

namespace Panda.Core {
    public class BoundingBoxDrawer : MonoBehaviour {
        public static bool drawBoundingBox = true;
        [SerializeField] Color boundingBoxColor = Color.green;
        private List<GameObject> trackedGameObjects;

        public static void SetToogle(bool value) {
            drawBoundingBox = value;
        }

        private void Start() {
            trackedGameObjects = new List<GameObject>();
        }

        /// <summary>
        /// Updates bounding boxes every frame.
        /// TODO: only update if the object is moved or changed its location.
        /// </summary>
        private void Update() {
            if (drawBoundingBox) {
                // find all GameObjects with tag "track"
                trackedGameObjects = GameObjectFilter.GetAllGameObjectsWithTag("track");

                // draw bounding-boxes for found GameObjects
                foreach (GameObject go in trackedGameObjects) {
                    DrawBoundingBox(go);
                }
            }
        }

        private void DrawBoundingBox(GameObject go) {
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
}