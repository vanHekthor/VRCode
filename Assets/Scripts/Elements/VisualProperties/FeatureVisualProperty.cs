using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.VisualProperties {

    /**
     * An instance of a visual property for feature properties.
     * Wont hold any important information compared to other types of visual properties.
     * 
     * Could be used to count occurrences of a feature
     * but we can do this on another way as well so thats why the comments.
     */
    public class FeatureVisualProperty : VisualProperty {

        //private int featureCount = 0;


        // CONSTRUCTOR

        public FeatureVisualProperty(string propertyName, bool active)
        : base(propertyName, active) {
            // ...
        }


        // GETTER AND SETTER

        /*
        public int GetFeatureCount() { return featureCount; }
        public void SetFeatureCount(int count) { featureCount = count; }
        */


        // FUNCTIONALITY

        /** Called for each region that has this property. */
        /*
        public void Process() {
            
            // just count occurrences
            featureCount++;

        }
        */

    }
}
