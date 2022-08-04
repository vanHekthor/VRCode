using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Spawner.Edges;
using VRVis.Spawner.File;

namespace VRVis.Spawner {

    /// <summary>
    /// This script takes care of setting up the visible links and edges
    /// that connect nodes across or inside a code window.<para/>
    /// It will be notified by the FileSpawner script when code window related events occur.<para/>
    /// This script should be attached to the same object that holds the FileSpawner instance.<para/>
    /// There should only be one instance of this class!
    /// Ensure to check if this is the case, because the script currently does not prevent it.
    /// </summary>
    public class CodeWindowEdgeSpawner : ASpawner {

        [Tooltip("The edge representing GameObject with the LineRenderer component")]
	    public GameObject edgeConnectionPrefab;
        [Tooltip("The UI element that indicates the existence of an edge to another file and allows opening that file by clicking it.")]
        public GameObject linkPrefab;
        [Tooltip("The UI element that indicates the existence of references to a method and allows opening those.")]
        public GameObject refPrefab;

        public GameObject connectionManager;

        /// <summary>
        /// Stores instances of connection instances of spawned edges.<para/>
        /// key = edge id, value = connection instance
        /// </summary>
        private Dictionary<uint, CodeWindowEdgeConnection> edgeConnections = new Dictionary<uint, CodeWindowEdgeConnection>();

        private Dictionary<uint, CodeWindowLink> codeWindowLinks = new Dictionary<uint, CodeWindowLink>();

        private Dictionary<uint, CodeWindowMethodRef> codeWindowMethodRefs = new Dictionary<uint, CodeWindowMethodRef>();


        /// <summary>Stores ids (value) of spawned edges starting from the code file instance (key)</summary>
        private Dictionary<CodeFileReferences, HashSet<uint>> spawnedLinksAndEdges = new Dictionary<CodeFileReferences, HashSet<uint>>();

        /// <summary>Stores ids (value) of spawned edges ending at the code file instance (key)</summary>
        private Dictionary<CodeFileReferences, HashSet<uint>> spawnedRefs = new Dictionary<CodeFileReferences, HashSet<uint>>();

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
        public IEnumerable<CodeWindowEdgeConnection> GetSpawnedEdgeConnections(CodeFileReferences codeFileInstance) {
            if (!spawnedLinksAndEdges.ContainsKey(codeFileInstance)) { return null; }
            return GetSpawnedEdgeConnections(spawnedLinksAndEdges[codeFileInstance]);
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

        public bool EdgeIsSpawned(Edge edge) {
            return edgeConnections.ContainsKey(edge.GetID());
        }

        /// <summary>
        /// Called by FileSpawner on code window creation.<para/>
        /// Creates links that indicate edges going from this file to other code files,
        /// and the outgoing edges of other files ending in this file. <para/>
        /// Links can then be used by the user to spawn an edge connection.
        /// </summary>
        public void CodeWindowSpawnedEvent(CodeFileReferences codeFileInstance) {
            // edge loader is required
            if (edgeLoader == null) { edgeLoader = GetEdgeLoader(); }
            if (edgeLoader == null) { return; }

            // structure loader is required to get target code files
            StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
            if (structureLoader == null) { return; }


            // check if there is "garbage" to clean up or initialize hash set for this file
            if (spawnedLinksAndEdges.ContainsKey(codeFileInstance)) {
                spawnedLinksAndEdges[codeFileInstance].Clear();
            }
            else {
                spawnedLinksAndEdges.Add(codeFileInstance, new HashSet<uint>());
            }
            if (spawnedRefs.ContainsKey(codeFileInstance)) {
                spawnedRefs[codeFileInstance].Clear();
            }
            else {
                spawnedRefs.Add(codeFileInstance, new HashSet<uint>());
            }

            uint failedSpawns = 0;

            // basically unused because a edge connection is now only spawned after pressing the corresponding link button
            uint newEdgesSpawned = 0;

            uint newLinkButtons = 0;

            // add the missing link buttons
            AddMissingLinks(structureLoader, out failedSpawns, out newEdgesSpawned, out newLinkButtons);
            // add the missing ref buttons
            AddMissingRefs(structureLoader);

            // log infos and warn if some failed to spawn
            string spawnedEdgesMsg = "New edges spawned: " + newEdgesSpawned + " for instance of file: " +
                codeFileInstance.GetCodeFile().GetNode().GetName() + " (failed incl. edges & link buttons: " + failedSpawns + ")";

            string spawnedLinkButtonsMsg = "New link buttons spawned: " + newLinkButtons + "for instance of file: " +
                codeFileInstance.GetCodeFile().GetNode().GetName() + " (failed incl. edges & link buttons: " + failedSpawns + ")";

            string failedSpawnsMsg = failedSpawns + " failed link button and edge spawns for instance of file:: " +
                codeFileInstance.GetCodeFile().GetNode().GetName();

            if (failedSpawns > 0) {
                Debug.LogWarning(failedSpawnsMsg);
            }
            else {
                Debug.Log(spawnedEdgesMsg);
                Debug.Log(spawnedLinkButtonsMsg);
            }
        }

        public CodeWindowEdgeConnection SpawnSingleEdgeConnection(CodeFileReferences baseFileInstance, CodeFileReferences targetFileInstance, Edge edge) {
            // edge loader is required
            if (edgeLoader == null) { edgeLoader = GetEdgeLoader(); }
            if (edgeLoader == null) {
                Debug.LogError("Missing edge loader!");
                return null;
            }

            // structure loader is required to get target code files
            StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
            if (structureLoader == null) {
                Debug.LogError("Missing structuce loader!");
                return null;
            }
            
            // this edge connection already exists / is already spawned
            if (spawnedLinksAndEdges[baseFileInstance].Contains(edge.GetID())) {
                if (edgeConnections.ContainsKey(edge.GetID())) {
                    Debug.LogError("Edge connection already exists!");
                    return null;
                }
            }

            // try to create the connection instance
            dynamic linkOrEdge = null;
            int state = SpawnConnectionInstance(baseFileInstance, targetFileInstance, edge, structureLoader, out linkOrEdge);
            if (state < 0) {
                if (state == -1) {
                    Debug.LogError("Spawning edge connection for " + edge.GetLabel() + " failed!");
                }
            }
            else if (state == 0) {
                if (!codeWindowLinks.ContainsKey(edge.GetID())) {
                    codeWindowLinks.Add(edge.GetID(), linkOrEdge);
                    spawnedLinksAndEdges[baseFileInstance].Add(edge.GetID());
                    Debug.Log("Successfully spawned link for " + edge.GetLabel() + " !");
                }
            }
            else if (state == 1) {
                if (!edgeConnections.ContainsKey(edge.GetID())) {
                    edgeConnections.Add(edge.GetID(), linkOrEdge);
                    spawnedLinksAndEdges[baseFileInstance].Add(edge.GetID());
                    Debug.Log("Successfully spawned edge connection for " + edge.GetLabel() + " !");

                    if (codeWindowLinks.ContainsKey(edge.GetID())) {
                        Destroy(codeWindowLinks[edge.GetID()].gameObject);
                        codeWindowLinks.Remove(edge.GetID());
                    }
                }
            }

            if (connectionManager == null) {
                connectionManager = GameObject.FindGameObjectsWithTag("ConnectionManager")[0];
            }

            string connectionName = $"{edge.GetFrom().file.Replace('/', '.')}:{edge.GetFrom().callMethodLines.from} <> {edge.GetTo().file.Replace('/', '.')}:{edge.GetTo().lines.from}";
            var connectionComponent = connectionManager.transform.Find(connectionName).GetComponent<Connection>();
            //connectionComponent.points[0].color = Color.cyan;
            //connectionComponent.points[1].color = Color.cyan;
            connectionComponent.ChangeColor(Color.cyan);

            return (CodeWindowEdgeConnection)linkOrEdge;
        }


        /// <summary>
        /// Returns true if this edge type is currently active.
        /// </summary>
        private bool IsEdgeTypeActive(string edgeType) {
            return ApplicationLoader.GetApplicationSettings().IsEdgeTypeActive(edgeType);
        }


        /// <summary>
        /// Adds missing link buttons after the spawnedEdges dictionary changed (e.g. new file added).
        /// </summary>
        private void AddMissingLinks(StructureLoader sLoader, out uint failedSpawns, out uint newEdgesSpawned, out uint newLinkButtons) {

            // keep entries that should be removed from spawnedLinksEdges  
            var removeFailed = new List<CodeFileReferences>();

            failedSpawns = 0;

            // basically unused because a edge connection is now only spawned after pressing the corresponding link button
            newEdgesSpawned = 0;

            newLinkButtons = 0;

            foreach (KeyValuePair<CodeFileReferences, HashSet<uint>> entry in spawnedLinksAndEdges) {

                // check if references exist - file is probably not spawned so clean up
                if (!entry.Key) {
                    removeFailed.Add(entry.Key);
                    continue;
                }

                // edges can be null if there a none defined
                IEnumerable<Edge> edges = edgeLoader.GetEdgesOfFile(entry.Key.GetCodeFile(), entry.Key.Config.Name);

                if (edges == null) { continue; }

                foreach (Edge edge in edges) {
                    
                    // skip if this edge type is not active/selected to be shown
                    if (!IsEdgeTypeActive(edge.GetEdgeType())) { continue; }

                    // this edge connection already exists / is already spawned
                    if (entry.Value.Contains(edge.GetID())) {
                        if (edgeConnections.ContainsKey(edge.GetID())) {
                            continue;
                        }
                    }

                    // try to create the connection instance
                    dynamic linkOrEdge = null;
                    int state = SpawnLink(entry.Key, edge, sLoader, out linkOrEdge);
                    if (state < 0) {
                        if (state == -1) { failedSpawns++; }
                    }
                    else if (state == 0) {
                        if (!codeWindowLinks.ContainsKey(edge.GetID())) {
                            codeWindowLinks.Add(edge.GetID(), linkOrEdge);
                            entry.Value.Add(edge.GetID());
                            newLinkButtons++;
                        }                 
                    }
                }                
            }

            // remove entries from spawnedEdges dict. that have no valid references
            foreach (CodeFileReferences cf in removeFailed) { spawnedLinksAndEdges.Remove(cf); }
        }

        /// <summary>
        /// Adds missing link buttons after the spawnedEdges dictionary changed (e.g. new file added).
        /// </summary>
        private void AddMissingRefs(StructureLoader sLoader) {

            // keep entries that should be removed from spawnedLinksEdges  
            var removeFailed = new List<CodeFileReferences>();

            foreach (KeyValuePair<CodeFileReferences, HashSet<uint>> entry in spawnedRefs) {

                // check if references exist - file is probably not spawned so clean up
                if (!entry.Key) {
                    removeFailed.Add(entry.Key);
                    continue;
                }

                // edges can be null if there a none defined
                IEnumerable<Edge> refEdges = edgeLoader.GetRefEdgesOfFile(entry.Key.GetCodeFile(), entry.Key.Config.Name);

                if (refEdges == null) { continue; }

                foreach (Edge refEdge in refEdges) {

                    // skip if this edge type is not active/selected to be shown
                    if (!IsEdgeTypeActive(refEdge.GetEdgeType())) { continue; }

                    // this edge connection already exists / is already spawned
                    if (entry.Value.Contains(refEdge.GetID())) {
                        if (edgeConnections.ContainsKey(refEdge.GetID())) {
                            continue;
                        }
                    }

                    // try to create the ref instance
                    int state = SpawnRef(entry.Key, refEdge, sLoader, out CodeWindowMethodRef refComponent);
                    if (state < 0) {
                        if (state == -1) {
                            Debug.LogError($"Failed to spawn ref link button for '{refEdge.GetLabel()}' in '{refEdge.GetTo().file}'");
                        }
                    }
                    else if (state == 0) {
                        if (!codeWindowMethodRefs.ContainsKey(refEdge.GetID())) {
                            codeWindowMethodRefs.Add(refEdge.GetID(), refComponent);
                            entry.Value.Add(refEdge.GetID());
                        }
                    }
                }
            }

            // remove entries from spawnedEdges dict. that have no valid references
            foreach (CodeFileReferences cf in removeFailed) { spawnedLinksAndEdges.Remove(cf); }
        }

        public void RemoveSingleEdgeConnection(CodeFileReferences codeFileInstance, Edge edge) {

            // structure loader is required to get code files
            StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
            if (structureLoader == null) { return; }

            if (!spawnedLinksAndEdges.ContainsKey(codeFileInstance)) {
                Debug.LogError("Failed to remove edge connection, because code file instance was not found!");
                return;
            }

            var fileInstanceEdges = spawnedLinksAndEdges[codeFileInstance];
            fileInstanceEdges.Remove(edge.GetID());
            
            // try to create the link to replace the edge connection
            dynamic edgeCon = null;
            int state = SpawnLink(edgeConnections[edge.GetID()].GetStartCodeFileInstance(), edgeConnections[edge.GetID()].GetEdge(), structureLoader, out edgeCon);
            if (state < 0) { // failure
                Debug.LogError("Failed to spawn the link that replaces the edge connection!");
            }
            else if (state == 0) {
                if (!codeWindowLinks.ContainsKey(edge.GetID())) {
                    codeWindowLinks.Add(edge.GetID(), edgeCon);
                    spawnedLinksAndEdges[codeFileInstance].Add(edge.GetID());
                }
            }

            if (edgeConnections.ContainsKey(edge.GetID())) {
                Destroy(edgeConnections[edge.GetID()].gameObject);
                edgeConnections.Remove(edge.GetID());
            }                
        }


        /// <summary>
        /// Called by FileSpawner on code window deletion.<para/>
        /// Removes link buttons it contains and also edges going from and to this code file.
        /// When edges have to be removed because the targeted code file is closed,
        /// the edges get replaced by link buttons.
        /// </summary>
        public void CodeWindowRemovedEvent(CodeFileReferences codeFileInstance) {
            
            // Steps of cleanup:
            // 1. get all edges that are currently spawned
            // 2. check if the edge originates from this file -> remove connection instance
            // 3. check if the edge connection ends at this file -> remove connection instance

            // list to gather the IDs of edge connection that we have to remove from the scene
            HashSet<uint> conInstancesToRemove = new HashSet<uint>();

            foreach (KeyValuePair<CodeFileReferences, HashSet<uint>> entry in spawnedLinksAndEdges) {

                // if this is the code file, remove all outgoing edges
                bool removeAll = entry.Key == codeFileInstance;
                
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
                    if (edgeConnections.ContainsKey(edgeID)) {
                        if (edgeConnections[edgeID].GetEndCodeFileInstance() == codeFileInstance) {
                            addToRemoveList.Add(edgeID);
                        }
                    }
                    else if (codeWindowLinks.ContainsKey(edgeID)) {
                        if (codeWindowLinks[edgeID].BaseFileInstance == codeFileInstance) {
                            addToRemoveList.Add(edgeID);
                        }
                    }                 
                }

                // structure loader is required to get code files
                StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
                if (structureLoader == null) { return; }

                // add to list and remove edge IDs from file index
                foreach (uint removeID in addToRemoveList) {
                    conInstancesToRemove.Add(removeID);
                    entry.Value.Remove(removeID);

                    // try to create the link to replace the edge connection
                    dynamic edgeCon = null;
                    int state = SpawnLink(edgeConnections[removeID].GetStartCodeFileInstance(), edgeConnections[removeID].GetEdge(), structureLoader, out edgeCon);
                    if (state < 0) { continue; } // failure
                    else if (state == 0) {
                        if (!codeWindowLinks.ContainsKey(removeID)) {
                            codeWindowLinks.Add(removeID, edgeCon);
                            entry.Value.Add(removeID);
                        }
                    }
                }
            }

            // remove the listed edge instance
            Debug.Log("Removing " + conInstancesToRemove.Count + " edge connections and link buttons...");

            int removedLinks = 0;
            int removedEdges = 0;
            // destroy game objects of connections and remove from connection instance index
            foreach (uint edgeID in conInstancesToRemove) {
                if (edgeConnections.ContainsKey(edgeID)) {
                    Destroy(edgeConnections[edgeID].gameObject);
                    edgeConnections.Remove(edgeID);
                    removedEdges++;
                }
                else if (codeWindowLinks.ContainsKey(edgeID)) {
                    Destroy(codeWindowLinks[edgeID].gameObject);
                    codeWindowLinks.Remove(edgeID);
                    removedLinks++;
                }
            }

            // remove the codefile entry from spawned edges dictionary
            spawnedLinksAndEdges.Remove(codeFileInstance);
            Debug.Log("Finished removing edge connections and link buttons!");
            Debug.Log("Removed " + removedEdges + " edges and " + removedLinks + " link buttons!");

            RemoveRefs(codeFileInstance);
        }

        private void RemoveRefs(CodeFileReferences codeFileInstance) {
            foreach (var refID in spawnedRefs[codeFileInstance]) {
                Destroy(codeWindowMethodRefs[refID].gameObject);
                codeWindowMethodRefs.Remove(refID);
            }
            spawnedRefs[codeFileInstance].Clear();
            spawnedRefs.Remove(codeFileInstance);
        }

        public void UpdateControlFlowInsideCodeWindow(CodeFileReferences fileInstance) {
            foreach (var edgeID in spawnedLinksAndEdges[fileInstance]) {
                if (codeWindowLinks.ContainsKey(edgeID)) {
                    var link = codeWindowLinks[edgeID];
                    link.ForcePositionUpdate();
                    continue;
                }
                if (edgeConnections.ContainsKey(edgeID)) {
                    var edgeConnection = edgeConnections[edgeID];
                    edgeConnection.ForcePositionUpdate();
                }
            }
        }

        /// <summary>
        /// Spawn a new connection instance (i.e. edge connection or link button if targeted code file is not open)<para/>
        /// Returns 1 when an edge connection was successfully spawned
        /// 0 when target file is not open yet and a link button was spawned
        /// -1 when spawning failed
        /// 2 when target file is not open yet and a link button already exists 
        /// </summary>
        /// <param name="edgeCon">The edge connection instance or null on failure</param>
        private int SpawnConnectionInstance(CodeFileReferences fromCodeFileInstance, CodeFileReferences targetFileInstance, Edge edge, StructureLoader sLoader, out dynamic edgeCon) {

            edgeCon = null;

            // find target - just skip if missing
            string targetPath = edge.GetTo().file.ToLower();
            CodeFile targetFile = sLoader.GetFileByRelativePath(targetPath);
            if (targetFile == null) { return -1; }

            // check that the target file is spawned as well, otherwise spawn link button!
            if (!spawnedLinksAndEdges.ContainsKey(targetFileInstance)) {
                return SpawnLink(fromCodeFileInstance, edge, sLoader, out edgeCon);
            }

            // create edge connection instance
            GameObject edgeConInstance = Instantiate(edgeConnectionPrefab, Vector3.zero, Quaternion.identity);
            edgeCon = edgeConInstance.GetComponent<CodeWindowEdgeConnection>();
            if (!edgeCon) {
                DestroyImmediate(edgeConInstance);
                return -1;
            }

            // add instance to the according container
            Transform container = fromCodeFileInstance.GetEdgePoints().connectionContainer;
            edgeConInstance.transform.SetParent(container, false);
            edgeConInstance.transform.rotation = container.rotation;
            edgeCon.Prepare(edge);

            // initialize the connection objects
            // (CAN BE DONE IN A SEPARATE STEP IF REQUIRED)
            bool success = edgeCon.InitConnection(fromCodeFileInstance, targetFileInstance);
            if (!success) {
                DestroyImmediate(edgeConInstance);
                return -1;
            }

            return 1;
        }

        private int SpawnLink(CodeFileReferences fromCodeFileInstance, Edge edge, StructureLoader sLoader, out dynamic edgeCon) {
            edgeCon = null;
            
            // find target - just skip if missing
            string targetPath = edge.GetTo().file.ToLower();
            CodeFile targetFile = sLoader.GetFileByRelativePath(targetPath);
            if (targetFile == null) { return -1; }

            if (!codeWindowLinks.ContainsKey(edge.GetID())) {
                // show link button that indicates the existence of the edge and opens the targeted file when pressing it
                GameObject linkInstance = Instantiate(linkPrefab, Vector3.zero, Quaternion.identity);
                edgeCon = linkInstance.GetComponent<CodeWindowLink>();
                if (!edgeCon) {
                    DestroyImmediate(linkInstance);
                    return -1;
                }

                // add instance to the according container
                Transform linkContainer = fromCodeFileInstance.GetEdgePoints().connectionContainer;
                linkInstance.transform.SetParent(linkContainer, false);
                linkInstance.transform.rotation = linkContainer.rotation;

                if (!edgeCon.InitLink(edge, fromCodeFileInstance, targetFile)) {
                    DestroyImmediate(linkInstance);
                    return -1;
                }

                return 0;
            }
            return 2;
        }

        private int SpawnRef(CodeFileReferences declarationCodeFileInstance, Edge edge, StructureLoader sLoader, out CodeWindowMethodRef refComponent) {
            refComponent = null;

            // find target - just skip if missing
            string basePath = edge.GetFrom().file.ToLower();
            CodeFile callingFile = sLoader.GetFileByRelativePath(basePath);
            if (callingFile == null) { return -1; }

            if (!codeWindowMethodRefs.ContainsKey(edge.GetID())) {
                // show link button that indicates the existence of the edge and opens the targeted file when pressing it
                GameObject refInstance = Instantiate(refPrefab, Vector3.zero, Quaternion.identity);
                refComponent = refInstance.GetComponent<CodeWindowMethodRef>();
                if (!refComponent) {
                    DestroyImmediate(refInstance);
                    return -1;
                }

                // add instance to the according container
                Transform refContainer = declarationCodeFileInstance.GetEdgePoints().connectionContainer;
                refInstance.transform.SetParent(refContainer, false);
                refInstance.transform.rotation = refContainer.rotation;

                if (!refComponent.InitRef(edge, declarationCodeFileInstance, callingFile)) {
                    DestroyImmediate(refInstance);
                    return -1;
                }

                return 0;
            }
            return 2;
        }

        /// <summary>
        /// Updates the currently spawned links and edges by checking if they are marked 
        /// as active/selected to be shown.<para/>
        /// If a spawned link or edge is no longer active, it will be removed
        /// and if an active control flow type is available but not spawned, 
        /// the link will be spawned.<para/>
        /// Returns false when errors occur (e.g. edge- or structure-loader missing).
        /// </summary>
        public bool UpdateSpawnedLinksAndEdges(out uint edgesAdded, out uint edgesRemoved, out uint linksAdded, out uint linksRemoved) {

            edgesAdded = 0; // basically unused because now only links get spawned by this method
            edgesRemoved = 0;
            linksAdded = 0;
            linksRemoved = 0;

            // edge loader is required
            if (edgeLoader == null) { edgeLoader = GetEdgeLoader(); }
            if (edgeLoader == null) { return false; }

            // structure loader is required to get target code files to spawn new edges
            StructureLoader structureLoader = ApplicationLoader.GetInstance().GetStructureLoader();
            if (structureLoader == null) { return false; }


            // check if spawned edge type is not active
            foreach (var entry in spawnedLinksAndEdges) {

                IEnumerable<Edge> edges = edgeLoader.GetEdgesOfFile(entry.Key.GetCodeFile(), entry.Key.Config.Name);
                if (edges == null) { continue; }

                // list of IDs of edges to remove or spawn
                List<uint> removeIDs = new List<uint>();
                List<Edge> toAdd = new List<Edge>();

                foreach (Edge edge in edges) {
                
                    // remove if not active or add if not spawned yet
                    if (!IsEdgeTypeActive(edge.GetEdgeType())) {
                        removeIDs.Add(edge.GetID());
                    }
                    else if (!entry.Value.Contains(edge.GetID())) { toAdd.Add(edge); }
                }


                // remove the edges stored in the list
                foreach (uint edgeID in removeIDs) {

                    // destroy edge gameobject                    
                    if (edgeConnections.ContainsKey(edgeID)) {
                        Destroy(edgeConnections[edgeID].gameObject);

                        // remove from spawned edges index
                        edgeConnections.Remove(edgeID);
                        entry.Value.Remove(edgeID);
                        edgesRemoved++;
                    }
                    if (codeWindowLinks.ContainsKey(edgeID)) {
                        Destroy(codeWindowLinks[edgeID].gameObject);

                        // remove from spawned edges index
                        codeWindowLinks.Remove(edgeID);
                        entry.Value.Remove(edgeID);
                        linksRemoved++;
                    }           
                   
                }

                // add missing edges stored in list
                foreach (Edge edge in toAdd) {

                    // if (edgeConnections.ContainsKey(edge.GetID())) { continue; }

                    // try to create the connection instance
                    dynamic edgeCon = null;
                    int state = SpawnLink(entry.Key, edge, structureLoader, out edgeCon);
                    if (state < 0) { continue; } // failure
                    else if (state == 0) {
                        if (!codeWindowLinks.ContainsKey(edge.GetID())) {
                            codeWindowLinks.Add(edge.GetID(), edgeCon);
                            entry.Value.Add(edge.GetID());
                            linksAdded++;
                        }
                    }
                    // basically unused because now only links get spawned by this method
                    else if (state == 1) {
                        if (!edgeConnections.ContainsKey(edge.GetID())) {
                            // add to spawned edges index
                            edgeConnections.Add(edge.GetID(), edgeCon);
                            entry.Value.Add(edge.GetID());
                            edgesAdded++;
                        }
                    }                    
                }
            }

            return true;
        }

    }
}
