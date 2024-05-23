using System.Collections.Generic;
using UnityEngine;

public class BoundingBoxDrawer : MonoBehaviour {
    
    // Serielles Feld zur Steuerung, ob die Bounding-Box gezeichnet werden soll oder nicht
    [SerializeField] private bool drawBoundingBox = true;

    // Serielles Feld zur Einstellung der Farbe der Bounding-Box
    [SerializeField] private Color boundingBoxColor = Color.green;

    private List<GameObject> trackedGameObjects;

    void Start() {
        // Initialisierung der Liste
        trackedGameObjects = new List<GameObject>();
    }

    void Update() {
        if (drawBoundingBox) {
            // Finde alle GameObjects mit dem Tag "track"
            trackedGameObjects = GameObjectFilter.GetAllGameObjectsWithTag("track");

            // Zeichne Bounding-Boxen um die gefundenen GameObjects
            foreach (GameObject go in trackedGameObjects) {
                DrawBoundingBox(go);
            }
        }
    }

    void DrawBoundingBox(GameObject go) {
        // Hole das Renderer-Component des GameObjects
        Renderer renderer = go.GetComponent<Renderer>();

        if (renderer != null) {
            // Berechne die Eckpunkte der Bounding-Box
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

            // Zeichne die Linien der Bounding-Box
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