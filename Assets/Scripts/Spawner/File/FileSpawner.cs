using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.RegionProperties;
using VRVis.Settings;
using VRVis.Spawner.File;
using VRVis.Spawner.Regions;

namespace VRVis.Spawner {

    /// <summary>
    /// Takes care of spawning and managing code windows
    /// which show the content of a code file.<para/>
    /// There can only be one instance of this class!
    /// </summary>
    [RequireComponent(typeof(RegionSpawner))]
    [RequireComponent(typeof(CodeWindowEdgeSpawner))]
    public class FileSpawner : ASpawner {

        private static FileSpawner INSTANCE;

        [Tooltip("The prefab for the code window")]
        public GameObject codeWindowPrefab;

        [Tooltip("Transform to attach code windows")]
        public Transform codeWindowParent;

        [Tooltip("Prefab with TextMeshPro component and content size fitter")]
        public GameObject textPrefab;

        [Tooltip("Instance of the according region spawner")]
        public RegionSpawner regionSpawner;

        [Tooltip("Instance of the according edge spawner")]
        public CodeWindowEdgeSpawner edgeSpawner;

        // callbacks for when the file was spawned (e.g. used by content overview)
        [HideInInspector]
        public CodeFileEvent onFileSpawned = new CodeFileEvent();
        public class CodeFileEvent : UnityEvent<CodeFile> {}

        // after how many characters we have to split up the text
        // and add the rest to another text element
        [Tooltip("Maximum amount of characters per text instance")]
        public int LIMIT_CHARACTERS_PER_TEXT = 10000;

        private Dictionary<string, CodeFile> spawnedFiles = new Dictionary<string, CodeFile>();
        private bool scrollbarDisabled = false;
        
        // information required during spawn procedure
        private bool spawning = false; // tells if there is currently a file to be spawned
        private CodeFile spawn_file;
        private SNode spawn_node;
        private Vector3 spawn_position;
        private Quaternion spawn_rotation;
        private GameObject spawn_window;


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

            if (!edgeSpawner) { Debug.LogError("Missing edge spawner!"); }
            if (!regionSpawner) { Debug.LogError("Missing region spawner!"); }
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

        void Update() {

            if (scrollbarDisabled) {
                scrollbarDisabled = false;

                // enable scroll rect to fix the scroll bar bug
                if (spawn_file.GetReferences().GetScrollRect()) {
                    spawn_file.GetReferences().GetScrollRect().enabled = true;
                }
            }

        }


        // numbers behind name must not be given explicitly if order is not relevant
        public new enum SpawnerList {
            RegionSpawner = 0,
            EdgeSpawner = 1
        }

        /// <summary>Returns the according spawner or null if invalid.</summary>
        public override ASpawner GetSpawner(uint spawner) {
            
            switch (spawner) {
                case (uint) SpawnerList.EdgeSpawner: return edgeSpawner;
                case (uint) SpawnerList.RegionSpawner: return regionSpawner;
            }

            return null;
        }


        /// <summary>
        /// Spawn a code window showing the file content.<para/>
        /// A file is unique and can not be spawned multiple times.<para/>
        /// Calls the callback method/action and passes the CodeFile instance on success or null otherwise!
        /// The boolean parameter tells about the success so that no "null"-check is required.
        /// The string parameter of the callback function includes a failure message.
        /// </summary>
        public void SpawnFile(SNode fileNode, Vector3 position, Quaternion rotation, Action<bool, CodeFile, string> callback) {

            // DONE: Spawn the game object (from prefab) of the code window
            // DONE: Attach a new instance of the "SpawnedFileInfo" script
            // DONE: Use the "SpawnedFileInfo" script to store file related information
            // DONE: Spawn the actual file content
            // DONE: Spawn the file regions for the current selection
            // DONE: Color the file regions according to their visual attributes

            if (spawning) {
                callback(false, null, "Currently spawning a file...");
                return;
            }

            // prevent spawning the same file multiple times
            if (IsFileSpawned(fileNode.GetFullPath())) {
                callback(false, null, "File already spawned: " + fileNode.GetName());
                return;
            }

            // get according and required code file instance
            CodeFile codeFile = fileNode.GetCodeFile();
            if (codeFile == null) {
                callback(false, null, "Failed to get CodeFile instance for file: " + fileNode.GetFullPath());
                return;
            }

            // assign required information
            spawn_file = codeFile;
            spawn_node = fileNode;
            spawn_position = position;
            spawn_rotation = rotation;
            spawning = true;

            // start the spawn coroutine
            StartCoroutine(SpawnCoroutine(callback));
        }


        /// <summary>
        /// The spawning procedure consists of multiple steps.<para/>
        /// This is due to the time it takes to align the UI components.<para/>
        /// So we have to wait until alignment is finished.
        /// </summary>
        IEnumerator SpawnCoroutine(Action<bool, CodeFile, string> callback) {

            for (uint i = 0; i < 3; i++) {
                
                switch(i) {

                    case 0: 
                        string failure = SpawnCodeWindow();
                        if (failure != null) {
                            Debug.LogError("Spawn step " + i + " failure!");
                            callback(false, null, failure);
                            spawning = false;
                            yield break;
                        }
                        break;

                    case 1:
                        string msg = SpawnRegions();
                        if (msg != null) { Debug.LogWarning(msg); }
                        break;

                    case 2:
                        // notify edge spawner to take care of spawning node edges
                        if (edgeSpawner) { edgeSpawner.CodeWindowSpawnedEvent(spawn_file); }
                        break;
                }

                yield return null;
            }

            // when we arrive here, everything completed successful
            spawning = false;
            Debug.Log("Spawning file completed successful: " + spawn_node.GetName());
            callback(true, spawn_file, "");
            onFileSpawned.Invoke(spawn_file);
        }


        /// <summary>
        /// Spawns the code window.
        /// Returns a failure message on failure or null otherwise.
        /// </summary>
        private string SpawnCodeWindow() {

            // instantiate a new game object and attach to parent
            spawn_window = Instantiate(codeWindowPrefab, spawn_position, spawn_rotation);
            spawn_window.transform.SetParent(codeWindowParent, true);

            // use CodeFileReferences (should already be attached to the code window prefab)
            CodeFileReferences fileRefs = spawn_window.GetComponent<CodeFileReferences>();
            if (!fileRefs) {
                DestroyImmediate(spawn_window);
                return "Spawning file failed! Missing CodeFileReferences component!";
            }
            
            // set the reference to the CodeFileReferences instance
            spawn_file.SetReferences(fileRefs);
            fileRefs.SetCodeFile(spawn_file);

            // disable and enable later in update to fix scroll bar bug
            scrollbarDisabled = false;
            if (fileRefs.GetScrollRect()) {
                fileRefs.GetScrollRect().enabled = false;
                scrollbarDisabled = true;
            }

            // load the actual file content
            if (!LoadFileContent(spawn_node, textPrefab)) {
                spawn_file.SetReferences(null);
                DestroyImmediate(spawn_window);
                return "Failed to load content of file: " + spawn_node.GetFullPath();
            }

            // register this file as spawned using its full path as the key
            spawnedFiles.Add(spawn_node.GetFullPath(), spawn_file);
            return null;
        }


        /// <summary>
        /// Takes care of spawning the code regions.<para/>
        /// This includes creating the visualizations:<para/>
        /// - nfp region marking<para/>
        /// - nfp heightmap<para/>
        /// - feature regions<para/>
        /// Furthermore, according value mappings will be applied.<para/>
        /// Returns a failure message or null on success.
        /// </summary>
        private string SpawnRegions() {

            // add line numbers (use information from read content)
            spawn_file.GetReferences().AddLineNumbers((uint) spawn_file.GetContentInfo().linesRead_total);

            // try to get correct line information (height and so on)
            spawn_file.UpdateLineInfo();

            // enable or disable heightmap based on current application settings
            bool heightMapVisible = ApplicationLoader.GetApplicationSettings().IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP);
            spawn_file.ToggleHeightMap(heightMapVisible);

            // show/hide active feature visualization according to default state in app settings
            spawn_file.ToggleActiveFeatureVis(ApplicationLoader.GetApplicationSettings().GetDefaultActiveFeatureVisState());

            // spawn the regions using the RegionSpawner instance
            if (!regionSpawner) { return "Missing RegionSpawner instance!"; }

            regionSpawner.RefreshRegions(spawn_file, ARProperty.TYPE.NFP, false);
            regionSpawner.RefreshRegions(spawn_file, ARProperty.TYPE.FEATURE, false);

            // apply visual properties / region coloring and scaling accordingly
            new RegionModifier(spawn_file, regionSpawner).ApplyRegionValues();
            return null;
        }


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
                spawn_file.GetReferences().SetLinesTotal(contentInfo.linesRead_total);
                spawn_file.SetContentInfo(contentInfo);

                // save last instance if not done yet
                if (!saved) {
                    SaveTextObject(currentTextObject, sourceCode);
                    Debug.Log("Saved text element " + elementNo + " (lines: " + linesRead + ", chars: " + charactersRead + ")");
                }
            }

            return true;
        }


        /// <summary>
        /// Creates and returns a new text element using TextMeshPro.
        /// </summary>
        public GameObject CreateNewTextElement(string name) {
            GameObject text = Instantiate(textPrefab);
            text.name = name;
            return text;
        }


        /// <summary>
        /// Save the current text object, adding its text and adding it to its parent transform.
        /// </summary>
        void SaveTextObject(GameObject text, string sourceCode) {

            // DONE: spawn the TMP element and attach it to the text container
            // DONE: add the text element to the according variable in the "SpawnedFileInfo" script

            // take care of adding the text
            TextMeshProUGUI tmpgui = text.GetComponent<TextMeshProUGUI>();
            if (tmpgui) {
                tmpgui.SetText(sourceCode);
                spawn_file.GetReferences().AddTextElement(tmpgui.textInfo);
                tmpgui.ForceMeshUpdate(); // force mesh update to calculate line heights instantly
            }
            else {
                TextMeshPro tmp = text.GetComponent<TextMeshPro>();
                if (!tmp) { tmp = text.AddComponent<TextMeshPro>(); }
                tmp.SetText(sourceCode);
                spawn_file.GetReferences().AddTextElement(tmp.textInfo);
                tmp.ForceMeshUpdate(); // force mesh update to calculate line heights instantly
            }

            // set the element parent without keeping world coordinates
            text.transform.SetParent(spawn_file.GetReferences().textContainer, false);
        }


        /// <summary>Refresh all the spawned regions of the given type for all spawned files.</summary>
        public void RefreshSpawnedFileRegions(ARProperty.TYPE propType) {
        
            bool heightMapVisible = ApplicationLoader.GetApplicationSettings().IsNFPVisActive(ApplicationSettings.NFP_VIS.HEIGHTMAP);

            foreach (CodeFile file in GetSpawnedFiles()) {

                // false (default - hides height map) means code marking
                // true means height map visualization (show height map)
                file.ToggleHeightMap(heightMapVisible);

                // refresh the NFP region visualization
                regionSpawner.RefreshRegions(file, propType, true);
            }
        }

        /// <summary>
        /// Refresh represented values of the spawned regions for all spawned files.
        /// (basically re-apply the visual properties)
        /// </summary>
        public void RefreshSpawnedFileRegionValues(ARProperty.TYPE propType) {
            foreach (CodeFile file in GetSpawnedFiles()) {
                regionSpawner.RefreshRegionValues(file, propType);
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
            if (edgeSpawner) { edgeSpawner.CodeWindowRemovedEvent(codeFile); }

            // unregister the file from "spawned" list
            spawnedFiles.Remove(codeFile.GetNode().GetFullPath());

            // destroy code file references
            Destroy(codeFile.GetReferences().gameObject);
            codeFile.SetReferences(null);
            return true;
        }

    }
}
