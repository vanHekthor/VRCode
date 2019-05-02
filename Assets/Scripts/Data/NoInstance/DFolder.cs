using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace Siro.Testing.IO.Structure {

    //[System.Serializable]
    public class DFolder {

        [SerializeField]
        private string path;

        [SerializeField]
        private string name;
    
        //[SerializeField]
        private List<DFolder> folders = new List<DFolder>();

        //[SerializeField]
        private List<DFile> files = new List<DFile>();


        // CONSTRUCTORS

        public DFolder(DirectoryInfo info) {
            path = info.FullName;
            name = info.Name;
        }

        public DFolder(DirectoryInfo info, List<DFolder> folders, List<DFile> files) {
            path = info.FullName;
            name = info.Name;
            this.folders = folders;
            this.files = files;
        }


        // GETTER AND SETTER

        public string getPath() {
            return path;
        }

        public string getName() {
            return name;
        }

        public List<DFolder> getFolders() {
            return folders;
        }

        public void addFolder(DFolder folder) {
            folders.Add(folder);
        }

        public List<DFile> getFiles() {
            return files;
        }

        public void addFile(DFile file) {
            files.Add(file);
        }

    }
}
