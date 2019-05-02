using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;

namespace VRVis.RegionProperties {

    /**
     * Abstract "RegionProperty" class.
     */
    public abstract class ARProperty {

        public enum TYPE { UNKNOWN, FEATURE, NFP };

        private readonly TYPE propertyType;
        private readonly string name;
        private readonly Region region;


        // CONSTRUCTORS

        public ARProperty(TYPE propertyType, string name, Region region) {
            this.propertyType = propertyType;
            this.name = name;
            this.region = region;
        }


        // GETTER AND SETTER

        /** Get the type from string. Returns TYPE.UNKNOWN on failure. */
        public static TYPE GetPropertyTypeFromString(string type) {
            if (type == null) { return TYPE.UNKNOWN; }
            if (type.ToLower() == "feature") { return TYPE.FEATURE; }
            if (type.ToLower() == "nfp") { return TYPE.NFP; }
            return TYPE.UNKNOWN;
        }

        public TYPE GetPropertyType() { return propertyType; }

        public string GetName() { return name; }

        /** Reference to the region this property belongs to. */
        public Region GetRegion() { return region; }

    }
}
