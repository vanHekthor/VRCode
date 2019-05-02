using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Siro.Rendering;
using Siro.IO.Structure;


namespace Siro.IO {

    /**
     * Set up the tree of nodes recursively.
     */
    public class FilesFromDiskNodes : MonoBehaviour {

        public string rootPath; // root path with slash at the end!
        public string rootFolder = "src";
        public string filePattern = "*.rt";
        public int maxFolderDepth = 20;

        public bool spawnStructure = false;
        public SpawnStructure spawnScript;

        [Space]
        [Header("Loaded Information")]
        [SerializeField] private int subFoldersTotal = 0;
        [SerializeField] private int filesTotal = 0;


        // PRIVATE VARIABLES

        private DNode rootNode;


	    // Initialization
	    void Start () {

            // take care of missing slash and create full root path
            if (rootPath.Length > 0 && !rootPath.EndsWith("/") && !rootPath.EndsWith("\\")) {
                rootPath += "/";
            }
            string rootFullPath = rootPath + rootFolder;

            if (rootFullPath.Length == 0) {
                Debug.LogError("Missing root path!");
                return;
            }

            // get sub-directories and files contained in this folder
            DirectoryInfo dirInf = directoryExists(rootFullPath);
            if (dirInf != null) {
                Debug.Log("Root folder exists. Trying to get contents...");
                try {
                    subFoldersTotal = 0;
                    filesTotal = 0;
                    rootNode = getProgramStructure(dirInf);
                    Debug.Log("Getting folder contents finished!");
                }
                catch (System.Exception ex) {
                    Debug.LogError("Failed to parse program structure!");
                    Debug.LogError(ex.StackTrace);
                }
            }
            else {
                Debug.LogError("Root directory (" + rootFullPath + ") not found!");
                return;
            }

            if (rootNode == null) {
                Debug.LogError("Missing root node.");
                return;
            }

            // spawn the structure if spawn script is given
            if (spawnStructure && spawnScript) {
                spawnScript.spawn(rootNode);
            }
            else if (!spawnScript) {
                Debug.LogWarning("Structure not spawned because no spawn script is set!");
            }
            else if (!spawnStructure) {
                Debug.LogWarning("Structure not spawned (disabled).");
            }

	    }
	
        /**
         * Get the directories contained in this given directory path.
         * Make sure you check if the path you pass exists before calling this method!
         * Can e.g. throw a DirectoryNotFoundException.
         */
        DirectoryInfo[] getDirectories(DirectoryInfo dirInf) {
            return dirInf.GetDirectories();
        }

        /**
         * Get the files contained in this directory.
         * Pass "null" as the searchPattern to deactivate filtering.
         */
        FileInfo[] getDirectoryFiles(DirectoryInfo dirInf, string searchPattern) {
            if (searchPattern == null) { return dirInf.GetFiles(); }
            return dirInf.GetFiles(searchPattern);
        }


        /**
         * Check if the path leads to an existing directory.
         * Returns a DirectoryInfo instance or null if the path does not exist.
         */
        DirectoryInfo directoryExists(string dirPath) {
            DirectoryInfo dirInf = new DirectoryInfo(dirPath);
            if (dirInf.Exists) { return dirInf; }
            return null;
        }

        /**
         * Get the structure of the program by traversing the folders recursively.
         * The variable maxFolderDepth will stop the recursion if too long.
         * Setting its value to zero or negative will deactivate this security feature.
         */
        DNode getProgramStructure(DirectoryInfo rootDirectory) {
            int depth = 0;
            DNode root = new DNode("", rootDirectory.FullName, rootDirectory.Name, DNode.DNodeTYPE.FOLDER);
            setStructureRecursively(root, rootDirectory, depth);
            return root;
        }

        /**
         * This method should not be used directly.
         * It is mainly used by "getProgramStructure" and the base of recursion.
         */
        void setStructureRecursively(DNode node, DirectoryInfo dirInf, int depth) {

            // check if the recursion limit is reached
            bool recursionLimitReached = maxFolderDepth > 0 && depth >= maxFolderDepth;

            // get folder files
            FileInfo[] fileInfos = getDirectoryFiles(dirInf, filePattern);
            foreach (FileInfo fi in fileInfos) {
                string relativePath = fi.FullName.Substring(rootPath.Length);
                DNode fileNode = new DNode(relativePath, fi.FullName, fi.Name, DNode.DNodeTYPE.FILE);
                node.addNode(fileNode);
                filesTotal++;
            }

            // iterate over all sub-folders and do the same as previous
            DirectoryInfo[] subFolders = getDirectories(dirInf);
            foreach (DirectoryInfo di in subFolders) {

                // create the folder instance and add it to its root folder
                string relativePath = di.FullName.Substring(rootPath.Length);
                DNode folderNode = new DNode(relativePath, di.FullName, di.Name, DNode.DNodeTYPE.FOLDER);
                node.addNode(folderNode);
                subFoldersTotal++;

                // continue recusion for each sub-folder if limit not reached yet
                if (!recursionLimitReached) {
                    setStructureRecursively(folderNode, di, ++depth);
                }
            }
        }

    }
}