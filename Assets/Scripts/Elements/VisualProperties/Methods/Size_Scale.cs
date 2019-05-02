using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.RegionProperties;
using VRVis.Settings;
using VRVis.Spawner.Regions;
using VRVis.Utilities;


namespace VRVis.VisualProperties.Methods {

    /**
     * Class for the "Size_Scale" method.
     * It maps a numeric value to the heightmap scaling.
     * Negative values are not allowed. They count as "missing" entries.
     */
    public class Size_Scale : VisualPropertyMethod {

        private Color heightMapColor_default = new Color(1, 1, 1, 0.6f);
        private Color heightMapColor_missing = new Color(1, 0, 0, 0.2f);

        private readonly float scaleFrom = 0; // minimum scale for value = 0
        private readonly float scaleTo = 1;


        // CONSTRUCTOR

        public Size_Scale(string methodName)
        : base(methodName) {}


        // GETTER AND SETTER
        // ...


        // FUNCTIONALITY

        /**
         * Will apply the size mapping and coloring to the heightmap region.
         * Won't do anything if the passed value is no float.
         * Returns false if not active or errors occured.
         * Only applied to NumericVisualProperties if heightmap visualization is active!
         */
        public override bool Apply(GameObject obj, VisualProperty visProp, VisualPropertyEntryInfo entryInfo) {
            
            // ensure inactive ones are not executed
            if (!entryInfo.IsActive()) { return false; }


            // get region information (including region instance, property and code file)
            RegionGameObject goInfo = null;
            if ((goInfo = GetGameObjectInfo(obj)) == null) { return false; }


            // try to convert the object to float
            // TESTED WITH: https://dotnetfiddle.net/
            //float floatValue;
            //if (!Utility.ObjectToFloat(value, out floatValue)) {
            //    Debug.LogError("Failed to apply a visual property due to conversion failure (value: " + value + ")!");
            //    return false;
            //}


            // apply according to the provided visual property type
            if (visProp.GetType() == typeof(NumericVisualProperty)) {
                
                // check if even one of the supported visualizations is active (currently only heightmap)
                bool oneSupportedActive = false;
                oneSupportedActive = ApplicationLoader.GetApplicationSettings().IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP);
                if (!oneSupportedActive) { return false; }

                // get min and max values
                float min, max;
                RProperty_NFP property = (RProperty_NFP) goInfo.GetProperty();
                if (!GetMinMaxValues(property, goInfo.GetCodeFile(), out min, out max)) {
                    Debug.LogError("Failed to apply visual property method! - Could not get correct min/max values!");
                    return false;
                }

                //Debug.Log("Color Scale Min: " + min);
                //Debug.Log("Color Scale Max: " + max);

                float value = property.GetValue();

                // only apply following code to height map regions for now
                if (goInfo.GetNFPVisType() == ApplicationSettings.NFP_VIS.HEIGHTMAP) {

                    // size of negative values will be 1 with the default color (default behaviour)
                    float p = 1;
                    Color regionColor = heightMapColor_default;
                    if (value < 0) { regionColor = heightMapColor_missing; }
                    else {
                        // map to percentage range and change size accordingly
                        p = GetRangePercentage(min, max, value);
                        p = p < 0 ? 0 : p > 1 ? 1 : p;
                    }

                    // apply color and scaling 
                    return ApplyHeightmapRegion(obj, p, regionColor);
                }
                return false;
            }
            else {
                Debug.LogWarning("Type of visual property not supported for this method: " +
                    GetMethodName() + " (type: " + visProp.GetType() + ")");
                return false;
            }
        }


        /** Apply scaling and color on a heightmap region. */
        private bool ApplyHeightmapRegion(GameObject obj, float percentage, Color color) {

            // get rect transform required to change scaling
            HeightmapRegionInfo info = obj.GetComponent<HeightmapRegionInfo>();
            if (!info) {
                Debug.LogError("Missing heightmap region info component!");
                return false;
            }

            // try to apply the color
            if (!Utility.ChangeImageColorTo(info.foregroundPanel.gameObject, color)) {
                Debug.LogWarning("Could not change image color of height map region!");
                return false;
            }

            RectTransform rt = info.foregroundPanel; //obj.GetComponent<RectTransform>();
            if (!rt) {
                Debug.LogError("Missing rect transform of region!");
                return false;
            }

            // apply scaling
            Vector3 newScale = rt.localScale;
            newScale.x = scaleFrom + percentage * (scaleTo - scaleFrom);
            rt.localScale = newScale;
            return true;
        }

    }
}
