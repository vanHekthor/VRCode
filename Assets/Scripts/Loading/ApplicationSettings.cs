using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.RegionProperties;
using VRVis.Spawner;

namespace VRVis.Settings {

    /// <summary>
    /// Stores current application settings.<para/>
    /// This includes user selections and so on.<para/>
    /// An instance of this class is managed by the ApplicationLoader.
    /// </summary>
    public class ApplicationSettings {

        /// <summary>Types of non functional property visualizations.</summary>
        public enum NFP_VIS { NONE, CODE_MARKING, HEIGHTMAP };


        /// <summary>Tells how many features can be active at the same time for "focus" (shown left next to code window)</summary>
        public readonly int MAX_ACTIVE_FEATURES = 5;

        /// <summary>Tells how many edge types can be active at the same time</summary>
        public readonly int MAX_ACTIVE_EDGE_TYPES = 20;


        // notify listeners when nfp relativity was changed
        public UnityEvent nfpRelativityChangedEvent = new UnityEvent();
        public UnityEvent selectedNFPChangedEvent = new UnityEvent();
        
        // currently: to notify listeners when the overview visualization visibility changes
        class StringBoolEvent : UnityEvent<string, bool> {}
        public UnityEvent<string, bool> visibilityChangedEvent = new StringBoolEvent();

        /// <summary>Dictionary to store new visualization visibility states</summary>
        public Dictionary<string, bool> visualizationVisibility = new Dictionary<string, bool>();


        /// <summary>Holds the active features selected by the user</summary>
        private List<string> activeFeatures = new List<string>();

        /// <summary>Holds edge types the user wants to see</summary>
        private HashSet<string> activeEdgeTypes = new HashSet<string>();

        /** switch between "in-text" region marking (false/default) or "heightmap" region marking (true) */
        private bool nfpVisSwitch = false;
        private Dictionary<NFP_VIS, bool> nfpVisualizationActive = new Dictionary<NFP_VIS, bool>();
        private bool toggleNFPVisActive = true; // for TESTING! (to iterate over visualization types)
        private readonly int NFPVisTypes;

        /// <summary>Non functional property to be currently shown (e.g. performance, memory, ...)</summary>
        private string selectedNFP = "";

        /// <summary>Tells if local min max values of a file should be used for visualization</summary>
        private bool applyLocalMinMaxValues = false;

        /// <summary>Visibility state of the active feature visualization.</summary>
        private bool activeFeatureVisVisible = false;



        // CONSTRUCTOR

        public ApplicationSettings() {

            // store all visualization types as disabled
            NFPVisTypes = 0;
            foreach (NFP_VIS visType in System.Enum.GetValues(typeof(NFP_VIS))) {
                if (visType != NFP_VIS.NONE) {
                    nfpVisualizationActive.Add(visType, false);
                    NFPVisTypes++;
                }
            }

            // only enable code marking as the default visualization
            //nfpVisualizationActive[NFP_VIS.CODE_MARKING] = true;
        }



        // GETTER AND SETTER

        /// <summary>Tells if local NFP min/max values of a file should be used for visualization.</summary>
        public bool GetApplyLocalMinMaxValues() { return applyLocalMinMaxValues; }

        public bool SetApplyLocalMinMaxValues(bool state) {

            if (state == applyLocalMinMaxValues) { return false; }
            applyLocalMinMaxValues = state;

            // update all currently spawned code window regions
            RefreshSpawnedFiles(ARProperty.TYPE.NFP, false);
            nfpRelativityChangedEvent.Invoke();
            return true;
        }


        /// <summary>Get active features (features in focus on left next to code window).</summary>
        public List<string> GetActiveFeatures() { return activeFeatures; }

        public string GetActiveFeature(int featureNo) { return activeFeatures[featureNo]; }

        public int GetActiveFeaturesCount() { return activeFeatures.Count; }


        /// <summary>Get active edge types (edge types that should be shown).</summary>
        public IEnumerable<string> GetActiveEdgeTypes() { return activeEdgeTypes; }

        /// <summary>Returns true if this is an active edge type.</summary>
        public bool IsEdgeTypeActive(string edgeType) { return activeEdgeTypes.Contains(edgeType); }


        /// <summary>Tells if this visualization is active.</summary>
        public bool IsNFPVisActive(NFP_VIS visType) { return nfpVisualizationActive[visType]; }

        /// <summary>Enable/disable this NFP visualization type.</summary>
        public void SetNFPVisActive(NFP_VIS visType, bool state, bool updateRegions = false) {
            if (nfpVisualizationActive[visType] == state) { return; } // nothing to do
            nfpVisualizationActive[visType] = state;
            bool respawnRegionObjects = true;
            if (visType == NFP_VIS.HEIGHTMAP && state == false) { respawnRegionObjects = false; } // improves performance
            if (updateRegions) { NFPSettingsChanged(respawnRegionObjects); }
        }


        /// <summary>Can be called to notify overview visualizations to change their visible state.</summary>
        public void SetVisualizationVisibility(string visName, bool state) {
            visibilityChangedEvent.Invoke(visName, state);
            visualizationVisibility[visName] = state;
        }

        /// <summary>
        /// Get the visualization visibility state (currently only used by overview window - future ready approach).<para/>
        /// Returns true if the visualization exists and writes its visibility state to the visibility parameter.
        /// </summary>
        public bool GetVisualizationVisibility(string visName, out bool visibility) {
            visibility = false;
            if (!visualizationVisibility.ContainsKey(visName)) { return false; }
            visibility = visualizationVisibility[visName];
            return true;
        }


        /// <summary>Currently selected non function property to be shown (e.g. performance, memory, ...)</summary>
        public string GetSelectedNFP() { return selectedNFP; }

        public void SetSelectedNFP(string propertyName) {
            if (propertyName == selectedNFP) { return; }
            selectedNFP = propertyName;
            NFPSettingsChanged(true);
            selectedNFPChangedEvent.Invoke();
        }

        /// <summary>Currently selected non function property visualization.</summary>
        public bool GetNFPVisSwitch() { return nfpVisSwitch; }

        public void SetNFPVisSwitch(bool state) {

            if (state == nfpVisSwitch) { return; }
            nfpVisSwitch = state;

            /*if (nfpVisSwitch) { SetNFPVisType(NFP_VIS.HEIGHTMAP); }
            else { SetNFPVisType(NFP_VIS.CODE_MARKING); }*/

            if (nfpVisSwitch) {
                SetNFPVisActive(NFP_VIS.HEIGHTMAP, true);
                SetNFPVisActive(NFP_VIS.CODE_MARKING, false);
            }
            else {
                SetNFPVisActive(NFP_VIS.HEIGHTMAP, false);
                SetNFPVisActive(NFP_VIS.CODE_MARKING, true);
            }

            NFPSettingsChanged(true);
        }

        /// <summary>Toggle the switch and return the new state.</summary>
        public bool ToggleNFPVisSwitch() {
            SetNFPVisSwitch(!GetNFPVisSwitch());
            return GetNFPVisSwitch();
        }

        /** TESTING: to toggle nfp visualizations. */
        public void ToggleNFPVis() {

            /*
            switch (GetNFPVisType()) {
                case NFP_VIS.CODE_MARKING: SetNFPVisType(NFP_VIS.HEIGHTMAP); break;
                case NFP_VIS.HEIGHTMAP: SetNFPVisType(NFP_VIS.BOTH); break;
                case NFP_VIS.BOTH: SetNFPVisType(NFP_VIS.CODE_MARKING); break;
            }
            */

            int no = 0;
            bool allSameState = true;
            foreach (KeyValuePair<NFP_VIS, bool> entry in nfpVisualizationActive) {
                no++;
                if (entry.Value != toggleNFPVisActive) {  
                    SetNFPVisActive(entry.Key, toggleNFPVisActive);
                    if (no < NFPVisTypes) { allSameState = false; }
                    break;
                }
            }

            // switch the toggle and now turn all of one after another
            if (allSameState) { toggleNFPVisActive = !toggleNFPVisActive; }

            NFPSettingsChanged(true);
        }


        /// <summary>Get the default visibility state of the active feature visualization.</summary>
        public bool GetDefaultActiveFeatureVisState() { return activeFeatureVisVisible; }



        // FUNCTIONALITY

        /// <summary>
        /// Try to add the feature as active.<para/>
        /// Returns:
        /// 1 on success,
        /// 0 if maximum was reached,
        /// -1 if feature already added
        /// </summary>
        public int AddActiveFeature(string feature) {

            feature = feature.ToLower();
            if (activeFeatures.Count >= MAX_ACTIVE_FEATURES) { return 0; }
            if (activeFeatures.Contains(feature)) { return -1; }
            activeFeatures.Add(feature);
            ActiveFeaturesChanged(true);
            Debug.Log("Active feature added: " + feature);
            return 1;
        }

        /// <summary>Remove a feature. Returns true if successful.</summary>
        public bool RemoveActiveFeature(string feature) {

            feature = feature.ToLower();
            bool success = activeFeatures.Remove(feature);
            if (success) {
                ActiveFeaturesChanged(true);
                Debug.Log("Active feature removed: " + feature);
            }
            return success;
        }


        /// <summary>
        /// Try to add the edge type as active.<para/>
        /// Returns:
        /// 1 on success,
        /// 0 if maximum was reached,
        /// -1 if edge type already added
        /// </summary>
        public int AddActiveEdgeType(string edgeType) {

            edgeType = edgeType.ToLower();
            if (activeEdgeTypes.Count > MAX_ACTIVE_EDGE_TYPES) { return 0; }
            if (activeEdgeTypes.Contains(edgeType)) { return -1; }
            activeEdgeTypes.Add(edgeType);
            ActiveEdgeTypesChanged();
            Debug.Log("Edge type selected: " + edgeType);
            return 1;
        }

        /// <summary>
        /// Remove the edge type from active list.
        /// Returns true if successful.
        /// </summary>
        public bool RemoveActiveEdgeType(string edgeType) {

            edgeType = edgeType.ToLower();
            bool success = activeEdgeTypes.Remove(edgeType);
            if (success) {
                ActiveEdgeTypesChanged();
                Debug.Log("Active edge type removed: " + edgeType);
            }
            return success;
        }


        /// <summary>
        /// Checks if the current feature model configuration is valid.<para/>
        /// Returns true if this is the case and false otherwise.
        /// </summary>
        public bool IsFeatureModelValid() {

            VariabilityModel model = ApplicationLoader.GetInstance().GetVariabilityModel();
            VariabilityModelValidator validator = new VariabilityModelValidator(model);
            Debug.Log("Checking feature model validity...");
            bool valid = validator.IsConfigurationValid();
            Debug.Log("Finished checking feature model validity! Result: " + (valid ? "valid" : "invalid"));
            return valid;
        }


        // ======== Region Update ======== //

        /// <summary>Refresh the spawned file window regions accordingly.</summary>
        private void RefreshSpawnedFiles(ARProperty.TYPE propertyType, bool regionsChanged) {

            // update spawned code windows accordingly
            FileSpawner fspawner = FileSpawner.GetInstance();
            if (fspawner == null) { return; }

            if (regionsChanged) {
                // refresh spawned regions and their representation
                fspawner.RefreshSpawnedFileRegions(propertyType);
            }
            else {
                // refresh just the representation of spawned regions
                fspawner.RefreshSpawnedFileRegionValues(propertyType);
            }
        }

        /// <summary>Called when active features changed to refresh code windows.</summary>
        private void ActiveFeaturesChanged(bool regionsChanged) {
            RefreshSpawnedFiles(ARProperty.TYPE.FEATURE, regionsChanged);
        }

        /// <summary>Called when NFP settings (selection, switch, ...) changed to refresh code windows.</summary>
        private void NFPSettingsChanged(bool regionsChanged) {

            RefreshSpawnedFiles(ARProperty.TYPE.NFP, regionsChanged);
            //Debug.Log("NFP Settings changed: switch=" + GetNFPVisSwitch() + ", property=" + selectedNFP);

            // TESTING: print min/max code file
            StructureLoaderUpdater upd = ApplicationLoader.GetInstance().GetStructureLoaderUpdater();
            CodeFile mincf = upd.GetMinCodefile(selectedNFP);
            CodeFile maxcf = upd.GetMaxCodefile(selectedNFP);
            if (mincf != null) { Debug.Log("Codefile with min value: " + mincf.GetNode().GetPath()); }
            if (maxcf != null) { Debug.Log("Codefile with max value: " + maxcf.GetNode().GetPath()); }
        }


        /// <summary>Show/hide active feature visualization accordingly.</summary>
        public void ToggleActiveFeatureVisualization(bool visible) {

            if (activeFeatureVisVisible == visible) { return; }
            activeFeatureVisVisible = visible;

            // update all spawned code files accordingly
            FileSpawner fs = (FileSpawner) ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
            if (!fs) { return; }
            foreach (CodeFile spawned in fs.GetSpawnedFiles()) {
                spawned.ToggleActiveFeatureVis(activeFeatureVisVisible);
            }
        }


        // ======== Edge Update ======== //

        /// <summary>
        /// Refresh the spawned edges if active edge types changed.<para/>
        /// This includes removing invalid edges that were already spawned
        /// and adding edges of a possible type that was added at last.
        /// </summary>
        private void ActiveEdgeTypesChanged() {

            FileSpawner fs = (FileSpawner) ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
            if (!fs) { return; }
            CodeWindowEdgeSpawner eSpawner = (CodeWindowEdgeSpawner) fs.GetSpawner((uint) FileSpawner.SpawnerList.EdgeSpawner);
            if (!eSpawner) { return; }

            uint edgesAdded = 0;
            uint edgesRemoved = 0;
            if (eSpawner.UpdateSpawnedEdges(out edgesAdded, out edgesRemoved)) {
                Debug.Log("Edges updated: " + edgesAdded + " added, " + edgesRemoved + " removed");
            }
            else { Debug.LogError("Failed to update spawned edges!"); }
        }

    }
}
