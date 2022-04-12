using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Interaction.LaserHand;
using VRVis.Interaction.LaserPointer;
using VRVis.Spawner.CodeCity;

namespace VRVis.UI.CodeCity {

    /// <summary>
    /// User interface to show on hover.<para/>
    /// This script is attached to the code city elements.<para/>
    /// Last Updated: 22.08.2019
    /// </summary>
    public class CodeCityHoverUI : MonoBehaviour {

        [Tooltip("Prefab of the hover UI that has the CCUITextAdder component attached")]
        public GameObject uiPrefab;

        [Tooltip("Positional offset")]
        public Vector3 uiPosition;
        public Vector3 uiRotation;

        [Tooltip("UI distance from the camera")]
        public float uiDistance = 1;

        [Tooltip("Collision layers to avoid when placing the UI")]
        public LayerMask collisionLayerMask;

        [Tooltip("Collision radius to check")]
        public float uiCollisionRadius = 0.15f;

        private GameObject uiInstance;
        private Transform laserOrigin;


        void Update() {

            if (uiInstance) {

                // calculate position
                Vector3 pos = CalculatePosition(uiDistance);
                Vector3 camPos = Camera.main.transform.position;

                // ToDo: maybe use "Physics.BoxCast()" to improve behaviour
                // perform raycast from hand position to the desired UI position
                // and check if there is anything between to avoid putting the UI inside an element
                Vector3 handPos = laserOrigin.transform.position;
                Vector3 handToPos = pos - handPos;
                Ray ray = new Ray(handPos, handToPos);
                RaycastHit hitInfo = new RaycastHit();
                float max_ray_dist = handToPos.magnitude;
                float radius = uiCollisionRadius;
                if (Physics.SphereCast(ray, radius, out hitInfo, max_ray_dist - uiCollisionRadius * 0.5f, collisionLayerMask)) {
                    if (hitInfo.collider.gameObject != uiInstance) { pos = CalculatePosition(hitInfo.distance); }
                }

                // assign ui position and rotate to camera
                uiInstance.transform.position = pos;
                Vector3 rotDir = camPos - pos;
                uiInstance.transform.rotation = Quaternion.LookRotation(-rotDir);
            }
        }


        /// <summary>
        /// Calculate the UI position.
        /// </summary>
        private Vector3 CalculatePosition(float distance) {
            Vector3 fw = laserOrigin.transform.forward;
            Vector3 u = Vector3.up;
            Vector3 r = -Vector3.Cross(fw, u); // direction "to the right"
            return laserOrigin.transform.position + (fw * uiPosition.z + r * uiPosition.x + u * uiPosition.y).normalized * distance;
        }


        /// <summary>
        /// Called through SendMessage method call by Laser Pointer.
        /// </summary>
        public void PointerEntered(Hand hand) {
            AttachUI(hand);
        }

        /// <summary>
        /// Called through SendMessage method call by Laser Pointer.
        /// </summary>
        public void PointerExit(Hand hand) {
            DetachUI();
        }


        /// <summary>
        /// Spawn the UI for this hand.
        /// </summary>
        private void AttachUI(Hand hand) {

            // parent to controller or to hand itself
            laserOrigin = hand.transform;                       

            var laser = hand.transform.GetComponentInChildren<LaserHand>().transform;
            if (!laser) {
                if (hand.currentAttachedObject != null) {
                    laser = hand.currentAttachedObject.transform;
                }
            }
            else {
                laserOrigin = laser;
            }
        

            uiInstance = Instantiate(uiPrefab);//, parentTo);
            uiInstance.transform.localPosition = uiPosition;
            uiInstance.transform.localRotation = Quaternion.Euler(uiRotation);

            // set according structure information
            UpdateUIInfo();
        }


        /// <summary>
        /// Detach hover UI from hand.
        /// </summary>
        private void DetachUI() {
            if (uiInstance) { Destroy(uiInstance); }
        }


        /// <summary>
        /// Update the shown information using code city element components.
        /// </summary>
        private void UpdateUIInfo() {

            // we can get the node information from this object
            // because this component is attached to it
            CodeCityElement e = GetComponent<CodeCityElement>();
            if (e) { SetUIInfo(e); return; }
            Debug.LogWarning("Failed to set UI info! - No CodeCityElement component found.", this);
        }


        /// <summary>
        /// Set UI information (text contents) using the text adder.
        /// </summary>
        private void SetUIInfo(CodeCityElement e) {
            
            CCUITextAdder tadder = uiInstance.GetComponent<CCUITextAdder>();
            if (!tadder) { return; }
            e.GetInfo().ForEach((info) => { tadder.AddText(info.Key, info.Value); });
        }

    }
}
