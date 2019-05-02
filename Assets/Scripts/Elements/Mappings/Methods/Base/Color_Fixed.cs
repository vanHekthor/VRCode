using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Mappings.Methods.Base {

    /// <summary>
    /// Color method mapping for a fixed color value.
    /// </summary>
    public class Color_Fixed : AColorMethod {

        // CONSTRUCTOR

        public Color_Fixed(string methodName, Color color)
        : base(methodName, color, color) {}


        // GETTER AND SETTER

        public void SetColor(Color color) {
            SetFromColor(color);
            SetToColor(color);
        }

        /// <summary>Get the fixed color.</summary>
        public Color GetColor() { return GetFromColor(); }

    }
}
