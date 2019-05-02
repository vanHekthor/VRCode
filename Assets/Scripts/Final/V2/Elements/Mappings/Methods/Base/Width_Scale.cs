using System.Collections;
using System.Collections.Generic;

namespace VRVis.Mappings.Methods.Base {

    /// <summary>
    /// Width method for mapping a value between [0, 1]
    /// at a new value interpolated of two initial range values.
    /// </summary>
    public class Width_Scale : ASizeMethod {

        // CONSTRUCTOR

        public Width_Scale(string methodName, float fromSize, float toSize)
        : base(methodName, fromSize, toSize) { }


        // GETTER AND SETTER

        // ToDo: cleanup
        /*
        public void SetRangeMin(float min) { GetRange().SetMinValue(min); }

        public void SetRangeMax(float max) { GetRange().SetMaxValue(max); }
        */

    }
}

