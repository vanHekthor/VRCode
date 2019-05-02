using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Siro.Rendering;
using Siro.Testing.IO.Structure;


namespace Siro.IO {

    /**
     * DEPRECATED (OLD CODE)!
     * Use FileFromDiskNodes instead!
     */
    public class FilesFromDisk : MonoBehaviour {

        public string rootPath;
        public string filePattern = "*.json";
        public int maxFolderDepth = 20;

        public bool spawnStructure = false;
        public SpawnStructure spawnScript;

        [Space]
        [Header("Loaded Information")]
        [SerializeField] private int subFoldersTotal = 0;
        [SerializeField] private int filesTotal = 0;

        private DFolder rootFolder;


	    // Initialization
	    void Start () {

            if (rootPath.Length == 0) {
                Debug.LogError("Missing root path!");
                return;
            }

            // get sub-directories and files contained in this folder
            DirectoryInfo dirInf = directoryExists(rootPath);
            if (dirInf != null) {
                Debug.Log("Root folder exists. Trying to get contents...");
                try {
                    subFoldersTotal = 0;
                    filesTotal = 0;
                    rootFolder = getProgramStructure(dirInf);
                    Debug.Log("Getting folder contents finished!");
                }
                catch (System.Exception ex) {
                    Debug.LogError("Failed to parse program structure!");
                    Debug.LogError(ex.StackTrace);
                }
            }
            else {
                Debug.LogError("Root directory (" + rootPath + ") not found!");
                return;
            }

            if (rootFolder == null) {
                Debug.LogError("Missing root folder.");
                return;
            }

            // spawn the structure if spawn script is given
            /*if (spawnStructure && spawnScript) {
                spawnScript.spawn(rootFolder);
            }
            else {
                Debug.LogWarning("Structure not spawned because no spawn script is set!");
            }
            */

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
        DFolder getProgramStructure(DirectoryInfo rootDirectory) {
            int depth = 0;
            DFolder mainFolder = new DFolder(rootDirectory);
            setStructureRecursively(mainFolder, rootDirectory, depth);
            return mainFolder;
        }

        /**
         * This method should not be used directly.
         * It is mainly used by "getProgramStructure" and the base of recursion.
         */
        void setStructureRecursively(DFolder rootFolder, DirectoryInfo directory, int depth) {

            // check if the recursion limit is reached
            bool recursionLimitReached = maxFolderDepth > 0 && depth >= maxFolderDepth;

            // get folder files
            FileInfo[] fileInfos = getDirectoryFiles(directory, filePattern);
            foreach (FileInfo fi in fileInfos) {
                DFile file = new DFile(fi);
                rootFolder.addFile(file);
                filesTotal++;
            }

            // iterate over all sub-folders and do the same as previous
            DirectoryInfo[] subFolders = getDirectories(directory);
            foreach (DirectoryInfo dirInf in subFolders) {

                // create the folder instance and add it to its root folder
                DFolder subFolder = new DFolder(dirInf);
                rootFolder.addFolder(subFolder);
                subFoldersTotal++;

                // continue recusion for each sub-folder if limit not reached yet
                if (!recursionLimitReached) {
                    setStructureRecursively(subFolder, dirInf, ++depth);
                }
            }
        }

    }
}