using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siro.IO;
using Siro.IO.Structure;
using Siro.Effects;


namespace Siro.Rendering {

    public class SpawnStructure : MonoBehaviour {
    
        public enum LayoutDirection { LEFT, RIGHT };
        public LayoutDirection layoutDirection = LayoutDirection.RIGHT;

        // to set the variable in "CodeFile.cs" accordingly
        public LoadFile loadFileScript;

        // prefabs for folders and files
        public GameObject folderPrefab;
        public GameObject filePrefab;
        public Material lineMaterial;

        // empty gameobject that holds structure (should be at position 0, use offset to adjust final position)
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
        private bool done = true;

        // width and height of the prefabs
        private Vector3 folderPrefabSize = Vector3.zero;
        private Vector3 filePrefabSize = Vector3.zero;
        private DNode rootNode;


        class NodeInfo {

            // start position regaring all nodes on this level
            public GameObject gameObject = null;
            public Vector3 pos = Vector3.zero;
            public float width = 0;
            public float height = 0;

            public NodeInfo() {}

            public NodeInfo(NodeInfo info) {
                gameObject = info.gameObject;
                pos = info.pos;
                width = info.width;
                height = info.height;
            }
        }


        /**
         * Show the spawn position of the directory structure
         */
        void OnDrawGizmos() {

            if (!Application.isPlaying) {
                structureRootPos = structureRoot.transform.position;
            }

            Vector3 origin = structureRootPos + structureRootOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, 0.1f);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(structureRootRotation) * Vector3.left);
        }


        void Start() {

            if (!folderPrefab || !filePrefab) {
                Debug.LogError("Prefab missing!");
                return;
            }

            if (!structureRoot) {
                Debug.LogError("Structure root missing!");
                return;
            }

            if (!filePrefab.GetComponent<Renderer>()) {
                Debug.LogError("File prefab has no renderer!");
                return;
            }
            filePrefabSize = filePrefab.GetComponent<Renderer>().bounds.size;

            if (!folderPrefab.GetComponent<Renderer>()) {
                Debug.LogError("Folder prefab has no renderer!");
                return;
            }
            folderPrefabSize =  folderPrefab.GetComponent<Renderer>().bounds.size;
        }


        void Update() {
        
            if (!done && rootNode != null) {
                // spawn the whole structure
                NodeInfo startNI = new NodeInfo();
                startNI.pos = structureRoot.position;
                spawnWholeStructure(rootNode, new NodeInfo());

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
            }
    
        }


        /**
         * Spawns the folder structure.
         * Called by "FilesFromDisk" script.
         */
        public void spawn(DNode rootNode) {
            this.rootNode = rootNode;
            done = false;
        }


        /**
         * Spawn the whole structure at once using recursion.
         * Returns information about the last spawned node.
         */
        NodeInfo spawnWholeStructure(DNode node, NodeInfo prevNode) {
    
            NodeInfo lastChildInfo = new NodeInfo();
            //lastChildInfo.gameObject = prevNode.gameObject;
            lastChildInfo.pos = prevNode.pos;
            lastChildInfo.width = prevNode.width;
            lastChildInfo.height = prevNode.height;

            // store info of each child for later usage to connect them with a line
            List<NodeInfo> childNodes = new List<NodeInfo>();

            // go down to leaf nodes
            if (node.getNodes().Count > 0) {
                lastChildInfo.pos += Vector3.down * 0.3f;
                foreach (DNode subNode in node.getNodes()) {
                    lastChildInfo = spawnWholeStructure(subNode, lastChildInfo);
                    childNodes.Add(new NodeInfo(lastChildInfo));
                }
            }

            // spawn this node
            NodeInfo spawnInfo = spawnNode(node, prevNode);
            bool writeZ = spawnInfo.pos.z > lastChildInfo.pos.z;
            if (layoutDirection == LayoutDirection.LEFT) { writeZ = spawnInfo.pos.z < lastChildInfo.pos.z; }
            if (writeZ) { spawnInfo.pos.z = lastChildInfo.pos.z; }

            // add connection line to the child nodes adding the according script
            if (childNodes.Count > 0) {
                foreach (NodeInfo child in childNodes) {

                    // add line connection script
                    ConnectLine lineScript = child.gameObject.AddComponent<ConnectLine>();
                    lineScript.trackedObject = spawnInfo.gameObject.transform;
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
        NodeInfo spawnNode(DNode node, NodeInfo lastNode) {
    
            GameObject prefab = null;
            NodeInfo info = new NodeInfo();

            // get width, height and prefab according to the type
            if (node.getType() == DNode.DNodeTYPE.FOLDER) {
                info.width = folderPrefabSize.y;
                info.height = folderPrefabSize.z;
                prefab = folderPrefab;
            }
            else if (node.getType() == DNode.DNodeTYPE.FILE) {
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
                updateStructureParentBounds(GOInstance.transform.position);

                // get or if not available, add element info and set information
                ElementInfo fei = GOInstance.GetComponent<ElementInfo>();
                if (!fei) { fei = GOInstance.AddComponent<ElementInfo>(); }
                fei.elementName = node.getName();
                fei.elementPath = node.getPath();
                fei.elementFullPath = node.getFullPath();
                fei.nodeType = node.getType();
                fei.UpdateView();

                // add code file
                if (loadFileScript) {
                    CodeFile cf = GOInstance.GetComponent<CodeFile>();
                    if (!cf) { cf = GOInstance.AddComponent<CodeFile>(); }
                    cf.info = fei; // not required
                    cf.loadFileScript = loadFileScript;
                }
            }

            return info;
        }


        /**
         * Set the min and max positions accordingly for each new node.
         */
        void updateStructureParentBounds(Vector3 p) {

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