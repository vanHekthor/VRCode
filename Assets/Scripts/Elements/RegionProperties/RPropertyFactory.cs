using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;

namespace VRVis.RegionProperties {

    /**
     * Factory to create instances of Region Properties (ARProperty).
     */
    public class RPropertyFactory {

        private static readonly RPropertyFactory INSTANCE = new RPropertyFactory();


        // CONSTRUCTOR

        private RPropertyFactory() {}


        // GETTER AND SETTER

        public static RPropertyFactory GetInstance() { return INSTANCE; }


        // FUNCTIONALITY

        /**
         * Returns the instance according to the type stored in the JSON data.
         * Returns null if the type is invalid.
         */
        public ARProperty GetPropertyFromJSON(Region region, JObject o, int entryNo) {
            
            string name = (string) o["name"];
            if (name == null) {
                Debug.LogError("Missing property name (entry: " + entryNo + ")");
                return null;
            }

            string type = (string) o["type"];
            ARProperty.TYPE propType = ARProperty.GetPropertyTypeFromString(type);
            if (propType == ARProperty.TYPE.UNKNOWN) {
                Debug.LogError("Invalid property type: " + type + " (entry " + entryNo + ")");
                return null;
            }

            ARProperty propInstance = null;

            switch (propType) {
                case ARProperty.TYPE.FEATURE:
                    propInstance = new RProperty_Feature(propType, name, region, o);
                    break;

                case ARProperty.TYPE.NFP:
                    propInstance = new RProperty_NFP(propType, name, region, o);
                    break;
            }

            return propInstance;
        }

    }
}
