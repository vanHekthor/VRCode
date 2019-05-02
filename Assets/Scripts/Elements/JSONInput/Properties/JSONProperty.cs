using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization {

    /**
     * Used for deserialization.
     * Basic class used with JSONUtility
     * to load a property of a region.
     * 
     * Used by "RegionLoader" class.
     * 
     * (DEPRECATED)
     * (No longer used in 2019 version!)
     */
    [System.Serializable]
    public class JSONProperty {

        public string type;
        public string value; // deserialization to type "object" always yields "null" so don't use it
        public string unit;

    }

}
