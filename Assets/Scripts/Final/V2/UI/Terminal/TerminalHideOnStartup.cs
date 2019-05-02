using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.Terminal {

    /// <summary>
    /// To show or hide the terminal on application startup.
    /// </summary>
    public class TerminalHideOnStartup : MonoBehaviour {

        [Tooltip("The element to hide")]
        public GameObject hideElement;

        [Tooltip("The state to set the element at (false = hide, true = show)")]
        public bool setState;

        [Tooltip("Trigger after that many frames")]
        public uint triggerAfterFrame = 1;

        private uint cnt = 0;


        void Update() {

            if (hideElement && cnt++ == triggerAfterFrame) {
                hideElement.SetActive(setState);
            }
        }

    }
}
