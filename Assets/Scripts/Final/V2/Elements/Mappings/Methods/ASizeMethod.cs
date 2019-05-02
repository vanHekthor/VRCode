using System.Collections;
using System.Collections.Generic;
using VRVis.Utilities;

namespace VRVis.Mappings.Methods {

    /// <summary>
    /// Abstract base method for size methods.<para/>
    /// Between the "from-color" and "to-color" will be interpolated.<para/>
    /// 
    /// Possible ToDo:
    /// - AColorMethod and ASizeMethod with generic to one method
    /// </summary>
    public class ASizeMethod : IMappingMethod {

        private readonly string methodName;

        private float fromSize;
        private float toSize;

        //private readonly MinMaxValue range = new MinMaxValue(); // ToDo: cleanup


        // CONSTRUCTOR

        public ASizeMethod(string methodName, float fromSize, float toSize) {
            this.methodName = methodName;
            this.fromSize = fromSize;
            this.toSize = toSize < fromSize ? fromSize : toSize;
        }

        protected ASizeMethod(string methodName)
        : this(methodName, 0, 1) {
            // ...
        }


        // GETTER AND SETTER

        public string GetMethodName() { return methodName; }

        public float GetFromSize() { return fromSize; }

        public void SetFromSize(float size) { fromSize = size; }

        public float GetToSize() { return toSize; }

        /// <summary>Set to value (if less than from, from value will be assigned instead)</summary>
        public void SetToSize(float size) {
            size = size < fromSize ? fromSize : size;
            toSize = size;
        }

        //public MinMaxValue GetRange() { return range; } // ToDo: cleanup


        // FUNCTIONALITY

        /// <summary>
        /// Evaluate the color value at position t.<para/>
        /// Values out of bounds [0, 1] will be cropped.
        /// </summary>
        public float Evaluate(float t) {
            
            // validate bounds of t
            t = t < 0 ? 0 : t > 1 ? 1 : t;

            if (t == 0) { return GetFromSize(); }
            if (t == 1) { return GetToSize(); }

            float evalSize = (1-t) * GetFromSize() + t * GetToSize();
            return evalSize;
        }

    }
}
