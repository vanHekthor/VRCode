using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRVis.Elements;
using VRVis.JSON.Serialization;
using VRVis.Utilities;

namespace VRVis.IO {

    /// <summary>
    /// Loads edges that connect nodes/regions with other nodes/regions.<para/>
    /// A node is considered to be a line of code
    /// so that an edge is a connection between two lines
    /// either of the same, or of two distinct files.<para/>
    /// </summary>
    public class EdgeLoader : FileLoader {

        // edges files naming convention definition search pattern
        // https://docs.microsoft.com/de-de/dotnet/api/system.io.directoryinfo.getfiles?view=netframework-4.7.2#System_IO_DirectoryInfo_GetFiles_System_String_System_IO_SearchOption_
        private static readonly string namingConvention = "edges_*.json";

        /// <summary>key = edge id, value = edge instance</summary>
        private Dictionary<uint, Edge> edges = new Dictionary<uint, Edge>();

        /// <summary>information about a type of edges</summary>
        public class EdgeTypeInfo {
            public HashSet<uint> edgeIDs = new HashSet<uint>();
            public MinMaxValue valueMinMax = new MinMaxValue();
        }

        /// <summary>Key = edge type, Value = list of ids of according edges</summary>
        private Dictionary<string, EdgeTypeInfo> edgeTypes = new Dictionary<string, EdgeTypeInfo>();

        /// <summary>Key = code file instance, Value = list of ids of according edges</summary>
        private Dictionary<Tuple<CodeFile, string>, HashSet<uint>> fileEdges = new Dictionary<Tuple<CodeFile, string>, HashSet<uint>>();

        /// <summary>Path of the currently loaded file</summary>
        private string curFile = "";
        private int edgesTotal = 0;
        private int edgesFailed = 0;
        uint edgeID = 0;



        // CONSTRUCTOR
        
	    public EdgeLoader(string[] filePaths)
        : base(filePaths) {}

        public EdgeLoader(string filePath)
        : this(new string[]{filePath}) {}



        // GETTER AND SETTER
        
        public IEnumerable<Edge> GetEdges() { return edges.Values; }

        /// <summary>
        /// Get a list of edges with the specific type.<para/>
        /// Case of input does not matter. It will be converted to lower case.<para/>
        /// Returns null if this type does not exist.
        /// </summary>
        public IEnumerable<Edge> GetEdges(string type) {
            
            type = type.ToLower();
            if (!edgeTypes.ContainsKey(type)) { return null; }

            // gather all the according edge instances
            List<Edge> list = new List<Edge>();
            foreach (uint edgeID in edgeTypes[type].edgeIDs) {
                if (!edges.ContainsKey(edgeID)) { continue; }
                list.Add(edges[edgeID]);
            }

            return list;
        }

        /// <summary>
        /// Get outgoing edges that belong to this file.<para/>
        /// Converts the input path to lowercase.<para/>
        /// Returns null if there are no edges for this file!
        /// </summary>
        public IEnumerable<Edge> GetEdgesOfFile(CodeFile codeFile, string configName) {
            var key = Tuple.Create(codeFile, configName);
            if (!fileEdges.ContainsKey(key)) { return null; }

            // gather all the according edge instances
            List<Edge> list = new List<Edge>();
            foreach (uint edgeID in fileEdges[key]) {
                if (!edges.ContainsKey(edgeID)) { continue; }
                list.Add(edges[edgeID]);
            }

            return list;
        }

        /// <summary>
        /// Returns the amount of outgoing edges of this file.
        /// </summary>
        public int GetEdgeCountOfFile(CodeFile codeFile, string configName) {
            var key = Tuple.Create(codeFile, configName);
            if (!fileEdges.ContainsKey(key)) { return 0; }
            return fileEdges[key].Count;
        }

        /// <summary>Returns the edge or null if not found.</summary>
        public Edge GetEdgeByID(uint id) {
            if (!edges.ContainsKey(id)) { return null; }
            return edges[id];
        }

        /// <summary>
        /// Get the min/max value for this edge type or null if not found.
        /// </summary>
        public MinMaxValue GetEdgeTypeMinMax(string edgeType) {

            edgeType = edgeType.ToLower();
            if (!edgeTypes.ContainsKey(edgeType)) { return null; }
            return edgeTypes[edgeType].valueMinMax;
        }

        /// <summary>Returns all types of edges.</summary>
        public IEnumerable<string> GetEdgeTypes() { return edgeTypes.Keys; }



        // FUNCTIONALITY

        /// <summary>
        /// Used by ApplicationLoader to find files that are
        /// named according to naming convention ("edges_<name>.json").<para/>
        /// Returns a list of files that follow the convention.<para/>
        /// Returns null on errors so check for that before using the list!
        /// </summary>
        public static List<string> GetEdgeFiles(string path) {

            DirectoryInfo dirInf = new DirectoryInfo(path);

            if (!dirInf.Exists) {
                Debug.LogError("Folder with edges files does not exist: " + path);
                return null;
            }

            // get valid files and add full path to list
            FileInfo[] files = dirInf.GetFiles(namingConvention, SearchOption.AllDirectories);
            List<string> paths = new List<string>();
            foreach (FileInfo fi in files) { paths.Add(fi.FullName); }
            return paths;
        }


        /// <summary>
        /// Load the edges from the files.
        /// </summary>
        public override bool Load() {
            
            loadingSuccessful = false;

            // prepare loading
            edges.Clear();
            edgeTypes.Clear();
            fileEdges.Clear();

            edgeID = 0;
            edgesTotal = 0;
            edgesFailed = 0;

            uint success = 0;
            uint total = 0;

            // try to load edges from each file
            foreach (string filePath in GetFilePaths()) {
                total++;
                curFile = filePath;
                if (LoadFile(filePath)) { success++; }
                else { Debug.LogWarning("Failed to load edges from file: " + filePath); }
            }

            // print debug result information
            string msg = "Finished loading " + success + "/" + total + " edge files!\n(" +
                "edges loaded: " + (edgesTotal - edgesFailed) + "/" + edgesTotal + "; " +
                "edge types: " + edgeTypes.Count + ")";
            if (success != total || edgesFailed > 0) { Debug.LogWarning(msg); } else { Debug.Log(msg); }

            loadingSuccessful = true;
            return true;
        }
    

        /// <summary>
        /// Loads a single edges file and returns true on success.
        /// </summary>
        private bool LoadFile(string filePath) {
            // name of the corresponding configuration is the name of the parent directory
            string parentDirName = new DirectoryInfo(filePath).Parent.Name;

            if (!File.Exists(filePath)) {
                Debug.LogError("Edges file does not exist! (" + filePath + ")");
                return false;
            }

            // deserialize the JSON data to objects
            Debug.Log("Loading edges from JSON file: " + filePath);
            string edgeData = File.ReadAllText(filePath);
            JSONEdges jsonEdges = JsonUtility.FromJson<JSONEdges>(edgeData);
            int edgeCount = jsonEdges.edges != null ? jsonEdges.edges.Length : 0;
            int edgesLoaded = 0;

            // convert jsonEdges to Edge instances
            Debug.Log("JSON edges loaded (" + edgeCount + "). Converting data...");
            
            for (int i = 0; i < edgeCount; i++) {

                JSONEdge jsonEdge = jsonEdges.edges[i];

                // [INDEXING] add edge instance for the current id
                Edge edgeInstance = new Edge(edgeID, jsonEdge);
                
                // get according code file (target file wont be checked)
                string edgeFromFile = edgeInstance.GetFrom().file.ToLower();
                CodeFile codeFile = ApplicationLoader.GetInstance().GetStructureLoader().GetFileByRelativePath(edgeFromFile);
                if (codeFile == null) {
                    Debug.LogError("Failed to load edge (file: " + curFile + ", entry: " + i + ") - File is unknown: " + edgeFromFile + "!");
                    continue;
                }

                // [INDEXING] add edge ID relation to code file
                var key = Tuple.Create(codeFile, parentDirName);
                if (!fileEdges.ContainsKey(key)) {
                    fileEdges.Add(key, new HashSet<uint>());
                }
                fileEdges[key].Add(edgeID);


                // ToDo: maybe check if there is already an edge leading to exactly the same location


                // [INDEXING] add edge ID to list of edges with this type
                EdgeTypeInfo eType = null;
                string edgeType = edgeInstance.GetEdgeType().ToLower();
                if (!edgeTypes.ContainsKey(edgeType)) {
                    eType = new EdgeTypeInfo();
                    edgeTypes.Add(edgeType, eType);
                }
                else { eType = edgeTypes[edgeType]; }

                // add edge ID to type info
                eType.edgeIDs.Add(edgeInstance.GetID());

                // sum up required information (e.g. min/max of a type)
                eType.valueMinMax.Update(edgeInstance.GetValue());

                // add edge instance
                edges.Add(edgeID, edgeInstance);
                edgeID++;

                // local counter
                edgesLoaded++;
            }

            string msg = "Edges loaded from file: " + edgesLoaded + "/" + edgeCount;
            if (edgesLoaded != edgeCount || edgeCount == 0) { Debug.LogWarning(msg); } else { Debug.Log(msg); }
            
            edgesTotal += edgeCount;
            edgesFailed += (edgeCount - edgesLoaded);
            return true;
        }

    }
}
