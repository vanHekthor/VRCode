using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Siro.VisualProperties {

    /**
     * A visual property for single numeric values.
     * This could be e.g. a performance value.
     * Datatype of the value is float.
     * 
     * You can also set min and max values for
     * if the method to call has a range that the user can define.
     */
    public class VisProp_SingleValue : VisProp {

	    private float value = 0;
        private float min = 0;
        private float max = 0;


        // CONSTRUCTORS

        public VisProp_SingleValue(string type, string method, ATTRIBUTE attribute, float value)
        : base(type, method, attribute) {
            this.value = value;
        }

        public VisProp_SingleValue(string type, string method, ATTRIBUTE attribute, float value, float min, float max)
        : base(type, method, attribute) {
            this.value = value;
            this.min = min;
            this.max = max;
        }


        // GETTER AND SETTER

        public float GetValue() { return value; }
        public void SetValue(float value) { this.value = value; }

        public float GetMin() { return min; }
        public void SetMin(float min) { this.min = min; }

        public float GetMax() { return max; }
        public void SetMax(float max) { this.max = max; }


        // FUNCTIONALITY

        public override void Apply() {
            
            // call the method with given properties
            
        }
    }

}