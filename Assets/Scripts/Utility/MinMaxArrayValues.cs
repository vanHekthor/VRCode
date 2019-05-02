using System.Collections;
using System.Collections.Generic;

namespace VRVis.Utilities {

    /**
     * Mainly used by "CodeFile".
     * Allows to store min and max value for each index of an array.
     */
    public class MinMaxArrayValues {

        private readonly float[] minValue;
        private readonly float[] maxValue;

        private readonly bool[] minValueSet;
        private readonly bool[] maxValueSet;

        private readonly int arrSize;


        // CONSTRUCTOR

        public MinMaxArrayValues(int size) {
            arrSize = size;
            minValue = new float[size];
            maxValue = new float[size];
            minValueSet = new bool[size];
            maxValueSet = new bool[size];
        }


        // GETTER AND SETTER

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
