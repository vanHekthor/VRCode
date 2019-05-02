using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.RegionProperties;
using VRVis.Settings;
using VRVis.Spawner.File;

namespace VRVis.Spawner {

    /// <summary>
    /// Takes care of spawning and managing code windows
    /// which show the content of a code file.<para/>
    /// There can only be one instance of this class!
    /// </summary>
    public class FileSpawner : MonoBehaviour {

        private static FileSpawner INSTANCE;

        [Tooltip("The prefab for the code window")]
        public GameObject codeWindowPrefab;

        [Tooltip("Transform to attach code windows")]
        public Transform codeWindowParent;

        [Tooltip("Prefab with TextMeshPro component and content size fitter")]
        public GameObject textPrefab;

        //[Tooltip("Due to TextMeshPro, it is sometimes necessary to try again adding the regions")]
        //public int addFileRegionTriesMax = 5;

        // after how many characters we have to split up the text
        // and add the rest to another text element
        [Tooltip("Maximum amount of characters per text instance")]
        public int LIMIT_CHARACTERS_PER_TEXT = 10000;

        private Dictionary<string, CodeFile> spawnedFiles = new Dictionary<string, CodeFile>();
        private CodeFile currentFile;
        private bool scrollbarDisabled = false;
        private int fileSpawningStep = 0;

        // will be notified about code window related events if attached to the same object
        private CodeWindowEdgeSpawner cwEdgeSpawner;


        // "Constructor"
        void Awake() {

            if (!INSTANCE) { INSTANCE = this; }
            else {
                Debug.LogError("There can only be one instance of the FileSpawner!");
                DestroyImmediate(this); // destroy this component instance
                return;
            }

            if (!codeWindowPrefab) {
                Debug.LogError("Missing code window prefab!");
            }

            cwEdgeSpawner = GetComponent<CodeWindowEdgeSpawner>();
            if (!cwEdgeSpawner) { cwEdgeSpawner = ApplicationLoader.GetInstance().GetEdgeSpawner(); }
            if (cwEdgeSpawner) {
                Debug.Log("CodeWindowEdgeSpawner script found!\nNotification about code window related events enabled.");
            }
        }


        // GETTER AND SETTER

        /// <summary>Get the only instance of this class. Can be null if not set yet!</summary>
        public static FileSpawner GetInstance() { return INSTANCE; }

        /// <summary>Returns true if the code file is already spawned by using its full path.</summary>
        public bool IsFileSpawned(CodeFile codeFile) {
            return spawnedFiles.ContainsKey(codeFile.GetNode().GetFullPath());
        }

        /// <summary>Returns true if the file behind this path is spawned.</summary>
        public bool IsFileSpawned(string fullFilePath) {
            return spawnedFiles.ContainsKey(fullFilePath);
        }

        /// <summary>Returns the spawned files CodeFile instance or null.</summary>
        public CodeFile GetFileSpawned(string fullFilePath) {
            return IsFileSpawned(fullFilePath) ? spawnedFiles[fullFilePath] : null;
        }

        /// <summary>Returns the spawned code files.</summary>
        public IEnumerable<CodeFile> GetSpawnedFiles() {
            return spawnedFiles.Values;
        }


        // FUNCTIONALITY

        /**
         * Adding regions is done here because the required
         * text information is not available right after spawning the element.
         */
        void Update() {

            if (scrollbarDisabled) {
                scrollbarDisabled = false;

                // enable scroll rect to fix the scroll bar bug
                if (currentFile.GetReferences().GetScrollRect()) {
                    currentFile.GetReferences().GetScrollRect().enabled = true;
                }
            }

        }

        /** Called after all Update functions are called. */
        void LateUpdate() {
            
            // last step of the file spawning process is to notify the code file itself about it
            if (fileSpawningStep > 0) {

                // wait a bit so that the text mesh is aligned
                // (one iteration should be sufficient but we use more than one to ensure)
                if (fileSpawningStep > 2) {
                    currentFile.JustSpawnedEvent(cwEdgeSpawner);
                    fileSpawningStep = 0;
                }
                else {
                    fileSpawningStep++;
                }
            }

        }

        /// <summary>Refresh all the spawned regions of the given type for all spawned files.</summary>
        public void RefreshSpawnedFileRegions(ARProperty.TYPE propType) {
        
            bool heightMapVisible = ApplicationLoader.GetApplicationSettings().IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP);

            foreach (CodeFile file in GetSpawnedFiles()) {

                // false (default - hides height map) means code marking
                // true means height map visualization (show height map)
                file.ToggleHeightMap(heightMapVisible);

                // refresh the NFP region visualization
                file.RefreshRegions(propType, true);
            }
        }

        /// <summary>
        /// Refresh represented values of the spawned regions for all spawned files.
        /// (basically re-apply the visual properties)
        /// </summary>
        public void RefreshSpawnedFileRegionValues(ARProperty.TYPE propType) {
            foreach (CodeFile file in GetSpawnedFiles()) {
                file.RefreshRegionValues(propType);
            }
        }


        /// <summary>
        /// Removes all regions of the property type from spawned file windows
        /// using the cleanup method of the RegionSpawner class.
        /// </summary>
        public void RemoveSpawnedFileRegions(ARProperty.TYPE propType) {

            foreach (CodeFile file in GetSpawnedFiles()) {
                
                switch(propType) {
                    case ARProperty.TYPE.NFP: RegionSpawner.CleanupNFPRegions(file); break;
                    case ARProperty.TYPE.FEATURE: RegionSpawner.CleanupFeatureRegions(file); break;
                }
            }
        }


        /// <summary>Delete the current code window representing this file.</summary>
        public bool DeleteFileWindow(CodeFile codeFile) {

            if (codeFile == null) { return false; }

            if (!IsFileSpawned(codeFile.GetNode().GetFullPath())) {
                Debug.LogError("Tried to delete a file that is not spawned yet!");
                return false;
            }

            if (!codeFile.IsCodeWindowExisting()) {
                Debug.LogError("Code window references missing!");
                return false;
            }

            // notify edge spawner to take care of removing edges
            Debug.Log("Deleting code window of file: " + codeFile.GetNode().GetName());
            if (cwEdgeSpawner) { cwEdgeSpawner.CodeWindowRemovedEvent(codeFile); }

            // unregister the file from "spawned" list
            spawnedFiles.Remove(codeFile.GetNode().GetFullPath());

            // destroy code file references
            Destroy(codeFile.GetReferences().gameObject);
            codeFile.SetReferences(null);
            return true;
        }


        /// <summary>
        /// Spawn a code window showing the file content.<para/>
        /// A file is unique and can not be spawned multiple times.<para/>
        /// Returns the code window CodeFile or null on errors.<para/>
        /// Returns null if already spawned.
        /// </summary>
        public CodeFile SpawnFile(SNode fileNode, Vector3 position, Quaternion rotation) {

            // DONE: Spawn the game object (from prefab) of the code window
            // DONE: Attach a new instance of the "SpawnedFileInfo" script
            // DONE: Use the "SpawnedFileInfo" script to store file related information
            // DONE: Spawn the actual file content
            // DONE: Spawn the file regions for the current selection
            // DONE: Color the file regions according to their visual attributes

            if (fileSpawningStep != 0) {
                Debug.Log("Currently spawning a file...");
            }

            // prevent spawning the same file multiple times
            if (IsFileSpawned(fileNode.GetFullPath())) {
                Debug.LogWarning("File already spawned: " + fileNode.GetName());
                return null;
            }

            // get according and required code file instance
            CodeFile file = ApplicationLoader.GetInstance().GetStructureLoader().GetFileByFullPath(fileNode.GetFullPath());
            if (file == null) {
                Debug.LogError("Failed to get CodeFile instance for file: " + fileNode.GetFullPath());
                return null;
            }

            // instantiate a new game object and attach to parent
            fileSpawningStep = 1;
            GameObject newCodeWindow = Instantiate(codeWindowPrefab, position, rotation);
            newCodeWindow.transform.SetParent(codeWindowParent, true);

            // use CodeFileReferences (should already be attached to the code window prefab)
            CodeFileReferences fileRefs = newCodeWindow.GetComponent<CodeFileReferences>();
            if (!fileRefs) {
                Debug.LogError("Spawning file failed! Missing CodeFileReferences component!");
                DestroyImmediate(newCodeWindow);
                return null;
            }

            // store reference to the current file for later usage
            // and set the reference to the CodeFileReferences instance
            currentFile = file;
            currentFile.SetReferences(fileRefs);
            fileRefs.SetCodeFile(currentFile);

            // disable and enable later in update to fix scroll bar bug
            scrollbarDisabled = false;
            if (fileRefs.GetScrollRect()) {
                fileRefs.GetScrollRect().enabled = false;
                scrollbarDisabled = true;
            }

            // load the actual file content
            if (!LoadFileContent(fileNode, textPrefab)) {
                Debug.LogError("Failed to load content of file: " + fileNode.GetFullPath());
                currentFile.SetReferences(null);
                DestroyImmediate(newCodeWindow);
                return null;
            }

            // register this file as spawned using its full path as the key
            spawnedFiles.Add(fileNode.GetFullPath(), currentFile);
            return file;
        }


        // ################# FILE CONTENT LOADING ################# //

        /// <summary>
        /// Loads the highlighted source code from the file.<para/>
        /// Returns true on success and false otherwise.
        /// </summary>
        public bool LoadFileContent(SNode fileNode, GameObject textPrefab) {

            // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.-ctor?view=netframework-4.7.2
            string filePath = fileNode.GetFullPath();
            FileInfo fi = new FileInfo(filePath);

            if (!fi.Exists) {
                Debug.LogError("File does not exist! (" + filePath + ")");
                return false;
            }

            Debug.Log("Reading file contents...");
            // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.opentext?view=netframework-4.7.2
            using (StreamReader sr = fi.OpenText()) {

                // get and clear possible old content information
                CodeFile.ReadInformation contentInfo = new CodeFile.ReadInformation();
                contentInfo.Clear();

                // local and temp. information
                string curLine = "";
                string sourceCode = "";
                int charactersRead = 0;
                int linesRead = 0;
                int elementNo = 0;
                bool saved = false;
                GameObject currentTextObject = CreateNewTextElement(fileNode.GetName() + "_" + elementNo);

                while ((curLine = sr.ReadLine()) != null) {

                    // update counts and source code
                    sourceCode += curLine + "\n";
                    linesRead++;
                    contentInfo.linesRead_total++;
                    charactersRead += curLine.Length + 1; // "+ 1" because of the added "\n" (line break)
                    contentInfo.charactersRead_total += curLine.Length + 1;
                    saved = false;

                    // characer limit exceeded, so add this line to the next element
                    if (charactersRead > LIMIT_CHARACTERS_PER_TEXT) {
                    
                        // save the source code in the text object
                        SaveTextObject(currentTextObject, sourceCode);
                        Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
                        saved = true;

                        // use another text element
                        elementNo++;
                        currentTextObject = CreateNewTextElement(fileNode.GetName() + "_" + elementNo);

                        // reset "local" counts
                        sourceCode = "";
                        linesRead = 0;
                        charactersRead = 0;
                    }
                }

                Debug.Log("Lines read: " + contentInfo.linesRead_total);
                currentFile.GetReferences().SetLinesTotal(contentInfo.linesRead_total);
                currentFile.SetContentInfo(contentInfo);

                // save last instance if not done yet
                if (!saved) {
                    SaveTextObject(currentTextObject, sourceCode);
                    Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
                }
            }

            return true;
        }

        /**
         * Creates and returns a new text element using TextMeshPro.
         */
        public GameObject CreateNewTextElement(string name) {
            GameObject text = Instantiate(textPrefab);
            text.name = name;
            return text;
        }

        /**
         * Save the current text object,
         * adding its text and adding it to its parent transform.
         */
        void SaveTextObject(GameObject text, string sourceCode) {

            // DONE: spawn the TMP element and attach it to the text container
            // DONE: add the text element to the according variable in the "SpawnedFileInfo" script

            // take care of adding the text
            TextMeshProUGUI tmpgui = text.GetComponent<TextMeshProUGUI>();
            if (tmpgui) {
                tmpgui.SetText(sourceCode);
                currentFile.GetReferences().AddTextElement(tmpgui.textInfo);
                tmpgui.ForceMeshUpdate(); // force mesh update to calculate line heights instantly
            }
            else {
                TextMeshPro tmp = text.GetComponent<TextMeshPro>();
                if (!tmp) { tmp = text.AddComponent<TextMeshPro>(); }
                tmp.SetText(sourceCode);
                currentFile.GetReferences().AddTextElement(tmp.textInfo);
                tmp.ForceMeshUpdate(); // force mesh update to calculate line heights instantly
            }

            // set the element parent without keeping world coordinates
            text.transform.SetParent(currentFile.GetReferences().textContainer, false);
        }

    }
}
