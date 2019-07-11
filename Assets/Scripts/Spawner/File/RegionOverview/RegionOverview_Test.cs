using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VRVis.Spawner.File {

    /// <summary>
    /// Takes care of dealing with input events to the region overview.<para/>
    /// This overview shows all the file regions in a summarized way using a texture.
    /// </summary>
    public class RegionOverview_Test : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

        private PointerEventData ped;
        private bool pedIn = false;

        private Vector3 gizmoPos;
        private bool drawGrizmo = false;


        void Start () {
		
	    }
	

	    void Update () {
		
            drawGrizmo = false;
            if (pedIn && ped != null) {

                RaycastHit hitInf;
                if (CanBeSeen(ped, out hitInf)) {
                    gizmoPos = hitInf.point;
                    drawGrizmo = true;
                }
            }
	    }


        private void OnDrawGizmos() {

            if (drawGrizmo) {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(gizmoPos, 0.1f);
            }
        }


        /// <summary>
        /// Check if this object can be seen and is not occluded.
        /// </summary>
        private bool CanBeSeen(PointerEventData dat, out RaycastHit hitInf) {

            // ToDo: stereoscopic?
            Ray ray = dat.enterEventCamera.ScreenPointToRay(dat.position);
            
            //Debug.DrawRay(ray.origin, ray.direction);
            if (Physics.Raycast(ray, out hitInf)) {
                if (hitInf.collider.gameObject == gameObject) { return true; }
            }

            return false;
        }


        /// <summary>
        /// Detects a pointer enter event.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData) {
        
            //Debug.Log("Pointer entered region overview! (pos: " +
            //    eventData.position + ", world: " + eventData.pointerCurrentRaycast.worldPosition + ")");
            
            ped = eventData;
            pedIn = true;
        }

        public void OnPointerExit(PointerEventData eventData) {
            
            //Debug.Log("Pointer left region overview!");
            pedIn = false;
        }

    }
}
