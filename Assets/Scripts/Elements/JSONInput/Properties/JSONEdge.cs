using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization {

    /**
     * Used for deserialization.
     * Basic class used with JSONUtility
     * to load an edge (a connection between two nodes).
     */
    [System.Serializable]
    public class JSONEdge {

        [System.Serializable]
        public class Range {
            public int from;
            public int to;
        }

        [System.Serializable]
        public class NodeLocation {
            public string file;
            public Range lines;
            public Range columns;            
        }

        public string type;
        public string label;
        public NodeLocation from;
        public NodeLocation to;
        public float value;
	    
    }
}
