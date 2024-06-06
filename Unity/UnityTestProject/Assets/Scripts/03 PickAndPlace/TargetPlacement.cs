using System;
using UnityEngine;

namespace Panda.PickAndPlace {
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BoxCollider))]
    public class TargetPlacement : MonoBehaviour {
        public enum PlacementState { Outside, InsideFloating, InsidePlaced }
        [SerializeField] GameObject target;
        
        [Tooltip("Alpha value for any color set during state changes.")]
        [Range(0, 255)]
        [SerializeField] int colorAlpha = 100;

        static readonly int shaderColorId = Shader.PropertyToID("_Color");
        
        private const string nameExpectedTarget = "Target";
        private const float maximumSpeedForStopped = 0.01f; // The threshold that the Target's speed must be under to be considered "placed" in the target area
        private float colorAlpha01 => colorAlpha / 255f;
        private MeshRenderer targetMeshRenderer;
        private MeshRenderer meshRenderer;
        private BoxCollider boxCollider;
        private PlacementState currentState;
        private PlacementState lastColoredState;

        public PlacementState CurrentState {
            get => currentState;
            private set {
                currentState = value;
                UpdateStateColor();
            }
        }

        // Start is called before the first frame update
        void Start() {
            // Check for misconfigurations and disable if something has changed without this script being updated
            // These are warnings because this script does not contain critical functionality
            if (target == null) {
                target = GameObject.Find(nameExpectedTarget);
            }
            if (target == null) {
                Debug.LogWarning($"{nameof(TargetPlacement)} expects to find a GameObject named " +
                    $"{nameExpectedTarget} to track, but did not. Can't track placement state.");
                enabled = false;
                return;
            }
            if (!TrySetComponentReferences()) {
                enabled = false;
                return;
            }
            InitializeState();
        }

        bool TrySetComponentReferences() {
            targetMeshRenderer = target.GetComponent<MeshRenderer>();
            if (targetMeshRenderer == null) {
                Debug.LogWarning($"{nameof(TargetPlacement)} expects a {nameof(MeshRenderer)} to be attached " +
                    $"to {nameExpectedTarget}. Cannot check bounds without it, so cannot track placement state.");
                return false;
            }

            // Assume these are here because they are RequiredComponent components
            meshRenderer = GetComponent<MeshRenderer>();
            boxCollider = GetComponent<BoxCollider>();
            return true;
        }

        void OnValidate() {
            // Useful for visualizing state in editor, but doesn't wholly guarantee accurate coloring in EditMode
            // Enter PlayMode to see color update correctly
            if (target != null) {
                if (TrySetComponentReferences()) {
                    InitializeState();
                }
            }
        }

        void InitializeState() {
            if (target.GetComponent<BoxCollider>().bounds.Intersects(boxCollider.bounds)) {
                CurrentState = IsTargetStoppedInsideBounds() ?
                    PlacementState.InsidePlaced : PlacementState.InsideFloating;
            }
            else {
                CurrentState = PlacementState.Outside;
            }
        }

        void OnTriggerEnter(Collider other) {
            if (other.gameObject.name == target.name) {
                CurrentState = PlacementState.InsideFloating;
            }
        }

        void OnTriggerExit(Collider other) {
            if (other.gameObject.name == target.name) {
                CurrentState = PlacementState.Outside;
            }
        }

        bool IsTargetStoppedInsideBounds() {
            var targetIsStopped = target.GetComponent<Rigidbody>().velocity.magnitude < maximumSpeedForStopped;
            var targetIsInBounds = boxCollider.bounds.Contains(targetMeshRenderer.bounds.center);

            return targetIsStopped && targetIsInBounds;
        }

        // Update is called once per frame
        void Update() {
            if (CurrentState != PlacementState.Outside) {
                CurrentState = IsTargetStoppedInsideBounds() ?
                    PlacementState.InsidePlaced : PlacementState.InsideFloating;
            }
        }

        void UpdateStateColor() {
            if (currentState == lastColoredState) {
                return;
            }
            var mpb = new MaterialPropertyBlock();
            Color stateColor;
            switch (currentState) {
                case PlacementState.Outside:
                    stateColor = Color.red;
                    break;
                case PlacementState.InsideFloating:
                    stateColor = Color.yellow;
                    break;
                case PlacementState.InsidePlaced:
                    stateColor = Color.green;
                    break;
                default:
                    Debug.LogError($"No state handling implemented for {currentState}");
                    stateColor = Color.magenta;
                    break;
            }
            stateColor.a = colorAlpha01;
            mpb.SetColor(shaderColorId, stateColor);
            meshRenderer.SetPropertyBlock(mpb);
            lastColoredState = currentState;
        }
    }
}