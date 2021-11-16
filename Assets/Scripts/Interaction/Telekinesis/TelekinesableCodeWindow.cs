using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        protected override void Initialize() {
            codeWindow = GetComponentInParent<CodeFileReferences>().gameObject;
            gridElement = codeWindow.GetComponent<GridElement>();
            grid = gridElement.Grid;
            zoomButton = codeWindow.GetComponentInChildren<ZoomCodeWindowButton>();

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