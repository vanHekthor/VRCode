using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;


namespace VRVis.IO.Features {

    /// <summary>
    /// Class of the "range feature" type
    /// that implements the abstract feature class.<para/>
    /// It is basically just the abstract feature class
    /// without any additional information.
    /// It exists to implement this as a own "type".
    /// </summary>
    public class Feature_Range : AFeature {

        private InfluenceFunction stepFunction = null;

        /// <summary>A list of all values this option can have defined by the step function.</summary>
        private List<float> allValues = null;



        // CONSTRUCTOR

        public Feature_Range(VariabilityModel model, string name, float from, float to, float step)
        : base(model, name, from, to, step) {}

        public Feature_Range(VariabilityModel model, string name)
        : base(model, name, 0, 0, 0) {}



        // GETTER AND SETTER

        /// <summary>Get the step function defining the values this option can have.*/
        public InfluenceFunction GetStepFunction() { return stepFunction; }

        /// <summary>Returns <code>true</code> if the value of the option is valid.</summary>
        public bool IsValueValid() {

            if (IsValueOutOfBounds()) { return false; }

            // ToDo: more checks here?

            return true;
        }


        /// <summary>
        /// Use the step function and the current option value
        /// to calculate the next value on the function.
        /// </summary>
        public float GetNextValue() { return GetNextValue(GetValue()); }

        /// <summary>
        /// Use the step function and the given value
        /// to calculate the next value on the function.<para/>
        /// The value returned can be the same in some cases.
        /// </summary>
        public float GetNextValue(float curValue) {

            // step function must be present
            if (stepFunction == null) {
                Debug.LogWarning("Missing step function! (Option: " + GetName() + ")");
                return curValue;
            }

            // evaluate expression with current option value
            float prevValue = GetValue();
            SetValue(curValue); // set the current value for evaluation
            float result = stepFunction.EvaluateExpression(GetVariabilityModel());
            SetValue(prevValue); // set previous value again
            return result;
        }

        /// <summary>
        /// If all values were calculated previously and stored in the "allValues" list,
        /// we search for the one that is the closest to the passed value
        /// and return the next in the list.<para/>
        /// Returns the same value if the list is not initialized yet or empty.
        /// </summary>
        public float GetNextValueFast(float curValue) {

            // use already calculated values and get next in list
            // (this should make calls faster once the list is initialized)
            if (allValues != null && allValues.Count > 0) {
                int i = FindClosestStepIndex(curValue);
                if (i < 0) { return curValue; }
                return i + 1 < allValues.Count ? allValues[i + 1] : allValues[i];
            }

            return curValue;
        }


        /// <summary>
        /// Find the index of the value that is the closest
        /// to this value in the list of possible values this option can have.<para/>
        /// If the list of possible values is not initialized yet -1 will be returned.
        /// </summary>
        public int FindClosestStepIndex(float value) {

            if (allValues == null) { return -1; }

            int index = 0;
            int closest_index = 0;
            float closest_dist = Mathf.Infinity;

            foreach (float v in allValues) {
                
                if (v == value) { return index; }

                // get index of value with closest distance
                float dist = Mathf.Abs(value - v);
                if (dist < closest_dist) {
                    closest_dist = dist;
                    closest_index = index;
                }

                index++;
            }

            return closest_index;
        }


        /// <summary>
        /// Generates a list of all values this option can have.<para/>
        /// Such values are defined by its step function.<para/>
        /// The size of the list can be huge in some cases, so ensure step functions are defined accordingly.<para/>
        /// The list will only be generated once and will remain in memory after that.<para/>
        /// You can use the method "ReleaseResources" to clear the list.
        /// </summary>
        public List<float> GetAllValues() {

            if (allValues != null) { return allValues; }

            // initialize the list of values
            allValues = new List<float>();

            // start from and add minimum
            float curValue = GetFrom();
            allValues.Add(curValue);

            // stop if the difference to the previous values is x times less than this diff
            float min_diff = 0.001f;
            uint max_dist_less_times = 5;
            uint dist_less_times = 0;
            uint max_loop_steps = 100000; // limit to avoid possible endless loops
            uint loop_steps = 0;

            // continue until maximum reached or value stays the same
            while (curValue < GetTo() && dist_less_times < max_dist_less_times && loop_steps < max_loop_steps) {
                
                float nextValue = GetNextValue(curValue);
                float distToPrev = Mathf.Abs(nextValue - curValue);

                // count how often in a row the difference was too small
                if (distToPrev < min_diff) { dist_less_times++; }
                else { dist_less_times = 0; }

                if (nextValue <= GetTo()) { allValues.Add(nextValue); }
                curValue = nextValue;
                loop_steps++;
            }

            // ToDo: debug - remove if no longer required
            /*
            Debug.LogWarning("Got all values for feature: " + GetName());
            uint i = 0;
            foreach (float val in allValues) {
                Debug.Log((i++) + " = " + val);
            }
            */

            return allValues;
        }



        // FUNCTIONALITY

        /// <summary>Release the list of possible values which can be huge in some cases.</summary>
        public void ReleaseResources() {

            // clear possible values list
            allValues.Clear();
            allValues = null;
        }

        /// <summary>
        /// Load additional information (mainly for the variability model) from XML.<para/>
        /// Code inspired by SPLConqueror to match XML definition:
        /// https://github.com/se-passau/SPLConqueror/blob/master/SPLConqueror/SPLConqueror/NumericOption.cs
        /// </summary>
        public override void LoadFromXML(XmlElement node) {
            
            // load base settings from XML
            base.LoadFromXML(node);

            // iterate over sub-nodes (entries), validate and apply
            bool isValueSet = false;
            foreach (XmlElement subNode in node.ChildNodes) {

                string text = subNode.InnerText.Replace(',', '.');
                switch (subNode.Name) {
                    case "minValue": SetFrom(float.Parse(text, System.Globalization.CultureInfo.InvariantCulture)); break;
                    case "maxValue": SetTo(float.Parse(text, System.Globalization.CultureInfo.InvariantCulture)); break;
                    case "stepFunction": stepFunction = new InfluenceFunction(text, this); break;

                    // [case "values"] not supported because we load "performance influence model" values in another way (from JSON region files)

                    // NOTE: NOT DEFINED IN SPLConqueror! (adds ability to pass default values)
                    case "default":
                        SetValue(float.Parse(text, System.Globalization.CultureInfo.InvariantCulture));
                        isValueSet = true;
                        break;
                }
            }

            // set minimum as initial value
            if (!isValueSet) { SetValue(GetFrom(), true); }
        }

        /// <summary>
        /// Creates an instance of the boolean feature/option and loads settings from the XML.
        /// </summary>
        public static Feature_Range LoadFromXML(XmlElement node, VariabilityModel model) {
            Feature_Range instance = new Feature_Range(model, null);
            instance.LoadFromXML(node);
            return instance;
        }

    }
}
