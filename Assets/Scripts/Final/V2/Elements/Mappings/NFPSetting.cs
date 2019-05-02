using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.Mappings.Methods;
using VRVis.Mappings.Methods.Base;
using VRVis.Settings;
using VRVis.Utilities;

namespace VRVis.Mappings {

    /// <summary>
    /// Settings of a non functional property region including value mapping.
    /// </summary>
    public class NFPSetting : AMappingEntry {

        private static NFPSetting DEFAULT_INSTANCE = null;

        private AColorMethod defaultColorMethod;
        private AColorMethod heightmapColorMethod;

        /// <summary>Min/max values explicitly set by user.</summary>
        private MinMaxValue minMaxValue = new MinMaxValue();

        /// <summary>Unit of values</summary>
        private string unit = "";
        


        // CONSTRUCTOR

        public NFPSetting() {}

        /// <summary>
        /// Create this instance from another instance.<para/>
        /// Will not copy the min/max values.
        /// </summary>
        public NFPSetting(NFPSetting other) {
            
            defaultColorMethod = other.defaultColorMethod;
            heightmapColorMethod = other.heightmapColorMethod;
            unit = other.unit;
        }



        // GETTER AND SETTER

        public AColorMethod GetDefaultColorMethod() { return defaultColorMethod; }
        
        public Color GetDefaultColor(float t) { return defaultColorMethod.Evaluate(t); }

        public AColorMethod GetHeightmapColorMethod() { return heightmapColorMethod; }

        public Color GetHeightmapColor(float t) { return heightmapColorMethod.Evaluate(t); }

        /// <summary>Get color method according to the passed visualization type.</summary>
        public AColorMethod GetColorMethod(ApplicationSettings.NFP_VIS nfpVisType) {

            if (nfpVisType == ApplicationSettings.NFP_VIS.HEIGHTMAP) {
                return GetHeightmapColorMethod();
            }

            return GetDefaultColorMethod();
        }

        /// <summary>Get the min/max value instance of this NFP.</summary>
        public MinMaxValue GetMinMaxValue() { return minMaxValue; }

        /// <summary>Get the value unit or an empty string it not set.</summary>
        public string GetUnit() { return unit; }

        public void SetUnit(string unit) { this.unit = unit; }

        /// <summary>Returns true if a value unit was assigned.</summary>
        public bool IsUnitSet() { return unit != null && unit.Length > 0; }



        // FUNCTIONALITY

        /// <summary>
        /// Loads default settings.<para/>
        /// Used to initialize the default setting for this type.<para/>
        /// Should only be used for the default setting instance!
        /// </summary>
        private void LoadDefaults() {

            Color defColor_from = new Color(0.2f, 1.0f, 0.0f, 0.1f); // green
            Color defColor_to = new Color(1.0f, 0.2f, 0.2f, 0.8f); // red
            defaultColorMethod = new Color_Scale("nfp_dc", defColor_from, defColor_to);

            Color defHMColor = new Color(1, 1, 1, 0.6f); // white
            heightmapColorMethod = new Color_Fixed("nfp_hmdc", defHMColor);
        }

        /// <summary>Initialize this instance from JSON.</summary>
        /// <param name="loader">Required to check if the methods used exist.</param>
        public override bool LoadFromJSON(JObject o, ValueMappingsLoader loader, string name) {
            
            // load base components first
            base.LoadFromJSON(o, loader, name);

            // overwrite default color methods
            if (o["color"] != null) {
                JObject color = (JObject) o["color"];

                if (color["default"] != null) {

                    string methodName = (string) color["default"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.NFP);
                    if (method == null) {
                        Debug.LogError("NFP [" + name + "] color -> default: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("NFP [" + name + "] color -> default: method is no color method!");
                        return false;
                    }

                    defaultColorMethod = (AColorMethod) method;
                }

                if (color["heightmap"] != null) {

                    string methodName = (string) color["heightmap"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.NFP);
                    if (method == null) {
                        Debug.LogError("NFP [" + name + "] color -> heightmap: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("NFP [" + name + "] color -> heightmap: method is no color method!");
                        return false;
                    }

                    heightmapColorMethod = (AColorMethod) method;
                }
            }

            // try to apply min value if user set it explicitly
            if (o["minValue"] != null) {

                float minVal;
                if (Utility.StrToFloat((string) o["minValue"], out minVal)) { minMaxValue.SetMinValue(minVal); }
                else { Debug.LogError("NFP [" + name + "] minValue: Failed to parse value to float!"); }
            }

            // try to apply max value if user set it explicitly
            if  (o["maxValue"] != null) {

                float maxVal;
                if (Utility.StrToFloat((string) o["maxValue"], out maxVal)) { minMaxValue.SetMaxValue(maxVal); }
                else { Debug.LogError("NFP [" + name + "] maxValue: Failed to parse value to float!"); }
            }

            // set the value unit
            if (o["unit"] != null) { SetUnit((string) o["unit"]); }

            return true;
        }

        /// <summary>
        /// Create an instance of this class from the JSON data.<para/>
        /// Returns null on errors.
        /// </summary>
        /// <param name="defaultSetting">Default settings to apply first, pass null if this is the default instance!</param>
        /// <param name="loader">Required to check if the methods used exist.</param>
        public static NFPSetting FromJSON(JObject json, ValueMappingsLoader loader, NFPSetting defaultSetting, string name) {
            
            NFPSetting instance = defaultSetting != null ? new NFPSetting(defaultSetting) : new NFPSetting();
            if (instance.LoadFromJSON(json, loader, name)) { return instance; }
            return null;
        }

        /// <summary>
        /// Get the default instance of this setting.<para/>
        /// Creates it if it was not created yet.<para/>
        /// Always returns a reference to the same instance!
        /// </summary>
        public static NFPSetting Default() {
            
            if (DEFAULT_INSTANCE != null) { return DEFAULT_INSTANCE; }

            // create default instance and returns it
            DEFAULT_INSTANCE = new NFPSetting();
            DEFAULT_INSTANCE.LoadDefaults();
            return DEFAULT_INSTANCE;
        }

    }
}
