using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Mappings.Methods.Base {

    /// <summary>
    /// <para>Color method mapping a value between [0, 1].</para>
    /// <para>Provides a color gradient from a passed color to another.</para>
    /// <para>Optionally a neutral color that is inbetween, a neutral value and a ratio where the the neutral value is located inside [0,1] can be provided.</para>
    /// <para>The optional paramaters are especially useful for visualizing delta values meaning differences or scales with threshholds
    /// where values over, under and close to threshhold need to be differentiated visually.</para>
    /// </summary>
    public class Color_Scale : AColorMethod {

        public Color_Scale(string methodName, Color fromColor, Color toColor, Color neutralColor, float neutralValue, float ratio)
        : base(methodName, fromColor, toColor, neutralColor, neutralValue, ratio) { }

        public Color_Scale(string methodName, Color fromColor, Color toColor, Color neutralColor, float neutralValue)
        : base(methodName, fromColor, toColor, neutralColor, neutralValue) { }

        public Color_Scale(string methodName, Color fromColor, Color toColor, Color neutralColor)
        : base(methodName, fromColor, toColor, neutralColor) { }

        public Color_Scale(string methodName, Color fromColor, Color toColor)
        : base(methodName, fromColor, toColor) { }

    }
}
