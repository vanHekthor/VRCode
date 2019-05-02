using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Siro.Regions {

    [System.Serializable]
    public class Region {

        public string id;
        public string location;
        public int start;
        public int end;
        public NodeProperty[] properties;

        /**
         * Returns some summed up information about this region.
         * Mainly for printing to debug.
         */
        public string info() {
            return "(id: " + id +
                ", location: " + location + 
                ", start: " + start +
                ", end: " + end +
                ", properties: " + properties.Length +
                ")";
        }

    }

}
