using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner.Structure;
using VRVis.Spawner.Layouts.ConeTree;
using VRVis.Mappings;

namespace VRVis.Spawner {

    /// <summary>
    /// Takes care of spawning the software system structure.<para/>
    /// This is the 4. version using a technique to split up a full circle in angular sectors (bubble-tree layout).<para/>
    /// Sub-nodes / circles are then placed within these areas.
    /// </summary>
    [System.Serializable]
    public class StructureSpawnerV4 : ASpawner {
    
        // prefabs for folders and files
        public GameObject folderPrefab;
        public float folderPrefabRadius = 0.1f;

        public GameObject filePrefab;
        public float filePrefabRadius = 0.06f;

        [Tooltip("Spacing between hierarchy levels")]
        public float levelSpacing = 0.5f;

        [Tooltip("Transform that tells where to start building the tree and where to attach nodes to")]
        public Transform hierarchyParent;

        [Tooltip("Rotation applied after the structure is created")]
        public Vector3 structureRootRotation = new Vector3(0, -90, 0);

        [Tooltip("Rotate the nodes towards position of their parent on y-axis")]
        public bool rotateNodeToParent = true;

        [Header("Generic Layout Settings")]
        [Tooltip("Cone Tree Layout Settings")]
        public LayoutSettings settings;


        // if spawning is done
        private bool done = false;

        private SNode rootNode;

        /// <summary>Stores all the nodes that are at the same level in one list</summary>
        private List<List<StructureNodeInfoV2>> nodes;

        /// <summary>Stores the positioning information of all nodes</summary>
        private PosInfo rootPosInfo = null;
        
        /// <summary>Amount of levels (including the root node)</summary>
        private int treeLevels = 0;



        // GETTER AND SETTER

        public SNode GetRootNode() { return rootNode; }
        public void SetRootNode(SNode rootNode) { this.rootNode = rootNode; }



        // FUNCTIONALITY

        /// Show the spawn position of the directory structure in editor
        void OnDrawGizmos() {

            if (hierarchyParent) {
                Vector3 origin = hierarchyParent.position;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(origin, settings.minRadius <= 0 ? 1 : settings.minRadius);
                Gizmos.DrawLine(origin, origin + hierarchyParent.rotation * Vector3.left);
            }
        }

        /// <summary>Get the angle in radians.</summary>
        private float DegreeToRadians(float degree) { return Utilities.Utility.DegreeToRadians(degree); }


        /// <summary>Prepares and spawns the visualization.</summary>
        public override bool SpawnVisualization() {

            StructureLoader sl = ApplicationLoader.GetInstance().GetStructureLoader();

            if (!sl.LoadedSuccessful()) {
                Debug.LogWarning("Failed to spawn structure! Loading was not successful.");
                return false;
            }

            // set root node and spawn the structure
            SetRootNode(sl.GetRootNode());
            bool success = SpawnStructure();

            if (!success) { Debug.LogWarning("Failed to spawn software structure v2."); }
            else { Debug.Log("Software structure v4 successfully spawned."); }

            return success;
        }


        /// <summary>Show/Hide the visualization.</summary>
        public override void ShowVisualization(bool state) {

            if (!done || !hierarchyParent) { return; }
            hierarchyParent.gameObject.SetActive(state);
        }


        /// <summary>Spawn the software system structure.</summary>
        public bool SpawnStructure() {

            if (done) {
                Debug.LogError("Structure already spawned!");
                return false;
            }

            if (!CheckSettings()) {
                Debug.LogError("Settings are invalid!");
                return false;
            }

            if (rootNode == null) {
                Debug.LogError("Missing root node (null)!");
                return false;
            }
            
            // calculate node and position information recursively
            treeLevels = 0;
            if (nodes != null) { nodes.Clear(); }
            rootPosInfo = CalculateLayout(rootNode, 0);
            Debug.Log("Structure tree levels to spawn: " + treeLevels);
            nodes = new List<List<StructureNodeInfoV2>>(treeLevels);

            // spawns the tree GameObject instances and positions them
            SpawnTreeRecursively(rootPosInfo, null, hierarchyParent);

            // move tree structure up by its height (only for now)
            float parentLocalScale = hierarchyParent.localScale.y;
            float treeHeight = treeLevels * levelSpacing * parentLocalScale;
            Debug.Log("Structure tree height: " + treeHeight);
            hierarchyParent.position += Vector3.up * treeHeight;

            // rotate structure root accordingly
            hierarchyParent.Rotate(structureRootRotation, Space.Self);

            done = true;
            return true;
        }


        /// <summary>Calculates the relative node layout.</summary>
        private PosInfo CalculateLayout(SNode rootNode, int level) {

            ConeTreeLayout layout = new ConeTreeLayout(rootNode, settings);

            // tell layout how to treat leaf nodes
            layout.SetLeafRadiusOverride(node => {
                SNode n = node as SNode;
                if (n == null) { return -1; }
                if (n.GetNodeType() == SNode.DNodeTYPE.FILE) { return filePrefabRadius; }
                else if (n.GetNodeType() == SNode.DNodeTYPE.FOLDER) { return folderPrefabRadius; }
                return -1;
            });

            PosInfo result = layout.Create();
            treeLevels = layout.GetTreeLevels();
            return result;
        }


        /// <summary>Check the structure spawner settings.</summary>
        bool CheckSettings() {

            if (!folderPrefab || !filePrefab) {
                Debug.LogError("Prefab missing!");
                return false;
            }

            if (!hierarchyParent) {
                Debug.LogError("Hierarchy parent transform is missing!");
                return false;
            }

            return true;
        }


        /// <summary>
        /// Spawn the tree in space recursively using 
        /// the previously calculated position information.
        /// </summary>
        private void SpawnTreeRecursively(PosInfo info, StructureNodeInfoV2 parentNodeInf, Transform parent) {
            
            string err_msg = "Failed to spawn a node of the file structure";

            // get according prefab based on the type of the node
            GameObject prefab = null;
            Color c = Color.black;
            bool assignColor = false;

            SNode node = info.node as SNode;
            if (node == null) {
                Debug.LogError("Can not deal with node not of type SNode!");
                return;
            }
            
            if (node.GetNodeType() == SNode.DNodeTYPE.FILE) {

                prefab = filePrefab;
                string name = node.GetName().ToLower();

                // get color coding according to filename
                ValueMappingsLoader vml = ApplicationLoader.GetInstance().GetMappingsLoader();
                if (vml.HasFilenameSettings()) {
                    foreach (FilenameSetting s in vml.GetFilenameSettings()) {
                        if (s.Applies(name)) {
                            c = s.GetColor();
                            assignColor = true;
                            break;
                        }
                    }
                }
            }
            else if (node.GetNodeType() == SNode.DNodeTYPE.FOLDER) { prefab = folderPrefab; }
            else {
                Debug.LogWarning(err_msg + " - type not supported: " + node.GetNodeType());
                return;
            }


            // calculate position in space
            Vector3 nodeMove = new Vector3(info.relPos.x, 0, info.relPos.y);
            Vector3 nodePos = (info.level > 0 ? 1 : 0) * levelSpacing * Vector3.down + nodeMove;
            GameObject nodeInstance = Instantiate(prefab, nodePos, Quaternion.identity);
            if (rotateNodeToParent) { nodeInstance.transform.LookAt(nodePos + nodeMove); }
            nodeInstance.transform.SetParent(parent, false);
            

            // color edges according to the color mapping
            if (assignColor) { nodeInstance.GetComponent<Renderer>().material.color = c; }


            // check for reference information object
            StructureNodeInfoV2 nodeInf = nodeInstance.GetComponent<StructureNodeInfoV2>();
            if (!nodeInf) {
                Debug.LogError(err_msg + " - Missing component StructureNodeInfoV2 (Check if properly attached to prefab)!");
                DestroyImmediate(nodeInstance);
                return;
            }

            // set node info accordingly
            nodeInf.SetInformation(node, info.level, info.radius);

            // add reference to all nodes on the specific level
            if ((info.level+1) > nodes.Count) { nodes.Add(new List<StructureNodeInfoV2>()); }
            if ((info.level+1) > nodes.Count) {
                Debug.LogError(err_msg + " - Broken level info! (level: " + info.level + ")");
                DestroyImmediate(nodeInstance);
                return;
            }
            nodes[info.level].Add(nodeInf);


            // SET CONNECTION LINE ============>

            // check if this is not the root node
            if (parentNodeInf != null && nodeInf.GetLevel() > 0) {

                // set the line anchors according to the notations
                if (nodeInf.connectionLine) {
                    nodeInf.connectionLine.lineStart = nodeInf.GetAttachmentTop();
                    nodeInf.connectionLine.lineEnd = parentNodeInf.GetAttachmentBottom();
                }
            }
            else if (nodeInf.connectionLine) {

                // remove the component because it is doing nothing (e.g. for root)
                Destroy(nodeInf.connectionLine);
            }

            // <============ SET CONNECTION LINE


            // get parent to attach child nodes to
            Transform childNodesParent = nodeInf.GetChildNodesParent();
            if (childNodesParent == null) { childNodesParent = nodeInstance.transform; }
            else if (info.childNodes.Count == 0) {
                // remove unused transform element
                Destroy(childNodesParent.gameObject);
            }


            // proceed recursively with child nodes
            foreach (PosInfo child in info.childNodes) {
                SpawnTreeRecursively(child, nodeInf, childNodesParent);
            }
        }

    }
}
