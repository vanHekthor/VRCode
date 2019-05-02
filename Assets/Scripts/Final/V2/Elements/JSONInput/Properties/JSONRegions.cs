using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization {

    /**
     * Used for deserialization.
     * Basic class used with JSONUtility
     * to load the regions array from the JSON data.
     * 
     * Used by "RegionLoader" class.
     * 
     * (DEPRECATED)
     * (No longer used in 2019 version!)
     */
    [System.Serializable]
    public class JSONRegions {
        
        public JSONRegion[] regions = new JSONRegion[]{};

    }

}
