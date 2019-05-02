using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.UI.Helper {

    /// <summary>
    /// Helper script that can be attached to a UI element.<para/>
    /// It can then be used to easily change the according color of an object
    /// by sending a message at this object using the method name "ChangeColor" with a Color instance as value.
    /// </summary>
    public class ChangeColorHelper : MonoBehaviour {

        public Image img;

        /// <summary>Change the color.</summary>
        public void ChangeColor(Color color) {
            if (img) { img.color = color; }
        }

    }
}
