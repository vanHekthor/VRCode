using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Utilities;

namespace VRVis.Mappings.Methods {

    /// <summary>
    /// A color mapping method.<para/>
    /// Evaluates a color value at a specific time "t".<para/>
    /// Between the "from-color" and "to-color" will be interpolated.<para/>
    /// Optionally 
    /// </summary>
    public abstract class AColorMethod : IMappingMethod {

        private readonly string methodName;

	    private Color fromColor;
        private Color toColor;
        private Color midColor;

        public bool HasNeutralColor { get; private set; }
        private float neutralValue;
        private float ratio = 0.5f;

        // gradient steps enabled when this is greater than 1
        private uint steps = 0;

        // store the generated texture so that we do not create a new one each time
        private readonly Texture2D texture2D = null;


        // CONSTRUCTOR

        public AColorMethod(string methodName, Color fromColor, Color toColor, Color midColor, float neutralValue, float ratio) {
            this.methodName = methodName;
            this.fromColor = fromColor;
            this.toColor = toColor;
            this.midColor = midColor;
            HasNeutralColor = true;
            this.ratio = ratio;
            this.neutralValue = neutralValue;
        }

        public AColorMethod(string methodName, Color fromColor, Color toColor, Color midColor, float neutralValue) 
            : this(methodName, fromColor, toColor, midColor, neutralValue, 0.5f) {}

        public AColorMethod(string methodName, Color fromColor, Color toColor, Color midColor) 
            : this(methodName, fromColor, toColor, midColor, 0.0f) {}

        public AColorMethod(string methodName, Color fromColor, Color toColor) {
            this.methodName = methodName;
            this.fromColor = fromColor;
            this.toColor = toColor;
            HasNeutralColor = false;
        }

        protected AColorMethod(string methodName)
        : this(methodName, Color.green, Color.red) {}


        // GETTER AND SETTER

        public string GetMethodName() { return methodName; }

        public Color GetFromColor() { return fromColor; }
        public void SetFromColor(Color color) { fromColor = color; }

        public Color GetToColor() { return toColor; }
        public void SetToColor(Color color) { toColor = color; }

        public Color GetNeutralColor() { return midColor; }
        public void SetNeutralColor(Color color) { midColor = color; }

        public float GetNeutralValue() { return neutralValue; }
        public void SetNeutralValue(float value) { neutralValue = value; }

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

            Gradient gradient;
            GradientColorKey[] colorKey;
            GradientAlphaKey[] alphaKey;


            gradient = new Gradient();

            // Populate the color keys at the relative time 0 and 1 (0 and 100%)
            if (!HasNeutralColor) {
                colorKey = new GradientColorKey[2];
                colorKey[0].color = fromColor;
                colorKey[0].time = 0.0f;
                colorKey[1].color = toColor;
                colorKey[1].time = 1.0f;

                // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
                alphaKey = new GradientAlphaKey[2];
                alphaKey[0].alpha = fromColor.a;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = toColor.a;
                alphaKey[1].time = 1.0f;
            } else {
                colorKey = new GradientColorKey[3];
                colorKey[0].color = fromColor;
                colorKey[0].time = 0.0f;
                colorKey[1].color = midColor;
                colorKey[1].time = ratio;
                colorKey[2].color = toColor;
                colorKey[2].time = 1.0f;

                // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
                alphaKey = new GradientAlphaKey[3];
                alphaKey[0].alpha = fromColor.a;
                alphaKey[0].time = 0.0f;
                alphaKey[1].alpha = midColor.a;
                alphaKey[1].time = ratio;
                alphaKey[2].alpha = toColor.a;
                alphaKey[2].time = 1.0f;
            }

            gradient.SetKeys(colorKey, alphaKey);

            return gradient.Evaluate(t);

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
