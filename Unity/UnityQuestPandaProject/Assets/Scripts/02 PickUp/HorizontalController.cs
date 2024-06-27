/* Copyright (C) 2024 Vincent Schmitt - All Rights Reserved
 * You may use, distribute and modify this code under the
 * terms of the Educational Community License (ECL), Version 2.0.
 *
 * You should have received a copy of the ECL license with
 * this file. If not, please write to: schmittv@hs-pforzheim.de,
 * or visit: https://opensource.org/licenses/ECL-2.0
 */

using UnityEngine;

namespace Panda.PickUp {
    public enum GripState { Fixed = 0, Opening = 1, Closing = -1 };
    public class HorizontalController : MonoBehaviour {
        public static float speed = 0.05f;
        public GripState gripState = GripState.Fixed;
        public static readonly string[] linkNames = { "panda_rightfinger", "panda_leftfinger" };
        private FingerController[] fingerControllers;
        public float grip;
        public float gripSpeed = 3.0f;

        void Start() {
            fingerControllers = new FingerController[2];
            for (int i = 0; i < 2; ++i) {
                fingerControllers[i] = transform.Find(linkNames[i]).GetComponentInChildren<FingerController>();
            }
        }

        public void FixedUpdate() {
            switch (gripState) {
                case GripState.Fixed:
                    return;
                case GripState.Opening:
                    OpenFingers();
                    return;
                case GripState.Closing:
                    CloseFingers();
                    return;
                default:
                    return;
            }
        }

        public void OpenFingers() {
            foreach (var fingerController in fingerControllers) {
                fingerController.Open();
            }
        }

        public void CloseFingers() {
            foreach (var fingerController in fingerControllers) {
                fingerController.Close();
            }
        }
    }
}