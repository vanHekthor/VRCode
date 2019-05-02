using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/**
 * (DEPRECATED! - DOES NOT WORK)
 * 
 * Class to test region click detection.
 * This script should detect a click on a region (mouse and pointer).
 * It should be attached to a region (UI panel).
 * 
 * TESTED: YES
 * WORKS: NO
 */
public class RegionClickDetection : MonoBehaviour, IPointerClickHandler {

    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log("Clicked on region: " + eventData.pointerCurrentRaycast.gameObject);
    }

}
