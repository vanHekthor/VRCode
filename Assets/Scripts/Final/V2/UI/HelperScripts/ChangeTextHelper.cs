using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.UI.Helper {

    /// <summary>
    /// Helper script that can be attached to a UI element.<para/>
    /// It can then be used to easily change the according text
    /// by sending a message at this object using the method name "ChangeText" with a string as value.
    /// </summary>
    public class ChangeTextHelper : MonoBehaviour {

	    public Text text;
        public TextMeshProUGUI textTMP;

        /// <summary>Change the text.</summary>
        public void ChangeText(string newText) {
            if (text) { text.text = newText; }
            if (textTMP) { textTMP.SetText(newText); }
            //Debug.Log("Text changed: " + gameObject.name, gameObject);
        }

    }
}
