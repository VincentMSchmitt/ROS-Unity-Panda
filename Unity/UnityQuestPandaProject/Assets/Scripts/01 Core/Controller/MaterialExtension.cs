/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using UnityEngine;

public static class MaterialExtensions {
    public static Color GetMaterialColor(Renderer renderer) {
        return renderer.material.color;
    }

    public static void SetMaterialColor(Material material, Color color) {
        material.color = color;
    }
}