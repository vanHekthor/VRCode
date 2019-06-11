using System.Collections;
using System.Collections.Generic;
using VRVis.Utilities;


namespace VRVis.IO.Structure {

    /// <summary>
    /// Represents a basic node of the software system structure.<para/>
    /// Holds information about a folder or a file.<para/>
    /// Such information is the path, fullPath, name, type (folder or file) and possible sub-nodes.
    /// </summary>
    public class SNode {

        public enum DNodeTYPE {UNKNOWN, FILE, FOLDER};

        private readonly string path;
        private readonly string fullPath;
        private readonly string name;
        private readonly DNodeTYPE nodeType = DNodeTYPE.UNKNOWN;
        private List<SNode> nodes = new List<SNode>();
        private CodeFile codeFile;

        public SNode(string path, string fullPath, string name, DNodeTYPE nodeType) {
            this.path = Utility.GetFormattedPath(path);
            this.fullPath = Utility.GetFormattedPath(fullPath);
            this.name = name;
            this.nodeType = nodeType;
        }

        /// <summary>Path relative to the root folder.</summary>
        public string GetPath() { return path; }

        public string GetFullPath() { return fullPath; }

        /// <summary>Original filename as given by the file system (e.g. no lower case)</summary>
        public string GetName() { return name; }

        public DNodeTYPE GetNodeType() { return nodeType; }

        /// <summary>Get child nodes.</summary>
        public List<SNode> GetNodes() { return nodes; }

        /// <summary>Amount of child nodes.</summary>
        public int GetNodesCount() { return nodes.Count; }

        /// <summary>Add a child node.</summary>
        public void AddNode(SNode node) { nodes.Add(node); }

        /// <summary>Get the code file regarding this node.</summary>
        public CodeFile GetCodeFile() { return codeFile; }
        public void SetCodeFile(CodeFile cf) { codeFile = cf; }

    }
}
