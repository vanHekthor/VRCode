using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Mappings.Methods.Base;
using VRVis.Utilities;

namespace VRVis.Mappings.Methods {

     /// <summary>
     /// Factory that creates instances of mapping methods.<para/>
     /// The type of the instance will be detected by value of the "base" key.
     /// </summary>
    public class MappingMethodFactory {

        /// <summary>
        /// Creates an instance from the given JSON data and returns it.<para/>
        /// Can also return "null" if the type could not be found or errors occur.
        /// </summary>
	    public static IMappingMethod Create(JObject o) {
           
            // minimum requires properties
            string name = o["name"] != null ? (string) o["name"] : "";
            string baseClass = (string) o["base"];
            IMappingMethod method = null;

            // create instance of matching class
            switch (baseClass) {

                case "Color_Scale":
                    method = CreateColorScale(name, o); break;
                
                case "Color":
                case "Color_Fixed":
                    method = CreateFixedColor(name, o); break;
                
                case "Width_Scale":
                    method = CreateWidthScale(name, o); break;
                
                case "Width":
                case "Width_Fixed":
                    method = CreateFixedWidth(name, o); break;
                
            }

            return method;
        }


        /// <summary>Create an instance of the color scale method.</summary>
        private static Color_Scale CreateColorScale(string methodName, JObject o) {

            // default colors
            Color fromColor = Color.black;
            Color toColor = Color.black;

            // create the color using the input string (values separated by comma)
            if (o["from"] != null) { fromColor = Utility.ColorFromString((string) o["from"], ',', 0.5f); }
            if (o["to"] != null) { toColor = Utility.ColorFromString((string) o["to"], ',', 0.5f); }

            // create instance
            Color_Scale color_scale = new Color_Scale(methodName, fromColor, toColor);

            // gradient steps
            uint steps = 0;
            if (o["steps"] != null) { if (uint.TryParse((string) o["steps"], out steps)) { color_scale.SetSteps(steps); } }

            return color_scale;
        }

        /// <summary>Create an instance of the fixed color method (useful for features and edges).</summary>
        private static Color_Fixed CreateFixedColor(string methodName, JObject o) {

            // default color is black
            Color color = Color.black;

            // create the color using the input string (values separated by comma)
            if (o["color"] != null) { color = Utility.ColorFromString((string) o["color"], ',', 1); }

            // create instance
            Color_Fixed fixed_color = new Color_Fixed(methodName, color);

            // add additional information
            // nothing to do yet

            return fixed_color;
        }

        /// <summary>Create an instance of the width scale method (useful for edges).</summary>
        private static Width_Scale CreateWidthScale(string methodName, JObject o) {

            // default values
            float fromValue = 10; // min 0
            float toValue = 50; // max 100

            // create the color using the input string (values separated by comma)
            if (o["from"] != null) { Utility.StrToFloat((string) o["from"], out fromValue, true); }
            if (o["to"] != null) { Utility.StrToFloat((string) o["to"], out toValue, true); }

            // create instance based on validated values
            fromValue = fromValue < 0 ? 0 : fromValue > 100 ? 100 : fromValue;
            toValue = toValue < 0 ? 0 : toValue > 100 ? 100 : toValue < fromValue ? fromValue : toValue;
            Width_Scale size_scale = new Width_Scale(methodName, fromValue, toValue);

            return size_scale;
        }

        /// <summary>Create an instance of the fixed width method.</summary>
        private static Width_Fixed CreateFixedWidth(string methodName, JObject o) {

            // default width value
            float width = 0;

            // create the color using the input string (values separated by comma)
            if (o["value"] != null) { Utility.StrToFloat((string) o["value"], out width, true); }

            // create instance
            Width_Fixed fixed_width = new Width_Fixed(methodName, width);

            // add additional information
            // nothing to do yet

            return fixed_width;
        }

    }
}
