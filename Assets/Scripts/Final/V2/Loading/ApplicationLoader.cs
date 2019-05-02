using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VRVis.IO.Features;
using VRVis.Settings;
using VRVis.Spawner;
using VRVis.Utilities;

namespace VRVis.IO {

    /// <summary>
    /// Script to load static files on application startup
    /// as well to create code file instances on runtime.<para/>
    /// The static information that will be loaded includes:<para/>
    /// - Global configuration<para/>
    /// - Mappings<para/>
    /// - Regions<para/>
    /// - Features<para/>
    /// - Edges
    /// </summary>
    [RequireComponent(typeof(StructureLoaderUpdater))]
    public class ApplicationLoader : MonoBehaviour {

        private static ApplicationLoader INSTANCE;

        // ToDo: how should this information be given at the end?
        // (maybe given by the global configuration file and only the path to this file is given by the user)

        [Tooltip("Path to main folder that includes all the files to load (required if \"INPUT\" is selected below!)")]
        public string mainPath;
        
        public class StringValue : System.Attribute {
            private readonly string value;

            public StringValue(string value) { this.value = value; }
            public string GetStringValue() { return value; }
        }


        // ==== FOR NOW GET PATH FROM ENUM SELECTION ==== //

        public enum DEFAULT_PATH {
            INPUT,
            ASSETS,
            REPO
            //[StringValue("add/path/here/")]
            //LAB
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

        [Tooltip("Select INPUT to use the string you entered in the \"Main Path\" section.")]
        public DEFAULT_PATH usePath = DEFAULT_PATH.INPUT;
        public string example_folder = "example_8_04022019";

        // ==== FOR NOW GET PATH FROM ENUM SELECTION ==== //

        
        public string configurationName = "app_config.json";
        //public string featuresName = "features.json"; // ToDo: cleanup / remove
        //public string visualPropertiesName = "visual_properties.json"; // ToDo: cleanup / remove
        //public string edgesName = "edges.json"; // ToDo: cleanup / remove
        public string variabilityModelName = "variability_model.xml";

        [Tooltip("Used to spawn the software system structure")]
        public StructureSpawner structureSpawner; // ToDo: remove or disable by default if replaced by v2 (maybe make switch between both)

        [Tooltip("Spawn the first version of the structure (2D)")]
        public bool spawnStructureV1 = false;

        [Tooltip("Used to spawn the software system structure")]
        public StructureSpawnerV2 structureSpawnerV2;

        [Tooltip("Used to spawn code windows representing files")]
        public FileSpawner fileSpawner;

        [Tooltip("Used to spawn variability model hierarchy")]
        public VariabilityModelSpawner varModelSpawner;

        [Tooltip("Used to spawn edges connecting nodes")]
        public CodeWindowEdgeSpawner edgeSpawner;


        [Tooltip("Add the first x found feature regions to the selected list on startup")]
        public bool addFeatureRegions = false;


        // Order of loading is important!
        // 1. global configuration
        // 2. software system structure (important: after global config)
        // 3. feature definitions / variability model loading
        // 4. file regions
        // 5. edge loading
        // 6. visual properties (depends on regions and edges being loaded previously!)
        // 7. mappings (replaces visual properties if ready)
        private AppConfigLoader configLoader;
        private StructureLoader structureLoader;
        private StructureLoaderUpdater structureLoaderUpdater;
        //private FeatureLoader featureLoader;
        private VariabilityModelLoader variabilityModelLoader;
        private RegionLoader regionLoader;
        private EdgeLoader edgeLoader;
        //private VisualPropertiesLoader visPropsLoader; // ToDo: REMOVE if mappings loader is replacing it
        private ValueMappingsLoader mappingsLoader;

        /// <summary>Application settings regarding currently shown data (e.g. selected property..)</summary>
        private readonly ApplicationSettings appSettings = new ApplicationSettings();


        /** Similar to a constructor - this method starts first. */
	    void Awake () {

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


            // ToDo: USE VARIABILITY MODEL INSTEAD AS SOON AS POSSIBLE
            // load features
            //featureLoader = new FeatureLoader(mainPath + featuresName);
            //if (!featureLoader.Load()) {
            //    Debug.LogError("Failed to load features!");
            //}


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


            // load edges from file
            List<string> edgeFiles = EdgeLoader.GetEdgeFiles(mainPath);
            string[] edgeFilePaths = edgeFiles != null ? edgeFiles.ToArray() : new string[]{};
            edgeLoader = new EdgeLoader(edgeFilePaths);
            if (!edgeLoader.Load()) {
                Debug.LogError("Failed to load edges!");
            }


            // ToDo: (cleanup) REMOVE if mappings loader is replacing it
            // load all user-defined visual properties
            //visPropsLoader = new VisualPropertiesLoader(mainPath + visualPropertiesName, regionLoader);
            //if (!visPropsLoader.Load()) {
            //    Debug.LogError("Failed to load visual properties!");
            //}


            // load all user-defined mappings
            List<string> mappingFiles = ValueMappingsLoader.GetMappingFiles(mainPath);
            string[] mappingFilePaths = mappingFiles != null ? mappingFiles.ToArray() : new string[]{};
            mappingsLoader = new ValueMappingsLoader(mappingFilePaths);
            if (!mappingsLoader.Load()) {
                Debug.LogError("Failed to load mappings!");
            }


            // remind to set spawner instances
            if (spawnStructureV1 && !structureSpawner) { Debug.LogError("Missing structure spawner!"); } // ToDo: removed if replaced by v2
            if (!structureSpawnerV2) { Debug.LogError("Missing structure spawner v2!"); }
            if (!fileSpawner) { Debug.LogError("Missing file spawner!"); }
            if (!varModelSpawner) { Debug.LogError("Missing variability model spawner!"); }
            if (!edgeSpawner) { Debug.LogError("Missing edge spawner!"); }


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

            // ToDo: removed if replaced by v2
            // spawn the software system structure
            if (spawnStructureV1 && structureSpawner) {
                if (regionLoader.LoadedSuccessful()) {
                    structureSpawner.SetRootNode(structureLoader.GetRootNode());
                    if (structureSpawner.SpawnStructure()) {
                        Debug.Log("Software system structure spawned");
                    }
                }
                else {
                    Debug.LogError("Failed to spawn structure because regions are not loaded!");
                }
            }
            
            // spawn the software system structure
            if (structureSpawnerV2) {
                if (regionLoader.LoadedSuccessful()) {
                    structureSpawnerV2.SetRootNode(structureLoader.GetRootNode());
                    if (structureSpawnerV2.SpawnStructure()) {
                        Debug.Log("Software system structure spawned by spawner v2");
                    }
                }
                else {
                    Debug.LogError("Failed to spawn structure because regions are not loaded!");
                }
            }


            // spawn variability model structure
            if (varModelSpawner) {
                if (variabilityModelLoader.LoadedSuccessful()) {
                    varModelSpawner.Spawn(GetVariabilityModel());
                    Debug.Log("Variability model hierarchy spawned.");
                }
                else {
                    Debug.LogError("Failed to spawn variability model hierarchy because model was not loaded!");
                }
            }


            // prepare user interface
            UISpawner[] uiSpawner = GetComponents<UISpawner>();
            foreach (UISpawner spawner in uiSpawner) { spawner.InitialSpawn(this); }


            // update the nfp values for the first time (initial update)
            UpdateNFPValues(true);
            

            // ToDo: DEBUG - remove if no longer required
            //Debug.Log(VariabilityModelLoader.GetModelHierarchyRecursivelyJSON(GetVariabilityModel().GetRoot()));
        }



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

        //public FeatureLoader GetFeatureLoader() { return featureLoader; } // ToDo: remove if no longer required

        public VariabilityModelLoader GetVariabilityModelLoader() { return variabilityModelLoader; }

        public VariabilityModel GetVariabilityModel() { return variabilityModelLoader.GetModel(); }

        public RegionLoader GetRegionLoader() { return regionLoader; }

        public EdgeLoader GetEdgeLoader() { return edgeLoader; }

        //public VisualPropertiesLoader GetVisualPropertiesLoader() { return visPropsLoader; } // ToDo: remove if no longer required

        public ValueMappingsLoader GetMappingsLoader() { return mappingsLoader; }


        public FileSpawner GetFileSpawner() { return fileSpawner; }

        public CodeWindowEdgeSpawner GetEdgeSpawner() { return edgeSpawner; }

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

    }
}
