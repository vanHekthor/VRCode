using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using VRVis.Utilities;
using VRVis.IO.Structure;
using VRVis.JSON.Serialization.Configuration;
using System.Text.RegularExpressions;


namespace VRVis.IO {

    /// <summary>
    /// Set up the tree of nodes of the software system recursively.<para/>
    /// The settings of this instance might be changed/initialized by the ApplicationLoader script
    /// because most of them are set in the global application configuration file.
    /// </summary>
    public class StructureLoader : BasicLoader {

        // path to the folder that includes the root folder of the analyzed software system
        private string softwareSystemPath; // root path with slash at the end!

        // name of the root folder (e.g. "src")
        private string rootFolder = "src";

        // maximum recursive folder depth
        private int maxFolderDepth = 20;

        // Array of strings (regex/wildcard) that tell about files that should be excluded.
        // Used for validation will be the relative file path.
        private string[] excludeFiles;

        // Extensions that should be removed if they occur in the file name.
        // It will affect the value of the "fixedName" attribute in later file information.
        private string[] removeExtensions;

        // loaded information
        private int subFoldersTotal = 0;
        private int filesTotal = 0;

        // full path to root folder and root node that represents it
        private string rootFullPath;
        private SNode rootNode;

        /// <summary>
        /// Holds all the code files that exist in the software system to visualize.<para/>
        /// Key is always the FULL PATH to the file to ensure uniqueness.
        /// </summary>
        private Dictionary<string, CodeFile> files = new Dictionary<string, CodeFile>();

        /// <summary>
        /// Holds as key a relative file path and as value the according full file path.<para/>
        /// This is required to get a code file by its relative path (get full or relative path).
        /// </summary>
        private Dictionary<string, string> filesFullPath = new Dictionary<string, string>();

        /// <summary>
        /// Stores the global min/max values for each non functional property (NFP) (key = prop. name).
        /// </summary>
        private Dictionary<string, MinMaxValue> nfpMinMaxValues = new Dictionary<string, MinMaxValue>();


        // CONSTRUCTOR

        public StructureLoader() {}


        // GETTER AND SETTER

        public string GetSoftwareSystemPath() { return softwareSystemPath; }
        public void SetSoftwareSystemPath(string path) { softwareSystemPath = path; }

        public string GetRootFolder() { return rootFolder; }
        public void SetRootFolder(string folderName) { rootFolder = folderName;}

        public int GetMaxFolderDepth() { return maxFolderDepth; }
        public void SetMaxFolderDepth(int depth) { maxFolderDepth = depth > 0 ? depth : 0; }

        /// <summary>Extensions that should be removed from displayed file names.</summary>
        public string[] GetRemoveExtensions() { return removeExtensions; }
        public void SetRemoveExtensions(string[] extensionsToRemove) {
            removeExtensions = extensionsToRemove;

            // convert extensions to lowercase
            for (int i = 0; i < removeExtensions.Length; i++) {
                removeExtensions[i] = removeExtensions[i].ToLower();
            }
        }

        /// <summary>Relative file path regex patterns telling about files to exclude.</summary>
        public string[] GetExcludeFiles() { return excludeFiles; }
        public void SetExcludeFiles(string[] excludeFiles) { this.excludeFiles = excludeFiles; }

        public string GetFullRootPath() { return rootFullPath; }

        /// <summary>Get the root node of the software system file structure.</summary>
        public SNode GetRootNode() { return rootNode; }

        /// <summary>Get amount of loaded files.</summary>
        public int GetFileCount() { return files.Count; }

        /// <summary>Get all code file instances as a list.</summary>
        public IEnumerable<CodeFile> GetFiles() { return files.Values; }

        /// <summary>Get the according CodeFile instance or null if the file does not exist.</summary>
        public CodeFile GetFileByFullPath(string filePath) {
            if (!files.ContainsKey(filePath)) { return null; }
            return files[filePath];
        }

        /// <summary>
        /// Get the according CodeFile instance or null if the file does not exist.<para/>
        /// Ensure the passed path is in lowercase!
        /// </summary>
        public CodeFile GetFileByRelativePath(string relativeFilePath) {
            if (!filesFullPath.ContainsKey(relativeFilePath)) { return null; }
            return GetFileByFullPath(filesFullPath[relativeFilePath]);
        }

        /// <summary>Get min/max values of all non functional properties.</summary>
        public Dictionary<string, MinMaxValue> GetNFPMinMaxValues() { return nfpMinMaxValues; }

        /// <summary>Returns the min/max value of this NFP or null if not found.</summary>
        public MinMaxValue GetNFPMinMaxValue(string propertyName) {
            if (!nfpMinMaxValues.ContainsKey(propertyName)) { return null; }
            return nfpMinMaxValues[propertyName];
        }

        /// <summary>
        /// Add min/max value instance for this property.<para/>
        /// Returns true if added, false if it already exists!
        /// </summary>
        public bool AddNFPMinMaxValue(string propertyName, MinMaxValue minMax) {
            if (nfpMinMaxValues.ContainsKey(propertyName)) { return false; }
            nfpMinMaxValues.Add(propertyName, minMax);
            return true;
        }


        // FUNCTIONALITY

        /// <summary>Apply the given configuration to this instance.</summary>
        public bool ApplyConfiguration(JSONSoftwareSystemConfig config, string configPath) {

            // get software system path (if "." or "./" given, use folder that contains app configuraiton file
            string syspath = config.path;
            if (syspath == "." || syspath == "./") {
                try { syspath = Directory.GetParent(configPath).FullName; }
                catch (System.Exception ex) {
                    Debug.LogError("Software system path could not be retrieved from config file!\n" + ex.Message);
                    return false;
                }
            }

            // validate path by length
            if (syspath.Trim().Length == 0 || !Directory.Exists(syspath)) {
                Debug.LogError("Invalid software system path");
                return false;
            }

            SetSoftwareSystemPath(syspath);
            SetRootFolder(config.root_folder);
            SetMaxFolderDepth(config.max_folder_depth);
            SetRemoveExtensions(config.remove_extensions);
            SetExcludeFiles(config.ignore_files);

            return true;
        }
        
        /// <summary>Load the folder/file structure of the software system.</summary>
        public override bool Load() {
            
            loadingSuccessful = false;

            // prepare and check for correct paths...
            if (!Prepare()) { return false; }

            // get sub-directories and files contained in this folder
            DirectoryInfo dirInf = Utility.GetDirectoryInfo(rootFullPath);
            if (dirInf != null) {

                Debug.Log("Root folder exists! Trying to get its contents...");
                try {
                    subFoldersTotal = 0;
                    filesTotal = 0;
                    rootNode = GetProgramStructureRecursively(dirInf);
                    Debug.Log("Retrieving folder contents finished!");
                }
                catch (System.Exception ex) {
                    Debug.LogError("Failed to parse program structure!");
                    Debug.LogError(ex.StackTrace);
                    return false;
                }
            }
            else {
                Debug.LogError("Root directory (" + rootFullPath + ") not found!");
                return false;
            }

            if (rootNode == null) {
                Debug.LogError("Missing root node.");
                return false;
            }


            // ToDo: debug code - remove if no longer required
            //Debug.Log("################ Relative files loaded:");
            //foreach (string relpath in filesFullPath.Keys) {
            //    Debug.Log("RELPATH: " + relpath);
            //}


            // print some debug info about the load process
            Debug.Log("Loading software system structure finished successfully.\n" +
                "(Folders: " + subFoldersTotal +
                ", Files on disk: " + filesTotal +
                ", Code Files: " + files.Count + ")"
            );
            loadingSuccessful = true;
            return true;
        }

        /// <summary>Prepare the structure loader (e.g. fix paths)</summary>
        private bool Prepare() {

            // take care of missing slash and create full root path
            if (softwareSystemPath.Length > 0 && !softwareSystemPath.EndsWith("/") && !softwareSystemPath.EndsWith("\\")) {
                softwareSystemPath += "/";
            }
            rootFullPath = Utility.GetFormattedPath(softwareSystemPath + rootFolder);

            if (rootFullPath.Length == 0) {
                Debug.LogError("Missing root path!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the directories contained in this given directory path.<para/>
        /// Make sure you check if the path you pass exists before calling this method!<para/>
        /// Can e.g. throw a DirectoryNotFoundException.
        /// </summary>
        private DirectoryInfo[] GetDirectories(DirectoryInfo dirInf) {
            return dirInf.GetDirectories();
        }

        /// <summary>
        /// Get the files contained in this directory.<para/>
        /// Pass "null" as the searchPattern to deactivate filtering.
        /// </summary>
        private FileInfo[] GetDirectoryFiles(DirectoryInfo dirInf, string searchPattern) {
            if (searchPattern == null) { return dirInf.GetFiles(); }
            return dirInf.GetFiles(searchPattern);
        }

        /// <summary>
        /// Get the structure of the program by traversing the folders recursively.<para/>
        /// The variable maxFolderDepth will stop the recursion if too long.<para/>
        /// Setting its value to zero or negative will deactivate this security feature.
        /// </summary>
        private SNode GetProgramStructureRecursively(DirectoryInfo rootDirectory) {
            int depth = 0;
            SNode root = new SNode("", rootDirectory.FullName, rootDirectory.Name, SNode.DNodeTYPE.FOLDER);
            SetStructureRecursively(root, rootDirectory, depth);
            return root;
        }

        /// <summary>
        /// This method should not be used directly.<para/>
        /// It is mainly used by "GetProgramStructure" (the base of recursion).
        /// </summary>
        private void SetStructureRecursively(SNode node, DirectoryInfo dirInf, int depth) {

            // check if the recursion limit is reached
            bool recursionLimitReached = maxFolderDepth > 0 && depth >= maxFolderDepth;

            // get folder files
            try {
                FileInfo[] fileInfos = GetDirectoryFiles(dirInf, null);
                foreach (FileInfo fi in fileInfos) {

                    // get full path (take care of "/" and convert to lower)
                    string fullPath = Utility.GetFormattedPath(fi.FullName);

                    // skip this node if it should be excluded
                    if (IsExcluded(fullPath)) {
                        //Debug.Log("[Excluded] Skipping file node: " + fullPath);
                        continue;
                    }

                    // get path relative to software system root
                    string relativePath = fi.FullName.Substring(softwareSystemPath.Length);
                    relativePath = Utility.GetFormattedPath(relativePath);

                    // remove extensions from the path and name (e.g. ".rt")
                    string name = RemoveExtension(fi.Name);
                    relativePath = RemoveExtension(relativePath).ToLower(); // lower case required

                    // (do not remove extension from full path!)
                    // (previously done -> bug because not loading the ".rt" file!)
                    //fullPath = RemoveExtension(fullPath);

                    // create node instance
                    SNode fileNode = new SNode(relativePath, fullPath, name, SNode.DNodeTYPE.FILE);
                    files.Add(fullPath, new CodeFile(fileNode)); // CREATE CODE FILE INSTANCE (! IMPORTANT STEP !)
                    filesFullPath.Add(relativePath, fullPath);
                    node.AddNode(fileNode);
                    filesTotal++;
                }
            }
            catch (System.Exception ex) {
                Debug.LogError("Failed to read folder contents (path: " + node.GetFullPath() + ")!");
                Debug.LogError(ex.StackTrace);
                return;
            }

            // iterate over all sub-folders and do the same as previous
            DirectoryInfo[] subFolders = GetDirectories(dirInf);
            foreach (DirectoryInfo di in subFolders) {

                // get full path (take care of "/" and convert to lower)
                string fullPath = Utility.GetFormattedPath(di.FullName);

                // skip this node if it should be excluded
                if (IsExcluded(fullPath)) {
                    Debug.Log("[Excluded] Skipping folder node: " + fullPath);
                    continue;
                }

                // get path relative to software system root
                string relativePath = di.FullName.Substring(softwareSystemPath.Length);
                relativePath = Utility.GetFormattedPath(relativePath);

                // no need to remove extensions for folders
                string name = di.Name; // original name (no lower case)
                relativePath = relativePath.ToLower(); // lower case required

                // (do not remove extension from full path!)
                // (previously done -> bug because not loading the ".rt" file!)
                //fullPath = RemoveExtension(fullPath);

                // create the folder instance and add it to its root folder
                SNode folderNode = new SNode(relativePath, fullPath, name, SNode.DNodeTYPE.FOLDER);
                node.AddNode(folderNode);
                subFoldersTotal++;

                // continue recusion for each sub-folder if limit not reached yet
                if (!recursionLimitReached) {
                    SetStructureRecursively(folderNode, di, ++depth);
                }
            }
        }


        /// <summary>
        /// Checks if the node is valid to the configuration.<para/>
        /// This means that if the pattern of the files/folders path
        /// matches a pattern of the "exclude" list,
        /// this node wont be valid and ignored in the structure.
        /// </summary>
        private bool IsExcluded(string path) {
            bool excluded = false;
            foreach (string pattern in GetExcludeFiles()) {
                if (Regex.IsMatch(path, pattern)) {
                    //Debug.Log("Matching pattern (" + pattern + "): " + path); // DEBUG
                    excluded = true;
                    break;
                }
            }
            return excluded;
        }


        /// <summary>
        /// Removes extensions from the given paths.<para/>
        /// Looks up the list of "remove_extension" to do so.<para/>
        /// </summary>
        private string RemoveExtension(string path) {
            foreach (string extension in GetRemoveExtensions()) {
                if (path.ToLower().EndsWith(extension.ToLower())) {
                    return path.Remove(path.Length - extension.Length);
                }
            }
            return path;
        }

    }
}
