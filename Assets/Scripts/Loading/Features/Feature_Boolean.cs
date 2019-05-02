using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace VRVis.IO.Features {

    /// <summary>
    /// Class of the "boolean feature" type
    /// that implements the abstract feature class.
    /// </summary>
    public class Feature_Boolean : AFeature {

        private bool optional = true; // tells if optional or mandatory


        // CONSTRUCTOR

        /// <summary>
        /// Create boolean feature/option instance.
        /// Is mandatory by default, so use "SetOptional" to change if required.
        /// </summary>
        /// <param name="model">The variability model instance</param>
        /// <param name="name">Name of the option</param>
        public Feature_Boolean(VariabilityModel model, string name)
        : base(model, name, 0, 1, 1) {
            SetOptional(false); // not optional by default
        }


        // FUNCTIONALITY

        public bool IsOptional() { return optional; }

        public void SetOptional(bool optional) { this.optional = optional; }

        public void SetSelected(bool selected) { SetValue(selected ? 1 : 0); }

        /// <summary>Set the inverse of the current selection state.</summary>
        public void SwitchSelected() { SetSelected(!IsSelected()); }

        /// <summary>Tells if this feature is selected.</summary>
        /// <param name="considerHierarchyConfiguration">Recursively take parent selection state into account</param>
        public bool IsSelected(bool considerHierarchyConfiguration = false) {
            
            if (considerHierarchyConfiguration) { return GetInfluenceValue() == 1; }
            return GetValue() == 1;
        }


        /// <summary>
        /// Checks if all of the passed options have the same parent´as this option.<para/>
        /// Returns <code>true</code> if this is the case, <code>false</code> otherwise.
        /// </summary>
        public bool HaveSameParent(List<AFeature> optionsGroup) {

            foreach (AFeature option in optionsGroup) {
                if (option.GetParent() != GetParent()) { return false; }
            }

            return true;
        }
        

        /// <summary>
        /// Returns <code>true</code> if this binary option has at least
        /// one "group with same parent (alternative)" stored in its "excluded options" list
        /// and if this option itself is a non-optional (mandatory) option.
        /// </summary>
        public bool HasAlternatives() {

            // this option must be mandatory
            if (IsOptional()) { return false; }

            // check if at least one group of excluded options has the same parent
            foreach (List<AFeature> optionGroup in GetExcludedOptions()) {
                if (HaveSameParent(optionGroup)) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Get all options excluded by this option with the same parent.<para/>
        /// This is required to find alternative groups.<para/>
        /// If this option is mandatory, the returned list will be empty.
        /// </summary>
        public List<AFeature> GetAlternativeOptions() {

            //List<AFeature> options = new List<AFeature>();
            HashSet<AFeature> options = new HashSet<AFeature>();
            if (IsOptional()) { return new List<AFeature>(); }

            // check each entry with only a single feature for the same parent
            foreach (List<AFeature> excludedGroup in GetExcludedOptions()) {

                // list of multiple entries is simply skipped
                // as in original SPLC code: https://github.com/se-passau/SPLConqueror/blob/a54ad1bf2afcb873a3a03ac346c3a854b6592ff1/SPLConqueror/SPLConqueror/BinaryOption.cs#L143
                //if (excludedGroup.Count != 1) { continue; }

                // as in original SPLC code
                if (excludedGroup.Count == 1) {

                    // get and validate single excluded option
                    AFeature other = excludedGroup[0];
                    if (ValidExcludedOption(other)) { options.Add(other); }
                    continue;
                }


                // !! =========================================================================================== !!
                //    ToDo: [!!!] investigate how to deal correctly with excluded option entries like "A | B | C"
                // !! =========================================================================================== !!

                // treat them as if they were added as single entries
                foreach (AFeature excludedOption in excludedGroup) {
                    if (ValidExcludedOption(excludedOption)) { options.Add(excludedOption); }
                }
            }

            //return options;
            return new List<AFeature>(options);
        }

        /// <summary>
        /// Validates this option by checking if it is a binary option,
        /// has the same parent as this option and is not optional.
        /// </summary>
        private bool ValidExcludedOption(AFeature other) {

            // check if binary option
            if (!(other is Feature_Boolean)) { return false; }

            // add to result list if parent is the same and option is mandatory
            if (other.GetParent() == GetParent() && ((Feature_Boolean) other).IsOptional() == false) { return true; }
            return false;
        }


        /// <summary>
        /// Get all options excluded by this option,
        /// that do NOT have the same parent (i.e. cross-tree constraints).
        /// </summary>
        public List<List<AFeature>> GetNonAlternativeExcludedOptions() {

            List<List<AFeature>> options = new List<List<AFeature>>();
            foreach (List<AFeature> excludedGroup in GetExcludedOptions()) {
                
                // skip this entry if it contains multiple options
                // as in original SPLC code: https://github.com/se-passau/SPLConqueror/blob/a54ad1bf2afcb873a3a03ac346c3a854b6592ff1/SPLConqueror/SPLConqueror/BinaryOption.cs#L162
                if (excludedGroup.Count != 1) { continue; }

                // get and validate the single excluded option
                AFeature option = excludedGroup[0];
                if (!(option is Feature_Boolean)) { continue; }

                // add if different parent or if both optional with same parent
                if (option.GetParent() != GetParent()) { options.Add(excludedGroup); }
                else if (IsOptional() && option.GetParent() == GetParent() && ((Feature_Boolean) option).IsOptional()) {
                    options.Add(excludedGroup);
                }
            }

            return options;
        }


        /// <summary>
        /// Load additional information (mainly for the variability model) from XML.<para/>
        /// Code inspired by SPLConqueror to match XML definition:
        /// https://github.com/se-passau/SPLConqueror/blob/master/SPLConqueror/SPLConqueror/BinaryOption.cs
        /// </summary>
        public override void LoadFromXML(XmlElement node) {
            
            // load base settings from XML
            base.LoadFromXML(node);

            // iterate over sub-nodes (entries), validate and apply
            foreach (XmlElement subNode in node.ChildNodes) {
                string text = subNode.InnerText.ToLower();
                switch (subNode.Name) {
                    case "optional": 
                        if (text == "true" || text == "1") { SetOptional(true); }
                        else if (text == "false" || text == "0") { SetOptional(false); }
                        break;
                    
                    // NOTE: NOT DEFINED IN SPLConqueror! (adds ability to pass default values)
                    case "default":
                        if (text == "true" || text == "1") { SetSelected(true); }
                        else if (text == "false" || text == "0") { SetSelected(false); }
                        break;
                }
            }
        }

        /// <summary>
        /// Creates an instance of the boolean feature/option and loads settings from the XML.
        /// </summary>
        public static Feature_Boolean LoadFromXML(XmlElement node, VariabilityModel model) {
            Feature_Boolean instance = new Feature_Boolean(model, null);
            instance.LoadFromXML(node);
            return instance;
        }

    }
}
