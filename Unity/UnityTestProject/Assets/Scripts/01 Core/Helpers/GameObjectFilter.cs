using System.Collections.Generic;
using UnityEngine;

namespace Panda.Core {
    public static class GameObjectFilter {
        /// <summary>
        /// Filters a given list of GameObject and returns all GameObjects with a given tag.
        /// </summary>
        /// <param name="tag">The tag which the function should look for.</param>
        /// <returns>A list of the found GameObjects</returns>
        public static List<GameObject> GetAllGameObjectsWithTag(string tag) {
            List<GameObject> filteredGameObjects = new List<GameObject>();
            GameObject[] gameObjectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            filteredGameObjects.AddRange(gameObjectsWithTag);
            return filteredGameObjects;
        }
    }
}