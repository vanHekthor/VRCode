using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.Mappings.Methods;
using VRVis.Mappings.Methods.Base;

namespace VRVis.Mappings {

    /// <summary>
    /// Settings of a feature.
    /// </summary>
    public class FeatureSetting : AMappingEntry {

        private static Color DEFAULT_COLOR = new Color(0.6933f, 0.9471f, 1f);

        private static FeatureSetting DEFAULT_INSTANCE = null;

        private Color color;

        
        // CONSTRUCTOR

        public FeatureSetting() {}

        /// <summary>Create this instance from another instance.</summary>
        public FeatureSetting(FeatureSetting other) {
            color = other.color;
        }


        // GETTER AND SETTER

        public Color GetColor() { return color; }

        public void SetColor(Color color) { this.color = color; }


        // FUNCTIONALITY

        /// <summary>
        /// Loads default settings.<para/>
        /// Used to initialize the default setting for this type.<para/>
        /// Should only be used for the default setting instance!
        /// </summary>
        private void LoadDefaults() {
            color = DEFAULT_COLOR;
        }

        /// <summary>Initialize this instance from JSON.</summary>
        public override bool LoadFromJSON(JObject o, ValueMappingsLoader loader, string name) {
            
            // load base components first
            base.LoadFromJSON(o, loader, name);

            // overwrite color method
            if (o["color"] != null) {

                string methodName = (string) o["color"];
                IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.FEATURE);
                if (method == null) {
                    Debug.LogError("FEATURE [" + name + "] color: method not found: " + methodName + " (ensure it is defined!)");
                    return false;
                }

                // check if the method is supported
                bool isFixedColorMethod = method is Color_Fixed;
                if (!isFixedColorMethod) {
                    Debug.LogError("FEATURE [" + name + "] color: method is no fixed color method!");
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
        public static FeatureSetting FromJSON(JObject json, ValueMappingsLoader loader, FeatureSetting defaultSetting, string name) {
            
            FeatureSetting instance = defaultSetting != null ? new FeatureSetting(defaultSetting) : new FeatureSetting();
            if (instance.LoadFromJSON(json, loader, name)) { return instance; }
            return null;
        }

        /// <summary>
        /// Get the default instance of this setting.<para/>
        /// Creates it if it was not created yet.<para/>
        /// Always returns a reference to the same instance!
        /// </summary>
        public static FeatureSetting Default() {
            
            if (DEFAULT_INSTANCE != null) { return DEFAULT_INSTANCE; }

            // create default instance and returns it
            DEFAULT_INSTANCE = new FeatureSetting();
            DEFAULT_INSTANCE.LoadDefaults();
            return DEFAULT_INSTANCE;
        }

    }
}

