using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Mappings.Methods.Base {

    /// <summary>
    /// Color method mapping a value between [0, 1]
    /// at a color value interpolated of two initial colors.
    /// </summary>
    public class Color_Scale : AColorMethod {

        public Color_Scale(string methodName, Color fromColor, Color toColor)
        : base(methodName, fromColor, toColor) { }

    }
}
