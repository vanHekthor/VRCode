using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner.Structure;

namespace VRVis.Spawner {

    /// <summary>
    /// Takes care of spawning the software system structure.<para/>
    /// This is the third version using a technique to split up a full circle in angular sectors.<para/>
    /// Sub-nodes / circles are then placed within these areas.
    /// </summary>
    [System.Serializable]
    public class StructureSpawnerV3 : ASpawner {
    
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


            // assign radius depending on type of leaf node and return
            if (node.IsLeaf()) {
                if (node.GetNodeType() == SNode.DNodeTYPE.FILE) { info.radius = filePrefabRadius; }
                else if (node.GetNodeType() == SNode.DNodeTYPE.FOLDER) { info.radius = folderPrefabRadius; }
                return info;
            }


            // get info of child nodes recursively
            float radius_sum = 0;
            foreach (SNode child in node.GetNodes()) {
                PosInfo childInfo = CalculateLevelPositioningRecursively(child, level + 1);
                info.childNodes.Add(childInfo);
                radius_sum += childInfo.radius;
            }


            // ---------------------------------------------------
            // calculate position for each child node

            // the final calculated radius of this node
            float nodeRadius = info.radius;
            int childNodesCount = node.GetNodesCount();

            if (childNodesCount == 1) {
                
                // a single node needs no position adjustment but radius is important
                nodeRadius = info.childNodes[0].radius;
            }
            else {

                // use angular sector calculation for multiple nodes
                float fullCircle = 2 * Mathf.PI;
                float angleTotal = 0;
                float size_n = minimumRadius; // size of center "node" n

                // calculate positioning on angular sector around n
                foreach (PosInfo childInfo in info.childNodes) {

                    float angle = (childInfo.radius / radius_sum) * fullCircle;
                    float d_i = Mathf.Max(size_n + childInfo.radius, childInfo.radius / Mathf.Sin(angle * 0.5f));
                    float pos_x = d_i * Mathf.Cos(angleTotal + angle * 0.5f);
                    float pos_y = d_i * Mathf.Sin(angleTotal + angle * 0.5f);
                    childInfo.relPos = new Vector2(pos_x, pos_y);
                    angleTotal += angle;
                }
                
                // find enclosing disk / circle using Welzl
                List<Disk> P = CreateShuffledList(info.childNodes);
                List<Disk> R = new List<Disk>(3);
                Disk SED = Welzl(P, R, P.Count);

                // ToDo: continue implementation

                // ToDo: readjust the centroid accordingly
                // ToDo: readjust the final radius accordingly
            }

            // apply the calculated radius based on previous levels
            if (useRadiusOfPreviousLevel) { info.radius = nodeRadius; }

            //Debug.LogWarning("Radius = " + nodeRadius);
            return info;
        }


        class Disk {

            public Vector2 pos;
            public float radius; // for case that |R| == 1

            public Disk(Vector2 p, float r) { pos = p; radius = r; }

            /// <summary>
            /// Checks if the position of the disk d is inside this disk.
            /// </summary>
            public bool Contains(Disk d) {
                return (d.pos - pos).magnitude <= radius;
            }
        }

        /// <summary>
        /// Creates a list of randomly shuffled elements based on Durstenfeld's algorithm.<para/>
        /// The shuffled list is created in O(n) time and returned as a list of disks.<para/>
        /// See: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#The_modern_algorithm
        /// </summary>
        private List<Disk> CreateShuffledList(List<PosInfo> nodes) {

            List<Disk> result = new List<Disk>(nodes.Count);
            nodes.ForEach(n => result.Add(new Disk(n.relPos, n.radius)));

            // Note: we start at i = |nodes| bc. the upper bound in Random.Range is exclusive
            for (int i = nodes.Count; i > 0; i++) {
                int n = Random.Range(0, i);
                Disk tmp = result[i-1];
                result[i-1] = result[n];
                result[n] = tmp;
            }

            return result;
        }

        /// <summary>
        /// Use Welzl's algorithm for smallest enclosing disk calculation.
        /// </summary>
        /// <param name="P">Set of points</param>
        /// <param name="R">Set of points on boundary</param>
        /// <param name="max">Size of P to avoid modifying the list</param>
        private Disk Welzl(List<Disk> P, List<Disk> R, int max) {

            if (P.Count == 0 || R.Count == 3) { return Trivial(R); }
            Disk p = P[max-1]; // list is already shuffled, so just select
            Disk D = Welzl(P, R, max-1); // call without p in P
            if (D.Contains(p)) { return D; }
            List<Disk> R_ = new List<Disk>(R){p}; // add p to R
            return Welzl(P, R_, max-1); // call without p in P but p in R
        }

        /// <summary>
        /// Calculates the Disk for |R| less than 3 and returns it.<para/>
        /// Otherwise returns null!
        /// </summary>
        private Disk Trivial(List<Disk> R) {

            if (R.Count == 1) { return R[0]; }

            // create circle from 2 points
            if (R.Count == 2) {
                Vector2 p1 = R[0].pos + R[0].pos.normalized * R[0].radius;
                Vector2 p2 = R[1].pos + R[1].pos.normalized * R[1].radius;
                Vector2 mid = (p1 + p2) * 0.5f;
                float radius = Vector2.Distance(mid, p1);
                return new Disk(mid, radius);
            }

            // calculate circumcircle of triangle
            if (R.Count == 3) { return CalculateTriangleCircumcircle(R); }

            return null;
        }


        /// <summary>
        /// Calculates a disk / circle from the given 3 points.<para/>
        /// This circle is the circumcircle of the triangle they form together.
        /// </summary>
        private Disk CalculateTriangleCircumcircle(List<Disk> R) {

            // Possible other approach:
            // http://www.meracalculator.com/graphic/perpendicularbisector.php
            // 1. calculate midpoint of line like:                ML = (p1.x + p2.x, p1.y + p2.y) / 2
            // 2. calculate slope of line like:                   SL = (p2.y - p1.y) / (p2.x - p2.y)
            // 3. calculate slope of perpendicular bisector:      SB = -1 / slope_line
            // 4. get p. bisect. equation using midpoint & slope: y - ML.y = SB * (x - ML.x)  =>  y = SB * (x - ML.x) + ML.y
            //
            // Find intersection of two perpendicular bisector lines y = L1 = m1 * x1 + n1 &  y = L2 = m2 * x2 + n2:
            // 1. Same y-coordinate at point of interaction, so:     L1 = L2  =>  m1 * x1 + n1 = m2 * x2 + n2
            // 2. m and n are known, so only x is unknown -> solve:  
            //    (see steps: https://www.xarg.org/2016/10/calculate-the-intersection-point-of-two-lines/)

            // ---------------------------
            // Current approach

            // 90 degree rotation matrix [cos(a), -sin(a), sin(a), cos(a)]
            float[] rotMat = new float[4]{0, -1, 1, 0};

            // find midpoints of two points and calculate perpendicular two bisectors
            Vector2 p1 = R[0].pos + R[0].pos.normalized * R[0].radius;
            Vector2 p2 = R[1].pos + R[1].pos.normalized * R[1].radius;
            Vector2 p3 = R[2].pos + R[2].pos.normalized * R[2].radius;

            Vector2 pb_pos_12 = (p1 + p2) * 0.5f; // midpoint between p1 and p2
            Vector2 pb_dir_12 = CalculateBisectorDirection(ref rotMat, pb_pos_12, p2);
            
            Vector2 pb_pos_13 = (p1 + p3) * 0.5f; // midpoint between p1 and p3
            Vector2 pb_dir_13 = CalculateBisectorDirection(ref rotMat, pb_pos_13, p3);
            
            // "Koordinatenform aus Parameterform" (parameter form -> general form)
            // https://de.wikipedia.org/wiki/Koordinatenform#Aus_der_Parameterform
            // genf_xx = [a, b, c] , so that g = ax + by + c = 0
            Vector2 norm_12 = new Vector2(-pb_dir_12.y, pb_dir_12.x); // normal vector of direction vector
            Vector3 genf_12 = new Vector3(norm_12.x, norm_12.y, Vector2.Dot(pb_pos_12, -norm_12));

            Vector2 norm_13 = new Vector2(-pb_dir_13.y, pb_dir_13.x); // normal vector of direction vector
            Vector3 genf_13 = new Vector3(norm_13.x, norm_13.y, Vector2.Dot(pb_pos_13, -norm_13));

            // apply cross-product to find intersection point of both lines
            Vector3 cross = Vector3.Cross(genf_12, genf_13);

            // circumcenter
            Vector2 cc = new Vector2(cross.x / cross.z, cross.y / cross.z);

            // get radius (cc distance to any other point)
            float radius = Vector2.Distance(cc, p1);

            return new Disk(cc, radius);
        }

        /// <summary>
        /// Calculates and returns the perpendicular bisector direction.<para/>
        /// Uses a rotation-matrix to turn vector mp around point m.<para/>
        /// </summary>
        /// <param name="rotMat">2x2 rotation-matrix with total of 4 values (first 2 = first row)</param>
        /// <param name="m">Midpoint between p1 and p2</param>
        /// <param name="p">One of the points (either p1 or p2)</param>
        private Vector2 CalculateBisectorDirection(ref float[] rotMat, Vector2 m, Vector2 p) {

            // rotate vector mp around point m
            Vector2 mp = p - m;
            return new Vector2(
                rotMat[0] * mp.x + rotMat[1] * mp.y,
                rotMat[2] * mp.x + rotMat[3] * mp.y
            );
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
