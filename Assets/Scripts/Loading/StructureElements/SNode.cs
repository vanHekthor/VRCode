using System.Collections;
using System.Collections.Generic;
using VRVis.Spawner.Layouts;
using VRVis.Utilities;


namespace VRVis.IO.Structure {

    /// <summary>
    /// Represents a basic node of the software system structure.<para/>
    /// Holds information about a folder or a file.<para/>
    /// Such information is the path, fullPath, name, type (folder or file) and possible sub-nodes.<para/>
    /// Last Updated: 22.08.2019
    /// </summary>
    public class SNode : GenericNode {

        public enum DNodeTYPE { UNKNOWN, FILE, FOLDER };

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

        public string GetRelativePath() {
            string relativePath = path;
            int index = relativePath.LastIndexOf("/");
            if (index >= 0)
                relativePath = relativePath.Substring(0, index);
            relativePath += "/" + name;

            return relativePath;
        }

        public string GetFullPath() { return fullPath; }

        /// <summary>Original filename as given by the file system (e.g. no lower case)</summary>
        public string GetName() { return name; }

        public DNodeTYPE GetNodeType() { return nodeType; }

        /// <summary>Get child nodes.</summary>
        public override IEnumerable GetNodes() { return nodes; }

        /// <summary>Amount of child nodes.</summary>
        public override int GetNodesCount() { return nodes.Count; }

        /// <summary>Tells if this node is a leaf node by checking the amount of child nodes.</summary>
        public override bool IsLeaf() { return nodes.Count < 1; }

        /// <summary>Add a child node.</summary>
        public void AddNode(SNode node) { nodes.Add(node); }

        /// <summary>Get the code file regarding this node.</summary>
        public CodeFile GetCodeFile() { return codeFile; }
        public void SetCodeFile(CodeFile cf) { codeFile = cf; }

        /// <summary>Tells if this node is of type folder.summary>
        public bool IsFolder() { return nodeType == DNodeTYPE.FOLDER; }

        /// <summary>Tells if this node is of type file.summary>
        public bool IsFile() { return nodeType == DNodeTYPE.FILE; }

    }
}
