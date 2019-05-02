using Siro.IO.Structure;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Siro.IO {

    /**
     * If a user interacts with a file (e.g. picking it up),
     * the information of that file should be displayed
     * (syntax highlighted code and regions).
     * 
     * Added to a file while spawning the structure.
     */
    public class CodeFile : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

        // to get file information from
        public ElementInfo info;

        // to load the file content
        public LoadFile loadFileScript;

        public Color colorHighlighted = new Color(0.5f, 1.0f, 0.5f);
        public Color colorClickedFile = new Color(0.9f, 1.0f, 0.2f);
        public Color colorClickedOther = new Color(1.0f, 0.4f, 0.4f);
        public float clickColorShowTime = 0.2f;

        private Color colorDefault;
        private float clickedTime = 0;
        private bool clickColorShow = false;


        void Start() {
        
            colorDefault = GetComponent<Renderer>().material.color;

            if (!info) { info = GetComponent<ElementInfo>(); }
            if (!info) {
                Debug.LogError("ElementInfo not assigned!");
            }
        }

        void Update() {
        
            // remove clicked color after the time passed
            if (clickColorShow && Time.time > clickedTime) {
                GetComponent<Renderer>().material.color = colorDefault;
                clickColorShow = false;
            }
        }

        /**
         * If an interaction with this file occurs.
         * Currently for loading its content and associated regions.
         */
        public void Interaction() {

            if (info && loadFileScript) {
                Debug.Log("Interaction with file: " + info.elementName);

                if (info.GetNodeType() == DNode.DNodeTYPE.FILE) {
                    InteractionVisualFeedback(colorClickedFile);
                    loadFileScript.LoadFileContent(info.elementPath, info.elementFullPath);
                }
                else {
                    InteractionVisualFeedback(colorClickedOther);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData) {

            Debug.Log("Clicked at file structure item: " + gameObject.name);
            Interaction();
        }

        private void InteractionVisualFeedback(Color feedbackColor) {

            // show click color for a short moment
            clickColorShow = true;
            clickedTime = Time.time + clickColorShowTime;
            GetComponent<Renderer>().material.color = feedbackColor;
        }

        public void OnPointerEnter(PointerEventData eventData) {

            GameObject rayHit = eventData.pointerCurrentRaycast.gameObject;

            // disabling the if-statement will result in all parent objects to be colored as well (could use this!)
            if (rayHit && rayHit == gameObject) {
                GetComponent<Renderer>().material.color = colorHighlighted;
                clickColorShow = false;
            }
        }

        public void OnPointerExit(PointerEventData eventData) {

            if (!clickColorShow) {
                GetComponent<Renderer>().material.color = colorDefault;
            }
        }
    }
}