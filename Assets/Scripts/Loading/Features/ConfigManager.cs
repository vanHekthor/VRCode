using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.IO.Features {

    /// <summary>
    /// Unfinished config manager 
    /// </summary>
    public class ConfigManager : MonoBehaviour {

        private static ConfigManager INSTANCE;

        public string configPath1;
        public string configPath2;

        private List<Configuration> configurations;

        public Configuration Config1 { get; private set; }
        public Configuration Config2 { get; private set; }

        void Awake() {
            if (!INSTANCE) { INSTANCE = this; }
            else {
                Debug.LogError("There can only be one instance of the ConfigManager!");
                DestroyImmediate(this); // destroy this component instance
                return;
            }
        }

        void Start() {
            var vm = ApplicationLoader.GetInstance().GetVariabilityModel();

            if (configPath1 == "") {
                Debug.LogError("Path to config1 is an empty string!");
            }

            if (configPath2 == "") {
                Debug.LogError("Path to config2 is an empty string!");
            }

            Config1 = Configuration.LoadFromJson(configPath1);
            if (Config1 == null) {
                Debug.LogError("Not able to load config1 from file!");
            }

            Config2 = Configuration.LoadFromJson(configPath2);
            if (Config2 == null) {
                Debug.LogError("Not able to load config2 from file!");
            }

            // var defaultConfig = new Configuration(vm.GetBinaryOptions(), vm.GetNumericOptions());
            // configurations.Add(defaultConfig);
        }

        /// <summary>Get the only instance of this class. Can be null if not set yet!</summary>
        public static ConfigManager GetInstance() { return INSTANCE; }
    }
}
