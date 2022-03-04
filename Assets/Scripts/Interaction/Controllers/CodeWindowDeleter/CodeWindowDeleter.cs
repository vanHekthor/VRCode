using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;

namespace VRVis.Interaction.Controller {

    /// <summary>
    /// Written by github.com/S1r0hub<para/>
    /// 
    /// Created: 2019/02/28<para/>
    /// Updated: 2019/03/30<para/>
    /// 
    /// Controller to select code windows using trigger button and trackpad click to delete them.
    /// </summary>
    [RequireComponent(typeof(Interactable))]
    public class CodeWindowDeleter : MonoBehaviour {

        public SteamVR_Action_Boolean selectButton;
        public SteamVR_Action_Boolean confirmButton;

        [Tooltip("Ray mask for selection")]
        public LayerMask rayMask;
        public Transform rayOrigin;
        public float laserDistance = 200f;
        public float laserThickness = 0.01f;
        public Color laserColor;
       
        [Tooltip("Prefab added to objects showing that they are selected for deletion.")]
        public GameObject crossPrefab;

        // should be true for now - maybe something else is required in future versions so this is prepared
        [Space]
        public bool deleteCodeWindowsOnly = true;

        [Tooltip("Tag of the main component of code windows.")]
        public string codeWindowTag = "CodeWindow";

        [Tooltip("Deleting a code window can take a while so wait for x seconds before deleting the next one.")]
        public float waitAfterCodeWindowDeleted = 0.5f;

        // now assigned by pickup event
        protected Hand controller;

        private Ray ray;
        private Vector3 lastRayPoint;
        private RaycastHit rcHit;

        private GameObject pointerObject;
        private static Material ptrMaterial;
        private MeshRenderer laser_mr;

        private bool hit = false;
        private HashSet<GameObject> selectedObjects = new HashSet<GameObject>();
        private Dictionary<GameObject, GameObject> crossDict = new Dictionary<GameObject, GameObject>();
        private GameObject hitObject; // always holds the last hit object
        private bool deletionRunning = false;

        private Coroutine currentDeletionProcess;

        
        void Awake() {

            if (!rayOrigin) { rayOrigin = transform; }
        }


        void Start() {

            // create laser and hit object
            CreatePointer();
        }


        /// <summary>Draw icons in editor showing ray origin.</summary>
        private void OnDrawGizmos() {
         
            // show the laser origin
            if (rayOrigin) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(rayOrigin.position, 0.01f);
                Gizmos.DrawRay(rayOrigin.position, transform.forward * 0.1f);
            }
        }


        /// <summary>Called by the "hand" scripts update method.</summary>
        void HandUpdateCall () {

            // use ray and get position
            ray = new Ray(rayOrigin.position, rayOrigin.forward);
            float hitDistance = laserDistance;
            hit = false;

            // perform physics raycast and get results
            if (Physics.Raycast(ray, out rcHit, laserDistance, rayMask)) {
                hitDistance = rcHit.distance;
                hitObject = rcHit.collider.gameObject;
                hit = true;
            }

            // scale and position the laser "ray"
            if (pointerObject) {
                pointerObject.transform.localScale = new Vector3(laserThickness, laserThickness, hitDistance);
                pointerObject.transform.position = ray.origin + hitDistance * 0.5f * ray.direction;
            }

            HandleButtonPress();
        }


        /// <summary>Create and prepare laser pointer and placement object.</summary>
        private void CreatePointer() {

            // create laser pointer
            if (!pointerObject) {
                pointerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pointerObject.transform.SetParent(transform, false);
                pointerObject.transform.localScale = new Vector3(laserThickness, laserThickness, 100.0f);
                pointerObject.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);
                pointerObject.SetActive(true);

                // destroy collider
                DestroyImmediate(pointerObject.GetComponent<BoxCollider>());

                // create pointer material
                if (!ptrMaterial) {
                    ptrMaterial = new Material(Shader.Find("VRVis/LaserPointer"));
                    ptrMaterial.SetColor("_Color", Color.white); // set default color
                }

                // get mesh renderer components and apply default color
                laser_mr = pointerObject.GetComponent<MeshRenderer>();
                if (laser_mr) { laser_mr.material = ptrMaterial; }
                SetLaserColor(laserColor);
            }
        }


        /// <summary>
        /// Takes care of handling the button press events.
        /// </summary>
        private void HandleButtonPress() {

            if (hit && TriggerButtonClicked()) {

                // deselect if clicked on this object again
                if (hitObject == null) { return; }
                if (selectedObjects.Contains(hitObject)) { DeselectObject(hitObject); }
                else { SelectObject(hitObject); }
                return;
            }

            if (!deletionRunning && TeleportButtonClicked()) {

                // start deletion process
                if (selectedObjects.Count == 0) {
                    Debug.Log("No objects selected to delete.");
                    return;
                }

                Debug.Log("Starting deletion process. Objects: " + selectedObjects.Count);
                currentDeletionProcess = StartCoroutine(ExecuteDeletion());
            }
        }


        /// <summary>
        /// Object selected to be deleted.<para/>
        /// Deselects the object if it is already selected.<para/>
        /// Not possible if a deletion process is running.<para/>
        /// Returns true if the object was selected.
        /// </summary>
        private bool SelectObject(GameObject obj) {
            
            if (deletionRunning) {
                Debug.LogWarning("Failed to add new element. Deletion process is running!");
                return false;
            }

            // check if code window
            if (deleteCodeWindowsOnly) {
                
                // get the code window and add the main element
                CodeFileReferences fileRefs;
                if (IsCodeWindow(obj, out fileRefs)) {

                    // deselect bc. clicked again
                    if (selectedObjects.Contains(fileRefs.gameObject)) {
                        DeselectObject(fileRefs.gameObject);
                        return false;
                    }

                    AddCrossVisual(fileRefs.gameObject);
                    return selectedObjects.Add(fileRefs.gameObject);
                }

                return false;
            }

            if (selectedObjects.Contains(obj)) {
                DeselectObject(obj);
                return false;
            }
            
            return selectedObjects.Add(obj);
        }


        /// <summary>Object no longer selected to be deleted.</summary>
        private bool DeselectObject(GameObject obj) {

            // remove cross if it exists
            if (crossDict.ContainsKey(obj)) {
                Destroy(crossDict[obj]);
                crossDict.Remove(obj);
            }

            return selectedObjects.Remove(obj);
        }


        /// <summary>Add cross to the object showing that it should be deleted.</summary>
        private void AddCrossVisual(GameObject obj) {

            GameObject crossInstance = Instantiate(crossPrefab);
            crossInstance.transform.SetParent(obj.transform, false);
            crossDict.Add(obj, crossInstance);
        }


        /// <summary>Starts the deletion progress deleting all selected objects.</summary>
        private IEnumerator ExecuteDeletion() {

            deletionRunning = true;

            // remove all selected objects
            uint deleted = 0;
            foreach (GameObject go in selectedObjects) {

                // get the CodeFileReference script of code windows to be able to delete them
                CodeFileReferences fileInstance;
                bool isCodeWindow = IsCodeWindow(go, out fileInstance);
                if (!isCodeWindow && deleteCodeWindowsOnly) { continue; }

                // remove cross if exists
                if (crossDict.ContainsKey(go)) {
                    Destroy(crossDict[go]);
                    crossDict.Remove(go);
                }

                // try to delete the code window
                if (isCodeWindow) {

                    Debug.LogWarning("Deleting a code window...");

                    FileSpawner fs = (FileSpawner) ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
                    if (!fs || !fs.DeleteFileWindow(fileInstance)) {
                        Debug.LogWarning("Failed to delete code window!", go);
                    }
                    else {
                        deleted++;
                        yield return new WaitForSecondsRealtime(waitAfterCodeWindowDeleted);
                    }

                    continue;
                }

                // simple object deletion
                if (go != null) { Destroy(go); }
                deleted++;
            }

            // remove possible left cross objects
            foreach (var entry in crossDict) { Destroy(entry.Value); }
            crossDict.Clear();

            // clear selection list
            int selectedTotal = selectedObjects.Count;
            selectedObjects.Clear();
            deletionRunning = false;
            Debug.Log("Selected objects deleted: " + deleted + "/" + selectedTotal);
            yield return null;
        }


        /// <summary>
        /// Checks if this object is a CodeWindow by searching for the references script.
        /// </summary>
        private bool IsCodeWindow(GameObject obj, out CodeFileReferences fileRefs) {

            fileRefs = obj.GetComponent<CodeFileReferences>();
            if (fileRefs != null) { return true; }

            // if references script not directly found,
            // try to find main transform of a code window
            Transform codeWindowMain = obj.transform;
            bool mainTransformFound = true;

            if (!codeWindowMain.tag.Equals(codeWindowTag)) {

                mainTransformFound = false;
                while (codeWindowMain.parent != null) {
                    
                    if (codeWindowMain.tag.Equals(codeWindowTag)) {
                        mainTransformFound = true;
                        break;
                    }

                    // go "one layer above" in hierarchy
                    codeWindowMain = codeWindowMain.parent;
                }
            }

            // if found, try to get references and return according state
            if (!mainTransformFound) { return false; }
            fileRefs = codeWindowMain.GetComponent<CodeFileReferences>();
            return fileRefs != null;
        }


        public void SetLaserColor(Color color) {
            if (laser_mr) { laser_mr.material.color = color; }
        }

        public void ResetLaserColor() { SetLaserColor(laserColor); }


        /// <summary>True if the user started pressing the button (short amount of time).</summary>
        public bool TriggerButtonClicked() {
            return selectButton.GetStateDown(controller.handType);;
        }

        /// <summary>True if the user started pressing the button (short amount of time).</summary>
        public bool TeleportButtonClicked() {
            return confirmButton.GetStateDown(controller.handType);
        }


        /// <summary>Cleanup spawned objects and stop deletion coroutine.</summary>
        void OnDestroy() {

            // stop possible deletion process
            if (deletionRunning && currentDeletionProcess != null) {
                Debug.LogWarning("Stopping running deletion process!");
                StopCoroutine(currentDeletionProcess);
            }

            // remove remaining cross instances
            foreach (var e in crossDict) { Destroy(e.Value); }

            // clean up stored references
            crossDict.Clear();
            selectedObjects.Clear();
        }


        // =========================== STEAM VR REQUIRED METHODS =========================== //

        //-------------------------------------------------
        private void OnAttachedToHand(Hand attachedHand) {

            controller = attachedHand;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Debug.Log("CodeWindowDeleter attached to hand: " + attachedHand.name);
	    }

	    //-------------------------------------------------
        // Equal to Update Method!
        // Will be called by "Hand.cs -> protected virtual void Update()" every frame.
        // Only called by the hand that this object is attached to!
	    private void HandAttachedUpdate(Hand hand) {

		    // Reset transform since we cheated it right after getting poses on previous frame
		    //transform.localPosition = Vector3.zero;
		    //transform.localRotation = Quaternion.identity;

            // perform a laser update call
            HandUpdateCall();
        }

	    //-------------------------------------------------
	    private void OnHandFocusLost(Hand hand) {
		    gameObject.SetActive(false);
	    }


	    //-------------------------------------------------
	    private void OnHandFocusAcquired(Hand hand) {
		    gameObject.SetActive(true);
		    OnAttachedToHand(hand);
	    }

        /// <summary>Important so that the controller and its components get deleted after detach.</summary>
	    private void OnDetachedFromHand(Hand hand) {
            Debug.Log("CodeWindowDeleter detached from hand!");
		    Destroy(gameObject);
	    }

        // =========================== STEAM VR REQUIRED METHODS END =========================== //

    }
}
