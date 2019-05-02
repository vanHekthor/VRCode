using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.Utilities;

namespace VRVis.RegionProperties {

    /// <summary>
    /// Extends the "ARProperty" class for the property type "NFP".
    /// </summary>
    public class RProperty_NFP : ARProperty {

        private readonly float[] values;

        // store the current value calculated through PIM
        private bool currentValueSet = false;
        private float currentValue = -1;

	    
        // CONSTRUCTOR

        public RProperty_NFP(TYPE propertyType, string name, Region region, JObject o)
        : base(propertyType, name, region) {
        
            // get property values
            JArray jsonNodes = (JArray) o["value"];
            values = new float[jsonNodes.Count];
            for (int k = 0; k < jsonNodes.Count; k++) {
                values[k] = (float) jsonNodes[k];
            }
        
        }


        // GETTER AND SETTER

        /// <summary>Length of values array.</summary>
        public int GetValuesCount() { return values.Length; }

        public float[] GetValues() { return values; }

        public float GetValue(int index) { return values[index]; }

        public bool IsIndexValid(int index) {
            if (index < 0 || index > values.Length-1) { return false; }
            return true;
        }

        /// <summary>Tells if the value is calculated and valid.</summary>
        public bool GotValue() { return currentValueSet; }


        /// <summary>Get the current value.</summary>
        public float GetValue() { return currentValue; }

        /// <summary>Sets the current value and that it is valid.</summary>
        public void SetValue(float value) {
            currentValueSet = true;
            currentValue = value;
        }

        /// <summary>
        /// Sets the value at -1 and its validity at invalid.<para/>
        /// Should be applied before recalculation is done!
        /// </summary>
        public void ResetValue() {
            currentValueSet = false;
            currentValue = -1;
        }


        /// <summary>Calculates and returns the average of the values.</summary>
        public float GetAverageValue() {

            if (values.Length == 0) { return 0; }

            float sum = 0;
            foreach (float val in values) { sum += val; }
            return sum / values.Length;
        }

    }
}
