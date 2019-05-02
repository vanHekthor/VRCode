using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO.Structure;
using VRVis.Elements;
using VRVis.Spawner.File;
using TMPro;
using VRVis.Spawner;
using VRVis.RegionProperties;
using VRVis.Spawner.Regions;
using VRVis.Settings;
using VRVis.Utilities;

namespace VRVis.IO {

    /// <summary>
    /// ("GLOBAL" INFORMATION ABOUT A FILE)<para/>
    /// This class represents a source code file in the application.
    /// It holds according graph-node, region objects and so on.<para/>
    /// This class also keeps the method to apply properties.<para/>
    /// Instances of this class are created and stored by the "StructureLoader".
    /// </summary>
    public class CodeFile {
        
        /// <summary>
        /// According node in the software system structure.
        /// Keeps all the important information about that file using references.
        /// This means that e.g. SNode is accessible as well as the code window references.
        /// Instances of this method are created by the "StructureLoader" for each file node.
        /// </summary>
        private readonly SNode node;

        /// <summary>attached to the editor code window and holds several references and information (set by FileSpawner)</summary>
        private CodeFileReferences references;

        /// <summary>information about the content after the file was spawned</summary>
        public struct ReadInformation {
            public int charactersRead_total;
            public int linesRead_total;

            public void Clear() {
                charactersRead_total = 0;
                linesRead_total = 0;
            }
        }

        private ReadInformation readContentInfo = new ReadInformation();

        /// <summary>information about the lines after the file was spawned</summary>
        public struct LineInformation {
            public bool isSet;
            public int lineCount;
            public float lineHeight;
            public float lineWidth;

            public void Clear() {
                isSet = false;
                lineCount = 0;
                lineHeight = 0;
                lineWidth = 0;
            }

            public bool IsInfoSet() { return isSet; }
            public void SetInfo(bool state) { isSet = state; }
        }

        private LineInformation lineInfo = new LineInformation();
        
        /// <summary>
        /// stores the spawned regions (modified by the RegionSpawner)<para/>
        /// key = region ID (to ensure uniqueness) and value = Region instance
        /// </summary>
        private Dictionary<string, Region> spawnedRegions = new Dictionary<string, Region>();

        /// <summary>stores min/max values of non functional properties (NFP) of all regions (key = prop. name)</summary>
        private Dictionary<string, MinMaxValue> nfpMinMaxValues = new Dictionary<string, MinMaxValue>();



        // CONSTRUCTORS

        public CodeFile(SNode node) {
            this.node = node;
            readContentInfo.Clear();
            lineInfo.Clear();
        }



        // GETTER AND SETTER

        public SNode GetNode() { return node; }

        public CodeFileReferences GetReferences() { return references; }
        public void SetReferences(CodeFileReferences references) { this.references = references; }

        public LineInformation GetLineInfo() { return lineInfo; }
        public bool IsLineInfoSet() { return lineInfo.isSet; }

        /// <summary>
        /// Uses the region loader instance and returns all regions of this file.<para/>
        /// The RELATIVE PATH is used as the key for lookup (because relative given in nodes JSON file).<para/>
        /// Returns an empty list if the region loader could not be found or no regions exist.<para/>
        /// The returned regions are not necessarily spawned!
        /// </summary>
        public List<Region> GetRegions() {
            RegionLoader rl = ApplicationLoader.GetInstance().GetRegionLoader();
            if (rl == null) { return new List<Region>(); }
            return rl.GetFileRegions(node.GetPath());
        }

        /// <summary>Tells if the references are available that lead to the code window components.</summary>
        public bool IsCodeWindowExisting() { return references != null; }

        public ReadInformation GetContentInfo() { return readContentInfo; }

        public void SetContentInfo(ReadInformation readInfo) { readContentInfo = readInfo; }

        public List<Region> GetSpawnedRegions() { return new List<Region>(spawnedRegions.Values); }

        /// <summary>Get a list of spawned regions that include this property type.</summary>
        public List<Region> GetSpawnedRegions(ARProperty.TYPE propType) {
            List<Region> list = new List<Region>();
            foreach (KeyValuePair<string, Region> entry in spawnedRegions) {
                if (entry.Value.HasPropertyType(propType)) {
                    list.Add(entry.Value);
                }
            }
            return list;
        }

        public bool HasSpawnedRegion(string regionID) { return spawnedRegions.ContainsKey(regionID); }

        /// <summary>
        /// Add a region to spawned regions list.<para/>
        /// Uniqueness is ensured by region ID.
        /// </summary>
        public bool AddSpawnedRegion(Region region) {
            if (HasSpawnedRegion(region.GetID())) { return false; }
            spawnedRegions.Add(region.GetID(), region);
            return true;
        }

        /// <summary>
        /// Add a list of regions to spawned regions list.<para/>
        /// Uniqueness is ensured by region ID.
        /// </summary>
        public void AddSpawnedRegions(List<Region> regions) {
            regions.ForEach(region => AddSpawnedRegion(region));
        }

        /// <summary>Get min/max values of all non functional properties.</summary>
        public Dictionary<string, MinMaxValue> GetNFPMinMaxValues() { return nfpMinMaxValues; }

        /// <summary>Returns the min/max value of this NFP or null if not found.</summary>
        public MinMaxValue GetNFPMinMaxValue(string propertyName) {
            if (!nfpMinMaxValues.ContainsKey(propertyName)) { return null; }
            return nfpMinMaxValues[propertyName];
        }



        // FUNCTIONALITY

        /// <summary>
        /// Called after this code file was spawned to perform remaining steps.
        /// </summary>
        public void JustSpawnedEvent(CodeWindowEdgeSpawner cwEdgeSpawner) {
            
            // add line numbers (use information from read content)
            GetReferences().AddLineNumbers((uint) GetContentInfo().linesRead_total);

            // try to get correct line information (height and so on)
            UpdateLineInfo();

            // enable or disable heightmap based on current application settings
            bool heightMapVisible = ApplicationLoader.GetApplicationSettings().IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP);
            ToggleHeightMap(heightMapVisible);

            // show/hide active feature visualization according to default state in app settings
            ToggleActiveFeatureVis(ApplicationLoader.GetApplicationSettings().GetDefaultActiveFeatureVisState());

            // spawn the regions using the RegionSpawner instance
            if (!RegionSpawner.GetInstance()) {
                Debug.LogError("Missing RegionSpawner instance!");
                return;
            }

            RefreshRegions(ARProperty.TYPE.NFP, false); // RegionSpawner.GetInstance().SpawnNFPRegions(this);
            RefreshRegions(ARProperty.TYPE.FEATURE, false); // RegionSpawner.GetInstance().SpawnFeatureRegions(this);

            // apply visual properties / region coloring and scaling accordingly
            new RegionModifier(this).ApplyRegionValues();

            // notify edge spawner to take care of spawning node edges
            if (cwEdgeSpawner) { cwEdgeSpawner.CodeWindowSpawnedEvent(this); }
        }

        /// <summary>
        /// Update the line information to get line count
        /// and line height info which is required for region spawning.
        /// </summary>
        public void UpdateLineInfo() {
            
            int totalLineCount = 0;
            float lineHeight = -1;
            float lineWidth = -1;
            int textNo = 0;
            foreach (TMP_TextInfo textInfo in GetReferences().GetTextElements()) {

                // force a mesh update before reading the properties
                textInfo.textComponent.ForceMeshUpdate();
                totalLineCount += textInfo.lineCount;
                Debug.Log("Text " + (textNo++) + " lines: " + textInfo.lineCount);

                if (lineHeight <= 0) {
                    TMP_Text text = textInfo.textComponent;
                    if (text != null) { lineHeight = text.fontScale * text.font.fontInfo.LineHeight; }
                    else { Debug.LogError("TextInfo textComponent is null!"); }
                }

                /*
                // Debug to check if we gather correct line information
                int x = 0;
                foreach (TMP_LineInfo li in textInfo.lineInfo) {
                    Debug.Log((x++) + " width: " + li.width + ", characters: " + li.characterCount);
                }
                */
                
                // getting the value of the first line is sufficient
                // because the total width is the same for all lines of this text instance
                float thisWidth = textInfo.lineInfo[0].width;
                if (lineWidth <= 0 || thisWidth > lineWidth) {
                    lineWidth = thisWidth;
                }
            }

            lineInfo.lineCount = totalLineCount;
            lineInfo.lineHeight = lineHeight > 0 ? lineHeight : 0;
            lineInfo.lineWidth = lineWidth > 0 ? lineWidth : 0;
            lineInfo.isSet = true;
        }


        /// <summary>Simply removes each spawned region reference.</summary>
        public void ClearSpawnedRegions() { spawnedRegions.Clear(); }

        /// <summary>
        /// Clear spawned regions that are no longer required.<para/>
        /// Checks if a region has an active feature or a selected NFP.<para/>
        /// If this is the case, the region is still required and wont be removed.
        /// </summary>
        public void ClearSpawnedRegions(List<string> activeFeatures, string selectedNFP) {

            List<string> removeKeys = new List<string>();

            foreach (KeyValuePair<string, Region> entry in spawnedRegions) {

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

            removeKeys.ForEach(key => spawnedRegions.Remove(key));
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


        /// <summary>Refresh the represented region value (to re-apply the visual properties).</summary>
        public void RefreshRegionValues(ARProperty.TYPE propType) {
            new RegionModifier(this).ApplyRegionValues(new ARProperty.TYPE[]{ propType });
        }


        /// <summary>Refresh the shown regions for this property type.</summary>
        /// <param name="refreshRepresentation">Tells if the visual properties should be applied or not</param>
        public void RefreshRegions(ARProperty.TYPE propType, bool refreshRepresentation) {

            // ToDo: examine! the call "spawner.SpawnNFPRegions" is removing all spawned regions anyway!
            // remove old region objects
            //ClearOldRegions();

            RegionSpawner spawner = RegionSpawner.GetInstance();
            if (!spawner) {
                Debug.LogError("Failed to refresh regions of file (" +
                    GetNode().GetName() + ") - Missing RegionSpawner instance!");
                return;
            }

            // refresh spawned region objects
            if (propType == ARProperty.TYPE.NFP) { spawner.SpawnNFPRegions(this); }
            else if (propType == ARProperty.TYPE.FEATURE) { spawner.SpawnFeatureRegions(this); }

            // refresh visual property mapping
            if (refreshRepresentation) { RefreshRegionValues(propType); }
        }


        /// <summary>Makes the heightmap visible if hidden.</summary>
        public void ShowHeightMap() {
            GameObject heightmap = GetReferences().GetHeightmap();
            if (heightmap && !heightmap.activeSelf) { heightmap.SetActive(true); }
        }

        /// <summary>Makes the heightmap invisible if shown.</summary>
        public void HideHeightMap() {
            GameObject heightmap = GetReferences().GetHeightmap();
            if (heightmap && heightmap.activeSelf) { heightmap.SetActive(false); }
        }

        /// <summary>Change visibility of height map.</summary>
        public void ToggleHeightMap(bool visible) {
            if (visible) { ShowHeightMap(); }
            else { HideHeightMap(); }
        }


        /// <summary>Makes active feature visualization visible if hidden.</summary>
        public void ShowActiveFeatureVis() {
            GameObject afv = GetReferences().GetActiveFeatureVis();
            if (afv && !afv.activeSelf) { afv.SetActive(true); }
        }

        /// <summary>Hides active feature visualization if visible.</summary>
        public void HideActiveFeatureVis() {
            GameObject afv = GetReferences().GetActiveFeatureVis();
            if (afv && afv.activeSelf) { afv.SetActive(false); }
        }

        /// <summary>Change visibility of active feature visualization.</summary>
        public void ToggleActiveFeatureVis(bool visible) {
            if (visible) { ShowActiveFeatureVis(); }
            else { HideActiveFeatureVis(); }
        }


        /// <summary>
        /// Recalculates the current performance influence model value
        /// for each property of each region and then
        /// uses this value to determine the current min/max values.
        /// </summary>
        public void UpdateNFPValues() {

            // reset previous min max values
            foreach (MinMaxValue mm in nfpMinMaxValues.Values) { mm.ResetMinMax(); }

            foreach (Region region in GetRegions()) {

                // recalculate PIM values and min/max of that region
                region.UpdateNFPValues();

                foreach (KeyValuePair<string, MinMaxValue> entry in region.GetNFPMinMaxValues()) {
                    
                    string propName = entry.Key;
                    MinMaxValue minMax = new MinMaxValue();
                    if (nfpMinMaxValues.ContainsKey(propName)) { minMax = nfpMinMaxValues[propName]; }
                    else { nfpMinMaxValues.Add(propName, minMax); }
                    minMax.Update(entry.Value.GetMinValue());
                    minMax.Update(entry.Value.GetMaxValue());
                }
            }
        }

    }
}
