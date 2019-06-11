using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner.Structure;

namespace VRVis.Spawner {

    /// <summary>
    /// Takes care of spawning the software system structure.<para/>
    /// This is the second version using the same concept as the variability model spawner.
    /// </summary>
    [System.Serializable]
    public class StructureSpawnerV2 : ASpawner {
    
        // prefabs for folders and files
        public GameObject folderPrefab;
        public float folderPrefabRadius = 0.5f;

        public GameObject filePrefab;
        public float filePrefabRadius = 0.5f;
        
        [Tooltip("Transform that tells where to start building the tree and where to attach nodes to")]
        public Transform hierarchyParent;

        [Tooltip("Minimum radius of a single option node")]
        public float minimumRadius = 0.5f;

        [Tooltip("Maximum radius of a signle option node (set 0 for unlimited)")]
        public float maximumRadius = 200f;

        [Tooltip("Scale the node distance from center by this factor (everything <= 0 behaves like 1)")]
        public float scaleNodeDistance = 0;

        [Tooltip("Restrict the maximum node distance from center at this value (set 0 for umlimited)")]
        public float maxNodeDistance = 0;

        [Tooltip("Spacing between hierarchy levels")]
        public float levelSpacing = 0.5f;

        [Tooltip("Gap between nodes on the same level")]
        public float nodeSpacing = 0.20f;

        [Tooltip("If previous radius is used, higher level nodes are positioned according to it. Turning this off can lead to overlapping!")]
        public bool useRadiusOfPreviousLevel = true;

        [Tooltip("Rotation applied after the structure is created")]
        public Vector3 structureRootRotation = new Vector3(0, -90, 0);

        // if spawning is done
        private bool done = false;

        private SNode rootNode;

        /// <summary>Stores all the nodes that are at the same level in one list</summary>
        private List<List<StructureNodeInfoV2>> nodes;

        /// <summary>Used while position information gathering</summary>
        private class PosInfo {
            public SNode node = null;
            public int level = 0;
            public float radius = 0;
            public Vector2 relPos = Vector2.zero;
            public List<PosInfo> childNodes = new List<PosInfo>();
        }

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
                Gizmos.DrawWireSphere(origin, minimumRadius <= 0 ? 1 : minimumRadius);
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
            else { Debug.Log("Software structure v2 successfully spawned."); }

            return success;
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
            rootPosInfo = CalculateLevelPositioningRecursively(rootNode, 0);
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

            if (filePrefabRadius < minimumRadius) { filePrefabRadius = minimumRadius; }
            if (folderPrefabRadius < minimumRadius) { folderPrefabRadius = minimumRadius; }

            return true;
        }


        /// <summary>
        /// Recursively calculate the position of nodes for each level so that they do not intersect.<para/>
        /// This requires traversing the whole hierarchy once!<para/>
        /// Returns an array of Pairs representing the positions at the index level (starting at level 0 with the root node only).
        /// </summary>
        private PosInfo CalculateLevelPositioningRecursively(SNode node, int level) {

            if (level > treeLevels) { treeLevels = level; }

            // initial information about a node
            PosInfo info = new PosInfo {
                node = node,
                level = level,
                radius = minimumRadius
            };


            // we are at a leaf node if this statement is true
            if (node.GetNodesCount() == 0) {

                // assign radius depending on type of leaf node
                if (node.GetNodeType() == SNode.DNodeTYPE.FILE) { info.radius = filePrefabRadius; }
                else if (node.GetNodeType() == SNode.DNodeTYPE.FOLDER) { info.radius = folderPrefabRadius; }

                return info;
            }


            // get recursively info of child nodes
            float maxRadius = minimumRadius;
            foreach (SNode child in node.GetNodes()) {
                PosInfo childInfo = CalculateLevelPositioningRecursively(child, level + 1);
                info.childNodes.Add(childInfo);
                if (childInfo.radius > maxRadius) { maxRadius = childInfo.radius; }
            }


            // calculate position for each child node
            float nodeRadius = info.radius; // the final calculated radius of this node

            int childNodesCount = node.GetNodesCount();
            if (childNodesCount == 1) {
                
                // a single node needs no position adjustment but radius is important
                nodeRadius = info.childNodes[0].radius;
            }
            else if (childNodesCount == 2) {

                // use easy calculation for two nodes
                Vector2 curPos = Vector2.zero;
                float rad0 = info.childNodes[0].radius;
                float rad1 = info.childNodes[1].radius;
                float dist = (rad0 + rad1) * 2 + nodeSpacing;
                info.childNodes[0].relPos = curPos + (dist * 0.5f - rad0) * Vector2.left;
                info.childNodes[1].relPos = curPos + (dist * 0.5f - rad1) * Vector2.right;
                nodeRadius = dist * 0.5f;

                // restrict position of the nodes
                RestrictChildPosition(info.childNodes[0]);
                RestrictChildPosition(info.childNodes[1]);
            }
            else {

                // use polygon calculation for more than two nodes
                Vector2 curPos = Vector2.zero;
                float dist = maxRadius * 2 + nodeSpacing;

                // move in a circle starting at (0,0) but do not "close" it
                float fullCircle = 2 * Mathf.PI;
                float angleStep = fullCircle / info.childNodes.Count;
                float angle = 0;

                foreach (PosInfo childInfo in info.childNodes) {

                    // calculate where to go next
                    childInfo.relPos = curPos;
                    Vector2 curDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    curPos += curDir * dist;
                    angle += angleStep;
                }


                // calculate radius using an isosceles triangle equation
                float gamma = angleStep;
                float c = dist;
                float a = 0.5f * (c / Mathf.Sin(gamma * 0.5f));
                nodeRadius = a + maxRadius;


                // calculate the centroid using position of each child node
                Vector2 centroid = Vector2.zero;
                
                if (childNodesCount == 3) {

                    // use barycentric coordinates to calculate centroid
                    Vector2 l = info.childNodes[0].relPos;
                    Vector2 m = info.childNodes[1].relPos;
                    Vector2 n = info.childNodes[2].relPos;
                    centroid = 1f / 3f * (l + m + n);
                }
                else {

                    // use right-angled triangle and the pythagorean theorem to calculate centroid
                    float up = Mathf.Sqrt(a*a - Mathf.Pow(c * 0.5f, 2));
                    centroid = info.childNodes[0].relPos + c * 0.5f * Vector2.right + up * Vector2.up;
                }

                // adjust positions of child nodes so that centroid is at (0,0)
                foreach (PosInfo childInfo in info.childNodes) {
                    childInfo.relPos -= centroid;
                    RestrictChildPosition(childInfo);
                }
            }
            
            // limit radius to maximum value
            if (maximumRadius > 0 && nodeRadius > maximumRadius) {
                nodeRadius = maximumRadius;
                Debug.LogError("Maximum graph radius reached (" + maximumRadius + ")!");
            }

            // apply the calculated radius based on previous levels
            if (useRadiusOfPreviousLevel) { info.radius = nodeRadius; }

            //Debug.LogWarning("Radius = " + nodeRadius);
            return info;
        }

        /// <summary>
        /// Restricts e.g. the distance of the node from its center
        /// according to the user settings "scaleNodeDistance" and "maxNodeDistance".<para/>
        /// Applied after the nodes have been positioned.
        /// </summary>
        private void RestrictChildPosition(PosInfo childInfo) {

            // scale node distance by this factor
            if (scaleNodeDistance > 0 && scaleNodeDistance != 1) {
                childInfo.relPos = childInfo.relPos.magnitude * scaleNodeDistance * childInfo.relPos.normalized;
            }
                    
            // limit maximum distance from center
            if (maxNodeDistance > 0 && childInfo.relPos.magnitude > maxNodeDistance) {
                childInfo.relPos = maxNodeDistance * childInfo.relPos.normalized;
            }
        }


        /// <summary>
        /// Spawn the tree in space recursively using 
        /// the previously calculated position information.
        /// </summary>
        private void SpawnTreeRecursively(PosInfo info, StructureNodeInfoV2 parentNodeInf, Transform parent) {
            
            string err_msg = "Failed to spawn a node of the file structure";

            // get according prefab
            GameObject prefab = null;

            if (info.node.GetNodeType() == SNode.DNodeTYPE.FILE) { prefab = filePrefab; }
            else if (info.node.GetNodeType() == SNode.DNodeTYPE.FOLDER) { prefab = folderPrefab; }
            else {
                Debug.LogWarning(err_msg + " - type not supported: " + info.node.GetNodeType());
                return;
            }


            // calculate position in space
            Vector3 nodeMove = new Vector3(info.relPos.x, 0, info.relPos.y);
            Vector3 nodePos = (info.level > 0 ? 1 : 0) * levelSpacing * Vector3.down + nodeMove;
            GameObject nodeInstance = Instantiate(prefab, nodePos, Quaternion.identity);
            nodeInstance.transform.SetParent(parent, false);


            // check for reference information object
            StructureNodeInfoV2 nodeInf = nodeInstance.GetComponent<StructureNodeInfoV2>();
            if (!nodeInf) {
                Debug.LogError(err_msg + " - Missing component StructureNodeInfoV2 (Check if properly attached to prefab)!");
                DestroyImmediate(nodeInstance);
                return;
            }

            // set node info accordingly
            nodeInf.SetInformation(info.node, info.level, info.radius);

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
