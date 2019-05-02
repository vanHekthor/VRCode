using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
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
    public class Fixed_Color : VisualPropertyMethod {

        private Color color;


        // CONSTRUCTOR

        public Fixed_Color(string methodName, Color color)
        : base(methodName) {
            this.color = color;
        }


        // GETTER AND SETTER

        public Color GetColor() { return color; }
        public void SetColor(Color color) { this.color = color; }



        // FUNCTIONALITY

        /**
         * Will apply the fixed color to the region.
         * Basically ignores the value parameter because it is not required for this method.
         * So passing null for "value" is totally fine.
         * Returns false if not active or errors occured.
         */
        public override bool Apply(GameObject obj, VisualProperty visProp, VisualPropertyEntryInfo entryInfo) {

            // ensure inactive ones are not executed
            if (!entryInfo.IsActive()) { return false; }


            // get region information (including region instance, property and code file)
            RegionGameObject goInfo = null;
            if ((goInfo = GetGameObjectInfo(obj)) == null) { return false; }


            // don't apply a fixed color on height map regions
            if (goInfo.GetNFPVisType() == Settings.ApplicationSettings.NFP_VIS.HEIGHTMAP) { return false; }


            // apply according to the provided visual property type
            if (visProp.GetType() == typeof(FeatureVisualProperty)) {
                //FeatureVisualProperty featureVisProp = (FeatureVisualProperty) propertyInfo; // not required currently

                // change color of the game object
                if (!Utility.ChangeImageColorTo(obj, GetColor())) {
                    Debug.LogWarning("Failed to change color of a region object (" + obj.name + ") - Missing image component!");
                    return false;
                }
            }
            else if (visProp.GetType() == typeof(NumericVisualProperty)) {
            
                // change color of the game object
                if (!Utility.ChangeImageColorTo(obj, GetColor())) {
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
