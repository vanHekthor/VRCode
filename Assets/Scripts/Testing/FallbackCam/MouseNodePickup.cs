using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.Spawner.ConfigModel;
using VRVis.Spawner.File;
using VRVis.Spawner.Structure;

namespace VRVis.Fallback {

    /// <summary>
    /// The mouse should be able to click on a node.<para/>
    /// The node then gets "attached" / "selected"
    /// and the next click of the mouse will spawn
    /// the code window at this position in the world.
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
                            Debug.Log("Mouse click object selection!", hitObj);

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
                    if (fs) { fs.SpawnFile(attachedNode, spawnPos, spawnRot, WindowSpawnedCallback); }
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
        private void WindowSpawnedCallback(bool success, CodeFile file, string msg) {

            if (!success) {
                Debug.LogError("Failed to place window (" + file.GetNode().GetName() + ")! " + msg);
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

                // destroy old placeholder
                if (placeHolderInstance != null) {
                    Destroy(placeHolderInstance);
                    placeHolderInstance = null;
                }

                // only files can be selected to be spawned
                if (attachedNode.GetNodeType() == SNode.DNodeTYPE.FILE) {

                    // create placeholder
                    placeHolderInstance = Instantiate(nodePrefab, hit.point, Quaternion.identity);
                    curRayDist = hit.distance;
                }
                else { attachedNode = null; }

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

            // in case a node info was found
            if (nInf != null) {

                // find according option by index
                AFeature option = nInf.GetOption();
                if (option != null) {

                    // just switch its selected state
                    if (option is Feature_Boolean) { ((Feature_Boolean) option).SwitchSelected(); }
                                        
                    // ToDo: handle numeric option (Feature_Range)
                    // ToDo: show name and more (e.g. value for num. option) facing user

                    // update color according to new value
                    nInf.UpdateColor();

                    Debug.Log("Clicked at feature model node: " + option.GetName() + " aka. " + option.GetDisplayName() + ", value now: " + option.GetValue());
                }
                else { Debug.LogWarning("Clicked at feature tree node but option not found!"); }

                return;
            }
        }


        /// <summary>
        /// If midle mouse button click on an object occurred.
        /// </summary>
        private void MiddleMouseButtonClick(RaycastHit hit) {

            GameObject hitObj = hit.collider.gameObject;

            // try to find main component with codefile references
            CodeFileReferences refs = hitObj.GetComponent<CodeFileReferences>();

            // try to find somewhere in hierarchy
            if (!refs) {

                Transform codeWindowMain = hitObj.transform;
                while (codeWindowMain.parent != null) {
                                    
                    refs = codeWindowMain.GetComponent<CodeFileReferences>();
                    if (refs) { break; }

                    // go "one layer above" in hierarchy
                    codeWindowMain = codeWindowMain.parent;
                }
            }

            // if found, delete the code window
            if (refs) {

                Debug.LogWarning("Code window deletion request!");

                FileSpawner fs = (FileSpawner) ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
                if (fs && fs.DeleteFileWindow(refs.GetCodeFile())) { Debug.LogWarning("File window removed!"); }
                else { Debug.LogError("Failed to delete code window!"); }
                return;
            }
        }

    }
}
