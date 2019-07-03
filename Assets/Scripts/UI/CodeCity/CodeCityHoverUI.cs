using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Spawner.CodeCity;

namespace VRVis.UI.CodeCity {

    /// <summary>
    /// User interface to show on hover.<para/>
    /// This script is attached to the code city elements.
    /// </summary>
    public class CodeCityHoverUI : MonoBehaviour {

        [Tooltip("Prefab of the hover UI that has the CCUITextAdder component attached")]
        public GameObject uiPrefab;

        [Tooltip("Positional offset")]
        public Vector3 uiPosition;
        public Vector3 uiRotation;

        private GameObject uiInstance;
        private Hand attachedHand;


        void Update() {

            if (uiInstance) {

                // calculate position
                Vector3 pos = CalculatePosition();
                Vector3 camPos = Camera.main.transform.position;

                // ToDo: improve by passing a more useful object to PointerEntered
                //       (it should tell about the Hand, hit object and also about the hit position)
                float posDist = (pos - camPos).magnitude;
                float elDist = (transform.position - attachedHand.transform.position).magnitude;

                if (posDist > elDist) { pos = CalculatePosition(0.5f); }
                uiInstance.transform.position = pos;

                // rotate to camera
                Vector3 dir = camPos - pos;
                uiInstance.transform.rotation = Quaternion.LookRotation(-dir);
            }
        }


        /// <summary>
        /// Calculate the UI position.
        /// </summary>
        private Vector3 CalculatePosition(float scale = 1f) {

            Vector3 fw = attachedHand.transform.forward;
            Vector3 u = Vector3.up;
            Vector3 r = -Vector3.Cross(fw, u);
            Vector3 pos = attachedHand.transform.position + (fw * uiPosition.z + r * uiPosition.x + u * uiPosition.y) * scale;
            return pos;
        }


        /// <summary>
        /// Called through SendMessage method call by Laser Pointer.
        /// </summary>
        public void PointerEntered(Hand hand) {
            AttachUI(hand);
        }

        /// <summary>
        /// Called through SendMessage method call by Laser Pointer.
        /// </summary>
        public void PointerExit(Hand hand) {
            DetachUI();
        }


        /// <summary>
        /// Spawn the UI for this hand.
        /// </summary>
        private void AttachUI(Hand hand) {

            // parent to controller or to hand itself
            attachedHand = hand;
            Transform parentTo = hand.transform;
            if (hand.currentAttachedObject != null) { parentTo = hand.currentAttachedObject.transform; }
            uiInstance = Instantiate(uiPrefab);//, parentTo);
            uiInstance.transform.localPosition = uiPosition;
            uiInstance.transform.localRotation = Quaternion.Euler(uiRotation);

            // set according structure information
            UpdateUIInfo();
        }


        /// <summary>
        /// Detach hover UI from hand.
        /// </summary>
        private void DetachUI() {
            if (uiInstance) { Destroy(uiInstance); }
        }


        /// <summary>
        /// Update the shown information using code city element components.
        /// </summary>
        private void UpdateUIInfo() {

            // we can get the node information from this object
            // because this component is attached to it
            CodeCityElement e = GetComponent<CodeCityElement>();
            if (e) { SetUIInfo(e); return; }

            Debug.LogWarning("Failed to set UI info! - No CodeCityElement component found.", this);
        }


        /// <summary>
        /// Set UI information.
        /// </summary>
        private void SetUIInfo(CodeCityElement e) {
            
            CCUITextAdder tadder = uiInstance.GetComponent<CCUITextAdder>();
            if (!tadder) { return; }

            foreach (KeyValuePair<string, string> entry in e.GetInfo()) {
                tadder.AddText(entry.Key, entry.Value);
            }
        }

    }
}
