using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;

namespace VRVis.IO.Features {

    /// <summary>
    /// Abstract base class for all types of features.<para/>
    /// This base class basically implements a "range feature".
    /// Because everything can be defined as a range of numbers.
    /// For instance, a boolean feature is a range of 0 to 1,
    /// defined with a step size of 1 (on or off).
    /// </summary>
    public abstract class AFeature {

        private readonly VariabilityModel model;

        private string name = null;
        private string name_display = null;

        private float from;
        private float to;
        private readonly float step; // step size

        private float value_default;
        private float value; // current value

        private string parentName = null;
        private AFeature parent = null;
        private Dictionary<string, AFeature> children = new Dictionary<string, AFeature>();

        // "List, in which the current option implies one and/or a combination of other options"
        // could be parsed from an entry similar to this: "A|B|C" or "G|D|F"
        private List<List<AFeature>> impliedOptions = new List<List<AFeature>>();
        private List<string[]> impliedOptions_names = new List<string[]>();

        // "List, in which the current option excludes the selection of one and/or a combination of other options"
        // could be parsed from an entry similar to this: "A|B|C" or "G|D|F"
        private List<List<AFeature>> excludedOptions = new List<List<AFeature>>();
        private List<string[]> excludedOptions_names = new List<string[]>();

        private bool isValidInitialized = false;

        /// <summary>Index in performance influence model array of values.</summary>
        private uint pimIndex = 0;



        // CONSTRUCTORS

        public AFeature(VariabilityModel model, string name, float from, float to, float step, float value) {
            this.model = model;
            name_display = name ?? ""; // https://docs.microsoft.com/de-de/dotnet/csharp/language-reference/operators/null-coalescing-operator
            this.name = name != null ? name.ToLower() : "";
            this.from = from;
            this.to = to < from ? from : to;
            this.step = step > to - from ? to - from : step;
            SetValue(value, true);
        }

        /// <summary>Sets the "from" value as default value.</summary>
        public AFeature(VariabilityModel model, string name, float from, float to, float step)
        : this(model, name, from, to, step, from) {}



        // GETTER AND SETTER

        public VariabilityModel GetVariabilityModel() { return model; }


        /// <summary>Get the name (in lowercase - use GetDisplayname() to get original one).</summary>
        public string GetName() { return name; }

        /// <summary>
        /// First removes invalid characters from the name (if there are any)
        /// and then sets the name to the result string converted to LOWER CASE!
        /// </summary>
        private void SetName(string name) { this.name = RemoveInvalidCharsFromName(name).ToLower(); }


        /// <summary>The display name that wont be formatted or validated.</summary>
        public string GetDisplayName() {
            if (name_display == null) { return name; }
            return name_display;
        }

        public void SetDisplayName(string name) { name_display = name; }


        public float GetFrom() { return from; }
        public void SetFrom(float from) { this.from = from; }

        public float GetTo() { return to; }
        public void SetTo(float to) { this.to = to; }


        /** ToDo: remove if no longer used! */
        public float GetStep() { return step; }


        /// <summary>Get the currently set value of this feature.</summary>
        public float GetValue() { return value; }

        public float GetDefaultValue() { return value_default; }

        /// <summary>
        /// Get the current value considering the hierarchy configuration.<para/>
        /// The returned value just tells about the current influence in the variability model.<para/>
        /// As a result, the value can be 0 even if the minimum is not!<para/>
        /// This is used for calculating the performance influence model value.
        /// </summary>
        public float GetInfluenceValue() {

            float parentSelected = 1;
            if (HasParent()) {

                AFeature parent = GetParent();
                if (parent is Feature_Boolean) {
                    parentSelected = ((Feature_Boolean) parent).IsSelected(true) ? 1 : 0;
                }
            }

            return value * parentSelected;
        }
        

        /// <summary>Set the value of this feature (will also crop it to the bounds).</summary>
        public void SetValue(float value, bool setAsDefault = false, bool ignoreBounds = false) {

            if (!ignoreBounds) { value = value < from ? from : value > to ? to : value; }
            if (setAsDefault) { value_default = value; }

            float prevValue = this.value;
            float newValue = value;

            if (this.value == value) { return; }
            this.value = value;

            // notify variability model about the change
            model.ValueChangeNotification(this, prevValue, newValue);
        }


        /// <summary>Get child options.</summary>
        public IEnumerable<AFeature> GetChildren() { return children.Values; }
        public int GetChildrenCount() { return children == null ? 0 : children.Count; }
        public bool HasChildren() { return GetChildrenCount() > 0; }

        /// <summary>Add the option/feature as a child of this feature.</summary>
        public bool AddChild(AFeature option) {
            string cName = option.GetName();
            if (children.ContainsKey(cName)) { return false; }
            children.Add(cName, option);
            return true;
        }


        /// <summary>Get the parent option or null if not set.</summary>
        public AFeature GetParent() { return parent; }
        public bool HasParent() { return parent != null; }

        /// <summary>
        /// Set feature/option as the parent.<para/>
        /// Will also add this as a child to the parent.
        /// </summary>
        public void SetParent(AFeature parent) {
            if (parent == this.parent) { return; }
            this.parent = parent;
 
            if (parent == null) { return; }
            parentName = parent.GetName();
            parent.AddChild(this);
        }

        /// <summary> Get the parent name or null if not set.
        public string GetParentName() { return parentName; }

        /// <summary>
        /// Removes invalid characters from the passed string
        /// and converts it to LOWER CASE before setting it as the parent name.
        /// </summary>
        private void SetParentName(string name) {
            if (string.IsNullOrEmpty(name)) { name = VariabilityModel.ROOT_NAME; }
            parentName = RemoveInvalidCharsFromName(name).ToLower();
        }


        public IEnumerable<List<AFeature>> GetExcludedOptions() { return excludedOptions; }

        public int GetExcludedOptionsCount() { return excludedOptions.Count; }


        public IEnumerable<List<AFeature>> GetImpliedOptions() { return impliedOptions; }
        
        public int GetImpliedOptionsCount() { return impliedOptions.Count; }


        /// <summary>
        /// Get the affected index in the performance influence model.<para/>
        /// This value must be greater than zero because at index zero is the base value.<para/>
        /// As a result, checking if this value is greater than zero is equal to checking if the index was set!
        /// </summary>
        public uint GetPIMIndex() { return pimIndex; } // ToDo: obsolete - cleanup

        public void SetPIMIndex(uint index) { pimIndex = index; } // ToDo: obsolete - cleanup



        // FUNCTIONALITY

        public void IncreaseValue() { SetValue(GetValue() + step); }

        public void DecreaseValue() { SetValue(GetValue() - step); }

        /// <summary>Sets the value at its default value.</summary>
        public void ResetValue() { SetValue(GetDefaultValue()); }

        /// <summary>False if something went wrong during initialization.</summary>
        public bool IsValidInitialized() { return isValidInitialized; }

        /// <summary>Checks if the given value is out of bounds.</summary>
        public bool IsValueOutOfBounds(float value) { return value < GetFrom() || value > GetTo(); }

        /// <summary>Tells if the current value is out of bounds.</summary>
        public bool IsValueOutOfBounds() { return IsValueOutOfBounds(GetValue()); }


        /// <summary>
        /// Remove invalid characters from a given feature/option name.
        /// Allowed are only letters and numbers, separated by "_".
        /// </summary>
        /// <returns>The valid string with invalid characters removed</returns>
        public static string RemoveInvalidCharsFromName(string name) {

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++) {
                char c = name[i];
                if (!char.IsLetterOrDigit(c) && !c.Equals('_')) { continue; }
                sb.Append(c);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Load additional information (mainly for the variability model) from XML.<para/>
        /// The tags "prefix" and "postfix" are not supported because we don't require them.<para/>
        /// Code inspired by SPLConqueror to match XML definition:
        /// https://github.com/se-passau/SPLConqueror/blob/master/SPLConqueror/SPLConqueror/ConfigurationOption.cs <para/>
        /// Consider using the "Init()" method after loading the data to make this instance usable!
        /// </summary>
        public virtual void LoadFromXML(XmlElement node) {

            foreach (XmlElement subNode in node.ChildNodes) {
                switch (subNode.Name) {
                    case "name": SetName(subNode.InnerText); break;
                    case "outputString": SetDisplayName(subNode.InnerText); break;
                    case "parent": SetParentName(subNode.InnerText); break;

                    case "impliedOptions":
                        foreach (XmlElement n in subNode.ChildNodes) {
                            string[] options = n.InnerText.Split(new string[]{"|"}, StringSplitOptions.RemoveEmptyEntries);

                            // ToDo: maybe perform name validation already here so that we don't have to do it later several times

                            impliedOptions_names.Add(options);
                        }
                        break;

                    case "excludedOptions":
                        foreach (XmlElement n in subNode.ChildNodes) {
                            string[] options = n.InnerText.Split(new string[]{"|"}, StringSplitOptions.RemoveEmptyEntries);

                            // ToDo: maybe perform name validation already here so that we don't have to do it later several times

                            excludedOptions_names.Add(options);
                        }
                        break;

                    // ToDo: obsolete - cleanup
                    case "pimIndex":
                        uint i = 0;
                        if (uint.TryParse(subNode.InnerText, out i)) { SetPIMIndex(i); }
                        break;
                }
            }
        }


        /// <summary>
        /// Loading XML only results in loading names of other XML objects that should be used.
        /// This step initialized the feature/option by replacing these names
        /// with the actual instance of corresponding options in the VariabilityModel.
        /// </summary>
        public void Init() {

            isValidInitialized = true;

            // get parent option by name
            if (parentName != null) {
                AFeature parentOption = model.GetBinaryOption(parentName, true);
                if (parentOption != null) { SetParent(parentOption); }
                else {
                    Debug.LogError("Feature " + GetName() + " has no valid binary parent (" + parentName + ")!");
                    isValidInitialized = false;
                }
            }

            // get instances of implied options
            impliedOptions.Clear();
            foreach (string[] names in impliedOptions_names) {

                List<AFeature> list = new List<AFeature>();
                foreach (string name in names) {

                    AFeature option = model.GetOption(name.ToLower(), true);
                    if (option == null) { continue; }
                    list.Add(option);
                }

                impliedOptions.Add(list);
            }

            // get instances of excluded options
            excludedOptions.Clear();
            foreach (string[] names in excludedOptions_names) {

                List<AFeature> list = new List<AFeature>();
                foreach (string name in names) {

                    AFeature option = model.GetOption(name.ToLower(), true);
                    if (option == null) { continue; }
                    list.Add(option);
                }

                // as done in SPLC code: https://github.com/se-passau/SPLConqueror/blob/a54ad1bf2afcb873a3a03ac346c3a854b6592ff1/SPLConqueror/SPLConqueror/ConfigurationOption.cs#L283
                excludedOptions.Add(list);
            }
        }


        /// <summary>
        /// Update reference of parent node instance for each node
        /// that has "parentNode" set to the name of this node.
        /// </summary>
        public void UpdateChildrenParent() {
            
            foreach (AFeature option in model.GetBinaryOptions()) {
                if (option.GetParentName() != null && option.GetParentName().Equals(name)) {
                    option.SetParent(this);
                }
            }
        }


        /// <summary>
        /// Checks the lists of the list of excluded options,
        /// if a list with a single entry contains the passed option.
        /// </summary>
        public bool ExcludesOption(AFeature option) {

            foreach (List<AFeature> excludedList in excludedOptions) {

                if (excludedList.Count == 1 && excludedList[0] == option) { return true; }
                else {

                    // !! =========================================================================================== !!
                    //    ToDo: [!!!] investigate how to deal correctly with excluded option entries like "A | B | C"
                    // !! =========================================================================================== !!

                    // currently done: treat each entry as if it was added as a single one
                    foreach (AFeature excludedOption in excludedList) {
                        if (excludedOption == option) { return true; }
                    }
                }
            }
            
            return false;
        }

    }
}
