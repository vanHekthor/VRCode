using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.UI.CodeCity {

    /// <summary>
    /// Class that adds text to the hover UI of code city.<para/>
    /// Needs to be attached to the hover UI prefab and configured accordingly.
    /// </summary>
    public class CCUITextAdder : MonoBehaviour {

        [Tooltip("The text prefab with the ChangeTextHelper script attached")]
        public GameObject textPrefab;

        [Tooltip("Container to add new text elements to")]
        public Transform textContainer;

        // ToDo: improved versions possible with nice layout
        public void AddText(string headline, string content, bool noHeadline = false) {

            if (!textPrefab || !textContainer) { return; }
            
            GameObject t = Instantiate(textPrefab, textContainer);
            string text = noHeadline ? content : headline + ": " + content;
            t.SendMessage("ChangeText", text, SendMessageOptions.RequireReceiver);
        }

    }
}
