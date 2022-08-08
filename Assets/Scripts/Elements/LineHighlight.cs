using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRVis.Elements {

    /// <summary>
    /// Manages the a highlighted code region. Is attached to every line highlight object but does nothing at the moment.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LineHighlight : MonoBehaviour {
        private Image background;

        void Start() {
            background = transform.GetComponent<Image>();
        }

        public void ChangeColor(Color color) {
            background.color = color;
        }
    }
}
