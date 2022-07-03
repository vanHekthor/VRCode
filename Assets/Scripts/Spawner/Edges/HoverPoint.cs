using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Valve.VR.InteractionSystem;
using VRVis.IO;
using VRVis.UI;

namespace VRVis.Spawner.Edges {

    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(ControlFlowHoverUI))]
    public class HoverPoint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

        public Transform circleTransform;

        public CodeWindowEdgeConnection EdgeConnection { get; private set; }

        private ControlFlowHoverUI hoverUI;
        private SphereCollider sphereCollider;

        private FileSpawner fs;

        private void Awake() {
            hoverUI = GetComponent<ControlFlowHoverUI>();
            sphereCollider = GetComponent<SphereCollider>();
            fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        }

        public void ChangeSize(float radius) {
            circleTransform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
            sphereCollider.radius = radius;
        }

        public void AttachToControlFlowEdge(CodeWindowEdgeConnection edge) {
            EdgeConnection = edge;
        }

        public void PointerEntered(Hand hand) {
            hoverUI.PointerEntered(hand);
        }

        public void PointerExit(Hand hand) {
            hoverUI.PointerExit(hand);
        }

        private bool entered = false;
        private Vector3 originalScale;
        public void OnPointerEnter(PointerEventData eventData) {
            if (!entered) {
                originalScale = transform.localScale;
                transform.localScale = 1.2f * originalScale;
            }
            entered = true;
            exited = false;
        }

        private bool exited = false;
        public void OnPointerExit(PointerEventData eventData) {
            if (!exited) {
                transform.localScale = originalScale;
            }
            exited = true;
            entered = false;
        }

        public void OnPointerClick(PointerEventData eventData) {
            fs.edgeSpawner.RemoveSingleEdgeConnection(EdgeConnection.GetStartCodeFileInstance(), EdgeConnection.GetEdge());
        }
    }
}