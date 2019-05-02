using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.IO.Features;
using VRVis.RegionProperties;

namespace VRVis.IO {

    /**
     * Loads the definition of features from a file.
     * This information tells us the name of a feature and its type
     * and if the type requires it, additional settings of this type.
     * For example, a feature can be "boolean" or "range"
     * and a "range feature" could have a "step size".
     * 
     * 
     * #### VERSION OUT OF DATE ####
     * - NEW: VariabilityModelloader
     * - ToDo: REMOVE or DISABLE if no longer required!
     */
    public class FeatureLoader : FileLoader {

        // dictionary of feature name (key) and the feature instance (value)
        private Dictionary<string, AFeature> features = new Dictionary<string, AFeature>();

        // holds the keys in the same order as read from the array (required to get correct value from region array)
        private List<string> arrayOrder = new List<string>();


        // CONSTRUCTORS

        public FeatureLoader(string filePath)
        : base(filePath) {}


        // GETTER AND SETTER

        /**
         * Returns the feature for the given name or null if not found.
         * The name parameter will be converted to lower case.
         */
        public AFeature GetFeature(string name) {
            name = name.ToLower();
            if (!features.ContainsKey(name)) { return null; }
            return features[name];
        }

        /** Returns the storage order of the read array. */
        public List<string> GetArrayOrder() { return arrayOrder; }

        /**
         * Adds a feature to the list of features.
         * Returns false if this feature already exists.
         */
        public bool AddFeature(AFeature feature) {
            string name = feature.GetName().ToLower();

            // store array order (even if we don't add it again)
            arrayOrder.Add(name);

            // check if feature already added
            if (features.ContainsKey(name)) { return false; }
            features.Add(name, feature);
            return true;
        }

        public int GetFeatureCount() { return features.Count; }

        /**
         * Tells if the feature exists by returning true.
         * The case of the name does not matter (converted to lower case).
         */
        public bool FeatureExists(string name) {
            name = name.ToLower();
            if (!features.ContainsKey(name)) { return false; }
            return true;
        }


        // FUNCTIONALITY
        
        public override bool Load() {
            loadingSuccessful = false;

            if (!FileExists(GetFilePath())) {
                Debug.LogError("Failed to load features (file does not exist)!");
                return false;
            }

            // load the json object from file
            Debug.Log("Loading features from file...");
            using (StreamReader sr = File.OpenText(GetFilePath())) {

                // get the main json element
                JObject o = (JObject) JToken.ReadFrom(new JsonTextReader(sr));

                // see https://stackoverflow.com/a/7216958
                JArray features = (JArray) o["features"];
                if (features == null) { Debug.LogWarning("No features defined in the JSON file!"); }
                else { ParseFeatures(features); }
            }

            Debug.Log("Loading feature definitions finished.\n" +
                "(Features loaded: " + GetFeatureCount() + ")"
            );
            loadingSuccessful = true;
            return true;
        }

        /** Parse each entry of the feature array. */
        private void ParseFeatures(JArray features) {
            
            for (int i = 0; i < features.Count; i++) {
                
                // try to parse the feature
                try { ParseFeature(features[i]); }
                catch (Exception ex) {
                    Debug.LogError("Failed to parse feature entry " + i + "!");
                    Debug.LogError(ex.StackTrace);
                    continue;
                }
            }

        }

        /**
         * Parse a single feature entry and store it.
         * 
         * JToken source code:
         * https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Linq/JToken.cs
         */
        private void ParseFeature(JToken featureToken) {
            JObject featureJSON = (JObject) featureToken;
            AFeature feature = FeatureFactory.Create(featureJSON);
            if (feature == null) { return; }
            AddFeature(feature);
        }


        /** Returns a list of the first x features. */
        public List<AFeature> GetFirstFeatures(int amount) {
            List<AFeature> outList = new List<AFeature>();

            int cnt = 0;
            foreach (AFeature feature in features.Values) {
                outList.Add(feature);
                if (++cnt > amount) { break; }
            }

            return outList;
        }


        /**
         * Calculate the "NFP property" value using the performance influence model.
         * A region property stores the affect of each feature in an array.
         * This array will be used together with the current system configuration.
         * The size of the array is always feature size + 1.
         * 
         * Returns false if the array size is wrong.
         */
        public bool CalculatePIMValue(RProperty_NFP property) {
            
            // set value of property to be invalid and -1
            property.ResetValue();
            float[] regionValueArray = property.GetValues();

            // check for valid array size
            if (regionValueArray.Length != GetFeatureCount() + 1) {
                Debug.LogError("Failed to calculate NFP PIM value (property: " + property.GetName() +
                ", region: " + property.GetRegion().GetID() + ") - wrong array size: " +
                regionValueArray.Length + " - should be: " + (GetFeatureCount() + 1));
                return false;
            }
            
            // calculate the new value
            float pimValue = regionValueArray[0];
            int index = 1; // 0 = base, so start at 1
            foreach (string featureName in GetArrayOrder()) {
                AFeature feature = features[featureName];
                pimValue += regionValueArray[index++] * feature.GetValue();
            }

            // set value and mark it as valid
            property.SetValue(pimValue);
            return true;
        }

    }
}
