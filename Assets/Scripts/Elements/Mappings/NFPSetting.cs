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
        private AColorMethod defaultColorDeltaMethod;
        private AColorMethod heightMapColorMethod;
        private AColorMethod heightMapColorDeltaMethod;
        private AColorMethod codeCityColorMethod;
        private AColorMethod codeCityColorDeltaMethod;

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
            heightMapColorMethod = other.heightMapColorMethod;
            unit = other.unit;
        }



        // GETTER AND SETTER

        public AColorMethod GetDefaultColorMethod() { return defaultColorMethod; }

        public AColorMethod GetDefaultColorDeltaMethod() { return defaultColorDeltaMethod; }

        public Color GetDefaultColor(float t) { return defaultColorMethod.Evaluate(t); }

        public AColorMethod GetHeightMapColorMethod() { return heightMapColorMethod; }

        public AColorMethod GetHeightMapColorDeltaMethod() { return heightMapColorDeltaMethod; }

        public Color GetHeightmapColor(float t) { return heightMapColorMethod.Evaluate(t); }

        public AColorMethod GetCodeCityColorMethod() { return codeCityColorMethod; }

        public AColorMethod GetCodeCityColorDeltaMethod() { return codeCityColorDeltaMethod; }

        /// <summary>Get color method according to the passed visualization type.</summary>
        public AColorMethod GetColorMethod(ApplicationSettings.NFP_VIS nfpVisType) {

            if (nfpVisType == ApplicationSettings.NFP_VIS.HEIGHTMAP) {
                return GetHeightMapColorMethod();
            }

            return GetDefaultColorMethod();
        }

        /// <summary>
        /// Returns a color method according to the passed visualization type that is adjusted to the min and max value.
        /// </summary>
        /// <param name="nfpVisType"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public AColorMethod GetMinMaxColorMethod(ApplicationSettings.NFP_VIS nfpVisType, float min, float max) {

            if (nfpVisType == ApplicationSettings.NFP_VIS.CODE_MARKING || nfpVisType == ApplicationSettings.NFP_VIS.NONE) {
                if (defaultColorDeltaMethod.HasNeutralColor) {
                    return AdjustColorMethod(defaultColorDeltaMethod, min, max);
                }
                else {
                    return GetDefaultColorDeltaMethod();
                }
            }
            else if (nfpVisType == ApplicationSettings.NFP_VIS.HEIGHTMAP) {
                if (heightMapColorDeltaMethod.HasNeutralColor) {
                    return AdjustColorMethod(heightMapColorDeltaMethod, min, max);
                }
                else {
                    return GetHeightMapColorDeltaMethod();
                }
            }
            else if (nfpVisType == ApplicationSettings.NFP_VIS.CODE_CITY) {
                if (codeCityColorDeltaMethod.HasNeutralColor) {
                    return AdjustColorMethod(codeCityColorDeltaMethod, min, max);
                }
                else {
                    return GetCodeCityColorDeltaMethod();
                }
            }
            else {
                return GetDefaultColorDeltaMethod();
            }
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
            heightMapColorMethod = new Color_Fixed("nfp_hmdc", defHMColor);
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

                if (color["default_delta"] != null) {

                    string methodName = (string)color["default_delta"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.NFP);
                    if (method == null) {
                        Debug.LogError("NFP [" + name + "] color -> default delta: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("NFP [" + name + "] color -> default delta: method is no color method!");
                        return false;
                    }

                    defaultColorDeltaMethod = (AColorMethod)method;
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

                    heightMapColorMethod = (AColorMethod) method;
                }

                if (color["heightmap_delta"] != null) {

                    string methodName = (string)color["heightmap_delta"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.NFP);
                    if (method == null) {
                        Debug.LogError("NFP [" + name + "] color -> heightmap delta: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("NFP [" + name + "] color -> heightmap delta: method is no color method!");
                        return false;
                    }

                    heightMapColorDeltaMethod = (AColorMethod)method;
                }

                if (color["code_city"] != null) {

                    string methodName = (string)color["code_city"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.NFP);
                    if (method == null) {
                        Debug.LogError("NFP [" + name + "] color -> code_city: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("NFP [" + name + "] color -> code_city: method is no color method!");
                        return false;
                    }

                    codeCityColorMethod = (AColorMethod)method;
                }

                if (color["code_city_delta"] != null) {

                    string methodName = (string)color["code_city_delta"];
                    IMappingMethod method = loader.GetMappingMethod(methodName, ValueMappingsLoader.SettingType.NFP);
                    if (method == null) {
                        Debug.LogError("NFP [" + name + "] color -> code city delta: method not found: " + methodName + " (ensure it is defined!)");
                        return false;
                    }

                    // check if the method is supported
                    bool isColorMethod = method is AColorMethod;
                    if (!isColorMethod) {
                        Debug.LogError("NFP [" + name + "] color -> code city delta: method is no color method!");
                        return false;
                    }

                    codeCityColorDeltaMethod = (AColorMethod)method;
                }
            }

            // try to apply min value if user set it explicitly
            if (!ApplicationLoader.GetApplicationSettings().ComparisonMode) {
                if (o["minValue"] != null) {

                    float minVal;
                    if (Utility.StrToFloat((string)o["minValue"], out minVal)) { minMaxValue.SetMinValue(minVal); }
                    else { Debug.LogError("NFP [" + name + "] minValue: Failed to parse value to float!"); }
                }
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

        /// <summary>
        /// Adjusts an existing color method to new min and max values while considering the
        /// the neutral value with its neutral color<para/>
        /// Different adjustments depending on the position of the neutral value 
        /// in comparison to the min-max interval (below | inside | above).<para/> 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private AColorMethod AdjustColorMethod(AColorMethod method, float min, float max) {

            float neutralValue = method.GetNeutralValue();
            Color_Scale adjustedScale = null;

            // neutral value is inbetween the min and max value
            // therefore there should be a gradient from FromColor to NeutralColor to ToColor 
            if (min <= neutralValue && neutralValue <= max) {
                // determine the relative position of the neutral value inside the min-max interval
                float ratio = (neutralValue - min) / (max - min);

                adjustedScale = new Color_Scale(
                    method.GetMethodName(),
                    method.GetFromColor(),
                    method.GetToColor(),
                    method.GetNeutralColor(),
                    neutralValue,
                    ratio);

            }
            // if the neutral value is below the min-max interval
            // a sub-gradient of the complete gradient from NeutralColor (neutral value) to ToColor (max value) is needed.  
            else if (min >= neutralValue && neutralValue <= max) {
                // relative min position inside the neutral-max interval
                float relativeMinPosition = (min - neutralValue) / (max - neutralValue);

                // scale representing the complete neutral-max interval with a color gradient from
                // NeutralColor to ToColor, for example Transparent to Red
                var helperScale = new Color_Scale(
                    method.GetMethodName(),
                    method.GetNeutralColor(),
                    method.GetToColor());

                Color newMinColor = helperScale.Evaluate(relativeMinPosition);

                // sub-gradient of the complete NeutralColor-ToColor gradient
                // meaning only the part of the gradient corresponding to the min and max interval
                // for examle:
                // complete gradient = Transparent to Red <=> neutral (to min) to max value 
                // sub-gradient = Transparent-Red to Red <=> (neutral to min part is cut off) min to max value
                adjustedScale = new Color_Scale(
                    method.GetMethodName(),
                    newMinColor,
                    method.GetToColor());
            }
            // if the neutral value is above the min-max interval
            // a sub-gradient of the complete gradient from FromColor (min value) to NeutralColor (neutral value) is needed.
            else if (min <= neutralValue && neutralValue >= max) {
                // relative max position inside the min-neutral interval
                float relativeMaxPosition = (max - min) / (neutralValue - min);

                // scale representing the complete min-neutral interval with a color gradient from
                // FromColor to ToColor, for example Green to Transparent
                var helperScale = new Color_Scale(
                    method.GetMethodName(),
                    method.GetFromColor(),
                    method.GetNeutralColor());

                Color newMaxColor = helperScale.Evaluate(relativeMaxPosition);

                // sub-gradient of the complete FromColor-NeutralColor gradient
                // meaning only the part of the gradient corresponding to the min and max interval
                // for examle:
                // complete gradient = Green to Transparent <=> min (to max) to neutral value
                // sub-gradient = Green to Transparent-Green <=> min to max value (part to neutral value is cut off) 
                adjustedScale = new Color_Scale(
                    method.GetMethodName(),
                    method.GetFromColor(),
                    newMaxColor);
            }

            return adjustedScale;
        }

    }
}
