using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // for image color manipulation (regions use images)
using System.IO;
using TMPro;
using Siro.VisualProperties;
using Siro.Regions;

namespace Siro.IO {

    /**
     * To load REGIONS from the region file
     * and VISUAL PROPERTIES from another file.
     * 
     * Also to load SOURCE CODE from a file and display it with syntax and region highlighting.
     */
    public class LoadFile : MonoBehaviour {

        // ToDo: Think about how this information should be given and when
        public string nodesPath = "";
        public string visualPropertiesPath = "";

        // an object with text mesh pro component and content size fitter
        public GameObject textPrefab;
        public GameObject regionPrefab;

        // used to attach text and regions
        public Transform textContainer;
        public Transform regionContainer;
        private List<TMP_TextInfo> textElementInfos = new List<TMP_TextInfo>();
        private RectTransform textContainerRT;

        // after how many characters we have to split up the text
        // and add the rest to another text element
        public int LIMIT_CHARACTERS_PER_TEXT = 10000;

        // pixel error per text-element to consider for region creation
        public float ERROR_PER_ELEMENT = 0.2f; // 0.2 seems to be a good value for font-size 8-14

        private RegionsArray regions;
        private bool regionsAdded;
        private int maxRegionAddTries;
        private int linesTotal;
        private bool fileLoaded;

        // The following is "global" information (maybe make it accessible for other scripts)
        // Key is a file name and value is an array of the files regions
        // (this is like an implementation of the indexing for faster lookups)
        // https://docs.unity3d.com/ScriptReference/Hashtable.html
        // https://docs.microsoft.com/en-us/dotnet/api/system.collections.hashtable?redirectedfrom=MSDN&view=netframework-4.7.2
        private Dictionary<string, List<Region>> fileRegions = new Dictionary<string, List<Region>>();

        // visual properties currently to color regions according to properties
        private VisProps visualProperties;

        // used to find according regions in "fileRegions" after loading a file
        private string current_relativeFilePath;
        private string current_fullFilePath;

        // how long to wait until loading a new file works
        // (to limit the file load operations a bit)
        [Tooltip("Time in seconds that the user has to wait before he can load another file")]
        public float loadFileWaitTime = 1.0f;
        private float lastTimeFileLoaded = 0;


        private void Start() {

            if (visualPropertiesPath.Trim().Length > 0) {
                Debug.Log("Loading visual properties");
                LoadVisualProperties(visualPropertiesPath);
            }
            else {
                Debug.LogError("Failed to load visual properties! (invalid file path)");
            }

            // load the regions file (this should always be done on application start)
            if (nodesPath.Trim().Length > 0) {
                Debug.Log("Loading regions");
                LoadRegions(nodesPath);
            }
            else {
                Debug.LogError("Failed to load regions! (invalid nodes file path)");
            }
        }


        /**
         * Load source code from file and according regions.
         * Wont load the same file multiple times.
         * (Thats the case if relative and full path match the ones of the last call)
         */
        public void LoadFileContent(string relativeFilePath, string fullFilePath) {

            // use has to wait before he can load another file
            if (Time.time < lastTimeFileLoaded + loadFileWaitTime) {
                return;
            }
            lastTimeFileLoaded = Time.time;

            // do not load same file multiple times
            if (current_relativeFilePath == relativeFilePath && current_fullFilePath == fullFilePath) {
                return;
            }

            // store for later usage in region creation
            current_relativeFilePath = relativeFilePath;
            current_fullFilePath = fullFilePath;

            // get rect transform of the text container for size retrieval
            if (textContainer) {
                textContainerRT = textContainer.GetComponent<RectTransform>();
            }
            else {
                Debug.LogError("Missing text container!");
                return;
            }

            // check that region container is set
            if (!regionContainer) {
                Debug.LogError("Missing region container!");
                return;
            }


            // reset settings
            fileLoaded = false;
            regionsAdded = false;
            maxRegionAddTries = 5;
            linesTotal = 0;
            textElementInfos.Clear();


            // remove possible old spawned text and regionobjects
            foreach (Transform child in textContainer.transform) { Destroy(child.gameObject); }
            foreach (Transform child in regionContainer.transform) { Destroy(child.gameObject); }


            // load the highlighted source code file
            // (this should later be done if a file was clicked)
            string fileName = "";
		    if (fullFilePath != "") {
                fileName = LoadCodeFile(fullFilePath);
            }
            else {
                Debug.LogError("Missing file path!");
                return;
            }

            if (fileName == null || fileName.Length == 0) {
                Debug.LogError("Missing file name!");
                return;
            }

            if (fileName.EndsWith(".rt")) {
                fileName = fileName.Substring(0, fileName.Length - 3);
            }

            fileLoaded = true;
            Debug.Log("Source code file loaded for class: " + fileName);
	    }
	

	    void Update () {

            // we have to wait for text mesh pro to create line information
            if (fileLoaded && !regionsAdded) {

                // add regions regarding the loaded file
                Debug.Log("Trying to spawn regions...");

                // get the file as it is stored in the "fileRegions" table
                string fileIdentifier = current_relativeFilePath;
                if (fileIdentifier.EndsWith(".rt")) {
                    fileIdentifier = fileIdentifier.Substring(0, fileIdentifier.Length - 3);
                }
                fileIdentifier = fileIdentifier.Replace("\\", "/");

                // add the regions for this file if possible
                int status = AddFileRegions(fileIdentifier);
            
                // stop trying to show regions (2 means try again next cycle)
                maxRegionAddTries--;
                if (status == 0 || status == 1 || maxRegionAddTries == 0) {
                    regionsAdded = true;
                }

                if (status == 0) { Debug.Log("Regions loaded successful!"); }
            }
    
	    }


        /**
         * Loads the highlighted source code from the file.
         * Currently returns the file name (not path) or null on errors.
         * ToDo: Return the internal program structure path to the file and use it!
         */ 
        string LoadCodeFile(string filePath) {
        
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.-ctor?view=netframework-4.7.2
            FileInfo fi = new FileInfo(filePath);

            if (!fi.Exists) {
                Debug.LogError("File does not exist! (" + filePath + ")");
                return null;
            }

            Debug.Log("Reading file contents...");
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.opentext?view=netframework-4.7.2
            using (StreamReader sr = fi.OpenText()) {

                string curLine = "";
                string sourceCode = "";

                int charactersRead = 0;
                int charactersRead_total = 0;
                int linesRead = 0;
                int linesRead_total = 0;
                int elementNo = 0;
                bool saved = false;

                GameObject currentTextObject = CreateNewTextElement(fi.Name + "_" + elementNo);

                while ((curLine = sr.ReadLine()) != null) {

                    // update counts and source code
                    sourceCode += curLine + "\n";
                    linesRead++;
                    linesRead_total++;
                    charactersRead += curLine.Length +  1; // +1 because of the added "\n" (line break)
                    charactersRead_total += curLine.Length + 1;
                    saved = false;

                    // characer limit exceeded, so add this line to the next element
                    if (charactersRead > LIMIT_CHARACTERS_PER_TEXT) {
                    
                        // save the source code in the text object
                        SaveTextObject(currentTextObject, sourceCode);
                        Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
                        saved = true;

                        // use another text element
                        elementNo++;
                        currentTextObject = CreateNewTextElement(fi.Name + "_" + elementNo);

                        // reset "local" counts
                        sourceCode = "";
                        linesRead = 0;
                        charactersRead = 0;
                    }
                }

                linesTotal = linesRead_total;

                // save last instance if not done yet
                if (!saved) {
                    SaveTextObject(currentTextObject, sourceCode);
                    Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
                }
            }

            return fi.Name;
        }


        /**
         * Creates and returns a new text element using TextMeshPro.
         */
        GameObject CreateNewTextElement(string name) {
            GameObject text = Instantiate(textPrefab);
            return text;
        }


        /**
         * Save the current text object,
         * adding its text and adding it to its parent transform.
         */
        void SaveTextObject(GameObject text, string sourceCode) {

            // take care of adding the text
            TextMeshProUGUI tmpgui = text.GetComponent<TextMeshProUGUI>();
            if (tmpgui) {
                tmpgui.SetText(sourceCode);
                textElementInfos.Add(tmpgui.textInfo);
            }
            else {
                TextMeshPro tmp = text.GetComponent<TextMeshPro>();
                if (!tmp) { tmp = text.AddComponent<TextMeshPro>(); }
                tmp.SetText(sourceCode);
                textElementInfos.Add(tmp.textInfo);
            }

            // set the element parent without keeping world coordinates
            text.transform.SetParent(textContainer, false);
        }


        /**
         * Load visual properties from the file.
         * This should be done before the loadRegions call
         * because visual properties map values to visual attributes.
         */
        void LoadVisualProperties(string filePath) {
            
            visualProperties = new VisProps(filePath);
            bool success = visualProperties.LoadProperties();
            if (!success) {
                Debug.LogError("Visual properties loading failed.");
            }
            else if (visualProperties.HasProperties()) {
                
                // print basic information about loaded properties
                string info = "Visual properties loaded:\n";
                foreach (string property in visualProperties.GetProperties()) {
                    info += "  - " + property + ": " + visualProperties.GetCount(property) + "\n";
                }
                Debug.Log(info);
                
            }
            else {
                Debug.Log("No visual property loaded.");
            }

        }


        /**
         * Load and display the JSON regions.
         * https://docs.unity3d.com/Manual/JSONSerialization.html
         */
        void LoadRegions(string nodesFilePath) {

            if (!File.Exists(nodesFilePath)) {
                Debug.LogError("Nodes file does not exist! (" + nodesFilePath + ")");
                return;
            }

            string nodesData = File.ReadAllText(nodesFilePath);
            regions = JsonUtility.FromJson<RegionsArray>(nodesData);
            Debug.Log("Regions loaded: " + regions.regions.Length);

            // ToDo: remove later (currently for debug)
            //Debug.Log("Region 1: " + regions.regions[0].info());

            // create index by adding regions regarding a file to the hashtable
            foreach (Region region in regions.regions) {

                string fileName = region.location;
                if (!fileRegions.ContainsKey(fileName)) {
                    fileRegions[fileName] = new List<Region>();
                }

                fileRegions[fileName].Add(region);

                // load property information
                foreach (NodeProperty property in region.properties) {

                    // check if a mapping for this property exists
                    if (visualProperties.HasProperty(property.type)) {
                        PropertyInformation propInf = visualProperties.GetList()[property.type];

                        // update min and max information
                        float propVal = property.value;
                        if (propVal < propInf.minValue || !propInf.minValueSet) { propInf.SetMinValue(propVal); } 
                        if (propVal > propInf.maxValue || !propInf.maxValueSet) { propInf.SetMaxValue(propVal); }
                    }
                }
            }

            PropertyInformation inf = visualProperties.getProperty("performance");
            Debug.Log("Performance: [" + inf.minValue + ".." + inf.maxValue + "]");

            // ToDo: more indexing?
        }


        /**
         * Adds regions regarding the file.
         * Returns possible error codes:
         * - 0 = okay
         * - 1 = error
         * - 2 = try again in next cycle (e.g. on line height missing)
         */
        int AddFileRegions(string fileName) {
        
            if (!fileRegions.ContainsKey(fileName)) {
                Debug.LogWarning("There are no regions for this file: " + fileName);
                return 1;
            }

            // used to get line height from text
            if (textElementInfos.Count == 0) {
                Debug.LogWarning("No text element infos!");
                return 1;
            }

            if (textElementInfos[0].lineInfo.Length == 0) {
                Debug.LogWarning("No line infos.");
                return 1;
            }

            // get line height and width of the text container
            TMP_LineInfo lineInf = textElementInfos[0].lineInfo[0];
            float lineHeight = lineInf.lineHeight;
            float totalWidth = textContainerRT.sizeDelta.x;

            if (lineHeight == 0) {
                Debug.LogWarning("Line height is zero! Try again.");
                return 2;
            }
            Debug.Log("Line height: " + lineHeight);

            List<Region> regs = fileRegions[fileName];
            Debug.Log("Regions for file: " + regs.Count);

            foreach (Region reg in regs) {

                // create and attach region
                GameObject regionObj = Instantiate(regionPrefab);
                regionObj.transform.SetParent(regionContainer, false);
            
                // pixel error due to amount of text-elements
                float pxErr = ((float) reg.start / (float) linesTotal) * ((float) textElementInfos.Count-1) * ERROR_PER_ELEMENT;

                // scale and position rection
                float x = 0;
                float y = (reg.start - 1) * -lineHeight + pxErr; // lineHeight needs to be a negative value!
                float width = totalWidth;
                float height = (reg.end - reg.start + 1) * lineHeight;

                RectTransform rt = regionObj.GetComponent<RectTransform>();
                if (!rt) { Debug.LogWarning("Could not find rect transform on region!"); }
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(width, height);

                // apply property mapping for this region (currently only region color)
                foreach (NodeProperty regionProp in reg.properties) {
                    string propType = regionProp.type;

                    // check if there are mappings regarding this region property
                    if (visualProperties.HasProperty(propType)) {
                        PropertyInformation pInf = visualProperties.getProperty(propType);    

                        // apply each mapping accordingly
                        foreach (MethodInformation mInf in pInf.methodInfos) {
                        
                            // ToDo: implement a system that does work for general case
                            if (mInf.name == "Color_Scale_1") {
                                Color regionColor = Methods.Color_Scale_1(regionProp.value, pInf.minValue, pInf.maxValue);
                                Image regionImageScript = regionObj.GetComponent<Image>();
                                if (regionImageScript) { regionImageScript.color = regionColor; }
                            }
                        
                        }
                    }
                }
            }
            Debug.Log("Added file regions!");

            return 0;
        }
    }

}