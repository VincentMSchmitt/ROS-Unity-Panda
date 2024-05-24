using System.Collections.Generic;
using UnityEngine;

public class BoundingBoxDrawer : MonoBehaviour {
    [SerializeField] private bool drawBoundingBox = true;
    [SerializeField] private Color boundingBoxColor = Color.green;
    private List<GameObject> trackedGameObjects;

    void Start() {
        trackedGameObjects = new List<GameObject>();
    }

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
        // get renderer of the GameObject
        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null) {
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

public static class GameObjectFilter {
    public static List<GameObject> GetAllGameObjectsWithTag(string tag) {
        // Erstelle eine Liste, um die gefilterten GameObjects zu speichern
        List<GameObject> filteredGameObjects = new List<GameObject>();

        // Finde alle GameObjects mit dem angegebenen Tag
        GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag(tag);

        // Füge die gefundenen GameObjects der Liste hinzu
        filteredGameObjects.AddRange(gameObjectsWithTag);

        return filteredGameObjects;
    }
}