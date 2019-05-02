using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.VisualProperties.Methods {

    /// <summary>
    /// Adds range method attributes to a mapping method.<para/>
    /// Such are min/max and calculations.
    /// </summary>
    public abstract class ARangeMethod {

        protected float range_min = 0;
        protected float range_max = 100;
        protected bool range_min_set = false;
        protected bool range_max_set = false;

        public float GetRangeMin() { return range_min; }
        public void SetRangeMin(float min) { range_min = min; range_min_set = true; }
        public bool IsRangeMinSet() { return range_min_set; }

        public float GetRangeMax() { return range_max; }
        public void SetRangeMax(float max) { range_max = max; range_max_set = true; }
        public bool IsRangeMaxSet() { return range_max_set; }

        /// <summary>
        /// Get the percentage that describes
        /// the position of the value on the range.<para/>
        /// Min and max value of the range is passed as a parameter.
        /// </summary>
        protected float GetRangePercentage(float min, float max, float value) {
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
        protected float GetRangePercentage(float value) {
            return (value - GetRangeMin()) / (GetRangeMax() - GetRangeMin());
        }

    }
}
