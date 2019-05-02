using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Effects;
using VRVis.IO.Structure;
using VRVis.Spawner.Structure;


namespace VRVis.Spawner {

    /// <summary>
    /// First version of structure spawner that takes
    /// care of spawning the software system structure in space.
    /// </summary>
    [System.Serializable]
    public class StructureSpawner : MonoBehaviour {

        public enum LayoutDirection { LEFT, RIGHT };
        public LayoutDirection layoutDirection = LayoutDirection.RIGHT;

        // prefabs for folders and files
        public GameObject folderPrefab;
        public GameObject filePrefab;
        public Material lineMaterial;

        // empty gameobject that holds the whole structure
        // (should be at position 0, use offset to adjust final position)
        public Transform structureRoot;
        private Vector3 structureRootPos;

        // rotation applied after adding nodes
        public Vector3 structureRootRotation = new Vector3(0, -90, 0);
        public Vector3 structureRootOffset = new Vector3(0, 0.2f, 0);

        // min and max positions of child nodes of the structure parent
        private Vector3 structureBounds_min = Vector3.zero;
        private Vector3 structureBounds_max = Vector3.zero;
        private Vector3 structureBounds_dir = Vector3.zero;

        // if spawning is done
        private bool done = false;

        // width and height of the prefabs
        private Vector3 folderPrefabSize = Vector3.zero;
        private Vector3 filePrefabSize = Vector3.zero;
        private SNode rootNode;


        // GETTER AND SETTER

        public SNode GetRootNode() { return rootNode; }
        public void SetRootNode(SNode rootNode) { this.rootNode = rootNode; }


        // FUNCTIONALITY

        /** Show the spawn position of the directory structure in editor */
        void OnDrawGizmos() {

            if (!Application.isPlaying) {
                structureRootPos = structureRoot.transform.position;
            }

            Vector3 origin = structureRootPos + structureRootOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, 0.1f);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(structureRootRotation) * Vector3.left);
        }


        /** Spawn the software system structure */
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

            // spawn the whole structure
            NodeInfoHelper startNI = new NodeInfoHelper{ pos = structureRoot.position };
            SpawnWholeStructure(rootNode, startNI);

            // rotate structure root accordingly
            structureRoot.Rotate(structureRootRotation, Space.Self);

            // get the direction of the structure layout and negative it
            structureBounds_dir = structureBounds_max - structureBounds_min;
            structureBounds_dir = structureRoot.localRotation * structureBounds_dir;
            Debug.Log("Moving structure root in direction: " + structureBounds_dir);

            // set structure to be in middle and sitting on zero (floor)
            structureRoot.Translate(new Vector3(
                structureBounds_dir.x / 2.0f,
                structureBounds_dir.y,
                structureBounds_dir.z / 2.0f
            ), Space.World);

            // apply specified offset (e.g. for high adjustments
            structureRoot.Translate(structureRootOffset, Space.World);

            done = true;
            return true;
        }


        /** Check the structure spawner settings. */
        bool CheckSettings() {

            if (!folderPrefab || !filePrefab) {
                Debug.LogError("Prefab missing!");
                return false;
            }

            if (!structureRoot) {
                Debug.LogError("Structure root missing!");
                return false;
            }

            if (!filePrefab.GetComponent<Renderer>()) {
                Debug.LogError("File prefab has no renderer!");
                return false;
            }
            filePrefabSize = filePrefab.GetComponent<Renderer>().bounds.size;

            if (!folderPrefab.GetComponent<Renderer>()) {
                Debug.LogError("Folder prefab has no renderer!");
                return false;
            }
            folderPrefabSize =  folderPrefab.GetComponent<Renderer>().bounds.size;

            return true;
        }


        /**
         * Spawn the whole structure at once using recursion.
         * Returns information about the last spawned node.
         */
        NodeInfoHelper SpawnWholeStructure(SNode node, NodeInfoHelper prevNode) {

            NodeInfoHelper lastChildInfo = new NodeInfoHelper {
                //lastChildInfo.gameObject = prevNode.gameObject;
                pos = prevNode.pos,
                width = prevNode.width,
                height = prevNode.height
            };

            // store info of each child for later usage to connect them with a line
            List<NodeInfoHelper> childNodes = new List<NodeInfoHelper>();

            // go down to leaf nodes
            if (node.GetNodes().Count > 0) {
                lastChildInfo.pos += Vector3.down * 0.3f;
                foreach (SNode subNode in node.GetNodes()) {
                    lastChildInfo = SpawnWholeStructure(subNode, lastChildInfo);
                    childNodes.Add(new NodeInfoHelper(lastChildInfo));
                }
            }

            // spawn this node
            NodeInfoHelper spawnInfo = SpawnNode(node, prevNode);
            bool writeZ = spawnInfo.pos.z > lastChildInfo.pos.z;
            if (layoutDirection == LayoutDirection.LEFT) { writeZ = spawnInfo.pos.z < lastChildInfo.pos.z; }
            if (writeZ) { spawnInfo.pos.z = lastChildInfo.pos.z; }

            // add connection line to the child nodes adding the according script
            if (childNodes.Count > 0) {
                foreach (NodeInfoHelper child in childNodes) {

                    // add line connection script
                    ConnectionLine lineScript = child.gameObject.AddComponent<ConnectionLine>();
                    lineScript.lineEnd = spawnInfo.gameObject.transform;
                    lineScript.lineRendererMaterial = lineMaterial;
                    lineScript.updatePositions = true;

                    // set transform parent accordingly
                    child.gameObject.transform.SetParent(spawnInfo.gameObject.transform);
                }
            }

            return spawnInfo;
        }


        /**
         * Returns spawn position and width.
         */
        NodeInfoHelper SpawnNode(SNode node, NodeInfoHelper lastNode) {
    
            GameObject prefab = null;
            NodeInfoHelper info = new NodeInfoHelper();

            // get width, height and prefab according to the type
            if (node.GetNodeType() == SNode.DNodeTYPE.FOLDER) {
                info.width = folderPrefabSize.y;
                info.height = folderPrefabSize.z;
                prefab = folderPrefab;
            }
            else if (node.GetNodeType() == SNode.DNodeTYPE.FILE) {
                info.width = filePrefabSize.y;
                info.height = filePrefabSize.z;
                prefab = filePrefab;
            }

            // spawn the object
            if (prefab != null) {
                info.pos = Vector3.zero;

                // create an instance of the prefab
                Quaternion objectRotation = structureRoot.rotation * prefab.transform.rotation; // quaternions need to be multiplied
                GameObject GOInstance = Instantiate(prefab, info.pos, objectRotation);
                info.gameObject = GOInstance;

                // direction of layout regarding position
                if (layoutDirection == LayoutDirection.LEFT)
                { info.pos = lastNode.pos + info.width * Vector3.forward + lastNode.width * Vector3.forward; }
                else if (layoutDirection == LayoutDirection.RIGHT)
                { info.pos = lastNode.pos + info.width * -Vector3.forward + lastNode.width * -Vector3.forward; }
                GOInstance.transform.position = info.pos;

                // set parent transform
                if (lastNode.gameObject == null) { GOInstance.transform.SetParent(structureRoot); }
                /*
                if (lastNode.gameObject != null) { GOInstance.transform.SetParent(lastNode.gameObject.transform); }
                else { GOInstance.transform.SetParent(structureRoot); }
                */

                // gather min/max position values for the structure bounding box
                UpdateStructureParentBounds(GOInstance.transform.position);

                // add information about this element by storing a reference to the node it represents
                StructureNodeInfo nodeInfo = GOInstance.AddComponent<StructureNodeInfo>();
                nodeInfo.SetSNode(node);
            }

            return info;
        }


        /**
         * Set the min and max positions accordingly for each new node.
         */
        void UpdateStructureParentBounds(Vector3 p) {

            if (layoutDirection == LayoutDirection.RIGHT) {
                if (p.x < structureBounds_min.x) { structureBounds_min.x = p.x; }
                else if (p.x > structureBounds_max.x) { structureBounds_max.x = p.x; }

                if (p.z < structureBounds_min.z) { structureBounds_min.z = p.z; }
                else if (p.z > structureBounds_max.z) { structureBounds_max.z = p.z; }
            }
            else if (layoutDirection == LayoutDirection.LEFT) {
                if (p.x > structureBounds_min.x) { structureBounds_min.x = p.x; }
                else if (p.x < structureBounds_max.x) { structureBounds_max.x = p.x; }

                if (p.z > structureBounds_min.z) { structureBounds_min.z = p.z; }
                else if (p.z < structureBounds_max.z) { structureBounds_max.z = p.z; }
            }

            // height is the same for both layout directions
            if (p.y < structureBounds_min.y) { structureBounds_min.y = p.y; }
            else if (p.y > structureBounds_max.y) { structureBounds_max.y = p.y; }
        }

    }

}
