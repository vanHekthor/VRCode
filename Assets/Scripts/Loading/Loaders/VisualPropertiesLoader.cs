using Newtonsoft.Json; // see Asset Store: JSON.NET
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.RegionProperties;
using VRVis.VisualProperties;
using VRVis.VisualProperties.Methods;


namespace VRVis.IO {

    /**
     * Loads visual properties from the given file.
     * Visual properties tell how to map property values on visual attributes like color.
     */
    public class VisualPropertiesLoader : FileLoader {

        private static VisualPropertiesLoader INSTANCE;
        private readonly RegionLoader regionLoader;

        /// <summary>
        /// Dictionary of visual properties.<para/>
        /// A VisualProperty instance holds method information (NOT the definition).<para/>
        /// KEY = Type of the visual property (e.g. "NFP" or "FEATURE" or "EDGE")<para/>
        /// VALUE = propertyName and according VisualProperty (e.g. VisualProperty for "performance")
        /// </summary>
        private Dictionary<ARProperty.TYPE, Dictionary<string, VisualProperty>> visProperties = new Dictionary<ARProperty.TYPE, Dictionary<string, VisualProperty>>();

        /// <summary>
        /// Holds the visual property method definitions.<para/>
        /// KEY = name of the method for fast lookup (e.g. "Color_Scale_1").
        /// This name is stored in the VisualPropertyMethodInfo instances.<para/>
        /// VALUE = VisualPropertyMethod instance
        /// 
        /// ToDo: Method to load default methods to this dictionary.
        /// </summary>
        private Dictionary<string, VisualPropertyMethod> visPropMethods = new Dictionary<string, VisualPropertyMethod>();

        private int propertiesCount = 0;


	    // CONSTRUCTOR

        public VisualPropertiesLoader(string filePath, RegionLoader regionLoader)
        : base(filePath) {
            INSTANCE = this;
            this.regionLoader = regionLoader;
        }


        // GETTER AND SETTER

        /** Get the current loader instance. */
        public VisualPropertiesLoader GetInstance() {
            return INSTANCE;
        }

        /** Get all types of properties available. */
        public List<ARProperty.TYPE> GetPropertyTypes() {
            return new List<ARProperty.TYPE>(visProperties.Keys);
        }

        /** Returns the property names for that type or null if the type does not exist. */
        public List<string> GetPropertyNames(ARProperty.TYPE propertyType) {
            if (!HasPropertyType(propertyType)) { return null; }
            return new List<string>(visProperties[propertyType].Keys);
        }

        /** Check if the property type is known. */
        public bool HasPropertyType(ARProperty.TYPE propertyType) {
            return visProperties.ContainsKey(propertyType);
        }

        /** Check if example this property is known. */
        public bool HasProperty(ARProperty.TYPE propertyType, string propertyName) {
            return HasPropertyType(propertyType) && visProperties[propertyType].ContainsKey(propertyName);
        }

        /** Get the according visual property or null if unknown. */
        public VisualProperty GetProperty(ARProperty.TYPE propertyType, string propertyName) {
            if (!HasProperty(propertyType, propertyName)) { return null; }
            return visProperties[propertyType][propertyName];
        }

        public VisualProperty GetProperty(ARProperty property) {
            return GetProperty(property.GetPropertyType(), property.GetName());
        }


        /** Returns the amount of properties loaded. */
        public int GetPropertiesCount() { return propertiesCount; }

        public List<VisualPropertyMethod> GetMethods() {
            return new List<VisualPropertyMethod>(visPropMethods.Values);
        }

        /** Get the visual property method or null if unknown. */
        public VisualPropertyMethod GetMethod(VisualPropertyEntryInfo methodInfo) {
            if (!HasMethod(methodInfo)) { return null; }
            return visPropMethods[methodInfo.GetMethodName()];
        }

        public bool HasMethod(VisualPropertyEntryInfo methodInfo) {
            return visPropMethods.ContainsKey(methodInfo.GetMethodName());
        }


        // FUNCTIONALITY

        public override bool Load() {

            loadingSuccessful = false;
            propertiesCount = 0;

            if (!FileExists(GetFilePath())) {
                Debug.LogError("Failed to load visual properties (file does not exist)!");
                return false;
            }

            // load the json object from file
            Debug.Log("Loading visual properties from file...");
            using (StreamReader sr = File.OpenText(GetFilePath())) {

                // get the main json element
                JObject o = (JObject) JToken.ReadFrom(new JsonTextReader(sr));

                // see https://stackoverflow.com/a/7216958
                JArray methods = (JArray) o["methods"];
                if (methods == null) { Debug.LogWarning("No methods defined in the JSON file!"); }
                else { ParseMethods(methods); }

                JArray props = (JArray) o["properties"];
                if (props == null) { Debug.LogWarning("No visual properties defined in JSON file!"); }
                else { ParseProperties(props); }
            }

            Debug.Log("Loading visual properties finished.\n" +
                "(Properties loaded: " + GetPropertiesCount() +
                ", Property Types: " + visProperties.Count +
                ", Methods: " + visPropMethods.Count + ")"
            );
            loadingSuccessful = true;
            return true;
        }


        /**
         * Parse the user defined methods.
         * Such methods are defined in the JSON file under the key "methods".
         */
        private void ParseMethods(JArray methods) {

            // parse all method entries
            for (int i = 0; i < methods.Count; i++) {
                JObject entry = (JObject) methods[i];
                
                // parse single entry to method instance
                VisualPropertyMethod method;

                try { method = ParseMethod(entry); }
                catch (Exception ex) {
                    Debug.LogError("Failed to parse visual property entry " + i);
                    Debug.LogError(ex.StackTrace);
                    continue;
                }
                
                if (method.GetMethodName().Length == 0) {
                    Debug.LogWarning("Missing method name of methods entry " + i + " (skipping)");
                    continue;
                }

                // Add the VisualPropertyMethod instance to the dictionary.
                // This will overwrite an already existing instance and thus, result in unique method names.
                visPropMethods[method.GetMethodName()] = method;
            }
        }

        /** Parse a method definition. */
        private VisualPropertyMethod ParseMethod(JObject o) {
            return VisualPropertyMethodFactory.Create(o);
        }


        /**
         * Parse the user defined property mappings.
         * They are stored in the JSON file under the key "properties".
         */
        private void ParseProperties(JArray props) {

            // parse all properties
            for (int i = 0; i < props.Count; i++) {
                JObject entry = (JObject) props[i];

                // parse single entry
                VisualPropertyEntryInfo mInf;

                try { mInf = ParseProperty(entry); }
                catch (Exception ex) {
                    Debug.LogError("Failed to parse visual property entry " + i);
                    Debug.LogError(ex.StackTrace);
                    continue;
                }

                // check if the given method exists and skip if it doesn't
                if (!visPropMethods.ContainsKey(mInf.GetMethodName())) {
                    Debug.LogError("Visual property method \"" + mInf.GetMethodName() + "\" does not exist (entry: " + i + ")!");
                    continue;
                }

                if (mInf.GetPropertyName().Length == 0) {
                    Debug.LogError("The property name cannot be empty!");
                    continue;
                }

                // check if the property exists using the region loader
                ARProperty.TYPE propertyType = mInf.GetPropertyType();
                string propertyName = mInf.GetPropertyName();
                if (!regionLoader.PropertyExists(propertyType, propertyName)) {
                    Debug.LogError("There is no such property defined in any region! (" +
                        "type: " + propertyType + ", name: " + propertyName + ")");
                    continue;
                }

                // create dictionary for this property type if it doesn't exist yet
                if (!HasPropertyType(propertyType)) {
                    visProperties.Add(propertyType, new Dictionary<string, VisualProperty>());
                }

                // create visual property instances or add methods to it
                if (propertyType == ARProperty.TYPE.NFP) {

                    // create numerical visual property for NFPs
                    // (assumed for now that all have numerical values)
                    if (!HasProperty(propertyType, propertyName)) {
                        VisualProperty visProp = new NumericVisualProperty(mInf.GetPropertyName(), true);
                        visProp.AddMethod(mInf);
                        visProperties[propertyType].Add(propertyName, visProp);
                    }
                    else {
                        visProperties[propertyType][propertyName].AddMethod(mInf);
                    }
                    propertiesCount++;
                }
                else if (propertyType == ARProperty.TYPE.FEATURE) {

                    // create feature visual property for features
                    // (assumed for now that they don't have any value - just to set default colors)
                    if (!HasProperty(propertyType, propertyName)) {
                        VisualProperty visProp = new FeatureVisualProperty(mInf.GetPropertyName(), true);
                        visProp.AddMethod(mInf);
                        visProperties[propertyType].Add(propertyName, visProp);

                        // [No longer required due to replacement by variabiliy model]
                        // check if this feature has a definition
                        //if (!ApplicationLoader.GetInstance().GetFeatureLoader().FeatureExists(propertyName)) {
                        //    Debug.LogWarning("Feature (" + propertyName + ") of visual property (" + i + ") is not defined!");
                        //}
                    }
                    else {
                        visProperties[propertyType][propertyName].AddMethod(mInf);
                    }
                    propertiesCount++;
                }
            }
        }


        /**
         * Parse a property mapping from JSON object to method information.
         */
        private VisualPropertyEntryInfo ParseProperty(JObject o) {

            // get the property type and check if it is known
            string propTypeStr = (string) o["type"];
            ARProperty.TYPE propType = ARProperty.GetPropertyTypeFromString(propTypeStr);

            if (propType == ARProperty.TYPE.UNKNOWN) {
                throw new Exception("Unknown property type: " + propTypeStr);
            }

            // we can not check the name here because regions are loaded afterwards
            string propName = (string) o["name"];

            // get basic method info
            string methodName = (string) o["method"];
            bool methodActive = true;
            if (o["active"] != null) { methodActive = (bool) o["active"]; }
   
            // remove information we no longer need
            o.Remove("type");
            o.Remove("method");
            o.Remove("active");

            // create method info instance and add the rest of the JSON data as additional info
            VisualPropertyEntryInfo mi = new VisualPropertyEntryInfo(propType, propName, methodName, methodActive);
            mi.SetAdditionalInformation(o);
            return mi;
        }

    }
}
