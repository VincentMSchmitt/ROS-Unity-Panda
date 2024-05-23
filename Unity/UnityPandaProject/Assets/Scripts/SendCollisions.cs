using System.Collections.Generic;
using UnityEngine;

public class BoundingBoxDrawer : MonoBehaviour {

    // Serial field to control whether the bounding box should be drawn or not
    [SerializeField] private bool drawBoundingBox = true;

    // Serial field for setting the color of the bounding box
    [SerializeField] private Color boundingBoxColor = Color.green;

    private List<GameObject> trackedGameObjects;

    void Start() {
        // initialising list
        trackedGameObjects = new List<GameObject>();
    }

    void Update() {
        if (drawBoundingBox) {
            // find all GameObjects with Tag "track"
            trackedGameObjects = GameObjectFilter.GetAllGameObjectsWithTag("track");

            // draw bounding-boxs around the found GameObjects
            foreach (GameObject go in trackedGameObjects) {
                DrawBoundingBox(go);
            }
        }
    }

    void DrawBoundingBox(GameObject go) {
        // get the renderer component of the GameObjects
        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null) {
            // Calculate the corner points of the bounding box
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

            // Draw the lines of the bounding box
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

public static class GameObjectFilter {
    public static List<GameObject> GetAllGameObjectsWithTag(string tag) {
        // Create a list to save the filtered GameObjects
        List<GameObject> filteredGameObjects = new List<GameObject>();

        // Find all GameObjects with the specified tag
        GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag(tag);

        // Add the GameObjects found to the list
        filteredGameObjects.AddRange(gameObjectsWithTag);

        return filteredGameObjects;
    }
}