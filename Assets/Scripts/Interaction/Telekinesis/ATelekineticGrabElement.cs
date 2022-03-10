using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.PsychicHand;
using VRVis.Spawner.File;
using VRVis.UI.Helper;
using VRVis.Utilities;

namespace VRVis.Interaction.Telekinesis {
    public abstract class ATelekineticGrabElement : MonoBehaviour, ITelekinesable {

        public Transform grabTransform;
        public Transform stretchTransform;
        public ParticleSystem focusEffect;
        public ParticleSystem grabEffect;
        public float snapTime = 1;

        public AnimationCurve distanceIntensityCurve = AnimationCurve.Linear(0.0f, 800.0f, 1.0f, 800.0f);
        public AnimationCurve pulseIntervalCurve = AnimationCurve.Linear(0.0f, 0.01f, 1.0f, 0.0f);

        public float pulseIntensity = 1000.0f;
        public float pulseInterval = FocusPulseInterval;

        public bool IsVisible { get; private set; }

        protected Transform targetPoint;
        protected bool attachedToHand = false;
        protected Transform telekineticAttachmentPoint;
        protected bool stretching;

        private const float FocusPulseInterval = 0.5f;
        private const float GrabPulseInterval = 0.33f;

        private Camera vrCamera;
        private Collider elementCollider;

        private bool grabbed = false;
        private bool focused = false;

        private IEnumerator pulseCoroutine;      
        
        private float dropTimer;
        private bool moveToTarget = false;
        private bool moveTowardsHand = false;
        private Vector3 originalScale;

        void Start() {
            if (grabTransform == null) {
                Debug.LogError("Object to be grabbed is null. " +
                    "Probably reference needs to be set in the inspector!");
            }

            if (stretchTransform == null) {
                Debug.LogError("Object to be stretched is null. " +
                    "Probably reference needs to be set in the inspector!");
            }

            originalScale = grabTransform.localScale;

            // vrCamera = Player.instance.gameObject.GetComponentInChildren<Camera>();
            elementCollider = gameObject.GetComponent<Collider>();

            Initialize();
        }

        protected abstract void Initialize();

        void Update() {
            if (!moveToTarget || attachedToHand) {
                dropTimer = -1;
            }
            else {
                dropTimer += Time.deltaTime / (snapTime / 2);

                if (dropTimer > 1) {
                    //transform.parent = snapTo;
                    grabTransform.position = targetPoint.position;
                    grabTransform.rotation = targetPoint.rotation;
                    moveToTarget = false;

                    if (moveTowardsHand) {
                        attachedToHand = true;
                        moveTowardsHand = false;
                    }
                }
                else {
                    float t = Mathf.Pow(35, dropTimer);
                    grabTransform.position = Vector3.Lerp(grabTransform.position, targetPoint.position, Time.fixedDeltaTime * t * 3);
                    grabTransform.rotation = Quaternion.Slerp(grabTransform.rotation, targetPoint.rotation, Time.fixedDeltaTime * t * 2);

                    if (moveTowardsHand) {
                        grabTransform.localScale = Vector3.Lerp(grabTransform.localScale, originalScale * 0.67f, Time.fixedDeltaTime * t * 3);
                    }
                    else {
                        grabTransform.localScale = Vector3.Lerp(grabTransform.localScale, originalScale, Time.fixedDeltaTime * t * 3);
                    }
                }
            }
        }

        public void OnFocus(Hand hand) {
            Debug.Log("Telekinetic power entered object!");

            if (!focused) {
                focused = true;
                focusEffect.Play();

                pulseCoroutine = HapticFeedback(hand);
                //StartCoroutine(pulseCoroutine);
            }

            telekineticAttachmentPoint = hand.GetComponentInChildren<PsychicHand>().TelekinesisAttachmentPoint.transform;
            if (telekineticAttachmentPoint == null) {
                Debug.LogError("Hand is missing psychic hand component and/or " +
                    "psychic hand component has no telekinesis attachement point!");
            }
        }

        public void OnUnfocus(Hand hand) {
            Debug.Log("Telekinetic power exited object!");

            if (focused) {
                focused = false;
                focusEffect.Stop();

                //if (!grabbed) {
                //    StopCoroutine(pulseCoroutine);
                //}
            }
        }

        public void OnGrab() {
            Debug.Log("Grabbed object via telekinesis!");
            pulseInterval = GrabPulseInterval;
            if (!grabbed) {
                grabEffect.Play();
            }

            grabbed = true;
            WasGrabbed();
        }

        protected abstract void WasGrabbed();

        public void OnPull() {
            //zoomButton.OnPointerClick(null);
            moveTowardsHand = true;
            ChangeTargetPoint(telekineticAttachmentPoint);
            WasPulled();
        }

        protected abstract void WasPulled();

        public void OnDrag(Transform pointer) {     
            if (moveTowardsHand) {
                ChangeTargetPoint(telekineticAttachmentPoint);
            }
            IsBeingDragged(pointer);
        }

        protected abstract void IsBeingDragged(Transform pointer);

        public void OnRelease(Ray ray) {
            Debug.Log("Released telekinetically grabbed object!");
            pulseInterval = FocusPulseInterval;

            if (grabbed) {
                grabEffect.Stop();
                //if (!focused) {
                //    StopCoroutine(pulseCoroutine);
                //}
            }

            grabbed = false;
            moveTowardsHand = false;
            attachedToHand = false;

            WasReleased(ray);
        }

        protected abstract void WasReleased(Ray ray);

        public abstract void OnStretch(float factor);

        public abstract void OnStretchEnded();

        private IEnumerator HapticFeedback(Hand hand) {
            while (true) {
                if (hand != null) {
                    float pulse = pulseIntensity;
                    hand.TriggerHapticPulse((ushort)pulse);

                    //SteamVR_Controller.Input( (int)trackedObject.index ).TriggerHapticPulse( (ushort)pulse );
                }

                float nextPulse = pulseInterval;

                yield return new WaitForSeconds(nextPulse);
            }
        }

        protected void ChangeTargetPoint(Transform target) {
            // dropTimer = -1;
            moveToTarget = true;
            targetPoint = target;
        }

        protected void ChangeTargetPoint(Vector3 position, Quaternion rotation) {
            // dropTimer = -1;
            moveToTarget = true;
            targetPoint.position = position;
            targetPoint.rotation = rotation;
        }
    }
}