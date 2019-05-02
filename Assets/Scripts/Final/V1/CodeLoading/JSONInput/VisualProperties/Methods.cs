using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siro;

namespace Siro.VisualProperties {

    /**
     * Holds information about a method that should be called.
     * More attributes can follow in the future, thats why a struct is used.
     */
    public struct MethodInformation {

        public string name;

        public MethodInformation(string name) {
            this.name = name;
        }
    }

    /**
     * Visual property mapping methods.
     */
    public class Methods {

        /**
         * Returns a color for a value in the specified range
         * converting the color from a string.
         * 
         * value = input value
         * min = min range
         * max = max range
         * fromColor = color that corresponds to the minimum
         * toColor = color that corresponds to the maximum
         */
        private static Color Color_Scale(float value, float min, float max, string fromColor, string toColor) {
            
            Vector3 cFromVec = Utility.VectorFromString(fromColor, ',');
            Vector3 cToVec = Utility.VectorFromString(toColor, ',');

            Color cFrom = new Color(cFromVec.x, cFromVec.y, cFromVec.z);
            Color cTo = new Color(cToVec.x, cToVec.y, cToVec.z);

            return Color_Scale(value, min, max, cFrom, cTo);
        }

        /** Returns a color for a value in the specified range. */
        private static Color Color_Scale(float value, float min, float max, Color fromColor, Color toColor) {
            float perc = (value-min) / (max-min);
            return (1-perc) * fromColor + perc * toColor;
        }
	    
        /** Map the value from green (less) to red (greater). */
        public static Color Color_Scale_1(float value, float min, float max) {
            Color cFrom = new Color(0.2f, 1.0f, 0.2f);
            Color cTo = new Color(1.0f, 0.2f, 0.2f);
            return Color_Scale(value, min, max, cFrom, cTo);
        }

    }


    ///**
    // * Creates visual property instances according to the given method.
    // */
    //public class VisualPropertyFactory {

    //    /**
    //     * Create a visual property based on the passed method name.
    //     * Returns the visual property instance or null
    //     * if the method does not exist!
    //     */
    //    public static VisProp Create(string methodName, PropertyInformation propertyInfo, float propertyValue) {

    //        switch (methodName) {

    //            case "Color_Scale_1":
    //                VisProp_SingleValue vp = new VisProp_SingleValue(
    //                    propertyInfo.propertyType,
    //                    VisProp.ATTRIBUTE.COLOR,
    //                    propertyValue
    //                );

    //                break;

    //        }

    //        Debug.LogWarning("Failed to create visual property for method: " + methodName);
    //        return null;
    //    }

    //}
}