using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.UI.Config;

namespace VRVis.IO.Features {

    /// <summary>
    /// Unfinished config manager 
    /// </summary>
    public class ConfigManager : MonoBehaviour {

        private static ConfigManager INSTANCE;

        public bool isActive;
        public string defaultConfigPath;
        public string configPath1;
        public string configPath2;

        public GameObject configPanel;

        public Dictionary<string, Configuration> ConfigDict { get; private set; }
        private List<ConfigPanel> configPanels;
        private List<ConfigPanel> comparisonPanels;

        public Configuration DefaultConfig { get; private set; }
        public Configuration Config1 { get; private set; }
        public Configuration Config2 { get; private set; }

        void Awake() {
            if (!isActive) { return; }

            if (!INSTANCE) { INSTANCE = this; }
            else {
                Debug.LogError("There can only be one instance of the ConfigManager!");
                DestroyImmediate(this); // destroy this component instance
                return;
            }

            var vm = ApplicationLoader.GetInstance().GetVariabilityModel();

            if (defaultConfigPath == "") {
                Debug.LogError("Path to default config is an empty string!");
            }

            if (configPath1 == "") {
                Debug.LogError("Path to config1 is an empty string!");
            }

            if (configPath2 == "") {
                Debug.LogError("Path to config2 is an empty string!");
            }

            DefaultConfig = Configuration.LoadFromJson(defaultConfigPath);
            if (DefaultConfig == null) {
                Debug.LogError("Not able to load default config from file!");
            }

            Config1 = Configuration.LoadFromJson(configPath1);
            if (Config1 == null) {
                Debug.LogError("Not able to load config1 from file!");
            }

            Config2 = Configuration.LoadFromJson(configPath2);
            if (Config2 == null) {
                Debug.LogError("Not able to load config2 from file!");
            }

            ConfigDict = new Dictionary<string, Configuration>();
            ConfigDict.Add(DefaultConfig.GetConfigID(), DefaultConfig);
            ConfigDict.Add(Config1.GetConfigID(), Config1);
            ConfigDict.Add(Config2.GetConfigID(), Config2);

        }

        /// <summary>Get the only instance of this class. Can be null if not set yet!</summary>
        public static ConfigManager GetInstance() { return INSTANCE; }
        

        public void AddConfig(Configuration config) {
            ConfigDict.Add(config.GetConfigID(), config);
            NotifySubscribers();
        }

        private int configsToCompare = 0;
        public bool Subscribe(ConfigPanel configPanel) {

            if (configPanel.IsComparisonPanel && configsToCompare >= 2) {
                Debug.LogError("Currently max. 2 config cards can be compared!");
                return false;
            }
            else {
                if (configPanels == null) {
                    configPanels = new List<ConfigPanel>();
                }
                configPanels.Add(configPanel);
                configPanel.ConfigDict = ConfigDict;

                if (configPanel.IsComparisonPanel) {
                    configsToCompare++;
                    if (configsToCompare == 1) {
                        configPanel.SelectedConfigID = Config1.GetConfigID();
                    }
                    else if (configsToCompare == 2) {
                        configPanel.SelectedConfigID = Config2.GetConfigID();
                    }

                    if (comparisonPanels == null) {
                        comparisonPanels = new List<ConfigPanel>();
                    }

                    comparisonPanels.Add(configPanel);
                }
            }          
            
            return true;
        }

        public void TryShowingDifferencesBetweenComparisonConfigPanels() {
            if (configsToCompare == 2) {
                bool readyToShowDifferences = true;

                if (comparisonPanels != null) {

                    foreach (var configPanel in comparisonPanels) {
                        if (!configPanel.FinishedDisplayingConfig) {
                            readyToShowDifferences = false;
                        }
                    }

                    if (readyToShowDifferences) {
                        foreach (var configPanel in comparisonPanels) {
                            if (configPanel.referencePanel != null) {
                                configPanel.ShowDifferenceToReferenceConfig(configPanel.referencePanel.SelectedConfig);
                            }
                        }
                    }
                }
            }
            else {
                Debug.LogWarning("Not able to show differences between comparison config panels, " +
                    "because there is only 1 comparison panel!");
            }            
        }

        private void NotifySubscribers() {
            int count = 0;
            foreach (var subscriber in configPanels) {
                Debug.Log("Subsriber Number: " + count);
                count++;
                subscriber.UpdatePanel();
            }
        }
    }
}
