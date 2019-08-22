using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR.InteractionSystem;
using VRVis.IO.Structure;
using VRVis.Spawner.Structure;


namespace VRVis.Interaction {

    /// <summary>
    /// Provides the possibility to select nodes using the
    /// mouse or the VR pointer and interact with them.<para/>
    /// This functionality was previously covered by "CodeFile.cs".<para/>
    /// This component is currently only used for "visual feedback" (color change) when clicked with a pointer.
    /// </summary>
    public class StructureNodeInteraction : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

        public StructureNodeInfo nodeInfo; // ToDo: remove if v2 is replacing it
        public StructureNodeInfoV2 nodeInfoV2;

        [Tooltip("If set, this object will be highlighted")]
        public Transform highlightOnEnter;

        public Color colorHighlighted = new Color(0.5f, 1.0f, 0.5f);
        public Color colorClickedFile = new Color(0.9f, 1.0f, 0.2f);
        public Color colorClickedOther = new Color(1.0f, 0.4f, 0.4f);
        public float clickColorShowTime = 0.2f;

        private Color colorDefault;
        private float clickedTime = 0;
        private bool clickColorShow = false;
        private bool pointerHovering = false;


        void Start() {
            
            // get default color to reset it later
            if (!highlightOnEnter) { colorDefault = GetComponent<Renderer>().material.color; }
            else { colorDefault = highlightOnEnter.GetComponent<Renderer>().material.color; }

            // get node information instance if not set yet
            if (!nodeInfo) { nodeInfo = GetComponent<StructureNodeInfo>(); } // ToDo: remove if v2 is replacing it
            if (!nodeInfoV2) { nodeInfoV2 = GetComponent<StructureNodeInfoV2>(); }
            
            if (!nodeInfo && ! nodeInfoV2) {
                Debug.LogWarning("Interaction script is missing node information script!", this);
            }
        }


        void Update() {
        
            // remove clicked color after the time passed
            if (clickColorShow && Time.time > clickedTime) {

                Color c = colorDefault;
                if (pointerHovering) { c = colorHighlighted; }

                if (!highlightOnEnter) { GetComponent<Renderer>().material.color = c; }
                else { highlightOnEnter.GetComponent<Renderer>().material.color = c; }
                clickColorShow = false;
            }
        }


        /// <summary>
        /// If an interaction with this file occurs.<para/>
        /// Currently for loading its content and associated regions.
        /// </summary>
        public void Interaction() {

            // ToDo: remove if v2 is replacing it
            if (nodeInfo) {
                Debug.Log("Interaction with: " + nodeInfo.GetSNode().GetPath());

                if (nodeInfo.GetSNode().GetNodeType() == SNode.DNodeTYPE.FILE) {
                    InteractionVisualFeedback(colorClickedFile);

                    // spawn code window if not done yet
                    // ToDo: give user ability to place the code window before actually spawning it
                    /*if (ApplicationLoader.GetInstance().fileSpawner.SpawnFile(nodeInfo.GetDNode(), Vector3.up * 3, Quaternion.identity)) {
                        Debug.Log("Code window spawned.");
                    }*/
                }
                else { InteractionVisualFeedback(colorClickedOther); }
            }
            else if (nodeInfoV2) {
                Debug.Log("Interaction with: " + nodeInfoV2.GetSNode().GetPath());

                if (nodeInfoV2.GetSNode().GetNodeType() == SNode.DNodeTYPE.FILE) { InteractionVisualFeedback(colorClickedFile); }
                else { InteractionVisualFeedback(colorClickedOther); }
            }
        }



        // POINTER EVENT HANDLING

        public void OnPointerClick(PointerEventData eventData) {

            Debug.Log("Clicked at file structure item: " + gameObject.name, gameObject);
            Interaction();
        }

        private void InteractionVisualFeedback(Color feedbackColor) {

            // show click color for a short moment
            clickColorShow = true;
            clickedTime = Time.time + clickColorShowTime;
            if (!highlightOnEnter) { GetComponent<Renderer>().material.color = feedbackColor; }
            else { highlightOnEnter.GetComponent<Renderer>().material.color = feedbackColor; }
        }

        public void OnPointerEnter(PointerEventData eventData) {

            GameObject rayHit = eventData.pointerCurrentRaycast.gameObject;

            // disabling the if-statement will result in all parent objects to be colored as well (could use this!)
            if (rayHit && rayHit == gameObject) {

                if (!highlightOnEnter) { GetComponent<Renderer>().material.color = colorHighlighted; }
                else { highlightOnEnter.GetComponent<Renderer>().material.color = colorHighlighted; }
                clickColorShow = false;
                pointerHovering = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData) {

            if (!clickColorShow) {
                if (!highlightOnEnter) { GetComponent<Renderer>().material.color = colorDefault; }
                else { highlightOnEnter.GetComponent<Renderer>().material.color = colorDefault; }
            }

            pointerHovering = false;
        }

        /// <summary>Receives message from laser pointer (also on change).</summary>
        public void PointerExit(Hand hand) {

            if (!clickColorShow) {
                if (!highlightOnEnter) { GetComponent<Renderer>().material.color = colorDefault; }
                else { highlightOnEnter.GetComponent<Renderer>().material.color = colorDefault; }
            }

            pointerHovering = false;
        }
        
    }
}
