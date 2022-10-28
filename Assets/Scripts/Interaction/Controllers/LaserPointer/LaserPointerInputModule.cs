using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Hint:<para/>
/// If this is not working at all (e.g. the Process function is not called)<para/>
/// ensure that you have only one "EventSystem" component in the scene!<para/>
/// 
/// Initial code by Wacki<para/>
/// Modified by S1r0hub (11.2018)<para/>
/// Updated: 19.09.2019
/// </summary>
namespace VRVis.Interaction.LaserPointer {

    public class LaserPointerInputModule : BaseInputModule {

        public static LaserPointerInputModule instance { get { return _instance; } }
        private static LaserPointerInputModule _instance = null;

        public UnityEvent attachedController;

        public LayerMask layerMask;

        // storage class for controller specific data
        public class ControllerData {

            public LaserPointerEventData pointerEvent;
            public GameObject currentPoint;
            public GameObject currentPressed;
            public GameObject currentDragging;

            public void Reset() {
                pointerEvent = null;
                currentPoint = currentPressed = currentDragging = null;
            }
        };

        private Camera UICamera;
        private PhysicsRaycaster raycaster;
        private HashSet<ALaserPointer> _controllers;

        // controller data
        private Dictionary<ALaserPointer, ControllerData> _controllerData = new Dictionary<ALaserPointer, ControllerData>();


        protected override void Awake() {

            base.Awake();

            if(_instance != null) {
                Debug.LogWarning("Tried to instantiate multiple LaserPointerInputModule instances!");
                DestroyImmediate(gameObject);
            }

            _instance = this;
            Debug.Log("LaserPointerInputModule instance created.");
        }


        protected override void Start() {

            base.Start();
            
            // Create a new camera that will be used for raycasts
            UICamera = new GameObject("UI Camera").AddComponent<Camera>();

            // Added PhysicsRaycaster so that pointer events are sent to 3d objects
            raycaster = UICamera.gameObject.AddComponent<PhysicsRaycaster>();
            UICamera.clearFlags = CameraClearFlags.Nothing;
            UICamera.enabled = false;
            UICamera.fieldOfView = 5;
            UICamera.nearClipPlane = 0.01f;

            // Find canvases in the scene and assign our custom UICamera to them
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();            
            foreach (Canvas canvas in canvases) { canvas.worldCamera = UICamera; }
            Debug.Log("LaserPointerInputModule instance prepared.");
        }


        /// <summary>
        /// Adds the controller to be an input provider
        /// and synchronizes the layer masks if desired.
        /// </summary>
        public void AddController(ALaserPointer controller) {
            if (controller.syncLayerMaskInputModule) { controller.rayLayerMask = layerMask; } // synchronize/override layermasks
            _controllerData.Add(controller, new ControllerData());

            attachedController.Invoke();

            Debug.Log("Laser pointer registered");
        }

        public void RemoveController(ALaserPointer controller) {
            _controllerData.Remove(controller);
        }


        protected void UpdateCameraPosition(ALaserPointer controller) {

            // use the laser origin if the user set it
            Transform updateTransform = controller.transform;
            if (controller.laserOrigin) { updateTransform = controller.laserOrigin; }

            // update the camera position and rotation
            UICamera.transform.position = updateTransform.position;
            UICamera.transform.rotation = updateTransform.rotation;
        }


        // clear the current selection
        public void ClearSelection() {
            if(eventSystem.currentSelectedGameObject) {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        // select a game object
        private void Select(GameObject go) {
            ClearSelection();

            if(ExecuteEvents.GetEventHandler<ISelectHandler>(go)) {
                eventSystem.SetSelectedGameObject(go);
            }
        }


        public override void Process() {

            //Debug.Log("InputModule Process call...");
            raycaster.eventMask = layerMask;

            foreach (var pair in _controllerData) {

                ALaserPointer controller = pair.Key;
                ControllerData data = pair.Value;

                // use controller layermask if desired
                if (controller.overrideInputModuleLayerMask) {
                    raycaster.eventMask = controller.rayLayerMask;
                }

                // skip raycasting and events in general
                // if the layer is currently not enabled
                if (!controller.IsLaserActive()) {
                    ClearSelection();
                    data.Reset();
                    continue;
                }

                // test if UICamera is looking at a GUI element
                UpdateCameraPosition(controller);

                if (data.pointerEvent == null) { data.pointerEvent = new LaserPointerEventData(eventSystem); }
                else { data.pointerEvent.Reset(); }

                data.pointerEvent.controller = controller;
                data.pointerEvent.delta = Vector2.zero;
                data.pointerEvent.position = new Vector2(UICamera.pixelWidth * 0.5f, UICamera.pixelHeight * 0.5f);
                //data.pointerEvent.scrollDelta = Vector2.zero;

                // trigger a raycast
                eventSystem.RaycastAll(data.pointerEvent, m_RaycastResultCache);
                //data.pointerEvent.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache); // is buggy (does not take hit distance into account!)


                // own "FIND RAYCAST" that should work correctly by taking the hit distance into account
                bool minSet = false;
                float minDistance = 0;
                RaycastResult result = new RaycastResult();
                foreach (RaycastResult r in m_RaycastResultCache) {
                    if (r.gameObject == null) { continue; }

                    // rounding is applied because otherwise the min object switches many times
                    // (e.g. if a button with a text is hovered over) - which can lead to weird behaviour.
                    // rounding at 2 decimals after comma is enough precision for now
                    float dist = Mathf.Round(r.distance * 100) / 100f;
                    if (!minSet || dist < minDistance) {
                        minSet = true;
                        minDistance = dist;
                        result = r;
                    }
                }
                data.pointerEvent.pointerCurrentRaycast = result;
                m_RaycastResultCache.Clear();

                
                /*
                // DEBUG / OUTPUT HIT DISTANCES
                GameObject outHitGO = GameObject.Find("RayHitOut");
                if (outHitGO) {
                    TextMeshProUGUI outHit = outHitGO.GetComponent<TextMeshProUGUI>();
                    if (outHit) {

                        System.Text.StringBuilder strb = new System.Text.StringBuilder();
                        foreach (RaycastResult r in m_RaycastResultCache) {
                            strb.Append(r.gameObject.name);
                            //strb.Append(", Distance (Cam): ");
                            //strb.Append(Vector3.Magnitude(r.gameObject.transform.position - UICamera.transform.position));
                            strb.Append(", Distance (Hit): ");
                            strb.Append(r.distance);
                            strb.Append('\n');
                        }
                        strb.Append("\nFINAL: ");
                        strb.Append(data.pointerEvent.pointerCurrentRaycast.gameObject.name);
                        outHit.SetText(strb.ToString());
                    }
                }
                m_RaycastResultCache.Clear();
                */

                // make sure our controller knows about the raycast result
                // we add 0.01 because that is the near plane distance of our camera and we want the correct distance
                if (data.pointerEvent.pointerCurrentRaycast.distance > 0.0f)
                    controller.LimitLaserDistance(data.pointerEvent.pointerCurrentRaycast.distance + 0.01f);

                // Send control enter and exit events to our controller
                var hitControl = data.pointerEvent.pointerCurrentRaycast.gameObject;
                if (data.currentPoint != hitControl) {
                    if (data.currentPoint != null) { controller.OnExitControl(data.currentPoint); }
                    if (hitControl != null) { controller.OnEnterControl(hitControl); }
                }
                data.currentPoint = hitControl;

                // Handle enter and exit events on the GUI controlls that are hit
                HandlePointerExitAndEnter(data.pointerEvent, data.currentPoint);

                if (controller.ButtonDown()) {
                    ClearSelection();

                    data.pointerEvent.pressPosition = data.pointerEvent.position;
                    data.pointerEvent.pointerPressRaycast = data.pointerEvent.pointerCurrentRaycast;
                    data.pointerEvent.pointerPress = null;

                    // update current pressed if the curser is over an element
                    if (data.currentPoint != null) {

                        data.currentPressed = data.currentPoint;
                        data.pointerEvent.current = data.currentPressed;

                        GameObject newPressed = ExecuteEvents.ExecuteHierarchy(data.currentPressed, data.pointerEvent, ExecuteEvents.pointerDownHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerDownHandler);

                        if (newPressed == null) {
                            // some UI elements might only have click handler and not pointer down handler
                            newPressed = ExecuteEvents.ExecuteHierarchy(data.currentPressed, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                            ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                            if (newPressed != null) { data.currentPressed = newPressed; }
                        }
                        else {
                            data.currentPressed = newPressed;
                            // we want to do click on button down at same time, unlike regular mouse processing
                            // which does click when mouse goes up over same object it went down on
                            // reason to do this is head tracking might be jittery and this makes it easier to click buttons
                            ExecuteEvents.Execute(newPressed, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                            ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerClickHandler);
                        }

                        if (newPressed != null) {
                            data.pointerEvent.pointerPress = newPressed;
                            data.currentPressed = newPressed;
                            Select(data.currentPressed);
                        }

                        ExecuteEvents.Execute(data.currentPressed, data.pointerEvent, ExecuteEvents.beginDragHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.beginDragHandler); 

                        data.pointerEvent.pointerDrag = data.currentPressed;
                        data.currentDragging = data.currentPressed;
                    }
                } // button down end


                if (controller.ButtonUp()) {
                    ClearSelection(); // clear selection so that the objects dont stay in highlighted color

                    if (data.currentDragging != null) {
                        data.pointerEvent.current = data.currentDragging;

                        ExecuteEvents.Execute(data.currentDragging, data.pointerEvent, ExecuteEvents.endDragHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.endDragHandler);
                        if (data.currentPoint != null) {
                            ExecuteEvents.ExecuteHierarchy(data.currentPoint, data.pointerEvent, ExecuteEvents.dropHandler);
                        }

                        data.pointerEvent.pointerDrag = null;
                        data.currentDragging = null;
                    }

                    if (data.currentPressed) {
                        data.pointerEvent.current = data.currentPressed;

                        ExecuteEvents.Execute(data.currentPressed, data.pointerEvent, ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.pointerUpHandler);

                        data.pointerEvent.rawPointerPress = null;
                        data.pointerEvent.pointerPress = null;
                        data.currentPressed = null;
                    }

                } // button up end


                // drag handling
                if (data.currentDragging != null) {
                    data.pointerEvent.current = data.currentPressed;
                    ExecuteEvents.Execute(data.currentDragging, data.pointerEvent, ExecuteEvents.dragHandler);
                    ExecuteEvents.Execute(controller.gameObject, data.pointerEvent, ExecuteEvents.dragHandler);
                }


                // update selected element for keyboard focus
                if (eventSystem.currentSelectedGameObject != null) {
                    data.pointerEvent.current = eventSystem.currentSelectedGameObject;
                    ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
                    //ExecuteEvents.Execute(controller.gameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
                }


                // scroll event handling
                if (data.currentPoint != null && controller.IsScrolling()) {
                    data.pointerEvent.current = data.currentPoint;
                    data.pointerEvent.scrollDelta = controller.GetScrollDelta();
                    ExecuteEvents.Execute(data.currentPoint, data.pointerEvent, ExecuteEvents.scrollHandler);
                    ExecuteEvents.ExecuteHierarchy(data.currentPoint, data.pointerEvent, ExecuteEvents.scrollHandler);
                }
            }
        }

    }
}