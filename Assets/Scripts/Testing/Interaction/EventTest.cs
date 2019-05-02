using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace VRVis.Testing.Events {

    /**
     * (! CURRENTLY DEPRECATED ! - can be deleted)
     * To test which events are received by the laser pointer.
     */
    public class EventTest : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerClick(PointerEventData eventData) {
            Debug.Log("Pointer Click Event!");
        }

        public void OnPointerEnter(PointerEventData eventData) {
            Debug.Log("Pointer Enter Event!");
        }

        public void OnPointerExit(PointerEventData eventData) {
            Debug.Log("Pointer Exit Event!");
        }
    }

}