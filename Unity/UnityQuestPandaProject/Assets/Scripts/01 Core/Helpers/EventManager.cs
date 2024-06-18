using System;
using UnityEngine;

namespace Panda.Core {
    /// <summary>
    /// Manages game events and provides a centralized way to subscribe to and trigger events.
    /// </summary>
    public class EventManager : MonoBehaviour {
        public static EventManager GetInstance { get; private set; } // Singleton instance

        /// <summary>
        /// Ensures only one instance of this class exists. If an instance exists and it is not this instance, the
        /// current game object is destroyed to enforce the singleton property. If no instance exists, this instance is
        /// assigned to the static Instance property.
        /// </summary>
        void Awake() {
            if (GetInstance == null) {
                GetInstance = this;
                DontDestroyOnLoad(gameObject);
            }
            else {
                Destroy(gameObject);
            }
        }

        public event Action OnStartupComplete;

        public void TriggerStartupComplete() {
            // Check if any subscribers exist before invoking the event
            if (OnStartupComplete != null) {
                OnStartupComplete.Invoke();
            }
        }
    }
}