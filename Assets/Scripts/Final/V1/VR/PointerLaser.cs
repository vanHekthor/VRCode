using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Siro.IO;

/**
 * DEPRECATED (OLD CODE)!
 * USE VRInputModule INSTEAD!
 * 
 * To point a laser somewhere and interact with files and UI elements.
 * Requires the SteamVR InteractionSystem "Interactable" Script to be attached to the same object.
 */
public class PointerLaser : MonoBehaviour {

    public GameObject laserPrefab;
    public Transform laserStartPosition;
    public float laserMaxDistance = 100.0f;

    public Transform JoystickButton;
    public Vector3 JoystickPosPressed = new Vector3(0, -0.0005f, 0);
    
    //[SteamVR_DefaultActionSet("platformer")] // no longer supported
    public SteamVR_ActionSet actionSet;

    //[SteamVR_DefaultAction("Teleport", "platformer")]
    public SteamVR_Action_Boolean a_pointing;
    
    //[SteamVR_DefaultAction("Interact", "platformer")]
    public SteamVR_Action_Boolean a_interact;

    private Vector3 JoystickPosDefault;
    private bool JoystickPressed = false;

    private SteamVR_Input_Sources hand;
    private Interactable interactable;
    private bool pointing; // if user clicks the button
    private bool pointingLast;
    private bool laserActive;
    private GameObject laserInstance;

    private bool interacting;
    private bool interactingLast;
    private bool interactionPressed;


	void Start () {
    
        if (!laserPrefab) {
            Debug.LogWarning("Missing Laser Prefab!");
        }
        else {
            laserInstance = Instantiate(laserPrefab);
            laserInstance.SetActive(false);
        }

        if (!laserStartPosition) {
            Debug.LogError("Missing laser start position!");
        }

        interactable = GetComponent<Interactable>();
        interactable.activateActionSetOnAttach = actionSet;

        if (JoystickButton) {
            JoystickPosDefault = JoystickButton.localPosition;
        }
	}
	

	void Update () {

        // check if the laser is attached to a hand
        if (interactable.attachedToHand) {

            // get the hand and get if user presses the button to point at something
            hand = interactable.attachedToHand.handType;
            pointing = a_pointing.GetState(hand);
            interacting = a_interact.GetState(hand);

            // change state of the laser if user clicked the button
            if (pointing && !pointingLast) { laserActive = !laserActive; }

            // set interaction state
            if (interacting && !interactingLast) { interactionPressed = true; }
        }
        else {
            pointing = false;
            laserActive = false;
        }

        // save last pointing state
        pointingLast = pointing;
        interactingLast = interacting;

        // show the laser object (if it hits something then to this point, otherwise max length)
        if (laserActive && laserStartPosition) {
            RaycastHit hit;
            if (Physics.Raycast(laserStartPosition.position, laserStartPosition.forward, out hit, laserMaxDistance)) {
                ShowLaser(hit.point, hit.collider);
            }
            else {
                ShowLaser(laserStartPosition.position + laserStartPosition.forward * laserMaxDistance, null);
            }
        }
        else if (!laserActive) {
            HideLaser();
        }

        // joystick button press animation
        if (pointing && JoystickButton) {
            if (!JoystickPressed) {
                JoystickPressed = true;
                JoystickButton.localPosition = JoystickPosPressed;
            }
        }
        else if (JoystickPressed) {
            JoystickPressed = false;
            JoystickButton.localPosition = JoystickPosDefault;
        }
	}

    void OnDrawGizmos() {
        
        if (laserStartPosition) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(laserStartPosition.position, 0.005f);
        }
    }


    void ShowLaser(Vector3 hitPos, Collider col) {
        
        if (!laserInstance.activeSelf) {
            laserInstance.SetActive(true);
        }

        if (interactionPressed) {
            interactionPressed = false;

            if (col) {
                // check if object has required component and call
                CodeFile cf = col.gameObject.GetComponent<CodeFile>();
                if (cf) { cf.Interaction(); }
            }
        }

        // set scale and position accordingly
        Transform lt = laserInstance.transform;

        // get position between start and hit point
        lt.position = Vector3.Lerp(laserStartPosition.position, hitPos, 0.5f);

        // look at the hit position and scale the laser according to the distance
        float hitDistance = Vector3.Distance(laserStartPosition.position, hitPos);
        lt.LookAt(hitPos);
        lt.localScale = new Vector3(lt.localScale.x, lt.localScale.y, hitDistance);
    }

    void HideLaser() {

        if (laserInstance.activeSelf) {
            laserInstance.SetActive(false);
        }
    }

}
