using UnityEngine;

public static class MaterialExtensions {
    public static Color GetMaterialColor(Renderer renderer) {
        return renderer.material.color;
    }

    public static void SetMaterialColor(Material material, Color color) {
        material.color = color;
    }
}