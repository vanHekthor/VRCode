using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.VisualProperties.Methods;
using VRVis.Utilities;


namespace VRVis.VisualProperties {

    /**
     * Factory that creates instances of visual property methods.
     * The type of the instance will be detected by value of the "base" key.
     */
    public class VisualPropertyMethodFactory {

        /**
         * Creates an instance from the given JSON data and returns it.
         * Can also return "null" if the type could not be found or errors occur.
         */
	    public static VisualPropertyMethod Create(JObject o) {
           
            // minimum requires properties
            string name = (string) o["name"];
            string baseClass = (string) o["base"];
            VisualPropertyMethod method = null;

            // create instance of matching class
            switch (baseClass) {

                case "Color_Scale":
                    method = CreateColorScale(name, o); break;

                case "Color":
                    method = CreateFixedColor(name, o); break;

                case "Size_Scale":
                    method = CreateSizeScale(name, o); break;

            }

            return method;
        }


        /** Create an instance of the color scale method. */
        private static Color_Scale CreateColorScale(string methodName, JObject o) {

            // default color range is from green to red
            Color fromColor = Color.green;
            Color toColor = Color.red;

            // create the color using the input string (values separated by comma)
            if (o["from"] != null) { fromColor = Utility.ColorFromString((string) o["from"], ',', 0.5f); }
            if (o["to"] != null) { toColor = Utility.ColorFromString((string) o["to"], ',', 0.5f); }

            // create instance
            Color_Scale color_scale = new Color_Scale(methodName, fromColor, toColor);

            // add additional information (min & max or min or max)
            float minVal = 0, maxVal = 0;
            bool minGiven = false, maxGiven = false;
            if (o["min"] != null) { if (Utility.StrToFloat((string) o["min"], out minVal, true)) { minGiven = true; }; }
            if (o["max"] != null) { if (Utility.StrToFloat((string) o["max"], out maxVal, true)) { maxGiven = true; }; }
            if (minGiven) { color_scale.SetRangeMin(minVal); }
            if (maxGiven) { color_scale.SetRangeMax(maxVal); }

            // user can allow negative values to be allowed (min and max should then be negative as well!)
            // EDIT: NO LONGER - NEGATIVE DEFINES "MISSING VALUE"
            /*
            if (o["allowNegativeValues"] != null) {

                // ToDo: check for the case that the value is no boolean
                // (maybe simply check for string equals "true" or "false")
                bool allowed = o["allowNegativeValues"].ToObject<bool>();
                color_scale.SetNegativeValuesAllowed(allowed);
            }
            */

            return color_scale;
        }

        /** Create an instance of the fixed color method (used for features). */
        private static Fixed_Color CreateFixedColor(string methodName, JObject o) {

            // default color range is from green to red
            Color color = Color.black;

            // create the color using the input string (values separated by comma)
            if (o["color"] != null) { color = Utility.ColorFromString((string) o["color"], ',', 0.5f); }

            // create instance
            Fixed_Color fixed_color = new Fixed_Color(methodName, color);

            // add additional information
            // nothing to do yet

            return fixed_color;
        }

        /** Create an instance of the color scale method. */
        private static Size_Scale CreateSizeScale(string methodName, JObject o) {

            // create instance
            Size_Scale size_scale = new Size_Scale(methodName);

            // add additional information (min & max or min or max)
            float minVal = 0, maxVal = 0;
            bool minGiven = false, maxGiven = false;
            if (o["min"] != null) { if (Utility.StrToFloat((string) o["min"], out minVal, true)) { minGiven = true; }; }
            if (o["max"] != null) { if (Utility.StrToFloat((string) o["max"], out maxVal, true)) { maxGiven = true; }; }
            if (minGiven) { size_scale.SetRangeMin(minVal); }
            if (maxGiven) { size_scale.SetRangeMax(maxVal); }

            return size_scale;
        }

    }
}
