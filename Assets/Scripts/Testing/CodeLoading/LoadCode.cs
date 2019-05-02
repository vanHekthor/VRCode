using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using Siro.Regions;


public class LoadCode : MonoBehaviour {

    public string filePath;
    public string nodesPath;

    // an object with text mesh pro component and content size fitter
    public GameObject textPrefab;
    public GameObject regionPrefab;

    // used to attach text and regions
    public Transform textContainer;
    public Transform regionContainer;
    public List<TMP_TextInfo> textElementInfos;

    private RectTransform textContainerRT;
    private Vector2 textContainerSizeLast;

    // after how many characters we have to split up the text
    // and add the rest to another text element
    public int LIMIT_CHARACTERS_PER_TEXT = 10000;

    // pixel error per text-element to consider for region creation
    public float ERROR_PER_ELEMENT = 0.2f; // 0.2 seems to be a good value for font-size 8-14

    private RegionsArray regions;
    private bool regionsAdded = false;
    private int maxRegionAddTries = 5;
    private string loadedFileName = "";
    private int linesTotal = 0;

    // key is a file name and value is an array of the files regions
    // (this is like an implementation of the indexing for fast lookups)
    // https://docs.unity3d.com/ScriptReference/Hashtable.html
    // https://docs.microsoft.com/en-us/dotnet/api/system.collections.hashtable?redirectedfrom=MSDN&view=netframework-4.7.2
    private Dictionary<string, List<Region>> fileRegions = new Dictionary<string, List<Region>>();


	void Start () {

        // load the regions file
        // (this should later be done on application start)
        if (nodesPath != "") {
            loadRegions(nodesPath);
        }

        // get rect transform of the text container for size retrieval
        if (textContainer) {
            textContainerRT = textContainer.GetComponent<RectTransform>();
        }

        // load the highlighted source code file
        // (this should later be done if a file was clicked)
        string fileName = "";
		if (filePath != "") {
            fileName = loadFile(filePath);
        }

        if (fileName == null || fileName.Length == 0) {
            Debug.LogWarning("No file name!");
            return;
        }

        if (fileName.EndsWith(".rt")) {
            fileName = fileName.Substring(0, fileName.Length - 3);
        }

        // ToDo: this should later work using the program structure!
        fileName = "src/main/java/" + fileName;
        loadedFileName = fileName;

        Debug.Log("Source code file loaded for class: " + fileName);
	}
	

	void Update () {

        // we have to wait for text mesh pro to create line information
        if (!regionsAdded) {

            // add regions regarding the loaded file
            Debug.Log("Trying to spawn regions...");
            int status = addFileRegions(loadedFileName);
            
            // stop trying to show regions (2 means try again next cycle)
            maxRegionAddTries--;
            if (status == 0 || status == 1 || maxRegionAddTries == 0) {
                regionsAdded = true;
            }

            if (status == 0) { Debug.Log("Regions loaded successful!"); }
        }
    
	}
    
    
    /**
     * Get if the text container size changed
     * since the last request of this method.
     */
    bool textContainerSizeChanged() {
        if (!textContainerRT) { return false; }
        bool changed = textContainerSizeLast != textContainerRT.sizeDelta;
        if (changed) { textContainerSizeLast = textContainerRT.sizeDelta; }
        return changed;
    }


    /**
     * Loads the highlighted source code from the file.
     * Currently returns the file name (not path) or null on errors.
     * ToDo: Return the internal program structure path to the file and use it!
     */ 
    string loadFile(string filePath) {
        
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

            GameObject currentTextObject = createNewTextElement(fi.Name + "_" + elementNo);

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
                    saveTextObject(currentTextObject, sourceCode);
                    Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
                    saved = true;

                    // use another text element
                    elementNo++;
                    currentTextObject = createNewTextElement(fi.Name + "_" + elementNo);

                    // reset "local" counts
                    sourceCode = "";
                    linesRead = 0;
                    charactersRead = 0;
                }
            }

            linesTotal = linesRead_total;

            // save last instance if not done yet
            if (!saved) {
                saveTextObject(currentTextObject, sourceCode);
                Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
            }
        }

        return fi.Name;
    }


    /**
     * Creates and returns a new text element using TextMeshPro.
     */
    GameObject createNewTextElement(string name) {
        GameObject text = Instantiate(textPrefab);
        return text;
    }


    /**
     * Save the current text object,
     * adding its text and adding it to its parent transform.
     */
    void saveTextObject(GameObject text, string sourceCode) {

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
     * Load and display the JSON regions.
     * https://docs.unity3d.com/Manual/JSONSerialization.html
     */
    void loadRegions(string nodesFilePath) {

        if (!File.Exists(nodesFilePath)) {
            Debug.LogError("Nodes file does not exist! (" + nodesFilePath + ")");
            return;
        }

        string nodesData = File.ReadAllText(nodesFilePath);
        regions = JsonUtility.FromJson<RegionsArray>(nodesData);
        Debug.Log("Regions loaded: " + regions.regions.Length);

        // ToDo: remove later (currently for debug)
        Debug.Log("Region 1: " + regions.regions[0].info());

        // create index by adding regions regarding a file to the hashtable
        foreach (Region region in regions.regions) {

            string fileName = region.location;
            if (!fileRegions.ContainsKey(fileName)) {
                fileRegions[fileName] = new List<Region>();
            }

            fileRegions[fileName].Add(region);
        }

        // ToDo: more indexing? maybe also property loading...
    }


    /**
     * Adds regions regarding the file.
     * Returns possible error codes:
     * - 0 = okay
     * - 1 = error
     * - 2 = try again in next cycle (e.g. on line height missing)
     */
    int addFileRegions(string fileName) {
        
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
        }

        return 0;
    }

}
