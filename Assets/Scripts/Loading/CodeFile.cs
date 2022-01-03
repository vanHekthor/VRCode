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

        /// <summary>stores min/max values of non functional properties (NFP) of all regions (key = prop. name)</summary>
        private Dictionary<string, MinMaxValue> nfpMinMaxValues = new Dictionary<string, MinMaxValue>();

        // stores line count for access to this information without opening the file
        private long lineCountQuick = -1;
        private bool lineCountQuick_set = false;        

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

        /// <summary>Get min/max values of all non functional properties.</summary>
        public Dictionary<string, MinMaxValue> GetNFPMinMaxValues() { return nfpMinMaxValues; }

        /// <summary>Returns the min/max value of this NFP or null if not found.</summary>
        public MinMaxValue GetNFPMinMaxValue(string propertyName) {
            if (!nfpMinMaxValues.ContainsKey(propertyName)) { return null; }
            return nfpMinMaxValues[propertyName];
        }

        /// <summary>
        /// Retrieves the amount of lines this file has.<para/>
        /// This method can be used without the need to spawn the file first.<para/>
        /// Once read, the file will only be analyzed when you use the force parameter,
        /// otherwise, you can safely call this method many times without performance issues.<para/>
        /// If an issue occurs while reading the file, the returned value will be zero.
        /// </summary>
        /// <param name="forceRefresh">Force to refresh the line count (can take some time).</param>
        public long GetLineCountQuick(bool forceRefresh = false) {
            if (lineCountQuick_set && !forceRefresh) { return lineCountQuick; }
            lineCountQuick_set = true;
            lineCountQuick = Utility.GetLOC(GetNode().GetFullPath());
            return lineCountQuick;
        }



        // FUNCTIONALITY

        /// <summary>
        /// Update the line information to get line count
        /// and line height info which is required for region spawning.
        /// </summary>
        public void UpdateLineInfo() {
            
            int totalLineCount = 0;
            float lineHeight = -1;
            float lineWidth = -1;
            //int textNo = 0;
            foreach (TMP_TextInfo textInfo in GetReferences().GetTextElements()) {

                // force a mesh update before reading the properties
                textInfo.textComponent.ForceMeshUpdate();
                totalLineCount += textInfo.lineCount;
                //Debug.Log("Text " + (textNo++) + " lines: " + textInfo.lineCount); // debug

                if (lineHeight <= 0) {
                    TMP_Text text = textInfo.textComponent;
                    if (text != null) {
                        lineHeight = text.fontScale * text.font.faceInfo.lineHeight;
                    }
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

        /// <summary>
        /// Highlights a region in the code.
        /// </summary>
        /// <param name="start">start line number</param>
        /// <param name="end">end line number</param>
        /// <returns>line highlight component attached to instantiated object</returns>
        public LineHighlight HighlightLines(int start, int end) {

            float lineHeight = lineInfo.lineHeight;
            float totalWidth_codeMarking = lineInfo.lineWidth;

            // check height and width values
            if (lineHeight == 0) {
                Debug.LogWarning("Failed to spawn regions! Line height is zero!");
                return null;
            }

            if (totalWidth_codeMarking == 0) {
                Debug.LogWarning("Failed to spawn regions! Code marking width is zero!");
                return null;
            }

            // get region width for code marking visualization (might change in future)
            RectTransform scrollRectRT = references.GetScrollRect().GetComponent<RectTransform>();
            RectTransform textContainerRT = references.textContainer.GetComponent<RectTransform>();
            RectTransform vertScrollbarRT = references.GetVerticalScrollbarRect();
            if (scrollRectRT && textContainerRT && vertScrollbarRT) {
                totalWidth_codeMarking = scrollRectRT.sizeDelta.x - textContainerRT.anchoredPosition.x - Mathf.Abs(vertScrollbarRT.sizeDelta.x) - 5;
            }

            return references.spawnLineHighlight(start, end, totalWidth_codeMarking, lineHeight);
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

    }
}
