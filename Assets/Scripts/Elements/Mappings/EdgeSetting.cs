using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.Mappings.Methods;
using VRVis.Mappings.Methods.Base;
using VRVis.Utilities;

namespace VRVis.Mappings {

    /// <summary>
    /// Settings of an edge including value mapping.
    /// </summary>
    public class EdgeSetting : AMappingEntry {

        private static EdgeSetting DEFAULT_INSTANCE = null;

        /// <summary>
        /// To what the color value should be set.<para/>
        /// - DIRECTION: (default) - to map fixed color or color_scale<para/>
        /// - VALUE: map value percentage of min/max to the provided color method<para/>
        /// - REGION: take the color of a connected region (method provides color if no region is connected)
        /// </summary>
        public enum Relative { NONE, DIRECTION, VALUE, REGION };
        private Relative relative_to = Relative.NONE;

        /// <summary>Required to be set when "relative_to" is "direction" or "value"</summary>
        private AColorMethod colorMethod;

        /// <summary>Tells how to map the width (to value if its a width_scale base method)</summary>
        private ASizeMethod widthMethod;

        /// <summary>Amount of points the line consists of (between 2 and 100).</summary>
        private uint steps;

        /// <summary>Strength of the curve (between 0 and 1).</summary>
        private float curve_strength;

        /// <summary>Curve strength noise to make lines with same strength distinguishable (between 0 and 0.5).</summary>
        private float curve_noise;

        /// <summary>Min/max values explicitly set by user.</summary>
        private MinMaxValue minMaxValue = new MinMaxValue();

        
        // CONSTRUCTOR

        public EdgeSetting() {}

        /// <summary>Create this instance from another instance.</summary>
        public EdgeSetting(EdgeSetting other) {
            
            colorMethod = other.colorMethod;
            widthMethod = other.widthMethod;
            steps = other.steps;
            curve_strength = other.curve_strength;
            curve_noise = other.curve_noise;
        }


        // GETTER AND SETTER

        /// <summary>Get to what this edges color is relative to.</summary>
        public Relative GetRelativeTo() { return relative_to; }

        public AColorMethod GetColorMethod() { return colorMethod; }

        public Color GetColor(float t) { return colorMethod.Evaluate(t); }

        public ASizeMethod GetWidthMethod() { return widthMethod; }

        /// <summary>Get the line width at this segment position (t between 0 and 1).</summary>
        public float GetWidth(float t) { return widthMethod.Evaluate(t); }

        /// <summary>Amount of points the line consists of (between 2 and 100).</summary>
        public uint GetSteps() { return steps; }

        /// <summary>Strength of the curve (between 0 and 1).</summary>
        public float GetCurveStrength() { return curve_strength; }

        /// <summary>Curve strength noise to make lines with same strength distinguishable (between 0 and 0.5).</summary>
        public float GetCurveNoise() { return curve_noise; }

        /// <summary>Get the min/max value instance of this edge type.</summary>
        public MinMaxValue GetMinMaxValue() { return minMaxValue; }


        // FUNCTIONALITY

        /// <summary>
        /// Loads default settings.<para/>
        /// Used to initialize the default setting for this type.<para/>
        /// Should only be used for the default setting instance!
        /// </summary>
        private void LoadDefaults() {

            relative_to = Relative.NONE;

            Color fromColor = Color.black; //new Color(0.4f, 0.4f, 1.0f);
            Color toColor = Color.black; //new Color(0.4f, 1.0f, 0.4f);
            colorMethod = new Color_Scale("edge_dcm", fromColor, toColor);
            widthMethod = new Width_Scale("edge_dwm", 1, 5);

            steps = 30;
            curve_strength = 0.3f;
            curve_noise = 0.02f;
        }

        /// <summary>Initialize this instance from JSON.</summary>
        public override bool LoadFromJSON(JObject o, ValueMappingsLoader loader, string name) {
            
            // load base components first
            base.LoadFromJSON(o, loader, name);

            // overwrite color method and relative to
            if (o["color"] != null) {
                JObject color = (JObject) o["color"];

                // get to what the color is relative to
                if (color["relative_to"] != null) {

                    string relStr = ((string) color["relative_to"]).ToLower();

                    bool v = false;
                    switch (relStr) {
                        case "direction": relative_to = Relative.DIRECTION; v = true; break;
                        case "value": relative_to = Relative.VALUE; v = true; break;
                        case "region": relative_to = Relative.REGION; v = true; break;
                        case "none": relative_to = Relative.NONE; v = true; break;
                    }

                    // if relative to input is invalid
                    if (!v) {
                        Debug.LogError("EDGE [" + name + "] color -> relative_to: invalid input (using NONE)!");
                    }
                }

                // get the color method
                if (color["method"] != null) {

                    string methodName = (string) color["method"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.EDGE);
                    if (method == null) {
                        Debug.LogError("EDGE [" + name + "] color -> method: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("EDGE [" + name + "] color -> method: method is no fixed color method!");
                        return false;
                    }

                    // check if no fixed color method when relative to value and warn if so
                    if (relative_to == Relative.VALUE && method is Color_Fixed) {
                        Debug.LogWarning("EDGE [" + name + "] color -> method: mapping is relative to VALUE but method fixed color (value wont have any affect!)");
                    }

                    // check if no scale color method when relative to NONE and warn if so
                    if (relative_to == Relative.NONE && method is Color_Scale) {
                        Debug.LogWarning("EDGE [" + name + "] color -> method: mapping is relative to NONE but method is color scale (only \"from\" will have an affect!)");
                    }

                    colorMethod = (AColorMethod) method;
                }
            }

            // overwrite width method
            if (o["width"] != null) {
                
                string methodName = (string) o["width"];
                IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.EDGE);
                if (method == null) {
                    Debug.LogError("EDGE [" + name + "] width: method not found: " + methodName + " (ensure it is defined!)");
                    return false;
                }

                // check if the method is supported
                bool isSizeMethod = method is ASizeMethod;
                if (!isSizeMethod) {
                    Debug.LogError("EDGE[" + name + "] width: method is no size method!");
                    return false;
                }

                widthMethod = (ASizeMethod) method;
            }

            // overwrite steps
            if (o["steps"] != null) {

                uint newSteps = 0;
                if (!uint.TryParse((string) o["steps"], out newSteps)) {
                    Debug.LogError("EDGE [" + name + "] steps: invalid integer!");
                    return false;
                }

                // check that value is in range
                bool fixedValue = true;
                if (newSteps < 2) { newSteps = 2; }
                else if (newSteps > 100) { newSteps = 100; }
                else { fixedValue = false; }

                if (fixedValue) { Debug.LogWarning("EDGE [" + name + "] steps: value out of bounds (set to" + newSteps + ")!"); }
                steps = newSteps;
            }

            // overwrite strength
            if (o["curve_strength"] != null) {

                float strength = 0;
                if (!Utility.StrToFloat((string) o["curve_strength"], out strength, true)) {
                    Debug.LogError("EDGE [" + name + "] curve_strength: invalid float value!");
                    return false;
                }

                // check that value is in range
                bool fixedValue = true;
                if (strength < 0) { strength = 0; }
                else if (strength > 1) { strength = 1; }
                else { fixedValue = false; }

                if (fixedValue) { Debug.LogWarning("EDGE [" + name + "] curve_strength: value out of bounds (set to " + strength + ")!"); }
                curve_strength = strength;
            }

            // overwrite noise
            if (o["curve_noise"] != null) {

                float noise = 0;
                if (!Utility.StrToFloat((string) o["curve_noise"], out noise, true)) {
                    Debug.LogError("EDGE [" + name + "] curve_noise: invalid float value!");
                    return false;
                }

                // check that value is in range
                bool fixedValue = true;
                if (noise < 0) { noise = 0; }
                else if (noise > 1) { noise = 1; }
                else { fixedValue = false; }

                if (fixedValue) { Debug.LogWarning("EDGE [" + name + "] curve_noise: value out of bounds (set to" + noise + ")!"); }
                curve_noise = noise;
            }

            // try to apply min value if user set it explicitly
            if (o["minValue"] != null) {

                float minVal;
                if (Utility.StrToFloat((string) o["minValue"], out minVal)) { minMaxValue.SetMinValue(minVal); }
                else { Debug.LogError("EDGE [" + name + "] minValue: Failed to parse value to float!"); }
            }

            // try to apply max value if user set it explicitly
            if  (o["maxValue"] != null) {

                float maxVal;
                if (Utility.StrToFloat((string) o["maxValue"], out maxVal)) { minMaxValue.SetMaxValue(maxVal); }
                else { Debug.LogError("EDGE [" + name + "] maxValue: Failed to parse value to float!"); }
            }

            return true;
        }

        /// <summary>
        /// Create an instance of this class from the JSON data.<para/>
        /// Returns null on errors.
        /// </summary>
        /// <param name="defaultSetting">Default settings to apply first, pass null if this is the default instance!</param>
        /// <param name="loader">Required to check if the methods used exist.</param>
        public static EdgeSetting FromJSON(JObject json, ValueMappingsLoader loader, EdgeSetting defaultSetting, string name) {
            
            EdgeSetting instance = defaultSetting != null ? new EdgeSetting(defaultSetting) : new EdgeSetting();
            if (instance.LoadFromJSON(json, loader, name)) { return instance; }
            return null;
        }

        /// <summary>
        /// Get the default instance of this setting.<para/>
        /// Creates it if it was not created yet.<para/>
        /// Always returns a reference to the same instance!
        /// </summary>
        public static EdgeSetting Default() {
            
            if (DEFAULT_INSTANCE != null) { return DEFAULT_INSTANCE; }

            // create default instance and returns it
            DEFAULT_INSTANCE = new EdgeSetting();
            DEFAULT_INSTANCE.LoadDefaults();
            return DEFAULT_INSTANCE;
        }

    }
}
