using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq; // see Asset Store: JSON.NET
using Newtonsoft.Json;


namespace Siro.VisualProperties {

    /**
     * Holds information about a property type.
     * More attributes can follow in the future, thats why a struct is used.
     */
    public class PropertyInformation {
        
        public string propertyType;

        // Range of the values regarding this property.
        // Usually loaded later if "nodes"/regions are loaded.
        public float minValue, maxValue;
        public bool minValueSet, maxValueSet;

        // Holds information about the methods to call.
        public List<MethodInformation> methodInfos;

        public PropertyInformation(string propertyType) {
            this.propertyType = propertyType;
            methodInfos = new List<MethodInformation>();
            minValue = maxValue = 0;
            minValueSet = maxValueSet = false;
        }

        public void SetMinValue(float value) {
            minValue = value;
            minValueSet = true;
        }

        public void SetMaxValue(float value) {
            maxValue = value;
            maxValueSet = true;
        }
    }


    /**
     * List that holds all the visual properties.
     * The problem is, that they are loaded before nodes with values are loaded.
     * Thus, we can not set a value for the visual properties yet!
     * We can only store information like "method" that a property of this type will call.
     */
    public class VisProps {

    	private string propertiesFilePath;

        /**
         * Dictionary of visual properties.
         * Key = Type of the visual property (e.g. "performance")
         * Value = List<VisProp> = A list of mappings to apply (e.g. to color, to scale...)
         */
        private Dictionary<string, PropertyInformation> list = new Dictionary<string, PropertyInformation>();
        private bool loaded = false;


        // CONSTRUCTORS

        public VisProps(string propertiesFilePath) {
            this.propertiesFilePath = propertiesFilePath;
        }
        

        // GETTER AND SETTER

        /**
         * Get the loaded visual properties.
         * (Key = property type, Value = list for this type)
         */
        public Dictionary<string, PropertyInformation> GetList() {
            return list;
        }

        /** Tells if loading was successful. */
        public bool PropertiesLoaded() {
            return loaded;
        }

        public List<string> GetProperties() {
            return new List<string>(list.Keys);
        }

        public int GetCount() {
            return list.Count;
        }

        public int GetCount(string propertyType) {
            return list[propertyType].methodInfos.Count;
        }

        /**
         * Returns the property information if the type exists.
         * Returns null if the key does not exist!
         */
        public PropertyInformation getProperty(string propertyType) {
            if (!HasProperty(propertyType)) { return null; }
            return list[propertyType];
        }

        public bool HasProperties() {
            return GetCount() > 0;
        }

        public bool HasProperty(string propertyType) {
            return list.ContainsKey(propertyType);
        }


        // FUNCTIONALITY

        /**
         * Loads the properties and returns if successful.
         */
        public bool LoadProperties() {

            loaded = false;
            string filePath = propertiesFilePath;

            if (!File.Exists(filePath)) {
                Debug.LogError("Failed to load properties! (file does not exist)");
                return false;
            }

            // load the json object from file
            using (StreamReader sr = File.OpenText(filePath)) {

                // get the main json element with key = "properties"
                JObject o = (JObject) JToken.ReadFrom(new JsonTextReader(sr));
                JArray props = (JArray) o["properties"];

                // see https://stackoverflow.com/a/7216958
                if (props == null) {
                    Debug.LogError("Missing properties in JSON file!");
                }
                else {

                    // parse all properties
                    for (int i = 0; i < props.Count; i++) {
                        JObject entry = (JObject) props[i];

                        // parse single entry
                        string propertyType = "";
                        MethodInformation mInf = ParseProperty(entry, out propertyType);
                        if (propertyType.Length == 0) { continue; }

                        // create the new list or simply add the property
                        if (!list.ContainsKey(propertyType)) {
                            PropertyInformation propInf = new PropertyInformation(propertyType);
                            propInf.methodInfos.Add(mInf);
                            list.Add(propertyType, propInf);
                        }
                        else {
                            list[propertyType].methodInfos.Add(mInf);
                        }
                    }

                }
            }

            loaded = true;
            return true;
        }

        /**
         * Parse a property mapping from JSON object to method information.
         */
        private MethodInformation ParseProperty(JObject o, out string propertyType) {

            string method = (string) o["method"];
            propertyType = (string) o["type"];

            // ToDo: more method information to parse?

            return new MethodInformation(method);
        }

    }

}