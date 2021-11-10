﻿using System.Collections;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.LaserPointer;

namespace VRVis.Interaction.PsychicHand {

    /// <summary>
    /// Component that can control telekinesables.
    /// </summary>
    public class PsychicHand : IUILaserPointerPickup {

        // DEFINABLE IN UNITY EDITOR
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

        [Tooltip("How long to wait (seconds) after exiting something before another pulse occurs")]
        public float pulseWait = 0.05f;

        [Tooltip("If this is used in VRVis")]
        public bool VRVIS = true;

        public Transform head;

        public Hand hand;

        // PROPERTIES

        public GameObject TelekinesisAttachmentPoint { get; private set; }

        public bool IsActive { get; set; }
        public ITelekinesable GrabbedTelekinesable { get; private set; }
        public ITelekinesable FocusedTelekinesable { get; private set; }

        // PRIVATE FIELDS

        private bool lastToggleState = false;

        // to prevent haptic pulse spam
        private float lastPulseTime = 0;

        private bool scrolling = false;
        private bool scrollStarted = false;
        private bool hideWheel = false;
        private Coroutine hideScrollWheelCoroutine;
        private Vector2 lastScrollAxis;
        private Vector2 scrollChange;
        private Vector2 lastScrollChange;
        private float lastFeedback = 0;

        private bool playerWasGrippingBefore = false;
        private bool gripping = false;
        private bool focusing = false;           

        protected override void Initialize() {
            base.Initialize();

            pointer.SetActive(false);
            hitPoint.SetActive(false);

            TelekinesisAttachmentPoint = new GameObject("TelekinesisAttachmentPoint");
            TelekinesisAttachmentPoint.transform.SetParent(transform);

            Debug.Log("Initialize Super Hand!");
        }

        protected override void Update() {
            
            pointer.SetActive(false);
            hitPoint.SetActive(false);
            
            IsActive = true;

            if (GrabbedTelekinesable == null) {
                if (!CheckHeadAndHandAlignment()) {
                    IsActive = false;
                    return;
                }
            }

            UpdateCall();            

            // show pointer and hitPoint when hitting
            if (bHit) {
                if (hitInfo.transform != null) {
                    if (!hitPoint.activeSelf) {
                        // pointer.SetActive(true);
                        hitPoint.SetActive(true);
                    }
                }
            }

            TelekinesisAttachmentPositionUpdate();

            TelekinesisUpdate();

            CheckScrolling();
        }

        private bool IsAvailable() {
            return hand && IsLaserActive();
        }

        public override bool ButtonDown() {
            if (!IsAvailable()) { return false; }
            return grip.GetStateDown(hand.handType);
        }

        public override bool ButtonUp() {
            if (!IsAvailable()) { return false; }
            return grip.GetStateUp(hand.handType);
        }

        public override void OnExitControl(GameObject control) {

            if (!IsAvailable()) { return; }
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

        private bool CheckHeadAndHandAlignment() {
            float alignment = Vector3.Dot(transform.forward, head.forward);
            if (alignment < 0.95f) {
                return false;
            }

            return true;
        }

        private void TelekinesisAttachmentPositionUpdate() {
            Vector3 hmdToPsychicHand = transform.position - Player.instance.hmdTransform.transform.position;
            hmdToPsychicHand = new Vector3(hmdToPsychicHand.x, 0, hmdToPsychicHand.z);
            hmdToPsychicHand.Normalize();


            Vector3 attachmentPoint = transform.position + hmdToPsychicHand * 0.5f;
            Quaternion lookDirection = Quaternion.LookRotation(hmdToPsychicHand);

            TelekinesisAttachmentPoint.transform.position = attachmentPoint;
            TelekinesisAttachmentPoint.transform.rotation = lookDirection;
            // TelekinesisAttachmentPoint.transform.position = new Vector3(transform.position.y);
        }

        private void TelekinesisUpdate() {

            gripping = grip.GetState(hand.handType);

            if (bHit) {
                if (hitInfo.transform != null) {
                    var hitTelekinesable = hitInfo.transform.gameObject.GetComponent<ITelekinesable>();

                    if (hitTelekinesable != null) {
                        if (GrabbedTelekinesable == null) {
                            if (!focusing) {
                                focusing = true;
                                FocusedTelekinesable = hitTelekinesable;
                                FocusedTelekinesable.OnFocus(hand);
                            }
                            else {
                                if (gripping && !playerWasGrippingBefore) {
                                    playerWasGrippingBefore = true;
                                    hitTelekinesable.OnGrab();
                                    GrabbedTelekinesable = hitTelekinesable;
                                }
                            }
                        }
                    }
                    else {
                        if (focusing) {
                            focusing = false;
                            FocusedTelekinesable.OnUnfocus(hand);
                            FocusedTelekinesable = null;
                        }

                        if (gripping && !playerWasGrippingBefore) {
                            playerWasGrippingBefore = true;
                            GrabbedTelekinesable = null;
                        }
                    }
                }
            }
            else {
                if (focusing) {
                    focusing = false;
                    FocusedTelekinesable.OnUnfocus(hand);
                    FocusedTelekinesable = null;
                }
            }

            if (gripping && playerWasGrippingBefore && GrabbedTelekinesable != null) {
                CheckVelocityFlick(hand);
                GrabbedTelekinesable.OnDrag(hitPoint.transform);                
            }            

            if (!gripping && playerWasGrippingBefore) {
                playerWasGrippingBefore = false;

                if (GrabbedTelekinesable != null) {
                    GrabbedTelekinesable.OnRelease(ray);
                    GrabbedTelekinesable = null;
                }
            }
        }

        private float beginThreshold = 1.6f;
        private float endThreshold = 0.25f;
        private bool thresholdBroken = false;

        private void CheckVelocityFlick(Hand hand) {
            float handVelocity = hand.GetTrackedObjectVelocity().magnitude;
            float handAngularVelocity = hand.GetTrackedObjectAngularVelocity().magnitude;

            if (!thresholdBroken) {
                thresholdBroken = handVelocity > beginThreshold;
            }

            if (thresholdBroken) {
                if (handVelocity < endThreshold) {
                    if (GrabbedTelekinesable != null) {
                        GrabbedTelekinesable.OnPull();
                    }
                    thresholdBroken = false;
                }
            }
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
        void OnDestroy() { ShutDown(); }

        private void ShutDown() {

            // remove the controller from the input module
            LaserPointerInputModule.instance.RemoveController(this);
        }
    }
}
