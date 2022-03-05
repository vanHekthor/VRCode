using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.Spawner.CodeCity;
using VRVis.Spawner.ConfigModel;
using VRVis.Spawner.File;
using VRVis.Spawner.Structure;

namespace VRVis.Fallback {

    /// <summary>
    /// The mouse should be able to click on a node.<para/>
    /// The node then gets "attached" / "selected"
    /// and the next click of the mouse will spawn
    /// the code window at this position in the world.<para/>
    /// Created: 2019 (Leon H.)<para/>
    /// Updated: 13.09.2019
    /// </summary>
    public class MouseNodePickup : MonoBehaviour {

        public Camera cam;
        public float rayDist = 20.0f;
        public GameObject nodePrefab;
        
        private GameObject hitObj;
        private SNode attachedNode;
        private Vector3 mousePos;
        private Ray mouseRay;
        private bool buttonWasDown = false;
	    private GameObject placeHolderInstance;
        private float curRayDist = 0;
        private float placeHolderDist = 0;

        private Action<CodeFileReferences> nodePlacedCallback;
        private string selectedConfig;

        public class MousePickupEventData : PointerEventData {
        
            private readonly MouseNodePickup mnp = null;

            public MousePickupEventData(EventSystem eventSystem, MouseNodePickup mnp) : base(eventSystem) {
                this.mnp = mnp;
            }

            public MouseNodePickup GetMNP() { return mnp; }

        }


	    void Update () {
		
            // show the currently selected position using a sphere
            if (attachedNode != null) {

                // https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
                mousePos = Input.mousePosition;
            
                // https://docs.unity3d.com/ScriptReference/Camera.ScreenPointToRay.html
                mouseRay = cam.ScreenPointToRay(mousePos);
                Debug.DrawRay(mouseRay.origin, mouseRay.direction, Color.red);

                // set user specified distance
                placeHolderDist = curRayDist;

                // shorten distance on collision
                RaycastHit hit;
                if (Physics.Raycast(mouseRay, out hit, rayDist)) {
                    placeHolderDist = hit.distance;
                }

                // move the node preview to position
                Transform phTransform = placeHolderInstance.GetComponent<Transform>();
                if (phTransform) {
                    phTransform.position = mouseRay.origin + mouseRay.direction * placeHolderDist;
                }
            }

            // if user holds mouse down
            bool lmb = Input.GetMouseButton(0);
            bool mmb = Input.GetMouseButton(2);

            if ((lmb || mmb) && !buttonWasDown) {
                buttonWasDown = true;

                if (attachedNode == null) {

                    // https://docs.unity3d.com/ScriptReference/Input-mousePosition.html
                    mousePos = Input.mousePosition;
            
                    // https://docs.unity3d.com/ScriptReference/Camera.ScreenPointToRay.html
                    mouseRay = cam.ScreenPointToRay(mousePos);
                    Debug.DrawRay(mouseRay.origin, mouseRay.direction, Color.red);

                    RaycastHit hit;
                    if (Physics.Raycast(mouseRay, out hit, rayDist)) {

                        if (hit.collider.gameObject != null) {

                            hitObj = hit.collider.gameObject;
                            //Debug.Log("Mouse click on game object", hitObj);

                            // notify event handlers
                            MousePickupEventData data = new MousePickupEventData(EventSystem.current, this) {
                                pointerCurrentRaycast = new RaycastResult() { distance = hit.distance, gameObject = hitObj, worldPosition = hit.point }
                            };
                            ExecuteEvents.Execute(hitObj, data, ExecuteEvents.pointerClickHandler);

                            // check if object has required component
                            if (lmb) { LeftMouseButtonClick(hit); }
                            else if (mmb) { MiddleMouseButtonClick(hit); }
                        }
                    }
                    else {
                        attachedNode = null;
                        hitObj = null;
                    }
                }
                else {
                    
                    // wrong button so abort
                    if (!lmb) {
                        Debug.Log("Placing code window aborted by user.");
                        Destroy(placeHolderInstance);
                        attachedNode = null;
                        return;
                    }

                    // code window spawn position
                    Vector3 spawnPos = placeHolderInstance.transform.position;

                    // get rotation towards camera
                    Vector3 relPos = cam.transform.position - spawnPos;
                    Quaternion spawnRot = Quaternion.LookRotation(-relPos);
                    spawnRot.x = spawnRot.z = 0;

                    // spawn the code window
                    spawnPos += Vector3.up * 0.5f;
                    Debug.Log("Spawning code window at position: " + spawnPos);

                    FileSpawner fs = (FileSpawner) ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
                    if (fs) { fs.SpawnFile(attachedNode, selectedConfig, spawnPos, spawnRot, WindowSpawnedCallback); }
                    else { WindowSpawnedCallback(false, null, "Missing FileSpawner!"); }

                    // cleanup
                    Destroy(placeHolderInstance);
                    attachedNode = null;
                }
            }
            else if (!lmb && !mmb && buttonWasDown) {
                buttonWasDown = false;
            }

	    } // Update() end


        /// <summary>
        /// Called after the window placement finished.
        /// </summary>
        private void WindowSpawnedCallback(bool success, CodeFileReferences fileInstance, string msg) {
            nodePlacedCallback?.Invoke(fileInstance);

            if (!success) {
                string name = "";
                if (fileInstance != null && fileInstance.GetCodeFile().GetNode() != null) {
                    name = "(" + fileInstance.GetCodeFile().GetNode().GetName() + ") ";
                }
                Debug.LogError("Failed to place window! " + name + msg);
                return;
            }
        }


        /// <summary>
        /// If left mouse button click on an object occurred.
        /// </summary>
        private void LeftMouseButtonClick(RaycastHit hit) {
       
            GameObject hitObj = hit.collider.gameObject;

            StructureNodeInfo nodeInfo = hitObj.GetComponent<StructureNodeInfo>();
            StructureNodeInfoV2 nodeInfoV2 = hitObj.GetComponent<StructureNodeInfoV2>();
            
            bool valid = true;
            if (nodeInfo != null) { attachedNode = nodeInfo.GetSNode(); }
            else if (nodeInfoV2 != null) { attachedNode = nodeInfoV2.GetSNode(); }
            else { valid = false; }

            if (valid) {

                // remember hit object so that next click can not be on same object
                hitObj = hit.collider.gameObject;
                Debug.Log("Mouse click at node: " + attachedNode.GetName());
                AttachFileToSpawn(attachedNode, ConfigManager.GetInstance().DefaultConfig.Name, hit.point);
                return;
            }


            // check if a feature model node was selected
            Transform nodeWithInfo = hitObj.transform;
            VariabilityModelNodeInfo nInf = null;

            do {
                nInf = nodeWithInfo.GetComponent<VariabilityModelNodeInfo>();
                if (nInf != null) { break; }
                nodeWithInfo = nodeWithInfo.parent;
            }
            while (nodeWithInfo.parent != null);


            // other click handling like for variability model and code city
            // is performed over PointerClickEvent in their according code
        }


        /// <summary>
        /// Attach the previous so that user can select position to spawn file at.
        /// </summary>
        /// <param name="initPos">Position to initialize place holder instance at</param>
        public bool AttachFileToSpawn(SNode node, string configName, Vector3 initPos, Action<CodeFileReferences> callback = null) {

            nodePlacedCallback = callback;

            if (node == null) { return false; }
            attachedNode = node;

            selectedConfig = configName;

            // destroy possible old placeholder
            if (placeHolderInstance != null) {
                Destroy(placeHolderInstance);
                placeHolderInstance = null;
            }

            // only files can be selected to be spawned
            if (attachedNode.GetNodeType() == SNode.DNodeTYPE.FILE) {

                // create placeholder
                placeHolderInstance = Instantiate(nodePrefab, initPos, Quaternion.identity);
                curRayDist = Vector3.Distance(transform.position, initPos);
            }
            else {
                attachedNode = null;
                return false;
            }

            return true;
        }


        /// <summary>
        /// If midle mouse button click on an object occurred.
        /// </summary>
        private void MiddleMouseButtonClick(RaycastHit hit) {

            GameObject hitObj = hit.collider.gameObject;

            // try to find main component with codefile references
            CodeFileReferences fileInstance = hitObj.GetComponent<CodeFileReferences>();

            // try to find somewhere in hierarchy
            if (!fileInstance) {

                Transform codeWindowMain = hitObj.transform;
                while (codeWindowMain.parent != null) {
                                    
                    fileInstance = codeWindowMain.GetComponent<CodeFileReferences>();
                    if (fileInstance) { break; }

                    // go "one layer above" in hierarchy
                    codeWindowMain = codeWindowMain.parent;
                }
            }

            // if found, delete the code window
            if (fileInstance) {

                Debug.LogWarning("Code window deletion request!");

                FileSpawner fs = (FileSpawner) ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
                if (fs && fs.DeleteFileWindow(fileInstance)) { Debug.LogWarning("File window removed!"); }
                else { Debug.LogError("Failed to delete code window!"); }
                return;
            }
        }

    }
}
