using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRVis.Elements;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.RegionProperties;
using VRVis.Settings;
using VRVis.Spawner.File;
using VRVis.Spawner.Regions;

namespace VRVis.Spawner {

    /// <summary>
    /// Update: 12.06.2019 - refactoring: "child of FileSpawner"<para/>
    /// An instance of this spawner takes care of creating the regions in a CodeWindow.<para/>
    /// There is a total of 3 visualizations created through this spawner:<para/>
    /// - nfp region marking<para/>
    /// - nfp heightmap<para/>
    /// - feature regions<para/>
    /// An instance of this class is "owned" by the file spawner,
    /// so make sure that the according component is attached to the same object.
    /// </summary>
    public class RegionSpawner : ASpawner {

        private static readonly bool LOGGING = false;
        private static RegionSpawner INSTANCE;

        [Tooltip("Prefab for code marking regions")]
        public GameObject regionPrefab;
        
        [Tooltip("Prefab for heightmap regions")]
        public GameObject regionPrefabHeightmap;

        [Tooltip("Prefab for feature regions")]
        public GameObject featurePrefab; // in previous version (v2 and v2_large: 20) now: 96

        [Tooltip("Maximum width available in the feature bar")]
        public float featureWidth;

        // pixel error per text-element to consider for region creation
        [Tooltip("Error correction value for multiple text instances")]
        public float ERROR_PER_ELEMENT = 0.2f; // 0.2 seems to be a good value for font-size 8-14

        // callbacks for when the regions were modified (e.g. used by overview)
        [HideInInspector]
        public CodeFileEvent onRegionsValuesChanged = new CodeFileEvent();
        public class CodeFileEvent : UnityEvent<CodeFileReferences> {}

        private string activeNFP; // stores active property temporarily
        private CodeFile currentFile; // regions currently spawned for this file

        /// <summary>
        /// Stores the spawned regions for each CodeFile (modified by the RegionSpawner)<para/>
        /// 2. dict: key = region ID (to ensure uniqueness) and value = Region instance
        /// </summary>
        private Dictionary<CodeFileReferences, Dictionary<string, Region>> spawnedRegions = new Dictionary<CodeFileReferences, Dictionary<string, Region>>();


        // kinda like the class constructor
        private void Awake() {
            
            if (!INSTANCE) { INSTANCE = this; }
            else {
                Debug.LogError("There can only be one instance of RegionSpawner!");
                DestroyImmediate(this);
                return;
            }
            
        }


        // GETTER AND SETTER

        public GameObject GetRegionPrefab() { return regionPrefab; }
        public GameObject GetRegionPrefabHeightmap() { return regionPrefabHeightmap; }
        public GameObject GetFeaturePrefab() { return featurePrefab; }

        public float GetErrorPerElement() { return ERROR_PER_ELEMENT; }


        // FUNCTIONALITY

        /// <summary>
        /// Used to set the position for in-text, feature and heightmap visualization.
        /// This is to use the same calculation and ensure same positions.
        /// Returns false on errors (e.g. if RectTransform is missing).
        /// </summary>
        /// <param name="lineHeight">Height of a single line</param>
        /// <param name="width">Maximum width</param>
        /// <param name="scaleX">Heightmap scale</param>
        private bool SetRegionPositionAndSize(GameObject regionObject, CodeFileReferences fileRefs, Region.Section section, float lineHeight, float width, float xPos, float scaleX) {
            
            // pixel error calculation (caused by multiple text-elements)
            float curLinePos = section.start / (float) fileRefs.GetLinesTotal();
            float pxErr = curLinePos * ((float) fileRefs.GetTextElements().Count-1) * GetErrorPerElement();

            // scale and position region
            float x = xPos;
            float y = (section.start - 1) * -lineHeight + pxErr; // lineHeight needs to be a negative value!
            float finalWidth = width;
            float finalHeight = (section.end - section.start + 1) * lineHeight;
                    
            RectTransform rt = regionObject.GetComponent<RectTransform>();
            if (!rt) { Debug.LogWarning("Could not find rect transform on region instance!"); return false; }
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(finalWidth, finalHeight);

            // adjust scaling (done for height map)
            Vector3 localScale = rt.localScale;
            localScale.x = scaleX;
            rt.localScale = localScale;
            return true;
        }

        private bool SetRegionPositionAndSize(GameObject regionObject, CodeFileReferences fileRefs, Region.Section section, float height, float width, float xPos) {
            return SetRegionPositionAndSize(regionObject, fileRefs, section, height, width, xPos, 1);
        }


        /// <summary>Remove all NFP region objects of a file.</summary>
        public static void CleanupNFPRegions(CodeFileReferences fileInstance) {

            List<GameObject> elementsToDestroy = new List<GameObject>();

            // remove possible old region elements of "code" marking visualization
            Transform regionContainer = fileInstance.regionContainer;
            if (regionContainer) {
                // (works while iterating because Destroy is not running immediately)
                foreach (Transform child in regionContainer) {
                    if (child != regionContainer) { elementsToDestroy.Add(child.gameObject); }
                }
            }

            // remove possible old region elements of "heightmap" visualization
            Transform heightmapContainer = fileInstance.heightmapRegionContainer;
            if (heightmapContainer) {
                // (works while iterating because Destroy is not running immediately)
                foreach (Transform child in heightmapContainer) {
                    if (child != heightmapContainer) { elementsToDestroy.Add(child.gameObject); }
                }
            }

            // destroy elements now
            foreach (GameObject element in elementsToDestroy) {
                if (!element) { continue; }
                DestroyImmediate(element);
            }
        }


        /// <summary>
        /// Spawn non function property regions
        /// according to selected type of visualization.<para/>
        /// This will just spawn the NFP regions but not coloring it.<para/>
        /// Coloring should be done in a separate step because it can be
        /// influence by "visual properties".
        /// </summary>
        public bool SpawnNFPRegions(CodeFileReferences fileInstance) {

            currentFile = fileInstance.GetCodeFile();    

            // remove old regions objects before spawning new ones
            CleanupNFPRegions(fileInstance);


            // check if variability model is used and valid (if invalid, do not spawn any regions)
            VariabilityModelLoader vml = ApplicationLoader.GetInstance().GetVariabilityModelLoader();
            if (vml != null && vml.LoadedSuccessful()) {
                string reason;
                if (!vml.IsModelValidAndUsed(vml.GetModel(), out reason)) {
                    Debug.LogWarning("Failed to spawn NFP regions! - " + reason);
                    return false;
                }
            }


            if (fileInstance.GetTextElements().Count == 0) {
                Debug.LogWarning("Failed to spawn NFP regions! No text elements found.");
                return false;
            }

            float lineHeight = fileInstance.GetCodeFile().GetLineInfo().lineHeight;
            float totalWidth_codeMarking = fileInstance.GetCodeFile().GetLineInfo().lineWidth;

            // adjust width or height depending on visualization type
            ApplicationSettings appSettings = ApplicationLoader.GetApplicationSettings();
            if (appSettings.IsNFPVisActive(ApplicationSettings.NFP_VIS.CODE_MARKING)) {

                //// get region width for code marking visualization (might change in future)
                //RectTransform scrollRectRT = fileInstance.GetScrollRect().GetComponent<RectTransform>();
                //RectTransform textContainerRT = fileInstance.textContainer.GetComponent<RectTransform>();
                //RectTransform vertScrollbarRT = fileInstance.GetVerticalScrollbarRect();
                //if (scrollRectRT && textContainerRT && vertScrollbarRT) {
                //    totalWidth_codeMarking = scrollRectRT.sizeDelta.x - textContainerRT.anchoredPosition.x - Mathf.Abs(vertScrollbarRT.sizeDelta.x) - 5;
                //}
            }


            // check height and width values
            if (lineHeight == 0) {
                Debug.LogWarning("Failed to spawn regions! Line height is zero!");
                return false;
            }

            if (appSettings.IsNFPVisActive(ApplicationSettings.NFP_VIS.CODE_MARKING) && totalWidth_codeMarking == 0) {
                Debug.LogWarning("Failed to spawn regions! Code marking width is zero!");
                return false;
            }

            // [18.03.2019] Heightmap width no longer set
            /*
            if (appSettings.IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP) && heightMapWidth == 0) {
                Debug.LogWarning("Failed to spawn regions! Height map width is zero!");
                return false;
            }
            */
            //Debug.Log("Region line height: " + lineHeight + ", width: " + totalWidth);


            // check if prefab is set
            if (GetRegionPrefab() == null) {
                Debug.LogError("Failed to spawn regions - NFP region prefab is missing!");
                return false;
            }

            // use file path and property selection to only spawn regions of interest
            List<Region> regionsToSpawn = new List<Region>();

            // get the currently selected property to show
            activeNFP = ApplicationLoader.GetInstance().GetAppSettings().GetSelectedNFP().ToLower();

            // use loader to find regions of that file
            RegionLoader regionLoader = ApplicationLoader.GetInstance().GetRegionLoader();
            foreach (Region region in regionLoader.GetFileRegions(fileInstance.GetCodeFile().GetNode().GetPath())) {
                if (region.HasProperty(ARProperty.TYPE.NFP, activeNFP)) {
                    regionsToSpawn.Add(region);
                }
            }
            if (LOGGING) { Debug.Log("NFP regions to spawn: " + regionsToSpawn.Count); }

            // spawn the regions
            List<Region> regionsSpawned = new List<Region>();
            foreach (Region region in regionsToSpawn) {

                // success if at least one section was spawned
                int sectionsSpawned = 0;

                foreach (Region.Section section in region.GetSections()) {
                    
                    // depending on the current state of the visualization "switch"
                    // show the regions marking the code text or show the height map
                    CodeFileReferences fileRefs = fileInstance;

                    // depending on visualization type, create the regions
                    if (appSettings.IsNFPVisActive(ApplicationSettings.NFP_VIS.CODE_MARKING)) {
                        sectionsSpawned += CreateNFPRegion_InText(region, section, fileRefs, lineHeight, totalWidth_codeMarking) ? 1 : 0;
                    }

                    if (appSettings.IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP)) {
                        sectionsSpawned += CreateNFPRegion_HeightMap(region, section, fileRefs, lineHeight) ? 1 : 0;
                    }
                }

                // store this region for later usage
                if (sectionsSpawned > 0) { regionsSpawned.Add(region); }
            }

            // add all the spawned NFP regions to the CodeFile instance as reference
            // (this reference is required to color or scale them)
            AddSpawnedRegions(fileInstance, regionsSpawned);
            if (LOGGING) { Debug.Log("NFP regions spawned: " + regionsSpawned.Count); }

            return true;
        }


        /// <summary>
        /// Create region for NFP visualization marking the code text.<para/>
        /// Returns true if successful, false otherwise.
        /// </summary>
        private bool CreateNFPRegion_InText(Region region, Region.Section section, CodeFileReferences fileRefs, float lineHeight, float totalWidth) {

            // (! IMPORTANT STEP !) create and set GameObject reference
            // The access to the GameObjects is required to color or rescale them
            GameObject regionObj = Instantiate(GetRegionPrefab());
            RegionGameObject info = regionObj.GetComponent<RegionGameObject>();
            if (!info) {
                Debug.LogError("Failed to spawn region of code marking - Prefab is missing RegionGameObject component!");
                return false;
            }
            ARProperty property = region.GetProperty(ARProperty.TYPE.NFP, activeNFP);
            info.SetInfo(currentFile, region, property);
            info.SetNFPVisType(ApplicationSettings.NFP_VIS.CODE_MARKING);

            // check if region container is given and if so, attach the region to it
            Transform regionContainer = null;
            if (fileRefs) { regionContainer = fileRefs.regionContainer; }
            if (!regionContainer) {
                Debug.LogError("Failed to spawn region \"in-text marker\" because container is missing!");
                region.RemoveUIGameObject(regionObj); // remove from list
                DestroyImmediate(regionObj);
                return false;
            }

            // register region object
            region.AddUIGameObject(ARProperty.TYPE.NFP, regionObj);

            // parent new object to the region container
            regionObj.transform.SetParent(regionContainer, false);

            // set position and scale
            SetRegionPositionAndSize(regionObj, fileRefs, section, lineHeight, totalWidth, 0);
            return true;
        }


        /// <summary>
        /// Create region for NFP visualization marking the code text.<para/>
        /// Returns true if successful, false otherwise.
        /// </summary>
        private bool CreateNFPRegion_HeightMap(Region region, Region.Section section, CodeFileReferences fileRefs, float lineHeight) {

            // (! IMPORTANT STEP !) create and set GameObject reference
            // The access to the GameObjects is required to color or rescale them
            GameObject regionObj = Instantiate(GetRegionPrefabHeightmap());
            RegionGameObject info = regionObj.GetComponent<RegionGameObject>();
            if (!info) {
                Debug.LogError("Failed to spawn region of heightmap - Prefab is missing RegionGameObject component!");
                return false;
            }
            ARProperty property = region.GetProperty(ARProperty.TYPE.NFP, activeNFP);
            info.SetInfo(currentFile, region, property);
            info.SetNFPVisType(ApplicationSettings.NFP_VIS.HEIGHTMAP);

            // check if region container is given and if so, attach the region to it
            Transform regionContainer = null;
            if (fileRefs) { regionContainer = fileRefs.heightmapRegionContainer; }
            if (!regionContainer) {
                Debug.LogError("Failed to spawn region of height map because container is missing!");
                DestroyImmediate(regionObj);
                return false;
            }

            // register region object
            region.AddUIGameObject(ARProperty.TYPE.NFP, regionObj);

            // parent new object to the region container
            regionObj.transform.SetParent(regionContainer, false);

            // set position and scale (scale x default 0 so that it can be adjusted later)
            SetRegionPositionAndSize(regionObj, fileRefs, section, lineHeight, 0, 0, 1); //heightMapWidth, 0, 1);
            return true;
        }


        // ========================== FEATURES ==========================//

        /// <summary>Remove all feature regions of a file.</summary>
        public static void CleanupFeatureRegions(CodeFileReferences fileInstance) {

            List<GameObject> elementsToRemove = new List<GameObject>();

            // remove possible old region elements of "code" marking visualization
            Transform featureContainer = fileInstance.featureContainer;
            if (featureContainer) {
                // (works while iterating because Destroy is not running immediately)
                foreach (Transform child in featureContainer) {
                    if (child != featureContainer) { elementsToRemove.Add(child.gameObject); }
                }
            }

            // destroy immediate now to prevent errors on coloring/mapping
            foreach (GameObject go in elementsToRemove) {
                if (!go) { continue; }
                DestroyImmediate(go);
            }

        }

        /// <summary>
        /// Creates visuals for features shown on the left of the code window.<para/>
        /// Only the x currently active features should be shown there.<para/>
        /// 
        /// This method also takes care of showing the "feature" overview window
        /// above the code window (which includes not just the active, but all features).<para/>
        /// 
        /// Returns true on success and false otherwise.
        /// </summary>
        public bool SpawnFeatureRegions(CodeFileReferences fileInstance) {

            currentFile = fileInstance.GetCodeFile();
            
            // remove old regions objects before spawning new ones
            CleanupFeatureRegions(fileInstance);

            // region loader tells about loaded regions
            RegionLoader loader = ApplicationLoader.GetInstance().GetRegionLoader();

            if (fileInstance.GetTextElements().Count == 0) {
                Debug.LogWarning("Failed to spawn feature regions! No text elements found.");
                return false;
            }

            // features use only the height because width must be calculated
            // by using the amount of active features and the fixed width of the feature box
            float lineHeight = fileInstance.GetCodeFile().GetLineInfo().lineHeight;
            if (lineHeight == 0) {
                Debug.LogWarning("Failed to spawn regions! A dimension is zero! (height: " + lineHeight + ")");
                return false;
            }
            //Debug.Log("Region line height: " + lineHeight);


            // ======== SPAWN FEATURE OVERVIEW ======== //

            // ToDo: Spawn feature overview window!


            // ======== SPAWN FEATURE SELECTION ======== //

            // spawn only active features on the left side
            List<Region> regionsToSpawn = new List<Region>();

            // get the currently selected features
            List<string> activeFeatures = ApplicationLoader.GetInstance().GetAppSettings().GetActiveFeatures();
            if (LOGGING) { Debug.Log("Active features: " + activeFeatures.Count); }

            foreach (Region region in loader.GetFileRegions(fileInstance.GetCodeFile().GetNode().GetPath())) {
                foreach (string featureName in activeFeatures) {
                    if (region.HasProperty(ARProperty.TYPE.FEATURE, featureName)) {
                        regionsToSpawn.Add(region); // adding the region once is sufficient
                        break;
                    }
                }
            }
            if (LOGGING) { Debug.Log("Feature regions to spawn: " + regionsToSpawn.Count); }

            // spawn the regions
            List<Region> regionsSpawned = new List<Region>();

            // spawn each active feature that belongs to a region
            int featureNo = -1;
            float widthPerFeature = featureWidth / activeFeatures.Count;
            foreach (string featureName in activeFeatures) {
                featureNo++;

                foreach (Region region in regionsToSpawn) {

                    // check if this region does not have this feature
                    if (!region.HasProperty(ARProperty.TYPE.FEATURE, featureName)) { continue; }

                    // success if at least one section was spawned
                    int sectionsSpawned = 0;
                    foreach (Region.Section section in region.GetSections()) {
                        sectionsSpawned += CreateFeatureRegion(region, section, fileInstance, lineHeight, widthPerFeature, featureNo) ? 1 : 0;
                    }

                    // store this region for later usage
                    if (sectionsSpawned > 0) { regionsSpawned.Add(region); }
                }
            }

            // add all the spawned feature regions to the CodeFile instance as reference
            AddSpawnedRegions(fileInstance, regionsSpawned);
            if (LOGGING) { Debug.Log("Feature regions spawned: " + regionsSpawned.Count); }
            return true;
        }

        /// <summary>
        /// Create a single feature region.<para/>
        /// Returns true on success and false otherwise.
        /// </summary>
        /// <param name="featureNo">Required to calculate the x_position (index in the "activeFeatures" list)</param>
        bool CreateFeatureRegion(Region region, Region.Section section, CodeFileReferences fileRefs, float lineHeight, float width, int featureNo) {

            // create and attach region gameObject instance
            if (GetFeaturePrefab() == null) {
                Debug.LogError("Failed to spawn Feature region - Missing prefab!");
                return false;
            }

            // (! IMPORTANT STEP !) create and set GameObject reference
            // The access to the GameObjects is required to color or rescale them
            GameObject featureObj = Instantiate(GetFeaturePrefab());
            RegionGameObject info = featureObj.GetComponent<RegionGameObject>();
            if (!info) {
                Debug.LogError("Failed to spawn feature region - Prefab is missing RegionGameObject component!");
                return false;
            }
            string featureName = ApplicationLoader.GetApplicationSettings().GetActiveFeature(featureNo);
            ARProperty property = region.GetProperty(ARProperty.TYPE.FEATURE, featureName);
            info.SetInfo(currentFile, region, property);

            // check if feature container is given and if so, attach the new feature bar to it
            Transform featureContainer = null;
            if (fileRefs) { featureContainer = fileRefs.featureContainer; }
            if (!featureContainer) {
                Debug.LogError("Failed to spawn feature region because container is missing!");
                DestroyImmediate(featureObj);
                return false;
            }

            // register region object
            region.AddUIGameObject(ARProperty.TYPE.FEATURE, featureObj);

            // parent new object to the region container
            featureObj.transform.SetParent(featureContainer, false);

            // set position and scale
            float xPos = featureNo * width;
            SetRegionPositionAndSize(featureObj, fileRefs, section, lineHeight, width, xPos);
            return true;
        }
        


        // ----------------------------------------------------------------------------------------------------------
        // Region Management

        public List<Region> GetSpawnedRegions(CodeFileReferences fileInstance) {
            if (!spawnedRegions.ContainsKey(fileInstance)) { return new List<Region>(); }
            return new List<Region>(spawnedRegions[fileInstance].Values);
        }

        /// <summary>Get a list of spawned regions that include this property type.</summary>
        public List<Region> GetSpawnedRegions(CodeFileReferences fileInstance, ARProperty.TYPE propType) {
            
            List<Region> list = new List<Region>();
            if (!spawnedRegions.ContainsKey(fileInstance)) { return list; }

            foreach (KeyValuePair<string, Region> entry in spawnedRegions[fileInstance]) {
                if (entry.Value.HasPropertyType(propType)) {
                    list.Add(entry.Value);
                }
            }

            return list;
        }

        public bool HasSpawnedRegions(CodeFileReferences fileInstance) {
            if (!spawnedRegions.ContainsKey(fileInstance)) { return false; }
            return spawnedRegions[fileInstance].Count > 0;
        }

        public bool HasSpawnedRegion(CodeFileReferences fileInstance, string regionID) {
            if (!spawnedRegions.ContainsKey(fileInstance)) { return false; }
            return spawnedRegions[fileInstance].ContainsKey(regionID);
        }

        /// <summary>
        /// Add a region to spawned regions list.<para/>
        /// Uniqueness is ensured by region ID.
        /// </summary>
        public bool AddSpawnedRegion(CodeFileReferences fileInstance, Region region) {

            if (HasSpawnedRegion(fileInstance, region.GetID())) { return false; }

            if (!spawnedRegions.ContainsKey(fileInstance)) {
                spawnedRegions.Add(fileInstance, new Dictionary<string, Region>());
            }

            spawnedRegions[fileInstance].Add(region.GetID(), region);
            return true;
        }

        /// <summary>
        /// Add a list of regions to spawned regions list.<para/>
        /// Uniqueness is ensured by region ID.
        /// </summary>
        public void AddSpawnedRegions(CodeFileReferences fileInstance, List<Region> regions) {
            regions.ForEach(region => AddSpawnedRegion(fileInstance, region));
        }
        
        
        /// <summary>Simply removes each spawned region reference.</summary>
        public void ClearSpawnedRegions(CodeFileReferences fileInstance) {
            if (!spawnedRegions.ContainsKey(fileInstance)) { return; }
            spawnedRegions[fileInstance].Clear();
        }

        /// <summary>
        /// Clear spawned regions that are no longer required.<para/>
        /// Checks if a region has an active feature or a selected NFP.<para/>
        /// If this is the case, the region is still required and wont be removed.
        /// </summary>
        public void ClearSpawnedRegions(CodeFileReferences fileInstance, List<string> activeFeatures, string selectedNFP) {

            if (!spawnedRegions.ContainsKey(fileInstance)) { return; }
            List<string> removeKeys = new List<string>();

            foreach (KeyValuePair<string, Region> entry in spawnedRegions[fileInstance]) {

                // do not remove if this region has the selected NFP
                if (entry.Value.HasProperty(ARProperty.TYPE.NFP, selectedNFP)) { continue; }

                // do not remove if this region has one of the active features
                bool found = false;
                foreach (string activeFeature in activeFeatures) {
                    if (entry.Value.HasProperty(ARProperty.TYPE.FEATURE, activeFeature)) {
                        found = true;
                        break;
                    }
                }
                if (found) { continue; }

                // add key so that this region gets removed
                removeKeys.Add(entry.Key);
            }

            removeKeys.ForEach(key => spawnedRegions[fileInstance].Remove(key));
        }


        /// <summary>Refresh the represented region value (to re-apply the visual properties).</summary>
        public void RefreshRegionValues(CodeFileReferences fileInstance, ARProperty.TYPE propType) {
            new RegionModifier(fileInstance, this).ApplyRegionValues(new ARProperty.TYPE[]{ propType });
            FireRegionValuesChangedEvent(fileInstance);
        }


        /// <summary>Notify listeners that regions changed.</summary>
        public void FireRegionValuesChangedEvent(CodeFileReferences fileInstance) {
            onRegionsValuesChanged.Invoke(fileInstance);
        }


        /// <summary>Refresh the shown regions for this property type.</summary>
        /// <param name="refreshRepresentation">Tells if the visual properties should be applied or not</param>
        public void RefreshRegions(CodeFileReferences fileInstance, ARProperty.TYPE propType, bool refreshRepresentation) {

            // ToDo: examine! the call "spawner.SpawnNFPRegions" is removing all spawned regions anyway!
            // remove old region objects
            //ClearOldRegions();

            // refresh spawned region objects
            if (propType == ARProperty.TYPE.NFP) { SpawnNFPRegions(fileInstance); }
            else if (propType == ARProperty.TYPE.FEATURE) { SpawnFeatureRegions(fileInstance); }

            // refresh visual property mapping
            if (refreshRepresentation) { RefreshRegionValues(fileInstance, propType); }
        }

        // ToDo: examine! the call "spawner.SpawnNFPRegions" is removing all spawned regions anyway!
        /// <summary>
        /// Checks if previous regions are no longer required.<para/>
        /// This means each currently spawned region will be checked for the current selection.<para/>
        /// It is a heavy operation and should be used with care!
        /// </summary>
        /*public void ClearOldRegions() {
            List<string> activeFeatures = ApplicationLoader.GetApplicationSettings().GetActiveFeatures();
            string selectedNFP = ApplicationLoader.GetApplicationSettings().GetSelectedNFP();
            ClearSpawnedRegions(activeFeatures, selectedNFP);
        }
        */

    }
}
