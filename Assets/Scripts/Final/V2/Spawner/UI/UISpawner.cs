using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.Mappings;
using VRVis.Mappings.Methods;
using VRVis.Mappings.Methods.Base;

namespace VRVis.Spawner {

    /// <summary>
    /// Takes care of spawning UI elements
    /// once the application finished loading.<para/>
    /// Used and called by the ApplicationLoader class so attach it to the same GameObject.
    /// </summary>
    public class UISpawner : MonoBehaviour {

        [Tooltip("Name of this spawner to identify it")]
        public string spawnerName = "MySpawner";

        [Header("Prefabs")]
        [Tooltip("Prefab for an entry of a checkbox button group")]
        public GameObject checkboxPrefab;

        [Tooltip("General prefab for an entry of a radio button group")]
        public GameObject radioPrefab;
        public GameObject radioPrefabNFP; // specific for nfp

        [Tooltip("Prefab for an entry to move right")]
        public GameObject moveRightPrefab;

        [Tooltip("Prefab for an entry to move left")]
        public GameObject moveLeftPrefab;

        [Header("NFP Selection")]
        [Tooltip("Parent element to attach elements to")]
        public Transform nfpGroupParent;
        //public Transform featureGroupParent; // previous version using checkboxes
        //public Transform edgeGroupParent; // previous version using checkboxes

        [Header("Feature Region Selection")]
        [Tooltip("Container for inactive features")]
        public Transform featureContainerInactive;
        public Transform featureContainerActive;

        [Header("Edge Type Selection")]
        public Transform edgeContainerInactive;
        public Transform edgeContainerActive;

        [Header("Variability Model")]
        public TMPro.TMP_Text statusTextValidate;
        public TMPro.TMP_Text statusTextApply;
        public GameObject statusBarValidate;
        public GameObject statusBarApply;


        private ApplicationLoader loader;
        private bool performInitialSpawn = false;
        private bool initialSpawnSuccessful = false;



        // GETTER AND SETTER

        /// <summary>Get the name of this spawner.</summary>
        public string GetName() { return spawnerName; }

        /// <summary>Tells if the last spawn operation was successful.</summary>
        public bool InitialSpawnSuccessful() { return initialSpawnSuccessful; }



        // FUNCTIONALITY

        void Update() {

            if (performInitialSpawn) {
                performInitialSpawn = false;
                initialSpawnSuccessful = PerformInitialSpawn();
            }
        }
        
        /// <summary>
        /// Spawn UI elements on the next Update call.<para/>
        /// Should only be called once after application loaded.
        /// </summary>
        public bool InitialSpawn(ApplicationLoader loader) {
        
            this.loader = loader;
            if (loader == null) {
                Debug.LogError("Invalid application loader!", this);
                return false;
            }

            performInitialSpawn = true;
            initialSpawnSuccessful = false;
            return true;
        }

        /// <summary>
        /// Spawn UI elements on the next Update call.<para/>
        /// Should only be called once after application loaded.<para/>
        /// Returns true if spawning everything was successful.
        /// </summary>
        private bool PerformInitialSpawn() {

            uint state = 0;
            if (SpawnNFPToggleGroup(nfpGroupParent)) { state++; }
            //if (SpawnFeatureGroup(featureGroupParent)) { state++; } // checkbox version
            //if (SpawnEdgeTypeGroup(edgeGroupParent)) { state++; } // checkbox version
            if (SpawnFeatureUI(featureContainerInactive, featureContainerActive)) { state++; }
            if (SpawnEdgeTypeUI(edgeContainerInactive, edgeContainerActive)) { state++; }
            return state == 3;
        }

        /// <summary>
        /// Spawn the toggle group for non functional properties.
        /// </summary>
        private bool SpawnNFPToggleGroup(Transform groupParent) {

            RegionLoader rLoader = loader.GetRegionLoader();
            if (!rLoader.LoadedSuccessful()) { return true; } // nothing to do

            if (!groupParent) {
                Debug.LogWarning("Missing NFP group parent!", this);
                return false;
            }

            // check if this is a radio button group
            ToggleGroup group = groupParent.GetComponent<ToggleGroup>();

            // get correct prefab and validate it
            GameObject prefabToUse = group ? radioPrefabNFP ? radioPrefabNFP : radioPrefab : checkboxPrefab;
            if (!prefabToUse) {
                Debug.LogWarning("Failed to spawn NFP group - Missing " + (group ? "radio" : "checkbox") + " prefab!", this);
                return false;
            }

            string curSelectedNFP = loader.GetAppSettings().GetSelectedNFP();
            ValueMappingsLoader ml = ApplicationLoader.GetInstance().GetMappingsLoader();

            foreach (string nfp in rLoader.GetPropertyNames(RegionProperties.ARProperty.TYPE.NFP)) {

                // spawn an instance of the prefab
                Toggle toggle = SpawnGroupEntry(prefabToUse, groupParent, "NFPToggle_" + nfp, nfp, group);
                if (!toggle) { continue; }

                // set the toggle "on" if this is the currently selected NFP
                if (nfp.Equals(curSelectedNFP)) { toggle.isOn = true; }

                // set color range of default region marking color
                if (ml.HasNFPSetting(nfp)) {
                    NFPSetting nfps = ml.GetNFPSetting(nfp);
                    toggle.gameObject.SendMessage("ChangeImageTexture",
                        nfps.GetColorMethod(Settings.ApplicationSettings.NFP_VIS.CODE_MARKING).CreateTexture2D(64, 64),
                        SendMessageOptions.DontRequireReceiver
                    );
                }

                // add callback listener
                toggle.onValueChanged.AddListener(delegate {
                    if (toggle.isOn) { ApplicationLoader.GetApplicationSettings().SetSelectedNFP(nfp); }
                });
            }

            return true;
        }

        /// <summary>
        /// Spawn a group entry.<para/>
        /// Returns the Toggle component instance if found, null otherwise!
        /// </summary>
        /// <param name="goName">Name of the spawned instance</param>
        /// <param name="text">Toggle text to set</param>
        /// <param name="prefab">The prefab to be instantiated</param>
        /// <param name="group">The toggle group of the parent or null</param>
        private Toggle SpawnGroupEntry(GameObject prefab, Transform groupParent, string goName, string text, ToggleGroup group) {

            GameObject entry = Instantiate(prefab, groupParent);
            entry.name = goName;

            // the message will be used by the "ChangeTextHelper" component if it exists
            entry.SendMessage("ChangeText", text, SendMessageOptions.DontRequireReceiver);

            // find toggle script
            Toggle toggle = entry.GetComponent<Toggle>();
            if (!toggle) { DestroyImmediate(entry); return null; }

            // add toggle to group if there is one
            //ToggleGroup group = groupParent.GetComponent<ToggleGroup>();
            if (group) { toggle.group = group; }

            return toggle;
        }


        /// <summary>
        /// Spawns the features in the according container.<para/>
        /// Also adds event listeners to change the container they are added to.
        /// </summary>
        private bool SpawnFeatureUI(Transform containerInactive, Transform containerActive) {

            RegionLoader rLoader = loader.GetRegionLoader();
            if (!rLoader.LoadedSuccessful()) { return true; } // nothing to do

            ValueMappingsLoader mLoader = loader.GetMappingsLoader();
            if (!mLoader.LoadedSuccessful()) { return true; } // nothing to do

            if (!containerInactive || !containerActive) {
                Debug.LogWarning("A required feature container is not assigned!", this);
                return false;
            }

            List<string> activeFeatures = loader.GetAppSettings().GetActiveFeatures();

            foreach (string feature in rLoader.GetPropertyNames(RegionProperties.ARProperty.TYPE.FEATURE)) {

                bool isActive = activeFeatures.Contains(feature);
                Color color = mLoader.GetFeatureSetting(feature).GetColor();
                CreateFeatureInstance(feature, isActive, color, containerActive, containerInactive);
            }

            return true;
        }

        /// <summary>
        /// Creates UI prefab of the feature that is added to active/inactive container.
        /// </summary>
        private GameObject CreateFeatureInstance(string feature, bool isActive, Color color, Transform conActive, Transform conInactive) {

            // add to right list if active and to left if inactive
            // also use the according prefab type, set the feature name and color
            GameObject prefabToUse = isActive ? moveLeftPrefab : moveRightPrefab;
            Transform containerToUse = isActive ? conActive : conInactive;
            GameObject instance = Instantiate(prefabToUse, containerToUse);

            // send text and color change events
            instance.SendMessage("ChangeText", feature, SendMessageOptions.DontRequireReceiver);
            instance.SendMessage("ChangeColor", color, SendMessageOptions.DontRequireReceiver);

            // find button required for event callback listening
            Button btn = instance.GetComponentInChildren<Button>(false);
            if (!btn) {
                Debug.LogError("Couldn't find button of feature: " + feature);
                return instance;
            }

            // event listener for button clicks
            btn.onClick.AddListener(delegate {

                if (!loader.GetAppSettings().GetActiveFeatures().Contains(feature)) {

                    int state = ApplicationLoader.GetApplicationSettings().AddActiveFeature(feature);
                    if (state != 1) { return; }

                    // destroy current instance
                    Destroy(instance);

                    // create according instance in other container
                    CreateFeatureInstance(feature, true, color, conActive, conInactive);
                }
                else {

                    bool removed = ApplicationLoader.GetApplicationSettings().RemoveActiveFeature(feature);
                    if (!removed) { return; }

                    // destroy current instance
                    Destroy(instance);

                    // create according instance in other container
                    CreateFeatureInstance(feature, false, color, conActive, conInactive);
                }
            });

            return instance;
        }


        /// <summary>
        /// Spawns the edge types in the according container.<para/>
        /// Also adds event listeners to change the container they are added to.
        /// </summary>
        private bool SpawnEdgeTypeUI(Transform containerInactive, Transform containerActive) {

            EdgeLoader eLoader = loader.GetEdgeLoader();
            if (!eLoader.LoadedSuccessful()) { return true; } // nothing to do

            ValueMappingsLoader mLoader = loader.GetMappingsLoader();
            if (!mLoader.LoadedSuccessful()) { return true; } // nothing to do

            if (!containerInactive || !containerActive) {
                Debug.LogWarning("A required feature container is not assigned!", this);
                return false;
            }

            foreach (string edgeType in eLoader.GetEdgeTypes()) {

                // ToDo: maybe show as inactive and user can activate?
                bool hasActiveMapping = ApplicationLoader.GetInstance().GetMappingsLoader().HasEdgeSetting(edgeType);
                if (!hasActiveMapping) { continue; }
                
                bool isActive = ApplicationLoader.GetApplicationSettings().IsEdgeTypeActive(edgeType);
                AColorMethod colMeth = mLoader.GetEdgeSetting(edgeType).GetColorMethod();
                CreateEdgeTypeInstance(edgeType, isActive, colMeth, containerActive, containerInactive);
            }

            return true;
        }

        /// <summary>
        /// Creates UI prefab of the edge type that is added to active/inactive container.
        /// </summary>
        private GameObject CreateEdgeTypeInstance(string edgeType, bool isActive, AColorMethod cm, Transform conActive, Transform conInactive) {

            // add to right list if active and to left if inactive
            // also use the according prefab type, set the feature name and color
            GameObject prefabToUse = isActive ? moveLeftPrefab : moveRightPrefab;
            Transform containerToUse = isActive ? conActive : conInactive;
            GameObject instance = Instantiate(prefabToUse, containerToUse);

            // send text and color change events
            instance.SendMessage("ChangeText", edgeType, SendMessageOptions.DontRequireReceiver);

            // use fixed color if this is such a method
            if (cm is Color_Fixed) {
                Color color = ((Color_Fixed) cm).GetColor();
                instance.SendMessage("ChangeColor", color, SendMessageOptions.DontRequireReceiver);
            }
            else {
                // render a texture from the color scale and use it
                instance.SendMessage("ChangeImageTexture", cm.CreateTexture2D(64, 64), SendMessageOptions.DontRequireReceiver);
            }

            // find button required for event callback listening
            Button btn = instance.GetComponentInChildren<Button>(false);
            if (!btn) {
                Debug.LogError("Couldn't find button of edge type: " + edgeType);
                return instance;
            }

            // event listener for button clicks
            btn.onClick.AddListener(delegate {

                if (!loader.GetAppSettings().IsEdgeTypeActive(edgeType)) {

                    int state = ApplicationLoader.GetApplicationSettings().AddActiveEdgeType(edgeType);
                    if (state != 1) { return; }

                    // destroy current instance
                    Destroy(instance);

                    // create according instance in other container
                    CreateEdgeTypeInstance(edgeType, true, cm, conActive, conInactive);
                }
                else {

                    bool removed = ApplicationLoader.GetApplicationSettings().RemoveActiveEdgeType(edgeType);
                    if (!removed) { return; }

                    // destroy current instance
                    Destroy(instance);

                    // create according instance in other container
                    CreateEdgeTypeInstance(edgeType, false, cm, conActive, conInactive);
                }
            });

            return instance;
        }

        // ===== CODE FOR CHECKBOXES (currently not in use but is working!) ===== //

        /*
        /// <summary>
        /// Spawns the group of feature checkboxes.
        /// </summary>
        private bool SpawnFeatureGroup(Transform groupParent) {

            RegionLoader rLoader = loader.GetRegionLoader();
            if (!rLoader.LoadedSuccessful()) { return true; } // nothing to do

            if (!groupParent) {
                Debug.LogWarning("Missing feature group parent!", this);
                return false;
            }

            // check if this is a radio button group
            ToggleGroup group = groupParent.GetComponent<ToggleGroup>();

            // get correct prefab and validate it
            GameObject prefabToUse = group ? radioPrefab : checkboxPrefab;
            if (!prefabToUse) {
                Debug.LogWarning("Failed to spawn NFP group - Missing " + (group ? "radio" : "checkbox") + " prefab!", this);
                return false;
            }

            List<string> activeFeatures = loader.GetAppSettings().GetActiveFeatures();

            foreach (string feature in rLoader.GetPropertyNames(RegionProperties.ARProperty.TYPE.FEATURE)) {

                // spawn an instance of the prefab
                Toggle toggle = SpawnGroupEntry(prefabToUse, groupParent, "FTToggle_" + feature, feature, group);
                if (!toggle) { continue; }

                // set the toggle "on" if this is a currently active feature
                if (activeFeatures.Contains(feature)) { toggle.isOn = true; }

                // add callback listener
                toggle.onValueChanged.AddListener(delegate {
                    if (toggle.isOn) {
                        int state = ApplicationLoader.GetApplicationSettings().AddActiveFeature(feature);
                        if (state < 1) { toggle.isOn = false; }
                    }
                    else { ApplicationLoader.GetApplicationSettings().RemoveActiveFeature(feature); }
                });
            }

            return true;
        }

        /// <summary>
        /// Spawns the group of edge type checkboxes.
        /// </summary>
        private bool SpawnEdgeTypeGroup(Transform groupParent) {

            EdgeLoader eLoader = loader.GetEdgeLoader();
            if (!eLoader.LoadedSuccessful()) { return true; } // nothing to do

            if (!groupParent) {
                Debug.LogWarning("Missing edge type group parent!", this);
                return false;
            }

            // check if this is a radio button group
            ToggleGroup group = groupParent.GetComponent<ToggleGroup>();

            // get correct prefab and validate it
            GameObject prefabToUse = group ? radioPrefab : checkboxPrefab;
            if (!prefabToUse) {
                Debug.LogWarning("Failed to spawn NFP group - Missing " + (group ? "radio" : "checkbox") + " prefab!", this);
                return false;
            }

            foreach (string edgeType in eLoader.GetEdgeTypes()) {

                // spawn an instance of the prefab
                Toggle toggle = SpawnGroupEntry(prefabToUse, groupParent, "ETToggle_" + edgeType, edgeType, group);
                if (!toggle) { continue; }

                // set the toggle "on" if this is the currently selected NFP
                if (ApplicationLoader.GetApplicationSettings().IsEdgeTypeActive(edgeType)) { toggle.isOn = true; }

                // add callback listener
                toggle.onValueChanged.AddListener(delegate {
                    if (toggle.isOn) {
                        int state = ApplicationLoader.GetApplicationSettings().AddActiveEdgeType(edgeType);
                        if (state < 1) { toggle.isOn = false; }
                    }
                    else { ApplicationLoader.GetApplicationSettings().RemoveActiveEdgeType(edgeType); }
                });
            }


            return true;
        }
        */

        // ======================================================= //


        // UI FEEDBACK SECTION

        /// <summary>
        /// Called by the StructureLoaderUpdater instance when there is
        /// a running recalculation of NFP values over all files and its state changed.
        /// </summary>
        /// <param name="progress">Update progress in range (0-1)</param>
        public void NFPUpdateProcessChanged(float progress, bool updateFailure = false) {

            // crop to range
            progress = progress < 0 ? 0 : progress > 1 ? 1 : progress;

            // change status text accordingly
            if (statusTextApply) {

                if (updateFailure) {
                    statusTextApply.text = "Failure!";
                }
                else if (progress == 1) {
                    statusTextApply.text = "Finished";
                    statusBarApply.SendMessage("ChangeImageColor", VariabilityModel.COLOR_VALID);
                }
                else {
                    float percentage = Mathf.Round(progress * 10000f) / 10000f * 100f;
                    statusTextApply.text = percentage.ToString() + " %";
                }
            }

            // update progress bar
            if (statusBarApply) {

                if (updateFailure) {
                    progress = 1;
                    statusBarApply.SendMessage("ChangeImageColor", VariabilityModel.COLOR_INVALID);
                }

                RectTransform rt = statusBarApply.GetComponent<RectTransform>();
                if (rt) {
                    Vector2 newScale = rt.localScale;
                    newScale.x = progress;
                    rt.localScale = newScale;
                }
            }
        }

        /// <summary>
        /// Called by ChangeUserSelection if the update failed due to invalid configuration.
        /// </summary>
        public void NFPUpdateFailed(string reason) {

            // change status text accordingly
            if (statusTextApply) {
                statusTextApply.text = reason;
            }

            // update progress bar
            if (statusBarApply) {

                RectTransform rt = statusBarApply.GetComponent<RectTransform>();
                if (rt) {
                    Vector2 newScale = rt.localScale;
                    newScale.x = 1;
                    rt.localScale = newScale;
                }

                statusBarApply.SendMessage("ChangeImageColor", VariabilityModel.COLOR_INVALID);
            }
        }

    }
}
