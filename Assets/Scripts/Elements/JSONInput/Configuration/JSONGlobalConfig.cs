using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization.Configuration {

    /// <summary>
    /// For deserialization of JSON data.<para/>
    /// Holds information about the global application configuration.
    /// </summary>
    [System.Serializable]
    public class JSONGlobalConfig {
        
        public JSONSoftwareSystemConfig software_system;

        /// <summary>
        /// A simple list of feature names that can be found
        /// in the variability model and should be used
        /// in exactly this order to calculate PIM values.
        /// </summary>
        public string[] features;

    }

}
