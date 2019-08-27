using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Spawner;

namespace VRVis.Interaction.CodeCity {

    /// <summary>
    /// Attached to the lift base plate.<para/>
    /// Allows the user to interact with the plate
    /// so that it can be moved up and down.<para/>
    /// This is then used to adjust the code city position.<para/>
    /// Script based on "LinearDrive.cs" by Valve (SteamVR).<para/>
    /// Created: 27.08.2019 (Leon H.)<para/>
    /// Updated: 27.08.2019
    /// </summary>
    [RequireComponent( typeof( Interactable ) )]
    public class CodeCityLift : MonoBehaviour {

		public Vector3 startPosition;
		public Vector3 endPosition = Vector3.up;

        public CodeCityV1 codeCity; // assigned by CodeCityBase on prefab creation
        public bool positionCodeCity = true;

        private Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.DetachFromOtherHand;

        private Interactable interactable;

        private float mappingValue = 0;
        private float initialMappingOffset = 0;


        private void Awake() {
            interactable = GetComponent<Interactable>();
        }

        private void Start() {
            if (!codeCity) { Debug.LogWarning("Code city component not assigned!"); }
            if (positionCodeCity && codeCity) { UpdateLinearMapping(codeCity.transform); }
        }


        /// <summary>
        /// Called every Update while hand hovers over this interactable.<para/>
        /// (see SteamVR InteractableExample.cs for more information)
        /// </summary>
        private void HandHoverUpdate(Hand hand) {

            GrabTypes startingGrab = hand.GetGrabStarting();
            bool grabEnding = hand.IsGrabEnding(gameObject);

            // check if there is nothing attached yet and user performs grab
            if (interactable.attachedToHand == null && startingGrab != GrabTypes.None) {
                initialMappingOffset = mappingValue - CalculateLinearMapping(hand.transform);
                hand.AttachObject(gameObject, startingGrab, attachmentFlags); // attach to hand
                hand.HoverLock(interactable); // prevent hand hovering over anything else
            }
            else if (grabEnding) {
                hand.DetachObject(gameObject); // detach from hand
                hand.HoverUnlock(interactable); // free hover lock
            }
        }


        /// <summary>
        /// Called every Update while attached to the hand.<para/>
        /// (see SteamVR InteractableExample.cs for more information)
        /// </summary>
        private void HandAttachedUpdate(Hand hand) {
            UpdateLinearMapping(hand.transform);
        }


        /// <summary>
        /// Maps the position of the transform to the range of start and end position.<para/>
        /// This returns 0 if at start and 1 if at end, "infront" of start will result in negative values.<para/>
        /// The returned value is not clamped to the range 0-1.
        /// </summary>
        private float CalculateLinearMapping(Transform updateTransform) {

            Vector3 liftDir = endPosition - startPosition;
            float liftLength = liftDir.magnitude;
            liftDir.Normalize();

            Vector3 displacement = updateTransform.position - startPosition;

            // calculate cosine of angle between the two vectors
            return Vector3.Dot(displacement, liftDir) / liftLength;
        }


        /// <summary>
        /// Updates the mapping based on the transform position and
        /// moves the code city accordingly.
        /// </summary>
        private void UpdateLinearMapping(Transform updateTransform) {

            // get the new mapping value and clamp it between 0 and 1
            mappingValue = Mathf.Clamp01(initialMappingOffset + CalculateLinearMapping(updateTransform));

            if (positionCodeCity && codeCity) {
                Vector3 ccPos = codeCity.transform.position;
                ccPos.y = Mathf.Lerp(startPosition.y, endPosition.y, mappingValue);
                codeCity.transform.position = ccPos;
            }
        }


        private void OnAttachedToHand(Hand hand) {
            Debug.Log(string.Format("CodeCityLift attached to hand {0}", hand.name), this);
        }

        private void OnDetachedFromHand(Hand hand) {
            Debug.Log(string.Format("CodeCityLift detached from hand {0}", hand.name), this);
        }

    }
}
