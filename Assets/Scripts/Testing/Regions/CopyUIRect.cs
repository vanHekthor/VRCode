using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CopyUIRect : MonoBehaviour {

    public RectTransform origin;

    public bool update = false;

    void OnDrawGizmos() {

        if (!update) { return; }
        update = false;

        RectTransform rt = GetComponent<RectTransform>();
        if (!rt) { return; }

        rt.anchoredPosition = origin.anchoredPosition;
        rt.sizeDelta = origin.sizeDelta;
        
    }
	
}
