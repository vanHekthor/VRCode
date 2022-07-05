using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRVis.JSON.Serialization.Configuration {

    /// <summary>
    /// For deserialization of JSON data.<para/>
    /// Holds information about the software system configuration.
    /// </summary>
    [System.Serializable]
    public class JSONSoftwareSystemConfig {
        
	    public string path;
        public string root_folder;
        public int max_folder_depth;
        public string[] ignore_files;
        public string[] remove_extensions;
        public string main_method = "";

    }

}
