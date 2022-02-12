using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.Controller.Mover;
using VRVis.Interaction.ControllerSelectionSystem;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.UI.CodeWindowScreen;
using VRVis.UI.Helper;
using VRVis.Utilities;

namespace VRVis.Interaction.Controller {

    /// <summary>
    /// Written by github.com/S1r0hub<para/>
    /// Created: 2019/01/10<para/>
    /// Updated: 2019/02/28<para/>
    /// </summary>
    /// Recently added:
    /// - player can change rotation of code file using touchpad
    /// - player can change distance of laser using touchpad
    [RequireComponent(typeof(Interactable))]
    public class CodeWindowMover : MonoBehaviour {

        public bool isActive = false;
        
        public SteamVR_Action_Boolean triggerButton;
        public SteamVR_Action_Vector2 trackpadPosition;
        public SteamVR_Action_Boolean trackpadButton;

        public static ControllerSelection.SelectableController selController;

        public Transform rayOrigin;
        public float laserThickness = 0.01f;
        public Color laserColor;

        public GameObject placementPrefab;
        public float placementPrefabScale = 0.1f;
        public Color placementColor;

        [Tooltip("Rotation offset of the placement preview")]
        public Vector3 placementRotationOffset = new Vector3(0, 180, 0);

        [Tooltip("Position offset of spawned file to selected position")]
        public Vector3 fileSpawnOffset = new Vector3(0, 0.25f, 0);

        [Tooltip("Tag assigned to the code windows main transform")]
        public string codeWindowTag = "CodeWindow";

        [Tooltip("Ray mask for when nothing is selected yet")]
        public LayerMask rayMask;
        
        [Tooltip("Ray mask for when a node to spawn is selected")]
        public LayerMask rayMaskNodeSelected;

        [Tooltip("Ray mask for when a window to move is selected")]
        public LayerMask rayMaskObjectSelected;

        [System.Serializable]
        public class LaserSettings {

            [Tooltip("Maximum distance of the ray to detect intersections")]
            public float maxRayDistance = 30;

            [Header("Laser")]
            [Tooltip("Maximum distance of the laser ray when something is selected")]
            public float maxLaserDistance = 20;
            public float minLaserDistance = 1;
            public float defaultLaserDistance = 10;

            [Tooltip("Threshold to start detecting laser distance change")]
            public float laserDistanceThreshold = 0.1f;
            public float distanceChangeSpeed = 100;
            public bool invertDirection = false;

            [Header("Rotation")]
            [Tooltip("Threshold to start detecting rotation")]
            public float rotationThreshold = 0.1f;
            public float rotationSpeed = 100;
            public bool invertRotation = true;
        }

        public LaserSettings laserSettings = new LaserSettings();

        // now assigned by pickup event
        protected Hand controller;

        private Ray ray;
        private Vector3 lastRayPoint;
        private RaycastHit rcHit;
        private float lastRayHitDistance = 0;

        private GameObject pointerObject;
        private static Material ptrMaterial;
        private MeshRenderer laser_mr;

        private GameObject placementObject;
        private MeshRenderer placement_mr;

        private bool hit = false;
        private bool pressed = false;
        private bool switchToPreviousControllerWhenDone = false;
        private bool rotationModified = false; // tells if the user modified the rotation
        private bool teleportButtonLocked = false;

        private bool placeOntoSphereScreen = false;

        private Action nodePlacedCallback;

        /// <summary>Moveable instance of the selected object</summary>
        private Movable selectedObject;

        /// <summary>Selected node of a code file for which the according code window should be spawned</summary>
        private SNode selectedNode;

        // store time of last touchpad button press to detect click event
        private float touchpadLastDownTime = 0;


        void Awake() {
            if (!rayOrigin) { rayOrigin = transform; }
            lastRayHitDistance = laserSettings.maxRayDistance;
        }


        void Start() {

            CreatePointer();

            // validate default laser distance
            if (laserSettings.defaultLaserDistance > laserSettings.maxLaserDistance) {
                laserSettings.defaultLaserDistance = laserSettings.maxLaserDistance;
            }
            else if (laserSettings.defaultLaserDistance < laserSettings.minLaserDistance) {
                laserSettings.defaultLaserDistance = laserSettings.minLaserDistance;
            }

            FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
            placeOntoSphereScreen = fs.SpawnOntoSphericalScreen;
        }


        /// <summary>Create and prepare laser pointer and placement object.</summary>
        private void CreatePointer() {

            // create laser pointer
            //if (!pointerObject) {
            //    pointerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    pointerObject.transform.SetParent(transform, false);
            //    pointerObject.transform.localScale = new Vector3(laserThickness, laserThickness, 100.0f);
            //    pointerObject.transform.localPosition = new Vector3(0.0f, 0.0f, 50.0f);
            //    pointerObject.SetActive(true);

            //    // destroy collider
            //    DestroyImmediate(pointerObject.GetComponent<BoxCollider>());

            //    // create pointer material
            //    if (!ptrMaterial) {
            //        ptrMaterial = new Material(Shader.Find("VRVis/LaserPointer"));
            //        ptrMaterial.SetColor("_Color", Color.white); // set default color
            //    }

            //    // get mesh renderer components and apply default color
            //    laser_mr = pointerObject.GetComponent<MeshRenderer>();
            //    if (laser_mr) { laser_mr.material = ptrMaterial; }
            //    SetLaserColor(laserColor);
            //}

            // create placement preview
            if (!placementObject) {
                if (!placementPrefab) {
                    placementObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    placementObject.transform.localScale = Vector3.one * placementPrefabScale;
                }
                else { placementObject = Instantiate(placementPrefab); }

                placementObject.name = "Placement Preview";
                //placementObject.transform.SetParent(transform, false);
                placementObject.transform.localPosition = new Vector3(0.0f, 0.0f, laserSettings.defaultLaserDistance);
                placementObject.SetActive(false);

                // destroy collider
                DestroyImmediate(placementObject.GetComponent<SphereCollider>());

                // get mesh renderer components and apply default color
                placement_mr = placementObject.GetComponent<MeshRenderer>();
                SetPlacementColor(placementColor);
            }
        }


        /// <summary>Called by the "hand" scripts update method.</summary>
        void HandUpdateCall () {
		    
            // check if the user modified the rotation or laser distance
            CheckForCustomRotationAndDistanceChange();


            // use ray and get position
            hit = false;
            float placementDistance = laserSettings.defaultLaserDistance;
            lastRayHitDistance = placementDistance;
            ray = new Ray(rayOrigin.position, rayOrigin.forward);

            // get the correct ray mask for the current use case
            LayerMask rayMaskToUse = rayMask;
            if (selectedNode != null) { rayMaskToUse = rayMaskNodeSelected; }
            else if (selectedObject != null) { rayMaskToUse = rayMaskObjectSelected; }

            // perform physics raycast and get results
            if (Physics.Raycast(ray, out rcHit, laserSettings.maxRayDistance, rayMaskToUse)) {
                lastRayPoint = rcHit.point;
                lastRayHitDistance = rcHit.distance;
                hit = true;

                // change placement distance according to raycast hit
                if (IsSomethingSelected()) {

                    // adjust laser distance only, if the hit is closer
                    if (rcHit.distance < placementDistance) { placementDistance = rcHit.distance; }
                    else { hit = false; }
                }
                else { placementDistance = rcHit.distance; }
            }
            
            // calculate correct ray point if there was no hit
            if (!hit) {
                lastRayPoint = ray.origin + ray.direction * placementDistance;
            }


            // scale and position the laser "ray"
            if (pointerObject) {
                pointerObject.transform.localScale = new Vector3(laserThickness, laserThickness, placementDistance);
                pointerObject.transform.position = ray.origin + placementDistance * 0.5f * ray.direction;
            }


            // selecting the position of where to spawn a code window
            if (selectedNode != null) {
                WindowPlacementUpdate(placementDistance);
            }

            // moving a selected object (e.g. a code window)
            if (selectedObject != null) { WindowMovementUpdate(placementDistance); }
            else if (selectedNode == null) { // selected object cleanup

                // check if the user selected a code window to move
                if (hit && TriggerButtonDown() && !pressed) {
                    pressed = true;
                    CheckIfCodeWindowSelected(rcHit.collider.gameObject);
                }
                else if (TriggerButtonUp() && pressed) {
                    pressed = false;
                }
            }
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


        /// <summary>Tells if the mover already has something selected.</summary>
        public bool IsSomethingSelected() {
            return selectedNode != null || selectedObject != null;
        }


        /// <summary>Checks if the user modified the rotation or laser distance and applies it.</summary>
        private void CheckForCustomRotationAndDistanceChange() {

            if (!IsSomethingSelected()) { return; }

            // handle locked teleport button (unlock if user releases the button)
            // !! temporaryly commented out before CodeWindowMover gets removed completely !!
            // if (TeleportButtonPressed() && teleportButtonLocked) { return; }
            // else if (!TeleportButtonPressed() && teleportButtonLocked) { teleportButtonLocked = false; }

            // get where the users finger is
            Vector2 fingerPos = GetTrackpadPosition();

            // map rotation to left/right
            RotateSelection(fingerPos.x);

            // map laser distance change to up/down
            ChangeLaserDistance(fingerPos.y);          
        }

        /// <summary>Rotate the selection.</summary>
        /// <param name="value">Between [-1, 1] tells which direction and speed</param>
        private bool RotateSelection(float value) {
            
            if (Mathf.Abs(value) < laserSettings.rotationThreshold) { return false; }
            rotationModified = true;

            // get speed percentage taking threshold into account
            float max = 1 - laserSettings.rotationThreshold;
            float speedPerc = max > 0 ? (Mathf.Abs(value) - laserSettings.rotationThreshold) / max : 0;
            speedPerc *= value > 0 ? 1 : value < 0 ? -1 : 0;

            // invert the rotation if desired
            bool invertRotation = laserSettings.invertRotation;
            if (selectedObject && !selectedObject.defaultRotationSettings) { invertRotation = selectedObject.invertRotation; }
            if (invertRotation) { speedPerc *= -1; }

            // get rotation speed using percentage
            float rotSpeed = laserSettings.rotationSpeed;
            if (selectedObject && !selectedObject.defaultRotationSettings) { rotSpeed = selectedObject.rotationSpeed; }

            // calculate rotation angle based on speed
            float rotationAngle = speedPerc * rotSpeed * Time.deltaTime;
            if (rotationAngle == 0) { return false; }

            // rotate left or right depending on rotation speed sign
            if (selectedNode != null && placementObject != null) {
                placementObject.transform.Rotate(Vector3.up, rotationAngle);
            }
            else if (selectedObject && selectedObject.movableObject) {
                selectedObject.movableObject.transform.Rotate(Vector3.up, rotationAngle);
            }

            return true;
        }

        /// <summary>Change the distance of the laser.</summary>
        /// <param name="value">Between [-1, 1] tells if it should be in-/decreased and by how much (percentage)</param>
        private void ChangeLaserDistance(float value) {

            float laserThreshold = laserSettings.laserDistanceThreshold;
            // !! temporaryly commented out before CodeWindowMover gets removed completely !!
            //if (Mathf.Abs(value) < laserThreshold) { return; }

            // start from this distance if the last hit distance was closer
            if (lastRayHitDistance < laserSettings.defaultLaserDistance) {
                laserSettings.defaultLaserDistance = lastRayHitDistance;
            }

            // get percentage taking threshold into account
            float max = 1 - laserThreshold;
            float speedPerc = max > 0 ? (Mathf.Abs(value) - laserThreshold) / max : 0;
            speedPerc *= value > 0 ? 1 : value < 0 ? -1 : 0;

            // invert direction if desired
            bool invertDirection = laserSettings.invertDirection;
            if (selectedObject && !selectedObject.defaultDistanceSettings) { invertDirection = selectedObject.invertDirection; }
            if (invertDirection) { speedPerc *= -1; }

            // get distance change speed
            float distanceChangeSpeed = laserSettings.distanceChangeSpeed;
            if (selectedObject && !selectedObject.defaultDistanceSettings) { distanceChangeSpeed = selectedObject.distanceChangeSpeed; }

            // get distance change
            float distanceChange = speedPerc * distanceChangeSpeed * Time.deltaTime;
            // !! temporaryly commented out before CodeWindowMover gets removed completely !!
            // if (distanceChange == 0) { return; }

            // move further away or closer depending on the distance change value
            float curLaserDist = laserSettings.defaultLaserDistance;
            float minDist = laserSettings.minLaserDistance;
            float maxDist = laserSettings.maxLaserDistance;
            float newDist = curLaserDist + distanceChange;

            // validate new distance against min and max and apply accordingly
            if (newDist > maxDist) { laserSettings.defaultLaserDistance = maxDist; return; }
            else if (newDist < minDist) { laserSettings.defaultLaserDistance = minDist; return; }
            laserSettings.defaultLaserDistance = newDist;

            // !! temporary change before CodeWindowMover gets removed completely !!
            laserSettings.defaultLaserDistance = 20;
        }
                
        /// <summary>
        /// Take care of selecting a spawn position for the code window.
        /// </summary>
        private void WindowPlacementUpdate(float placementDistance) {
            FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");

            // position the placement preview
            if (placementObject) {
                placementObject.SetActive(true);
                               
                if (!placeOntoSphereScreen) {
                    placementObject.transform.position = ray.origin + placementDistance * ray.direction;

                    //make object look in direction of camera or apply rotation that user changed
                    //(if user changed the rotation, keep it, otherwise rotate to the user)
                    if (!rotationModified) {
                        Vector3 v = rayOrigin.position - lastRayPoint;
                        v.x = v.z = 0;
                        placementObject.transform.LookAt(rayOrigin.position - v);
                        placementObject.transform.Rotate(placementRotationOffset);
                    }

                }
                else {
                    TransformPlaceholderOnSphericalWindowScreen(ref placementObject, ray, placementDistance);
                }                
            }

            // check for button press
            if (TriggerButtonDown() && !pressed) {
                pressed = true;

                // switch controller back to laser
                if (switchToPreviousControllerWhenDone) { SwitchControllerToPrevious(); }

                // define spawn position and spawn code window
                Vector3 spawnPos = lastRayPoint + fileSpawnOffset;
                if (!placeOntoSphereScreen) {
                    spawnPos = lastRayPoint + fileSpawnOffset;
                }
                else {
                    spawnPos = placementObject.transform.position;
                }
                Quaternion spawnRot = placementObject.transform.rotation; // Quaternion.identity                
                
                if (fs) { fs.SpawnFile(selectedNode, spawnPos, spawnRot, WindowSpawnedCallback); }
                else { WindowSpawnedCallback(false, null, "Missing FileSpawner!"); }

                // code window has been placed
                NodePlacedEvent();
                ResetSettings();
            }
            else if (TriggerButtonUp() && pressed) {
                pressed = false;
            }
        }


        /// <summary>
        /// Called after the window placement finished.
        /// </summary>
        private void WindowSpawnedCallback(bool success, CodeFile file, string msg) {
            nodePlacedCallback?.Invoke();

            if (!success) {
                string name = "";
                if (file != null && file.GetNode() != null) { name = "(" + file.GetNode().GetName() + ") "; }
                Debug.LogError("Failed to place window! " + name + msg);
                return;
            }
        }


        /// <summary>
        /// Take care of moving an already spawned code window.
        /// </summary>
        private void WindowMovementUpdate(float distance) {

            if (!placeOntoSphereScreen) {
                // move the code window to where the laser is
                selectedObject.transform.position = ray.origin + distance * ray.direction;

                // make object look in direction of camera or apply rotation that user changed
                // (if user changed the rotation, keep it, otherwise rotate to the user)
                if (!rotationModified) {

                    Vector3 v = rayOrigin.position - lastRayPoint;
                    v.x = v.z = 0;
                    selectedObject.transform.LookAt(rayOrigin.position - v);
                    selectedObject.transform.Rotate(placementRotationOffset);
                }
            }
            else {
                TransformWindowOnSphericalWindowScreen(ref selectedObject, ray, distance);
            }              

            // check for button press
            if (TriggerButtonUp() && pressed) {

                pressed = false;

                // switch controller back to laser
                SetLaserColor(laserColor);
                if (switchToPreviousControllerWhenDone) { SwitchControllerToPrevious(); }

                // deselect the object so that we no longer move it
                selectedObject = null;
                ResetSettings();
            }
        }

        /// <summary>
        /// Handles the transformation for the code window placeholder. When pointing at the window screen 
        /// the placeholder gets displayed on the screen at the intersection point and faces the sphere screen center.
        /// When the window screen also has a SphereGrid component then the placeholder snaps to the location of the
        /// closest grid point (or rather the attachment point of that grid point).
        /// When not pointing at the window screen the placeholder is shown at alternativePlacementDistance from
        /// the laser pointer.
        /// </summary>
        /// <param name="placeholderObject"></param>
        /// <param name="ray"></param>
        /// <param name="alternativePlacementDistance">distance where placeholder is shown when not pointing at the window screen</param>
        private void TransformPlaceholderOnSphericalWindowScreen(ref GameObject placeholderObject, Ray ray, float alternativePlacementDistance) {
            FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");

            Vector3 screenOrigin = fs.WindowScreenTransform.position;

            double t = PositionOnSphere.SphereIntersect(fs.SphereScreenRadius, screenOrigin, ray.origin, ray.direction);
            if (t > Mathf.Epsilon) {
                Vector3 placeholderPosition = ray.origin + (float)t * ray.direction;

                SphereGrid windowGrid = fs.WindowScreen.GetComponent<SphereGrid>();
                if (!windowGrid) {
                    placeholderPosition = FitIntoScreen(placeholderPosition, fs.WindowScreen.GetComponent<SphericalWindowScreen>());               
                } else {
                    placeholderPosition = windowGrid.GetClosestGridPoint(placeholderPosition).AttachmentPoint;
                    if (placeholderPosition == null) {
                        placeholderPosition = FitIntoScreen(placeholderPosition, fs.WindowScreen.GetComponent<SphericalWindowScreen>());
                    }
                }

                placeholderObject.transform.position = placeholderPosition;
                Vector3 lookDirection = placeholderPosition - screenOrigin;
                placeholderObject.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            else {
                placeholderObject.transform.position = ray.origin + alternativePlacementDistance * ray.direction;            
            }
        }

        /// <summary>
        /// Handles the transformation for a selected code window. When pointing at the window screen 
        /// the code window gets displayed on the screen at the intersection point and faces the sphere screen center.
        /// When the window screen also has a SphereGrid component then the code window snaps to the location of the
        /// closest grid point (or rather the attachment point of that grid point).
        /// When not pointing at the window screen the code window is shown at alternativePlacementDistance from
        /// the laser pointer.
        /// </summary>
        /// <param name="windowObject"></param>
        /// <param name="ray"></param>
        /// <param name="alternativePlacementDistance"></param>
        private void TransformWindowOnSphericalWindowScreen(ref Movable windowObject, Ray ray, float alternativePlacementDistance) {
            FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");

            Color moveColor = new Color(0, 1f, 1f, 1f);
            SetLaserColor(moveColor);

            Vector3 screenOrigin = fs.WindowScreenTransform.position;

            double t = PositionOnSphere.SphereIntersect(fs.SphereScreenRadius, screenOrigin, ray.origin, ray.direction);
            if (t > Mathf.Epsilon) {
                Vector3 selectedWindowPosition = ray.origin + (float)t * ray.direction;

                SphereGrid windowGrid = fs.WindowScreen.GetComponent<SphereGrid>();
                if (!windowGrid) {
                    selectedWindowPosition = FitIntoScreen(selectedWindowPosition, fs.WindowScreen.GetComponent<SphericalWindowScreen>());
                }
                else {
                    GridElement gridElement = windowObject.gameObject.GetComponent<GridElement>();
                    windowGrid.DetachGridElement(ref gridElement);

                    SphereGridPoint selectedGridPoint = windowGrid.GetClosestGridPoint(selectedWindowPosition);
                    selectedWindowPosition = selectedGridPoint.AttachmentPoint;

                    windowGrid.AttachGridElement(ref gridElement, selectedGridPoint.LayerIdx, selectedGridPoint.ColumnIdx);

                    if (selectedWindowPosition == null) {
                        selectedWindowPosition = FitIntoScreen(selectedWindowPosition, fs.WindowScreen.GetComponent<SphericalWindowScreen>());
                    }
                }

                windowObject.transform.position = selectedWindowPosition;
                Vector3 lookDirection = windowObject.transform.position - screenOrigin;
                windowObject.transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            else {
                windowObject.transform.position = ray.origin + alternativePlacementDistance * ray.direction;
            }
        }

        /// <summary>
        /// Keeps the point within the contraints defined SphericalWindowScreen component using the inspector.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="screenComponent"></param>
        /// <returns></returns>
        private Vector3 FitIntoScreen(Vector3 point, SphericalWindowScreen screenComponent) {
            GameObject sphereScreen = screenComponent.gameObject;

            SphericalCoordinates sphericalIntersectPoint = PositionOnSphere.CartesianToSpherical(point, sphereScreen.transform.position);

            float radius = sphereScreen.GetComponent<SphereCollider>().radius * sphereScreen.transform.lossyScale.x;
            float polar = sphericalIntersectPoint.polar;
            float elevation = sphericalIntersectPoint.elevation;            

            elevation = Mathf.Clamp(elevation, screenComponent.MinElevationAngleInRadians, screenComponent.MaxElevationAngleInRadians);
            polar = Mathf.Clamp(polar, screenComponent.MinPolarAngleInRadians, screenComponent.MaxPolarAngleInRadians);

            point = PositionOnSphere.SphericalToCartesian(radius, polar, elevation, sphereScreen.transform.position);
            
            return point;
        }


        /// <summary>
        /// Checks if the hit object is a code window and if so,
        /// selects it to be moved from now on by this controller.
        /// </summary>
        private void CheckIfCodeWindowSelected(GameObject hitObj) {
        
            // check if movable component is attached
            Movable movable = hitObj.GetComponent<Movable>();
            if (movable) {
                SelectObjectToMove(movable, false);
                return;
            }

            // if movable not directly found,
            // try to find main transform of a code window
            // and a possible attached movable component
            Transform codeWindowMain = hitObj.transform;
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

            // if found, select it as the selected object to move
            if (mainTransformFound) {
                movable = codeWindowMain.GetComponent<Movable>();
                if (movable) { SelectObjectToMove(movable, false); }  
            }
        }


        public void SetLaserColor(Color color) {
            if (laser_mr) { laser_mr.material.color = color; }
        }

        public void ResetLaserColor() { SetLaserColor(laserColor); }

        public void SetPlacementColor(Color color) {
            if (placement_mr) { placement_mr.material.color = color; }
        }

        public void ResetPlacementColor() { SetPlacementColor(placementColor); }


        /// <summary>Called after the code window was placed.</summary>
        private void NodePlacedEvent() {

            // switch controller back to laser
            //if (switchToPreviousControllerAfterPlacing) { SwitchControllerToPrevious(); }

            // unselect node
            selectedNode = null;
            isActive = false;

            // hide placement preview if shown
            if (placementObject) {
                placementObject.SetActive(false);
                placementObject.transform.rotation = Quaternion.identity; // reset rotation
            }            
        }

        /// <summary>Set selected node that is currently moved.</summary>
        /// <param name="switchToPreviousController">To switch to the previous controller after the window is placed</param>
        /// <param name="clickedAt">The object we initially clicked at (e.g. an element of the code city or structure tree).</param>
        /// <param name="callback">Method that will be called after the node was placed</param>
        public bool SelectNode(SNode node, bool switchToPreviousController, Transform clickedAt = null, Action callback = null) {
            nodePlacedCallback = callback;

            if (IsSomethingSelected()) { return false; }
            if (node == null) { return false; }
            if (node == selectedNode) { return false; }

            switchToPreviousControllerWhenDone = switchToPreviousController;

            selectedNode = node;
            Debug.Log("Node to spawn selected: " + node.GetFullPath());

            // set laser distance to last hit
            float hitDist = clickedAt == null ? -1 : Vector3.Distance(transform.position, clickedAt.position);
            if (hitDist > 0) { SetLaserDistance(hitDist); }
            else if (hit) { SetLaserDistance(rcHit.distance); }

            // user must release the teleport button first before he can use it again
            teleportButtonLocked = TeleportButtonPressed();

            // set pressed true so that user first has to release the trigger button
            pressed = TriggerButtonDown();
            return true;
        }

        /// <summary>Set the object that should be moved.</summary>
        /// <param name="movable">The Movable script of the object that should be moved</param>
        /// <param name="switchToPreviousController">To switch to the previous controller after the window is placed</param>
        public bool SelectObjectToMove(Movable movable, bool switchToPreviousController) {
            
            if (IsSomethingSelected()) { return false; }
            if (movable == null) { return false; }
            if (movable == selectedObject) { return false; }

            switchToPreviousControllerWhenDone = switchToPreviousController;

            selectedObject = movable;
            Debug.Log("Object to move selected: " + movable.name);

            // set laser distance to last hit
            if (hit) { SetLaserDistance(rcHit.distance); }

            // set pressed true so that user first has to release the button
            pressed = true;
            return true;
        }


        /// <summary>
        /// Applies the new laser distance with respect to the min/max range.
        /// </summary>
        private void SetLaserDistance(float newDistance) {

            if (newDistance < laserSettings.minLaserDistance) {
                laserSettings.defaultLaserDistance = laserSettings.minLaserDistance;
                return;
            }

            if (newDistance > laserSettings.maxLaserDistance) {
                laserSettings.defaultLaserDistance = laserSettings.maxLaserDistance;
                return;
            }

            laserSettings.defaultLaserDistance = newDistance;
        }


        /// <summary>Switch to previous controller after selection is made.</summary>
        private void SwitchControllerToPrevious() {

            if (!switchToPreviousControllerWhenDone) { return; }
            switchToPreviousControllerWhenDone = false;

            // get controller selection script from hand
            ControllerSelection selectionScript = controller.GetComponent<ControllerSelection>();
            if (!selectionScript) {
                Debug.LogError("Could not find selection script of hand!");
                return;
            }
            selectionScript.SwitchToPreviousController();
        }


        /// <summary>Reset the modified laser distance and rotation.</summary>
        private void ResetSettings() {        
            laserSettings.defaultLaserDistance = laserSettings.maxLaserDistance;
            rotationModified = false;
        }


        public bool TriggerButtonDown() {
            bool state = triggerButton.GetStateDown(controller.handType);
            //Debug.Log("ButtonDown event (" + state + ")");
            return state;
        }

        public bool TriggerButtonUp() {
            bool state = triggerButton.GetStateUp(controller.handType);
            //Debug.Log("ButtonUp event (" + state + ")");
            return state;
        }

        public Vector2 GetTrackpadPosition() {
            if (trackpadPosition == null) { return Vector2.zero; }
            return trackpadPosition.GetAxis(controller.handType);
        }

        /// <summary>True if the user started pressing the button (short amount of time).</summary>
        public bool TeleportButtonClicked() {
            return trackpadButton.GetStateDown(controller.handType);
        }

        /// <summary>True if the user holds down the button / keep pressing it.</summary>
        public bool TeleportButtonPressed() {
            return trackpadButton.GetState(controller.handType);
        }

        /// <summary>True if the user did hold down the button.</summary>
        public bool TeleportButtonPressedLast() {
            return trackpadButton.GetLastState(controller.handType);
        }


        /// <summary>Cleanup selected objects on deletion.</summary>
        void OnDestroy() {
            if (placementObject) { Destroy(placementObject); }
        }


        // =========================== STEAM VR REQUIRED METHODS =========================== //

	    //-------------------------------------------------
	    private void OnAttachedToHand(Hand attachedHand) {

            controller = attachedHand;
            // transform.localPosition = Vector3.zero;
            // transform.localRotation = Quaternion.identity;
            Debug.Log("CodeWindowMover attached to hand: " + attachedHand.name);
	    }

	    //-------------------------------------------------
        // Equal to Update Method!
        // Will be called by "Hand.cs -> protected virtual void Update()" every frame.
        // Only called by the hand that this object is attached to!
	    private void HandAttachedUpdate(Hand hand) {

            // Reset transform since we cheated it right after getting poses on previous frame
            //transform.localPosition = Vector3.zero;
            //transform.localRotation = Quaternion.identity;

            // perform a laser update call#
            if (isActive) {
                HandUpdateCall();
            }
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
            Debug.Log("CodeWindowMover detached from hand!");
            isActive = false;
		    // Destroy(gameObject);
	    }

        // =========================== STEAM VR REQUIRED METHODS END =========================== //

    }
}
