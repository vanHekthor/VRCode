using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace VRVis.Tutorial {

    /// <summary>
    /// Show most important button hints.
    /// </summary>
    public class TutorialButtonHints : MonoBehaviour {

        public Hand controller;

        [Tooltip("Time in seconds before hiding the hints after all are shown")]
        public float showTimeSeconds = 5f;

        [Tooltip("Time to wait in seconds before showing the next hint")]
        public float waitAfterEach = 3f;

        [Tooltip("Start showing after x seconds after application startup")]
        public float doNotShowBefore = 5;

        [Tooltip("Switch to trigger showing the hints")]
        public bool show = true;

        private bool showing = false;


        void Awake() {
            
            if (!controller) { controller = GetComponent<Hand>(); }
            if (!controller) { show = false; }
        }


        void Start() {

            if (!controller) { show = false; }
        }


        void Update() {

            if (Time.time < doNotShowBefore) { return; }

            if (show) {
                show = false;
                if (!controller || !controller.isActiveAndEnabled) { return; }

                if (showing) {
                    StopAllCoroutines();
                    HideHints();
                }

                StartCoroutine(HintsCoroutine(showTimeSeconds));
            }
        }


        /// <summary>
        /// Shows hints for a while and hides them.
        /// </summary>
        private IEnumerator HintsCoroutine(float timeToShow) {

            showing = true;

            ControllerButtonHints.ShowTextHint(controller, SteamVR_Actions.default_GrabPinch, "Trigger", true); 
            yield return new WaitForSecondsRealtime(waitAfterEach);

            ControllerButtonHints.ShowTextHint(controller, SteamVR_Actions.default_GrabGrip, "Grip", true); 
            yield return new WaitForSecondsRealtime(waitAfterEach);

            ControllerButtonHints.ShowTextHint(controller, SteamVR_Actions.default_Teleport, "Teleport", true); 
            yield return new WaitForSecondsRealtime(waitAfterEach);

            yield return new WaitForSecondsRealtime(timeToShow);
            HideHints();
        }


        private void HideHints() {
            ControllerButtonHints.HideAllTextHints(controller);
            ControllerButtonHints.HideAllButtonHints(controller);
            showing = false;
        }

    }
}
