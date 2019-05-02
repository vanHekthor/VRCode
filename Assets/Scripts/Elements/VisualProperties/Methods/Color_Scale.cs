using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.RegionProperties;
using VRVis.Spawner.Regions;
using VRVis.Utilities;


namespace VRVis.VisualProperties.Methods {

    /**
     * Class for the "Color_Scale" method.
     * It maps a numeric value to a color
     * on a specified range and returns it.
     * 
     * Default color range (if not given) is from green to red.
     */
    public class Color_Scale : VisualPropertyMethod {

	    private Color fromColor;
        private Color toColor;


        // CONSTRUCTOR

        public Color_Scale(string methodName, Color fromColor, Color toColor)
        : base(methodName) {
            this.fromColor = fromColor;
            this.toColor = toColor;
        }

        public Color_Scale(string methodName)
        : this(methodName, Color.green, Color.red) {
            // ...
        }


        // GETTER AND SETTER

        public Color GetFromColor() { return fromColor; }
        public void SetFromColor(Color color) { fromColor = color; }

        public Color GetToColor() { return toColor; }
        public void SetToColor(Color color) { toColor = color; }


        // FUNCTIONALITY

        /**
         * Will apply the color mapping to the region.
         * Won't do anything if the passed value is no float.
         * Returns false if not active or errors occured.
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


            // don't apply on height map regions (they have fixed colors)
            if (goInfo.GetNFPVisType() == Settings.ApplicationSettings.NFP_VIS.HEIGHTMAP) { return false; }


            // apply according to the provided visual property type
            if (visProp.GetType() == typeof(NumericVisualProperty)) {
                //NumericVisualProperty numVisProp = (NumericVisualProperty) visProp;

                float min, max;
                RProperty_NFP property = (RProperty_NFP) goInfo.GetProperty();
                if (!GetMinMaxValues(property, goInfo.GetCodeFile(), out min, out max)) {
                    Debug.LogError("Failed to apply visual property method! - Could not get correct min/max values!");
                    return false;
                }

                //Debug.Log("Color Scale Min: " + min);
                //Debug.Log("Color Scale Max: " + max);

                // do not apply color for negative values (default behaviour)
                float value = property.GetValue();
                if (value < 0) { return false; }

                // map to color range and change the color
                float p = GetRangePercentage(min, max, value);
                p = p < 0 ? 0 : p > 1 ? 1 : p;
                Color newColor = (1-p) * fromColor + p * toColor;

                if (!Utility.ChangeImageColorTo(obj, newColor)) {
                    Debug.LogWarning("Failed to change color of a region object (" + obj.name + ") - Missing image component!");
                    return false;
                }
            }
            else {
                Debug.LogWarning("Type of visual property not supported for this method: " +
                    GetMethodName() + " (type: " + visProp.GetType() + ")");
                return false;
            }

            return true;
        }

    }
}
