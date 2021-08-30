using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Mappings;
using VRVis.Mappings.Methods;
using VRVis.RegionProperties;
using VRVis.Settings;
using VRVis.Utilities;

namespace VRVis.Spawner.Regions {

    /// <summary>
    /// This class provides methods to apply values to spawned regions.<para/>
    /// It modifies the color and scale properties of the region objects.<para/>
    /// An instance of this class is spawned with a reference to the according CodeFile.<para/>
    /// It can retrieve the required "spawned regions" and other information from it.<para/>
    /// Created: early 2019 (Leon H.)<para/>
    /// Updated: 04.09.2019
    /// </summary>
    public class RegionModifier {

	    private readonly CodeFile file;
        private readonly RegionSpawner regionSpawner;
        //private readonly VisualPropertiesLoader visPropLoader;

        // heightmap scale from and to
        private readonly float hm_scaleFrom = 0; // means: minimum scale for value = 0
        private readonly float hm_scaleTo = 1;


        // CONSTRUCTOR

        public RegionModifier(CodeFile codeFile, RegionSpawner regionSpawner) {
            
            file = codeFile;
            this.regionSpawner = regionSpawner;

            //visPropLoader = ApplicationLoader.GetInstance().GetVisualPropertiesLoader();
            //if (visPropLoader == null) {
            //    Debug.LogError("Missing visual properties loader instance!");
            //}
        }


        // FUNCTIONALITY

        /// <summary>
        /// Apply region values for properties of the type NFP and Feature.<para/>
        /// This method will call sub-methods.<para/>
        /// The parameter "types" tells for which types to apply the values.<para/>
        /// This can be useful if only a specific type should be updated.
        /// </summary>
        public void ApplyRegionValues(IEnumerable<ARProperty.TYPE> types) {

            foreach (ARProperty.TYPE type in types) {

                if (type == ARProperty.TYPE.NFP) {

                    // update heightmap labels according to which min/max values are currently used as reference
                    UpdateHeightmapLabels();

                    // check if any region changed its color
                    bool regionColorChanged = false;

                    regionSpawner.GetSpawnedRegions(file, ARProperty.TYPE.NFP).ForEach(region =>
                        {
                            Color color_prev = region.GetCurrentNFPColor();
                            ApplyRegionValues_NFP(region);
                            if (!regionColorChanged && region.GetCurrentNFPColor() != color_prev) { regionColorChanged = true; }
                        }
                    );

                    // notify listeners about the change in color
                    if (regionColorChanged) { regionSpawner.FireRegionValuesChangedEvent(file); }
                }
                else if (type == ARProperty.TYPE.FEATURE) {
                    regionSpawner.GetSpawnedRegions(file, ARProperty.TYPE.FEATURE).ForEach(region => ApplyRegionValues_Features(region));
                }
                else {
                    // ... more ...
                }
            }
        }

        /// <summary>
        /// Runs the method "ApplyRegionValues"
        /// on all possible property types.
        /// </summary>
        public void ApplyRegionValues() {
            Array arr = Enum.GetValues(typeof(ARProperty.TYPE));
            ARProperty.TYPE[] propArr = new ARProperty.TYPE[arr.Length];
            arr.CopyTo(propArr, 0);
            ApplyRegionValues(propArr);
        }

        /// <summary>
        /// Apply region values for properties of type NFP.<para/>
        /// Depending on the type of visualization,
        /// this will color the regions marking the code sections
        /// or change the dimensions and color of tiles in the height map.
        /// </summary>
        private void ApplyRegionValues_NFP(Region region) {

            // get required configuration information
            string activeProperty = ApplicationLoader.GetApplicationSettings().GetSelectedNFP();
            // [O] int configIndex = ApplicationLoader.GetApplicationSettings().GetConfigurationIndex();

            // get the property of this region and get the value
            ARProperty.TYPE thisPropType = ARProperty.TYPE.NFP;
            ARProperty property = region.GetProperty(thisPropType, activeProperty);
            if (property == null || property.GetType() != typeof(RProperty_NFP)) { return; }
            // [O] RProperty_NFP nfpProperty = (RProperty_NFP) property;

            // check if index is valid to avoid out of bounds exceptions
            // [O] if (!nfpProperty.IsIndexValid(configIndex)) { return; }
            // [O] float value = nfpProperty.GetValue(configIndex);

            // apply visual properties
            // [O] ApplyVisualProperties(region, thisPropType, value);
            ApplyMappings(region, thisPropType);
        }

        /// <summary>
        /// Apply region values for properties of type features.<para/>
        /// NOTE: There are currently no values for features but may come in the future?
        /// </summary>
        private void ApplyRegionValues_Features(Region region) {

            // get the property of this region and get the value
            ARProperty.TYPE thisPropType = ARProperty.TYPE.FEATURE;
            if (!region.HasPropertyType(thisPropType)) { return; }

            // apply visual properties
            // [O] ApplyVisualProperties(region, thisPropType, null);
            ApplyMappings(region, thisPropType);
        }


        /// <summary>
        /// Apply the mappings for this region and its type.<para/>
        /// Uses the new ValueMappingsLoader system instead of the visual properties.
        /// </summary>
        private void ApplyMappings(Region region, ARProperty.TYPE propType) {

            // These objects should only be the ones created through the user selection.
            // To ensure that we color them correctly anyway, we use the "RegionGameObject" instance
            // that is attached to each region GameObject and tells us about its property type and name.
            // This avoid looking up the application settings everytime because we did this step while spawning.
            region.ClearUIGameObjects(propType);

            switch (propType) {
                case ARProperty.TYPE.NFP: ApplyRegionMapping_NFP(region); break;
                case ARProperty.TYPE.FEATURE: ApplyRegionMapping_Feature(region); break;
            }
        }


        /// <summary>
        /// Apply the mapping on game objects of a non functional property region.
        /// </summary>
        private void ApplyRegionMapping_NFP(Region region) {

            ARProperty.TYPE propType = ARProperty.TYPE.NFP;
            List<GameObject> gameObjects = region.GetUIGameObjects(propType);

            // get the name of the currently selected non functional property
            string nfpName = ApplicationLoader.GetApplicationSettings().GetSelectedNFP().ToLower();

            // get the setting for this non functional property (we only show one at a time)
            ValueMappingsLoader mappingsLoader = ApplicationLoader.GetInstance().GetMappingsLoader();
            NFPSetting setting = mappingsLoader.GetNFPSetting(nfpName);

            // use default setting instead if this setting is disabled
            if (!setting.IsActive()) { setting = mappingsLoader.GetNFPSetting(mappingsLoader.KEY_DEFAULT); }

            // Apply settings/mapping for each object.
            // Calculate the color only once bc. all objects belong to the same property.
            foreach (GameObject obj in gameObjects) {

                string err_msg = "";
                if (!IsValidRegionGameObject(obj, propType, out err_msg)) {
                    Debug.LogError("[Region: " + region.GetID() + "] " + err_msg);
                }

                // get info and property (we validated them in the previous step)
                RegionGameObject info = obj.GetComponent<RegionGameObject>();
                RProperty_NFP nfpProperty = (RProperty_NFP) info.GetProperty();

                // this should not happen (we only show one nfp at a time)
                if (!nfpProperty.GetName().Equals(nfpName)) {
                    // don't take this too serious - if using "Destroy()" it might just be destroyed later
                    Debug.LogWarning("Failed to apply mapping - NFP name (" + nfpProperty.GetName() +
                        ") different to selected one (" + nfpName + ")! - region: " + region.GetID(), obj);
                    continue;
                }

                // get color method and method min/max
                MinMaxValue minMax = GetMinMaxValues(info.GetProperty().GetName(), info.GetCodeFile(), setting.GetMinMaxValue());
                AColorMethod colMethod;
                if (ApplicationLoader.GetInstance().GetAppSettings().ComparisonMode) {
                    colMethod = setting.GetMinMaxColorMethod(info.GetNFPVisType(), minMax.GetMinValue(), minMax.GetMaxValue());
                }
                else {
                    colMethod = setting.GetColorMethod(info.GetNFPVisType());
                }           
                

                if (minMax == null) {
                    Debug.LogError("Failed to apply NFP mapping (" + nfpProperty.GetName() +  ") - Missing min/max!");
                    continue;
                }

                // get absolute value and crop to bounds
                float absValue = minMax.CropToBounds(nfpProperty.GetValue());

                // apply relative color
                float valuePercentage = minMax.GetRangePercentage(absValue);
                Color regionColor = colMethod.Evaluate(valuePercentage);
                region.SetCurrentNFPColor(regionColor);

                // apply scaling on heightmap region as well if this is one
                if (info.GetNFPVisType() == ApplicationSettings.NFP_VIS.CODE_MARKING) {
                
                    bool success = Utility.ChangeImageColorTo(obj, regionColor);
                    if (!success) {
                        Debug.LogError("Failed to change color of NFP region object " +
                            "(region: " + region.GetID() + ", property: " + nfpProperty.GetName() + ")!");
                    }
                }
                else if (info.GetNFPVisType() == ApplicationSettings.NFP_VIS.HEIGHTMAP) {

                    // turn absolute value in string with unit to show in region area
                    string valueStr = absValue.ToString();
                    if (setting.IsUnitSet()) { valueStr += " " + setting.GetUnit(); }

                    string error = "";
                    bool success = ApplyHeightmapRegionMapping(obj, valuePercentage, valueStr, regionColor, out error);
                    if (!success) {
                        Debug.LogError("Failed to apply heightmap mapping - " + error +
                           " (region: " + region.GetID() + ", property: " + nfpProperty.GetName() + ")!");
                    }
                }
            }
        }

        /// <summary>
        /// Apply scaling and the color on a NFP heightmap region.<para/>
        /// Returns true on success and false otherwise.
        /// </summary>
        /// <param name="error">Holds the error reason for the failure.</param>
        /// <param name="regionValue">The value to show that this region represents</param>
        private bool ApplyHeightmapRegionMapping(GameObject obj, float percentage, string regionValue, Color color, out string error) {

            error = "";

            // get rect transform required to change scaling
            HeightmapRegionInfo info = obj.GetComponent<HeightmapRegionInfo>();
            if (!info) {
                error = "Missing heightmap region info component!";
                return false;
            }

            // try to apply the color
            if (!Utility.ChangeImageColorTo(info.foregroundPanel.gameObject, color)) {
                error = "Could not change image color of height map region!";
                return false;
            }

            RectTransform rt = info.foregroundPanel; //obj.GetComponent<RectTransform>();
            if (!rt) {
                error = "Missing rect transform of region!";
                return false;
            }

            // set the value text
            if (info.textValueOut) { info.textValueOut.text = regionValue; }

            // apply scaling
            Vector3 newScale = rt.localScale;
            newScale.x = hm_scaleFrom + percentage * (hm_scaleTo - hm_scaleFrom);
            rt.localScale = newScale;
            return true;
        }


        /// <summary>
        /// Apply the mapping on game objects of a feature region.
        /// </summary>
        private void ApplyRegionMapping_Feature(Region region) {

            ARProperty.TYPE propType = ARProperty.TYPE.FEATURE;
            List<GameObject> gameObjects = region.GetUIGameObjects(propType);

            // get the setting for this non functional property (we only show one at a time)
            ValueMappingsLoader mappingsLoader = ApplicationLoader.GetInstance().GetMappingsLoader();

            foreach (GameObject obj in gameObjects) {

                string err_msg = "";
                if (!IsValidRegionGameObject(obj, propType, out err_msg)) {
                    Debug.LogError("[Region: " + region.GetID() + "] " + err_msg, obj);
                    continue;
                }

                // get info and property (we validated them in the previous step)
                RegionGameObject info = obj.GetComponent<RegionGameObject>();
                RProperty_Feature featureProperty = (RProperty_Feature) info.GetProperty();

                // get setting and color from it
                FeatureSetting setting = mappingsLoader.GetFeatureSetting(featureProperty.GetName());

                // use default setting instead if this setting is disabled
                if (!setting.IsActive()) { setting = mappingsLoader.GetFeatureSetting(mappingsLoader.KEY_DEFAULT); }

                Color fixedColor = setting.GetColor();
                bool success = Utility.ChangeImageColorTo(obj, fixedColor);

                if (!success) {
                    Debug.LogError("Failed to change color of FEATURE region object " +
                        "(region: " + region.GetID() + ", property: " + featureProperty.GetName() + ")!");
                }
            }
        }

        /// <summary>
        /// Returns true if this is a valid region gameobject.<para/>
        /// E.g. checks if the "RegionGameObject" component is attached,
        /// the property type matches the passed one, and a feature is active.
        /// </summary>
        /// <param name="error">Writes the error to this variable if one occurs. Is empty otherwise.</param>
        private bool IsValidRegionGameObject(GameObject obj, ARProperty.TYPE propType, out string error) {

            error = "";

            // get RegionGameObject component that holds important information
            RegionGameObject info = obj.GetComponent<RegionGameObject>();
            if (!info) {
                error = "Missing RegionGameObject component on region object (" + obj.name + ")!";
                return false;
            }

            // property information
            if (info.GetProperty() == null) {
                error = "Missing Property in RegionGameObject of object (" + obj.name + ")!";
                return false;
            }

            // get property type stored by the component
            ARProperty.TYPE objPropType = info.GetProperty().GetPropertyType();

            // ensure type is correct
            if (objPropType != propType) {
                error = "Region GameObject has wrong type of property it represents (" + obj.name + ")!";
                return false;
            }

            // ensure for features we really only show selected ones
            if (objPropType == ARProperty.TYPE.FEATURE) {
                string propName = info.GetProperty().GetName().ToLower();
                if (!ApplicationLoader.GetApplicationSettings().GetActiveFeatures().Contains(propName)) {
                    error = "Feature region GameObject not active but requested to be colored (" + obj.name + ")!";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get min/max values of this property accordingly.<para/>
        /// Which min/max values are returned depends on the priority.<para/>
        /// [1] Did the user explicitly set min or max in mapping file? => return min or max or both accordingly<para/>
        /// [2] Does the user want to see local file min/max? => return it<para/>
        /// [3] Return the global min/max for this property.<para/>
        /// Returns null if the property name or the code file is null.<para/>
        /// UPDATE 18.03.2019: Order changed of [1] and [2]! (explicitly set min/max is now used for global and local relativity)
        /// </summary>
        /// <param name="returnNullIfLocalMMNotFound">Returns null if the local min/max could not be found - this is caused if there are no regions in a file</param>
        public static MinMaxValue GetMinMaxValues(string propertyName, CodeFile codeFile, MinMaxValue explicitMinMax, bool returnNullIfLocalMMNotFound = false) {

            // Priority order of shown min/max information (1 = highest priority)
            // 1. min/max explicitly set in the mapping file => more priority bc. the user set it
            // 2. local file min/max information for this property => user interacted intentionally
            // 3. global min/max information for this property (calculated using all files)

            if (propertyName == null || codeFile == null) { return null; }

            // [P3] global min/max values
            MinMaxValue globalMinMax = ApplicationLoader.GetInstance().GetStructureLoader().GetNFPMinMaxValue(propertyName);
            if (globalMinMax == null) { return null; }
            float min = globalMinMax.GetMinValue();
            float max = globalMinMax.GetMaxValue();

            // [P2] local min/max values
            bool showLocalMinMax = ApplicationLoader.GetApplicationSettings().GetApplyLocalMinMaxValues();
            if (showLocalMinMax) {

                MinMaxValue localMinMax = codeFile.GetNFPMinMaxValue(propertyName);
                if (localMinMax == null) {

                    // set both 0 instead of null
                    if (returnNullIfLocalMMNotFound) { return null; }
                    min = max = 0;
                } 
                else {
                    min = localMinMax.GetMinValue();
                    max = localMinMax.GetMaxValue();
                }
            }

            // [P1] min/max explicitly set for this method mapping
            if (explicitMinMax.IsMinValueSet()) { min = explicitMinMax.GetMinValue(); }
            if (explicitMinMax.IsMaxValueSet()) { max = explicitMinMax.GetMaxValue(); }
            return new MinMaxValue(min, max);
        }


        /// <summary>
        /// Updates the heightmap labels according to the currently used min/max values.
        /// </summary>
        /// <param name="setInvalid">Mark label values as invalid using "/"</param>
        private void UpdateHeightmapLabels(bool setInvalid = false) {

            if (!file.GetReferences().GetHeightmap().activeInHierarchy) { return; }

            // get the name of the currently selected non functional property
            string nfpName = ApplicationLoader.GetApplicationSettings().GetSelectedNFP().ToLower();

            // get the setting for this non functional property (we only show one at a time)
            ValueMappingsLoader mappingsLoader = ApplicationLoader.GetInstance().GetMappingsLoader();
            NFPSetting setting = mappingsLoader.GetNFPSetting(nfpName);

            // use default setting instead if this setting is disabled
            if (!setting.IsActive()) { setting = mappingsLoader.GetNFPSetting(mappingsLoader.KEY_DEFAULT); }

            // get currently relevant min/max values
            //AColorMethod colMethod = setting.GetColorMethod(ApplicationSettings.NFP_VIS.HEIGHTMAP); // ToDo: cleanup
            //MinMaxValue minMax = GetMinMaxValues(nfpName, codeFile, colMethod.GetRange()); // ToDo: cleanup
            MinMaxValue minMax = GetMinMaxValues(nfpName, file, setting.GetMinMaxValue(), true);
            string minStr = "/", maxStr = "/";
            if (minMax != null) {
                minStr = minMax.GetMinValue().ToString();
                maxStr = minMax.GetMaxValue().ToString();

                // add unit of values if set
                if (setting.IsUnitSet()) {
                    minStr += " " + setting.GetUnit();
                    maxStr += " " + setting.GetUnit();
                }
            }

            // try to update the heightmap labels
            //Debug.Log("Updating heightmap labels of: " + codeFile.GetNode().GetName(), codeFile.GetReferences().gameObject);
            file.GetReferences().GetHeightmap().SendMessage("SetHeightmapLabel_from", minStr);
            file.GetReferences().GetHeightmap().SendMessage("SetHeightmapLabel_to", maxStr);
        }


        // #################### OLD CODE #################### //

        /// <summary>
        /// Apply the visual properties for all gameObjects of this type.
        /// </summary>
        //private void ApplyVisualProperties(Region region, ARProperty.TYPE propType, object value) {
            
            // These objects should only be the ones created through the user selection.
            // To ensure that we color them correctly anyway, we use the "RegionGameObject" instance
            // that is attached to each region GameObject and tells us about its property type and name.
            // This avoid looking up the application settings everytime because we did this step while spawning.

            //region.ClearUIGameObjects(propType);
            //List<GameObject> gameObjects = region.GetUIGameObjects(propType);

            //foreach (GameObject obj in gameObjects) {

            //    string err_msg = "";
            //    if (!IsValidRegionGameObject(obj, propType, out err_msg)) {
            //        Debug.LogError("[Region: " + region.GetID() + "] " + err_msg);
            //    }

            //    RegionGameObject info = obj.GetComponent<RegionGameObject>();
            //    if (info == null) { continue; }

            //    // get the visual property information (which holds a summary of the values)
            //    VisualProperty visProp = visPropLoader.GetProperty(info.GetProperty());
            //    if (visProp == null) { continue; }

            //    // get and apply defined methods
            //    foreach (VisualPropertyEntryInfo entryInfo in visProp.GetMethods()) {
            //        VisualPropertyMethod method = visPropLoader.GetMethod(entryInfo);
            //        if (method == null) { continue; }

            //        // try to apply the method
            //        method.Apply(obj, visProp, entryInfo);
            //    }
            //}
        //}

    }
}
