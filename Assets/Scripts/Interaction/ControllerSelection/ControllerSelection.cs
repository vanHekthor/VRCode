using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;


namespace VRVis.Interaction.ControllerSelectionSystem {

    /// <summary>Added to controllers to identify valid objects on selection easily.</summary>
    public class RadialController : MonoBehaviour {}

    /// <summary>
    /// Script that should be attached to a hand.<para/>
    /// It allows to select from a set of controllers.<para/>
    /// The currently selected controller can be used.
    /// </summary>
    //[RequireComponent(typeof(Hand))] // disabled for fallback tests
    public class ControllerSelection : MonoBehaviour { 

        [Tooltip("Button to show and hide and radial menu")]
        public SteamVR_Action_Boolean toggleButton;

        [Tooltip("Optional second button that needs to be active to open the menu")]
        public SteamVR_Action_Boolean toggleButton2;
        
        [Tooltip("Rotation of the radial plane")]
        public Vector3 radialPlaneRotation = new Vector3(-40, 0, 0);

        [Tooltip("How far the controllers are away from the center")]
        public float radialRadius = 1f;

        [Tooltip("Spawn controllers from left to right")]
        public bool spawnLeftRight = true;

        [Tooltip("If the highlighter object should be searched and deleted if the radial menu is closing")]
        public bool deleteHighlighter = true;
        public string highlighterName = "Highlighter";

        [Tooltip("If given, will be used to show controller information")]
        public GameObject infoUIPrefab;

        [Tooltip("Set hand hover layermask while radial menu is opened")]
        public LayerMask hoverLayerMask;
        private LayerMask previousMask;
        private bool previousMaskSet = false;
        private float lastHoverTime;

        // holds information about selectable controllers
        [System.Serializable]
        public class SelectableController {

            public string name = "";
            public string description = "";
            public bool showUI = true;
            public float uiYRotation = 0;

            [Tooltip("If disabled, this wont be treated like a controller that should be attached to the hand")]
            public bool attachToHand = true;

            [Tooltip("Optional if given in ItemPackage but needs Interactable component!")]
            public GameObject previewPrefab;

            public ItemPackage itemPackage;
            public float previewScale = 0.1f; // to scale down/up preview prefab

		    [EnumFlags] public Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags;

            public SelectableController() {
                previewScale = 0.1f;
                attachmentFlags = Hand.defaultAttachmentFlags;
                attachToHand = true;
                showUI = true;
            }

            // the current game object instance in the radial menu
            [HideInInspector()]
            public GameObject currentRadialInstance;
        }

        public SelectableController[] controllers;
        private SelectableController selectedController;
        private SelectableController selectedController_previous;

        // hand that can open the radial menu
        private Hand hand;
        private bool toggleLast = false;
        private bool showingRadial = false;
        private GameObject spawnedItem; // spawned controller item
		private bool itemIsSpawned = false;
        private bool itemSelected = false; // if user selected an item

        // hand attachment flags of current controller
		private Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags;

        // item package of selected controller
        private ItemPackage itemPackage;

        // an instance of a controller on the radial
        public class RadialObject {
            public GameObject gameObject;
            public SelectableController controller;
        }
        private RadialObject[] radialObjects;
        private Transform radialParent; // holds radialObjects

        
        private void Awake() {

            if (!hand) { hand = GetComponent<Hand>(); }
            if (!hand) { Debug.LogError("Missing required \"Hand\" script!", this); }

            if (!infoUIPrefab) { Debug.LogWarning("Info UI not assigned!", this); }

            if (toggleButton == null) { Debug.LogError("Toggle button not assigned!", this); }
        }


        void Update () {

            if (hand.hoveringInteractable) { lastHoverTime = Time.time; }

            if (selectedController != null) { hand.hoverLayerMask = hoverLayerMask; }

            // when user holds the toggle button
            if (ToggleDown()) {

                if (!toggleLast) {

                    toggleLast = true;
                    //Debug.Log("Pressed toggle button");

                    // don't open if we recently hovered over something
                    if (!showingRadial && Time.time < lastHoverTime + 0.2f) { return; }

                    // detach last selected controller first
                    DetachController();
                    itemSelected = false;

                    // show the radial controller selection
                    Vector3 forwardVec = hand.transform.forward * 0.75f + hand.transform.up * 0.1f;
                    ShowRadial(hand.transform, forwardVec, hand.transform.right);
                }
            }

            // if user releases the toggle button
            else {

                // if the button was previously active
                if (toggleLast) {
                    //Debug.Log("Released toggle button");

                    // check if the user interacted with a controller
                    Interactable interactable = hand.hoveringInteractable;
                    if (interactable != null && interactable.GetComponent<RadialController>()) {

                        itemSelected = true;

                        // get the selected controller to get item package
                        SelectableController selection = GetSelectedController(interactable.gameObject);
                        if (selection == null) { Debug.LogError("Failed to find selected controller instance!"); }

                        // attach controller to hand
                        AttachController(selection);
                    }
                    else {

                        // remove the selected controller
                        selectedController = null;

                        // change hover layermask of hand back to normal
                        if (previousMaskSet) {
                            hand.hoverLayerMask = previousMask;
                            previousMaskSet = false;
                        }
                    }

                    // hide the radial controller selection menu
                    HideRadial();
                }

                toggleLast = false;
            }
	    }


        /// <summary>Tells if user is pressing the button to show radial selection.</summary>
        private bool ToggleDown() {

            if (hand == null) { return false; }
            if (toggleButton == null) { return false; }

            bool active = toggleButton.GetState(hand.handType);

            if (toggleButton2 != null) {
                active = active && toggleButton2.GetState(hand.handType);
            }

            return active;
        }


        /// <summary>Detach the last selected controller from hand.</summary>
        public void DetachController() {

            // detach the currently selected controller when user opens menu
            if (selectedController != null) {
			    ItemPackage currentAttachedItemPackage = GetAttachedItemPackage(hand);
			    if (currentAttachedItemPackage == itemPackage && itemPackage != null) {
                    TakeBackItem(hand); // removes this item package from hand
                }
            }
        }


        /// <summary>
        /// Attach a controller.
        /// Returns true if successful. 
        /// </summary>
        public bool AttachController(SelectableController ctrl) {

            if (ctrl == null) { return false; }

            // store previous selection
            selectedController_previous = selectedController;
            selectedController = ctrl;

            // treat as controller to attach to hand i.e. an actual controller was selected and not a button. For button look at the corresponding "else".
            if (ctrl.attachToHand) {
                
                // detach current controller first
                DetachController();

                // get and set item package and attachment flags and spawn controller
                itemPackage = ctrl.itemPackage;
                if (!itemPackage) {
                    Debug.LogError("Failed to attach controller! (Missing ItemPackage)");
                    return false;
                }
                else {
                    attachmentFlags = ctrl.attachmentFlags;
                    Debug.Log("User selected controller: " + ctrl.itemPackage.name);

                    // attach selected controller to the hand
                    SpawnAndAttachObject(hand, GrabTypes.Scripted);
                    if (itemIsSpawned && spawnedItem != null) {
                        Debug.Log("Controller item spawned (" + ctrl.name + ")");
                        return true;
                    }
                }
            }
            else {

                // treat as something that just activates something (e.g. the terminal open/close button)
                if (selectedController.currentRadialInstance != null) {
                    selectedController.currentRadialInstance.SendMessage("Use");
                }

                // change hover layermask of hand back to normal
                if (previousMaskSet) {
                    hand.hoverLayerMask = previousMask;
                    previousMaskSet = false;
                }

                selectedController = null;
                return true;
            }

            return false;
        }

        /// <summary>Attach a controller defined in the array.</summary>
        public bool AttachController(int indexInArray) {
            if (indexInArray < 0 || indexInArray > controllers.Length) { return false; }
            return AttachController(controllers[indexInArray]);
        }

        /// <summary>Returns the controller definition at this index or null.</summary>
        public SelectableController GetController(int indexInArray) {
            if (indexInArray < 0 || indexInArray > controllers.Length) { return null; }
            return controllers[indexInArray];
        }

        /// <summary>Returns the first controller found with this name (case does not matter) or null.</summary>
        public SelectableController GetController(string name) {
            foreach (SelectableController ct in controllers) {
                if (ct.name.ToLower() == name.ToLower()) { return ct; }
            }
            return null;
        }

        /// <summary>Switch to the previous controller if there is one.</summary>
        public void SwitchToPreviousController() {
            if (selectedController_previous == null) { return; }
            Debug.Log("Switching controller to previous one (" + selectedController.name + ")");
            AttachController(selectedController_previous);
        }




        // TEST SECTION ===================>>

        public void ShowRadialTest(Transform spawn) {
            ShowRadial(spawn, spawn.forward, spawn.right);
        }

        public void HideRadialTest() {
            HideRadial();
        }

        // TEST SECTION <<===================



        /// <summary>Shows radial controller selection.</summary>
        private void ShowRadial(Transform spawn, Vector3 forward, Vector3 right) {
            
            if (showingRadial) { return; }

            // change hover layermask of hand
            if (!previousMaskSet) {
                previousMask = hand.hoverLayerMask;
                previousMaskSet = true;
            }
            hand.hoverLayerMask = hoverLayerMask;

            // create new array to keep instances of controllers
            radialObjects = new RadialObject[controllers.Length];

            // create parent object
            if (radialParent) { Destroy(radialParent.gameObject); }
            GameObject radialParentObj = new GameObject("Controller Radial");
            radialParentObj.transform.position = spawn.position;
            radialParent = radialParentObj.transform;

            // instantiate controller preview in radial layout around spawn position
            float arcLength = Mathf.PI;
            float radius = radialRadius; // how far away controllers are from the center
            int steps = controllers.Length + 1; // because controllers should be centered
            int i = 0;

            // possible optimization (if required):
            // - controllers are spawned from right to left
            // - imagine splitting the arc in the middle,
            //   then positions on left side are just mirrored ones of the right side
            // - so instead of calculating the positions on the left side,
            //   use the previously calculated ones and negate the x-value to mirror them
            foreach (SelectableController controller in controllers) {

                // check that item package exists
                if (!controller.itemPackage && controller.attachToHand) {
                    Debug.LogWarning("Controller " + i + " is missing ItemPackage!");
                    //continue;
                }

                // check preview prefab
                GameObject prefab = controller.previewPrefab;
                if (!prefab) { prefab = controller.itemPackage.previewPrefab; }
                if (!prefab) { Debug.LogError("Missing controller preview prefab!"); continue; }

                // get the position on the arc
                float invert = spawnLeftRight ? -1 : 1;
                float radialValue = ((i+1) / (float) steps) * arcLength;
                Vector2 arcPos = new Vector2(Mathf.Cos(radialValue) * invert, Mathf.Sin(radialValue));
                //Vector3 arcPos3 = forward * arcPos.y + right * arcPos.x;
                Vector3 arcPos3 = new Vector3(arcPos.x, 0, arcPos.y);
                //Debug.Log("Controller " + i + ": Radial Value = " + radialValue + ", Position = " + arcPos);

                // spawn new instance and rotate controller in the arc positions direction
                // then use the forward vector of the object (looking at the arcPos) with the radius
                GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                go.transform.LookAt(arcPos3);
                go.transform.position += go.transform.forward * radius;
                go.transform.SetParent(radialParent, false); // false to use position of parent which is at controller position
                go.transform.localScale = go.transform.localScale * controller.previewScale;
                go.AddComponent<RadialController>(); // for selection evaluation
                controller.currentRadialInstance = go;

                // spawn UI element showing information
                if (infoUIPrefab != null && controller.showUI) {

                    // spawn and position
                    GameObject uiInstance = Instantiate(infoUIPrefab, go.transform, true);
                    uiInstance.transform.position = go.transform.position + go.transform.forward * 0.1f;
                    Quaternion uiRot = go.transform.rotation;
                    uiRot.x = uiRot.z = 0;
                    uiRot.y += (controller.uiYRotation / 360f);
                    uiRot = Quaternion.Euler(-radialPlaneRotation) * uiRot;
                    uiInstance.transform.rotation = uiRot;

                    // set information
                    UI.RadialControllerInfo cinfo = uiInstance.GetComponent<UI.RadialControllerInfo>();
                    if (cinfo) {
                        if (cinfo.text_Name) { cinfo.text_Name.text = controller.name; }
                        if (cinfo.text_Description) { cinfo.text_Description.text = controller.description; }
                    }
                }

                // create radial object instance to store information (used to select correct controller later)
                RadialObject instance = new RadialObject {
                    controller = controller,
                    gameObject = go
                };
                radialObjects[i] = instance;
                i++;
            }

            // rotate the whole plane (with all controllers)
            Quaternion rot = spawn.rotation;
            rot.x = 0;
            rot.z = 0;
            radialParent.transform.rotation = rot;
            radialParent.transform.Rotate(radialPlaneRotation, Space.Self);

            showingRadial = true;
            Debug.Log("Opened radial controller menu.");

        }


        /// <summary>Hide the radial controller selection.</summary>
        private void HideRadial() {

            if (!showingRadial) { return; }

            // hide controller pickup hints before destroying the object
            if (hand) { hand.HideGrabHint(); }

            // if a controller was selected, destroy its leftover highlighter object
            if (itemSelected && deleteHighlighter) {

                // ToDo: fixme! sometimes highlighter still visible and not deleted!
                if (hand.hoveringInteractable) {
                    hand.hoveringInteractable.SendMessage("OnHandHoverEnd", hand, SendMessageOptions.DontRequireReceiver);
                }
                GameObject highlighter = GameObject.Find(highlighterName);
                if (highlighter) { DestroyImmediate(highlighter); }
            }

            // use parent element to destroy the game objects
            Destroy(radialParent.gameObject);
            showingRadial = false;
            Debug.Log("Closed radial controller menu.");
        }


        /// <summary>
        /// Get the selected controller from interactable.<para/>
        /// Returns it if valid and returns null if something else was selected (e.g. no registered controller).
        /// </summary>
        private SelectableController GetSelectedController(GameObject gameObject) {

            if (radialObjects == null) { return null; }

            foreach (RadialObject obj in radialObjects) {
                if (obj == null) { continue; }
                if (obj.gameObject.Equals(gameObject)) {
                    return obj.controller;
                }
            }

            return null;
        }


        // =============================>> CODE FROM STEAMVR "ItemPackageSpawner" =============================>>

		//-------------------------------------------------
		private void TakeBackItem( Hand hand ) {

			RemoveMatchingItemsFromHandStack( itemPackage, hand );
			if ( itemPackage.packageType == ItemPackage.ItemPackageType.TwoHanded ) {
				RemoveMatchingItemsFromHandStack( itemPackage, hand.otherHand );
			}

		}

		//-------------------------------------------------
		private void SpawnAndAttachObject( Hand hand, GrabTypes grabType ) {

            // can be the case using script in fallback mode
            if (hand == null) { return; }
            itemIsSpawned = false;

			if ( hand.otherHand != null ) {
				//If the other hand has this item package, take it back from the other hand
				ItemPackage otherHandItemPackage = GetAttachedItemPackage( hand.otherHand );
				if ( otherHandItemPackage == itemPackage ) {
					TakeBackItem( hand.otherHand );
				}
			}

			//if ( showTriggerHint ) { hand.HideGrabHint(); }

			if ( itemPackage.otherHandItemPrefab != null ) {
				if ( hand.otherHand.hoverLocked ) {
					//Debug.Log( "Not attaching objects because other hand is hoverlocked and we can't deliver both items." );
					return;
				}
			}

			// if we're trying to spawn a one-handed item, remove one and two-handed items from this hand and two-handed items from both hands
			if ( itemPackage.packageType == ItemPackage.ItemPackageType.OneHanded ) {
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.OneHanded, hand );
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.TwoHanded, hand );
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.TwoHanded, hand.otherHand );
			}

			// if we're trying to spawn a two-handed item, remove one and two-handed items from both hands
			if ( itemPackage.packageType == ItemPackage.ItemPackageType.TwoHanded ) {
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.OneHanded, hand );
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.OneHanded, hand.otherHand );
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.TwoHanded, hand );
				RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType.TwoHanded, hand.otherHand );
			}

			spawnedItem = Instantiate( itemPackage.itemPrefab );
			spawnedItem.SetActive( true );

            // ToDo: maybe find better solution for setting the initial controller position
            // set at same location as hand
            spawnedItem.transform.position = hand.transform.position;
            spawnedItem.transform.rotation = hand.transform.rotation;

			hand.AttachObject( spawnedItem, grabType, attachmentFlags );

			if (itemPackage.otherHandItemPrefab != null && hand.otherHand.isActive) {

				GameObject otherHandObjectToAttach = Instantiate( itemPackage.otherHandItemPrefab );
				otherHandObjectToAttach.SetActive( true );
				hand.otherHand.AttachObject( otherHandObjectToAttach, grabType, attachmentFlags );
			}

			itemIsSpawned = true;
		}

		//-------------------------------------------------
		private void RemoveMatchingItemsFromHandStack( ItemPackage package, Hand hand )
		{
			for ( int i = 0; i < hand.AttachedObjects.Count; i++ )
			{
				ItemPackageReference packageReference = hand.AttachedObjects[i].attachedObject.GetComponent<ItemPackageReference>();
				if ( packageReference != null )
				{
					ItemPackage attachedObjectItemPackage = packageReference.itemPackage;
					if ( ( attachedObjectItemPackage != null ) && ( attachedObjectItemPackage == package ) )
					{
						GameObject detachedItem = hand.AttachedObjects[i].attachedObject;
						hand.DetachObject( detachedItem );
					}
				}
			}
		}

		//-------------------------------------------------
		private ItemPackage GetAttachedItemPackage( Hand hand )
		{
			GameObject currentAttachedObject = hand.currentAttachedObject;

			if ( currentAttachedObject == null ) // verify the hand is holding something
			{
				return null;
			}

			ItemPackageReference packageReference = hand.currentAttachedObject.GetComponent<ItemPackageReference>();
			if ( packageReference == null ) // verify the item in the hand is matchable
			{
				return null;
			}

			ItemPackage attachedItemPackage = packageReference.itemPackage; // return the ItemPackage reference we find.

			return attachedItemPackage;
		}

		//-------------------------------------------------
		private void RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType packageType, Hand hand )
		{
			for ( int i = 0; i < hand.AttachedObjects.Count; i++ )
			{
				ItemPackageReference packageReference = hand.AttachedObjects[i].attachedObject.GetComponent<ItemPackageReference>();
				if ( packageReference != null )
				{
					if ( packageReference.itemPackage.packageType == packageType )
					{
						GameObject detachedItem = hand.AttachedObjects[i].attachedObject;
						hand.DetachObject( detachedItem );
					}
				}
			}
		}

        // <<============================= CODE FROM STEAMVR "ItemPackageSpawner" <<=============================

    }
}
