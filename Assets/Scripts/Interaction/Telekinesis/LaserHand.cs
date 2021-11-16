using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Valve.VR.InteractionSystem.Sample;
using VRVis.Interaction.Controller;
using VRVis.Interaction.ControllerSelectionSystem;
using VRVis.Interaction.LaserPointer;
using VRVis.IO.Features;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.Spawner.ConfigModel;
using VRVis.Spawner.Structure;

namespace VRVis.Interaction.LaserHand {

    /// <summary>
    /// Written by github.com/S1r0hub (Leon H.)<para/>
    /// 
    /// Created: 22.11.2018<para/>
    /// Updated: 12.09.2019<para/>
    /// 
    /// Some methods are from Wacki as mentioned in the default ViveUILaserPointer script.<para/>
    /// This script is modified to work only in the VRVis system.
    /// </summary>
    public class LaserHand : IUILaserPointerPickup, IPointerClickHandler {

        public SteamVR_Action_Boolean toggleButton;
        public SteamVR_Action_Boolean triggerButton;
        public SteamVR_Action_Boolean grip;

        [Tooltip("Simply select the touchpad as input for smooth scroll")]
        public SteamVR_Action_Vector2 scrollWheel;

        [Tooltip("If available, assign the scroll wheel here")]
        public Transform scrollWheelObject;

        [Tooltip("Multiply the scroll input value with this factor")]
        [Range(0, 100)] public float scrollMult = 16;

        [Tooltip("Minimum input of touchpad to have an effect")]
        public Vector2 minScrollThreshold = new Vector2(0.008f, 0.008f);

        [Tooltip("Threshold to activate scroll wheel (default 0.05)")]
        public float scrollActivationThreshold = 0.05f;

        [Tooltip("Degree to turn per change unit (the higher the faster)")]
        [Range(0, 360)] public float scrollWheelTurnDegree = 20;

        [Tooltip("Index of the code window mover controller in radial menu entries")]
        public int cwMoverIndex = 0;

        [Tooltip("How long to wait (seconds) after exiting something before another pulse occurs")]
        public float pulseWait = 0.05f;

        [Tooltip("If this is used in VRVis")]
        public bool VRVIS = true;


        // now assigned by pickup event
        public  Hand hand;
        private bool lastToggleState = false;

        // PROPERTIES
            
        public bool IsActive { get; private set; }

        // to prevent haptic pulse spam
        private float lastPulseTime = 0;
        private GameObject lastHovered;

        private bool scrolling = false;
        private bool scrollStarted = false;
        private bool hideWheel = false;
        private Coroutine hideScrollWheelCoroutine;
        private Vector2 lastScrollAxis;
        private Vector2 scrollChange;
        private Vector2 lastScrollChange;
        private float lastFeedback = 0;

        // Psychic Hand Reference
        private Telekinesis.PsychicHand psychicHand;

        private bool gripping;

        /// <summary>Get the last hovered game object.</summary>
        public GameObject GetLastHovered() { return lastHovered; }


        private bool IsAvailable() {
            return hand && IsLaserActive();
        }


        protected override void Initialize() {
            base.Initialize();
            ShowLaser();
            psychicHand = hand.GetComponentInChildren<Telekinesis.PsychicHand>();
            
            Debug.Log("Initialize LaserHand");
        }

        protected override void Update() {
            gripping = grip.GetState(hand.handType);

            ShowLaser();

            if (!gripping) {
                HideLaser();
                IsActive = false;
                return;
            }

            if (psychicHand.IsActive && !IsActive) {
                HideLaser();
                return;
            }

            IsActive = true;

            UpdateCall();
            // CheckScrolling();            
            pointer.SetActive(false);
            hitPoint.SetActive(false);
            
            //// show pointer and hitPoint when hitting
            if (bHit) {
                if (hitInfo.transform != null) {
                    if (!hitPoint.activeSelf) {
                        pointer.SetActive(true);
                        hitPoint.SetActive(true);
                    }
                }
            }
        }

        public override bool ButtonDown() {
            if (!IsAvailable()) { return false; }
            return triggerButton.GetStateDown(hand.handType);
        }

        public override bool ButtonUp() {
            if (!IsAvailable()) { return false; }
            return triggerButton.GetStateUp(hand.handType);
        }


        public override void OnEnterControl(GameObject control) {

            if (!IsAvailable()) { return; }

            // haptic pulse if object has supported layer
            PulseOnEnter poe = control.GetComponent<PulseOnEnter>();
            if (poe != null) {
                if (Time.time > lastPulseTime + pulseWait) {
                    lastPulseTime = Time.time;
                    float duration = poe.duration; // in seconds
                    hand.TriggerHapticPulse(duration, 1f / duration, poe.amplitude);
                }
            }

            //Debug.Log("Controller entered: " + control, control);
            control.SendMessage("PointerEntered", hand, SendMessageOptions.DontRequireReceiver);
            lastHovered = control;
        }

        public override void OnExitControl(GameObject control) {

            if (!IsAvailable()) { return; }
            control.SendMessage("PointerExit", hand, SendMessageOptions.DontRequireReceiver);
        }


        public override bool ButtonToggleClicked() {

            if (!hand) { return false; }

            // get the current button state and check if it changed from true to false
            bool stateChangedToTrue = false;
            bool toggleState = toggleButton.GetStateDown(hand.handType);
            if (!toggleState && toggleState != lastToggleState) { stateChangedToTrue = true; }
            lastToggleState = toggleState;
            return stateChangedToTrue;
        }


        public override bool IsScrolling() { return scrolling && scrollStarted && scrollChange != Vector2.zero; }

        public override Vector2 GetScrollDelta() {
            if (scrollWheel == null || !hand) { return Vector2.zero; }
            return new Vector2(scrollChange.x * -1, scrollChange.y); // invert scroll dir on horizontal axis
        }

        /// <summary>
        /// Checks if the user is scrolling on the trackpad.<para/>
        /// Takes care of showing the scroll wheel objects and so on.
        /// </summary>
        private void CheckScrolling() {

            if (scrollWheel == null || !hand) { scrolling = false; return; }

            scrolling = ScrollingGesture();

            // set value of scrollStarted variable accordingly (will be disabled by coroutine)
            if (!scrollStarted && scrolling) { scrollStarted = true; }

            // show wheel object on controller if it exists and hide if no longer in use
            if (scrollWheelObject) {

                if (scrolling) {

                    hideWheel = false;
                    if (hideScrollWheelCoroutine != null) {
                        StopCoroutine(hideScrollWheelCoroutine);
                        hideScrollWheelCoroutine = null;
                    }

                    scrollWheelObject.gameObject.SetActive(true);
                }
                else if (scrollWheelObject.gameObject.activeSelf && hideScrollWheelCoroutine == null) {
                    hideWheel = true;
                    hideScrollWheelCoroutine = StartCoroutine(HideScrollWheelAfter(2));
                }

                // store last scroll axis for change calculation
                Vector2 cur = scrollWheel.GetAxis(hand.handType);
                if (lastScrollAxis != Vector2.zero && cur != Vector2.zero) { scrollChange = cur - lastScrollAxis; }
                else { scrollChange = Vector2.zero; }
                lastScrollAxis = cur;

                scrollChange *= Time.deltaTime * 100;
                scrollChange.x = 0;
                //scrollChange.x = Mathf.Round(scrollChange.x * 10000.0f) / 10000.0f;
                scrollChange.y = Mathf.Round(scrollChange.y * 10000.0f) / 10000.0f;
                //if (Mathf.Abs(scrollChange.x) < minScrollThreshold.x) { scrollChange.x = 0; }
                if (Mathf.Abs(scrollChange.y) < minScrollThreshold.y) { scrollChange.y = 0; }
                scrollChange *= scrollMult;

                // smooth
                scrollChange = (scrollChange + lastScrollChange) * 0.5f;
                lastScrollChange = scrollChange;

                // rotate scroll wheel object accordingly
                // (change.y * 0.5 = "/ 2" bc. from top to bottom = maximum possible change of distance -> 2 on touchpad)
                float angleDegree = (scrollChange.y * 0.5f) * scrollWheelTurnDegree;
                scrollWheelObject.Rotate(new Vector3(0, 0, 1), angleDegree, Space.Self);
            }
        }

        /// <summary>
        /// Checks if the user performed a scrolling gesture on the trackpad.<para/>
        /// It also stores the initial speed of the "virtual" scroll wheel.
        /// </summary>
        private bool ScrollingGesture() {

            float wheel_width = 0.5f;
            float wheel_height = 1;

            // check if finger is somewhere on the trackpad
            // by assuming that if not, the vector will be zero
            if (scrollWheel.GetAxis(hand.handType).magnitude < 0.0001f) { return false; }

            // check if the change was strong enough
            float scrollDeltaMag = Mathf.Abs(scrollWheel.GetAxisDelta(hand.handType).y);
            if (scrollDeltaMag < 0.01f) { return false; }
            if (!scrollStarted && scrollDeltaMag < scrollActivationThreshold) { return false; }

            // user did just put finger on touchpad for the first time, which caused high delta magnitude
            if (scrollWheel.GetLastAxis(hand.handType) == Vector2.zero) { return false; }

            // check if the finger touches the virtual "wheel"
            Vector2 fingerpos = scrollWheel.GetAxis(hand.handType);
            if (fingerpos.x < -wheel_width || fingerpos.x > wheel_width) { return false; }
            else if (fingerpos.y > wheel_height || fingerpos.y < -wheel_height) { return false; }

            // haptic feedback
            if (scrollStarted && Time.time > lastFeedback + 0.1f) {
                hand.hapticAction.Execute(0, 0.1f, 1, 0.1f, hand.handType);
                lastFeedback = Time.time;
            }
            return true;
        }

        public void ToggleVisibility() {
            if (!gameObject.activeSelf) {
                gameObject.transform.position = gameObject.GetComponent<LockToPoint>().snapTo.transform.position;
                gameObject.transform.rotation = gameObject.GetComponent<LockToPoint>().snapTo.transform.rotation;
                gameObject.SetActive(true);
            }
            else {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Takes care of hiding the scroll wheel after a while.</summary>
        private IEnumerator HideScrollWheelAfter(float hideAfterSeconds) {
            yield return new WaitForSecondsRealtime(hideAfterSeconds);
            if (hideWheel) {
                if (scrollWheelObject) { scrollWheelObject.gameObject.SetActive(false); }
                scrollStarted = false;
            }
        }



        // ########### STEAMVR_SECTION ########### //

        //-------------------------------------------------
        private void OnAttachedToHand(Hand attachedHand) {

            hand = attachedHand;
            // transform.localPosition = Vector3.zero;
            // transform.localRotation = Quaternion.identity;

            ShowLaser();

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
            CheckScrolling();
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
            if (lastHovered != null) {
                lastHovered.SendMessage("PointerExit", hand, SendMessageOptions.DontRequireReceiver);
                //lastHovered.SendMessage("PointerDetached", hand, SendMessageOptions.DontRequireReceiver);
            }

            HideLaser();

            Debug.Log("Pickup Laser Pointer detached from hand!");
            // Destroy(gameObject);
            // remove the controller from the input module
        }


        //-------------------------------------------------
        void OnDestroy() { ShutDown(); }

        private void ShutDown() {

            // remove the controller from the input module
            LaserPointerInputModule.instance.RemoveController(this);
        }


        // ---------------------------------------------------------------------------------------------------
        // EVENTS

        /// <summary>
        /// Notify last hovered gameobject that the pointer exit (e.g. causes to hide UI elements).
        /// </summary>
        public override void HideLaser() {
            base.HideLaser();
            if (lastHovered != null) {
                lastHovered.SendMessage("PointerExit", hand, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void OnPointerClick(PointerEventData eventData) {

            if (!VRVIS) { return; }
            if (!hand) { return; }

            GameObject go = eventData.pointerCurrentRaycast.gameObject;
            if (!go) { return; }

            //Debug.Log("Controller click on object!", go);

            // check if click reached a structure node
            StructureNodeInfo nodeInf = go.GetComponent<StructureNodeInfo>();
            if (nodeInf != null) { StructureNodeClicked(nodeInf.GetSNode(), go.transform); }

            // check if click reached a structure node of structure version 2
            StructureNodeInfoV2 nodeInfV2 = go.GetComponent<StructureNodeInfoV2>();
            if (nodeInfV2 != null) { StructureNodeClicked(nodeInfV2.GetSNode(), go.transform); }

            // ToDo: cleanup
            // BELOW CODE IS DISCARDED: pointer click is now handled by VariabilityModelInteraction.cs
            //VariabilityModelNodeInfo vmNodeInf = go.GetComponent<VariabilityModelNodeInfo>();
            //if (vmNodeInf != null) { VariabilityModelNodeClicked(vmNodeInf); }

            // code city uses the EventSystem and catches OnPointerClick()
            // - use this approach for future additions!
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
                    ControllerSelection selectionScript = hand.GetComponent<ControllerSelection>();
                    if (!selectionScript) {
                        Debug.LogError("Could not find selection script of hand!");
                        return;
                    }

                    // attach CWMoverController

                    // OLD VERSION
                    // There were multiple different laser pointers for interaction / moving windows / deleting windows.
                    // Here the blue laser would have been switched out with the yellow laser that has the CodeWindowMover script.
                    // Not needed anymore after updated CodeWindow design with and draggable title bar and close button.
                    // Also the laser pointer is now a real physical object that can be grabbed
                    //
                    // ControllerSelection.SelectableController cwMoverCtrl = selectionScript.GetController(cwMoverIndex);
                    //
                    //
                    // ControllerSelection.SelectableController cwMoverCtrl = selectionScript.GetController(cwMoverIndex);
                    // if (cwMoverCtrl == null) {
                    //     Debug.LogError("Could not find controller definition for CodeWindowMover! Ensure it exists.");
                    //     return;
                    // }
                    //
                    //// if attached, try to pass the node info to the cw mover instance
                    // bool attached = selectionScript.AttachController(cwMoverCtrl);
                    // if (!attached) { return; }

                    // NEW VERSION
                    // blue laser also has the CodeWindowMover script and does not get replaced
                    GameObject attachedObject = hand.currentAttachedObject;
                    var cwm = gameObject.GetComponent<HandWindowMover>();
                    cwm.isActive = true;
                    if (cwm) { cwm.SelectNode(node, true, clickedAt); }
                }
                else {

                    // ToDo: maybe show some information about the file?
                    // or make it light up for a short amount of time to show where it is?

                    Debug.LogWarning("File already spawned: " + node.GetName());
                }
            }
        }

    }
}
