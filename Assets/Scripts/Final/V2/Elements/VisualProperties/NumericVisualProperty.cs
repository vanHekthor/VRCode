using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.VisualProperties {

    /**
     * An instance of a visual property for numeric properties.
     * A numeric property is a property with a numeric value.
     * The data type of this value is "float".
     * This instance stores data collected from all nodes.
     * Such data could be ranges, given by "min" and "max" values.
     * Applying this visual property will use the specified property methods
     * to map property values to visual features of a code region/node.
     * 
     * (06.01.2019)
     * This is a new version of the NumericalVisualProperty.
     * It uses arrays instead of single values because every region entry
     * has a value for each software system configuration.
     * So the values at the same index are always for the specific configuration!
     */
    public class NumericVisualProperty : VisualProperty {

        private float[] minValue;
        private float[] maxValue;

        private bool[] minValueSet;
        private bool[] maxValueSet;

        private int arrSize = 0;
        private bool arraysInitialized = false;

	    
        // CONSTRUCTOR

        public NumericVisualProperty(string propertyName, bool active)
        : base(propertyName, active) {}


        // GETTER AND SETTER

        /** Tells if the arrays are initialized. */
        public bool IsInitialized() { return arraysInitialized; }

        public void InitializeArrays(int size) {
            arrSize = size;
            minValue = new float[size];
            maxValue = new float[size];
            minValueSet = new bool[size];
            maxValueSet = new bool[size];
            arraysInitialized = true;
        }

        public int GetArraySize() { return arrSize; }

        public float GetMinValue(int index) { return minValue[index]; }
        public void SetMinValue(int index, float value) {
            minValue[index] = value;
            minValueSet[index] = true;
        }

        public float GetMaxValue(int index) { return maxValue[index]; }
        public void SetMaxValue(int index, float value) {
            maxValue[index] = value;
            maxValueSet[index] = true;
        }

        public bool IsMinValueSet(int index) { return minValueSet[index]; }
        public bool IsMaxValueSet(int index) { return maxValueSet[index]; }


        // FUNCTIONALITY

        /** Called for each region that has this property. */
        public void ProcessValues(float[] values) {

            // initialize the arrays
            int size = values.Length;
            if (!IsInitialized()) { InitializeArrays(size); }
            else if (size != arrSize) {
                Debug.LogError("CORRUPT DATA! Value arrays have a different size!");
            }

            // update min and max values accordingly
            for (int k = 0; k < size; k++) {
                float value = values[k];
                if (value < 0) { value = 0; } // negative numbers not allowed (they define "missing" information)
                if (value < GetMinValue(k) || !IsMinValueSet(k)) { SetMinValue(k, value); }
                if (value > GetMaxValue(k) || !IsMaxValueSet(k)) { SetMaxValue(k, value); }
            }
        }

        /**
         * Allows to reset the min max properties so that
         * they are re-calculated in the next ProcessValues call.
         * This method will not change the stored min/max values.
         */
        public void ResetMinMax() {
            for (int i = 0; i < GetArraySize(); i++) {
                minValueSet[i] = false;
                maxValueSet[i] = false;
            }
        }

    }
}
