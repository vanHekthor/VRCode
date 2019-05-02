using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.JSON.Serialization.Configuration;


namespace VRVis.IO {

    /// <summary>
    /// Loads the global application configuration file.<para/>
    /// This file holds information about things like the accuracy of bezier curves.<para/>
    /// It can help optimizing the user experience regarding performance and other general topics.
    /// </summary>
    public class AppConfigLoader : FileLoader {

        private JSONGlobalConfig globalConfig;


	    // CONSTRUCTOR

        public AppConfigLoader(string filePath) 
        : base(filePath) {
            // ...
        }


        // GETTER AND SETTER
        
        public JSONGlobalConfig GetGlobalConfig() { return globalConfig; }


        // FUNCTIONALITY

        public override bool Load() {
            
            loadingSuccessful = false;

            if (!File.Exists(GetFilePath())) {
                Debug.LogError("Failed to load application configuration (file does not exist)!");
                return false;
            }

            // deserialize the JSON data to objects
            Debug.Log("Loading application configuration from JSON file...");
            string jsonData = File.ReadAllText(GetFilePath());
            globalConfig = JsonUtility.FromJson<JSONGlobalConfig>(jsonData);

            // check if main variable is initialized correctly
            if (globalConfig == null) {
                Debug.LogError("Failed to initialize global configuration (null)!");
                return false;
            }

            Debug.Log("Loading application configuration finished successfully.");
            loadingSuccessful = true;
            return true;

        }

    }
}
