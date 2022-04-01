using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRVis.IO;
using VRVis.IO.Features;

namespace VRVis.UI.Config {

    public class ConfigPanel : MonoBehaviour {

        public GameObject contentArea;
        public GameObject textEntryPrefab;
        public GameObject binaryOptionPrefab;
        public GameObject numericOptionPrefab;
        public GameObject indentationPrefab;
        public ConfigManager configManager;
        public bool IsComparisonPanel;
        public ConfigPanel referencePanel;
        public UnityEvent finishedDisplayingConfig;

        public Dictionary<string, Configuration> ConfigDict { get; set; }        

        private string selectedConfigName;
        public string SelectedConfigName {
            get => selectedConfigName;
            set {
                selectedConfigName = value;
                SelectedConfig = ConfigDict[selectedConfigName];
                UpdatePanel();
            }
        }
        public Configuration SelectedConfig { get; private set; }
        public List<OptionItem> OptionItems { get; private set; }
        public bool FinishedDisplayingConfig { get; private set; }

        private IO.Features.VariabilityModel vm;
        private List<Feature_Boolean> binaryOptions;
        private List<Feature_Range> numericOptions;
        private Dictionary<string, AFeature> options;
        private GameObject configName;

        private bool configVisibiity;

        void Start() {
            if (!configManager.Subscribe(this)) {
                DestroyImmediate(this);
                return;
            }

            vm = ApplicationLoader.GetInstance().GetVariabilityModel();
            binaryOptions = vm.GetBinaryOptions();
            numericOptions = vm.GetNumericOptions();
            options = vm.GetOptionDict();

            configName = transform.Find("ConfigName").gameObject;
            if (configName == null) {
                Debug.LogError("Config panel is probably missing a config name game object!");
            }

            Init();
        }

        public void ShowConfigs(bool show) {
            configVisibiity = show;
        }

        public bool ConfigsVisible() {
            return configVisibiity;
        }

        public void ShowDifferenceToReferenceConfig(Configuration referenceConfig) {
            foreach (var item in OptionItems) {
                float optionValueInReferenceConfig = referenceConfig.GetOptionValue(item.Feature.GetName(), out bool optionExists);

                if (!optionExists) {
                    Debug.LogError("The configuration to be compared to has not an option called '" + item.Feature.GetName() + "'.");
                    return;
                }

                if (item.OptionValue != optionValueInReferenceConfig) {
                    if (item.OptionValue == 0 && optionValueInReferenceConfig == 1) {
                        // make label red
                        item.ChangeColor(OptionItem.OptionColor.turnedOff);
                    }
                    else if (item.OptionValue == 1 && optionValueInReferenceConfig == 0) {
                        // make label green
                        item.ChangeColor(OptionItem.OptionColor.turnedOn);
                    }
                    else if (item.OptionValue == optionValueInReferenceConfig) {
                        item.ChangeColor(OptionItem.OptionColor.standard);
                    }
                }
            }
        }

        public void UpdatePanel() {
            // ConfigDict = configManager.ConfigDict;
            DisplayConfig(SelectedConfigName);
        }

        private void DisplayConfig(string id) {
            var config = ConfigDict[id];
            foreach (var binOption in config.GetActiveBinaryOptions()) {
                Debug.Log(binOption);
            }
            foreach (var numOption in config.GetNumericOptions()) {
                Debug.Log(numOption.Key + ": " + numOption.Value);
            }
        }

        private void Init() {
            // OptionItems list gets filled while building the config tree in BuildConfigTree()
            OptionItems = new List<OptionItem>();
            BuildConfigTree();

            FinishedDisplayingConfig = true;
            finishedDisplayingConfig.Invoke();

            if (IsComparisonPanel) {
                configManager.TryShowingDifferencesBetweenComparisonConfigPanels();
            }
        }

        private void BuildConfigTree() {
            var root = vm.GetRoot();            
            InstantiateOptionItem(root, true, 0);            
        }

        private void InstantiateOptionItem(AFeature feature, bool instatiateAllChildren, int depth) {
            GameObject optionObj = null;

            if (feature is Feature_Boolean) {
                // Instantiate BinaryOption prefab
                optionObj = Instantiate(binaryOptionPrefab, contentArea.transform);
                
            }
            else if (feature is Feature_Range) {
                // Instantiate NumericOption prefab
                optionObj = Instantiate(numericOptionPrefab, contentArea.transform);                
            }

            if (optionObj == null) {
                Debug.LogError("Not able to properly instantiate a suitable prefab for the AFeature!");
                return;
            }

            var optionItem = optionObj.GetComponent<OptionItem>();

            if (optionItem == null) {
                Debug.LogError("Option object is missing an option item component!");
                return;
            }

            for (int i = 0; i < depth; i++) {
                var indentation = Instantiate(indentationPrefab, optionItem.gameObject.transform);
                indentation.transform.SetAsFirstSibling();
            }

            optionItem.Feature = feature;
            optionItem.OptionLabel = feature.GetName();

            float value = SelectedConfig.GetOptionValue(feature.GetName(), out bool optionExists);
            if (optionExists) {
                optionItem.OptionValue = value;
            }
            else {
                Debug.LogError("Not able to get the config value for option '" + feature.GetName() +
                    "', because the config does not contain such an option! Default value is used instead.");
                optionItem.OptionValue = feature.GetValue();
            }

            OptionItems.Add(optionItem);

            if (instatiateAllChildren) {
                foreach (var child in feature.GetChildren()) {
                    InstantiateOptionItem(child, true, depth + 1);
                }
            }
        }
        
    }

}