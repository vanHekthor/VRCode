using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace VRVis.UI.Terminal {

    /// <summary>
    /// Attached to a terminal rotation point.<para/>
    /// The player can graph it and rotate the terminal.<para/>
    /// Requires a "Interactable" component to be attached as well.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class TerminalRotationPoint : MonoBehaviour {

        public GameObject terminal;

        [Tooltip("The center position of the object represented")]
        public Transform center;

        [Tooltip("Ignores the hand offset on grab and always uses the center of this rotation point")]
        public bool snapToPointCenter = false;

        [Tooltip("Do not instantly stop rotating if turned fast")]
        public bool useRotationVelocity = true;

        protected Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.DetachFromOtherHand;

        private Quaternion rotationOffset = Quaternion.identity;
        private Interactable interactable;
        private Transform grabbed;

        private bool wasRotating = false;
        private static readonly uint queueCapacity = 5;
        private Queue<float> lastRotAngleQueue = new Queue<float>((int) queueCapacity);
        private Vector3 prevDirection = Vector3.zero;
        private bool prevDirSet = false;


	    void Start () {

            if (!terminal) { Debug.LogError("Missing terminal!"); }

		    interactable = GetComponent<Interactable>();
            if (!interactable) { Debug.LogError("Missing interactable!", this); }
	    }


        /// <summary>
        /// Called while hovering over the interactable.
        /// </summary>
        protected virtual void HandHoverUpdate(Hand hand) {

            if (!terminal) { return; }

            prevDirSet = false;
            wasRotating = false;
            lastRotAngleQueue.Clear();

            GrabTypes startingGrabType = hand.GetGrabStarting();

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None) {

                grabbed = center ?? transform; // use assigned object or self

                // offset rotation between direction of hand and this rotation point
                if (!snapToPointCenter) {
                    Vector3 handDir = hand.transform.position - terminal.transform.position;
                    Vector3 pointDir = grabbed.position - terminal.transform.position;
                    handDir.y = 0; // look on x-z plane
                    pointDir.y = 0;
                    rotationOffset = Quaternion.FromToRotation(handDir, pointDir);
                }
                else { rotationOffset = Quaternion.identity; }

                // stop possible rotation
                terminal.SendMessage("SetVelocity", 0f, SendMessageOptions.DontRequireReceiver);

                hand.AttachObject(gameObject, startingGrabType, attachmentFlags);
            }
		}


        /// <summary>
        /// Called every frame from attached hand.
        /// </summary>
        protected virtual void HandAttachedUpdate(Hand hand) {

            UpdateTerminalRotation(hand.transform.position);

            if (hand.IsGrabEnding(gameObject)) {
                hand.DetachObject(gameObject);
                wasRotating = true;
            }
        }

        protected virtual void OnDetachedFromHand(Hand hand) {
            wasRotating = true;
        }


        /// <summary>
        /// Update the rotation of the terminal
        /// </summary>
        private void UpdateTerminalRotation(Vector3 handPos) {

            if (!terminal) { return; }

            //Vector3 lookAt = hand.transform.forward; // doesn't work as good as the new approach
            Vector3 lookAt = handPos - terminal.transform.position;
            lookAt.y = 0; // lock on x-z plane
            lookAt.Normalize();

            Vector3 currentDirection = grabbed.position - terminal.transform.position;
            currentDirection.y = 0; // lock on x-z plane
            currentDirection.Normalize();

            // store previous direction
            if (!prevDirSet) { prevDirection = currentDirection; prevDirSet = true; }

            Quaternion lookRot = Quaternion.FromToRotation(currentDirection, lookAt);
            Quaternion newRotation = rotationOffset * lookRot * terminal.transform.rotation;;

            // store rotation delta for velocity calculation
            if (lastRotAngleQueue.Count + 1 >= queueCapacity) { lastRotAngleQueue.Dequeue(); }
            float newValue = Vector3.SignedAngle(prevDirection, currentDirection, Vector3.up);
            lastRotAngleQueue.Enqueue(newValue);
            prevDirection = currentDirection;

            // apply rotation
            terminal.transform.rotation = newRotation;
        }


        void Update() {

            // keep rotating if activated
            if (useRotationVelocity && wasRotating && interactable.attachedToHand == null) {
                terminal.SendMessage("SetVelocity", GetRotAngleDelta(), SendMessageOptions.DontRequireReceiver);
                wasRotating = false;
            }
        }


        /// <summary>
        /// Get the rotation delta (average of values in the queue).
        /// </summary>
        private float GetRotAngleDelta() {

            if (lastRotAngleQueue.Count == 0) { return 0; }

            float sum = 0;
            lastRotAngleQueue.ForEach(v => sum += v);
            return sum / lastRotAngleQueue.Count;
        }

    }
}
