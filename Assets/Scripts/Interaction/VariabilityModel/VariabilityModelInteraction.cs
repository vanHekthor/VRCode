using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR.InteractionSystem;
using VRVis.Spawner.ConfigModel;
using VRVis.UI.VariabilityModel;

namespace VRVis.Interaction {

    /// <summary>
    /// Class that handles pointer interaction for feature model nodes.<para/>
    /// This script should be attached to each node of the spawned model (attach to prefabs).<para/>
    /// Last Update: 22.08.2019
    /// </summary>
    public class VariabilityModelInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

        [Tooltip("The UI showing up if the user hovers over a node")]
        public GameObject hoverInfoPrefab;

        [Tooltip("Rotation offset looking at the user")]
        public Vector3 rotationOffset;

        [Tooltip("UI distance from center of this object when facing user")]
        public float distFromCenter = 0.3f;

        // there is always only one UI shown
        private static GameObject hoverUIInstance;
        private static VariabilityNodeHoverUI hoverUIScript;


        void Awake() {
        
            if (!hoverUIInstance) {
                hoverUIInstance = Instantiate(hoverInfoPrefab);
                hoverUIInstance.SetActive(false); // hide UI
                hoverUIScript = hoverUIInstance.GetComponent<VariabilityNodeHoverUI>();
            }
        }


        void Update() {
            MakeUIFaceUser();
        }


        /// <summary>
        /// Checks if this gameobject has a node information script attached.<para/>
        /// This script is required to get information about this node.
        /// </summary>
        public bool TryGetNodeInformation(out VariabilityModelNodeInfo nInfo) {
            nInfo = GetComponent<VariabilityModelNodeInfo>();
            return nInfo != null;
        }

        
        public void OnPointerEnter(PointerEventData eventData) {

            if (eventData.pointerEnter == gameObject) {

                if (!hoverUIScript) { return; }
                if (hoverUIScript.CurrentlyShownNode != null) { return; }

                VariabilityModelNodeInfo nInfo;
                if (!TryGetNodeInformation(out nInfo)) { return; }

                // set hover UI information
                hoverUIScript.CurrentlyShownNode = gameObject;
                hoverUIInstance.transform.position = transform.position;
                hoverUIInstance.SetActive(true);
                hoverUIScript.ShowNodeInformation(nInfo);
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            if (hoverUIScript.CurrentlyShownNode != gameObject) { return; }
            DetachUI();
        }

        /// <summary>Receive event from laser pointer that the pointer exit.</summary>
        public void PointerExit(Hand hand) { DetachUI(); }


        /// <summary>Hide the currently shown UI.</summary>
        public void DetachUI() {

            // hide hover UI
            if (hoverUIInstance) { hoverUIInstance.SetActive(false); }
            if (hoverUIScript) { hoverUIScript.CurrentlyShownNode = null; }
        }


        /// <summary>
        /// Rotate the UI around the node facing the user.<para/>
        /// It is useful having this method in this script because
        /// we can define a different behaviour for each node type (e.g. distance from center).
        /// </summary>
        private void MakeUIFaceUser() {

            // this must be the node controlling the UI currently
            if (!hoverUIScript) { return; }
            if (hoverUIScript.CurrentlyShownNode != gameObject) { return; }

            // rotate towards cam and move
            // set position in direction of camera
            hoverUIInstance.transform.position = transform.position;
            Transform userCam = Camera.main.transform;
            //Vector3 v = userCam.transform.position - transform.position;
            hoverUIInstance.transform.LookAt(userCam.transform.position);// - v);

            // move in direction and apply rotation offset
            hoverUIInstance.transform.position += hoverUIInstance.transform.forward * distFromCenter;
            hoverUIInstance.transform.Rotate(rotationOffset);
        }

    }
}
