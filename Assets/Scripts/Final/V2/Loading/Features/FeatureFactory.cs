using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace VRVis.IO.Features {

    /**
     * Feature factory that create instances
     * of the specific feature types by taking
     * the information provided in the JSON object into account.
     */
    public class FeatureFactory {

        /// <summary>
        /// Creates an instance of the feature class (FROM JSON!).<para/>
        /// The case of the "name" and "type" does not matter.
        /// It will be converted to lower case before validation.
        /// </summary>
        /// <returns>The feature instance or null if the type is unknown or errors occur.</returns>
	    public static AFeature Create(JObject featureJSON) {
            
            string name = GetStringFromJSON(featureJSON, "name");
            if (name == null) { return null; }

            string type = GetStringFromJSON(featureJSON, "type");
            if (type == null) { return null; }

            AFeature featureInstance = null;

            // check and create type instance
            switch (type.ToLower()) {
                
                case "boolean":
                    featureInstance = CreateBooleanInstance(name, featureJSON); break;

                case "range":
                    featureInstance = CreateRangeInstance(name, featureJSON); break;
            }

            if (featureInstance == null) {
                Debug.LogError("Failed to create feature instance - Type unknown: \"" + type + "\"!");
            }

            return featureInstance;
        }


        private static string GetStringFromJSON(JObject jObj, string key) {
            string val = (string) jObj[key];
            if (val == null) {
                Debug.LogError("Failed to create feature instance - Failed to parse \"" + key + "\"!");
                return null;
            }
            return val;
        }


        private static void PrintMissingError(string msg, string key) {
            Debug.LogError(msg + "Missing key \"" + key + "\"");
        }


        /** Create instance of boolean feature. */
        private static Feature_Boolean CreateBooleanInstance(string name, JObject featureJSON) {

            Feature_Boolean instance = new Feature_Boolean(null, name);

            // get optional default value
            if (featureJSON["default"] != null) {
                string input = ((string) featureJSON["default"]).ToLower();
                if (input == "1" || input == "true") { instance.SetValue(1, true); }
                else if (input == "0" || input == "false") { instance.SetValue(0, true); }
            }

            return instance;
        }


        /** Create instance of a range feature. */
        private static Feature_Range CreateRangeInstance(string name, JObject featureJSON) {
            
            // check if required keys exist
            string err_msg = "Failed to parse range feature entry - ";
            if (featureJSON["from"] == null) { PrintMissingError(err_msg, "from"); return null; }
            if (featureJSON["to"] == null) { PrintMissingError(err_msg, "to"); return null; }
            if (featureJSON["step"] == null) { PrintMissingError(err_msg, "step"); return null; }

            // get required key values
            float from = (float) featureJSON["from"];
            float to = (float) featureJSON["to"];
            float step = (float) featureJSON["step"];

            Feature_Range instance = new Feature_Range(null, name, from, to, step);

            // get optional default value
            if (featureJSON["default"] != null) {
                float defaultValue = (float) featureJSON["default"];
                instance.SetValue(defaultValue, true);
            }

            return instance;
        }

    }
}
