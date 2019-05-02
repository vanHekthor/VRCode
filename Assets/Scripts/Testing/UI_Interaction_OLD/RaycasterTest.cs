using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


/** Attach this script to the canvas.
 * https://docs.unity3d.com/ScriptReference/UI.GraphicRaycaster.Raycast.html
 * STATUS: not working yet!
 */
public class RaycasterTest : MonoBehaviour {

    private GraphicRaycaster graphicsRaycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

	// Use this for initialization
	void Start () {
		
        graphicsRaycaster = GetComponent<GraphicRaycaster>();
        eventSystem = GetComponent<EventSystem>();
	}
	
	// Update is called once per frame
	void Update () {

        //Check if the left Mouse button is clicked
        if (Input.GetKey(KeyCode.Mouse0))
        {
            //Set up the new Pointer Event
            pointerEventData = new PointerEventData(eventSystem);
            //Set the Pointer Event Position to that of the mouse position
            pointerEventData.position = Input.mousePosition;

            //Create a list of Raycast Results
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            graphicsRaycaster.Raycast(pointerEventData, results);
            Debug.Log("EventData Position: " + pointerEventData.position);

            //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            Debug.Log("\nResults:");
            foreach (RaycastResult result in results) {
                Debug.Log("Hit " + result.gameObject.name);
            }
        }
	}
}
