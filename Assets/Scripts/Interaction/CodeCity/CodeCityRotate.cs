using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Spawner;

namespace VRVis.Interaction.CodeCity {

    /// <summary>
    /// Attached to the rotation base plate.<para/>
    /// Allows the user to interact with the plate
    /// so that it can be rotated around its y-axis.<para/>
    /// This rotation is then used to adjust the city rotation.<para/>
    /// Created: 28.08.2019 (Leon H.)<para/>
    /// Updated: 28.08.2019
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class CodeCityRotate : MonoBehaviour {

        public CodeCityV1 codeCity; // assigned by CodeCityBase on prefab creation
        public bool rotateCodeCity = true;

        [Tooltip("Keep rotating after hand is detached")]
        public bool maintainMomentum = true;

        [Tooltip("Damp how fast the rotation stops")]
        [Range(0, 10)] public float momentumDampRate = 0.5f;

        [Tooltip("Minimum of change to stop the rotation faster")]
        public float changeRateMin = 0.05f;

        private readonly Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.DetachFromOtherHand;

	    private Interactable interactable;

        private Vector3 grabPosition;
        private Quaternion startRotation;
        private Quaternion rotationOffset;

        private readonly int numSamples = 5;
        private float[] changeSamples; // change in y-rotation of code city
        private int sampleCounter = 0;
        private float changeRate = 0;


        private void Awake() {
            interactable = GetComponent<Interactable>();
            changeSamples = new float[numSamples];
        }

        private void Start() {
            if (rotateCodeCity && !codeCity) { Debug.LogWarning("CodeCity component not assigned!"); }
        }


        /// <summary>
        /// Called every Update while hand hovers over this interactable.
        /// </summary>
        private void HandHoverUpdate(Hand hand) {

            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool grabEnding = hand.IsGrabEnding(gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None) {

                grabPosition = hand.transform.position;
                startRotation = codeCity.transform.localRotation;
                sampleCounter = 0;
                changeRate = 0;

                hand.AttachObject(gameObject, startingGrabType, attachmentFlags);
                hand.HoverLock(interactable);
            }
            else if (grabEnding) {
                hand.DetachObject(gameObject);
                hand.HoverUnlock(interactable);
            }
        }


        /// <summary>
        /// Called every Update while gameobject is attached to hand.
        /// </summary>
        private void HandAttachedUpdate(Hand hand) {
            UpdateRotation(hand.transform);
        }


        /// <summary>
        /// Calculate the mapping based on the transform's position.
        /// </summary>
        private Quaternion CalculateRotationOffset(Transform updateTransform) {
            Vector3 from = transform.position - grabPosition;
            Vector3 to = transform.position - updateTransform.position;
            from.y = to.y = 0;
            return Quaternion.FromToRotation(from.normalized, to.normalized);
        }


        /// <summary>
        /// Updates the linear mapping based on the given transform.<para/>
        /// Also takes care of rotating the city accordingly.
        /// </summary>
        private void UpdateRotation(Transform updateTransform) {

            Quaternion prevRotOffset = rotationOffset;
            rotationOffset = CalculateRotationOffset(updateTransform);
            changeSamples[sampleCounter % changeSamples.Length] = (1.0f / Time.deltaTime) * (rotationOffset.y - prevRotOffset.y);
            sampleCounter++;

            if (rotateCodeCity && codeCity) {
                codeCity.transform.localRotation = rotationOffset * startRotation;
            }
        }


        private void OnAttachedToHand(Hand hand) {
            Debug.Log(string.Format("CodeCityRotate attached to hand {0}", hand.name), this);
        }

        private void OnDetachedFromHand(Hand hand) {
            Debug.Log(string.Format("CodeCityRotate detached from hand {0}", hand.name), this);
            CalculateChangeRate();
        }


        /// <summary>
        /// Calculates the change rate for further rotation.
        /// </summary>
        private void CalculateChangeRate() {
            if (changeSamples.Length == 0) { return; }
            changeRate = 0;
            int sampleCount = Mathf.Min(sampleCounter, changeSamples.Length);
            for (int i = 0; i < sampleCount; i++) { changeRate += changeSamples[i]; }
            changeRate /= sampleCount;
        }


        private void Update() {
            
            // apply rest of momentum
            if (maintainMomentum && changeRate != 0) {

                changeRate = Mathf.Lerp(changeRate, 0, momentumDampRate * Time.deltaTime);
                
                // stop faster for too small rate
                if (Mathf.Abs(changeRate) < changeRateMin) { changeRate = Mathf.Lerp(changeRate, 0, Time.deltaTime); }

                if (rotateCodeCity && codeCity) {
                    codeCity.transform.Rotate(Vector3.up, changeRate, Space.Self);
                }
            }
        }

    }
}
