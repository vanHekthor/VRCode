using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VRVis.IO.Features;
using VRVis.Settings;
using VRVis.Spawner;
using VRVis.Testing.Interaction;
using VRVis.Utilities;
using VRVis.Utilities.Glimps;

namespace VRVis.IO {

    /// <summary>
    /// This is the framework's startup script.<para/>
    /// It provides possibilities to configure and load resources.<para/>
    /// 
    /// This is version 2, created at June 11, 2019.<para/>
    /// Last Update: June 11, 2019
    /// </summary>
    [RequireComponent(typeof(StructureLoaderUpdater))]
    public class ApplicationLoader : MonoBehaviour {

        private static ApplicationLoader INSTANCE;

        // ToDo: how should this information be given in future?
        // (maybe through global configuration file and only the path to this file is given by the user)

        [Tooltip("Path to main folder that includes all the files to load (required if \"INPUT\" is selected below!)")]
        public string mainPath;

        public string configurationName = "app_config.json";
        public string variabilityModelName = "variability_model.xml";
        public string influenceModelName = "model.csv";
        public string configsFolderName = "configs";

        [Tooltip("Add the first x found feature regions to the selected list on startup")]
        public bool addFeatureRegions = false;

        public bool validateAndEvaluateOnStartUp;
        public bool activateAllEdgeLinksOnStartUp;
        public bool showPropertyInCodeOnStartUp;
        public bool propertyValueRangeWithZero;


        // --------------------------------------------------------------------
        // The path is currently based on the enumeration selection.

        public enum DEFAULT_PATH {
            INPUT,
            ASSETS,
            REPO
            //[StringValue("add/path/here/")] // this is an example for how to use StringValue
            //ENUM_ENTRY_NAME
        }

        [Tooltip("Select INPUT to use the string you entered in the \"Main Path\" section.")]
        public DEFAULT_PATH usePath = DEFAULT_PATH.INPUT;
        public string example_folder = "example_8_04022019";

        [Tooltip("A list of public registered spawners. Do not add \"sub-spawners\" to it.")]
        public SpawnerEntry[] spawners;

        public ChangeUserSelection terminalInputController;

        // --------------------------------------------------------------------
        // Loaders to get resources from disk, required for visualizations.

        // Order of loading is important!
        // 1. global configuration
        // 2. software system structure (important: after global config)
        // 3. feature definitions / variability model loading
        // 4. file regions
        // 5. edge loading
        // --6. visual properties (depends on regions and edges being loaded previously!)-- no longer used (replaced by mappings)
        // 7. mappings (replaces visual properties)
        private AppConfigLoader configLoader;
        private StructureLoader structureLoader;
        private StructureLoaderUpdater structureLoaderUpdater;
        private VariabilityModelLoader variabilityModelLoader;
        private InfluenceModelLoader influenceModelLoader;
        private RegionLoader regionLoader;
        private EdgeLoader edgeLoader;
        private ValueMappingsLoader mappingsLoader;
        private GlimpsChopsLoader chopsLoader;

        /// <summary>Application settings regarding currently shown data (e.g. selected property..)</summary>
        private readonly ApplicationSettings appSettings = new ApplicationSettings();


        // --------------------------------------------------------------------
        // Method section starts here. Awake is similar to a constructor call.

        void Awake () {

            // ensure that there is only a single instance of the application loader.
            if (!INSTANCE) { INSTANCE = this; }
            else {
                Debug.LogError("There can only be one instance of the ApplicationLoader!");
                DestroyImmediate(this); // destroy this component instance
                return;
            }

           // log where the persistent data will be stored (e.g. user data)
            Debug.Log("Persistent data path: " + Application.persistentDataPath);

            // for now: get path from the user enum selection
            string pathToUse = GetStringValue(usePath);

            // ensure path consists of slash and no backslash and ends with a slash
            mainPath = Utility.GetFormattedPath(pathToUse);
            if (!mainPath.EndsWith("/")) { mainPath += "/"; }
            Debug.Log("Loading from main path: " + mainPath);


            // load global configurations
            configLoader = new AppConfigLoader(mainPath + configurationName);
            if (!configLoader.Load()) {
                Debug.LogError("Failed to load configuration!");
            }


            // load software system structure and prepare updater
            InitStructureLoader(configLoader);


            // load variability model
            variabilityModelLoader = new VariabilityModelLoader(mainPath + variabilityModelName);
            if (!variabilityModelLoader.Load()) {
                Debug.LogError("Failed to load variability model!");
            }

                       
            // load regions from file
            List<string> regionFiles = RegionLoader.GetRegionFiles(mainPath);
            string[] regionFilePaths = regionFiles != null ? regionFiles.ToArray() : new string[]{};
            regionLoader = new RegionLoader(regionFilePaths);
            if (!regionLoader.Load()) {
                Debug.LogError("Failed to load regions!");
            }

            // load influence model
            influenceModelLoader = new InfluenceModelLoader(mainPath + influenceModelName, variabilityModelLoader.GetModel().GetOptions());
            if (!influenceModelLoader.Load()) {
                Debug.LogError("Failed to load influence model!");
            }

            // load edges from file
            List<string> edgeFiles = EdgeLoader.GetEdgeFiles(mainPath + configsFolderName);
            string[] edgeFilePaths = edgeFiles != null ? edgeFiles.ToArray() : new string[]{};
            edgeLoader = new EdgeLoader(edgeFilePaths);
            if (!edgeLoader.Load()) {
                Debug.LogError("Failed to load edges!");
            }


            // load all user-defined mappings
            List<string> mappingFiles = ValueMappingsLoader.GetMappingFiles(mainPath);
            string[] mappingFilePaths = mappingFiles != null ? mappingFiles.ToArray() : new string[]{};
            mappingsLoader = new ValueMappingsLoader(mappingFilePaths);
            if (!mappingsLoader.Load()) {
                Debug.LogError("Failed to load mappings!");
            }

            // load chops data from glimps
            chopsLoader = new GlimpsChopsLoader();
            chopsLoader.LoadChops();

            // add default active features loaded by region loader
            if (addFeatureRegions && regionLoader.LoadedSuccessful()) {

                uint alreadyAdded = 0;
                int ftCount = GetAppSettings().MAX_ACTIVE_FEATURES;

                foreach (string ftName in regionLoader.GetPropertyNames(RegionProperties.ARProperty.TYPE.FEATURE)) {
                    int status = GetAppSettings().AddActiveFeature(ftName);
                    if (status == 0) { break; }
                    if (status == 1 && ++alreadyAdded == ftCount) { break; }
                }

                Debug.Log("Added first " + alreadyAdded + " features as active ones [" + string.Join(", ", GetAppSettings().GetActiveFeatures().ToArray()) + "]");
            }
        }


        private void Start() {

            // Execute spawners that should run on startup.
            foreach (SpawnerEntry e in spawners) {
                if (!e.executeOnStartup || e.spawner == null) { continue; }
                Debug.Log("Executing spawner: " + e.name);
                e.spawner.SpawnVisualization();
                e.spawner.ShowVisualization(!e.hideAfterSpawn);
            }


            // TODO: see if we can get this running with the same system as above?
            // prepare user interface
            UISpawner[] uiSpawner = GetComponents<UISpawner>();
            foreach (UISpawner spawner in uiSpawner) { spawner.InitialSpawn(this); }


            // update the nfp values for the first time (initial update)
            if (validateAndEvaluateOnStartUp) {
                UpdateNFPValues(true);

                if (GetApplicationSettings().IsFeatureModelValid()) {
                    terminalInputController.ApplyVariabilityModelConfiguration(null);
                }
                if (showPropertyInCodeOnStartUp) {
                    terminalInputController.ShowNFPRegionMarking(true);
                }
            }


            // activate all edge types which means each edge gets indicated and can be displayed
            // by a link button inside the code windows at the corresponding code lines
            if (activateAllEdgeLinksOnStartUp) {
                foreach (string edgeType in edgeLoader.GetEdgeTypes()) {
                    GetApplicationSettings().AddActiveEdgeType(edgeType);
                }
            }

        }

        
        // --------------------------------------------------------------------
        // GETTER AND SETTER
        
        public static ApplicationLoader GetInstance() { return INSTANCE; }
        public static ApplicationSettings GetApplicationSettings() {
            if (GetInstance()) { return GetInstance().GetAppSettings(); }
            return null;
        }

        public ApplicationSettings GetAppSettings() { return appSettings; }

        public AppConfigLoader GetConfigurationLoader() { return configLoader; }

        public StructureLoader GetStructureLoader() { return structureLoader; }

        public StructureLoaderUpdater GetStructureLoaderUpdater() { return structureLoaderUpdater; }

        public VariabilityModelLoader GetVariabilityModelLoader() { return variabilityModelLoader; }

        public VariabilityModel GetVariabilityModel() { return variabilityModelLoader.GetModel(); }

        public InfluenceModelLoader GetInfluenceModelLoader() { return influenceModelLoader;  }

        public InfluenceModel GetInfluenceModel() { return influenceModelLoader.Model; }

        public RegionLoader GetRegionLoader() { return regionLoader; }

        public EdgeLoader GetEdgeLoader() { return edgeLoader; }

        public ValueMappingsLoader GetMappingsLoader() { return mappingsLoader; }

        public GlimpsChopsLoader GetChopsLoader() { return chopsLoader; }

        // SPAWNER

        /// <summary>
        /// Returns the according spawner when the name is given or null.<para/>
        /// The name wont be modified so that the case matters!<para/>
        /// Only spawner names listed in the component are supported.
        /// </summary>
        public ASpawner GetSpawner(string name) {

            ASpawner applies = null;

            foreach (SpawnerEntry e in spawners) {
                if (!e.name.Equals(name)) { continue; }
                applies = e.spawner;
                break;
            }

            return applies;
        }

        // ToDo: see if we can get this running with the same system as for the other spawners
        public UISpawner[] GetAttachedUISpawners() { return GetComponents<UISpawner>(); }



        // FUNCTIONALITY

        /// <summary>
        /// Initialize the structure loader.<para/>
        /// The parameter is given only to ensure the conig is loaded previously.
        /// </summary>
        private void InitStructureLoader(AppConfigLoader configLoader) {

            // create new structure loader instance
            structureLoader = new StructureLoader();
            string err_base = "Failed to apply software system configuration to structure loader";

            // apply global configuration before starting loading procedure
            if (!configLoader.LoadedSuccessful()) {
                Debug.LogWarning(err_base + " (config not loaded)!");
                return;
            }

            // check existance of configuration
            if (configLoader.GetGlobalConfig().software_system == null) {
                Debug.LogWarning(err_base + " (software system not configured)!");
                return;
            }

            // apply according configuration to structure loader
            if (!structureLoader.ApplyConfiguration(configLoader.GetGlobalConfig().software_system, mainPath + configurationName)) {
                Debug.LogError("Applying software system configuraiton failed!");
                return;
            }
            Debug.Log("Applied software system configuration");

            // try to load software system structure
            if (!structureLoader.Load()) {
                Debug.LogError("Failed to load software system structure!");
                return;
            }

            // set structure loader reference in updater
            structureLoaderUpdater = GetComponent<StructureLoaderUpdater>();
            if (!structureLoaderUpdater) { Debug.LogError("Missing StructureLoaderUpdater component!"); return; }
            structureLoaderUpdater.SetStructureLoader(structureLoader);
        }

        /// <summary>
        /// Uses the StructureLoaderUpdater to update NFP values of all regions.<para/>
        /// Will only start one update procedure and wait until it is finished.
        /// So calling this method multiple times in a row will only result in one running procedure.
        /// </summary>
        public bool UpdateNFPValues(bool stopRunningUpdate) {

            if (!structureLoaderUpdater) {
                Debug.LogError("Failed to update NFP values! - Missing StructureLoaderUpdater!");
                return false;
            }

            return structureLoaderUpdater.UpdateNFPValues(false);
        }



        // --------------------------------------------------------------------
        // New spawner management. A spawner equals a visualization.

        [System.Serializable]
        public class SpawnerEntry {

            public string name;
            public ASpawner spawner;
            public string info;

            [Tooltip("Execute spawner with application startup")]
            public bool executeOnStartup = false;

            [Tooltip("Hide spawned elements")]
            public bool hideAfterSpawn = false;
        }
        

        // --------------------------------------------------------------------
        // Following is required to "annotate" enumeration entries.

        public class StringValue : System.Attribute {

            private readonly string value;

            public StringValue(string value) { this.value = value; }
            public string GetStringValue() { return value; }
        }

        public string GetStringValue(DEFAULT_PATH pathType) {

            if (pathType == DEFAULT_PATH.INPUT) { return mainPath; }

            string prePath = "";
            if (pathType == DEFAULT_PATH.ASSETS) { prePath += Application.dataPath; }
            else if (pathType == DEFAULT_PATH.REPO) { prePath += Application.dataPath + "/../"; }

            FieldInfo fieldInf = pathType.GetType().GetField(pathType.ToString());
            StringValue[] strVal = fieldInf.GetCustomAttributes(typeof(StringValue), false) as StringValue[];
            if (strVal.Length > 0) { prePath += strVal[0].GetStringValue(); }

            if (prePath.Length > 0) {
                if (!prePath.EndsWith("/") && !prePath.EndsWith("\\")) { prePath += "/"; }
                return prePath + example_folder;
            }

            return "";
        }

    }
}
