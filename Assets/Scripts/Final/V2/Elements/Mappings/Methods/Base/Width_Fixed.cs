using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Mappings.Methods.Base {

    /// <summary>
    /// Width method for mapping a fixed value.
    /// </summary>
    public class Width_Fixed : ASizeMethod {

        // CONSTRUCTOR

        public Width_Fixed(string methodName, float size)
        : base(methodName, size, size) { }


        // GETTER AND SETTER

        public float GetWidth() { return GetFromSize(); }

    }
}

