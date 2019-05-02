using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace VRVis.IO {

    /// <summary>
    /// Abstract base class for "file loaders".<para/>
    /// Such need at least a file path given.
    /// </summary>
    public abstract class FileLoader : BasicLoader {

        private readonly string[] filePaths;


        // CONSTRUCTORS

        public FileLoader(string filePath) {
            filePaths = new string[]{filePath};
        }

        public FileLoader(string[] filePaths) {
            this.filePaths = filePaths;
        }


        // GETTER AND SETTER

        /**
         * Get the file path of the given file
         * or the first file of the list of given files.
         */
        public string GetFilePath() {
            return filePaths[0];
        }

        /**
         * Get the whole list of given files
         * or a list including the only file if only one is given.
         */
        public string[] GetFilePaths() {
            return filePaths;
        }


        // FUNCTIONALITY
        
        public bool FileExists(string path) {
            return File.Exists(path);
        }

    }
}
