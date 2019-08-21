using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using VRVis.IO;
using VRVis.Mappings.Methods;
using VRVis.Mappings.Methods.Base;

namespace VRVis.Mappings {

    /// <summary>
    /// Settings of an edge including value mapping.
    /// </summary>
    public class FilenameSetting : AMappingEntry {

        private static FilenameSetting DEFAULT_INSTANCE = null;

        private Color color;
        private string query_method;
        private ISet<string> query_patterns = new HashSet<string>();

        private static readonly ISet<string> supported_methods = new HashSet<string>(){
            "startswith", "endswith", "regex"
        };


        // CONSTRUCTOR

        public FilenameSetting() {}

        /// <summary>Create this instance from another instance.</summary>
        public FilenameSetting(FilenameSetting other) {
            color = other.color;
        }


        // GETTER AND SETTER

        public Color GetColor() { return color; }
        public void SetColor(Color color) { this.color = color; }


        // FUNCTIONALITY

        /// <summary>Returns true if the filename applies to the query.</summary>
        public bool Applies(string filename) {

            if (query_method == null || query_method.Length == 0) { return false; }
            if (query_patterns == null || query_patterns.Count == 0) { return false; }
            
            string method_lower = query_method.ToLower();
            foreach (string p in query_patterns) {
                if (QueryApplies(filename, method_lower, p)) { return true; }
            }
            return false;
        }

        /// <summary>Returns true if the query (method and pattern) applies for the given name.</summary>
        private bool QueryApplies(string name, string method, string pattern) {

            switch (method) {
                case "startswith": return name.StartsWith(pattern);
                case "endswith": return name.EndsWith(pattern);
                case "regex": return Regex.IsMatch(name, pattern, RegexOptions.CultureInvariant);
            }

            return false;
        }

        /// <summary>Returns true if the query method is supported. Pass it as lower-case.</summary>
        private bool IsQueryMethodSupported(string method) { return supported_methods.Contains(method); }

        /// <summary>
        /// Loads default settings.<para/>
        /// Used to initialize the default setting for this type.<para/>
        /// Should only be used for the default setting instance!
        /// </summary>
        private void LoadDefaults() {
            color = new Color(1, 1, 1, 1);
            query_patterns = new HashSet<string>();
        }

        /// <summary>Initialize this instance from JSON.</summary>
        public override bool LoadFromJSON(JObject o, ValueMappingsLoader loader, string name) {
            
            // load base components first
            base.LoadFromJSON(o, loader, name);
            
            // overwrite query
            JObject q = o["query"] as JObject;
            if (q != null) {
                
                JArray qvalues = q["values"] as JArray;
                if (q["method"] == null) { Debug.LogError("FILENAME [" + name + "] query: missing method!"); return false; }
                if (qvalues == null) { Debug.LogError("FILENAME [" + name + "] query: missing values!"); return false; }
                
                query_method = ((string) q["method"]).ToLower();
                if (!supported_methods.Contains(query_method)) { Debug.LogError("FILENAME [" + name + "] query: method \"" + query_method + "\" not supported!"); return false; }
                
                foreach (JToken t in qvalues) {
                    if (t == null) { continue; }
                    query_patterns.Add((string) t);
                }
                if (qvalues.Count == 0) { Debug.LogError("FILENAME [" + name + "] query: no values!"); return false; }
            }

            // overwrite color method
            if (o["color"] != null) {

                string methodName = (string) o["color"];
                IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.FILENAME);
                if (method == null) {
                    Debug.LogError("FILENAME [" + name + "] color: method not found: " + methodName + " (ensure it is defined!)");
                    return false;
                }

                // check if the method is supported
                bool isFixedColorMethod = method is Color_Fixed;
                if (!isFixedColorMethod) {
                    Debug.LogError("FILENAME [" + name + "] color: method is no fixed color method!");
                    return false;
                }

                // apply fixed color only once
                color = ((Color_Fixed) method).GetColor();
            }

            return true;
        }

        /// <summary>
        /// Create an instance of this class from the JSON data.<para/>
        /// Returns null on errors.
        /// </summary>
        /// <param name="defaultSetting">Default settings to apply first, pass null if this is the default instance!</param>
        /// <param name="loader">Required to check if the methods used exist.</param>
        public static FilenameSetting FromJSON(JObject json, ValueMappingsLoader loader, FilenameSetting defaultSetting, string name) {
            
            FilenameSetting instance = defaultSetting != null ? new FilenameSetting(defaultSetting) : new FilenameSetting();
            if (instance.LoadFromJSON(json, loader, name)) { return instance; }
            return null;
        }

        /// <summary>
        /// Get the default instance of this setting.<para/>
        /// Creates it if it was not created yet.<para/>
        /// Always returns a reference to the same instance!
        /// </summary>
        public static FilenameSetting Default() {
            
            if (DEFAULT_INSTANCE != null) { return DEFAULT_INSTANCE; }

            // create default instance and returns it
            DEFAULT_INSTANCE = new FilenameSetting();
            DEFAULT_INSTANCE.LoadDefaults();
            return DEFAULT_INSTANCE;
        }

    }
}