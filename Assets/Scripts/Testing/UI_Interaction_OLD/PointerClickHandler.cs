using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerClickHandler : MonoBehaviour, IPointerEnterHandler {

    public void OnPointerEnter(PointerEventData eventData) {
        Debug.Log("Pointer entered!");
        Debug.Log("Screen position: " + eventData.pointerCurrentRaycast.screenPosition);
    }

}
