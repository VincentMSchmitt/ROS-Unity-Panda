using System;
using UnityEngine;

public class EventManager : MonoBehaviour {
    public static EventManager Instance { get; private set; }

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    public event Action OnStartupComplete;

    // Make sure, startup is completed befor doing anything else
    public void TriggerStartupComplete() {
        if (OnStartupComplete != null) {
            OnStartupComplete.Invoke();
        }
    }
}