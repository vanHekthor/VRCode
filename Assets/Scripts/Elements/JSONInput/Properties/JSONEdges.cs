using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization {

    /**
     * Used for deserialization.
     * Basic class used with JSONUtility
     * to load the edges array from the JSON data.
     * 
     * Used by "EdgeLoader" class.
     */
    [System.Serializable]
    public class JSONEdges {
        
        public JSONEdge[] edges = new JSONEdge[]{};

    }

}
