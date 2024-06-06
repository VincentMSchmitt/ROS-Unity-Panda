using System;
using UnityEngine;

namespace Panda.Core {
    /// <summary>
    /// Manages game events and provides a centralized way to subscribe to and trigger events.
    /// </summary>
    public class EventManager : MonoBehaviour {
        // Singleton instance
        public static EventManager Instance { get; private set; }

        /// <summary>
        /// Ensures only one instance of this class exists. If an instance exists
        /// and it is not this instance, the current game object is destroyed to
        /// enforce the singleton property. If no instance exists, this instance
        /// is assigned to the static Instance property.
        /// </summary>
        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Event that is triggered when startup is completed.
        /// </summary>
        public event Action OnStartupComplete;

        /// <summary>
        /// Triggers the startup complete event. This makes sure that startup is
        /// completed before doing anything else.
        /// </summary>
        // 
        public void TriggerStartupComplete() {
            // Check if any subscribers exist before invoking the event
            if (OnStartupComplete != null) {
                OnStartupComplete.Invoke();
            }
        }
    }
}