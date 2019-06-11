using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Spawner.Edges;

namespace VRVis.Spawner {

    /// <summary>
    /// This script takes care of setting up the visible edges
    /// that connect nodes across or inside a code window.<para/>
    /// It will be notified by the FileSpawner script when code window related events occur.<para/>
    /// This script should be attached to the same object that holds the FileSpawner instance.<para/>
    /// There should only be one instance of this class!
    /// Ensure to check if this is the case, because the script currently does not prevent it.
    /// </summary>
    public class CodeWindowEdgeSpawner : ASpawner {

        [Tooltip("The edge representing GameObject with the LineRenderer component")]
	    public GameObject edgeConnectionPrefab;

        /// <summary>
        /// Stores instances of connection instances of spawned edges.<para/>
        /// key = edge id, value = connection instance
        /// </summary>
        private Dictionary<uint, CodeWindowEdgeConnection> edgeConnections = new Dictionary<uint, CodeWindowEdgeConnection>();

        /// <summary>Stores ids (value) of spawned edges starting from the code file (key)</summary>
        private Dictionary<CodeFile, HashSet<uint>> spawnedEdges = new Dictionary<CodeFile, HashSet<uint>>();

        /// <summary>Stores all the currently spawned edge types. This is required when active edge types change.</summary>
        //private HashSet<string> spawnedEdgeTypes = new HashSet<string>();

        private EdgeLoader edgeLoader = null;


        // Startup of this component
        void Start() {
            edgeLoader = GetEdgeLoader();
        }



        // GETTER AND SETTER

        /// <summary>
        /// Get spawned edge connections.<para/>
        /// key = edge id, value = connection instance
        /// </summary>
        public Dictionary<uint, CodeWindowEdgeConnection> GetSpawnedEdgeConnections() { return edgeConnections; }

        /// <summary>
        /// Get code window edge connection starting at this code file.<para/>
        /// Returns a list that can also be empty if no according connection instances could be found!
        /// </summary>
        public IEnumerable<CodeWindowEdgeConnection> GetSpawnedEdgeConnections(CodeFile codeFile) {

            if (!spawnedEdges.ContainsKey(codeFile)) { return null; }
            return GetSpawnedEdgeConnections(spawnedEdges[codeFile]);
        }

        /// <summary>
        /// Get the code window edge connections for all IDs in the passed list.<para/>
        /// The returned list can have less items if some instances could not be found.
        /// As a result, the order is not guaranteed to be preserved!
        /// </summary>
        public IEnumerable<CodeWindowEdgeConnection> GetSpawnedEdgeConnections(IEnumerable<uint> edgeIDs) {

            List<CodeWindowEdgeConnection> listOut = new List<CodeWindowEdgeConnection>();

            foreach (uint conID in edgeIDs) {
                if (edgeConnections.ContainsKey(conID)) {
                    listOut.Add(edgeConnections[conID]);
                }
            }

            return listOut;
        }

        /// <summary>
        /// Get the spawned edge connection for this edge ID.<para/>
        /// Returns null if there is none!
        /// </summary>
        public CodeWindowEdgeConnection GetSpawnedEdgeConnection(uint edgeID) {

            if (!edgeConnections.ContainsKey(edgeID)) { return edgeConnections[edgeID]; }
            return null;
        }



        // FUNCTIONALITY

        /// <summary>
        /// Try to get the edge loader from application loader.<para/>
        /// Returns null if not found!
        /// </summary>
        private EdgeLoader GetEdgeLoader() {
            return ApplicationLoader.GetInstance().GetEdgeLoader();
        }


        /// <summary>
        /// Called by FileSpawner on code window creation.<para/>
        /// Creates edges that go from this file to other currently shown files,
        /// and the outgoing edges of other files ending in this file.
        /// </summary>
        public void CodeWindowSpawnedEvent(CodeFile codeFile) {
            
            // edge loader is required
            if (edgeLoader == null) { edgeLoader = GetEdgeLoader(); }
            if (edgeLoader == null) { return; }

            // structure loader is required to get target code files
            StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
            if (structureLoader == null) { return; }


            // check if there is "garbage" to clean up or initialize hash set for this file
            if (spawnedEdges.ContainsKey(codeFile)) { spawnedEdges[codeFile].Clear(); }
            else { spawnedEdges.Add(codeFile, new HashSet<uint>()); }


            // nothing to do yet because there are no other files to connect to
            // [10.02.2019 - Update] Required if the file has edges to itself!
            //if (spawnedEdges.Count == 1) { return; }


            // add the missing edges
            uint failedSpawns = 0;
            uint newEdgesSpawned = 0;
            AddMissingEdges(structureLoader, out failedSpawns, out newEdgesSpawned);


            // log infos and warn if some failed to spawn
            string msg = "New edges spawned: " + newEdgesSpawned + " for file: " +
                codeFile.GetNode().GetName() + " (failed: " + failedSpawns + ")";
            if (failedSpawns > 0) { Debug.LogWarning(msg); } else { Debug.Log(msg); }
        }


        /// <summary>Returns true if this edge type is currently active.</summary>
        private bool IsEdgeTypeActive(string edgeType) {
            return ApplicationLoader.GetApplicationSettings().IsEdgeTypeActive(edgeType);
        }


        /// <summary>
        /// Add missing edges after the spawnedEdges dictionary changed (e.g. new file added).
        /// </summary>
        private void AddMissingEdges(StructureLoader sLoader, out uint failedSpawns, out uint newEdgesSpawned) {

            // keep entries that should be removed from spawnedEdges  
            List<CodeFile> removeFailed = new List<CodeFile>(); 

            // check which other files are already spawned
            // and add missing connections for all of them
            failedSpawns = 0;
            newEdgesSpawned = 0;
            foreach (KeyValuePair<CodeFile, HashSet<uint>> entry in spawnedEdges) {

                // check if references exist - file is probably not spawned so clean up
                if (!entry.Key.GetReferences()) {
                    removeFailed.Add(entry.Key);
                    continue;
                }

                // edges can be null if there a none defined
                IEnumerable<Edge> edges = edgeLoader.GetEdgesOfFile(entry.Key);
                if (edges == null) { continue; }

                foreach (Edge edge in edges) {
                    
                    // skip if this edge type is not active/selected to be shown
                    if (!IsEdgeTypeActive(edge.GetEdgeType())) { continue; }

                    // this edge connection already exists / is already spawned
                    if (entry.Value.Contains(edge.GetID())) { continue; }

                    // try to create the connection instance
                    CodeWindowEdgeConnection edgeCon = null;
                    int state = SpawnEdgeConnection(entry.Key, edge, sLoader, out edgeCon);
                    if (state < 1) {
                        if (state == -1) { failedSpawns++; }
                        continue;
                    }

                    edgeConnections.Add(edge.GetID(), edgeCon);
                    entry.Value.Add(edge.GetID());
                    newEdgesSpawned++;
                }
            }

            // remove entries from spawnedEdges dict. that have no valid references
            foreach (CodeFile cf in removeFailed) { spawnedEdges.Remove(cf); }
        }


        /// <summary>
        /// Called by FileSpawner on code window deletion.<para/>
        /// Removes edges going from and to this code file.
        /// </summary>
        public void CodeWindowRemovedEvent(CodeFile codeFile) {
            
            // Steps of cleanup:
            // 1. get all edges that are currently spawned
            // 2. check if the edge originates from this file -> remove connection instance
            // 3. check if the edge connection ends at this file -> remove connection instance

            // list to gather the IDs of edge connection that we have to remove from the scene
            HashSet<uint> conInstancesToRemove = new HashSet<uint>();

            foreach (KeyValuePair<CodeFile, HashSet<uint>> entry in spawnedEdges) {

                // if this is the code file, remove all outgoing edges
                bool removeAll = entry.Key == codeFile;
                
                // add to list and remove edge IDs from file index
                if (removeAll) {
                    foreach (uint edgeID in entry.Value) { conInstancesToRemove.Add(edgeID); }
                    entry.Value.Clear();
                    continue;
                }


                // store edge ids removed by this loop
                List<uint> addToRemoveList = new List<uint>();

                // check for each edge id if it ends in the deleted code file
                foreach (uint edgeID in entry.Value) {
               
                    // add to list if connection ends at the deleted code file and remove from index
                    if (edgeConnections[edgeID].GetEndCodeFile() == codeFile) { addToRemoveList.Add(edgeID); }
                }

                // add to list and remove edge IDs from file index
                foreach (uint removeID in addToRemoveList) {
                    conInstancesToRemove.Add(removeID);
                    entry.Value.Remove(removeID);
                }
            }

            // remove the listed edge instance
            Debug.Log("Removing " + conInstancesToRemove.Count + " edge connections...");

            // destroy game objects of connections and remove from connection instance index
            foreach (uint edgeID in conInstancesToRemove) {
                Destroy(edgeConnections[edgeID].gameObject);
                edgeConnections.Remove(edgeID);
            }

            // remove the codefile entry from spawned edges dictionary
            spawnedEdges.Remove(codeFile);
            Debug.Log("Finished removing edge connections!");
        }


        /// <summary>
        /// Spawn a new edge connectio instance.<para/>
        /// Returns 1 on success, 0 if it already exists or target file is not spawned and -1 on failure!
        /// </summary>
        /// <param name="edgeCon">The edge connection instance or null on failure</param>
        private int SpawnEdgeConnection(CodeFile fromCodeFile, Edge edge, StructureLoader sLoader, out CodeWindowEdgeConnection edgeCon) {

            edgeCon = null;

            // find target - just skip if missing
            string targetPath = edge.GetTo().file.ToLower();
            CodeFile targetFile = sLoader.GetFileByRelativePath(targetPath);
            if (targetFile == null) { return -1; }

            // check that the target file is spawned as well, otherwise skip!
            if (!spawnedEdges.ContainsKey(targetFile)) { return 0; }

            // create edge connection instance
            GameObject edgeConInstance = Instantiate(edgeConnectionPrefab, Vector3.zero, Quaternion.identity);
            edgeCon = edgeConInstance.GetComponent<CodeWindowEdgeConnection>();
            if (!edgeCon) {
                DestroyImmediate(edgeConInstance);
                return -1;
            }

            // add instance to the according container
            Transform container = fromCodeFile.GetReferences().GetEdgePoints().connectionContainer;
            edgeConInstance.transform.SetParent(container);
            edgeConInstance.transform.rotation = container.rotation;
            edgeCon.Prepare(edge);

            // initialize the connection objects
            // (CAN BE DONE IN A SEPARATE STEP IF REQUIRED)
            bool success = edgeCon.InitConnection(fromCodeFile, targetFile);
            if (!success) {
                DestroyImmediate(edgeConInstance);
                return -1;
            }

            return 1;
        }


        /// <summary>
        /// Updates the currently spawned edges validating them by
        /// checking if they are marked as active/selected to be shown.<para/>
        /// If a spawned edge is no longer active, it will be removed
        /// and if an active edge type is available but not spawned, it will be spawned.<para/>
        /// Returns false on errors (e.g. edge- or structure-loader missing).
        /// </summary>
        public bool UpdateSpawnedEdges(out uint edgesAdded, out uint edgesRemoved) {

            edgesAdded = 0;
            edgesRemoved = 0;

            // edge loader is required
            if (edgeLoader == null) { edgeLoader = GetEdgeLoader(); }
            if (edgeLoader == null) { return false; }

            // structure loader is required to get target code files to spawn new edges
            StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
            if (structureLoader == null) { return false; }


            // check if spawned edge type is not active
            foreach (var entry in spawnedEdges) {

                IEnumerable<Edge> edges = edgeLoader.GetEdgesOfFile(entry.Key);
                if (edges == null) { continue; }

                // list of IDs of edges to remove or spawn
                List<uint> removeIDs = new List<uint>();
                List<Edge> toAdd = new List<Edge>();

                foreach (Edge edge in edges) {
                
                    // remove if not active or add if not spawned yet
                    if (!IsEdgeTypeActive(edge.GetEdgeType())) { removeIDs.Add(edge.GetID()); }
                    else if (!entry.Value.Contains(edge.GetID())) { toAdd.Add(edge); }
                }

                // remove the edges stored in the list
                foreach (uint edgeID in removeIDs) {

                    // destroy edge gameobject
                    if (!edgeConnections.ContainsKey(edgeID)) { continue; }
                    Destroy(edgeConnections[edgeID].gameObject);
                    
                    // remove from spawned edges index
                    edgeConnections.Remove(edgeID);
                    entry.Value.Remove(edgeID);
                    edgesRemoved++;
                }

                // add missing edges stored in list
                foreach (Edge edge in toAdd) {

                    if (edgeConnections.ContainsKey(edge.GetID())) { continue; }

                    // try to create the connection instance
                    CodeWindowEdgeConnection edgeCon = null;
                    int state = SpawnEdgeConnection(entry.Key, edge, structureLoader, out edgeCon);
                    if (state < 1) { continue; } // failure

                    // add to spawned edges index
                    edgeConnections.Add(edge.GetID(), edgeCon);
                    entry.Value.Add(edge.GetID());
                    edgesAdded++;
                }
            }

            return true;
        }

    }
}
