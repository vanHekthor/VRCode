using System.Collections;
using System.Collections.Generic;

namespace VRVis.Utilities {

    /// <summary>
    /// Class used to store min/max values.<para/>
    /// This class is basically used by "Region" NFP properties to store their current min/max value
    /// but also by the "RegionLoader" to store a global min/max value for each NFP property.
    /// </summary>
    public class MinMaxValue {

        private float minValue;
        private float maxValue;

        private bool minValueSet = false;
        private bool maxValueSet = false;


        // CONSTRUCTOR

        public MinMaxValue() {}

        public MinMaxValue(float min, float max) {
            SetMinValue(min);
            SetMaxValue(max);
        }


        // GETTER AND SETTER

        public float GetMinValue() { return minValue; }
        public void SetMinValue(float value) {
            minValue = value;
            minValueSet = true;
        }

        public float GetMaxValue() { return maxValue; }
        public void SetMaxValue(float value) {
            maxValue = value;
            maxValueSet = true;
        }

        public bool IsMinValueSet() { return minValueSet; }
        public bool IsMaxValueSet() { return maxValueSet; }


        // FUNCTIONALITY

        /// <summary>Checks the value and updates min/max accordingly.</summary>
        public void Update(float value) {
            if (value < GetMinValue() || !IsMinValueSet()) { SetMinValue(value); }
            if (value > GetMaxValue() || !IsMaxValueSet()) { SetMaxValue(value); }
        }

        /// <summary>
        /// Allows to reset the min max properties so that
        /// they are re-calculated in the next ProcessValues call.<para/>
        /// This method will not change the stored min/max values.
        /// </summary>
        public void ResetMinMax() {
            minValue = 0;
            maxValue = 0;
            minValueSet = false;
            maxValueSet = false;
        }

        /// <summary>
        /// Get the percentage that describes
        /// the position of the value on the range.<para/>
        /// Min and max value of the range is passed as a parameter.<para/>
        /// Returns 0 if the value is less than the min and 1 if greater than max.
        /// </summary>
        public float GetRangePercentage(float min, float max, float value) {

            if (value <= min) { return 0; }
            if (value >= max) { return 1; }

            double nominator = (value - min);
            double denominator = (max - min);
            if (nominator == 0 || denominator == 0) { return 0; }

            return (float) (nominator / denominator);
        }

        /// <summary>
        /// Get the percentage that describes
        /// the position of the value on the range.<para/>
        /// Uses the initernal stored range values.
        /// </summary>
        public float GetRangePercentage(float value) {
            return GetRangePercentage(GetMinValue(), GetMaxValue(), value);
        }

        /// <summary>
        /// Returns the value if it is inside the bounds,
        /// otherwise returns the cropped value.<para/>
        /// Checks also if the min/max values are set!
        /// </summary>
        public float CropToBounds(float value) {
            if (minValueSet && value < minValue) { return minValue; }
            if (maxValueSet && value > maxValue) { return maxValue; }
            return value;
        }

    }
}
