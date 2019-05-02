using System.Collections;
using System.Collections.Generic;


namespace Siro.IO.Structure {

    public class DNode {

        public enum DNodeTYPE {UNKNOWN, FILE, FOLDER};

        private string path;
        private string fullPath;
        private string name;
        private DNodeTYPE type = DNodeTYPE.UNKNOWN;
        private List<DNode> nodes = new List<DNode>();

        public DNode(string path, string fullPath, string name, DNodeTYPE type) {
            this.path = path;
            this.fullPath = fullPath;
            this.name = name;
            this.type = type;
        }

        /** Path relative to root folder. */
        public string getPath() {
            return path;
        }

        public string getFullPath() {
            return fullPath;
        }

        public string getName() {
            return name;
        }

        public DNodeTYPE getType() {
            return type;
        }

        public List<DNode> getNodes() {
            return nodes;
        }

        public void addNode(DNode node) {
            nodes.Add(node);
        }

    }
}
