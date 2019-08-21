using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Utilities;

namespace VRVis.Mappings.Methods {

    /// <summary>
    /// A color mapping method.<para/>
    /// Evaluates a color value at a specific time "t".<para/>
    /// Between the "from-color" and "to-color" will be interpolated.<para/>
    /// 
    /// Recently added:<para/>
    /// - continuous or stepwise interpolation
    /// </summary>
    public abstract class AColorMethod : IMappingMethod {

        private readonly string methodName;

	    private Color fromColor;
        private Color toColor;

        // gradient steps enabled when this is greater than 1
        private uint steps = 0;

        // store the generated texture so that we do not create a new one each time
        private readonly Texture2D texture2D = null;


        // CONSTRUCTOR

        public AColorMethod(string methodName, Color fromColor, Color toColor) {
            this.methodName = methodName;
            this.fromColor = fromColor;
            this.toColor = toColor;
        }

        protected AColorMethod(string methodName)
        : this(methodName, Color.green, Color.red) {}


        // GETTER AND SETTER

        public string GetMethodName() { return methodName; }

        public Color GetFromColor() { return fromColor; }
        public void SetFromColor(Color color) { fromColor = color; }

        public Color GetToColor() { return toColor; }
        public void SetToColor(Color color) { toColor = color; }

        public uint GetSteps() { return steps; }

        /// <summary>Set steps > 1 to enable gradient steps.</summary>
        public void SetSteps(uint steps) { this.steps = steps; }



        // FUNCTIONALITY

        /// <summary>
        /// Evaluate the color value at position t.<para/>
        /// Values out of bounds [0, 1] will be cropped.
        /// </summary>
        public Color Evaluate(float t) {

            // validate bounds of t
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            t = ApplyGradientSteps(t);

            if (t == 0) { return GetFromColor(); }
            if (t == 1) { return GetToColor(); }

            Color evalColor = (1-t) * fromColor + t * toColor;
            return evalColor;
        }

        /// <summary>
        /// Takes the current value (between [0, 1]) and modifies it
        /// so that the result is a stepwise color gradient.<para/>
        /// Returns the unmodified value (between [0, 1]) if steps is > 1.<para/>
        /// </summary>
        private float ApplyGradientSteps(float t) {

            if (steps < 2) { return t; }

            float step = 1f / steps;
            float stepIndex = Mathf.Floor(t / step);
            float newValue = stepIndex / (steps - 1);

            // result is > 1 if t is one, so ensure its in bounds
            return newValue > 1 ? 1 : newValue;
        }

        /// <summary>
        /// Creates a Texture2D instance from the color scale
        /// or returns the already created texture.
        /// </summary>
        /// <param name="forceRecreation">Force recreation of the texture</param>
        public Texture2D CreateTexture2D(int width, int height, bool forceRecreation = false) {
        
            // simply return it instead of recreating
            if (texture2D != null) { return texture2D; }

            Texture2D tex = new Texture2D(width, height) {
                name = GetMethodName(),
                filterMode = FilterMode.Point
            };

            // set the texture pixels
            for (int x = 0; x < width; x++) {

                float percOnScale = (x + 1) / (float) width;
                Color columnColor = Evaluate(percOnScale);

                // set color for the whole column
                for (int y = 0; y < height; y++) {
                    tex.SetPixel(x, y, columnColor);
                }
            }
            
            // apply the set pixels
            tex.Apply(true, true);
            return tex;
        }

    }
}
