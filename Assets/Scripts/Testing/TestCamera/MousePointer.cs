using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siro.IO;
using VRVis.Interaction;

/**
 * Use the mouse as a pointer to interact with objects.
 */
public class MousePointer : MonoBehaviour {

    public Camera cam;
    public float rayDist = 20.0f;
    private Ray mouseRay;
    public GameObject hitObj;

    // to limit the user on "click" events
    private bool buttonWasDown = false;


	void Start () {
		
        if (!cam) {
            Debug.LogError("No camera assigned!");
            enabled = false;
        }
	}


	void Update () {
		
        // if user holds mouse down
        if (Input.GetMouseButton(0) && !buttonWasDown) {
            buttonWasDown = true;

            // https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
            Vector3 mousePos = Input.mousePosition;
            
            // https://docs.unity3d.com/ScriptReference/Camera.ScreenPointToRay.html
            mouseRay = cam.ScreenPointToRay(mousePos);
            Debug.DrawRay(mouseRay.origin, mouseRay.direction, Color.red);

            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit, rayDist)) {

                if (!hit.collider.gameObject.Equals(hitObj)) {
                    //Debug.Log("New object selection!");
                    hitObj = hit.collider.gameObject;
                }
                
                // check if object has required component and call
                CodeFile cf = hitObj.GetComponent<CodeFile>();
                if (cf) { cf.Interaction(); }

                StructureNodeInteraction sni = hitObj.GetComponent<StructureNodeInteraction>();
                if (sni) { sni.Interaction(); }
            }
            else {
                hitObj = null;
            }
        }
        else if (!Input.GetMouseButton(0) && buttonWasDown) {
            buttonWasDown = false;
        }
	}

}
