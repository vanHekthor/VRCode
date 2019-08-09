using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.Controller;
using VRVis.Interaction.ControllerSelectionSystem;
using VRVis.IO.Features;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.Spawner.ConfigModel;
using VRVis.Spawner.Structure;

namespace VRVis.Interaction.LaserPointer {

    /// <summary>
    /// Written by github.com/S1r0hub (Leon H.)<para/>
    /// 
    /// Created: 22.11.2018<para/>
    /// Updated: 09.08.2019<para/>
    /// 
    /// Some methods are from Wacki as mentioned in the default ViveUILaserPointer script.<para/>
    /// This script is modified to work only in the VRVis system.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class ViveUILaserPointerPickup : IUILaserPointerPickup, IPointerClickHandler {

        public SteamVR_Action_Boolean toggleButton;
        public SteamVR_Action_Boolean triggerButton;

        [Tooltip("Simply select the touchpad as input for smooth scroll")]
        public SteamVR_Action_Vector2 scrollWheel;
        //public Color selectedColor = new Color(0.85f, 0.45f, 0.11f);
        
        [Tooltip("Minimum input of touchpad to have an effect")]
        public Vector2 minScrollThreshold = new Vector2(0.1f, 0.1f);

        [Tooltip("Map the values from min scroll to 1 on this vector (for x: 0...scrollMapping.x, for y: 0-scrollMapping.y)")]
        public Vector2 scrollMapping = new Vector2(1, 1);

        [Tooltip("If this is used in VRVis")]
        public bool VRVIS = false;

        [Tooltip("Index of the code window mover controller in radial menu entries")]
        public int cwMoverIndex = 0;

        [Tooltip("How long to wait (seconds) after exiting something before another pulse occurs")]
        public float pulseWait = 0.05f;

        [Tooltip("Layers to use pulse on")]
        public LayerMask pulseLayer;


        // now assigned by pickup event
        protected Hand controller;
        private bool lastToggleState = false;

        // to prevent haptic pulse spam
        private float lastPulseTime = 0;

        // currently selected file to be spawned
        // OLD CODE: attempt 1 of node selection (remove if attempt 2 works)
        //private NodeInformation selectedFile = null;

        private GameObject lastHovered;


        /// <summary>Get the last hovered game object.</summary>
        public GameObject GetLastHovered() { return lastHovered; }


        private bool IsAvailable() {
            return controller && IsLaserActive();
        }


        protected override void Initialize() {
            base.Initialize();
            Debug.Log("Initialize ViveUILaserPointer");
        }


        public override bool ButtonDown() {

            if (!IsAvailable()) { return false; }
            bool state = triggerButton.GetStateDown(controller.handType);
            //Debug.Log("ButtonDown event (" + state + ")");
            return state;
        }

        public override bool ButtonUp() {

            if (!IsAvailable()) { return false; }
            bool state = triggerButton.GetStateUp(controller.handType);
            //Debug.Log("ButtonUp event (" + state + ")");
            return state;
        }
        

        public override void OnEnterControl(GameObject control) {

            if (!IsAvailable()) { return; }

            //Debug.LogWarning("Laser entered: " + control.name, control);

            // haptic pulse if object has supported layer
            PulseOnEnter poe = control.GetComponent<PulseOnEnter>();
            if (poe != null) {
                if (Time.time > lastPulseTime + pulseWait) {
                    lastPulseTime = Time.time;
                    float duration = poe.duration; // in seconds
                    controller.TriggerHapticPulse(duration, 1f / duration, poe.amplitude);
                }
            }

            //Debug.Log("Controller entered: " + control, control);
            control.SendMessage("PointerEntered", controller, SendMessageOptions.DontRequireReceiver);
            lastHovered = control;
        }

        public override void OnExitControl(GameObject control) {

            if (!IsAvailable()) { return; }

            control.SendMessage("PointerExit", controller, SendMessageOptions.DontRequireReceiver);
        }


        public override bool ButtonToggleClicked() {

            if (!controller) { return false; }
            
            // get the current button state and check if it changed from true to false
            bool stateChangedToTrue = false;
            bool toggleState = toggleButton.GetStateDown(controller.handType);
            if (!toggleState && toggleState != lastToggleState) { stateChangedToTrue = true; }
            lastToggleState = toggleState;
            return stateChangedToTrue;
        }


        public override bool IsScrolling() {
            
            if (scrollWheel == null || !controller) { return false; }
            Vector2 curAxis = scrollWheel.GetAxis(controller.handType);
            return Mathf.Abs(curAxis.x) >= minScrollThreshold.x || Mathf.Abs(curAxis.y) >= minScrollThreshold.y;
        }

        public override Vector2 GetScrollDelta() {
            
            if (scrollWheel == null || !controller) { return Vector2.zero; }

            Vector2 curAxis = scrollWheel.GetAxis(controller.handType);

            // max of input values
            float max = 1;
            float rangeX = max - minScrollThreshold.x;
            float rangeY = max - minScrollThreshold.y;

            // get positive or negative direction
            float xNeg = curAxis.x < 0 ? -1 : 1;
            float yNeg = curAxis.y < 0 ? -1 : 1;

            // check that min threshold is exceeded (it is not, if diff is less than 0)
            float xCur = Mathf.Abs(curAxis.x) - minScrollThreshold.x;
            float yCur = Mathf.Abs(curAxis.y) - minScrollThreshold.y;
            if (xCur < 0) { xCur = 0; }
            if (yCur < 0) { yCur = 0; }

            // map value on range
            float xVal = (xCur / rangeX) * scrollMapping.x * xNeg;
            float yVal = (yCur / rangeY) * scrollMapping.y * yNeg;
            return new Vector2(xVal * -1, yVal); // invert scroll dir on horizontal axis
        }



        // ########### STEAMVR_SECTION ########### //

        //-------------------------------------------------
        private void OnAttachedToHand(Hand attachedHand) {

            controller = attachedHand;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Debug.Log("Pickup Laser Pointer attached to hand: " + attachedHand.name);
	    }

	    //-------------------------------------------------
        // Equal to Update Method!
        // Will be called by "Hand.cs -> protected virtual void Update()" every frame.
        // Only called by the hand that this object is attached to!
	    private void HandAttachedUpdate(Hand hand) {

		    // Reset transform since we cheated it right after getting poses on previous frame
		    //transform.localPosition = Vector3.zero;
		    //transform.localRotation = Quaternion.identity;

            // perform a laser update call
            UpdateCall();
        }

	    //-------------------------------------------------
	    private void OnHandFocusLost(Hand hand) {
		    gameObject.SetActive(false);
	    }


	    //-------------------------------------------------
	    private void OnHandFocusAcquired(Hand hand) {
		    gameObject.SetActive(true);
		    OnAttachedToHand(hand);
	    }


	    //-------------------------------------------------
	    private void OnDetachedFromHand(Hand hand) {

            // notify last hovered that pointer is detached
            if (lastHovered != null) { lastHovered.SendMessage("PointerExit", hand, SendMessageOptions.DontRequireReceiver); }

            Debug.Log("Pickup Laser Pointer detached from hand!");
		    Destroy(gameObject);
	    }


	    //-------------------------------------------------
	    void OnDestroy() {
		    ShutDown();
	    }

	    //-------------------------------------------------
	    private void ShutDown() {
        
            // remove the controller from the input module
            LaserPointerInputModule.instance.RemoveController(this);
        }

        public void OnPointerClick(PointerEventData eventData) {

            if (!VRVIS) { return; }
            if (!controller) { return; }

            GameObject go = eventData.pointerCurrentRaycast.gameObject;
            if (!go) { return; }
            
            Debug.Log("Controller click on object!", go);

            // check if click reached a structure node
            StructureNodeInfo nodeInf = go.GetComponent<StructureNodeInfo>();
            if (nodeInf != null) { StructureNodeClicked(nodeInf.GetSNode()); }

            // check if click reached a structure node of structure version 2
            StructureNodeInfoV2 nodeInfV2 = go.GetComponent<StructureNodeInfoV2>();
            if (nodeInfV2 != null) { StructureNodeClicked(nodeInfV2.GetSNode()); }

            // check if click reached a feature model node
            VariabilityModelNodeInfo vmNodeInf = go.GetComponent<VariabilityModelNodeInfo>();
            if (vmNodeInf != null) { VariabilityModelNodeClicked(vmNodeInf); }
        }

        /// <summary>Called when clicked on a structure node.</summary>
        /// <param name="clickedAt">The object that was clicked (e.g. code city element...)</param>
        public void StructureNodeClicked(SNode node, Transform clickedAt = null) {

            if (node == null) { return; }

            // if the file is not spawned, switch the controller to
            // code window mover and let user spawn the file window
            bool isFile = node.GetNodeType() == SNode.DNodeTYPE.FILE;
            if (isFile) {

                // check if already spawned
                bool isFileSpawned = FileSpawner.GetInstance().IsFileSpawned(node.GetFullPath());
                if (!isFileSpawned) {

                    // get the controller selection script
                    ControllerSelection selectionScript = controller.GetComponent<ControllerSelection>();
                    if (!selectionScript) {
                        Debug.LogError("Could not find selection script of hand!");
                        return;
                    }

                    // attach CWMoverController
                    ControllerSelection.SelectableController cwMoverCtrl = selectionScript.GetController(cwMoverIndex);
                    if (cwMoverCtrl == null) {
                        Debug.LogError("Could not find controller definition for CodeWindowMover! Ensure it exists.");
                        return;
                    }

                    // if attached, try to pass the node info to the cw mover instance
                    bool attached = selectionScript.AttachController(cwMoverCtrl);
                    if (!attached) { return; }

                    GameObject attachedObject = controller.currentAttachedObject;
                    CodeWindowMover cwm = attachedObject.GetComponent<CodeWindowMover>();
                    if (cwm) { cwm.SelectNode(node, true, clickedAt); }
                }
                else {

                    // ToDo: maybe show some information about the file?
                    // or make it light up for a short amount of time to show where it is?

                    Debug.LogWarning("File already spawned: " + node.GetName());

                }
            }
        }


        /// <summary>
        /// Called when a click on a vm node occurred.
        /// </summary>
        private void VariabilityModelNodeClicked(VariabilityModelNodeInfo nInf) {

            // simply change status if boolean feature
            AFeature option = nInf.GetOption();
            if (option is Feature_Boolean) {
                ((Feature_Boolean) option).SwitchSelected();
                nInf.UpdateColor();
            }
            
            // ToDo: show on terminal to change numeric value (slider)
            // ToDo: maybe also show boolean value on/off as slider (0 to 1)
        }


        // ToDo: cleanup
        /*
        // OLD CODE: "Node selection attempt 1" (remove if attempt 2 works)
        public void OnPointerClick(PointerEventData eventData) {

            GameObject go = eventData.pointerCurrentRaycast.gameObject;
            if (!go) { return; }
            Debug.Log("Controller click on: " + go);

            // check if click reached a structure node
            NodeInformation nodeInf = go.GetComponent<NodeInformation>();

            // no selection made yet and clicked on no node
            if (selectedFile == null && nodeInf == null) { return; }

            // get the file spawner instance
            FileSpawner fileSpawner = ApplicationLoader.GetInstance().GetFileSpawner();

            // if this pointer already selected a node (second click)
            if (selectedFile != null) {

                if (nodeInf != null) {
                    if (nodeInf.Equals(selectedFile)) {
                        Debug.LogWarning("This node is already selected with this pointer!");
                        return;
                    }
                    return; // dont spawn clicking on other nodes - ToDo: maybe abort selection at this point in future
                }

                // ToDo: rotation (touchpad left/right) and ray distance (touchpad up/down) adjustment by user

                // user pressed again with node selected, so spawn it at this location
                if (!fileSpawner.SpawnFile(nodeInf.GetDNode(), eventData.pointerCurrentRaycast.worldPosition, Quaternion.identity)) {
                    Debug.LogError("Failed to spawn file!");
                    return;
                }

                // clean up everything regarding the previous selection
                ClearFileSelection();
                return;
            }

            // catch this case (can happen if file is selected and clicked on other object)
            if (nodeInf == null) { return; }

            // check if the according file is already spawned
            if (fileSpawner.IsFileSpawned(nodeInf.GetDNode().GetFullPath())) {
                Debug.LogWarning("The file represented by the selected structure node is already spawned.");
                return;
            }
            
            // select the node as "selected"
            SetFileSelection(nodeInf);

        }
        */

        /** Set up everything required for code selection. */
        /*
        // OLD CODE: "Node selection attempt 1" (remove if attempt 2 works)
        private void SetFileSelection(NodeInformation nodeInf) {
            selectedFile = nodeInf;
            nodeInf.SetSelectedForSpawning(true);

            // settings regarding laser pointer
            SetSendEventsToHit(false); // stop sending events to hit objects
            SetHitColor(selectedColor);
            Debug.Log("Node selected: " + nodeInf.GetDNode().GetName());
        }
        */

        /** Clear file selection. Called after spawning it or abort. */
        /*
        // OLD CODE: "Node selection attempt 1" (remove if attempt 2 works)
        private void ClearFileSelection() {
            if (selectedFile != null) { selectedFile.SetSelectedForSpawning(false); }
            selectedFile = null;

            // settings regarding laser pointer
            SetSendEventsToHit(true);
            ResetHitColor();
            Debug.Log("Node selection cleared.");
        }
        */

    }
}
