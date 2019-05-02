using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI {

    /// <summary>
    /// Updates a rect transform with the values of another objects rect transform.<para/>
    /// Add this script to both objects that should be linked (receiver and sender).<para/>
    /// Apply the object that should receive changes to the receiver property.<para/>
    /// You don't have to set a receiver if this object is only a receiver and no sender!
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformDimensionLink : MonoBehaviour {
    
        [Tooltip("Minimum size that this object can have")]
        public Vector2 minSize = new Vector2();

        [System.Serializable]
        public class Receiver {
            public RectTransformDimensionLink link;
            public bool applyWidth = true;
            public bool applyHeight = true;
        }

        [Tooltip("Objects with this script to call. Leave empty if this object is only a receiver and no sender.")]
        public Receiver[] sendTo;
        private RectTransform thisRT;
	
	    void Awake () {
		
            // Get the attached Rect Transform component of this object.
            thisRT = GetComponent<RectTransform>();
            if (!thisRT) {
                Debug.LogWarning("This object does not have a rect transform attached!");
                return;
            }
	    }

        /**
         * Called if the rect transform dimension changes.
         */ 
        private void OnRectTransformDimensionsChange() {

            // do nothing if no receiver is set or RectTransform is missing
            //Debug.Log("Rect Transform Dimension changed!");
            if (sendTo.Length == 0 || !thisRT) { return; }

            //Debug.Log("Sending Update to receiver!");
            foreach (Receiver receiver in sendTo) {
                receiver.link.UpdateThisRectTransform(thisRT, receiver);
            }
        }

        /// <summary>Called by the sender if there is an update available.</summary>
        /// <param name="other">RectTransform of the sender</param>
        /// <param name="receiver">Holds Receiver component of the object to update</param>
        void UpdateThisRectTransform(RectTransform other, Receiver receiver) {
        
            //Debug.Log("Received Update!");
            if (!thisRT) { return; }

            // update size property
            Vector2 newSizeDelta = thisRT.sizeDelta;

            float newWidth = other.sizeDelta.x;
            float newHeight = other.sizeDelta.y;

            // validate width and height
            if (newWidth < minSize.x) { newWidth = minSize.x; }
            if (newHeight < minSize.y) { newHeight = minSize.y; }

            // add new width and height to the new size vector
            if (receiver.applyWidth) { newSizeDelta.x = newWidth; }
            if (receiver.applyHeight) { newSizeDelta.y = newHeight; }

            // apply size
            thisRT.sizeDelta = newSizeDelta;
        }

    }
}
