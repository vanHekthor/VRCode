using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Spawner;
using VRVis.Spawner.File;
using VRVis.UI.Helper;
using VRVis.Utilities;

namespace VRVis.Interaction.Telekinesis {
    public class TelekinesableCodeWindow : ATelekineticGrabElement {
        private GameObject codeWindow;
        private SphereGrid grid;
        private GridElement gridElement;
        private SphereGridPoint selectedGridPoint;
        private ZoomCodeWindowButton zoomButton;
        private bool moveWindowOnSphere;
        private RectTransform codeCanvasRect;

        protected override void Initialize() {
            codeWindow = GetComponentInParent<CodeFileReferences>().gameObject;
            if (codeWindow == null) {
                Debug.LogError("Code window instance not found!");
            }

            gridElement = codeWindow.GetComponent<GridElement>();
            if (gridElement == null) {
                Debug.LogError("Code window instance has no grid element component!");
            }

            grid = gridElement.Grid;
            zoomButton = codeWindow.GetComponentInChildren<ZoomCodeWindowButton>();
            if (zoomButton == null) {
                Debug.LogError("Code window instance has no zoom button!");
            }

            codeCanvasRect = stretchTransform.GetComponent<RectTransform>();
            if (codeCanvasRect == null) {
                Debug.LogError("Object to be stretched has no RectTransform!");
            }

            targetPoint = grid.GetGridPoint(gridElement.GridPositionLayer, gridElement.GridPositionColumn).AttachmentPointObject.transform;

            if (grid != null) {
                moveWindowOnSphere = true;
            }
        }

        protected override void WasGrabbed() {
            grid.DetachGridElement(ref gridElement);
        }

        protected override void WasPulled() {
            moveWindowOnSphere = false;
        }

        protected override void IsBeingDragged(Transform pointer) {
            if (moveWindowOnSphere) {
                SetTargetToClosestGridPoint(out selectedGridPoint, pointer.position);
            }

            if (attachedToHand) {
                codeWindow.transform.position = telekineticAttachmentPoint.position;
                codeWindow.transform.rotation = telekineticAttachmentPoint.rotation;
            }
        }

        protected override void WasReleased(Ray ray) {
            moveWindowOnSphere = true;

            float radius = grid.screenSphere.GetComponent<SphereCollider>().radius * grid.screenSphere.transform.lossyScale.x;
            double t = PositionOnSphere.SphereIntersect(radius, grid.screenSphere.transform.position, ray.origin, ray.direction);
            Vector3 pointPosOnSphere = ray.origin + (float)t * ray.direction;

            GameObject pointObject = new GameObject("PointObject");
            pointObject.transform.position = pointPosOnSphere;

            SetTargetToClosestGridPoint(out selectedGridPoint, pointPosOnSphere);
            grid.AttachGridElement(ref gridElement, selectedGridPoint.LayerIdx, selectedGridPoint.ColumnIdx);
        }

        float initialHeight;
        bool initialHeightWasSet = false;
        public override void OnStretch(float factor) {
            if (!initialHeightWasSet) {
                initialHeight = codeCanvasRect.sizeDelta.y;
                initialHeightWasSet = true;
            }

            codeCanvasRect.sizeDelta = new Vector2(codeCanvasRect.sizeDelta.x, initialHeight * factor);
        }

        public override void OnStretchEnded() {
            initialHeightWasSet = false;
        }

        private void SetTargetToClosestGridPoint(out SphereGridPoint sphereGridPoint, Vector3 pointOnWindowSphere) {
            Vector3 pointerPos = pointOnWindowSphere;

            sphereGridPoint = grid.GetClosestGridPoint(pointerPos);
            Vector3 previewPos = selectedGridPoint.AttachmentPoint;
            Vector3 lookDirection = previewPos - grid.screenSphere.transform.position;
            Quaternion previewRot = Quaternion.LookRotation(lookDirection);

            if ((codeWindow.transform.position - previewPos).magnitude > 0.1f) {
                ChangeTargetPoint(selectedGridPoint.AttachmentPointObject.transform);
            }
        }        
    }
}