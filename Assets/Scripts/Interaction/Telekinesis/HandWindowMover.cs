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
    /// </summary>
    public class HandWindowMover : MonoBehaviour {

        public bool isActive = false;

        public SteamVR_Action_Boolean triggerButton;
        public SteamVR_Action_Vector2 trackpadPosition;
        public SteamVR_Action_Boolean trackpadButton;

        public static ControllerSelection.SelectableController selController;

        public Transform rayOrigin;

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

        public Hand hand;

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

            InitPlacementObject();

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

        void Update() {
            if (isActive) {
                HandUpdateCall();
            }
        }

        /// <summary>Init the placement object.</summary>
        private void InitPlacementObject() {
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
        void HandUpdateCall() {

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

            // selecting the position of where to spawn a code window
            if (selectedNode != null) { WindowPlacementUpdate(placementDistance); }

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
                }
                else {
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
        public bool SelectNode(SNode node, bool switchToPreviousController, Transform clickedAt = null) {

            if (IsSomethingSelected()) { return false; }
            if (node == null) { return false; }
            if (node == selectedNode) { return false; }

            switchToPreviousControllerWhenDone = switchToPreviousController;

            selectedNode = node;
            Debug.Log("Node to spawn selected: " + node.GetFullPath());

            // set laser distance to last hit
            float hitDist = clickedAt == null ? -1 : Vector3.Distance(transform.position, clickedAt.position);

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

            // set pressed true so that user first has to release the button
            pressed = true;
            return true;
        }

        /// <summary>Reset the modified laser distance and rotation.</summary>
        private void ResetSettings() {
            laserSettings.defaultLaserDistance = laserSettings.maxLaserDistance;
            rotationModified = false;
        }

        public bool TriggerButtonDown() {
            bool state = triggerButton.GetStateDown(hand.handType);
            //Debug.Log("ButtonDown event (" + state + ")");
            return state;
        }

        public bool TriggerButtonUp() {
            bool state = triggerButton.GetStateUp(hand.handType);
            //Debug.Log("ButtonUp event (" + state + ")");
            return state;
        }

        public Vector2 GetTrackpadPosition() {
            if (trackpadPosition == null) { return Vector2.zero; }
            return trackpadPosition.GetAxis(hand.handType);
        }

        /// <summary>True if the user started pressing the button (short amount of time).</summary>
        public bool TeleportButtonClicked() {
            return trackpadButton.GetStateDown(hand.handType);
        }

        /// <summary>True if the user holds down the button / keep pressing it.</summary>
        public bool TeleportButtonPressed() {
            return trackpadButton.GetState(hand.handType);
        }

        /// <summary>True if the user did hold down the button.</summary>
        public bool TeleportButtonPressedLast() {
            return trackpadButton.GetLastState(hand.handType);
        }

        /// <summary>Cleanup selected objects on deletion.</summary>
        void OnDestroy() {
            if (placementObject) { Destroy(placementObject); }
        }
    }
}
