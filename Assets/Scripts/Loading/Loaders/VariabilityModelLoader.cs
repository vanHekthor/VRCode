using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRVis.IO.Features;

namespace VRVis.IO {

    /// <summary>
    /// Loads the variability model / aka. FEATURE MODEL from a given XML file.<para/>
    /// 
    /// The loading process is inspired by
    /// <a href="https://github.com/se-passau/SPLConqueror/blob/master/SPLConqueror/SPLConqueror/VariabilityModel.cs">SPL Conqueror</a>
    /// to guarantee that the same files can be loaded.
    /// </summary>
    public class VariabilityModelLoader : FileLoader {

        private static readonly string MODEL_DEFAULT_NAME = "Variability Model";
        private readonly VariabilityModel model;

        // CONSTRUCTOR

        public VariabilityModelLoader(string filePath)
        : base(filePath) {
            model = new VariabilityModel(MODEL_DEFAULT_NAME);
        }


        // GETTER AND SETTER

        public VariabilityModel GetModel() { return model; }


        // FUNCTIONALITY

        public override bool Load() {

            loadingSuccessful = false;

            // load model from xml file
            if (!model.LoadXML(GetFilePath())) { return false; }            
        
            Debug.Log("Loading variability model finished: " + model.GetName() + "\n(" +
                "Options: " + (model.GetOptionCount()-1) +
                ", Binary: " + (model.GetBinaryOptionCount()-1) + 
                ", Numeric: " + model.GetNumericOptionCount() +
            ")");
            loadingSuccessful = true;
            return true;
        }

        /// <summary>
        /// Checks if the model is valid and used.<para/>
        /// NOTE: returns also true if the model is null!
        /// So check for this case yourself.
        /// </summary>
        /// <param name="reason">In case false is returned, tells why</param>
        public bool IsModelValidAndUsed(VariabilityModel vm, out string reason) {

            reason = "";
            if (model == null) { return true; }

            bool validationRequired = vm.ChangedSinceLastValidation();
            bool invalid = !vm.GetLastValidationStatus();
            bool appliedOnce = vm.GetValuesAppliedOnce();

            if (validationRequired) {
                reason = "Variability Model not validated!";
                return false;
            }
            else if (invalid) {
                reason = "Variability Model is invalid!";
                return false;
            }
            else if (!appliedOnce) {
                reason = "Variability Model not applied yet!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Puts the model option hierarchy in JSON format.<para/>
        /// Copy the output and put it into a formatter like: https://jsonformatter.curiousconcept.com/.
        /// </summary>
        public static string GetModelHierarchyRecursivelyJSON(AFeature node) {

            // node info itself
            StringBuilder strb = new StringBuilder("{\"node\": \"" + node.GetName() + "\"");

            // add child nodes to the output
            if (node.HasChildren()) { strb.Append(", \"children\": [ "); }
            foreach (AFeature subNode in node.GetChildren()) {
                strb.Append(GetModelHierarchyRecursivelyJSON(subNode));
                strb.Append(",");
            }
            if (strb[strb.Length-1].Equals(',')) { strb.Remove(strb.Length-1, 1); }
            if (node.HasChildren()) { strb.Append(" ]"); }

            strb.Append("}");
            return strb.ToString();
        }

    }
}
