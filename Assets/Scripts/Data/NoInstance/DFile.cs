using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


namespace Siro.Testing.IO.Structure {

    [System.Serializable]
    public class DFile {

        [SerializeField]
        private string path;

        [SerializeField]
        private string name;


        // CONSTRUCTORS

        public DFile(FileInfo info) {
            path = info.FullName;
            name = info.Name;
        }


        // GETTER AND SETTER

        public string getPath() {
            return path;
        }

        public string getName() {
            return name;
        }

    }
}
