using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.Spawner.ConfigModel;
using VRVis.Spawner.Layouts.ConeTree;

namespace VRVis.Spawner {

    /// <summary>
    /// Spawns the variability model in 3D space.<para/>
    /// To be more precise, it spawns the option hierarchy tree in space.<para/>
    /// This is the second version using the same cone tree layout as the structure loader (bubble-tree layout).<para/>
    /// 
    /// ToDo:<para/>
    /// - [ ] Improve the option index retrieval (see accordingly commented sections)<para/>
    /// 
    /// Created: 01.02.2019 (by Leon H.)<para/>
    /// Updated: 06.08.2019
    /// </summary>
    public class VariabilityModelSpawnerV2 : ASpawner {

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

        [Tooltip("Spacing between hierarchy levels")]
        public float levelSpacing = 1;

        [Tooltip("Rotate the nodes towards position of their parent on y-axis")]
        public bool rotateNodeToParent = true;

        [Header("Generic Layout Settings")]
        [Tooltip("Cone Tree Layout Settings")]
        public LayoutSettings settings;


        private bool isSpawned = false;
        private VariabilityModel curModel = null;

        /// <summary>Stores all the nodes that are at the same level in one list</summary>
        private List<List<VariabilityModelNodeInfo>> nodes;

        /// <summary>Stores the positioning information of all nodes</summary>
        private PosInfo rootPosInfo = null;
        
        /// <summary>Amount of levels (including the root node)</summary>
        private int treeLevels = 0;


        /// <summary>Show spawn position in editor.</summary>
        void OnDrawGizmos() {

            if (hierarchyParent) {
                Vector3 origin = hierarchyParent.position;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(origin, settings.minRadius <= 0 ? 1 : settings.minRadius);
                Gizmos.DrawLine(origin, origin + hierarchyParent.rotation * Vector3.left);
            }
        }


        /// <summary>Prepares and spawns the visualization.</summary>
        public override bool SpawnVisualization() {

            VariabilityModelLoader vml = ApplicationLoader.GetInstance().GetVariabilityModelLoader();

            if (!vml.LoadedSuccessful()) {
                Debug.LogWarning("Failed to spawn variability model! Loading was not successful.");
                return false;
            }

            // try to spawn the model
            bool success = Spawn(vml.GetModel());

            if (!success) { Debug.LogWarning("Failed to spawn variability model."); }
            else { Debug.Log("Variability model v2 successfully spawned."); }

            return success;
        }


        /// <summary>Show/Hide the visualization.</summary>
        public override void ShowVisualization(bool state) {

            if (!isSpawned || !hierarchyParent) { return; }
            hierarchyParent.gameObject.SetActive(state);
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
            rootPosInfo = CalculateLayout(model.GetRoot(), 0);
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
            if (settings.minRadius < 0) { return false; }
            if (levelSpacing <= 0) { return false; }
            return true;
        }


        /// <summary>
        /// Recursively calculate the position of nodes for each level so that they do not intersect.<para/>
        /// This requires traversing the whole hierarchy once!<para/>
        /// Returns the according PosInfo of the recently processed level/node.
        /// </summary>
        private PosInfo CalculateLayout(AFeature node, int level) {

            ConeTreeLayout layout = new ConeTreeLayout(node, settings);
            PosInfo result = layout.Create();
            treeLevels = layout.GetTreeLevels();
            return result;
        }


        /// <summary>
        /// Spawn the tree in space recursively using 
        /// the previously calculated position information.
        /// </summary>
        private void SpawnTreeRecursively(PosInfo info, VariabilityModelNodeInfo parentNodeInf, Transform parent) {
            
            string err_msg = "Failed to spawn an option of feature hierarchy";

            AFeature option = info.node as AFeature;
            if (option == null) {
                Debug.LogError("Can not deal with node not of type AFeature!");
                return;
            }


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
            Vector3 nodeMove = new Vector3(info.relPos.x, 0, info.relPos.y);
            Vector3 nodePos = (info.level > 0 ? 1 : 0) * levelSpacing * Vector3.down + nodeMove;
            GameObject nodeInstance = Instantiate(prefab, nodePos, Quaternion.identity);
            if (rotateNodeToParent) { nodeInstance.transform.LookAt(nodePos + nodeMove); }
            nodeInstance.transform.SetParent(parent, false);


            // check for reference information object
            VariabilityModelNodeInfo nodeInf = nodeInstance.GetComponent<VariabilityModelNodeInfo>();
            if (!nodeInf) {
                Debug.LogError(err_msg + " - Missing component VariabilityModelNodeInfo (Check if properly attached to prefab)!");
                DestroyImmediate(nodeInstance);
                return;
            }

            // set node info accordingly
            int optionIndex = curModel.GetOptionIndex(option.GetName(), false);
            nodeInf.SetInformation(curModel, optionIndex, info.level, info.radius);
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

            // ToDo: security check if required ("what if node is no AFeature") or add the index to the feature in model!
            int parentOptionIndex = curModel.GetOptionIndex((commonParentInfo.node as AFeature).GetName(), false);

            // or-groups are defined by "implied option groups"
            AFeature curOption = curModel.GetOption(parentOptionIndex);
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
                
                // ToDo: security check if required ("what if node is no AFeature") or add the index to the feature in model!
                int optionIndex = curModel.GetOptionIndex((child.node as AFeature).GetName(), false);

                AFeature feature = curModel.GetOption(optionIndex);
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