using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using VRVis.RegionProperties;

namespace VRVis.IO.Features {

    /// <summary>
    /// Class thats representing a feature model.
    /// Will be initialized by the VariabilityModelLoader class.<para/>
    /// Notice that a feature "name" is treated as a unique identifier and could be referred to as "id".
    /// </summary>
    public class VariabilityModel {

        public static readonly string ROOT_NAME = "root";

        public static readonly Color COLOR_VALID = new Color(0.45f, 0.75f, 0.50f); // model valid color
        public static readonly Color COLOR_INVALID = new Color(0.77f, 0.33f, 0.33f); // model invalid color
        public static readonly Color COLOR_CHANGED = new Color(0.9f, 0.83f, 0.55f); // update required color

        private string name;
        private readonly Feature_Boolean root = null;
        private List<Feature_Boolean> binaryOptions = new List<Feature_Boolean>();
        private List<Feature_Range> numericOptions = new List<Feature_Range>();
        private List<string> binaryConstraints = new List<string>();

        // ToDo: implement nonBooleanConstraints and mixedConstraints
        /*
        private List<NonBooleanConstraint> nonBooleanConstraints = new List<NonBooleanConstraint>();
        private List<MixedConstraint> mixedConstraints = new List<MixedConstraint>();
        */

        // FOLLOWING IS ONLY REQUIRED to get the index of an option quickly
        // dictionary of feature/option name (key) and the feature/option instance (value)
        private Dictionary<int, string> indexToOption = new Dictionary<int, string>();
        private Dictionary<string, int> optionToIndex = new Dictionary<string, int>();

        /// <summary>Key = name of the option/feature, Value = the option/feature instance</summary>
        private Dictionary<string, AFeature> options = new Dictionary<string, AFeature>();


        // ToDo: maybe make use of this if there are no "pimIndex" elements in the XML document given?
        /// <summary>Holds the keys in the same order as read from the array (required to get correct value from region array)</summary>
        private List<string> arrayOrder = new List<string>();


        // ToDo: remove if no longer required? (replaced by global config entry "features")
        /// <summary>Holds IDs of options/features that should be applied at the array index equal to the value of a key.</summary>
        //private Dictionary<uint, List<int>> pimIndices = new Dictionary<uint, List<int>>();
        //private uint greatestPIMIndex = 0;


        /// <summary>Holds IDs of options/features that should be applied at the array index equal to the entrys list index.</summary>
        private List<int> featuresOrder = null;

        // store last validation status and if it changed since then
        private bool currentlyValidating = false;
        private bool lastValidationStatus = false;
        private bool changedSinceLastValidation = true; // default is true
        private bool valuesAppliedOnce = false;



        // CONSTRUCTOR

        public VariabilityModel(string name) {

            this.name = name;

            // add root as default binary option
            root = new Feature_Boolean(this, ROOT_NAME);
            root.SetSelected(true);
            root.SetReadOnly(true);
            binaryOptions.Add(root);

            // [INDEXING] root
            arrayOrder.Add(ROOT_NAME);
            options.Add(ROOT_NAME, root);
            indexToOption.Add(0, ROOT_NAME);
            optionToIndex.Add(ROOT_NAME, 0);
        }



        // GETTER AND SETTER

        public string GetName() { return name; }

        public Feature_Boolean GetRoot() { return root; }

        public List<Feature_Range> GetNumericOptions() { return numericOptions; }

        public List<Feature_Boolean> GetBinaryOptions() { return binaryOptions; }

        public bool HasOption(string name) { return HasOption(name, false); }

        /// <summary>Check if the option exists.</summary>
        /// <param name="name">The name of the option</param>
        /// <param name="validateName">Removes invalid characters from the name</param>
        public bool HasOption(string name, bool validateName) {

            // remove invalid characters (not letter and not number or "_")
            if (validateName) { name = AFeature.RemoveInvalidCharsFromName(name); }
            return options.ContainsKey(name);
        } 

        /// <summary>
        /// Get the binary feature/option for the given name after
        /// validating it or null if it could not be found.
        /// </summary>
        /// <returns>The feature/option instance or null</returns>
        public Feature_Boolean GetBinaryOption(string name, bool validateName) {

            if (validateName) { name = AFeature.RemoveInvalidCharsFromName(name); }
            if (!HasOption(name, false)) { return null; }

            foreach (Feature_Boolean option in binaryOptions) {
                if (option.GetName().Equals(name)) { return option; }
            }
            return null;
        }

        /// <summary>
        /// Get the numeric feature/option for the given name after validating it.
        /// </summary>
        /// <returns>The feature/option instance or null</returns>
        public Feature_Range GetNumericOption(string name, bool validateName) {

            if (validateName) { name = AFeature.RemoveInvalidCharsFromName(name); }
            if (!HasOption(name, false)) { return null; }

            foreach (Feature_Range option in numericOptions) {
                if (option.GetName().Equals(name)) { return option; }
            }
            return null;
        }

        /// <summary>Tries to get the option by name from binary and numeric options.</summary>
        /// <returns>The feature/option instance or null</returns>
        public AFeature GetOption(string name, bool validateName) {

            AFeature option = null;
            if ((option = GetBinaryOption(name, validateName)) != null) { return option; }
            return (option = GetNumericOption(name, validateName));
        }

        /// <summary>Get index position for this option. Returns "-1" if not found!</summary>
        /// <returns>The position of the feature or "-1" if not found!</returns>
        public int GetOptionIndex(string name, bool validateName) {
            
            if (validateName) { name = AFeature.RemoveInvalidCharsFromName(name); }
            if (!optionToIndex.ContainsKey(name)) { return -1; }
            return optionToIndex[name];
        }

        /// <summary>Get the option at this index position or null if not found!</summary>
        public AFeature GetOption(int index) {
            if (!indexToOption.ContainsKey(index)) { return null; }
            return GetOption(indexToOption[index], false);
        }

        /// <summary>Returns amount of loaded options (including the root!).</summary>
        public int GetOptionCount() { return options.Count; }

        /// <summary>Returns amount of loaded binary options (including the root!).</summary>
        public int GetBinaryOptionCount() { return binaryOptions.Count; }

        /// <summary>Returns amount of loaded numeric options.</summary>
        public int GetNumericOptionCount() { return numericOptions.Count; }

        /// <summary>Returns the storage order of the read array.</summary>
        public List<string> GetArrayOrder() { return arrayOrder; }


        /// <summary>If a variability model validator is currently validating this model configuration.</summary>
        public bool IsCurrentlyBeingValidated() { return currentlyValidating; }

        /// <summary>Should only be changed by a VariabilityModelValidator instance.</summary>
        public void SetCurrentlyBeingValidated(bool state) { currentlyValidating = state; }

        /// <summary>Tells if any of the options changed and a re-validation is required.</summary>
        public bool ChangedSinceLastValidation() { return changedSinceLastValidation; }

        public bool GetLastValidationStatus() { return lastValidationStatus; }


        /// <summary>Tells if values were applied at least once after app startup.</summary>
        public bool GetValuesAppliedOnce() { return valuesAppliedOnce; }

        public void SetValuesAppliedOnce(bool state) { valuesAppliedOnce = state; }



        // FUNCTIONALITY

        /// <summary>
        /// Load the variability model from the given file.<para/>
        /// Uses XmlDocument class provided by .NET Framework:
        /// https://docs.microsoft.com/de-de/dotnet/api/system.xml.xmldocument?view=netframework-4.7.2<para/>
        /// This system only supports the new definition.
        /// There seems to be an old one (using children node) that is outdated at this point (2019).
        /// </summary>
        public bool LoadXML(string filePath) {

            string err_default = "Failed to load variability model from XML";

            // check if the given file exists
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Exists) {
                Debug.LogError(err_default + " - File does not exist!");
                return false;
            }

            if (!fi.Extension.ToLower().Equals(".xml")) {
                Debug.LogError(err_default + " - Only XML files are supported! (given: " + fi.Extension + ")");
                return false;
            }

            // try to load the XML file and instantiate XmlDocument class
            XmlDocument xmlDoc = new XmlDocument();
            try { xmlDoc.Load(filePath); }
            catch (XmlException ex) {
                Debug.LogError(err_default + " - Bad XML document!");
                Debug.LogException(ex);
                return false;
            }

            // get the element that represents the document root
            // https://docs.microsoft.com/de-de/dotnet/api/system.xml.xmldocument.documentelement?view=netframework-4.7.2
            // (can be null of there is none!)
            XmlElement curElement = xmlDoc.DocumentElement;
            if (curElement == null) {
                Debug.LogError(err_default + " - Missing document root!");
                return false;
            }

            // adjust the models name if set in document
            if (curElement.HasAttribute("name")) { name = curElement.GetAttribute("name"); }

            // iterate over all sub-nodes and parse options and constraints
            foreach (XmlElement node in curElement.ChildNodes) {
                
                switch (node.Name) {
                    case "binaryOptions": LoadBinaryOptions(node); break;
                    case "numericOptions": LoadNumericOptions(node); break;
                    case "booleanConstraints": LoadBooleanConstraints(node); break;

                    // ToDo: implement nonBooleanConstraints and mixedConstraints
                    /*
                    case "nonBooleanConstraints": LoadNonBooleanConstraints(node); break;
                    case "mixedConstraints": LoadMixedConstraints(node); break;
                    */
                }
            }


            // DEBUG: log options
            /*
            StringBuilder strb = new StringBuilder("Options: ");
            foreach (string option in optionNames) {
                strb.Append(option);
                strb.Append(", ");
            }
            if (strb[strb.Length-2].Equals(',')) { strb.Remove(strb.Length-2, 2); }
            Debug.Log(strb.ToString());
            */


            // initialize the binary- and numeric options
            Debug.Log("Option initialization...");
            InitOptions();
            Debug.Log("Options initialized.");


            // ToDo: remove if no longer required (currently replaced by "features" in global app config)
            // warn if user did not define any PIM indices
            //if (pimIndices.Count == 0) { Debug.LogWarning("No PIM indices defined!"); }


            // loads the features order from the global app config
            // (required to calculate PIM values)
            LoadFeaturesOrder(out featuresOrder);

            return true;
        }

        /// <summary>
        /// Add a configuration option.
        /// Checks if this option already exists in the model
        /// as well as if the name is valid or not.
        /// </summary>
        /// <returns>True on success, false otherwise</returns>
        private bool AddConfigOption(AFeature option) {

            if (option == null) { return false; }

            // [INDEXING] store array order (even if we don't add it)
            string optionName = option.GetName(); // lower case is done by AFeature constructor
            arrayOrder.Add(optionName);

            // root is added by default
            if (optionName.Equals(ROOT_NAME)) { return true; }

            // check if already contained in any option list
            if (HasOption(optionName)) { return false; }

            // options must have a parent - if not given, the root option will be set
            if (string.IsNullOrEmpty(option.GetParentName())) { option.SetParent(root); }

            // information about "typeof" vs "is"
            // https://stackoverflow.com/questions/184681/is-vs-typeof
            // (using "is" to allow possible future sub-classes of boolean and numeric options)
            if (option is Feature_Boolean) { binaryOptions.Add((Feature_Boolean) option); }
            else if (option is Feature_Range) { numericOptions.Add((Feature_Range) option); }
            else {
                Debug.LogError("Type of option is not supported yet!");
                return false;
            }

            // [INDEXING]
            options.Add(optionName, option);
            int optionIndex = indexToOption.Count;
            indexToOption.Add(optionIndex, optionName);
            optionToIndex.Add(optionName, optionIndex);
            

            // ToDo: remove if no longer required - current replaced by "features" in global app config
            /*
            // add performance influence model array index if set
            uint pimIndex = option.GetPIMIndex();
            if (pimIndex > 0) {
                if (!pimIndices.ContainsKey(pimIndex)) { pimIndices.Add(pimIndex, new List<int>()); }
                else { Debug.LogWarning("More than one option is using PIM index " + pimIndex); }
                pimIndices[pimIndex].Add(optionIndex);
                if (pimIndex > greatestPIMIndex) { greatestPIMIndex = pimIndex; }
            }
            */


            return true;
        }


        /// <summary>
        /// Load binary options/features instances from XML node.
        /// </summary>
        private void LoadBinaryOptions(XmlElement node) {
            foreach (XmlElement entry in node.ChildNodes) {
                Feature_Boolean option = Feature_Boolean.LoadFromXML(entry, this);
                if (!AddConfigOption(option)) {
                    Debug.LogError("Failed to add a binary configuration option! (node: " + entry.Name + ")");
                }
            }
        }

        /// <summary>
        /// Load numeric options/features instances from XML node.
        /// </summary>
        private void LoadNumericOptions(XmlElement node) {
            foreach (XmlElement entry in node.ChildNodes) {
                Feature_Range option = Feature_Range.LoadFromXML(entry, this);
                if (!AddConfigOption(option)) {
                    Debug.LogError("Failed to add a numeric configuration option! (node: " + entry.Name + ")");
                }
            }
        }

        /// <summary>
        /// Load a boolean constraints from XML node.
        /// </summary>
        private void LoadBooleanConstraints(XmlElement node) {
            foreach (XmlElement entry in node.ChildNodes) {
                binaryConstraints.Add(entry.InnerText);
            }
        }


        /// <summary>
        /// Initialize features/options after all the names have been loaded in previous steps.
        /// Sets their instances as reference accordingly.
        /// </summary>
        private void InitOptions() {
            
            foreach (Feature_Boolean o in binaryOptions) { o.Init(); }
            foreach (Feature_Range o in numericOptions) { o.Init(); }

            // update the child parents now that we have instance references set
            foreach (Feature_Boolean o in binaryOptions) { o.UpdateChildrenParent(); }
            foreach (Feature_Range o in numericOptions) { o.UpdateChildrenParent(); }
        }


        /// <summary>
        /// Loads the features order from the global app config.<para/>
        /// A list of strings is defined in the file that contains the feature names to define the order and features to use.<para/>
        /// Returns true on success and false on errors.
        /// </summary>
        private bool LoadFeaturesOrder(out List<int> featuresOrderList) {

            featuresOrderList = null;

            // set features order according to the "features" array in the global app config
            JSON.Serialization.Configuration.JSONGlobalConfig gc = ApplicationLoader.GetInstance().GetConfigurationLoader().GetGlobalConfig();
            if (gc == null || gc.features == null) {
                featuresOrder = null;
                Debug.LogError("Missing \"features\" list in global app config! Can not calculate PIM values.");
                return false;
            }

            // find each according option instance and add it to the list
            featuresOrderList = new List<int>();
            foreach (string optionName in gc.features) {

                int optionIndex = GetOptionIndex(optionName.ToLower(), true);
                if (optionIndex < 1) { // index 0 is root - root can not be used for feature order!
                    Debug.LogError("Failed to load feature order list!\nOption \"" + optionName + "\"" +
                        "found in global app config \"features\" list but not in variability model!");
                    featuresOrderList.Clear();
                    featuresOrderList = null;
                    return false;
                }

                featuresOrderList.Add(optionIndex);
            }

            return true;
        }


        /// <summary>
        /// Calculate the "NFP property" value using the performance influence model.
        /// A region property stores the affect of each feature in an array.
        /// This array will be used together with the current system configuration.
        /// The size of the array is always feature size + 1 - or feature size including the root node.
        /// </summary>
        /// <param name="property">The NFP to calculate the value for</param>
        /// <param name="onlyPositiveValues">Use only positive values from values array</param>
        /// <returns>Returns false if the array size is wrong, true otherwise</returns>
        public bool CalculatePIMValue(RProperty_NFP property, bool onlyPositiveValues = false) {
            
            // set value of property to be invalid and -1
            property.ResetValue();
            float[] regionValueArray = property.GetValues();

            string err_msg = "Failed to calculate NFP PIM value (property: " +
                property.GetName() + ", region: " + property.GetRegion().GetID() + ")";


            // ====> PREVIOUS CODE USING ORDER OF OPTIONS IN XML-DOCUMENT

            /*
            // check for valid array size
            if (regionValueArray.Length != GetOptionCount()) {
                Debug.LogError(err_msg + " - wrong array size: " +
                    regionValueArray.Length + ", should be: " + GetOptionCount());
                return false;
            }
            
            // calculate the new value
            // (array order includes the root at first position)
            // (the base value belongs to the root node)
            float pimValue = 0;
            int index = 0;
            foreach (string featureName in GetArrayOrder()) {
                AFeature feature = options[featureName];
                pimValue += regionValueArray[index++] * feature.GetInfluenceValue();
            }
            */


            // ====> CODE USING GLOBAL APP CONFIG "features" ARRAY

            // do nothing if missing
            if (featuresOrder == null) { return false; }

            // check for valid array size
            // ("base" can not be given in "features" because it is no feature -> add 1 to length)
            if (regionValueArray.Length != featuresOrder.Count + 1) {
                Debug.LogError(err_msg + " - wrong array size: " +
                    regionValueArray.Length + ", should be: " + (featuresOrder.Count + 1));
                return false;
            }

            // calculate new value
            float pimValue = regionValueArray[0]; // get base value
            if (onlyPositiveValues && pimValue < 0) { pimValue = 0; }
            uint posInArray = 1;
            foreach (int optionIndex in featuresOrder) {
                AFeature option = GetOption(optionIndex);
                float val = regionValueArray[posInArray++];
                if (onlyPositiveValues && val < 0) { val = 0; }
                pimValue += val * option.GetInfluenceValue();
            }

            // set value and mark it as valid
            property.SetValue(pimValue);
            return true;
        }


        /// <summary>
        /// Validate the current configuration
        /// represented by the current values of the options/features.
        /// </summary>
        /// <returns>True if the configuration is valid, false otherwise</returns>
        public bool ValidateCurrentConfiguration() {
            return new VariabilityModelValidator(this).IsConfigurationValid();
        }



        // ========== NOTIFICATIONS ========== //

        /// <summary>Called by an option if its value recently changed.</summary>
        public void ValueChangeNotification(AFeature option, float prevValue, float newValue) {

            changedSinceLastValidation = true;

            // notify the UISpawners about this event
            foreach (Spawner.UISpawner s in ApplicationLoader.GetInstance().GetAttachedUISpawners()) {
                s.VariabilityModelConfigChanged();
            }
        }

        /// <summary>Called by the VariabilityModelValidator right after validation finished.</summary>
        public void JustValidatedNotification(bool valid) {

            bool prev_valid = lastValidationStatus;
            lastValidationStatus = valid;
            changedSinceLastValidation = false;

            // notify the UISpawners about this event
            foreach (Spawner.UISpawner s in ApplicationLoader.GetInstance().GetAttachedUISpawners()) {
                s.VariabilityModelValidated(prev_valid, valid);
            }
        }

    }
}
