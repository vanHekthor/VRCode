using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization {

    /**
     * Used for deserialization.
     * Basic class used with JSONUtility
     * to load a region from JSON data.
     * 
     * Used by "RegionLoader" class.
     * 
     * (DEPRECATED)
     * (No longer used in 2019 version!)
     */
    [System.Serializable]
    public class JSONRegion {

        public string id;
        public string location;
        public int[] nodes;
        public JSONProperty[] properties;

    }

}
