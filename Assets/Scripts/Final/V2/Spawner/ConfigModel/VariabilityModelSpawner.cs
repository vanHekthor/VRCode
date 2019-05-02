using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Effects;
using VRVis.IO.Features;
using VRVis.Spawner.ConfigModel;

namespace VRVis.Spawner {

    /// <summary>
    /// Spawns the variability model in 3D space.<para/>
    /// To be more precise, it spawns the option hierarchy tree in space.
    /// </summary>
    public class VariabilityModelSpawner : MonoBehaviour {

        public GameObject binaryOptionPrefab;
        public GameObject numericOptionPrefab;

        [System.Serializable]
        public struct NotationPrefabs { // for parental relationships
            public GameObject optional;
            public GameObject mandatory;
            public GameObject or; // at least one sub-feature must be selected (checkbox like) [1, *]
            public GameObject alt; // (XOR) one of the sub-features must be selected (radio button like) [1, 1]
        }

        public NotationPrefabs notationPrefabs;

        [Tooltip("Transform that tells where to start building the tree and where to attach nodes to")]
        public Transform hierarchyParent;

        [Tooltip("Minimum radius of a single option node")]
        public float minimumRadius = 0.5f;

        [Tooltip("Maximum radius of a single option node (set 0 for unlimited)")]
        public float maximumRadius = 200f;

        [Tooltip("Scale the node distance from center by this factor (everything <= 0 behaves like 1)")]
        public float scaleNodeDistance = 0;

        [Tooltip("Restrict the maximum node distance from center at this value (set 0 for umlimited)")]
        public float maxNodeDistance = 0;

        [Tooltip("Spacing between hierarchy levels")]
        public float levelSpacing = 1;

        [Tooltip("Gap between nodes on the same level")]
        public float nodeSpacing = 0.20f;

        [Tooltip("If previous radius is used, higher level nodes are positioned according to it. Turning this off can lead to overlapping!")]
        public bool useRadiusOfPreviousLevel = true;

        private bool isSpawned = false;
        private VariabilityModel curModel = null;

        /// <summary>Stores all the nodes that are at the same level in one list</summary>
        private List<List<VariabilityModelNodeInfo>> nodes;

        /// <summary>Used while position information gathering</summary>
        private class PosInfo {
            public int level = 0;
            public float radius = 0;
            public int optionIndex = 0;
            public Vector2 optionPos = Vector2.zero;
            public List<PosInfo> childNodes = new List<PosInfo>();
        }

        /// <summary>Stores the positioning information of all nodes</summary>
        private PosInfo rootPosInfo = null;
        
        /// <summary>Amount of levels (including the root node)</summary>
        private int treeLevels = 0;


        /// <summary>Show spawn position in editor.</summary>
        void OnDrawGizmos() {

            if (hierarchyParent) {
                Vector3 origin = hierarchyParent.position;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(origin, minimumRadius <= 0 ? 1 : minimumRadius);
                Gizmos.DrawLine(origin, origin + hierarchyParent.rotation * Vector3.left);
            }
        }


        /// <summary>
        /// Spawns the model hierarchy in space according to the settings.
        /// </summary>
        public bool Spawn(VariabilityModel model) {
        
            curModel = model;
            string err_msg = "Failed to spawn variability model";

            if (isSpawned) {
                Debug.LogError(err_msg + " - Already spawned!");
                return false;
            }

            if (!SettingsValid()) {
                Debug.LogError(err_msg + " - Invalid settings!");
                return false;
            }

            if (curModel == null) {
                Debug.LogError(err_msg + " - Invalid model!");
                return false;
            }

            // calculate node and position information recursively
            treeLevels = 0;
            if (nodes != null) { nodes.Clear(); }
            rootPosInfo = CalculateLevelPositioningRecursively(model.GetRoot(), 0);
            Debug.Log("Tree levels to spawn: " + treeLevels);
            nodes = new List<List<VariabilityModelNodeInfo>>(treeLevels);

            // spawns the tree GameObject instances and positions them
            SpawnTreeRecursively(rootPosInfo, null, hierarchyParent);

            // move tree structure up by its height (only for now)
            float parentLocalScale = hierarchyParent.localScale.y;
            float treeHeight = treeLevels * levelSpacing * parentLocalScale;
            Debug.Log("Tree height: " + treeHeight);
            hierarchyParent.position += Vector3.up * treeHeight;

            isSpawned = true;
            return true;
        }

        private bool SettingsValid() {

            if (!binaryOptionPrefab) { return false; }
            if (!numericOptionPrefab) { return false; }
            if (!hierarchyParent) { return false; }
            if (minimumRadius < 0) { return false; }
            if (levelSpacing <= 0) { return false; }
            return true;
        }


        /// <summary>
        /// Recursively calculate the position of nodes for each level so that they do not intersect.<para/>
        /// This requires traversing the whole hierarchy once!<para/>
        /// Returns an array of Pairs(option index, position relative to its parent)
        /// representing the positions at the index level (starting at level 0 with the root node only).
        /// </summary>
        private PosInfo CalculateLevelPositioningRecursively(AFeature node, int level) {

            if (level > treeLevels) { treeLevels = level; }

            // initial information about a node
            PosInfo info = new PosInfo();
            info.level = level;
            info.radius = minimumRadius;
            info.optionIndex = curModel.GetOptionIndex(node.GetName(), false);

            // we are at a leaf node if this statement is true
            if (!node.HasChildren()) { return info; }


            // get recursively info of child nodes
            float maxRadius = minimumRadius;
            foreach (AFeature child in node.GetChildren()) {
                PosInfo childInfo = CalculateLevelPositioningRecursively(child, level + 1);
                info.childNodes.Add(childInfo);
                if (childInfo.radius > maxRadius) { maxRadius = childInfo.radius; }
            }


            // calculate position for each child node
            float nodeRadius = info.radius; // the final calculated radius of this node

            int childNodesCount = node.GetChildrenCount();
            if (childNodesCount == 1) {
                
                // a single node needs no position adjustment but radius is important
                info.radius = info.childNodes[0].radius;
            }
            else if (childNodesCount == 2) {

                // use easy calculation for two nodes
                Vector2 curPos = Vector2.zero;
                float rad0 = info.childNodes[0].radius;
                float rad1 = info.childNodes[1].radius;
                float dist = (rad0 + rad1) * 2 + nodeSpacing;
                info.childNodes[0].optionPos = curPos + (dist * 0.5f - rad0) * Vector2.left;
                info.childNodes[1].optionPos = curPos + (dist * 0.5f - rad1) * Vector2.right;
                nodeRadius = dist * 0.5f + nodeSpacing;

                // restrict position of the nodes
                RestrictChildPosition(info.childNodes[0]);
                RestrictChildPosition(info.childNodes[1]);
            }
            else {

                // use polygon calculation for more nodes
                Vector2 curPos = Vector2.zero;
                float dist = maxRadius * 2 + nodeSpacing;

                // move in a circle starting at (0,0) but do not "close" it
                float fullCircle = 2 * Mathf.PI;
                float angleStep = fullCircle / info.childNodes.Count;
                float angle = 0;

                foreach (PosInfo childInfo in info.childNodes) {

                    // calculate where to go next
                    childInfo.optionPos = curPos;
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
                    Vector2 l = info.childNodes[0].optionPos;
                    Vector2 m = info.childNodes[1].optionPos;
                    Vector2 n = info.childNodes[2].optionPos;
                    centroid = 1f / 3f * (l + m + n);
                }
                else {

                    // use right-angled triangle and the pythagorean theorem to calculate centroid
                    float up = Mathf.Sqrt(a*a - Mathf.Pow(c * 0.5f, 2));
                    centroid = info.childNodes[0].optionPos + c * 0.5f * Vector2.right + up * Vector2.up;
                }

                // adjust positions of child nodes so that centroid is at (0,0)
                foreach (PosInfo childInfo in info.childNodes) {
                    childInfo.optionPos -= centroid;
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
                childInfo.optionPos = childInfo.optionPos.magnitude * scaleNodeDistance * childInfo.optionPos.normalized;
            }
                    
            // limit maximum distance from center
            if (maxNodeDistance > 0 && childInfo.optionPos.magnitude > maxNodeDistance) {
                childInfo.optionPos = maxNodeDistance * childInfo.optionPos.normalized;
            }
        }


        /// <summary>
        /// Spawn the tree in space recursively using 
        /// the previously calculated position information.
        /// </summary>
        private void SpawnTreeRecursively(PosInfo info, VariabilityModelNodeInfo parentNodeInf, Transform parent) {
            
            string err_msg = "Failed to spawn an option of feature hierarchy";
            AFeature option = curModel.GetOption(info.optionIndex);


            // get prefab and option type
            GameObject prefab = null;
            bool isBinary = false;

            if (option is Feature_Boolean) {
                prefab = binaryOptionPrefab;
                isBinary = true;
            }
            else if (option is Feature_Range) {
                prefab = numericOptionPrefab;
            }
            else {
                Debug.LogWarning(err_msg + " - type not supported: " + option.GetType());
                return;
            }


            // calculate position in space
            Vector3 nodeMove = new Vector3(info.optionPos.x, 0, info.optionPos.y);
            Vector3 nodePos = (info.level > 0 ? 1 : 0) * levelSpacing * Vector3.down + nodeMove;
            GameObject nodeInstance = Instantiate(prefab, nodePos, Quaternion.identity);
            nodeInstance.transform.SetParent(parent, false);


            // check for reference information object
            VariabilityModelNodeInfo nodeInf = nodeInstance.GetComponent<VariabilityModelNodeInfo>();
            if (!nodeInf) {
                Debug.LogError(err_msg + " - Missing component VariabilityModelNodeInfo (Check if properly attached to prefab)!");
                DestroyImmediate(nodeInstance);
                return;
            }

            // set node info accordingly
            nodeInf.SetInformation(curModel, info.optionIndex, info.level, info.radius);
            nodeInf.UpdateColor();

            // add reference to all nodes on the specific level
            if ((info.level+1) > nodes.Count) { nodes.Add(new List<VariabilityModelNodeInfo>()); }
            if ((info.level+1) > nodes.Count) {
                Debug.LogError(err_msg + " - Broken level info! (level: " + info.level + ")");
                DestroyImmediate(nodeInstance);
                return;
            }
            nodes[info.level].Add(nodeInf);


            // SET CONNECTION LINE AND ADD NOTATION MARKS ============>

            // check if this is not the root node
            if (parentNodeInf != null && nodeInf.GetLevel() > 0) {

                // check if children form an "or" and "alt"(xor) group
                //bool hasOrGroup = AreChildrenOrGroup(info);
                //bool hasAltGroup = AreChildrenAltGroup(info);

                // spawn type of notation below this node
                GameObject notationPrefabBelow = null;
                if (AreChildrenAltGroup(info)) { notationPrefabBelow = notationPrefabs.alt; } // alt-group
                else if (AreChildrenOrGroup(info)) { notationPrefabBelow = notationPrefabs.or; } // or-group

                // spawn "or" or "alt" notation accordingly
                if (notationPrefabBelow) {
                    GameObject notationInstanceBelow = Instantiate(notationPrefabBelow);
                    nodeInf.SetNotationBottom(notationInstanceBelow);
                    nodeInf.SetChildrenAreGroup(true);
                }

                // spawn notation above this node
                bool hideNotationWhenGroup = false; // show/hide notation above node when group exists
                if (!parentNodeInf.AreChildrenGroup() || !hideNotationWhenGroup) {

                    // check easily if binary feature is option or not - numeric feature is always required to be set
                    bool isOptional = (isBinary ? ((Feature_Boolean) option).IsOptional() : false);

                    GameObject notationPrefab = isOptional ? notationPrefabs.optional : notationPrefabs.mandatory;
                    if (notationPrefab) {
                        GameObject notationInstanceAbove = Instantiate(notationPrefab);
                        nodeInf.SetNotationTop(notationInstanceAbove);
                    }
                }

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

            // <============ SET CONNECTION LINE AND ADD NOTATION MARKS


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


        /// <summary>
        /// Tells if the child nodes are all part of an alternative group.<para/>
        /// This means, that all have the same parent, exclude each other and are not optional!<para/>
        /// Furthermore, all nodes have to be binary options because numeric options are not optional.
        /// </summary>
        /*
        private bool AreChildrenAlternative(PosInfo commonParentInfo) {

            // gather all the child nodes and check if they are binary
            List<Feature_Boolean> children = new List<Feature_Boolean>();
            foreach (PosInfo child in commonParentInfo.childNodes) {
                
                AFeature feature = curModel.GetIndexOption(child.optionIndex);
                if (!(feature is Feature_Boolean)) { return false; }
                children.Add((Feature_Boolean) feature);
            }

            foreach (Feature_Boolean child in children) {
                
                if (child.IsOptional()) { return false; }
                foreach (List<AFeature> excludedOptions in child.GetExcludedOptions()) {
                    
                }
            }

            // Wikipedia: Alternative when: or-group and all exclude each other (so we currently use this instead) 
        }
        */

        /// <summary>
        /// Tells if the child nodes form an "or" group together ("where at least one feature must be selected").<para/>
        /// This is the case if each node is connected to the same parent node
        /// and at least one of them is mandatory to be selected.<para/>
        /// Furthermore, all nodes have to be binary options because numeric options are not optional.<para/>
        /// For semantics see here: https://en.wikipedia.org/wiki/Feature_model#Semantics <para/>
        /// SPL_Conqueror alternative group creation:
        /// https://github.com/se-passau/SPLConqueror/blob/8df34a8e309caa23cf4703210f18374e6d411035/SPLConqueror/VariabilityModel_GUI/AlternativeGroupDialog.cs#L53
        /// (comparison is only done with single exclusion entries!)
        /// </summary>
        private bool AreChildrenOrGroup(PosInfo commonParentInfo) {

            // OLD VERSION
            /*
            // gather all the child nodes and check if they are binary
            int mandatoryFeatures = 0;
            foreach (PosInfo child in commonParentInfo.childNodes) {
                
                AFeature feature = curModel.GetOption(child.optionIndex);
                if (!(feature is Feature_Boolean)) { return false; }
                Feature_Boolean binFeature = (Feature_Boolean) feature;

                if (!binFeature.IsOptional()) { mandatoryFeatures++; }
            }

            return mandatoryFeatures > 0;
            */

            // NEW: or-groups are defined by "implied option groups"
            AFeature curOption = curModel.GetOption(commonParentInfo.optionIndex);
            if (!(curOption is Feature_Boolean)) { return false; }

            Feature_Boolean oBoolean = (Feature_Boolean) curOption;
            if (oBoolean.GetChildrenCount() < 2) { return false; }
            if (oBoolean.GetImpliedOptionsCount() < 1) { return false; }

            // check if all child nodes can be found in an implied group
            foreach (List<AFeature> impliedGroup in oBoolean.GetImpliedOptions()) {
                
                foreach (AFeature child in oBoolean.GetChildren()) {
                    if (!impliedGroup.Contains(child)) { continue; }

                    // all child nodes must be of type boolean!
                    if (!(child is Feature_Boolean)) { return false; }
                    Feature_Boolean childBool = (Feature_Boolean) child;

                    // Todo: check if "all child nodes must be optional"?
                    if (!childBool.IsOptional()) { return false; }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the child options exclude each other and none of them is optional.<para/>
        /// In such a case, this child group can be considered to be an alternative group.<para/>
        /// To return true, it is also required that this node has at least two child nodes.
        /// </summary>
        private bool AreChildrenAltGroup(PosInfo commonParentInfo) {
            
            // gather all the child nodes and check if they are binary
            List<Feature_Boolean> children = new List<Feature_Boolean>();
            foreach (PosInfo child in commonParentInfo.childNodes) {
                
                AFeature feature = curModel.GetOption(child.optionIndex);
                if (!(feature is Feature_Boolean)) { return false; }
                children.Add((Feature_Boolean) feature);
            }

            // this node needs to have at least two child nodes to compare
            if (children.Count < 2) { return false; }

            // return false if any of the options does not exclude one of the others
            foreach (Feature_Boolean child in children) {
                foreach (Feature_Boolean other in children) {

                    if (child == other) { continue; }
                    if (!child.ExcludesOption(other)) { return false; }
                }
            }

            return true;
        }

    }
}